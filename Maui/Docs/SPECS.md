# SPECS.md — Maui (application Windows)

**Statut :** Implémenté (première version — parité avec le site)  
**Version :** 1.0  
**Date :** 2026-10-05

---

## 1. Objectif

Offrir sur le PC du propriétaire une application native affichant le **même dashboard que le site InvestZapto**, avec les mêmes données, sans navigateur. Les fonctionnalités (connexion, KPI, répartitions, suivi, échéancier, mode confidentialité, synchronisation) sont spécifiées dans `Client.Shared/Docs/SPECS.md` et ne sont pas redéfinies ici.

## 2. Exigences propres à l'application

| # | Exigence |
|---|---|
| 1 | Application Windows 10 (1809, build 17763) ou plus récent, lancée directement depuis son exécutable (non packagée) |
| 2 | Fenêtre unique intitulée « Suivi des Investissements », contenant le dashboard |
| 3 | Les données viennent de l'Api de production (`https://invest.zapto.fr/`) ; l'URL peut être remplacée par la variable d'environnement `INVEST_API_BASE_URL` |
| 4 | Le même mot de passe que le site est demandé à la connexion ; session à expiration glissante d'1 h |
| 5 | Le mot de passe de session et la préférence « masquer les montants » sont stockés **chiffrés** par l'OS (jamais en clair sur disque) |
| 6 | Si une entrée stockée est illisible, elle est ignorée et supprimée : l'utilisateur retombe sur l'écran de connexion |
| 7 | Icône d'application (exécutable, barre des tâches, écran de démarrage) identique au favicon du site |
| 8 | Aucune clé d'API ni secret n'est embarqué dans l'application (un binaire se décompile) |
| 9 | Le lien « Documentation » ouvre le PDF du site hors de l'application |

## 3. Hors périmètre (évolutions prévues)

- Agents IA (Chat, Météo, Action, Portefeuille, Actualités) exécutés dans le processus de l'application — voir `CLAUDE.md` racine §14.
- Page de paramètres (URL de l'Api, identité Azure).
- Packaging MSIX / entrée dans le menu Démarrer.
- Exécution des tests et du build MAUI en CI.
