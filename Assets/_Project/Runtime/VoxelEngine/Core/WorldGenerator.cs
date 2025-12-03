using UnityEngine;

namespace Elarion.VoxelEngine
{
    /// <summary>
    /// Genera il mondo procedurale utilizzando noise Perlin e regole di bioma.
    /// Responsabile della creazione dei chunk e del terrain.
    /// </summary>
    public class WorldGenerator : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // PARAMETRI GENERAZIONE
        // ═══════════════════════════════════════════════════════════
        
        [Header("World Settings")]
        [SerializeField] private int seed = 12345;
        [SerializeField] private float scale = 0.05f;
        [SerializeField] private int octaves = 4;
        [SerializeField] private float persistence = 0.5f;
        [SerializeField] private float lacunarity = 2.0f;
        
        [Header("Terrain Heights")]
        [SerializeField] private int baseHeight = 64;
        [SerializeField] private int maxTerrainHeight = 32;
        
        [Header("Biome Settings")]
        [SerializeField] private float biomeScale = 0.02f;
        
        // ═══════════════════════════════════════════════════════════
        // GENERAZIONE CHUNK
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Genera un chunk alle coordinate specificate</summary>
        public void GenerateChunk(Chunk chunk)
        {
            Vector2Int chunkPos = chunk.chunkPosition;
            
            for (int x = 0; x < Chunk.CHUNK_WIDTH; x++)
            {
                for (int z = 0; z < Chunk.CHUNK_DEPTH; z++)
                {
                    // Coordinate mondiali del blocco
                    int worldX = chunkPos.x * Chunk.CHUNK_WIDTH + x;
                    int worldZ = chunkPos.y * Chunk.CHUNK_DEPTH + z;
                    
                    // Genera altezza terrain
                    int terrainHeight = GetTerrainHeight(worldX, worldZ);
                    
                    // Genera colonna di blocchi
                    GenerateColumn(chunk, x, z, terrainHeight, worldX, worldZ);
                }
            }
            
            chunk.MarkGenerated();
            chunk.MarkDirty();
        }
        
        // ═══════════════════════════════════════════════════════════
        // GENERAZIONE COLONNA
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Genera una colonna verticale di blocchi</summary>
        private void GenerateColumn(Chunk chunk, int x, int z, int terrainHeight, int worldX, int worldZ)
        {
            for (int y = 0; y < Chunk.CHUNK_HEIGHT; y++)
            {
                VoxelBlock block;
                
                if (y > terrainHeight)
                {
                    // Aria
                    block = new VoxelBlock(0);
                }
                else if (y == terrainHeight)
                {
                    // Superficie (erba)
                    block = new VoxelBlock(1); // ID 1 = grass
                }
                else if (y > terrainHeight - 3)
                {
                    // Strato terra
                    block = new VoxelBlock(2); // ID 2 = dirt
                }
                else
                {
                    // Pietra
                    block = new VoxelBlock(3); // ID 3 = stone
                }
                
                chunk.SetBlock(x, y, z, block);
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // NOISE GENERATION
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Calcola l'altezza del terreno usando Perlin noise</summary>
        private int GetTerrainHeight(int worldX, int worldZ)
        {
            float noiseValue = GetPerlinNoise(worldX, worldZ);
            
            // Normalizza tra 0 e 1
            noiseValue = (noiseValue + 1f) / 2f;
            
            // Calcola altezza finale
            int height = baseHeight + Mathf.FloorToInt(noiseValue * maxTerrainHeight);
            
            return Mathf.Clamp(height, 0, Chunk.CHUNK_HEIGHT - 1);
        }
        
        /// <summary>Genera Perlin noise con octaves</summary>
        private float GetPerlinNoise(int x, int z)
        {
            float total = 0f;
            float frequency = 1f;
            float amplitude = 1f;
            float maxValue = 0f;
            
            for (int i = 0; i < octaves; i++)
            {
                float sampleX = (x + seed) * scale * frequency;
                float sampleZ = (z + seed) * scale * frequency;
                
                float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f;
                
                total += perlinValue * amplitude;
                maxValue += amplitude;
                
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            
            return total / maxValue;
        }
        
        // ═══════════════════════════════════════════════════════════
        // BIOME DETECTION (da implementare)
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Determina il bioma alle coordinate mondiali</summary>
        private int GetBiomeAtPosition(int worldX, int worldZ)
        {
            // TODO: implementare logica biomi con noise secondario
            float biomeNoise = Mathf.PerlinNoise(worldX * biomeScale, worldZ * biomeScale);
            
            if (biomeNoise < 0.33f)
                return 0; // Forest
            else if (biomeNoise < 0.66f)
                return 1; // Plains
            else
                return 2; // Mountains
        }
    }
}
