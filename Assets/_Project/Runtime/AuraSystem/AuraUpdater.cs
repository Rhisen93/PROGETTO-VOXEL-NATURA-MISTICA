using UnityEngine;

namespace Elarion.AuraSystem
{
    /// <summary>
    /// Sistema di update continuo dell'AuraGrid.
    /// Gestisce diffusione, decay e propagazione dell'energia.
    /// </summary>
    public class AuraUpdater : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // CONFIGURAZIONE
        // ═══════════════════════════════════════════════════════════
        
        [Header("Update Settings")]
        [SerializeField] private bool enableAutoUpdate = true;
        [SerializeField] private float updateInterval = 0.1f;
        
        [Header("Diffusion")]
        [SerializeField, Range(0f, 1f)] private float diffusionRate = 0.05f;
        [SerializeField] private bool enableDiffusion = true;
        
        [Header("Decay")]
        [SerializeField, Range(0f, 0.1f)] private float energyDecayRate = 0.001f;
        [SerializeField, Range(0f, 0.1f)] private float purityDecayRate = 0.0005f;
        [SerializeField] private bool enableDecay = true;
        
        [Header("Performance")]
        [SerializeField] private int maxNodesPerFrame = 1000;
        
        // ═══════════════════════════════════════════════════════════
        // RUNTIME DATA
        // ═══════════════════════════════════════════════════════════
        
        private AuraGrid auraGrid;
        private float lastUpdateTime;
        private int currentUpdateIndex = 0;
        
        // ═══════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════
        
        private void Start()
        {
            auraGrid = AuraGrid.Instance;
            
            if (auraGrid == null)
            {
                Debug.LogWarning("AuraUpdater: AuraGrid not found!");
                enabled = false;
            }
        }
        
        private void Update()
        {
            if (!enableAutoUpdate || auraGrid == null || !auraGrid.IsInitialized)
                return;
            
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateAuraGrid();
                lastUpdateTime = Time.time;
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // UPDATE LOGIC
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Aggiorna l'AuraGrid (versione semplificata senza Jobs)</summary>
        private void UpdateAuraGrid()
        {
            Vector3Int dims = auraGrid.GridDimensions;
            int nodesProcessed = 0;
            
            // Update incrementale per distribuire il carico
            for (int i = 0; i < maxNodesPerFrame; i++)
            {
                // Calcola coordinate 3D dall'indice lineare
                int totalNodes = dims.x * dims.y * dims.z;
                
                if (currentUpdateIndex >= totalNodes)
                {
                    currentUpdateIndex = 0;
                    break;
                }
                
                int x = currentUpdateIndex % dims.x;
                int temp = currentUpdateIndex / dims.x;
                int y = temp % dims.y;
                int z = temp / dims.y;
                
                // Ottieni nodo corrente
                AuraNode node = auraGrid.GetNode(x, y, z);
                
                // Applica decay
                if (enableDecay)
                {
                    node.energy = Mathf.Max(0, node.energy - energyDecayRate * Time.deltaTime);
                    node.purity = Mathf.Clamp01(node.purity - purityDecayRate * Time.deltaTime);
                }
                
                // Applica diffusione (semplificata)
                if (enableDiffusion)
                {
                    node = ApplyDiffusion(x, y, z, node);
                }
                
                // Aggiorna nodo
                auraGrid.SetNode(x, y, z, node);
                
                currentUpdateIndex++;
                nodesProcessed++;
            }
        }
        
        /// <summary>Applica diffusione ai nodi vicini</summary>
        private AuraNode ApplyDiffusion(int x, int y, int z, AuraNode node)
        {
            // Semplificazione: media con i 6 nodi adiacenti
            float avgPurity = node.purity;
            float avgEnergy = node.energy;
            int neighborCount = 1;
            
            // Controlla i 6 vicini (sopra, sotto, sinistra, destra, avanti, dietro)
            Vector3Int[] neighbors = new Vector3Int[]
            {
                new Vector3Int(x + 1, y, z),
                new Vector3Int(x - 1, y, z),
                new Vector3Int(x, y + 1, z),
                new Vector3Int(x, y - 1, z),
                new Vector3Int(x, y, z + 1),
                new Vector3Int(x, y, z - 1)
            };
            
            foreach (var neighbor in neighbors)
            {
                AuraNode neighborNode = auraGrid.GetNode(neighbor.x, neighbor.y, neighbor.z);
                
                // Se il nodo vicino ha valori validi
                if (neighborNode.purity > 0 || neighborNode.energy > 0)
                {
                    avgPurity += neighborNode.purity;
                    avgEnergy += neighborNode.energy;
                    neighborCount++;
                }
            }
            
            // Calcola media
            avgPurity /= neighborCount;
            avgEnergy /= neighborCount;
            
            // Applica diffusione graduale
            node.purity = Mathf.Lerp(node.purity, avgPurity, diffusionRate * Time.deltaTime);
            node.energy = Mathf.Lerp(node.energy, avgEnergy, diffusionRate * Time.deltaTime);
            
            return node;
        }
        
        // ═══════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Forza un update completo della griglia</summary>
        public void ForceFullUpdate()
        {
            currentUpdateIndex = 0;
            
            Vector3Int dims = auraGrid.GridDimensions;
            int totalNodes = dims.x * dims.y * dims.z;
            
            // Processa tutti i nodi (attenzione: può essere lento!)
            for (int i = 0; i < totalNodes; i++)
            {
                int x = i % dims.x;
                int temp = i / dims.x;
                int y = temp % dims.y;
                int z = temp / dims.y;
                
                AuraNode node = auraGrid.GetNode(x, y, z);
                
                if (enableDecay)
                {
                    node.energy = Mathf.Max(0, node.energy - energyDecayRate);
                    node.purity = Mathf.Clamp01(node.purity - purityDecayRate);
                }
                
                auraGrid.SetNode(x, y, z, node);
            }
        }
        
        /// <summary>Resetta la griglia ai valori di default</summary>
        public void ResetGrid()
        {
            // Implementazione futura: reinizializza tutti i nodi
            Debug.Log("AuraUpdater: Grid reset requested");
        }
    }
}
