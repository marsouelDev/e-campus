# E-Campus

## Prérequis
- .NET 10 SDK, Node.js, MySQL

## Backend

cd backend
dotnet restore

# Secrets (à remplacer par vos valeurs)
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=ecampus;User=root;Password=VOTRE_MDP;"
dotnet user-secrets set "Jwt:Key" "UNE_CLE_ALEATOIRE_DE_PLUS_DE_32_CARACTERES"
dotnet user-secrets set "SeedAdmin:Password" "MOT_DE_PASSE_ADMIN"
dotnet user-secrets set "Email:Password" "CODE_SMTP"

# Adapter Email:Username et Email:FromAddress dans appsettings.json

dotnet run

Les migrations sont appliquées automatiquement au démarrage.
L'admin est créé avec l'email de SeedAdmin:Email.

## Frontend

cd frontend
npm install
# Créer .env.local avec : API_URL=
npm run dev

pour générer Tailwind proprement

Arrêtez le terminal Tailwind (Ctrl+C), puis relancez-le dans D:\Project\e-campus\frontend :
npx @tailwindcss/cli -i ./Styles/app.css -o ./wwwroot/css/tailwind.css --watch