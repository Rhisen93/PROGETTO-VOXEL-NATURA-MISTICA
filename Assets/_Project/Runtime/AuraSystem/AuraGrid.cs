using UnityEngine;
using Unity.Collections;

namespace Elarion.AuraSystem
{
    /// <summary>
    /// Griglia energetica 3D parallela al mondo voxel.
    /// Gestisce i valori di purezza, energia magica e influenza ambientale.
    /// Singleton per accesso globale.
    /// </summary>
    public class AuraGrid : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════════════════════
        
        private static AuraGrid instance;
        public static AuraGrid Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<AuraGrid>();
                }
                return instance;
            }
        }
        // ═══════════════════════════════════════════════════════════
        // CONFIGURAZIONE GRIGLIA
        // ═══════════════════════════════════════════════════════════
        
        [Header("Grid Settings")]
        [SerializeField] private int gridResolution = 4; // Ogni nodo copre 4×4×4 blocchi voxel
        [SerializeField] private int gridWidth = 256;
        [SerializeField] private int gridHeight = 64;
        [SerializeField] private int gridDepth = 256;
        
        [Header("Aura Parameters")]
        [SerializeField, Range(0f, 1f)] private float defaultPurity = 0.5f;
        [SerializeField, Range(0f, 1f)] private float defaultEnergy = 0.3f;
        // diffusionRate e decayRate gestiti da AuraUpdater
        
        // ═══════════════════════════════════════════════════════════
        // DATI GRIGLIA
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Array flat di nodi aura (NativeArray per Jobs)</summary>
        private NativeArray<AuraNode> auraNodes;
        
        /// <summary>Buffer temporaneo per aggiornamenti paralleli</summary>
        private NativeArray<AuraNode> tempBuffer;
        
        /// <summary>Flag per sapere se la griglia è inizializzata</summary>
        private bool isInitialized = false;
        
        // ═══════════════════════════════════════════════════════════
        // INIZIALIZZAZIONE
        // ═══════════════════════════════════════════════════════════
        
        private void Awake()
        {
            // Singleton pattern
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeGrid();
        }
        
        /// <summary>Inizializza la griglia con valori di default</summary>
        private void InitializeGrid()
        {
            int totalNodes = gridWidth * gridHeight * gridDepth;
            
            auraNodes = new NativeArray<AuraNode>(totalNodes, Allocator.Persistent);
            tempBuffer = new NativeArray<AuraNode>(totalNodes, Allocator.Persistent);
            
            // Inizializza tutti i nodi con valori default
            for (int i = 0; i < totalNodes; i++)
            {
                auraNodes[i] = new AuraNode
                {
                    purity = defaultPurity,
                    energy = defaultEnergy,
                    magic = 0f,
                    temperature = 0.5f
                };
            }
            
            isInitialized = true;
            Debug.Log($"AuraGrid initialized: {gridWidth}×{gridHeight}×{gridDepth} nodes ({totalNodes} total)");
        }
        
        // ═══════════════════════════════════════════════════════════
        // ACCESSO NODI
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Ottiene un nodo aura alle coordinate griglia</summary>
        public AuraNode GetNode(int x, int y, int z)
        {
            if (!IsValidPosition(x, y, z))
                return default;
            
            int index = GetNodeIndex(x, y, z);
            return auraNodes[index];
        }
        
        /// <summary>Imposta un nodo aura alle coordinate griglia</summary>
        public void SetNode(int x, int y, int z, AuraNode node)
        {
            if (!IsValidPosition(x, y, z))
                return;
            
            int index = GetNodeIndex(x, y, z);
            auraNodes[index] = node;
        }
        
        /// <summary>Ottiene un nodo aura da coordinate mondiali voxel</summary>
        public AuraNode GetNodeFromWorldPosition(int worldX, int worldY, int worldZ)
        {
            int gridX = worldX / gridResolution;
            int gridY = worldY / gridResolution;
            int gridZ = worldZ / gridResolution;
            
            return GetNode(gridX, gridY, gridZ);
        }
        
        // ═══════════════════════════════════════════════════════════
        // UPDATE SYSTEM (da implementare con Jobs)
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Aggiorna la griglia con diffusione e decay</summary>
        public void UpdateGrid(float deltaTime)
        {
            if (!isInitialized)
                return;
            
            // TODO: implementare con AuraUpdateJob (Jobs System + Burst)
            // - Diffusione energia tra nodi vicini
            // - Decay naturale
            // - Influenza sorgenti/dissipatori
        }
        
        // ═══════════════════════════════════════════════════════════
        // UTILITY
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Converte coordinate 3D in indice flat array</summary>
        private int GetNodeIndex(int x, int y, int z)
        {
            return x + gridWidth * (y + gridHeight * z);
        }
        
        /// <summary>Verifica se le coordinate sono valide per la griglia</summary>
        private bool IsValidPosition(int x, int y, int z)
        {
            return x >= 0 && x < gridWidth &&
                   y >= 0 && y < gridHeight &&
                   z >= 0 && z < gridDepth;
        }
        
        // ═══════════════════════════════════════════════════════════
        // CLEANUP
        // ═══════════════════════════════════════════════════════════
        
        private void OnDestroy()
        {
            if (isInitialized)
            {
                auraNodes.Dispose();
                tempBuffer.Dispose();
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════
        
        public int GridResolution => gridResolution;
        public Vector3Int GridDimensions => new Vector3Int(gridWidth, gridHeight, gridDepth);
        public bool IsInitialized => isInitialized;
    }
    
    // ═══════════════════════════════════════════════════════════
    // STRUTTURA DATI NODO AURA
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Singolo nodo della griglia energetica.
    /// Struct per compatibilità con Jobs System.
    /// </summary>
    public struct AuraNode
    {
        /// <summary>Purezza del bioma (0 = corrotto, 1 = puro)</summary>
        public float purity;
        
        /// <summary>Energia magica accumulata</summary>
        public float energy;
        
        /// <summary>Intensità magica attiva</summary>
        public float magic;
        
        /// <summary>Temperatura ambientale (influenza crescita)</summary>
        public float temperature;
    }
}
