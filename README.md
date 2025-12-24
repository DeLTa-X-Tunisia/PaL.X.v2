# PaL.X v2 🚀

**PaL.X** est une solution complète de messagerie instantanée et d'administration système, conçue avec une architecture hybride moderne alliant la puissance de **.NET** et la flexibilité des technologies web via **WebView2**.

![PaL.X Banner](https://via.placeholder.com/1000x300?text=PaL.X+v2+Communication+System)

## 📋 Fonctionnalités

### 📱 Client de Messagerie (PaL.X.App)
*   **Chat en Temps Réel** : Communication instantanée fluide grâce à SignalR.
*   **Interface Moderne** : UI soignée en HTML5/CSS3 intégrée dans une application native.
*   **Affichage Personnalisé** : Affichage du nom complet (Nom + Prénom) pour une expérience plus conviviale.
*   **Appels Audio & Vidéo** : Support WebRTC pour des communications claires.
*   **Partage de Fichiers** : Échangez des documents et des médias facilement.
*   **Gestion de Profil** : Personnalisation des avatars, statuts et informations utilisateur.

### 🛠️ Panneau d'Administration (PaL.X.Admin)
*   **Contrôle du Serveur** : Démarrage, arrêt et surveillance du serveur central.
*   **Gestion des Utilisateurs** : Supervision des comptes et des accès.
*   **Suivi des Sessions** : Historique complet des connexions et déconnexions utilisateurs.
*   **Logs & Monitoring** : Suivi de l'activité du système en temps réel.
*   **Mode Maintenance** : Interface dédiée pour les opérations de maintenance.

### 🖥️ Serveur (PaL.X.Server)
*   **API RESTful** : Backend robuste pour la gestion des données.
*   **Hub SignalR** : Cœur de la communication temps réel.
*   **Sécurité** : Authentification et gestion des sessions.

## 🏗️ Architecture Technique

Le projet est structuré en plusieurs modules :

*   `src/PaL.X.App` : Le client utilisateur (WinForms + WebView2).
*   `src/PaL.X.Admin` : La console d'administration (WinForms + WebView2).
*   `src/PaL.X.Server` : Le serveur API et WebSocket (ASP.NET Core).
*   `src/PaL.X.Shared` : Bibliothèque de modèles et DTOs partagés.
*   `src/PaL.X.Data` : Couche d'accès aux données (Entity Framework).

## 🚀 Installation et Démarrage

### Prérequis
*   [.NET 8.0 SDK](https://dotnet.microsoft.com/download) (ou version ultérieure)
*   [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (généralement préinstallé sur Windows 10/11)
*   Git

### Installation

1.  **Cloner le dépôt**
    ```bash
    git clone https://github.com/DeLTa-X-Tunisia/PaL.X.v2.git
    cd PaL.X.v2
    ```

2.  **Lancer l'application**
    Un script batch automatisé est disponible pour compiler et lancer tous les composants nécessaires.

    ```batch
    .\start_all.bat
    ```
    *Ce script se charge de démarrer le serveur, puis l'interface d'administration et le client.*

## 🔧 Configuration

Les fichiers de configuration se trouvent dans `appsettings.json` pour chaque projet (Server, Admin, App). Vous pouvez y configurer :
*   Les chaînes de connexion à la base de données.
*   Les ports d'écoute du serveur.
*   Les paramètres de journalisation.

## 🤝 Contribution

Les contributions sont les bienvenues ! Pour contribuer :
1.  Forkez le projet.
2.  Créez votre branche de fonctionnalité (`git checkout -b feature/AmazingFeature`).
3.  Commitez vos changements (`git commit -m 'Add some AmazingFeature'`).
4.  Poussez vers la branche (`git push origin feature/AmazingFeature`).
5.  Ouvrez une Pull Request.

## 📄 Licence

Distribué sous la licence MIT. Voir `LICENSE` pour plus d'informations.

---
*Développé avec ❤️ par l'équipe DeLTa-X Tunisia.*
