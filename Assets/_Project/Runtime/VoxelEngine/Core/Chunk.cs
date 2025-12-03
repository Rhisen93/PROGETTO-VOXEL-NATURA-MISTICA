using UnityEngine;
using Unity.Collections;

namespace Elarion.VoxelEngine
{
    /// <summary>
    /// Rappresenta un chunk del mondo voxel (16×16×128 blocchi).
    /// Gestisce la mesh, i blocchi e lo stato del chunk.
    /// </summary>
    public class Chunk : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // COSTANTI
        // ═══════════════════════════════════════════════════════════
        
        public const int CHUNK_WIDTH = 16;
        public const int CHUNK_HEIGHT = 128;
        public const int CHUNK_DEPTH = 16;
        public const int CHUNK_SIZE = CHUNK_WIDTH * CHUNK_HEIGHT * CHUNK_DEPTH; // 32768 blocchi
        
        // ═══════════════════════════════════════════════════════════
        // DATI CHUNK
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Posizione del chunk nel mondo (coordinate chunk, non world)</summary>
        public Vector2Int chunkPosition;
        
        /// <summary>Array di blocchi (flat array per performance)</summary>
        private VoxelBlock[] blocks;
        
        /// <summary>Flag per sapere se il chunk necessita di rebuild mesh</summary>
        private bool isDirty = true;
        
        /// <summary>Flag per sapere se il chunk è stato generato</summary>
        private bool isGenerated = false;
        
        // ═══════════════════════════════════════════════════════════
        // COMPONENTI
        // ═══════════════════════════════════════════════════════════
        
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        
        // ═══════════════════════════════════════════════════════════
        // INIZIALIZZAZIONE
        // ═══════════════════════════════════════════════════════════
        
        private void Awake()
        {
            blocks = new VoxelBlock[CHUNK_SIZE];
        }
        
        /// <summary>Inizializza i componenti mesh (chiamato da WorldManager)</summary>
        public void InitializeComponents()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
        }
        
        /// <summary>Inizializza il chunk con la posizione specificata</summary>
        public void Initialize(Vector2Int position)
        {
            chunkPosition = position;
            transform.position = new Vector3(position.x * CHUNK_WIDTH, 0, position.y * CHUNK_DEPTH);
            gameObject.name = $"Chunk_{position.x}_{position.y}";
        }
        
        // ═══════════════════════════════════════════════════════════
        // ACCESSO BLOCCHI
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Ottiene un blocco alle coordinate locali del chunk</summary>
        public VoxelBlock GetBlock(int x, int y, int z)
        {
            if (!IsValidPosition(x, y, z))
                return new VoxelBlock(0); // Ritorna aria se fuori bounds
            
            int index = GetBlockIndex(x, y, z);
            return blocks[index];
        }
        
        /// <summary>Imposta un blocco alle coordinate locali del chunk</summary>
        public void SetBlock(int x, int y, int z, VoxelBlock block)
        {
            if (!IsValidPosition(x, y, z))
                return;
            
            int index = GetBlockIndex(x, y, z);
            blocks[index] = block;
            isDirty = true; // Marca il chunk per rebuild mesh
        }
        
        // ═══════════════════════════════════════════════════════════
        // UTILITY
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Converte coordinate 3D in indice flat array</summary>
        private int GetBlockIndex(int x, int y, int z)
        {
            return x + CHUNK_WIDTH * (y + CHUNK_HEIGHT * z);
        }
        
        /// <summary>Verifica se le coordinate sono valide per il chunk</summary>
        private bool IsValidPosition(int x, int y, int z)
        {
            return x >= 0 && x < CHUNK_WIDTH &&
                   y >= 0 && y < CHUNK_HEIGHT &&
                   z >= 0 && z < CHUNK_DEPTH;
        }
        
        // ═══════════════════════════════════════════════════════════
        // MESH GENERATION
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Ricostruisce la mesh del chunk</summary>
        public void RebuildMesh()
        {
            if (!isDirty || !isGenerated)
                return;
            
            // Genera mesh usando ChunkMeshBuilder
            bool success = ChunkMeshBuilder.BuildChunkMesh(
                this, 
                meshFilter, 
                meshCollider,
                BlockRegistry.Instance
            );
            
            if (success)
            {
                isDirty = false;
            }
            else
            {
                Debug.LogWarning($"Failed to rebuild mesh for chunk {chunkPosition}");
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // PROPERTIES
        // ═══════════════════════════════════════════════════════════
        
        public bool IsDirty => isDirty;
        public bool IsGenerated => isGenerated;
        public VoxelBlock[] Blocks => blocks;
        
        public void MarkGenerated() => isGenerated = true;
        public void MarkDirty() => isDirty = true;
    }
}
