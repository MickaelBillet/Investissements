# SPECS.md — Maui (application Windows)

**Statut :** Implémenté (parité avec le site + agents IA Stock et Actualités)  
**Version :** 1.1  
**Date :** 2026-10-09

---

## 1. Objectif

Offrir sur le PC du propriétaire une application native affichant le **même dashboard que le site InvestZapto**, avec les mêmes données, sans navigateur. Les fonctionnalités (connexion, KPI, répartitions, suivi, échéancier, mode confidentialité, synchronisation) sont spécifiées dans `Client.Shared/Docs/SPECS.md` et ne sont pas redéfinies ici.

## 2. Exigences propres à l'application

| # | Exigence |
|---|---|
| 1 | Application Windows 10 (1809, build 17763) ou plus récent, installable via un package MSIX signé : entrée « Suivi des Investissements » dans le menu Démarrer, épinglable à la barre des tâches, désinstallable depuis les Paramètres Windows |
| 2 | Fenêtre unique intitulée « Suivi des Investissements », contenant le dashboard |
| 3 | Les données viennent de l'Api de production (`https://invest.zapto.fr/`)  ; l'URL est fixe (non configurable) |
| 4 | Le même mot de passe que le site est demandé à la connexion ; session à expiration glissante d'1 h |
| 5 | Le mot de passe de session et la préférence « masquer les montants » sont stockés **chiffrés** par l'OS (jamais en clair sur disque) |
| 6 | Si une entrée stockée est illisible, elle est ignorée et supprimée : l'utilisateur retombe sur l'écran de connexion |
| 7 | Icône d'application (exécutable, barre des tâches, écran de démarrage) identique au favicon du site |
| 8 | Aucune clé d'API ni secret n'est embarqué dans l'application (un binaire se décompile) |
| 9 | Le lien « Documentation » ouvre le PDF du site hors de l'application |
| 10 | Une page **Paramètres** (menu « ⋮ ») permet de saisir l'endpoint du projet Azure AI Foundry (URL https absolue) et le nom du modèle (facultatif, `gpt-5-mini` par défaut). Ces valeurs ne sont pas secrètes ; elles sont conservées entre les lancements et modifiables à tout moment |
| 11 | Sur chaque ligne d'action (`Stock`) de la liste, une icône ouvre le choix d'un agent IA : **Analyse de l'action** ou **Actualités de la société**. La réponse s'affiche dans une boîte de dialogue, avec un indicateur de progression pendant l'exécution |
| 12 | Les agents répondent en un seul tour : pas de question finale, pas de conversation. Fermer la boîte de dialogue annule l'exécution |
| 13 | Si l'endpoint Foundry n'est pas renseigné, ou si l'agent échoue, un message d'erreur est affiché dans la boîte de dialogue (l'application démarre normalement sans configuration) |
| 14 | Les agents s'exécutent dans le processus de l'application avec l'identité Azure du poste (`az login`) ; aucun secret Azure n'est embarqué. Leur usage consomme des tokens Azure AI Foundry facturés à l'usage |

## 3. Hors périmètre (évolutions prévues)

- Agents Chat, Météo et Portefeuille (déjà dans la bibliothèque `AgentAI.Core`, non exposés dans l'interface) — voir `CLAUDE.md` racine §14.
- Réglage de l'URL de l'Api et connexion Azure interactive (sans `az login`).
- Boucle conversationnelle avec les agents (questions de relance).
- Distribution à d'autres postes (certificat de confiance ou signature par une autorité).
- Exécution des tests et du build MAUI en CI.
