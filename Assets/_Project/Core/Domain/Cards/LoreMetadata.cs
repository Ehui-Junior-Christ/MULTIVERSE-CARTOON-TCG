namespace Multiverse.Domain.Cards
{
    /// <summary>Metadonnees de lore OBLIGATOIRES pour chaque capacite (PDF §6).
    // Le validateur de donnees refuse toute carte dont une capacite n'a pas ce bloc rempli.</summary>
    [System.Serializable]
    public sealed class LoreMetadata
    {
        public string cartoonReference;    // Serie ou univers d'origine
        public string characterReference;  // Personnage concerne
        public string loreElement;         // Pouvoir, objet, lieu, relation, faiblesse ou evennement utilise
        public string loreExplanation;     // Lien entre l'element et le personnage
        public string gameplayTranslation; // Effet concret dans les regles du TCG
        public CanonStatus canonStatus;    // Confirme / adaptation cohérente de gameplay / a verifier
    }
}
