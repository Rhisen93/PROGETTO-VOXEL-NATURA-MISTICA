using UnityEngine;

namespace Elarion.Ecosystem
{
    /// <summary>
    /// Definizione di una specie vegetale.
    /// Contiene parametri di crescita, requisiti ambientali e struttura.
    /// </summary>
    [CreateAssetMenu(fileName = "New Plant", menuName = "Elarion/Ecosystem/Plant Definition")]
    public class PlantDefinition : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════
        // IDENTIFICAZIONE
        // ═══════════════════════════════════════════════════════════
        
        [Header("Identity")]
        public string plantName = "Generic Plant";
        public PlantType plantType = PlantType.Herb;
        
        // ═══════════════════════════════════════════════════════════
        // REQUISITI AMBIENTALI
        // ═══════════════════════════════════════════════════════════
        
        [Header("Environmental Requirements")]
        [Range(0f, 1f)] public float minPurity = 0.3f;
        [Range(0f, 1f)] public float minEnergy = 0.2f;
        
        [Tooltip("ID del blocco su cui può crescere (es. 2=grass)")]
        public ushort[] validSoilBlocks = new ushort[] { 2 }; // Grass default
        
        [Range(0, 255)] public int minLightLevel = 8;
        [Range(0f, 1f)] public float minWaterProximity = 0.0f;
        
        // ═══════════════════════════════════════════════════════════
        // CRESCITA
        // ═══════════════════════════════════════════════════════════
        
        [Header("Growth")]
        public int maxGrowthStages = 3;
        
        [Tooltip("Tempo in secondi per passare da uno stage all'altro")]
        public float growthTimePerStage = 60f;
        
        [Tooltip("Variazione casuale del tempo di crescita (±%)")]
        [Range(0f, 0.5f)] public float growthTimeVariation = 0.2f;
        
        // ═══════════════════════════════════════════════════════════
        // STRUTTURA VOXEL
        // ═══════════════════════════════════════════════════════════
        
        [Header("Voxel Structure")]
        [Tooltip("Struttura voxel per ogni stage di crescita")]
        public PlantStageData[] growthStages;
        
        // ═══════════════════════════════════════════════════════════
        // COMPORTAMENTO
        // ═══════════════════════════════════════════════════════════
        
        [Header("Behavior")]
        [Tooltip("Influenza sull'aura circostante quando completamente cresciuta")]
        [Range(-0.1f, 0.1f)] public float auraPurityInfluence = 0.02f;
        
        [Tooltip("Probabilità di diffusione (semi/spore)")]
        [Range(0f, 1f)] public float spreadProbability = 0.1f;
        
        [Tooltip("Raggio massimo di diffusione in blocchi")]
        [Range(1, 10)] public int spreadRadius = 3;
        
        [Tooltip("Può morire se le condizioni peggiorano")]
        public bool canWither = true;
        
        [Tooltip("Tempo senza condizioni ideali prima di morire (secondi)")]
        public float witherTime = 300f;
        
        // ═══════════════════════════════════════════════════════════
        // UTILITY
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Controlla se il blocco è un terreno valido</summary>
        public bool IsValidSoil(ushort blockID)
        {
            foreach (ushort validID in validSoilBlocks)
            {
                if (blockID == validID)
                    return true;
            }
            return false;
        }
        
        /// <summary>Ottiene i dati di uno stage specifico</summary>
        public PlantStageData GetStageData(int stage)
        {
            if (growthStages == null || growthStages.Length == 0)
                return null;
            
            stage = Mathf.Clamp(stage, 0, growthStages.Length - 1);
            return growthStages[stage];
        }
    }
    
    // ═══════════════════════════════════════════════════════════════
    // SUPPORTING CLASSES
    // ═══════════════════════════════════════════════════════════════
    
    /// <summary>Dati di un singolo stage di crescita</summary>
    [System.Serializable]
    public class PlantStageData
    {
        [Tooltip("Voxel da piazzare (relativo alla base della pianta)")]
        public VoxelOffset[] voxels;
        
        [Tooltip("Modello visivo opzionale (alternativa ai voxel)")]
        public GameObject visualModel;
    }
    
    /// <summary>Offset di un voxel relativo alla base</summary>
    [System.Serializable]
    public class VoxelOffset
    {
        public Vector3Int offset;
        public ushort blockID;
        
        public VoxelOffset(int x, int y, int z, ushort id)
        {
            offset = new Vector3Int(x, y, z);
            blockID = id;
        }
    }
    
    /// <summary>Tipi di piante</summary>
    public enum PlantType
    {
        Herb,       // Erba, fiori
        Shrub,      // Arbusti
        Tree,       // Alberi
        Vine,       // Rampicanti
        Mushroom,   // Funghi
        Mystical    // Piante magiche speciali
    }
}
