FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Backend.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish Backend.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Backend.dll"]