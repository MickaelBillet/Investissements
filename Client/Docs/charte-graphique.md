# Charte Graphique — Application de Suivi des Investissements

---

## 1. Identité visuelle

**Nom de l'application** : Suivi des Investissements
**Style général** : Interface claire, épurée, orientée données. Inspiration Notion : fond blanc, typographie sobre, couleurs fonctionnelles utilisées avec parcimonie pour catégoriser les actifs et indiquer la performance.

---

## 2. Palette de couleurs

### Couleurs principales

| Rôle               | Hex       | RGB             | Usage                                      |
|---------------------|-----------|-----------------|---------------------------------------------|
| Fond principal      | `#FFFFFF` | 255, 255, 255   | Arrière-plan de page                        |
| Fond secondaire     | `#FAFAFA` | 250, 250, 250   | Cartes, zones de contenu, lignes alternées  |
| Texte principal     | `#37352F` | 55, 53, 47      | Titres, corps de texte                      |
| Texte secondaire    | `#787774` | 120, 119, 116   | Labels, légendes, métadonnées               |
| Bordures & séparateurs | `#E9E9E7` | 233, 233, 231 | Lignes de tableau, contours de cartes       |

### Couleurs d'accentuation par type d'actif

| Type d'actif   | Hex       | RGB             | Variante claire (tags) |
|----------------|-----------|-----------------|------------------------|
| Actions        | `#CE8BA0` | 206, 139, 160   | `#FADEC9` (pêche)      |
| Crypto         | `#9B8DD6` | 155, 141, 214   | `#E8DEEE` (lavande)    |
| ETF            | `#A0A0A0` | 160, 160, 160   | `#EEEEEE` (gris clair) |
| Immobilier     | `#4DAB9A` | 77, 171, 154    | `#DBEDDB` (vert pâle)  |
| Obligations    | `#E06D6D` | 224, 109, 109   | `#FFE2DD` (rose clair) |
| Capital-risque | `#D4A844` | 212, 168, 68    | `#FDECC8` (jaune pâle) |

### Couleurs fonctionnelles

| Rôle               | Hex       | Usage                                 |
|---------------------|-----------|---------------------------------------|
| ROI positif         | `#448361` | Texte ou barre indiquant un gain      |
| ROI négatif         | `#E06D6D` | Texte ou barre indiquant une perte    |
| Statut actif        | `#4DAB9A` | Pastille verte de statut              |
| Tag « Dividende »   | `#D9EAD3` | Badge dividende (fond vert pâle)     |
| Tag « Vendre »      | `#4DAB9A` | Badge transaction vente               |
| Tag « Acheter »     | `#CE8BA0` | Badge transaction achat               |
| Bouton principal    | `#2383E2` | Bleu Notion, CTA primaire             |

---

## 3. Typographie

| Rôle                  | Police              | Taille  | Graisse   | Couleur     |
|------------------------|---------------------|---------|-----------|-------------|
| Titre de page (H1)    | Système sans-serif  | 40 px   | Bold 700  | `#37352F`   |
| Titre de section (H2) | Système sans-serif  | 24 px   | Semibold 600 | `#37352F` |
| Titre de carte (H3)   | Système sans-serif  | 18 px   | Semibold 600 | `#37352F` |
| Corps de texte        | Système sans-serif  | 14 px   | Regular 400 | `#37352F`  |
| Texte secondaire      | Système sans-serif  | 13 px   | Regular 400 | `#787774`  |
| Données chiffrées     | Système sans-serif  | 14 px   | Medium 500  | `#37352F`  |
| Montant principal (KPI) | Système sans-serif | 36 px   | Bold 700  | `#37352F`   |
| Labels de graphique   | Système sans-serif  | 12 px   | Regular 400 | `#787774`  |

**Police système recommandée** : `-apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif` (stack Notion)

---

## 4. Composants UI

### 4.1 Cartes

- Fond : `#FFFFFF`
- Bordure : `1px solid #E9E9E7`
- Rayon de bordure : `6px`
- Padding : `16px 20px`
- Ombre : aucune (style plat)

### 4.2 Tags / Badges

- Padding : `2px 8px`
- Rayon de bordure : `4px`
- Taille du texte : `12px`
- Graisse : Medium 500
- Fond : variante claire de la couleur d'actif
- Texte : couleur d'actif pleine

### 4.3 Pastilles de statut

- Diamètre : `8px`
- Couleur « Actif » : `#4DAB9A`
- Couleur « Liste de surveillance » : `#4DAB9A` (même vert, label différent)

### 4.4 Tableau de données

- En-tête : texte `#787774`, taille `12px`, uppercase non, fond transparent
- Lignes : hauteur `44px`, séparateur bas `1px solid #E9E9E7`
- Hover : fond `#FAFAFA`
- Alignement : texte à gauche pour les noms, à droite pour les chiffres

### 4.5 Graphique donut (répartition portefeuille)

- Épaisseur de l'anneau : ~35px
- Trou central : ~60% du rayon
- Contenu central : montant total + label « Total Valeur actuelle »
- Couleurs : palette d'accentuation par type d'actif
- Légende : en dessous, horizontale, pastille carrée + label

### 4.6 Graphique barres horizontales (ROI par actif)

- Barres positives : `#E8A598` (saumon / corail clair)
- Barres négatives : `#E8A598` même couleur, direction gauche
- Fond : blanc, grille verticale gris clair `#E9E9E7`
- Labels à gauche : nom de l'actif, `13px`, `#787774`
- Valeurs : affichées en bout de barre

### 4.7 Boutons

- Primaire : fond `#2383E2`, texte blanc, rayon `4px`, padding `6px 12px`
- Secondaire (ghost) : fond transparent, bordure `1px solid #E9E9E7`, texte `#37352F`

---

## 5. Espacement & Grille

| Élément                    | Valeur   |
|-----------------------------|----------|
| Marge de page (latérale)   | `96px`   |
| Espacement entre sections  | `32px`   |
| Espacement entre cartes    | `16px`   |
| Padding interne des cartes | `16–20px`|
| Espacement lignes tableau  | `44px` (hauteur de ligne) |

**Grille** : 2 colonnes pour le dashboard (50/50), tableau pleine largeur.

---

## 6. Iconographie

- Style : outline, trait fin (1.5px)
- Taille : `16px` inline, `20px` dans les boutons
- Couleur : `#787774` (repos), `#37352F` (actif/hover)
- Source recommandée : Lucide Icons, Heroicons, ou équivalent

---

## 7. Règles d'usage

1. **Couleur = information** : chaque couleur est associée à un type d'actif. Ne pas utiliser les couleurs de manière décorative.
2. **Données au centre** : le design est au service de la lisibilité des chiffres. Privilégier l'espace blanc et l'alignement.
3. **Pas de dark mode par défaut** : l'interface est conçue en mode clair. Un dark mode pourra être ajouté ultérieurement en inversant fond/texte et ajustant les saturations.
4. **Responsive** : les deux colonnes du dashboard passent en empilement vertical sous `768px`. Les tableaux deviennent scrollables horizontalement.
5. **Montants** : format `$XX,XXX.XX` avec séparateur de milliers virgule, décimales point. Alignés à droite.
6. **Pourcentages ROI** : 2 décimales, colorés vert/rouge selon signe.
