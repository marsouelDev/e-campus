using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Backend.Services;

public class EmailService(IConfiguration config, ILogger<EmailService> logger)
{
    private string AppName => config["App:Name"] ?? "E-Campus";
    private string LoginUrl => config["App:LoginUrl"] ?? "";

    /// <summary>
    /// Envoie un email. Renvoie false en cas d'échec, sans planter l'appelant.
    /// Avec Email:Username (Gmail) : chiffrement + authentification.
    /// Sans Email:Username (Mailpit en développement) : ni l'un ni l'autre.
    /// </summary>
    public async Task<bool> SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            var s = config.GetSection("Email");

            var host = s["Host"];
            var username = s["Username"];
            var password = s["Password"];
            var fromAddress = s["FromAddress"];
            var fromName = s["FromName"] ?? AppName;

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress) ||
                !int.TryParse(s["Port"], out var port))
            {
                logger.LogError("Configuration Email incomplète (Host, Port, FromAddress). " +
                                "Vérifie docker-compose, .env, appsettings.json ou les user-secrets.");
                return false;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient { Timeout = 15000 };   // échoue en 15 s au lieu de 2 min

            if (string.IsNullOrWhiteSpace(username))
            {
                // Pas d'identifiants (Mailpit) : ni chiffrement, ni authentification
                await client.ConnectAsync(host, port, SecureSocketOptions.None);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    logger.LogError("Email:Username est défini mais Email:Password est vide.");
                    return false;
                }

                // Port 465 = SSL direct ; 587 = STARTTLS
                await client.ConnectAsync(host, port,
                    port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(username, password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("Email « {Subject} » envoyé à {Email}", subject, toEmail);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Échec de l'envoi de l'email à {Email}", toEmail);
            return false;
        }
    }

    public Task<bool> SendWelcomeAsync(string toEmail, string fullName, string defaultPassword) =>
        SendAsync(toEmail, fullName, $"Bienvenue sur {AppName}", Template(
            fullName,
            $"Un compte a été créé pour vous sur <b>{WebUtility.HtmlEncode(AppName)}</b>.",
            toEmail, defaultPassword));

    public Task<bool> SendResetAsync(string toEmail, string fullName, string defaultPassword) =>
        SendAsync(toEmail, fullName, $"Votre mot de passe a été réinitialisé ({AppName})", Template(
            fullName,
            "Votre mot de passe a été réinitialisé par un administrateur.",
            toEmail, defaultPassword));

    private string Template(string fullName, string intro, string email, string password) => $"""
        <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto">
          <p>Bonjour {WebUtility.HtmlEncode(fullName)},</p>
          <p>{intro}</p>
          <table style="background:#f4f4f4;padding:12px;border-radius:6px">
            <tr><td>Email :</td><td><b>{WebUtility.HtmlEncode(email)}</b></td></tr>
            <tr><td>Mot de passe :</td><td><b>{WebUtility.HtmlEncode(password)}</b></td></tr>
          </table>
          <p><a href="{WebUtility.HtmlEncode(LoginUrl)}">Se connecter</a></p>
          <p style="color:#555;font-size:13px">Nous vous conseillons de changer ce mot de passe
          depuis la page « Mon profil » après votre première connexion.</p>
        </div>
        """;
}