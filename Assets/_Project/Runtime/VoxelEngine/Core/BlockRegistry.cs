using UnityEngine;
using System.Collections.Generic;

namespace Elarion.VoxelEngine
{
    /// <summary>
    /// Database centralizzato per tutti i tipi di blocchi del gioco.
    /// Gestisce proprietà, texture, comportamenti e configurazione blocchi.
    /// Singleton pattern per accesso globale.
    /// </summary>
    public class BlockRegistry : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════════════════════
        
        private static BlockRegistry instance;
        public static BlockRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<BlockRegistry>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("BlockRegistry");
                        instance = go.AddComponent<BlockRegistry>();
                    }
                }
                return instance;
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // CONFIGURAZIONE
        // ═══════════════════════════════════════════════════════════
        
        [Header("Block Definitions")]
        [SerializeField] private List<BlockDefinition> blockDefinitions = new List<BlockDefinition>();
        
        [Header("Materials")]
        [SerializeField] private Material blockMaterial;
        [SerializeField] private Texture2D blockAtlas;
        
        // ═══════════════════════════════════════════════════════════
        // DATI RUNTIME
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Dizionario per accesso rapido ai blocchi tramite ID</summary>
        private Dictionary<ushort, BlockDefinition> blockDictionary;
        
        /// <summary>Numero totale di tipi di blocchi registrati</summary>
        private int totalBlockTypes = 0;
        
        // ═══════════════════════════════════════════════════════════
        // INIZIALIZZAZIONE
        // ═══════════════════════════════════════════════════════════
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeRegistry();
        }
        
        /// <summary>Inizializza il registro blocchi</summary>
        private void InitializeRegistry()
        {
            blockDictionary = new Dictionary<ushort, BlockDefinition>();
            
            // Registra tutti i blocchi
            foreach (var blockDef in blockDefinitions)
            {
                if (blockDef != null)
                {
                    RegisterBlock(blockDef);
                }
            }
            
            Debug.Log($"BlockRegistry initialized with {totalBlockTypes} block types");
            
            // Crea blocchi di default se la lista è vuota
            if (totalBlockTypes == 0)
            {
                CreateDefaultBlocks();
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // REGISTRAZIONE BLOCCHI
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Registra un nuovo tipo di blocco</summary>
        private void RegisterBlock(BlockDefinition blockDef)
        {
            if (blockDictionary.ContainsKey(blockDef.blockID))
            {
                Debug.LogWarning($"Block ID {blockDef.blockID} already registered. Skipping.");
                return;
            }
            
            blockDictionary[blockDef.blockID] = blockDef;
            totalBlockTypes++;
        }
        
        /// <summary>Crea blocchi di default per testing</summary>
        private void CreateDefaultBlocks()
        {
            // Blocco 1: Erba
            var grass = ScriptableObject.CreateInstance<BlockDefinition>();
            grass.blockID = 1;
            grass.blockName = "Grass";
            grass.isSolid = true;
            grass.isTransparent = false;
            grass.hardness = 1.0f;
            blockDefinitions.Add(grass);
            RegisterBlock(grass);
            
            // Blocco 2: Terra
            var dirt = ScriptableObject.CreateInstance<BlockDefinition>();
            dirt.blockID = 2;
            dirt.blockName = "Dirt";
            dirt.isSolid = true;
            dirt.isTransparent = false;
            dirt.hardness = 1.0f;
            blockDefinitions.Add(dirt);
            RegisterBlock(dirt);
            
            // Blocco 3: Pietra
            var stone = ScriptableObject.CreateInstance<BlockDefinition>();
            stone.blockID = 3;
            stone.blockName = "Stone";
            stone.isSolid = true;
            stone.isTransparent = false;
            stone.hardness = 3.0f;
            blockDefinitions.Add(stone);
            RegisterBlock(stone);
            
            // Blocco 4: Foglie
            var leaves = ScriptableObject.CreateInstance<BlockDefinition>();
            leaves.blockID = 4;
            leaves.blockName = "Leaves";
            leaves.isSolid = true;
            leaves.isTransparent = true;
            leaves.hardness = 0.5f;
            blockDefinitions.Add(leaves);
            RegisterBlock(leaves);
            
            // Blocco 5: Legno
            var wood = ScriptableObject.CreateInstance<BlockDefinition>();
            wood.blockID = 5;
            wood.blockName = "Wood";
            wood.isSolid = true;
            wood.isTransparent = false;
            wood.hardness = 2.0f;
            blockDefinitions.Add(wood);
            RegisterBlock(wood);
            
            Debug.Log("Default blocks created");
        }
        
        // ═══════════════════════════════════════════════════════════
        // ACCESSO BLOCCHI
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Ottiene la definizione di un blocco tramite ID</summary>
        public BlockDefinition GetBlock(ushort blockID)
        {
            if (blockDictionary.TryGetValue(blockID, out BlockDefinition blockDef))
            {
                return blockDef;
            }
            
            return null;
        }
        
        /// <summary>Verifica se un blocco è solido</summary>
        public bool IsSolid(ushort blockID)
        {
            var block = GetBlock(blockID);
            return block != null && block.isSolid;
        }
        
        /// <summary>Verifica se un blocco è trasparente</summary>
        public bool IsTransparent(ushort blockID)
        {
            var block = GetBlock(blockID);
            return block != null && block.isTransparent;
        }
        
        /// <summary>Ottiene la durezza di un blocco</summary>
        public float GetHardness(ushort blockID)
        {
            var block = GetBlock(blockID);
            return block != null ? block.hardness : 1.0f;
        }
        
        /// <summary>Ottiene il nome di un blocco</summary>
        public string GetBlockName(ushort blockID)
        {
            var block = GetBlock(blockID);
            return block != null ? block.blockName : "Unknown";
        }
        
        // ═══════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════
        
        public Material BlockMaterial => blockMaterial;
        public Texture2D BlockAtlas => blockAtlas;
        public int TotalBlockTypes => totalBlockTypes;
        public List<BlockDefinition> AllBlocks => new List<BlockDefinition>(blockDefinitions);
    }
}
