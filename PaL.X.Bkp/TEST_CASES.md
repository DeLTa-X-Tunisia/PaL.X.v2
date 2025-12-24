# Cas de test pour PaL.X

## Test 1: Base de données
- [ ] PostgreSQL est en cours d'exécution
- [ ] La base "PaL.X" existe
- [ ] La table "Users" est créée
- [ ] L'utilisateur admin par défaut est créé

## Test 2: API Backend
- [ ] L'API démarre sans erreur (https://localhost:5001)
- [ ] Swagger est accessible (https://localhost:5001/swagger)
- [ ] Endpoint /api/service/check répond
- [ ] Endpoint /api/auth/register fonctionne
- [ ] Endpoint /api/auth/login fonctionne

## Test 3: Application Client
- [ ] Le formulaire de login s'affiche
- [ ] L'inscription d'un nouvel utilisateur fonctionne
- [ ] La connexion avec un utilisateur existant fonctionne
- [ ] La MainForm s'affiche après connexion
- [ ] Le statut du service est affiché
- [ ] La déconnexion fonctionne

## Test 4: Application Admin
- [ ] Le formulaire de login admin s'affiche
- [ ] La connexion admin nécessite le flag IsAdmin=true
- [ ] La MainForm admin s'affiche
- [ ] Le bouton "Démarrer le Service" fonctionne
- [ ] Le bouton "Arrêter le Service" fonctionne
- [ ] La liste des clients se met à jour

## Test 5: Fonctionnalités principales
- [ ] Un client ne peut pas se connecter si le service est arrêté
- [ ] L'admin peut démarrer/arrêter le service
- [ ] Quand l'admin arrête le service, les clients sont déconnectés
- [ ] L'admin peut voir la liste des clients connectés
- [ ] L'admin peut déconnecter tous les clients