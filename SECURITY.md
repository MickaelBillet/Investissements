---
name: dotnet-security
description: >
  Règles et bonnes pratiques de cybersécurité pour le développement d'applications ASP.NET Core (C#).
  Utiliser cette skill SYSTÉMATIQUEMENT pour toute tâche impliquant : cryptographie (IV, sel, AES,
  hachage, PBKDF2), authentification, autorisation, validation des entrées (XSS, CSRF, SQLi),
  gestion des secrets, headers HTTP, logging sécurisé, configuration ASP.NET Core, accès aux données,
  upload de fichiers, APIs et microservices. Déclencher également si l'utilisateur mentionne
  "sécurité", "vulnérabilité", "attaque", "injection", "chiffrement", "hash", "JWT", "Identity",
  "OWASP", "IV", "salt", "sel", ou demande une revue de code C# liée à la sécurité.
---

# Sécurité ASP.NET Core — Règles de développement

Source : *Advanced ASP.NET Core 8 Security* (Scott Norberg, Apress 2024)

---

## 1. CRYPTOGRAPHIE

### Concepts clés : IV et Sel

**IV (Initialization Vector)** — utilisé dans le chiffrement symétrique :
- Garantit que chiffrer la même valeur deux fois donne deux ciphertexts différents
- Protège contre l'**analyse de patterns** dans les données chiffrées
- Pas secret — stocké à côté du ciphertext
- **Un nouvel IV à chaque chiffrement — jamais réutilisé**

**Sel (Salt)** — utilisé dans le hachage :
- Rend chaque hash unique même pour des valeurs identiques
- Protège contre les **attaques par dictionnaire et rainbow tables**
- Pas secret — stocké à côté du hash
- **Sel de ligne** : unique par enregistrement (mots de passe)
- **Sel de colonne** : partagé pour toute une colonne (données searchables comme les emails) — stocké dans Key Vault

### 1.1 Chiffrement symétrique
- **Utiliser AES** — standard recommandé par le NIST
- **Ne jamais utiliser ECB** — révèle des patterns dans les données
- **Préférer CBC ou GCM** — GCM ajoute l'intégrité des données
- **L'IV DOIT être généré aléatoirement à chaque chiffrement** — jamais hard-codé
- **La clé ne doit jamais être hard-codée** — stocker dans Azure Key Vault / AWS Secrets Manager
- Préfixer le ciphertext avec la version de l'algorithme (facilite la migration)

```csharp
// ✅ BON
using var aes = Aes.Create();
aes.Key = Convert.FromBase64String(_secretStore.GetEncryptionKey());
aes.Mode = CipherMode.CBC;
aes.GenerateIV(); // ← aléatoire à chaque appel

// IV + ciphertext concaténés (IV n'est pas secret)
var result = new byte[aes.IV.Length + cipherBytes.Length];
Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
return "AES-CBC-v1:" + Convert.ToBase64String(result);

// ❌ INTERDIT
byte[] iv = Encoding.ASCII.GetBytes("8947az34awl34kjq"); // hard-codé
aes.Mode = CipherMode.ECB; // révèle les patterns
```

### 1.2 Hachage
- **Ne jamais utiliser MD5 ou SHA-1** pour la sécurité
- **Pour les mots de passe : PBKDF2, bcrypt ou scrypt** — jamais SHA direct (trop rapide)
- **Pour l'intégrité de données : SHA-256 ou SHA-512**
- **Sel de ligne (32+ octets minimum)** pour les mots de passe
- **Sel de colonne (dans Key Vault)** pour les données searchables
- **Comparaison en temps constant** : `CryptographicOperations.FixedTimeEquals()`

```csharp
// ✅ PBKDF2 avec sel de ligne — mots de passe
var salt = RandomNumberGenerator.GetBytes(32);
var hash = Rfc2898DeriveBytes.Pbkdf2(
    Encoding.UTF8.GetBytes(password), salt,
    iterations: 600_000, HashAlgorithmName.SHA512, outputLength: 64);
return "PBKDF2-SHA512-v1:" + Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);

// ✅ Données chiffrées ET searchables — deux colonnes séparées
user.EmailEncrypted = Encrypt(email);                         // IV aléatoire
user.EmailHash = HashWithColumnSalt(email, columnSalt);       // Sel de colonne
```

