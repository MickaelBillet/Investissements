# Maui — Hôte application Windows (BlazorWebView)

## 1. Rôle

Application **MAUI Windows**, strictement personnelle (PC du propriétaire), qui affiche le dashboard dans un `BlazorWebView`. Elle réutilise **tout** le code Blazor de la bibliothèque `Client.Shared/` (voir `Client.Shared/Docs/CLAUDE.md`) et appelle la même Api que le site. Elle ne contient aucun composant ni logique métier : uniquement l'hôte (DI, stockage sécurisé, page d'hébergement, icône).

Elle prépare l'accueil des agents IA (CLAUDE.md racine §14) : ceux-ci s'exécuteront dans ce processus, sans la limite de 45 s du proxy SWA. Rien de cela n'est encore implémenté.

## 2. Stack

| Élément | Choix |
|---|---|
| SDK / cible | `Microsoft.NET.Sdk.Razor`, `net10.0-windows10.0.19041.0` (Windows uniquement), `UseMaui`, `SingleProject` |
| Paquets | `Microsoft.Maui.Controls`, `Microsoft.AspNetCore.Components.WebView.Maui` (versions `$(MauiVersion)`), `Microsoft.Extensions.Logging.Debug` |
| Packaging | **Non packagé** par défaut (`WindowsPackageType=None`) pour `dotnet run` et les tests ; **MSIX signé** pour l'installation sur le poste (voir §7). Le comportement d'un MSIX avec le futur flux d'identité Azure (`az login`) reste à valider |
| Référence | `Client.Shared` |
| Tests | `Maui.Tests/` (xUnit + Moq) |

## 3. Structure

```
Maui/
├── InvestissementsDashboard.Maui.csproj
├── App.xaml / App.xaml.cs      # fenêtre unique « Suivi des Investissements »
├── MainPage.xaml(.cs)          # BlazorWebView : #app → App (Client.Shared), head::after → HeadOutlet
├── MauiProgram.cs              # DI et configuration (voir §4)
├── Services/SecureStorageKeyValueStore.cs   # IKeyValueStore → ISecureStorage (chiffré par l'OS)
├── Platforms/Windows/          # App.xaml(.cs), app.manifest (boilerplate WinUI), Package.appxmanifest (identité MSIX)
├── Scripts/                    # New-DevCertificate.ps1, Publish-Msix.ps1, Install-Msix.ps1 (packaging, voir §7)
├── Resources/AppIcon/appicon.svg, Resources/Splash/splash.svg   # même visuel que le favicon du site
├── wwwroot/index.html          # page d'hébergement du BlazorWebView
└── Docs/                       # CLAUDE.md, SPECS.md

Maui.Tests/
└── SecureStorageKeyValueStoreTests.cs
```

## 4. `MauiProgram.cs`

1. `AddMauiBlazorWebView()` (+ `AddBlazorWebViewDeveloperTools()` et log debug en `DEBUG`).
2. `ISecureStorage` → `SecureStorage.Default`, `IKeyValueStore` → `SecureStorageKeyValueStore`.
3. `AddInvestissementsClient(ApiBaseUri)` — toute la DI du dashboard vient de `Client.Shared`.

**URL de l'Api** : `https://invest.zapto.fr/`, fixée dans `MauiProgram.ApiBaseUri` (l'Api n'est joignable que via le proxy SWA). Aucune surcharge par variable d'environnement : pour développer contre une Api locale, modifier la constante.

## 5. `SecureStorageKeyValueStore`

Stocke le mot de passe de session et le mode confidentialité chiffrés par l'OS (au lieu du clair du `localStorage` WebView2). `GetAsync` traite une entrée illisible (profil déplacé, clé de chiffrement perdue) comme absente : elle est journalisée (avertissement) puis supprimée, pour que l'utilisateur retombe sur l'écran de connexion au lieu d'être bloqué. `SetAsync` laisse remonter les erreurs.

## 6. `wwwroot/index.html`

Charge, dans l'ordre : `_content/MudBlazor/MudBlazor.min.css`, `_content/InvestissementsDashboard.Client.Shared/css/app.css`, `InvestissementsDashboard.Maui.styles.css` (qui importe le CSS isolé de la RCL), puis `_framework/blazor.webview.js` (`autostart="false"`) et `_content/MudBlazor/MudBlazor.min.js`. Pas d'`importmap`, pas de favicon (`data:,`).

## 7. Exécution, build et tests

