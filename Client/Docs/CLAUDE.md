# Client — Hôte Blazor WebAssembly (site web)

## 1. Rôle

`Client/` est l'hôte **WebAssembly mince** du dashboard, déployé sur Azure Static Web Apps. Il ne contient aucun composant ni logique métier : tout le code Blazor (App, vues, composants, ViewModels, services, ressources) vit dans la bibliothèque Razor partagée `Client.Shared/` — voir `Client.Shared/Docs/CLAUDE.md` pour l'architecture MVVM, les règles MudBlazor/ApexCharts, les services et les tests. Le même code est réutilisé par l'application Windows (`Maui/`).

Règle : **ne rien ajouter ici qui serait utile à l'hôte MAUI** — le placer dans `Client.Shared/`. Ici, uniquement ce qui est propre au navigateur et à l'hébergement SWA.

## 2. Stack

| Élément | Choix |
|---|---|
| SDK | `Microsoft.NET.Sdk.BlazorWebAssembly`, .NET 10 |
| Paquets | `Microsoft.AspNetCore.Components.WebAssembly` 10.0.11, `…WebAssembly.DevServer` 10.0.11 (dev uniquement) |
| Référence | `Client.Shared` (qui apporte MudBlazor, Blazor-ApexCharts, `Shared`) |
| Options csproj | `OverrideHtmlAssetPlaceholders=true` (fingerprint de `blazor.webassembly.js` dans `index.html`), `BlazorWebAssemblyLoadAllGlobalizationData=true` (culture `fr-FR`) |
| Déploiement | Azure Static Web Apps (Free) + Managed Functions (`Api/`) |

## 3. Structure

```
Client/
├── InvestissementsDashboard.Client.csproj
├── Program.cs                  # point d'entrée de l'hôte (voir §4)
├── Properties/launchSettings.json
├── Docs/                       # ce dossier : CLAUDE.md, SPECS.md, charte-graphique.*, clean-code-tips.md, diagramme de classes
└── wwwroot/
    ├── index.html              # page d'hébergement : CSS MudBlazor, CSS/icône de la RCL, bundle CSS isolé, importmap, scripts
    ├── staticwebapp.config.json# navigationFallback + en-têtes de sécurité globaux
    ├── appsettings.json        # ApiBaseUrl vide → repli sur l'adresse de base du site
    ├── appsettings.Development.json  # ApiBaseUrl: http://localhost:7071/
    ├── docs/presentation.pdf   # servi tel quel ; lien « Documentation » du menu
    ├── favicon.png, icon-192.png
    └── lib/bootstrap/          # vendored, non utilisé par index.html
```

## 4. `Program.cs`

1. Crée le `WebAssemblyHostBuilder`, monte `App` (de `Client.Shared`) sur `#app` et `HeadOutlet` sur `head::after`.
2. Résout l'URL de l'Api : `Configuration["ApiBaseUrl"]` si renseignée, sinon `HostEnvironment.BaseAddress` (en production l'Api est servie par le même hôte que le site).
3. Enregistre `IKeyValueStore` → `LocalStorageKeyValueStore` (session et mode confidentialité dans le `localStorage` du navigateur).
4. Appelle `AddInvestissementsClient(apiBase)` — tout le reste de l'injection de dépendances est dans la RCL (`Client.Shared/Extensions/ServiceCollectionExtensions.cs`).

Ajouter un service au dashboard = l'enregistrer dans `AddInvestissementsClient`, pas ici (sinon l'hôte MAUI ne l'aurait pas).

## 5. `index.html` et assets

- Les assets de la RCL sont servis sous `_content/InvestissementsDashboard.Client.Shared/` : `css/app.css`, `icon.svg` (aussi utilisé comme favicon).
- Le CSS isolé (`MainLayout.razor.css`, …) arrive par `InvestissementsDashboard.Client.styles.css`, qui `@import` le bundle de la RCL.
- `<script type="importmap">` est nécessaire au fingerprinting .NET 10 ; `_content/MudBlazor/MudBlazor.min.js` est chargé en fin de `<body>`.
- Aucune police ni icône externe (pile système, icônes MudBlazor intégrées).

## 6. `staticwebapp.config.json`

- `navigationFallback` → `/index.html`, avec exclusion de `/api/*`, `/_framework/*`, `/css/*`, `/_content/*`, `/docs/*` et des extensions statiques (`ico, png, svg, webp, woff, woff2, js`).
- En-têtes globaux : `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Permissions-Policy` (géolocalisation, micro, caméra désactivés).
- Le fichier doit rester dans `Client/wwwroot` pour être copié dans le `publish`.

## 7. Build, exécution, déploiement

- Local : démarrer l'Api (`func start` dans `Api/`, port 7071) puis `dotnet run --project Client` (ports `5105` / `7124`, voir `launchSettings.json`).
- CI (`.github/workflows/azure-static-web-apps-white-cliff-055f3f803.yml`) : `dotnet restore Investissements.slnx`, `dotnet test` de `Api.Tests`, puis `dotnet publish Client/InvestissementsDashboard.Client.csproj -c Release -o Client/publish` ; le résultat (`Client/publish/wwwroot`) est déployé avec `skip_app_build: true`. Le Client est précompilé par le runner car .NET 10 n'est pas disponible dans Oryx. **Ne pas renommer ce `csproj` ni son dossier sans mettre à jour ce chemin.**
- Les tests du Client (`Client.Tests`) ne sont pas exécutés par la CI : les lancer avant toute PR touchant `Client.Shared`.

## 8. Docs voisines

- `charte-graphique.md` / `.pdf` : charte graphique (la palette est aussi reprise dans `Client.Shared/Docs/CLAUDE.md` §6).
- `clean-code-tips.md` : conseils de code propre à appliquer.
- `client-class-diagram.drawio` / `.png` : diagramme de classes.

## 9. Git — Règle absolue

**Ne jamais faire de commit, push ou créer une PR sans que l'utilisateur le demande explicitement.**

Après avoir appliqué des modifications, s'arrêter et attendre. Ne commiter que si l'utilisateur dit explicitement "commit" ou "commit et PR". Ne jamais commiter de sa propre initiative pour "sauvegarder" ou "tester le CI". Le merge des PRs est toujours de la responsabilité de l'utilisateur.