### 1.3 Chiffrement asymétrique & Signatures
- **RSA 4096 bits** pour les nouvelles applications (2048 minimum absolu)
- Coupler avec **SHA-512** pour les signatures
- **Ne jamais partager la clé privée**
- Prévoir une **stratégie de rotation des clés** (RSA vulnérable aux ordinateurs quantiques)
- **Ne jamais créer ses propres algorithmes ou implémentations**

> 📖 Détails d'implémentation : `references/cryptography.md`

---

## 2. AUTHENTIFICATION

- **Upgrader le hasher par défaut d'ASP.NET Identity** (PBKDF2 600k itérations SHA-512)
- **IUserStore custom → implémenter IUserLockoutStore** obligatoirement (sinon lockout silencieusement désactivé)
- **Configurer le lockout explicitement** : MaxFailedAccessAttempts=5, LockoutTimeSpan=15min
- **Messages d'erreur génériques** — ne jamais révéler si le username existe
- **Protection anti-credential-stuffing** : compteurs par IP + par username
- **Tokens** : expiration configurée, chemins par défaut changés
- **Providers externes** : valider les claims, pas de données sensibles dans les tickets JWT
- **Toujours tester activement** que chaque protection fonctionne

> 📖 Détails d'implémentation : `references/authentication.md`

---

## 3. AUTORISATION

- **Principe du moindre privilège** : droits minimaux pour chaque composant
- **Policies** plutôt que rôles seuls pour une autorisation fine
- **IAuthorizationRequirement** pour les règles dépendant de la ressource
- **Protection IDOR** : filtrer systématiquement par `userId` en base
- **NotFound() plutôt que Forbid()** — ne pas confirmer l'existence d'une ressource
- **Toujours vérifier côté serveur** — contrôles client = cosmétiques uniquement
- **FallbackPolicy** : authentification requise par défaut

```csharp
// ✅ Anti-IDOR — double condition
var invoice = await _db.Invoices
    .Where(i => i.Id == id && i.UserId == currentUserId)
    .FirstOrDefaultAsync();
if (invoice == null) return NotFound(); // NotFound, pas Forbid
```

---

## 4. VALIDATION DES ENTRÉES

### 4.1 Règle fondamentale
- **La validation JavaScript est insuffisante** — toujours valider côté serveur
- **Fail closed** : rejeter les données non conformes

### 4.2 Injection SQL
- **EF Core LINQ ou requêtes paramétrées** — jamais de concaténation
- `FromSqlInterpolated` paramètre automatiquement — `FromSqlRaw` avec `$` standard ne le fait PAS

### 4.3 XSS
- **Razor encode `@` par défaut** — éviter `@Html.Raw()` sauf contenu de confiance absolu
- **jQuery** : `.text()` encode, `.html()` exécute

### 4.4 CSRF
- **`[ValidateAntiForgeryToken]`** sur tous les POST/PUT/DELETE
- Pièges : `AutoValidateAntiforgeryToken` ignore GET/HEAD/TRACE/OPTIONS ; `AddControllers()` sans `AddControllersWithViews()` ne génère pas les tokens

### 4.5 Upload de fichiers
- Vérifier les **magic bytes** (signature réelle du fichier)
- **Jamais utiliser le nom original** — générer un UUID
- Stocker hors de la racine web

> 📖 Détails d'implémentation : `references/input-validation.md`

---

## 5. ACCÈS AUX DONNÉES (EF Core)

- **Plusieurs connexions** avec droits limités (lecture / écriture / admin)
- **Schémas SQL** pour cloisonner les données par domaine
- **Jamais stocker les secrets** dans la même base que les données
- **Chiffrement natif BDD insuffisant** seul — doubler avec chiffrement applicatif
- **ValueConverter** pour chiffrement transparent (attention : pas searchable directement)
- **DbContext séparé** pour les logs de sécurité
- **Tester les sauvegardes** — chiffrer les fichiers, sauvegarder aussi les clés

