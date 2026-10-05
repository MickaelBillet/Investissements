# SPECS.md — Client (hôte Blazor WebAssembly)

**Statut :** Implémenté  
**Version :** 2.0  
**Date :** 2026-10-05

---

## 1. Périmètre

Le dashboard (écran de connexion, KPI, répartition, suivi, échéancier, mode confidentialité, synchronisation) est spécifié dans **`Client.Shared/Docs/SPECS.md`** — il est identique sur le site et dans l'application Windows. Ce document ne couvre que ce qui est propre au **site web** hébergé sur Azure Static Web Apps.

## 2. Exigences propres au site

| # | Exigence |
|---|---|
| 1 | Le site est servi en HTTPS sur `invest.zapto.fr` (certificat géré par Azure) |
| 2 | L'Api est appelée sur la **même origine** que le site (`/api/*`), via le proxy Static Web Apps — aucune clé n'est exposée dans le navigateur |
| 3 | En développement local, l'Api est jointe sur `http://localhost:7071/` (`appsettings.Development.json`) |
| 4 | La session (mot de passe + expiration) est conservée dans le `localStorage` du navigateur, retrouvée au rechargement tant que l'expiration glissante d'1 h n'est pas dépassée |
| 5 | Toute route inconnue est réécrite vers `index.html` (navigation côté client) ; `/api/*`, `/_framework/*`, `/_content/*`, `/css/*`, `/docs/*` et les fichiers statiques en sont exclus |
| 6 | Les réponses portent les en-têtes de sécurité `nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy` restrictive |
| 7 | Le lien « Documentation » du menu ouvre `docs/presentation.pdf` dans un nouvel onglet |
| 8 | Écran de démarrage « Chargement » affiché pendant le téléchargement du runtime WebAssembly (`index.html`) |
| 9 | Aucune ressource externe (polices, CDN) : le site fonctionne avec ses seuls fichiers |

## 3. Différences avec l'application Windows

| Sujet | Site (`Client/`) | Application (`Maui/`) |
|---|---|---|
| Stockage de session | `localStorage` (en clair) | `SecureStorage` (chiffré par l'OS) |
| URL de l'Api | Origine du site | `https://invest.zapto.fr/` par défaut, surchargeable |
| Mise à jour | Déploiement SWA à chaque push sur `main` | Recompilation locale |
