### Spécifications fonctionnelles

#### KPIs d’en-tête

SRS_01 La valeur totale du portefeuille doit être affichée en euros

SRS_12 La date de la dernière mise à jour des données doit être affichée

SRS_13 Le nombre total d’actifs en portefeuille doit être affiché

SRS_15 Le ROI du portefeuille (2valeurs) en fonction de la valeur totale des achats et de la valeur actuelle du portefeuille 

#### Répartition par classes d’actifs

SRS_02 Afficher dans le dashboard la répartition du portefeuille en pourcentage en fonction des classes d’actifs à la date du jour sous forme de graphique à secteurs

SRS_03 Lorsque l’utilisateur sélectionne une classe d’actifs sur le graphique à secteurs présentant les classes d'actifs, un nouveau graphique à secteurs s’affiche présentant la répartition des types d’actifs de la classe en pourcentage, le nouveau graphique occupe toute l'espace de la vue principale. Seul ce graphique est affiché, ainsi qu'un tableau dessous listant la liste des types d'actifs. (Master-Detail)

SRS_04 Lorsque l’utilisateur sélectionne un type d’actifs sur le graphique à secteurs présentant les types d'actifs, un nouveau graphique à secteurs s’affiche présentant la répartition des actifs du type en pourcentage, le nouveau graphique occupe toute l'espace de la vue principale. Seul ce graphique est affiché, ainsi qu'un tableau dessous listant la liste des actifs du type. (Master-Detail)

#### Répartition par types de supports

SRS_05 Afficher dans le dashboard la répartition du portefeuille en pourcentage en fonction des types de supports à la date du jour sous forme de graphique à secteurs

SRS_06 Lorsque l’utilisateur sélectionne un type de supports sur le graphique à secteurs, un nouveau graphique à secteurs s’affiche présentant la répartition des supports du type en pourcentage, le nouveau graphique occupe toute l'espace de la vue principale. Seul ce graphique est affiché, ainsi qu'un tableau dessous listant la liste des supports. (Master-Detail)

SRS_07 Lorsque l’utilisateur sélectionne un support sur le graphique à secteurs, un nouveau graphique à secteurs s’affiche présentant la répartition des actifs du support en pourcentage, le nouveau graphique occupe toute l'espace de la vue principale. Seul ce graphique est affiché, ainsi qu'un tableau dessous listant la liste des actifs du support. (Master-Detail)

#### Répartition par niveau de risque

SRS_09 Afficher dans le dashboard la répartition du portefeuille en pourcentage en fonction des niveaux de risque à la date du jour sous forme de graphique à secteurs

SRS_10 Lorsque l’utilisateur sélectionne un niveau de risque sur le graphique à secteurs, un nouveau graphique à secteurs s’affiche présentant la répartition des actifs de ce niveau en pourcentage, le nouveau graphique occupe toute l'espace de la vue principale. Seul ce graphique est affiché, ainsi qu'un tableau dessous listant la liste des actifs du support. (Master-Detail)

### Vue Principale
SRS_16 Le contenu de la vue principale (du dashboard) intègre les 3 graphiques à secteurs (SRS_02, SRS_05, SRS_09)
    - Portefeuille suivant les classes d'actifs
    - Portefeuille suivant les types de support
    - Portefeuille suivant les niveaux de risque

#### Navigation

SRS_08 Un contrôle graphique doit permettre de revenir au graphique à secteurs précédent et de revenir à la vue précédente
- Classes d’actifs(Parent) → (Detail)Types d’actifs(Parent) → (Detail)Actifs du type
- Types de supports(Parent) → (Detail)Supports(Parent) → (Detail)Actifs du support
- Niveaux de risque(Parent) → (Detail)Actifs du niveau

#### Détail des actifs

SRS_14 Lorsque le dernier niveau de drill-down est atteint (actifs du type, actifs du support, actifs du niveau de risque), un tableau s'affiche en dessous du graphique à secteurs présentant pour chaque actif : le nom, la valeur actuelle en euros, la plus-value latente en euros, le ROI en % et le rendement en %

#### Vue historique

SRS_11 Afficher un graphique en courbe représentant l’évolution de la performance du portefeuille dans le temps, comparée aux références LifeStrategy 60 et MSCI World. Les 3 courbes sont indexées à une base commune à la date T0 (première date disponible dans l’historique) afin de permettre une comparaison relative de la performance.
