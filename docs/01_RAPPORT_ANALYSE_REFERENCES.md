# Multiverse Cartoon TCG — Rapport d'analyse des références

> Rédigé avant tout code, conformément au PDF maître (§1 : « ne commence pas par coder à l'aveugle »).
> Sources analysées : `Multiverse_Cartoon_TCG_Prompt_Maitre_OpenCode_Unity.pdf` (7 pages) et les 3 images de référence du dossier racine.

---

## 1. Synthèse du PDF maître (exigences de cadrage)

### 1.1 Mission
Créer un véritable **jeu de cartes à collectionner et de duel** inspiré des grands cartoons :
- **Unity 2D, C#** — cibles **Windows PC** et **Android**.
- Direction artistique 100 % 2D avec terrain cartoon dessiné (pas de 3D, pas de sprites qui se déplacent en permanence).
- Configuration cible modeste : i7-8565U / Intel UHD 620 / 16 Go / Windows 11.
- Multi-entrée : souris, clavier, écran tactile.
- Budget nul : outils gratuits, solutions locales, PAS d'API payante ni de cloud payant.
- Ne pas afficher de faux matchmaking / faux joueurs en ligne — tout mode non réel doit être signalé « local ».

### 1.2 Règles fondamentales (à implémenter à la lettre)
| Règle | Valeur |
|---|---|
| Points de vie initiaux | **10 000 PV** |
| Taille de deck | **exactement 50 cartes** |
| Main initiale / max | **6 cartes** (pas de dépassement) |
| Pioche | automatique en Draw Phase, règle identique pour les 2 joueurs |
| Slots Personnage | **5** |
| Slots Magie/Piège | **5**, dont **1 slot Magie de Terrain** (donc 4 ordinaires) |
| Carte Terrain | avantage les 2 joueurs, davantage le propriétaire (ex. : +3 % ATK proprio / +1,5 % adverse) — paramétrable par carte |
| Premier joueur | **shifumi virtuel** (rejoué en cas d'égalité) ; le gagnant choisit qui commence ; le choix est terminé AVANT le duel |
| Tour 1 | le 1er joueur ne peut PAS attaquer à son tout premier tour ; le 2e joueur peut attaquer dès son 1er tour |
| Phases | Draw → Standby → Main 1 → Battle → Main 2 → End |
| Invocations | 1 invoc normale/tour ; les invocations spéciales suivent les cartes (pas de limite globale arbitraire) |
| Attaques | 1/personnage/tour sauf effet ; attaque directe uniquement si le terrain adverse est vide de personnages (ou effet explicite) |
| Intégrité moteur | jamais de double application de dégâts/destruction/coûts/effets — résolution **déterministe** |

### 1.3 Combat, réactions, chronomètre
- Attaque déclarée ⇒ **fenêtre de réaction** ouverte à l'adversaire (activation légale de Magie/Piège : neutraliser, annuler, rediriger, repousser, renvoyer, retraite).
- Priorité / **chaîne d'effets** à la moderne : validations d'activation, ordre de résolution, coûts, cibles, conditions.
- Le chronomètre du joueur actif est **suspendu** pendant la fenêtre de réaction.
- Chronomètre : **230 s** au début du tour, **réinitialisation à chaque nouveau tour du joueur**, **+50 s** à chaque nouveau tour du même joueur (comportement à documenter précisément).
- Extraction des derniers PV par attaque normale ⇒ **animation finisher** (purement visuelle, 0 dégât supplémentaire).

### 1.4 Cartes, statistiques, présentation
- La **Popularité** remplace le Niveau : **1–7** = cartes normales, **8–15** = cartes spéciales.
- Types : **Personnage, Magie, Piège, Magie de Terrain** (+ extensibilité).
- Cartes **verticales illustrées** : nom en haut, médaillon de Popularité, indication de rareté, illustration dominante, capacité lisible, ATK et DEF en bas.
- **Interdits visuels** : bordures noires épaisses, cadres lourds, texte incrusté dans l'illustration.
- Les illustrations doivent représenter les **véritables personnages** des cartoons, pas des substituts génériques.
- **Composer la carte dans Unity à partir d'éléments séparés** : illustration, fond, nom, rareté, Popularité, texte de capacité, statistiques.
- Raretés : **Common, Uncommon, Rare, Super Rare, Epic, Legendary, Ultra Legendary**.
- Nombre de capacités : lisibles et limitées sur les communes ; ~**4 capacités** sur une légendaire, jusqu'à **6** sur une ultra-légendaire. Ne pas confondre quantité et puissance.

### 1.5 Capacités fidèles au lore (règle obligatoire)
- Capacités issues d'éléments **réels et reconnaissables de la série** (pouvoirs, objets, lieux, alliés, faiblesses, événements) — pas de pouvoirs au hasard.
- Fiche de référence par personnage AVANT de finaliser les 30 cartes ; incertitudes volontairement marquées.
- Chaque capacité = **traduction mécanique jouable** (attaque, défense, esquive, invocation, protection, contrôle, contre, soutien, modif de stats, effet de terrain). Un nom spectaculaire sans effet n'est pas une capacité.
- **Métadonnées internes obligatoires par capacité** :
  - `cartoonReference` (série d'origine)
  - `characterReference` (personnage)
  - `loreElement` (pouvoir/objet/lieu/relation/faiblesse/événement utilisé)
  - `loreExplanation` (lien lore ↔ capacité)
  - `gameplayTranslation` (effet concret dans les règles)
  - `canonStatus` (confirmé / adaptation cohérente / à vérifier)
- Synergies intra-univers fondées sur les relations réelles (ex. Danny + alliés de Amity Park).

### 1.6 Moteur d'effets
- Capacités **activables = manuelles** : le jeu *propose* l'activation quand les conditions sont remplies, il ne l'exécute pas automatiquement.
- Passifs et déclenchés-par-événement peuvent se résoudre automatiquement.
- Conditions : **AND, OR et groupes imbriqués**.
- Chaque effet définit : déclencheur, conditions, coût, cible, durée, portée, limite d'utilisation, restrictions, ordre de résolution, conséquences — en **données exploitables par le moteur**, pas en pur texte.
- Validations : pas d'activation illégale, pas de cible invalide, pas de **boucle infinie**, pas de **double résolution**.

### 1.7 Contenu initial, écrans, modes
- **Minimum 30 cartes** réellement conçues (pas de clones au nom/aux chiffres près).
- **5 decks préconstruits de 50 cartes**, styles distincts : offensif, défensif, contrôle, combo, polyvalent (démonstration, pas des decks officiels).
- Univers candidates : Danny Phantom, Ben 10, Bob l'éponge, Tortues Ninja, Martin Mystère, Totally Spies, Code Lyoko, Kim Possible, etc.
- Écrans : splash/intro (skippable, contrôles audio), dashboard (avatar, monnaies, missions), collection (filtres univers/rareté/popularité/type, recherche, détail des capacités), deck builder (validation stricte 50 cartes), boutique/gacha (tirages, probabilités visibles, garanties, historique, récompenses réellement ajoutées à la collection), avatars.
- V1 : duel local contre **IA** (amical) — duel classé prévu pour plus tard (serveur autoritaire : le client ne décide jamais des PV/effets/tirages).
- Terrain de duel 2D cartoon décoré ; cartes fixes ; animations uniquement sur invocation / attaque / effet / réaction / finisher (+ multi-cibles si l'effet le permet).

### 1.8 Architecture attendue & plan d'exécution
Modules (responsabilités séparées, pas de dépendance circulaire) : Core/Domain, Card Database, Effect Engine, Duel Flow, Presentation, Collection & Decks, Economy & Gacha, AI, Persistence, Tests.

Plan en 8 étapes : ①Audit → ②Conception → ③Vertical slice (duel minimal jouable) → ④Moteur d'effets → ⑤Contenu 30 cartes + 5 decks → ⑥Interface (collection, deck builder, dashboard, boutique, terrain) → ⑦Qualité → ⑧Builds Windows & Android.

Critères d'acceptation et livrables : duel complet du shifumi à la victoire sans blocage, fenêtres de réaction sans double application, conditions AND/OR testées, métadonnées de lore sur les 30 cartes, decks valides, sauvegarde/restore fiables, lisibilité et performance sur configuration cible et Android, fonctionnalités non implémentées **explicitement signalées**.

---

## 2. Analyse des images de référence

### 2.1 Image `1794305798308.jpg` — Feuille de 16 cartes Danny Phantom (planche 4×4)
Planche d'identité visuelle de la série « Danny Phantom » du projet.

**Structure commune à chaque carte** :
1. **Badge de Popularité** : médaillon circulaire blanc à bord chrome en haut à gauche avec chiffre (5–14 sur la planche) — correspond à la règle Popularité 1–15 du PDF.
2. **Badge de rareté** en haut à droite : « SR » doré ou « R » argenté.
3. **Nom** en haut centre, typographie manuscrite/script blanche à contour sombre + **sous-titre** italique descriptif (« Le Protecteur Spectral », « L'Inchridien de l'Obsession »…).
4. **Illustration dominante** ≈ 55–60 % de la hauteur, style art de la série (Danny Phantom à l'écran), effets d'énergie.
5. **Bannière de type** : « pill » translucide « Personnage · Fantôme » ou caractérase « Récit/Gadget… » située sous l'illustration.
6. **Bloc de capacités** (fond clair dégradé) : icône ronde par capacité + titre bleu foncé en italique + description courte. Icônes de coût colorées (rouge/marron/jaune/…) à gauche de certains effects = ressources d'activation.
7. **Barres de stats en bas** : **ATK rouge** et **DEF bleue** encadrées, aux couleurs de rareté. Ex. Danny : ATK 5000 / DEF 4000.
8. **Citation de flavor** italique en bas (« Je protège les deux mondes! ») + répétition du badge de rareté en bas à droite.
9. **Fond** personnalisé par personnage (vert spectral, rouge, teal, violet, orange, bleu nuit…) + **cadre orné sombre** première planche.

**16 personnages identifiés** (avec Popularité / Rareté / ATK / DEF, à migrer dans la base de données) :
Danny Fantôme 12 SR (5000/4000), Vlad Plasmius 11 SR (4800/4200), Sam Manson 7 R (2200/1500), Tucker Foley 7 R (2500/1800), Ember McLain 10 SR (4500/3000), Skulker 9 R (4200/3800), Desiree 8 R (3500/2600), The Box Ghost 5 R (1000/800), Technus 9 R (3800/3200), Penelope Spectra 10 SR (4400/3500), Johnny 13 & Kitty 8 R (3600/2800), Clockwork 13 SR (5500/5000), Pariah Dark 14 SR (6000/5500), Nocturne 11 SR (4800/4000), Undergrowth 10 R (3900/4500), + 1 carte invisible sur l'échantillon (planche 4×4, 16 slots).

**Capacités lisibles (fidèles au lore)** — exemples : Intangibilité (esquive), Rayon Fantomatique (dégâts 3500), Manipulation Politique (déplacer un perso adverse entre zones), Flamme de Feu (1000 dégâts), Coup de Poing-Plante (2000 dégâts), Amie Fidèle (bonus allié cape), Piratage de Réseau (nier capacité un tour), Souhait Exaucé (bonus ATK allié), Contrôle d'Appareil, Drainage d'Énergie, Contrôle Mental, Passeur en Duo, Cauchemar Vivant (immobilisation 3 tours), Onde de Sommeil, Enlacement de Racines (immobiliser au spawn), Croissance Rapide (soin). Les icônes d'activation ne sont pas uniformes ⇒ il existe un catalogue d'icônes de coût/pouvoir.

### 2.2 Image `1794305834582.jpg` — Focus « Star Butterfly » UR + mur de cartes
Deux niveaux de lecture :

**a) La carte au premier plan (template hiérarchique pour cartes rares)**
- Cadre clair violet/lilas avec **motifs de cœurs** et filets décoratifs fins (pas de bordure noire épaisse, conforme au PDF §5).
- **Médaillon circulaire en haut à gauche** avec un symbole de relique/toile/dimension.
- **Titre** « Star Butterfly » + sous-titre « Héritière Interdimensionnelle », centrés en haut.
- **Badge de rareté UR + doré** dans un hexagone, en haut à droite, avec sous-légende.
- Illustration dominante pleine largeur (Star Butterfly en fée, ailes arc-en-ciel, baguette magique).
- **Bloc Capacités** sur fond blanc dégradé orbité : chaque capacité = **« Capacité N — Nom (condition) »** + puce-icône colorée à droite + texte d'effet + **limite d'usage explicite** (« une fois par tour », « utilisable une fois par duel »). C'est la structure attendue côté données : `Effect { name, trigger, usage limit, target, consequence }`.
- **Barres ATK 4500 / DEF 3800** en bas dans des badges rouge/bleu à biseau doré.
- **Citation de flavor** en italique au-dessus du bord inférieur.
- Le fond n'est pas noté dans une seule zone : le texte de capacité est bien **hors illustration**.

**b) Le mur en arrière-plan (concept de collection / gacha / planche de contact)**
Univers visibles sur les cartes-fantômes : **Adventure Time, Ben 10, Steven Universe, Ranma ½, Johnny Bravo, Johnny Test, Samurai Jack, Gumball, Kim Possible, Dexter, Perry the Platypus, Chowder** — cohérent avec la liste d'univers candidates du PDF (§8) et avec la mécanique « wall of cards » des écrans Collection/Gacha (§9).
Chaque carte miniature du mur suit la même grille : nom, 2 capacités numérotées « Capacité 1 / Capacité 2 », ATK/DEF en badges colorés — la V1 prévoit des capacités lisibles et limitées pour les communes.

### 2.3 Image `1794315075703.jpg` — Carte pleine résolution « Danny Fantôme » (référence template V1)
Version haute définition, isolée, de la carte Danny — **c'est le master layout à reproduire** dans Unity :
- **En-tête chrome effilé** (nom + sous-titre) qui couvre ~15 % du haut, sans bordure noire épaisse.
- **Médaillon Popularité 12** à double bord (blanc/anthracite) + **badge SR doré brillant** en haut à droite.
- Illustration centralisée (court Danny, Concentration d'énergie verte, colonnes fantômes) avec **halo de type (vert spectral)** débordant doucement sur le cadre — ambiance sans polluer la lisibilité.
- Bannière de type « Personnage · Fantôme » (pill translucide).
- **Bloc capacités** : 2 capacités, chacune avec une icône circulaire à gauche (fantôme gris = Intangibilité ; bouton vert = Rayon Fantomatique).
- Barres ATK 5000 (rouge) / DEF 4000 (bleu) + badge SR doré répété en bas à droite + citation saisie « Je protège les deux mondes ! ».
- Aucun texte incrusté dans l'illustration.

### 2.4 Éléments à REPRODUIRE dans le système de cartes Unity (mapping)
| Élément visuel (référence) | Implémentation Unity |
|---|---|
| Médaillon de Popularité | `Image` circulaire + `TMP_Text` chiffre (donnée carte) |
| Badge rareté (R/SR/UR…) | sprite par rareté (7 raretés PDF) + sous-label éventuel |
| Nom + sous-titre | 2 `TMP_Text` sur bandeau chrome |
| Illustration dominante | type `Illustration` séparée, ~55–60 % hauteur, `` Artwork `` par personnage |
| Bannière de type | pill « Personnage · Magie · Piège · Terrain » + sous-type univers |
| Liste de capacités | composant récursif `AbilityRow` : icône + titre + texte (data-driven) |
| Icônes de coût / pouvoir | atlas d'icônes (`IconCatalog`) |
| Barres ATK / DEF | badges rouge/bleu biseauté + valeurs dynamiques |
| Citation de flavor | `TMP_Text` italique bas de carte |
| Palette par personnage | palette bleue: fond de carte dérivé du personnage/univers (thin frame à la Star Butterfly pour les rares+) |
| Mur de cartes | grille virtuelle de minis-cartes (RecyclerView-like : `UI Virtualizer`) pour Collection / Gacha / planchette |

### 2.5 Règles de non-modification des concepts visuels (engagement)
1. Les 3 images sont des **références verrouillées** : la mise en page, la typographie, le style des badges, la hiérarchie (nom → médaillon → rareté → illustration → type → capacités → ATK/DEF → flavor) sont **reproduits à l'identique** via des éléments séparés, jamais redessinés « à notre sauce ».
2. Les illustrations de la planche ne sont **jamais remplacées** par des images génériques : en V1, la carte fait référence à l'artwork source fourni par l'utilisateur ; si un artwork manque, on laisse un **emplacement réservé explicitement marqué** (placeholder documenté), sans invention.
3. Aucune capacité n'est modifiée pour « l'équilibrage » sans validation : les ajustements ne touchent en priorité que chiffres/conditions (conforme PDF §6), le lore reste intact.

---

## 3. Conclusions et impacts sur la conception

- Le template de carte résolu correspond parfaitement aux exigences §5 du PDF : **composition modulaire**, Popularité à la place du Niveau, raretés visibles, ATK/DEF en barres colorées, capacities en liste.
- Il existe **deux familles visuelles** à donnéer : (a) cadre sombre orné (planche 4×4), (b) cadre clair/doré fin (Star Butterfly UR, Danny SR hi-res) → à reproduire en **deux skins de frame** pilotés par la rareté… **Décision Management :** la référence `1794315075703.jpg` (hi-res) fait foi pour le duel ; la planche 4×4 sert de sheet de contenu ; la Star Butterfly sert de master pour les écrans Collection/Gacha et les cartes de haute rareté.
- Les icônes de capacité/coût seront gérées en **atlas avec catalogue d'ids** (data-driven).
- Le mur de cartes en arrière-plan confirme le besoin d'un **virtualiseur de liste** performant (UHD 620 + Android).
- Le style artistique des illustrations doit rester celui de la série : **pas de redraw générique** ; les artworks seront les références fournies, ou des placeholders marqués « À VÉRIFIER / Art absent ».

---

*Fin du rapport d'analyse. Prochain document : `02_ARCHITECTURE_TECHNIQUE.md`.*
