# PaL.X - Guide de démarrage

## Prérequis
1. .NET 9 SDK
2. PostgreSQL avec pgAdmin
3. Visual Studio 2022 ou VS Code

## Configuration

### 1. Base de données
```bash
# Créer la base de données
psql -h localhost -U postgres -c "CREATE DATABASE \"PaL.X\";"
```

Configurer ensuite la connexion côté API via variable d'environnement (recommandé) :

- `ConnectionStrings__DefaultConnection` (ex: `Host=localhost;Database=PaL.X;Username=postgres;Password=YOUR_PASSWORD;Port=5432`)
- `Jwt__Key` (clé JWT pour l'auth)

### 2. Démarrage de l'application
Terminal 1 - API:
```bash
cd src/PaL.X.Api
dotnet run
```

Terminal 2 - Client:
```bash
cd src/PaL.X.Client
dotnet run
```

Terminal 3 - Admin:
```bash
cd src/PaL.X.Admin
dotnet run
```

## Comptes de test
Des comptes de test peuvent être générés via le seeding (si activé côté API). Pour un dépôt public, évitez de publier des identifiants/mots de passe réels.

## Dépannage
**L'API ne démarre pas**
- Vérifier que PostgreSQL est en cours d'exécution
- Vérifier la chaîne de connexion dans `appsettings.json`
- Exécuter les migrations: `dotnet ef database update`

**Les clients ne peuvent pas se connecter**
- Vérifier que l'API est en cours d'exécution
- Vérifier que le service a été démarré par l'admin
- Vérifier les logs de l'API

**Problèmes de certificat SSL**
- Pour le développement, vous pouvez désactiver la validation SSL dans les clients
- Ou générer un certificat de développement valide