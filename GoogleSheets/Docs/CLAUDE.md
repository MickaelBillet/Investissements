# GoogleSheets — Client de lecture de l'API Google Sheets

## 1. Rôle

Bibliothèque `net9.0` qui isole la dépendance à `Google.Apis.Sheets.v4` : l'Api (`Api/`) ne référence **que** ce projet pour lire le Google Sheet `InvestData` (`DEST_ID`) — aucune dépendance Google directe dans l'Api. Lecture seule ; l'écriture reste réservée à Google Apps Script (`Scripts/`), voir `CLAUDE.md` racine §5.2.3.

## 2. Contenu

| Fichier | Rôle |
|---|---|
| `IGoogleSheetsClient.cs` | Contrat : `Task<IReadOnlyList<IReadOnlyList<object>>> GetRangeAsync(string sheetName, CancellationToken ct = default)` |
| `GoogleSheetsClient.cs` | Implémentation (singleton, `IDisposable`) enregistrée dans `Api/Program.cs` |

`GetRangeAsync` lit `<onglet>!A:Z` avec `ValueRenderOption = UNFORMATTED_VALUE` : valeurs typées (nombres en nombres, dates en numéros de série), sans formatage de locale ni de devise — c'est ce que supposent les mappers de l'Api (`SheetMappers`, voir `Api/Docs/CLAUDE.md` §7). Une feuille vide donne une liste vide.

## 3. Configuration

Lue via `IConfiguration` (App Settings en production, `local.settings.json` en local) ; une valeur manquante lève `InvalidOperationException` à la construction :

| Clé | Contenu |
|---|---|
| `GOOGLE_SHEET_ID` | ID du Sheet `InvestData` |
| `GOOGLE_SERVICE_ACCOUNT_EMAIL` | Email du compte de service (accès **Lecteur** partagé sur le Sheet) |
| `GOOGLE_SERVICE_ACCOUNT_KEY` | Clé privée PEM sur une ligne, avec des `\n` littéraux (remplacés à la lecture) |

Le scope demandé est `SpreadsheetsReadonly` : le compte de service ne peut jamais modifier les données.

## 4. Règles

- Ne jamais logguer ni exposer ces trois valeurs.
- Pas de logique métier ici : le mapping lignes → DTO est dans `Api/Mappers/SheetMappers.cs`.
- Le projet n'appartient à aucune des deux solutions (`Investissements.slnx`, `Investissements.Maui.slnx`) : il est construit via la `ProjectReference` de l'Api. Les services de l'Api sont testés avec un `Mock<IGoogleSheetsClient>` (`Api.Tests/`).

## 5. Git — Règle absolue

**Ne jamais faire de commit, push ou créer une PR sans que l'utilisateur le demande explicitement.**

Après avoir appliqué des modifications, s'arrêter et attendre. Ne commiter que si l'utilisateur dit explicitement "commit" ou "commit et PR". Ne jamais commiter de sa propre initiative pour "sauvegarder" ou "tester le CI". Le merge des PRs est toujours de la responsabilité de l'utilisateur.
