using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

namespace Multiverse.Presentation
{
    using Domain.Cards;

    /// <summary>Construction programmatique d'une carte conforme a la reference visuelle (image 1794315075703.jpg /
    /// 1794305834582.jpg) : composition par ELEMENTS SEPARES comme l'exige le PDF §5 —
    /// nom, medaillon de Popularite, badge de rarete, illustration dominante, pill de type,
    /// bloc de capacites, barres ATK/DEF, citation de flavor.
    ///
    /// Regle d'art engagee (docs/01 §2.5) : l'illustration n'est JAMAIS generique. En V1 sans artwork importe,
    /// un emplacement reserve explicite "ART MANQUANT" est affiche, jamais une approximation.</summary>
    public static class CardVisualBuilder
    {
        // Couleurs de reference (planche Danny Phantom / Star Butterfly UR) : barres ATK rouge, DEF bleu.
        private static readonly Color AtkRed = new Color(0.85f, 0.22f, 0.25f, 1f);
        private static readonly Color DefBlue = new Color(0.18f, 0.45f, 0.95f, 1f);
        private static readonly Color RarityGold = new Color(0.95f, 0.78f, 0.35f, 1f);
        private static readonly Color SilverGray = new Color(0.78f, 0.80f, 0.84f, 1f);
        private static readonly Color MissingArt = new Color(0.16f, 0.20f, 0.27f, 1f);

        /// <summary>Construit la hiérarchie UI de la carte dans un RectTransform parent (taille carte standard 620 x 880).</summary>
        public static GameObject Build(Transform parent, CardDefinition card)
        {
            var root = new GameObject($"CardVisual_{card.id}", typeof(RectTransform));
            var rt = root.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620f, 880f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var bg = new GameObject("Bg", typeof(Image));
            bg.transform.SetParent(rt, false);
            var bgImg = bg.GetComponent<Image>();
            bgImg.color = ColorForUniverse(card.universeId);
            Stretch(bg.GetComponent<RectTransform>());

            // --- Bandeau nom + sous-titre (reference : bandeau chrome effile) ---
            var nameGo = new GameObject("NameBand", typeof(Image));
            nameGo.transform.SetParent(rt, false);
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.85f); nameRt.anchorMax = new Vector2(1f, 0.96f);
            nameGo.GetComponent<Image>().color = new Color(0.55f, 0.60f, 0.66f, 0.35f);

            var nameText = AddText(nameRt, "Name", card.cardName, 34, FontStyle.Bold, Color.white, TextAlignmentOptions.Top);
            var subText = AddText(nameRt, "Subtitle", card.subtitle, 18, FontStyle.Italic, Color.white, TextAlignmentOptions.Top);

            // Positionnement : nom en haut, sous-titre en dessous
            var nameR = (RectTransform)nameText.transform;
            nameR.anchorMin = new Vector2(0.05f, 0.45f); nameR.anchorMax = new Vector2(0.95f, 1f);
            nameText.fontSize = 34;
            var subR = (RectTransform)subText.transform;
            subR.anchorMin = new Vector2(0.05f, 0f); subR.anchorMax = new Vector2(0.95f, 0.45f);
            subText.fontSize = 18; subText.alignment = TextAlignmentOptions.TopRight;

            // --- Medaillon Popularite (reference : cercle blanc bord chrome en haut gauche) ---
            var med = new GameObject("PopularityMedallion", typeof(Image));
            med.transform.SetParent(rt, false);
            var medRt = med.GetComponent<RectTransform>();
            medRt.anchorMin = new Vector2(0.02f, 0.86f); medRt.anchorMax = new Vector2(0.12f, 0.94f);
            med.GetComponent<Image>().color = new Color(0.98f, 0.98f, 0.98f, 1f);
            AddText(medRt, "Popularity", card.popularity.ToString(), 30, FontStyle.Bold, Color.black, TextAlignmentOptions.Center);

            // --- Badge de rarete (haut droite ; SR doré, R argenté) ---
            var badge = new GameObject("RarityBadge", typeof(Image));
            badge.transform.SetParent(rt, false);
            var badgeRt = badge.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.82f, 0.86f); badgeRt.anchorMax = new Vector2(0.98f, 0.94f);
            bool gold = card.rarity >= Rarity.SuperRare;
            badge.GetComponent<Image>().color = gold ? RarityGold : SilverGray;
            AddText(badgeRt, "Rarity", card.rarity.ToShortString(), 34, FontStyle.Bold, Color.black, TextAlignmentOptions.Center);

            // --- Illustration dominante (~55% de la hauteur, PDF §5) ---
            var art = new GameObject("Illustration", typeof(Image));
            art.transform.SetParent(rt, false);
            var artRt = art.GetComponent<RectTransform>();
            artRt.anchorMin = new Vector2(0.04f, 0.38f); artRt.anchorMax = new Vector2(0.96f, 0.84f);
            var artImg = art.GetComponent<Image>();
            var sprite = TryLoadArtwork(card.artworkRef);
            if (sprite != null)
            {
                artImg.sprite = sprite;
                artImg.preserveAspect = true;
            }
            else
            {
                // PLACEHOLDER EXPLICITE (regle anti-generique) : ce n'est PAS une illustration inventee.
                artImg.color = MissingArt;
                AddText(artRt, "MissingArt", "EMPLACEMENT ART MANQUANT\n(illustration utilisateur attendue)",
                    20, FontStyle.Italic, new Color(0.7f, 0.75f, 0.8f), TextAlignmentOptions.Center);
            }

