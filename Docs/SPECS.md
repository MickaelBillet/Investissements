### Spécifications fonctionnelles

#### KPIs d’en-tête

SRS_01 La valeur totale du portefeuille doit être affichée en euros

SRS_12 La date de la dernière mise à jour des données doit être affichée

SRS_13 Le nombre total d’actifs en portefeuille doit être affiché

#### Répartition par classes d’actifs

SRS_02 Afficher dans le dashboard la répartition du portefeuille en pourcentage en fonction des classes d’actifs à la date du jour sous forme de graphique à secteurs

SRS_03 Lorsque l’utilisateur sélectionne une classe d’actifs sur le graphique à secteurs, un nouveau graphique à secteurs s’affiche présentant la répartition des types d’actifs de la classe en pourcentage

SRS_04 Lorsque l’utilisateur sélectionne un type d’actifs sur le graphique à secteurs, un nouveau graphique à secteurs s’affiche présentant la répartition des actifs du type en pourcentage

#### Répartition par types de supports

SRS_05 Afficher dans le dashboard la répartition du portefeuille en pourcentage en fonction des types de supports à la date du jour sous forme de graphique à secteurs

SRS_06 Lorsque l’utilisateur sélectionne un type de supports sur le graphique à secteurs, un nouveau graphique à secteurs s’affiche présentant la répartition des supports du type en pourcentage

SRS_07 Lorsque l’utilisateur sélectionne un support sur le graphique à secteurs, un nouveau graphique à secteurs s’affiche présentant la répartition des actifs du support en pourcentage

#### Répartition par niveau de risque

SRS_09 Afficher dans le dashboard la répartition du portefeuille en pourcentage en fonction des niveaux de risque à la date du jour sous forme de graphique à secteurs

SRS_10 Lorsque l’utilisateur sélectionne un niveau de risque sur le graphique à secteurs, un nouveau graphique à secteurs s’affiche présentant la répartition des actifs de ce niveau en pourcentage

#### Navigation

SRS_08 Un contrôle graphique doit permettre de revenir au graphique à secteurs précédent
- Classes d’actifs → Types d’actifs → Actifs du type
- Types de supports → Supports → Actifs du support
- Niveaux de risque → Actifs du niveau

#### Détail des actifs

SRS_14 Lorsque le dernier niveau de drill-down est atteint (actifs du type, actifs du support, actifs du niveau de risque), un tableau s'affiche en dessous du graphique à secteurs présentant pour chaque actif : le nom, la valeur actuelle en euros, la plus-value latente en euros, le ROI en % et le rendement en %

#### Vue historique

SRS_11 Afficher un graphique en courbe représentant l’évolution de la performance du portefeuille dans le temps, comparée aux références LifeStrategy 60 et MSCI World. Les 3 courbes sont indexées à une base commune à la date T0 (première date disponible dans l’historique) afin de permettre une comparaison relative de la performance.
