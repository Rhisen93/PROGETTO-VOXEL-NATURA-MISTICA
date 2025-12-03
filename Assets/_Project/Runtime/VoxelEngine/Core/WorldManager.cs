using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

namespace Elarion.VoxelEngine
{
    /// <summary>
    /// Manager centrale del mondo voxel.
    /// Gestisce caricamento/unload chunk, generazione mondo, chunk pool.
    /// </summary>
    public class WorldManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════════════════════
        
        private static WorldManager instance;
        public static WorldManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<WorldManager>();
                }
                return instance;
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // CONFIGURAZIONE
        // ═══════════════════════════════════════════════════════════
        
        [Header("World Settings")]
        [SerializeField] private int renderDistance = 8; // In chunk
        // [SerializeField] private int verticalRenderDistance = 4; // Non usato al momento
        [SerializeField] private bool enableDynamicLoading = true;
        
        [Header("Performance")]
        [SerializeField] private int maxChunkGenerationsPerFrame = 2;
        [SerializeField] private int maxMeshBuildsPerFrame = 4;
        
        [Header("References")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private WorldGenerator worldGenerator;
        [SerializeField] private GameObject chunkPrefab;
        
        // ═══════════════════════════════════════════════════════════
        // DATI RUNTIME
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Dizionario di tutti i chunk caricati (key = posizione chunk)</summary>
        private Dictionary<Vector2Int, Chunk> loadedChunks = new Dictionary<Vector2Int, Chunk>();
        
        /// <summary>Queue di chunk da generare</summary>
        private Queue<Vector2Int> chunksToGenerate = new Queue<Vector2Int>();
        
        /// <summary>Queue di chunk da ricostruire mesh</summary>
        private Queue<Chunk> chunksToRebuildMesh = new Queue<Chunk>();
        
        /// <summary>Pool di chunk riutilizzabili</summary>
        private Queue<Chunk> chunkPool = new Queue<Chunk>();
        
        /// <summary>Ultima posizione chunk del player</summary>
        private Vector2Int lastPlayerChunkPosition;
        
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
            
            // Trova riferimenti se non assegnati
            if (playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerTransform = player.transform;
            }
            
            if (worldGenerator == null)
            {
                worldGenerator = GetComponent<WorldGenerator>();
                if (worldGenerator == null)
                {
                    worldGenerator = gameObject.AddComponent<WorldGenerator>();
                }
            }
        }
        
        private void Start()
        {
            // Genera chunk iniziali attorno al player
            if (playerTransform != null)
            {
                lastPlayerChunkPosition = WorldToChunkPosition(playerTransform.position);
                GenerateChunksAroundPlayer();
            }
            else
            {
                Debug.LogWarning("WorldManager: Player transform not assigned!");
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // UPDATE LOOP
        // ═══════════════════════════════════════════════════════════
        
        private void Update()
        {
            if (!enableDynamicLoading || playerTransform == null)
                return;
            
            // Controlla se il player si è spostato in un nuovo chunk
            Vector2Int currentPlayerChunkPos = WorldToChunkPosition(playerTransform.position);
            
            if (!currentPlayerChunkPos.Equals(lastPlayerChunkPosition))
            {
                lastPlayerChunkPosition = currentPlayerChunkPos;
                UpdateLoadedChunks();
            }
            
            // Processa generazione chunk
            ProcessChunkGeneration();
            
            // Processa rebuild mesh
            ProcessMeshRebuilds();
        }
        
        // ═══════════════════════════════════════════════════════════
        // GENERAZIONE CHUNK
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Genera chunk iniziali attorno alla posizione del player</summary>
        private void GenerateChunksAroundPlayer()
        {
            Vector2Int playerChunkPos = WorldToChunkPosition(playerTransform.position);
            
            // Genera chunk in un raggio circolare
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    // Controllo distanza circolare
                    if (x * x + z * z > renderDistance * renderDistance)
                        continue;
                    
                    Vector2Int chunkPos = playerChunkPos + new Vector2Int(x, z);
                    
                    if (!loadedChunks.ContainsKey(chunkPos))
                    {
                        chunksToGenerate.Enqueue(chunkPos);
                    }
                }
            }
        }
        
        /// <summary>Aggiorna chunk caricati quando il player si muove</summary>
        private void UpdateLoadedChunks()
        {
            Vector2Int playerChunkPos = WorldToChunkPosition(playerTransform.position);
            
            // Unload chunk troppo lontani
            List<Vector2Int> chunksToUnload = new List<Vector2Int>();
            
            foreach (var kvp in loadedChunks)
            {
                Vector2Int chunkPos = kvp.Key;
                Vector2Int delta = chunkPos - playerChunkPos;
                
                if (Mathf.Abs(delta.x) > renderDistance || Mathf.Abs(delta.y) > renderDistance)
                {
                    chunksToUnload.Add(chunkPos);
                }
            }
            
            // Unload chunk
            foreach (var chunkPos in chunksToUnload)
            {
                UnloadChunk(chunkPos);
            }
            
            // Load nuovi chunk
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    if (x * x + z * z > renderDistance * renderDistance)
                        continue;
                    
                    Vector2Int chunkPos = playerChunkPos + new Vector2Int(x, z);
                    
                    if (!loadedChunks.ContainsKey(chunkPos) && !chunksToGenerate.Contains(chunkPos))
                    {
                        chunksToGenerate.Enqueue(chunkPos);
                    }
                }
            }
        }
        
        /// <summary>Processa generazione chunk dalla queue</summary>
        private void ProcessChunkGeneration()
        {
            int generated = 0;
            
            while (chunksToGenerate.Count > 0 && generated < maxChunkGenerationsPerFrame)
            {
                Vector2Int chunkPos = chunksToGenerate.Dequeue();
                
                if (!loadedChunks.ContainsKey(chunkPos))
                {
                    LoadChunk(chunkPos);
                    generated++;
                }
            }
        }
        
        /// <summary>Processa rebuild mesh dalla queue</summary>
        private void ProcessMeshRebuilds()
        {
            int rebuilt = 0;
            
            while (chunksToRebuildMesh.Count > 0 && rebuilt < maxMeshBuildsPerFrame)
            {
                Chunk chunk = chunksToRebuildMesh.Dequeue();
                
                if (chunk != null && chunk.IsDirty)
                {
                    chunk.RebuildMesh();
                    rebuilt++;
                }
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // GESTIONE CHUNK
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Carica e genera un chunk</summary>
        private void LoadChunk(Vector2Int chunkPos)
        {
            // Ottieni chunk dal pool o crea nuovo
            Chunk chunk = GetChunkFromPool();
            
            if (chunk == null)
            {
                // Crea nuovo chunk
                GameObject chunkObj = new GameObject();
                chunkObj.layer = LayerMask.NameToLayer("Ground"); // Layer per collisioni
                chunkObj.AddComponent<MeshFilter>();
                chunkObj.AddComponent<MeshRenderer>();
                chunkObj.AddComponent<MeshCollider>();
                chunk = chunkObj.AddComponent<Chunk>();
                
                // Inizializza componenti
                chunk.InitializeComponents();
                
                // Assegna materiale
                var renderer = chunkObj.GetComponent<MeshRenderer>();
                if (BlockRegistry.Instance != null && BlockRegistry.Instance.BlockMaterial != null)
                {
                    renderer.material = BlockRegistry.Instance.BlockMaterial;
                }
                
                chunkObj.transform.SetParent(transform);
            }
            
            // Inizializza chunk
            chunk.Initialize(chunkPos);
            chunk.gameObject.SetActive(true);
            
            // Genera terreno
            worldGenerator.GenerateChunk(chunk);
            
            // Registra chunk
            loadedChunks[chunkPos] = chunk;
            
            // Accoda per rebuild mesh
            chunksToRebuildMesh.Enqueue(chunk);
        }
        
        /// <summary>Scarica un chunk</summary>
        private void UnloadChunk(Vector2Int chunkPos)
        {
            if (loadedChunks.TryGetValue(chunkPos, out Chunk chunk))
            {
                loadedChunks.Remove(chunkPos);
                
                // Ritorna al pool
                ReturnChunkToPool(chunk);
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // CHUNK POOL
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Ottiene un chunk dal pool</summary>
        private Chunk GetChunkFromPool()
        {
            if (chunkPool.Count > 0)
            {
                return chunkPool.Dequeue();
            }
            return null;
        }
        
        /// <summary>Ritorna un chunk al pool</summary>
        private void ReturnChunkToPool(Chunk chunk)
        {
            if (chunk == null)
                return;
            
            chunk.gameObject.SetActive(false);
            chunkPool.Enqueue(chunk);
        }
        
        // ═══════════════════════════════════════════════════════════
        // ACCESSO BLOCCHI
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Ottiene un blocco da coordinate mondo</summary>
        public VoxelBlock GetBlock(int worldX, int worldY, int worldZ)
        {
            Vector2Int chunkPos = new Vector2Int(
                Mathf.FloorToInt(worldX / (float)Chunk.CHUNK_WIDTH),
                Mathf.FloorToInt(worldZ / (float)Chunk.CHUNK_DEPTH)
            );
            
            if (loadedChunks.TryGetValue(chunkPos, out Chunk chunk))
            {
                int localX = worldX - chunkPos.x * Chunk.CHUNK_WIDTH;
                int localZ = worldZ - chunkPos.y * Chunk.CHUNK_DEPTH;
                
                return chunk.GetBlock(localX, worldY, localZ);
            }
            
            return new VoxelBlock(0); // Aria se chunk non caricato
        }
        
        /// <summary>Imposta un blocco a coordinate mondo</summary>
        public void SetBlock(int worldX, int worldY, int worldZ, VoxelBlock block)
        {
            Vector2Int chunkPos = new Vector2Int(
                Mathf.FloorToInt(worldX / (float)Chunk.CHUNK_WIDTH),
                Mathf.FloorToInt(worldZ / (float)Chunk.CHUNK_DEPTH)
            );
            
            if (loadedChunks.TryGetValue(chunkPos, out Chunk chunk))
            {
                int localX = worldX - chunkPos.x * Chunk.CHUNK_WIDTH;
                int localZ = worldZ - chunkPos.y * Chunk.CHUNK_DEPTH;
                
                chunk.SetBlock(localX, worldY, localZ, block);
                
                // Accoda per rebuild mesh
                if (!chunksToRebuildMesh.Contains(chunk))
                {
                    chunksToRebuildMesh.Enqueue(chunk);
                }
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // UTILITY
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Converte posizione mondo in posizione chunk</summary>
        private Vector2Int WorldToChunkPosition(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / Chunk.CHUNK_WIDTH),
                Mathf.FloorToInt(worldPos.z / Chunk.CHUNK_DEPTH)
            );
        }
        
        /// <summary>Ottiene il chunk a una posizione specifica</summary>
        public Chunk GetChunkAtPosition(Vector2Int chunkPos)
        {
            loadedChunks.TryGetValue(chunkPos, out Chunk chunk);
            return chunk;
        }
        
        // ═══════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════
        
        public int LoadedChunkCount => loadedChunks.Count;
        public int ChunksInGenerationQueue => chunksToGenerate.Count;
        public int ChunksInMeshQueue => chunksToRebuildMesh.Count;
        public int RenderDistance => renderDistance;
        
        // ═══════════════════════════════════════════════════════════
        // DEBUG
        // ═══════════════════════════════════════════════════════════
        
        private void OnDrawGizmosSelected()
        {
            if (playerTransform == null)
                return;
            
            // Visualizza render distance
            Gizmos.color = Color.yellow;
            float radius = renderDistance * Chunk.CHUNK_WIDTH;
            
            Vector3 playerPos = playerTransform.position;
            playerPos.y = 0;
            
            // Cerchio render distance
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawWireDisc(playerPos, Vector3.up, radius);
            
            // Chunk caricati
            Gizmos.color = Color.green;
            foreach (var kvp in loadedChunks)
            {
                Vector3 chunkWorldPos = new Vector3(
                    kvp.Key.x * Chunk.CHUNK_WIDTH,
                    0,
                    kvp.Key.y * Chunk.CHUNK_DEPTH
                );
                
                Gizmos.DrawWireCube(
                    chunkWorldPos + new Vector3(Chunk.CHUNK_WIDTH / 2f, Chunk.CHUNK_HEIGHT / 2f, Chunk.CHUNK_DEPTH / 2f),
                    new Vector3(Chunk.CHUNK_WIDTH, Chunk.CHUNK_HEIGHT, Chunk.CHUNK_DEPTH)
                );
            }
        }
    }
}
