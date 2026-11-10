# Multiverse Cartoon TCG — Architecture technique (Unity 2D, PC & Android)

> Document de cadrage technique conforme au PDF maître (§2 vision, §11 architecture attendue, §12 plan d'exécution).
> Complément direct de `01_RAPPORT_ANALYSE_REFERENCES.md`. Pas une ligne de code n'est écrite avant validation.

---

## 1. Décisions techniques majeures (ADR synthétique)

| # | Décision | Justification |
|---|---|---|
| ADR-01 | **Unity 6 LTS — 6000.6.8f1** (installé sur la machine, vérifié) | version LTS récente déjà en place ; 2D natif, support Windows Standalone ✅ (WebGL en bonus) ; ⚠️ module **Android Build Support** à ajouter via Unity Hub pour la cible mobile |
| ADR-02 | **Données de cartes = JSON**, générées depuis des ScriptObjects en éditeur | donne lecture/modifiable hors Unity ; re-validate à l'import ; les règles ne portent jamais la donnée |
| ADR-03 | **Moteur d'effets en "data-pure C#"** sans `UnityEngine.MonoBehaviour` | les règles testables sans lancer une scène (PDF §11) |
| ADR-04 | **Machine à états de duel** (State Pattern sur les Phases), chronométrage inclus | empêche les états illégaux · garantie déterministe |
| ADR-05 | **Pile de résolution (stack / chaîne)** avec validation état-agent | fenêtres de réaction, priorité, pas de double résolution (PDF §4, §7) |
| ADR-06 | **IA locale : arbre de décision légal** (furthest-forward rule engine) plutôt qu'arbre de recherche lourd | pas de dépendance coûteuse, difficulté configurable |
| ADR-07 | **Sauvegarde locale JSON versionnée** (Application.persistentDataPath) + champ `schemaVersion` | migrations douces, données validées |
| ADR-08 | **Pas de réseau en V1.** Interfaces `INetTransport` factices mais signalées "LOCAL" partout | honnêteté sur les modes (PDF §10) |
| ADR-09 | Résolution de référence UI : **1920×1080 landscape** (duel) + sécurisation **portrait Android** pour écrans collection | lecture lisibilité sur config modeste (PDF §9) |
| ADR-10 | Sprites regroupés en **SpriteAtlas ASTC** (Android) / DXT (PC) + virtualiseur de grilles | performance cible (UHD 620, mobile) |

---

## 2. Structure du projet Unity

```
Assets/
  _Project/                     <- tout le code/données du jeu, hors plugins
    Core/                       <- domaine pur (pas d'UnityEngine autre que la sérialisation)
      Domain/Multiverse.Domain  <- CardModel, PlayerModel, GameState, PhaseMachine, Effects.
      DuelFlow/                 <- orchestration des phases, tours, attaques, chronomètre
      EffectEngine/             <- conditions AND/OR, coûts, cibles, stack, résolution
      AI/                       <- décision IA locale
    Data/
      Definitions/              <- ScriptableObject:onwards (CardDefinition, RarityDef, UniverseDef)
      Json/                     <- JSON source of truth (cards.json, decks.json, economy.json)
      Visuals/                  <- palettes, frames, icônes (mapping données ↔ sprites)
    Presentation/
      CardRenderer/             <- prefab carte + ses éléments séparés (voir §4)
      DuelBoard/                <- terrain 2D, slots, animations, réactions
      Screens/                  <- Splash, Dashboard, Collection, DeckBuilder, Shop, Duel
      FX/                       <- animations DoTween/simple, finisher, audio
    Persistence/                <- SaveManager versionné, migration, validation
    Tests/                      <- EditMode (règles/effets) + PlayMode (duel bout-en-bout)
  Plugins/                      <- aucun requis en V1 (aucun asset payant)
ProjectData/                    <- hors Assets : scripts d'import/export JSON, outils
docs/                          <- ce document + rapport analyse
```

**Assembly Definitions** pour interdire les dépendances circulaires :
`Multiverse.Domain.asmdef` (référence **aucune** UI) → `DuelFlow` → `EffectEngine` ← `AI` ← `Persistence` ; `Presentation` références `Domain`+`DuelFlow` mais **jamais l'inverse**.

---

## 3. Modèle de données des cartes

### 3.1 CardDefinition (source JSON, chargée en ScriptableObject)
```csharp
public sealed class CardDefinition {
  public string id;                 // "DP-001" (univers-préfixe + numéro)
  public string cardName;
  public string subtitle;
  public CardType type;             // Character | Spell | Trap | FieldSpell  (extensible)
  public string subType;            // "Fantôme", "Tech", "Nature"…
  public string universeId;         // "DannyPhantom"…
  public Rarity rarity;             // Common..UltraLegendary (7 niveaux)
  public int popularity;            // 1..7 normal / 8..15 special
  public int atk; public int def;   // 0 pour Magie/Piège
  public string flavorText;
  public AbilityDefinition[] abilities;
  public string artworkRef;         // chemin image fournie (ne JAMAIS regénérer)
  public LoreMetadata lore;         // voir 3.2
}
```

### 3.2 AbilityDefinition & LoreMetadata (obligations PDF §6/§7)
```csharp
public sealed class AbilityDefinition {
  public string abilityName;
  public AbilityKind kind;      // Active | Passive | Triggered  (Active = manuelle)
  public TriggerDefinition trigger;   // Always | OnAttackDeclared | OnCardSummoned …
  public ConditionTree condition;     // AND/OR groupes imbriqués
  public Cost[] costs;                // icônes de coût de la référence
  public Targeting targeting;         // targetType/filters incl. popularity ≤ N
  public EffectSpec[] effects;        // lignes du moteur (Data-only)
  public UsageLimit limit;            // "OncePerTurn" | "OncePerDuel" | "Unlimited"
  public LoreMetadata lore;
}
public sealed class LoreMetadata {
  public string cartoonReference;
  public string characterReference;
  public string loreElement;
  public string loreExplanation;
  public string gameplayTranslation;
  public CanonStatus canonStatus;     // Confirmed | GameplayAdaptation | ToVerify
}
```
Chaque `EffectSpec` porte : déclencheur, conditions, coût, cible, durée, portée, limite, restrictions, ordre de résolution, conséquences — **jamais encodé dans le texte affiché**.

### 3.3 Validation de données (éditeur)
- Script de validation (import JSON → warnings/erreurs) : types cohérents, popularité dans les bornes, rareté assignable, capacités ≥ 1 pour Personnage, pas d'effet sans `gameplayTranslation`, motif préféré : champs de lore **obligatoires** (PDF §6 : métadonnées obligatoires).
- Test unitaire de cohérence deck : **exactement 50 cartes**, main ≤ 6, limites de copie éventuelles.

---

## 4. Système de rendu des cartes (fidèle aux images de référence)

Le prefab `CardVisual` est composé d'éléments **strictement séparés** (PDF §5) — layout standard vertical :

| Zone (de haut en bas) | Composant | Donnée source |
|---|---|---|
| Nom + sous-titre sur bandeau chrome | `TMP_Name`, `TMP_Subtitle` | cardName, subtitle |
| Médaillon Popularité | `Img_Medallion` + `TMP_Popularity` | popularity |
| Badge rareté | `Img_Rarity` (7 sprites) | rarity |
| Illustration dominante | `Img_Illustration` (préservation du ratio) | artworkRef (jamais regénérée) |
| Bannière type | `Img_TypePill` + `TMP_Type` | type + subType |
| Bloc capacités | `RectTransform` listant `AbilityRow[icône ronde, titre, texte, limite]` | abilities[] |
| Barres ATK/DEF | `Img_AtkBadge` (rouge) / `Img_DefBadge` (bleu) + `TMP` valeurs | atk, def |
| Badge rareté répété + flavor | `Img_RaritySmall`, `TMP_Flavor` italic | flavorText |

- **Deux skins de frame** pilotées par la rareté : `FrameStandard` (cadre clair fin type Star Butterfly/Danny hi-res pour toutes les cartes) et `FrameOrnate` (cadre sombre orné réservé aux planches/productions futures) — conformes aux interdictions visuelles du PDF (pas de bordures noires épaisses).
- **Palette par univers** : `UniverseVisualDef { paletteGradient, topColor, accentColor }` reproduit les fonds Danny vert spectral, Vlad rouge, Sam teal, etc.
- Les cartes en duel affichent les **mutations** (buff/debuff) via des badges temporaires en coin, pas en modifiant l'art.
- Le **mur de cartes** (menu Collection / Gacha / planche de contact) réutilise une version simplifiée du même prefab + **virtualisation de grille** (scissoring + pooling) pour tenir la performance (PDF §9-11).

### 4.1 Pipeline d'art (règle anti-générique)
1. Récupérer les artworks **fournis par l'utilisateur** uniquement (dossier des images de référence, à compléter par lui).
2. Cartes import/SO: on référence le fichier exact (artworkRef), on ne décale pas les frames.
3. S'il manque un artwork → **placeholder créneau documenté** listé dans `docs/03_ASSETS_MANQUANTS.md` (aucune image générique, aucun redessin automatique).

---

## 5. Moteur d'effets (conception)

- **Event bus de jeu** : `GameEventBus` publie des événements typés (AttackDeclared, CardSummoned, DamageResolved, PhaseEntered…).
- **Fenêtres de réaction** : à chaque événement déclencheur, le `ReactionWindow` collecte les activations légales (pièges/magies rapides) des 2 joueurs avec priorité conforme au joueur non-actif en premier (style moderne). Pendant la fenêtre, le **timer est suspendu** (PDF §4).
- **Stack / chaîne** : les activations s'empilent, la résolution se fait **en ordre LIFO**, chaque effet s'exécute **une seule fois** (index de résolution + guard anti-double-résolution).
- **Conditions** : arbre `ICondition` composite `And`, `Or`, `Not`, groupés imbriqués — sérialisables.
- **Effets atomiques** : Damage, Heal, StatChange (buff/debuff atk/def), Destroy, ReturnToHand, Redirect, Multiply/Divide, SummonFrom (deck/hand)， SetCard, Immobilize (tour N), TargetSwap, Avoid Attack, Direct Attack Override… Chaque effet a un `Marshal` déterministe.
- **Anti-boucle** : compteur global de résolutions par activation ; timeout de stack (niveau moteur levé en erreur, log, rollback de la chaîne).
- **Rollback intégrité ?** Non : la résolution est *pas* transactionnelle en V1, mais **déterministe et unilatérale** (jamais résolue deux fois, jamais deux applications) + tests de non-régression.

### 5.1 Capacités manuelles
- Les capacités `Active` apparaissent comme **bouton "Activer"** sur la carte en phase appropriée, **uniquement si** le prédicat `LegalActivation` renvoie vrai (PDF §7 : le jeu propose, n'exécute pas).
- Historique de coût (population fichiers de deck) : la main humaine voit un toast si résultat légal nul ; IA similaire.

---

## 6. Duel Flow (phases, tours, chronomètre)

**Machine à états** : `DuelContext { state: Phase, activePlayer, turnCount, timers… }` avec règles strictes.

1. **Shifumi** : pierre-feuille-ciseaux virtuel rejoué en égalité ; le gagnant choisit 1er/2e avant le lancement du duel (dockable dans la configuration du duel).
2. **Phase machine** :
   - `DrawPhase` (pioche auto pour les 2 joueurs identiquement, main ≤ 6 sinon débordement → carton écart/cimetière/conforme design à préciser dans les règles fines)
   - `StandbyPhase` (résolution de coût/effect Setup, terrain, fin de tours, effets Triggered "debut de tour")
   - `MainPhase1` (invocation normale 1/tour, poser magie/piège, activer capacités manuelles)
   - `BattlePhase` (déclaration d'attaque → fenêtre réaction → résolution, sortie direct uniquement si plateau adverse sans personnage, hors effet explicite)
   - `MainPhase2` (comme MainPhase1)
   - `EndPhase` (nettoyage, expiration effets/limite, réincarnation permanente, changement joueur)
3. **Chronomètre** : 230 s au début du tour, réinitialisé à chaque **nouveau** tour du joueur, **+50 s si le joueur reçoit un 2e tour consécutif** (PDF §4). Suspend pendant fenêtre de réaction. UI toujours visible. À zéro : timeout du joueur (perte du tour en cours, clic-actions bloquées, adjudication à confirmer dans les règles fines — signalé).
4. **Anti double-attaque** : flag `hasAttacked` par personnage/tour ; `hasNormalSummoned` par tour ; total de magie/piège posé libre (conforme design).
5. Attaque → **fenêtre de réaction** → résolution → dégâts → check victoire (PV ≤ 0) → finisseur visuel (0 dmg bonus) puis fin de phase.

---

## 7. IA locale (V1)

- `IAgent` implémente la même interface que le joueur humain (`IDuelAgent`), la couche réseau n'existant pas — l'UI humaine et l'IA consomment les mêmes commandes → symétrie conforme du moteur.
- Difficultés : Facile (aléatoire pondéré), Normal (heuristique : rapport dégât/risque), Difficile (heuristique + évaluation de board multi-critères + réactions sélectives).
- Aucune connaissance des paquets de l'adversaire : l'IA agit sur l'information **de jeu légale** seulement.

---

## 8. Persistance & économie

- **SaveManager** : `Collection, Decks, Wallet, Profile, Progression, Settings, HistoriqueGacha` — JSON versionné ; champ `schemaVersion` ; migration simple par étapes.
- **Wallet** : pièces & tickets virtuels (aucun paiement réel, PDF §2 : budget nul) — uniquement simulés/utilisés par le gacha local.
- **Gacha** : `probability table` dans `economy.json` avec affiliations par univers/rareté, garanti configurable (ex. SR minimum) — tirage **uniquement côté code du jeu** (dans le futur serveur autoritaire : même code réutilisable côté serveur).
- Historique gacha persisté ; récompenses réellement ajoutées à la collection (jamais farce UI).

---

## 9. Branches de tests

| Niveau | Couverture |
|---|---|
| Unit (EditMode, sans scène) | modèle carte, conditions AND/OR/Net, coûts, limites, cibles, legs anti-double-résolution, équilibrage des decks (50 cartes), validation données |
| Intégration (PlayMode, tête de duel) | duel complet shifumi → victoire, invocations, attaques, fenêtres réaction, direct attack interdit/autorisé, chronomètre (230s/+50s/suspension), finisher |
| Non-régression | fichiers de duel enregistrés action par action, replayé pour vérifier l'identité des résultats |
| Perf marée | test de mur de cartes plein (500+), carte vidéo : ≤ heurt à 60 FPS sur config cible |

---

## 10. Plan d'exécution (mappé sur le PDF §12, avec feuilles de route)

| Étape | Contenu | Statut |
|---|---|---|
| 1 Audit | repo connecté, PDF analysé, images analysées | ✅ fait |
| 2 Conception | `docs/01_RAPPORT_ANALYSE_REFERENCES.md` + `docs/02_ARCHITECTURE_TECHNIQUE.md` (ce doc) | ✅ fait |
| 3 Vertical slice | projet Unity 6 créé, code Domain/Effects/Duel/Data compilable sans scène, **22/22 tests EditMode verts** ; scène de duel + UI restants | 🔶 en cours (moteur ✅, scène ⬜) |
| 4 Moteur d'effets | réactions, chaîne, AND/OR, capacités manuelles et déclenchées | ⬜ |
| 5 Contenu | fiches de lore + 30 cartes (ex. Danny Phantom planche = 16 premières références) + 5 decks préconstruits | ⬜ |
| 6 Interface | splash, dashboard, collection (mur de cartes), deck builder, boutique/gacha, terrain cartoon | ⬜ |
| 7 Qualité | tests complets, équilibrage (stats des planches comme seed), optimisation Android, sauvegarde | ⬜ |
| 8 Builds | Windows & Android, guide de lancement, limitations connues | ⬜ |

### Prochaine action concrète (Étape 3)
Créer la structure du projet Unity (arborescence `_Project`, asmdefs, prefab carte modulaire d'après les images, scripts Domain/DuelFlow) et poser les 6 premières cartes de test issues de la planche Danny Phantom (Danny, Sam, Tucker, Box Ghost, Ember, Skulker) — **avec leur artwork référencé, jamais reconstruit**.

---

## 11. Fonctionnalités non implémentées — avouées explicitement (PDF §13)

- Duel en ligne / classé / matchmaking : **non** (prévu, architecture préparée, tout mode affiché est local).
- Sauvegarde cloud : **non** (local versionné seulement).
- Traduction multi-langue : **non** (V1 FR, données texte déjà séparées pour faciliter l'ajout).
- Animations avancées / squences splash : **placeholders** en V1 (sans art générique).

---

*Rédigé le 2026-11-10. Toute modification de règle provenant du PDF doit se répercuter ici.*
