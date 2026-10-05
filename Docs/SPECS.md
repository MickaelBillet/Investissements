### Spécifications fonctionnelles

#### KPIs d’en-tête

SRS_01 Le capital net engagé (NetCapital) doit être affiché en euros

SRS_12 La date de la dernière mise à jour des données doit être affichée

SRS_13 Le nombre total d’actifs en portefeuille doit être affiché

SRS_15 Le ROI du portefeuille doit être affiché :
- ROI (Capital Engagé) = TotalReturns / NetCapital × 100
où TotalReturns = plus-values réalisées et latentes depuis l'origine (les cours sont saisis à la main chaque jour)

SRS_17 Le risque moyen du portefeuille (0–4) doit être affiché, calculé comme la moyenne pondérée par la valeur actuelle des actifs

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
- Classes d’actifs(Parent) → Stocks ou Bonds → (Géographie)Zones → (Detail)Actifs de la zone
- Classes d’actifs(Parent) → Stocks ou Bonds → (Secteur)Secteur économique → (Detail)Actifs du secteur
- Types de supports(Parent) → (Detail)Supports(Parent) → (Detail)Actifs du support
- Niveaux de risque(Parent) → (Detail)Actifs du niveau

#### Détail des actifs

SRS_14 Lorsque le dernier niveau de drill-down est atteint (actifs du type, actifs du support, actifs du niveau de risque), un tableau s'affiche en dessous du graphique à secteurs présentant pour chaque actif : le nom, la valeur actuelle en euros, la plus-value latente en euros, le ROI en % et le rendement en %. Dans le drill-down par zone géographique (SRS_21), une colonne « coefficient de zone » (SRS_30) s'ajoute après la valeur actuelle

#### ETF Stocks — Groupement par thématique

SRS_18 Lorsque le drill-down Classes d'actifs atteint le type d'actif `ETF_Stocks`, un toggle "Grouper par thématique" doit permettre d'activer un niveau intermédiaire groupant les ETF_Stocks par leur champ `information` avant de descendre aux actifs individuels

#### Répartition géographique

SRS_20 Lorsque le drill-down Classes d'actifs atteint le niveau 1 (type d'actif) sur la classe `Stocks` ou `Bonds`, la colonne droite du mode Master-Detail affiche **deux graphiques à secteurs côte à côte** : la répartition géographique pondérée et la répartition par secteur économique des actifs éligibles.

SRS_21 Lorsque l'utilisateur sélectionne une zone géographique sur le graphique de répartition géographique (Stocks/Bonds niveau 1), un tableau remplace les deux graphiques et liste les actifs de la classe dont le champ `geography` contient la zone sélectionnée. Un bouton retour permet de revenir aux deux graphiques.

SRS_22 Lorsque l'utilisateur sélectionne un secteur économique sur le graphique de répartition par secteur (Stocks/Bonds niveau 1), un tableau remplace les deux graphiques et liste les actifs de la classe appartenant à ce secteur. Un bouton retour permet de revenir aux deux graphiques.

SRS_23 Les sélections zone géographique et secteur économique sont mutuellement exclusives — en sélectionner une efface l'autre.

#### Layout en mode Master-Detail

SRS_19 En mode Master-Detail (drill-down actif), le graphique à secteurs occupe la partie gauche de la vue et le tableau de données (distribution ou actifs selon le niveau) occupe la partie droite en mode côte à côte

#### Vue Suivi

SRS_11 Afficher un graphique en courbe représentant l’évolution de la performance du portefeuille dans le temps, comparée aux références LifeStrategy 40 et MSCI World. Les 3 courbes sont indexées à une base commune à la date T0 (première date disponible dans l’historique) afin de permettre une comparaison relative de la performance.

SRS_24 Afficher un graphique en barres représentant le capital obligataire à percevoir par période d’échéance (hors coupons), agrégé depuis les actifs dont le champ `information` contient une échéance au format `MM/AAAA`. La granularité est l'année par défaut ; un interrupteur permet de passer au trimestre (SRS_28).

SRS_25 La vue Suivi présente le graphique de performance (SRS_11) et l'échéancier obligataire (SRS_24) sous forme de 2 onglets, chacun occupant toute la hauteur disponible, plutôt qu'empilés verticalement avec défilement.

SRS_26 Lorsque l'utilisateur clique sur une barre (année ou trimestre selon la granularité) de l'échéancier obligataire, un tableau affiche la liste des obligations de cette année avec leur nom et leur montant, ainsi qu'une ligne de total.

SRS_27 Sur écran large, le tableau du détail par obligation (SRS_26) s'affiche à droite du graphique de l'échéancier, côte à côte, plutôt qu'empilé en dessous — sur écran étroit il reste empilé sous le graphique.

SRS_28 Un interrupteur au-dessus de l'échéancier obligataire permet de basculer l'affichage entre année et trimestre ; la bascule réinitialise la période sélectionnée et ne déclenche aucun nouvel appel réseau (les données sont agrégées au mois par l'Api puis ré-agrégées par le Client)

#### Authentification

SRS_29 Tout le dashboard est protégé par un mot de passe unique : tant qu'il n'a pas été saisi correctement, seul un écran de connexion est affiché. La session expire après 1 h sans appel réussi (expiration glissante). Le mot de passe n'est jamais embarqué dans le code ; l'Api refuse (401) toute requête sans le bon mot de passe, sauf l'endpoint MCP (protégé par sa propre clé) et la route de vérification du mot de passe

#### Coefficient géographique

SRS_30 Dans le tableau des actifs d'une zone géographique (SRS_21), la colonne « coefficient de zone » affiche pour chaque actif la part de sa valeur allouée à cette zone ; le total du tableau est pondéré par ces coefficients

#### Synchronisation manuelle

SRS_31 Un menu de la barre d'application propose « Synchroniser » : l'action relance l'ETL complet (synchro des actifs depuis le Bilan, création des actifs manquants, snapshot du jour) sans attendre le déclenchement automatique de 06h00, affiche un indicateur d'attente puis une notification de succès ou d'erreur

#### Mode confidentialité

SRS_32 Un bouton du menu de la barre d'application masque tous les montants en euros (KPI, tableaux, infobulles et axes des graphiques) pour le partage d'écran ; ce choix est mémorisé entre deux sessions. Ce n'est pas une frontière de sécurité

#### Application Windows

SRS_33 Une application Windows (MAUI) affiche le même dashboard, avec les mêmes fonctionnalités et les mêmes données que le site, sans navigateur (voir `Maui/Docs/SPECS.md`). Elle stocke le mot de passe de session chiffré par le système

#### Accès par assistant IA

SRS_34 Un endpoint MCP (JSON-RPC, protégé par clé) expose en lecture les données du portefeuille (actifs, répartitions, métriques, snapshots, géographie) à un assistant IA compatible (Claude Code, Claude Desktop, Claude Web)
