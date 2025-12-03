using UnityEngine;

namespace Elarion.VoxelEngine
{
    /// <summary>
    /// Generatore di noise procedurale per terrain generation.
    /// Supporta Perlin noise multi-octave.
    /// VERSIONE SEMPLIFICATA (solo Perlin Unity built-in)
    /// </summary>
    public static class NoiseGenerator
    {
        // ═══════════════════════════════════════════════════════════
        // PERLIN NOISE
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>
        /// Genera Perlin noise 2D con parametri avanzati.
        /// </summary>
        /// <param name="x">Coordinata X</param>
        /// <param name="y">Coordinata Y</param>
        /// <param name="scale">Scala del noise (valori più piccoli = più variazione)</param>
        /// <param name="octaves">Numero di octaves (più alto = più dettaglio)</param>
        /// <param name="persistence">Quanto ogni octave contribuisce (0-1)</param>
        /// <param name="lacunarity">Frequenza moltiplicatore tra octaves</param>
        /// <param name="seed">Seed per randomizzazione</param>
        /// <returns>Valore noise normalizzato tra -1 e 1</returns>
        public static float PerlinNoise2D(
            float x, 
            float y, 
            float scale = 1f, 
            int octaves = 1,
            float persistence = 0.5f,
            float lacunarity = 2f,
            int seed = 0)
        {
            float total = 0f;
            float frequency = 1f;
            float amplitude = 1f;
            float maxValue = 0f;
            
            // Offset basato sul seed
            float offsetX = seed * 1000f;
            float offsetY = seed * 1000f + 1000f;
            
            for (int i = 0; i < octaves; i++)
            {
                float sampleX = (x + offsetX) * scale * frequency;
                float sampleY = (y + offsetY) * scale * frequency;
                
                // Unity Perlin ritorna [0,1], convertiamo a [-1,1]
                float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;
                
                total += perlinValue * amplitude;
                maxValue += amplitude;
                
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            
            return total / maxValue;
        }
        
        // ═══════════════════════════════════════════════════════════
        // UTILITY NOISE FUNCTIONS
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Rimappa un valore da un range a un altro</summary>
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
        }
        
        /// <summary>Applica una curva al noise per controllare distribuzione</summary>
        public static float ApplyCurve(float value, float exponent)
        {
            return Mathf.Pow(Mathf.Abs(value), exponent) * Mathf.Sign(value);
        }
        
        // ═══════════════════════════════════════════════════════════
        // NOISE LAYER STRUCT
        // ═══════════════════════════════════════════════════════════
        
        [System.Serializable]
        public struct NoiseLayer
        {
            public float scale;
            public int octaves;
            public float persistence;
            public float lacunarity;
            public float weight;
            public int seed;
            
            public NoiseLayer(float scale, int octaves, float weight, int seed = 0)
            {
                this.scale = scale;
                this.octaves = octaves;
                this.persistence = 0.5f;
                this.lacunarity = 2f;
                this.weight = weight;
                this.seed = seed;
            }
        }
    }
}