> 📖 Détails d'implémentation dans les exemples de code ci-dessous

---

## 6. LOGGING & GESTION DES ERREURS

- **`ISecurityLogger` dédié** — séparé du `ILogger` standard
- **DbContext séparé** pour les logs de sécurité
- **Logger** : IP, timestamp, résultat, user-agent — jamais passwords/tokens/données sensibles
- **Sanitiser les inputs** avant de logger (prévention log injection)
- **Détecter activement** les patterns d'attaque (credential stuffing, brute force)
- **Honeypots** pour détecter les bots
- **Messages génériques en production** + correlationId — jamais de stack trace
- **Fail closed** : en cas d'erreur de vérification sécurité → refuser l'accès

> 📖 Détails d'implémentation : `references/logging.md`

---

## 7. HEADERS HTTP & CONFIGURATION

### Headers essentiels
| Header | Valeur |
|--------|--------|
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` |
| `Content-Security-Policy` | Politique stricte + nonces pour scripts inline |
| `X-Frame-Options` | `DENY` |
| `X-Content-Type-Options` | `nosniff` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |

### Cookies
- `HttpOnly` + `Secure` + `SameSite=Strict` + préfixe `__Host-`

### CORS
- **Jamais `AllowAnyOrigin()`** avec credentials — origines explicites uniquement

### Secrets
- **Jamais dans le code ni appsettings.json versionné**
- Azure Key Vault / AWS Secrets Manager / User Secrets (dev uniquement)

### Divers
- **Swagger désactivé en production**
- **Kestrel derrière un reverse proxy** — `AddServerHeader = false`
- **Environnement de test aussi sécurisé que la production**

> 📖 Détails d'implémentation : `references/configuration.md`

---

## 8. APIs & MICROSERVICES

- **JWT** : valider issuer + audience + lifetime — expiration courte
- **Payload JWT** : encodé (pas chiffré) — jamais de données sensibles
- **Tokens de service dédiés** pour l'authentification inter-services
- **Valider les entrées** même pour les APIs internes (Zero Trust)
- **Rate limiting** sur tous les endpoints sensibles
- **Swagger désactivé** en production ou protégé par auth + URL non devinable
- **NoSQL** : API typée du driver — jamais de concaténation de filtre
- **Docker** : image Alpine, utilisateur non-root, secrets hors de l'image

---

## 9. GESTION DU CODE SOURCE & DÉPENDANCES

- **Aucun secret dans Git** — scanner avec gitleaks / trufflehog en CI/CD
- **Secret commité = compromis définitivement** — révoquer immédiatement
- **Méfiance vis-à-vis du code en ligne** (StackOverflow, CodeProject, docs officielles)
- **`dotnet list package --vulnerable`** régulièrement + Dependabot
- **Évaluer le risque réel** de chaque CVE dans le contexte de l'application
- **Pipeline CI/CD** : secrets scan + SAST (Semgrep) + SCA + DAST (OWASP ZAP)

---

## 10. PRINCIPES ARCHITECTURAUX

| Principe | Signification |
|----------|---------------|
| **Defense in Depth** | Multiplier les couches indépendantes |
| **Zero Trust** | Vérifier explicitement, même l'interne |
| **Fail Closed** | Refuser par défaut en cas de doute |
| **Secure by Default** | Configuration sécurisée sans action supplémentaire |
| **Least Privilege** | Droits minimaux pour chaque composant |
| **STRIDE** | Modéliser les menaces dès la conception |
| **Tester activement** | Ne jamais supposer qu'une protection est active |

**STRIDE** : Spoofing · Tampering · Repudiation · Information Disclosure · Denial of Service · Elevation of Privilege

---

## Références

| Domaine | Fichier |
|---------|---------|
| Cryptographie (IV, sel, AES, hachage) | `references/cryptography.md` |
| Authentification & Autorisation | `references/authentication.md` |
| Validation des entrées (XSS, CSRF, SQLi) | `references/input-validation.md` |
| Logging & erreurs | `references/logging.md` |
| Configuration & headers | `references/configuration.md` |