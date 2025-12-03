using UnityEngine;
using Elarion.VoxelEngine;
using Elarion.AuraSystem;

namespace Elarion.Ecosystem
{
    /// <summary>
    /// Istanza singola di una pianta nel mondo.
    /// Gestisce crescita, morte, diffusione.
    /// </summary>
    public class PlantInstance
    {
        // ═══════════════════════════════════════════════════════════
        // DATI PIANTA
        // ═══════════════════════════════════════════════════════════
        
        public PlantDefinition definition;
        public Vector3Int worldPosition; // Posizione base nel mondo
        
        // ═══════════════════════════════════════════════════════════
        // STATO
        // ═══════════════════════════════════════════════════════════
        
        public int currentStage;
        public float growthProgress; // 0-1 per lo stage corrente
        public bool isFullyGrown;
        public bool isDead;
        
        // ═══════════════════════════════════════════════════════════
        // TEMPO
        // ═══════════════════════════════════════════════════════════
        
        public float plantedTime;
        public float lastUpdateTime;
        public float nextGrowthTime;
        public float timeWithoutIdealConditions;
        
        // ═══════════════════════════════════════════════════════════
        // RIFERIMENTI
        // ═══════════════════════════════════════════════════════════
        
        private WorldManager worldManager;
        private AuraGrid auraGrid;
        
        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════
        
        public PlantInstance(PlantDefinition def, Vector3Int position)
        {
            definition = def;
            worldPosition = position;
            
            currentStage = 0;
            growthProgress = 0f;
            isFullyGrown = false;
            isDead = false;
            
            plantedTime = Time.time;
            lastUpdateTime = Time.time;
            
            // Calcola tempo prossima crescita con variazione
            float variation = Random.Range(-def.growthTimeVariation, def.growthTimeVariation);
            nextGrowthTime = plantedTime + def.growthTimePerStage * (1f + variation);
            
            worldManager = WorldManager.Instance;
            auraGrid = AuraGrid.Instance;
        }
        
        // ═══════════════════════════════════════════════════════════
        // UPDATE
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Aggiorna lo stato della pianta</summary>
        public void Update(float deltaTime)
        {
            if (isDead || definition == null)
                return;
            
            lastUpdateTime = Time.time;
            
            // Controlla condizioni ambientali
            bool hasIdealConditions = CheckGrowthConditions();
            
            if (!hasIdealConditions)
            {
                timeWithoutIdealConditions += deltaTime;
                
                // Muore se senza condizioni per troppo tempo
                if (definition.canWither && timeWithoutIdealConditions >= definition.witherTime)
                {
                    Wither();
                    return;
                }
            }
            else
            {
                timeWithoutIdealConditions = 0f;
                
                // Crescita
                if (!isFullyGrown && Time.time >= nextGrowthTime)
                {
                    GrowToNextStage();
                }
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // CRESCITA
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Controlla se le condizioni ambientali sono adatte</summary>
        private bool CheckGrowthConditions()
        {
            if (auraGrid == null || !auraGrid.IsInitialized)
                return true; // Fallback: cresce sempre se no aura
            
            // Converti posizione mondo in coordinate griglia aura
            int gridResolution = auraGrid.GridResolution;
            int gridX = worldPosition.x / gridResolution;
            int gridY = worldPosition.y / gridResolution;
            int gridZ = worldPosition.z / gridResolution;
            
            AuraNode node = auraGrid.GetNode(gridX, gridY, gridZ);
            
            // Controlla purezza e energia
            if (node.purity < definition.minPurity)
                return false;
            
            if (node.energy < definition.minEnergy)
                return false;
            
            // TODO: controllare luce, acqua quando implementati
            
            return true;
        }
        
        /// <summary>Fa crescere la pianta allo stage successivo</summary>
        private void GrowToNextStage()
        {
            if (currentStage >= definition.maxGrowthStages - 1)
            {
                isFullyGrown = true;
                return;
            }
            
            // Rimuovi voxel stage precedente
            RemoveCurrentStageVoxels();
            
            // Avanza stage
            currentStage++;
            growthProgress = 0f;
            
            // Piazza voxel nuovo stage
            PlaceCurrentStageVoxels();
            
            // Calcola prossimo tempo crescita
            if (currentStage < definition.maxGrowthStages - 1)
            {
                float variation = Random.Range(-definition.growthTimeVariation, definition.growthTimeVariation);
                nextGrowthTime = Time.time + definition.growthTimePerStage * (1f + variation);
            }
            else
            {
                isFullyGrown = true;
            }
            
            Debug.Log($"Plant {definition.plantName} grew to stage {currentStage} at {worldPosition}");
        }
        
        /// <summary>Piazza i voxel dello stage corrente nel mondo</summary>
        private void PlaceCurrentStageVoxels()
        {
            if (worldManager == null)
                return;
            
            PlantStageData stageData = definition.GetStageData(currentStage);
            if (stageData == null || stageData.voxels == null)
                return;
            
            foreach (VoxelOffset voxel in stageData.voxels)
            {
                Vector3Int worldPos = worldPosition + voxel.offset;
                worldManager.SetBlock(worldPos.x, worldPos.y, worldPos.z, new VoxelBlock(voxel.blockID));
            }
        }
        
        /// <summary>Rimuove i voxel dello stage corrente dal mondo</summary>
        private void RemoveCurrentStageVoxels()
        {
            if (worldManager == null)
                return;
            
            PlantStageData stageData = definition.GetStageData(currentStage);
            if (stageData == null || stageData.voxels == null)
                return;
            
            foreach (VoxelOffset voxel in stageData.voxels)
            {
                Vector3Int worldPos = worldPosition + voxel.offset;
                worldManager.SetBlock(worldPos.x, worldPos.y, worldPos.z, new VoxelBlock(0)); // 0 = Air
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // MORTE
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Fa morire la pianta</summary>
        private void Wither()
        {
            isDead = true;
            RemoveCurrentStageVoxels();
            
            Debug.Log($"Plant {definition.plantName} withered at {worldPosition}");
            
            // TODO: sostituire con blocco "dead plant" o lasciare vuoto
        }
        
        // ═══════════════════════════════════════════════════════════
        // DIFFUSIONE
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Tenta di diffondere semi/spore</summary>
        public void TrySpread()
        {
            if (!isFullyGrown || isDead)
                return;
            
            if (Random.value > definition.spreadProbability)
                return;
            
            // Trova posizione casuale nel raggio
            Vector3Int offset = new Vector3Int(
                Random.Range(-definition.spreadRadius, definition.spreadRadius + 1),
                0,
                Random.Range(-definition.spreadRadius, definition.spreadRadius + 1)
            );
            
            Vector3Int targetPos = worldPosition + offset;
            
            // Verifica se il terreno è adatto
            if (worldManager != null)
            {
                ushort soilBlock = worldManager.GetBlock(targetPos.x, targetPos.y - 1, targetPos.z).blockID;
                ushort airBlock = worldManager.GetBlock(targetPos.x, targetPos.y, targetPos.z).blockID;
                
                if (definition.IsValidSoil(soilBlock) && airBlock == 0)
                {
                    // Crea nuova pianta
                    EcosystemManager ecosystem = EcosystemManager.Instance;
                    if (ecosystem != null)
                    {
                        ecosystem.PlantSeed(definition, targetPos);
                    }
                }
            }
        }
    }
}
