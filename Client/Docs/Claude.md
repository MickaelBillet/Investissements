# claude.md

## 1. Rôle
Site Web afin présenter mes investissements financiers

## 2. Stack

| Élément | Choix |
|---|---|
| Runtime | .NET 10,- Blazor WebAssembly (Wasm) |

| Tests | A compléter |
| Déploiement | Lié à Azure Static Web Apps (Managed Functions) |

## 3. Architecture Patterns
- MVVM
    - Views : composants .razor dans src/Views
    - ViewModels : src/ViewModels
    - Model : src/Models
- Injection de dépendance via le DI container Blazor

## 4. Projet structure
- src/Extensions  → Méthodes d'extension
- src/Layout      → Layouts (MainLayout, MudThemeProvider)
- src/Model       → Modèles de données
- src/ViewModels  → ViewModels (logique de présentation)
- src/Views       → Pages et vues (.razor)
- src/Properties  → Configuration app
- src/Resources   → Fichiers statiques (images, icônes)
- src/Services    → Services injectés (logique métier)
- src/Shared      → Composants partagés transverses

## 5. UI - Library
- MudBlazor (dernière version stable)
- Utiliser exclusivement les composants MudBlazor — pas de HTML natif si un composant MudBlazor existe
- Thème personnalisé défini dans src/Layout/MudThemeProvider
- Palette de couleurs : [à compléter — ex: tons dorés/bordeaux pour l'ambiance thaïlandaise]
- Toujours utiliser MudText, MudGrid, MudStack, MudPaper plutôt que div/p/span bruts
- Responsive via le système de grille MudGrid (xs, sm, md, lg, xl)
- Icônes : MudBlazor Icons (pas de FontAwesome ni autre lib externe)

## 6. Design & Charte graphique
@Docs/charte_graphique.md


## 7. Règles d'implémentation

> Prendre en compte les conseils dans le fichier `clean-code-tips.md`