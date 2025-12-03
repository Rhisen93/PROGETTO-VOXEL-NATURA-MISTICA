using UnityEngine;

namespace Elarion.VoxelEngine
{
    /// <summary>
    /// ScriptableObject che definisce le proprietà di un tipo di blocco.
    /// Configurabile tramite Inspector per data-driven design.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBlock", menuName = "Elarion/Voxel/Block Definition")]
    public class BlockDefinition : ScriptableObject
    {
        // ═══════════════════════════════════════════════════════════
        // IDENTIFICAZIONE
        // ═══════════════════════════════════════════════════════════
        
        [Header("Identity")]
        [Tooltip("ID univoco del blocco (0 = aria, riservato)")]
        public ushort blockID;
        
        [Tooltip("Nome visualizzato del blocco")]
        public string blockName = "New Block";
        
        [Tooltip("Descrizione del blocco")]
        [TextArea(2, 4)]
        public string description;
        
        // ═══════════════════════════════════════════════════════════
        // PROPRIETÀ FISICHE
        // ═══════════════════════════════════════════════════════════
        
        [Header("Physical Properties")]
        [Tooltip("Il blocco è solido (collisione fisica)")]
        public bool isSolid = true;
        
        [Tooltip("Il blocco è trasparente (non fa culling delle facce vicine)")]
        public bool isTransparent = false;
        
        [Tooltip("Il blocco emette luce")]
        public bool isLuminous = false;
        
        [Tooltip("Livello di luce emessa (0-15)")]
        [Range(0, 15)]
        public int lightLevel = 0;
        
        [Tooltip("Durezza del blocco (tempo per romperlo)")]
        [Range(0.1f, 100f)]
        public float hardness = 1.0f;
        
        [Tooltip("Resistenza alle esplosioni")]
        [Range(0f, 100f)]
        public float blastResistance = 1.0f;
        
        // ═══════════════════════════════════════════════════════════
        // PROPRIETÀ VISUALI
        // ═══════════════════════════════════════════════════════════
        
        [Header("Visual Properties")]
        [Tooltip("Indice texture nell'atlas per faccia TOP")]
        public int textureIndexTop = 0;
        
        [Tooltip("Indice texture nell'atlas per faccia BOTTOM")]
        public int textureIndexBottom = 0;
        
        [Tooltip("Indice texture nell'atlas per facce LATERALI")]
        public int textureIndexSides = 0;
        
        [Tooltip("Colore tint applicato al blocco")]
        public Color tintColor = Color.white;
        
        [Tooltip("Tipo di rendering speciale")]
        public RenderType renderType = RenderType.Opaque;
        
        // ═══════════════════════════════════════════════════════════
        // PROPRIETÀ GAMEPLAY
        // ═══════════════════════════════════════════════════════════
        
        [Header("Gameplay Properties")]
        [Tooltip("Il blocco può essere raccolto dal giocatore")]
        public bool isCollectable = true;
        
        [Tooltip("Tipo di strumento richiesto per raccolta efficiente")]
        public ToolType requiredTool = ToolType.None;
        
        [Tooltip("Item droppato quando il blocco viene rotto")]
        public ushort dropItemID = 0;
        
        [Tooltip("Quantità di item droppati")]
        [Range(1, 64)]
        public int dropAmount = 1;
        
        [Tooltip("Probabilità di drop (0-1)")]
        [Range(0f, 1f)]
        public float dropChance = 1.0f;
        
        // ═══════════════════════════════════════════════════════════
        // PROPRIETÀ AURA (SISTEMA MAGICO)
        // ═══════════════════════════════════════════════════════════
        
        [Header("Aura Properties")]
        [Tooltip("Il blocco influenza l'aura circostante")]
        public bool affectsAura = false;
        
        [Tooltip("Modifica purezza ambientale")]
        [Range(-1f, 1f)]
        public float purityModifier = 0f;
        
        [Tooltip("Modifica energia magica")]
        [Range(-1f, 1f)]
        public float energyModifier = 0f;
        
        [Tooltip("Raggio di influenza aura (in blocchi)")]
        [Range(0, 16)]
        public int auraRadius = 0;
        
        // ═══════════════════════════════════════════════════════════
        // PROPRIETÀ CRESCITA (PER PIANTE)
        // ═══════════════════════════════════════════════════════════
        
        [Header("Growth Properties (Plants Only)")]
        [Tooltip("Il blocco è una pianta che può crescere")]
        public bool isPlant = false;
        
        [Tooltip("Numero di stadi di crescita")]
        [Range(1, 8)]
        public int growthStages = 1;
        
        [Tooltip("Tempo medio per crescere (secondi reali)")]
        public float growthTime = 60f;
        
        [Tooltip("Richiede luce per crescere")]
        public bool requiresLight = true;
        
        [Tooltip("Livello minimo di luce richiesto")]
        [Range(0, 15)]
        public int minLightLevel = 8;
        
        [Tooltip("Richiede terreno specifico")]
        public bool requiresSoil = true;
        
        [Tooltip("IDs dei blocchi validi come terreno")]
        public ushort[] validSoilBlocks = new ushort[] { 1, 2 }; // Grass, Dirt
        
        // ═══════════════════════════════════════════════════════════
        // PROPRIETÀ AUDIO
        // ═══════════════════════════════════════════════════════════
        
        [Header("Audio Properties")]
        [Tooltip("Suono quando il blocco viene piazzato")]
        public AudioClip placeSound;
        
        [Tooltip("Suono quando il blocco viene rotto")]
        public AudioClip breakSound;
        
        [Tooltip("Suono quando si cammina sul blocco")]
        public AudioClip stepSound;
        
        [Tooltip("Volume dei suoni")]
        [Range(0f, 1f)]
        public float soundVolume = 1.0f;
        
        // ═══════════════════════════════════════════════════════════
        // ENUMS
        // ═══════════════════════════════════════════════════════════
        
        public enum RenderType
        {
            Opaque,          // Rendering normale
            Transparent,     // Rendering con alpha blending
            Cutout,          // Alpha testing (foglie)
            Emissive,        // Materiale emissivo
            Water,           // Rendering speciale acqua
            Foliage          // Rendering piante con sway
        }
        
        public enum ToolType
        {
            None,
            Pickaxe,
            Axe,
            Shovel,
            Hoe,
            Shears
        }
        
        // ═══════════════════════════════════════════════════════════
        // VALIDATION
        // ═══════════════════════════════════════════════════════════
        
        private void OnValidate()
        {
            // Assicura che blockID non sia 0 (riservato per aria)
            if (blockID == 0)
            {
                Debug.LogWarning($"Block '{blockName}': ID 0 is reserved for Air. Please use ID > 0.");
            }
            
            // Validazione coerenza luminosità
            if (isLuminous && lightLevel == 0)
            {
                lightLevel = 1;
            }
            
            if (!isLuminous)
            {
                lightLevel = 0;
            }
            
            // Validazione crescita
            if (isPlant && growthStages < 1)
            {
                growthStages = 1;
            }
            
            // Validazione drop
            if (dropItemID == 0)
            {
                dropItemID = blockID; // Di default droppa sé stesso
            }
        }
    }
}
