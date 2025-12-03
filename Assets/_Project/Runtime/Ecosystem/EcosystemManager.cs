using UnityEngine;
using System.Collections.Generic;
using Elarion.VoxelEngine;
using Elarion.AuraSystem;

namespace Elarion.Ecosystem
{
    /// <summary>
    /// Gestore centrale dell'ecosistema.
    /// Gestisce tutte le piante, animali e processi naturali.
    /// Singleton per accesso globale.
    /// </summary>
    public class EcosystemManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════════════════════
        
        private static EcosystemManager instance;
        public static EcosystemManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<EcosystemManager>();
                }
                return instance;
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // CONFIGURAZIONE
        // ═══════════════════════════════════════════════════════════
        
        [Header("Plant Database")]
        [SerializeField] private PlantDefinition[] availablePlants;
        
        [Header("Update Settings")]
        [SerializeField] private float plantUpdateInterval = 1f;
        [SerializeField] private int maxPlantsPerUpdate = 50;
        
        [Header("Initial Population")]
        [SerializeField] private bool autoPopulateOnStart = true;
        [SerializeField] private int initialPlantCount = 100;
        [SerializeField] private int populationRadius = 64;
        
        [Header("Spread Settings")]
        [SerializeField] private bool enableNaturalSpread = true;
        [SerializeField] private float spreadCheckInterval = 10f;
        
        // ═══════════════════════════════════════════════════════════
        // RUNTIME DATA
        // ═══════════════════════════════════════════════════════════
        
        private List<PlantInstance> activePlants = new List<PlantInstance>();
        private float lastUpdateTime;
        private float lastSpreadCheckTime;
        private int currentUpdateIndex = 0;
        
        private WorldManager worldManager;
        private AuraGrid auraGrid;
        
        // ═══════════════════════════════════════════════════════════
        // LIFECYCLE
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
        }
        
        private void Start()
        {
            worldManager = WorldManager.Instance;
            auraGrid = AuraGrid.Instance;
            
            if (worldManager == null)
            {
                Debug.LogError("EcosystemManager: WorldManager not found!");
                enabled = false;
                return;
            }
            
            // Popolazione iniziale
            if (autoPopulateOnStart)
            {
                Invoke(nameof(PopulateInitialPlants), 2f); // Aspetta che il mondo sia caricato
            }
        }
        
        private void Update()
        {
            // Update piante
            if (Time.time - lastUpdateTime >= plantUpdateInterval)
            {
                UpdatePlants();
                lastUpdateTime = Time.time;
            }
            
            // Diffusione naturale
            if (enableNaturalSpread && Time.time - lastSpreadCheckTime >= spreadCheckInterval)
            {
                CheckPlantSpread();
                lastSpreadCheckTime = Time.time;
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // PLANT MANAGEMENT
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Aggiorna le piante attive</summary>
        private void UpdatePlants()
        {
            if (activePlants.Count == 0)
                return;
            
            int plantsUpdated = 0;
            
            for (int i = 0; i < maxPlantsPerUpdate && currentUpdateIndex < activePlants.Count; i++)
            {
                PlantInstance plant = activePlants[currentUpdateIndex];
                
                if (plant.isDead)
                {
                    activePlants.RemoveAt(currentUpdateIndex);
                    continue;
                }
                
                plant.Update(plantUpdateInterval);
                
                currentUpdateIndex++;
                plantsUpdated++;
            }
            
            // Reset index se abbiamo finito il ciclo
            if (currentUpdateIndex >= activePlants.Count)
            {
                currentUpdateIndex = 0;
            }
        }
        
        /// <summary>Controlla diffusione casuale delle piante</summary>
        private void CheckPlantSpread()
        {
            // Campiona alcune piante casuali
            int plantsToCheck = Mathf.Min(10, activePlants.Count);
            
            for (int i = 0; i < plantsToCheck; i++)
            {
                int randomIndex = Random.Range(0, activePlants.Count);
                activePlants[randomIndex].TrySpread();
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Pianta un seme in una posizione specifica</summary>
        public PlantInstance PlantSeed(PlantDefinition plantDef, Vector3Int position)
        {
            if (plantDef == null)
            {
                Debug.LogWarning("PlantSeed: null plant definition");
                return null;
            }
            
            // Verifica che il terreno sia adatto
            ushort soilBlock = worldManager.GetBlock(position.x, position.y - 1, position.z).blockID;
            ushort airBlock = worldManager.GetBlock(position.x, position.y, position.z).blockID;
            
            if (!plantDef.IsValidSoil(soilBlock) || airBlock != 0)
            {
                return null; // Terreno non adatto
            }
            
            // Crea istanza pianta
            PlantInstance plant = new PlantInstance(plantDef, position);
            activePlants.Add(plant);
            
            // Piazza voxel iniziali
            plant.Update(0); // Forza update per piazzare stage 0
            
            return plant;
        }
        
        /// <summary>Popolazione iniziale casuale</summary>
        private void PopulateInitialPlants()
        {
            if (availablePlants == null || availablePlants.Length == 0)
            {
                Debug.LogWarning("EcosystemManager: No plants defined for initial population");
                return;
            }
            
            int plantsSpawned = 0;
            int attempts = 0;
            int maxAttempts = initialPlantCount * 10;
            
            while (plantsSpawned < initialPlantCount && attempts < maxAttempts)
            {
                attempts++;
                
                // Posizione casuale nel raggio
                Vector3Int randomPos = new Vector3Int(
                    Random.Range(-populationRadius, populationRadius),
                    64, // Altezza base
                    Random.Range(-populationRadius, populationRadius)
                );
                
                // Trova superficie
                randomPos.y = FindSurfaceY(randomPos.x, randomPos.z);
                
                if (randomPos.y < 0)
                    continue;
                
                // Pianta casuale
                PlantDefinition randomPlant = availablePlants[Random.Range(0, availablePlants.Length)];
                
                if (PlantSeed(randomPlant, randomPos) != null)
                {
                    plantsSpawned++;
                }
            }
            
            Debug.Log($"EcosystemManager: Spawned {plantsSpawned} initial plants");
        }
        
        /// <summary>Trova l'altezza della superficie in una colonna XZ</summary>
        private int FindSurfaceY(int x, int z)
        {
            // Cerca dall'alto verso il basso
            for (int y = 127; y >= 0; y--)
            {
                ushort block = worldManager.GetBlock(x, y, z).blockID;
                
                if (block != 0) // Primo blocco solido
                {
                    // Verifica che sopra ci sia aria
                    ushort above = worldManager.GetBlock(x, y + 1, z).blockID;
                    if (above == 0)
                    {
                        return y + 1; // Posizione sopra il blocco solido
                    }
                }
            }
            
            return -1; // Nessuna superficie trovata
        }
        
        /// <summary>Rimuove tutte le piante</summary>
        public void ClearAllPlants()
        {
            foreach (PlantInstance plant in activePlants)
            {
                // Rimuovi voxel
                if (!plant.isDead)
                {
                    // TODO: chiamare metodo di cleanup
                }
            }
            
            activePlants.Clear();
            Debug.Log("EcosystemManager: All plants cleared");
        }
        
        // ═══════════════════════════════════════════════════════════
        // DEBUG
        // ═══════════════════════════════════════════════════════════
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 150, 300, 100));
            GUILayout.Label($"═══ ECOSYSTEM ═══", GUI.skin.box);
            GUILayout.Label($"Active Plants: {activePlants.Count}");
            
            int growing = 0;
            int fullyGrown = 0;
            foreach (var plant in activePlants)
            {
                if (plant.isFullyGrown) fullyGrown++;
                else growing++;
            }
            
            GUILayout.Label($"Growing: {growing} | Mature: {fullyGrown}");
            GUILayout.EndArea();
        }
    }
}