            // --- Pill de type (reference : « Personnage · Fantome » sous l'illustration) ---
            var pill = new GameObject("TypePill", typeof(Image));
            pill.transform.SetParent(rt, false);
            var pillRt = pill.GetComponent<RectTransform>();
            pillRt.anchorMin = new Vector2(0.20f, 0.345f); pillRt.anchorMax = new Vector2(0.80f, 0.385f);
            pill.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.28f);
            AddText(pillRt, "Type", $"{TypeLabel(card.type)} · {card.subType}", 22, FontStyle.Bold, Color.white, TextAlignmentOptions.Center);

            // --- Bloc de capacites (reference : liste a icone ronde + titre italique + texte) ---
            var abil = new GameObject("Abilities", typeof(Image));
            abil.transform.SetParent(rt, false);
            var abilRt = abil.GetComponent<RectTransform>();
            abilRt.anchorMin = new Vector2(0.04f, 0.10f); abilRt.anchorMax = new Vector2(0.96f, 0.33f);
            abil.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.16f);
            var sb = new StringBuilder();
            for (int i = 0; i < card.abilities.Length; i++)
            {
                var a = card.abilities[i];
                sb.Append('•').Append(a.abilityName).Append('\n');
                sb.Append("   ").Append(Describe(a)).Append('\n');
            }
            AddText(abilRt, "AbilityList", sb.ToString(), 17, FontStyle.Normal, Color.white, TextAlignmentOptions.TopLeft);

            // --- Barres ATK / DEF (reference : ATK rouge, DEF bleu en bas) ---
            var atkBar = new GameObject("AtkBar", typeof(Image));
            atkBar.transform.SetParent(rt, false);
            var atkRt = atkBar.GetComponent<RectTransform>();
            atkRt.anchorMin = new Vector2(0.06f, 0.045f); atkRt.anchorMax = new Vector2(0.50f, 0.09f);
            atkBar.GetComponent<Image>().color = AtkRed;
            AddText(atkRt, "Atk", $"ATK {card.atk}", 26, FontStyle.Bold, Color.white, TextAlignmentOptions.Center);

            var defBar = new GameObject("DefBar", typeof(Image));
            defBar.transform.SetParent(rt, false);
            var defRt = defBar.GetComponent<RectTransform>();
            defRt.anchorMin = new Vector2(0.52f, 0.045f); defRt.anchorMax = new Vector2(0.96f, 0.09f);
            defBar.GetComponent<Image>().color = DefBlue;
            AddText(defRt, "Def", $"DEF {card.def}", 26, FontStyle.Bold, Color.white, TextAlignmentOptions.Center);

            // --- Citation de flavor (tout en bas, italique; PDF §5) ---
            var flavorRt = Create(rt, "Flavor", out var _);
            flavorRt.anchorMin = new Vector2(0.05f, 0.01f); flavorRt.anchorMax = new Vector2(0.95f, 0.04f);
            AddText(flavorRt, "FlavorText", $"« {card.flavorText} »", 16, FontStyle.Italic, Color.white, TextAlignmentOptions.Center);
            return root;
        }

        // ---------- helpers ----------
        private static RectTransform Create(Transform parent, string name, out GameObject go)
        {
            go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI AddText(RectTransform parent, string name, string content, float size,
            FontStyle style, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = size;
            tmp.fontStyle = (FontStyles)EnumExtensionsStyle(style);
            tmp.color = color;
            tmp.alignment = align;
            return tmp;
        }

        private static int EnumExtensionsStyle(FontStyle style) => (int)style;

        private static Sprite TryLoadArtwork(string artworkRef)
        {
            if (string.IsNullOrEmpty(artworkRef)) return null;
            // Convention : artworkRef = chemin sous "Assets/_Project/Data/Resources/" sans l'extension.
            // Aucun artwork n'est encore importe -> null -> placeholder explicite. (docs/01 §2.4)
            return Resources.Load<Sprite>($"Artwork/{artworkRef}");
        }

        private static string TypeLabel(CardType t) => t switch
        {
            CardType.Character => "Personnage",
            CardType.Spell => "Magie",
            CardType.Trap => "Piege",
            CardType.FieldSpell => "Magie de Terrain",
            _ => t.ToString()
        };

        private static string Describe(AbilityDefinition a)
        {
            if (a.effects == null || a.effects.Length == 0 || string.IsNullOrEmpty(a.lore?.gameplayTranslation))
                return string.IsNullOrEmpty(a.lore?.gameplayTranslation) ? a.abilityName : a.lore.gameplayTranslation;
            return a.lore.gameplayTranslation;
        }

        private static Color ColorForUniverse(string universeId) => universeId switch
        {
            "DannyPhantom" => new Color(0.10f, 0.32f, 0.34f, 0.35f),   // fond vert spectral de la planche
            _ => new Color(0.20f, 0.22f, 0.30f, 0.35f)
        };

        private static string ToShortString(this Rarity r) => r switch
        {
            Rarity.Common => "C",
            Rarity.Uncommon => "U",
            Rarity.Rare => "R",
            Rarity.SuperRare => "SR",
            Rarity.Epic => "E",
            Rarity.Legendary => "L",
            Rarity.UltraLegendary => "UR",
            _ => "C"
        };
    }
}
