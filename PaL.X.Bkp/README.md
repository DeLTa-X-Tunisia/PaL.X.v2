<div align="center">

# 💬 # PaL.X - Plateforme de Communication Unifiée

<p align="center">
  <img src="https://img.shields.io/badge/Author-Azizi%20Mounir-blue?style=for-the-badge" alt="Author: Azizi Mounir">
  <img src="https://upload.wikimedia.org/wikipedia/commons/c/ce/Flag_of_Tunisia.svg" alt="Flag of Tunisia" width="60" height="40">
  <img src="https://img.shields.io/badge/Phone-%2B216%20XX%20XXX%20XXX-006400?style=for-the-badge" alt="Phone: +216 27 774075">
</p>

### *Private and Lightweight eXchange — 100% C# / .NET*

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![SignalR](https://img.shields.io/badge/SignalR-Real--Time-8A2BE2?logo=microsoft)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![WebRTC](https://img.shields.io/badge/WebRTC-Enabled-00D084?logo=webrtc)](https://webrtc.org/)

![UI/UX Custom](https://img.shields.io/badge/UI%2FUX-Custom%20Design-orange?style=for-the-badge)
![Code Optimisé](https://img.shields.io/badge/Code-Optimisé%20C%23-brightgreen?style=for-the-badge)

[![Enterprise Scale](https://img.shields.io/badge/Enterprise-500K%20Users-FF6B6B?style=for-the-badge&logo=rocket)](README.md#enterprise-scale)
[![Zero-Cost](https://img.shields.io/badge/Deployment-Zero%20Cost-4CAF50?style=for-the-badge)](README.md#deployment)

[![Bugs Squashed](https://img.shields.io/badge/Bugs%20Squashed-∞-success?logo=github)](docs/troubleshooting.md)
[![Coffee Consumed](https://img.shields.io/badge/Coffee%20Consumed-%E2%98%95%20%E2%98%95%20%E2%98%95-brown)](https://en.wikipedia.org/wiki/Coffee)
[![Code Quality](https://img.shields.io/badge/Code%20Quality-Chef's%20Kiss-ff69b4?logo=chef)](https://github.com/DeLTa-X-Tunisia/PaL.X)
</div>


PaL.X est une suite de communication complète et moderne développée en **.NET 9**. Elle offre une expérience utilisateur riche combinant messagerie instantanée, partage multimédia et appels vidéo haute définition, le tout sécurisé par une architecture robuste.

![PaL.X Banner](https://via.placeholder.com/800x200?text=PaL.X+Communication+Platform)

## 🌟 Fonctionnalités Détaillées

### 💬 Messagerie & Chat Complet
Une expérience de chat fluide et interactive :
*   **Messagerie Instantanée** : Échanges en temps réel ultra-rapides via **SignalR**.
*   **Smileys & Émojis** : Support étendu de packs de smileys (Basic, Premium, Animés) pour enrichir les conversations.
*   **Mise en forme** : Support du texte riche (couleurs, polices, styles).
*   **Statuts de Présence** : Gestion dynamique des statuts (En ligne, Occupé, Absent, Invisible).

### 📂 Partage Multimédia Avancé
PaL.X va au-delà du simple texte :
*   **Transfert de Fichiers** : Envoi et réception de tout type de documents avec barre de progression.
*   **Partage d'Images** : Prévisualisation et envoi rapide de photos directement dans le chat.
*   **Messages Audio** : Enregistrement vocal intégré et lecteur audio natif pour envoyer des notes vocales.

### 📹 Appels Vidéo & Audio (WebRTC)
Communication en temps réel de nouvelle génération :
*   **Technologie WebRTC** : Appels vidéo P2P haute qualité et faible latence (via WebView2).
*   **Interface Moderne** : Fenêtre d'appel "Dark Theme" immersive.
*   **Contrôles Complets** : Gestion du micro, de la caméra et bascule plein écran.
*   **Menu Contextuel** : Lancement rapide d'appels depuis la liste d'amis.

### 🛡️ Confidentialité & Gestion des Contacts
Un contrôle total sur vos interactions :
*   **Système d'Amis** : Recherche, demande d'ajout et gestion de la liste de contacts.
*   **Système de Blocage Avancé** : 
    *   Bloquez les utilisateurs indésirables pour empêcher tout contact (messages ou appels).
    *   Gestionnaire de liste noire (Blacklist) accessible depuis les paramètres.
    *   Protection immédiate de la vie privée.

### 🔧 Administration Système
Un panneau de contrôle puissant pour les administrateurs :
*   **Dashboard** : Vue d'ensemble des utilisateurs connectés et de l'état du serveur.
*   **Contrôle de Service** : Démarrage et arrêt du backend API à la demande.
*   **Logs Système** : Suivi des événements et diagnostics en temps réel.

---

## 🛠️ Architecture Technique

Le projet repose sur une stack technologique de pointe :

*   **Core Framework** : .NET 9.0 (Dernière version LTS).
*   **Backend API** : ASP.NET Core Web API.
*   **Communication** : SignalR (WebSocket) & WebRTC (Vidéo).
*   **Client Desktop** : Windows Forms (WinForms) modernisé.
*   **Base de Données** : PostgreSQL avec Entity Framework Core.
*   **Sécurité** : Authentification JWT, HTTPS (Port 5001).

---

## ⚙️ Prérequis

Pour exécuter PaL.X, assurez-vous d'avoir :
1.  **SDK .NET 9.0** installé.
2.  **PostgreSQL** (v13+) en cours d'exécution.
3.  **WebView2 Runtime** (Standard sur Windows 10/11).

---

## 🚀 Installation Rapide

1.  **Base de Données** :
    Créez une base vide PaL.X dans PostgreSQL.
    *(Config par défaut : User postgres / Pass 2012704)*

2.  **Démarrage** :
    Utilisez le script start_all.bat à la racine pour lancer l'environnement complet (API + Client + Admin).

3.  **Premier Login** :
    Créez un compte via l'interface client ou utilisez les comptes de test si générés.

---

*Développé avec passion sur .NET 9.*
