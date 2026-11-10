# Multiverse Cartoon TCG — Décisions & Journal de continuité

> Ce fichier documente les décisions prises de façon autonome durant le développement et assure la
> continuité du travail (protocole utilisateur : préserver contexte, tâches en cours, fichiers modifiés
> et décisions techniques en cas de changement de modèle).

## Décisions techniques prises (avec justification)

### D-01 — Version Unity
**Unity 6 LTS 6000.6.8f1** (installé sur la machine, vérifié). Le document d'architecture présumait
Unity 2022.3 LTS : mise à jour (ADR-01). Sans impact sur le code écrit (C# .NET Standard 2.1).

### D-02 — Pipeline de rendu
**Built-in Render Pipeline** (projet vide créé via `Unity -createProject`, pas de template URP).
Raisons : poids minimal sur la config cible UHD 620 + Android, aucune fonctionnalité URP 2D requise en
V1, moins de dépendances. Une migration URP 2D reste possible plus tard si post-processing souhaité.

### D-03 — Packages manifest
`com.unity.ugui` 2.0.0 (retourne TextMeshPro intégré), `com.unity.test-framework` 1.4.6,
`com.unity.2d.sprite` 1.0.0. Pas de Newtonsoft : **JsonUtility** suffit (données plates, pas de
polymorphisme) pour rester dans le budget zéro et éviter une dépendance.

### D-04 — Pioche du tout premier tour (ambiguïté du PDF résolue)
Le PDF impose **à la fois** : main de départ 6, main max 6, et pioche "au début de chaque tour".
Une pioche au tout premier tour rendrait la main à 7 > limite. **Décision** : pas de pioche au tout
premier tour du duel (convention type Yu-Gi-Oh) ; la règle reste identique pour les deux joueurs
(exigence PDF). À faire valider par l'utilisateur si souhait différent.

### D-05 — Spawn/overflow de main
Si une pioche survient avec la main pleine (ôme ≥ 6), la carte piochée part au **cimetière**
(convention : la main ne dépasse jamais 6 ; la carte n'est pas simplement détruite sans trace).
Documentée dans `DuelMachine.DrawCards`.

### D-06 — Résolution de combat (conventions retenues et documentées)
- ATK attaquant vs DEF défenseur (valeurs effectives = base + bonus de statut).
- Attaquant gagne : défenseur détruit au cimetière, **sur-dégâts infligés aux PV adverses**.
- Attaquant perd/égal : convention "recul" — la différence est infligée au PV du joueur attaquant,
  le défenseur survit, l'attaque est consommée.
  (Ces conventions imitent Yu-Gi-Oh ; elles sont isolées dans `DuelMachine.CombatResolve` et
  modifiables au mmême endroit ; à faire valider.)

### D-07 — Rareté PAS liée à la classe "spéciale"
Le PDF dit : normales = Popularité 1-7, spéciales = 8-15, sans exclure les Rares. La planche de
réference comporte des cartes **R** à Popularité 8-10 (Skulker 9, Desiree 8, Undergrowth 10) →
`DuelRules.IsPopularityInRange` accepte 1..15 pour toute rareté ; `IsSpecialCard(pop)` sert de filtre
collection/gacha. Correction d'une première interprétation trop restrictive.

### D-08 — Cartes filles du JSON avec citations de planche illisibles
Les citations de **Sam / Tucker / Box Ghost / Ember / Skulker** sont remplacées par
« A completer : citation ... (planche illisible) ». Aucune citation inventée (règle utilisateur :
ne pas remplacer/inventer sans autorisation). À reprendre avec l'utilisateur ou une image HD.

### D-09 — Artwork : placeholders explicites
`CardVisualBuilder` affiche « EMPLACEMENT ART MANQUANT (illustration utilisateur attendue) » quand
aucun artwork n'est importé. Convention `artworkRef` : chemin relatif sous `Assets/_Project/Data/Resources/Artwork/`
sans extension. Les 3 images de référence du repo **ne sont PAS importées dans Assets** tant que
l'utilisateur n'en a pas confirmé l'usage comme artworks du jeu (elles sont versionnées à la racine).

### D-10 — Tests sans scène
`Multiverse.Tests.EditMode` couvre (PDF §13) : constantes de règles, interdiction d'attaque du 1er
tour, direct attack, popularité 1..15, caps d'abilities par rareté, chronomètre 230s/+50s, chargement
et cohérence du JSON (valeurs de la planche), métadonnées de lore obligatoires, effectIds stables
(garde anti-double-résolution).

## Journal de continuité (état à chaque jalon)

### Jalon J3 — 2026-11-10 — Scaffold vertical slice écrit
- Repo connecté + docs d'analyse et d'architecture livrés (commits `a7c22ac`, `94837ba`).
- Projet Unity 6 créé at the repo root (`Assets/`, `ProjectSettings/`, `Packages/`).
- Code écrit : Domain (enums, ConditionTree, AbilityDefinition + LoreMetadata, CardDefinition,
  DuelRules, CardInstance/Status), EffectEngine (ConditionEvaluator AND/OR/NOT, EffectStack LIFO
  avec garde anti-double-résolution, EffectExecutor), DuelFlow (DuelMachine : phases, pioche,
  invocation 1/tour, combat, chrono 230s/+50s), Data (CardDatabase + cards.json 6 cartes Danny
  Phantom avec lore complet), Presentation (CardVisualBuilder), Tests (3 suites EditMode).
- En cours au moment de la rédaction : première compilation batch Unity (`compileLog.log`) pour
  vérifier exactitude C# / asmdefs / JsonUtility ; correction des erreurs éventuelles ; commit.
- Reste à faire (Étape 3) : scène de duel minimal + boutons debug, exécution des tests via
  `Unity -runTests` batch, commit/push, puis Étape 4 (réactions/chaînes complètes).

### Jalon J3.1 — 2026-11-10 — Vertical slice compilée + tests verts ✅
- Projet Unity 6 créé a la racine du repo (Assets/, ProjectSettings/, Packages/).
- Code compile PROPRE en batch mode (0 erreur CS, 0 warning bloquant) :
  Domain (enums, ConditionTree, AbilityDefinition+LoreMetadata, CardDefinition, DuelRules,
  CardInstance/Status), EffectEngine (ConditionEvaluator AND/OR/NOT, EffectStack LIFO, EffectExecutor),
  DuelFlow (DuelMachine : phases, pioche, invocation, combat, chrono), Data (CardDatabase + cards.json
  6 cartes Danny Phantom + lore complet), Presentation (CardVisualBuilder), Tests (3 suites EditMode).
- **22/22 tests EditMode PASSED** (commandes ci-dessous ; recursion : d'abord purger
  `Library/Bee` + `Library/ScriptAssemblies` si un cache Bee est stale, et tuer les VBCSCompiler).
- Pieges solutionnes : asmdef Presentation doit référencer explicitement `UnityEngine.UI` et
  `Unity.TextMeshPro` (UGUI 2.6) ; asmdef Test DOIT avoir `includePlatforms: ["Editor"]` —
  autrement Unity la classe en PLAYMODE et la découverte EditMode renvoie 0 tests
  (cause racine verifiée dans `EditorLoadedTestAssemblyProvider.cs` lignes 71-74, flag EditorOnly).
- Reste pour finir l'étape 3 (vertical slice) : scène de duel minimal + boutons debug +
  prefab de carte affiché a l'écran ; puis commit du projet Unity (fait ci-dessous).

### Commandes utiles (reprises exactes)
- Création du projet :
  `Unity.exe -createProject <chemin> -batchmode -quit -nographics -logFile log`
- Compilation/import :
  `Unity.exe -projectPath "C:\Users\junio\Downloads\Multiverse Cartoon TCG" -batchmode -quit -nographics -logFile compileLog.log`
- Tests EditMode batch (chemin ABSOLU pour le XML) :
  `Unity.exe -projectPath <chemin> -batchmode -nographics -runTests -testPlatform EditMode -testResults <absolu>\testResults.xml -logFile testLog.log`
  (+ `-quit` NE s'utilise PAS avec `-runTests` ; arrêter les process Unity : `Stop-Process -Name Unity -Force`)
- Éditeur installé : `C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe`

### Divers
- Module **Android Build Support** non installé (nécessaire pour l'étape 8) : à ajouter via
  Unity Hub → Installs → 6000.6.8f1 → Add modules. Non bloquant pour le développement PC.
- Modèles de secours : protocole utilisateur actif ; le présent fichier + `docs/02 §10` documente l'état
  des tâches pour reprendre sans recommencer à zéro. Modèle actuel : GLM-5.3-Flash.