- Les projets MAUI **ne figurent pas dans `Investissements.slnx`** (restauration impossible sur le runner Ubuntu de la CI). Ils sont dans **`Investissements.Maui.slnx`** (Client.Shared, Maui, Maui.Tests, Shared) — Windows uniquement, workload `maui-windows` requis.
- Build : `dotnet build Investissements.Maui.slnx`
- Lancer : `dotnet run --project Maui -f net10.0-windows10.0.19041.0` (ou exécuter `Maui/bin/Debug/net10.0-windows10.0.19041.0/win-x64/InvestissementsDashboard.Maui.exe`)
- Tests : `dotnet test "Maui.Tests/InvestissementsDashboard.Maui.Tests.csproj"` (4 tests). `Maui.Tests` cible aussi `net10.0-windows…` avec `UseMaui` (pour `ISecureStorage`) et **lie** le fichier `SecureStorageKeyValueStore.cs` au lieu de référencer le projet (une application `Exe` MAUI ne se référence pas depuis un projet de test).
- La CI ne construit ni ne teste ce projet.
- Si l'icône de l'application ne change pas après modification de `appicon.svg`, supprimer `Maui/obj` (cache de génération des icônes).

### Installer l'application (MSIX signé, auto-signé)

À faire dans l'ordre, depuis la racine du dépôt :

1. **Une seule fois** — créer le certificat de signature (PowerShell normal) : `.\Maui\Scripts\New-DevCertificate.ps1`. Le certificat vit dans le magasin utilisateur (`CN=Mickael Billet`, 3 ans) ; la signature se fait par empreinte, donc **aucun `.pfx` ni mot de passe** n'existe sur disque ou dans le dépôt. Seul le `.cer` public est exporté dans `%USERPROFILE%\.certs\`. Le `Publisher` du `Package.appxmanifest` doit rester égal au sujet du certificat.
2. **À chaque version** — construire le package : `.\Maui\Scripts\Publish-Msix.ps1` (le numéro de build `ApplicationVersion` est le nombre de commits du dépôt : Windows refuse d'installer un contenu différent sous la même version `1.0.0.N`, erreur `0x80073CFB` ; si l'on republie le même commit avec des modifications locales, désinstaller d'abord l'ancien package) → `Maui/artifacts/…/InvestissementsDashboard.Maui_<version>_x64.msix` (dossier ignoré par git).
3. **Dans un PowerShell administrateur** — `.\Maui\Scripts\Install-Msix.ps1` : fait confiance au `.cer` (`LocalMachine\TrustedPeople`, nécessite l'élévation) puis installe le `.msix` avec `Add-AppxPackage -ForceUpdateFromAnyVersion` (une mise à jour remplace la version installée en conservant ses données, à condition que le numéro de version ait augmenté).
4. L'application apparaît dans le menu Démarrer (« Suivi des Investissements ») avec l'icône du site. Désinstallation : Paramètres → Applications, ou `Get-AppxPackage fr.zapto.invest.maui | Remove-AppxPackage`.

Une application packagée est isolée : les données de `SecureStorage` et du profil WebView2 sont propres au package (une désinstallation les efface, le mot de passe est à ressaisir) et les variables d'environnement d'un terminal ne sont pas visibles.

## 8. Limites connues / à valider

- Le rendu de MudBlazor et d'ApexCharts dans le `BlazorWebView` n'est pas vérifié visuellement (l'application démarre et crée sa fenêtre).
- Le lien « Documentation » (`ClientOptions.DocumentationUri`, URL absolue du site) doit s'ouvrir dans le navigateur — non testé.
- Le certificat auto-signé expire au bout de 3 ans : en recréer un (`New-DevCertificate.ps1`), republier et réinstaller. Changer son sujet impose de changer le `Publisher` du manifeste, et donc de désinstaller l'ancien package.
- L'installation du MSIX n'est pas automatisée en CI (poste du propriétaire uniquement).
- Pas de page de paramètres : l'URL de l'Api est fixe (`MauiProgram.ApiBaseUri`).

## 9. Git — Règle absolue

**Ne jamais faire de commit, push ou créer une PR sans que l'utilisateur le demande explicitement.**

Après avoir appliqué des modifications, s'arrêter et attendre. Ne commiter que si l'utilisateur dit explicitement "commit" ou "commit et PR". Ne jamais commiter de sa propre initiative pour "sauvegarder" ou "tester le CI". Le merge des PRs est toujours de la responsabilité de l'utilisateur.
