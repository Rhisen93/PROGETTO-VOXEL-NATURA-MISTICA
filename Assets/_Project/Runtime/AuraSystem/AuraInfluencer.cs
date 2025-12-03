using UnityEngine;

namespace Elarion.AuraSystem
{
    /// <summary>
    /// Componente che influenza l'AuraGrid circostante.
    /// Può essere sorgente di purezza/energia o dissipatore (corruzione).
    /// </summary>
    public class AuraInfluencer : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // CONFIGURAZIONE
        // ═══════════════════════════════════════════════════════════
        
        [Header("Influence Type")]
        [SerializeField] private InfluenceType influenceType = InfluenceType.Source;
        
        [Header("Influence Values")]
        [SerializeField, Range(-1f, 1f)] private float purityModifier = 0.5f;
        [SerializeField, Range(-1f, 1f)] private float energyModifier = 0.3f;
        [SerializeField, Range(0f, 10f)] private float influenceStrength = 1.0f;
        
        [Header("Area of Effect")]
        [SerializeField, Range(1, 32)] private int influenceRadius = 8;
        [SerializeField] private AnimationCurve falloffCurve = AnimationCurve.Linear(0, 1, 1, 0);
        
        [Header("Update Settings")]
        [SerializeField] private bool updateContinuously = true;
        [SerializeField] private float updateInterval = 0.5f;
        
        // ═══════════════════════════════════════════════════════════
        // RUNTIME DATA
        // ═══════════════════════════════════════════════════════════
        
        private float lastUpdateTime;
        private AuraGrid auraGrid;
        
        // ═══════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════
        
        private void Start()
        {
            auraGrid = AuraGrid.Instance;
            
            if (auraGrid == null)
            {
                Debug.LogWarning($"AuraInfluencer on {gameObject.name}: AuraGrid not found!");
                enabled = false;
                return;
            }
            
            // Applica influenza iniziale
            ApplyInfluence();
        }
        
        private void Update()
        {
            if (!updateContinuously || auraGrid == null)
                return;
            
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                ApplyInfluence();
                lastUpdateTime = Time.time;
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // INFLUENCE APPLICATION
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Applica l'influenza all'AuraGrid</summary>
        public void ApplyInfluence()
        {
            if (auraGrid == null || !auraGrid.IsInitialized)
                return;
            
            Vector3 worldPos = transform.position;
            int gridResolution = auraGrid.GridResolution;
            
            // Converti posizione mondo in coordinate griglia
            int centerX = Mathf.FloorToInt(worldPos.x / gridResolution);
            int centerY = Mathf.FloorToInt(worldPos.y / gridResolution);
            int centerZ = Mathf.FloorToInt(worldPos.z / gridResolution);
            
            int radiusInGrid = Mathf.CeilToInt(influenceRadius / (float)gridResolution);
            
            // Itera su tutti i nodi nell'area di influenza
            for (int x = -radiusInGrid; x <= radiusInGrid; x++)
            {
                for (int y = -radiusInGrid; y <= radiusInGrid; y++)
                {
                    for (int z = -radiusInGrid; z <= radiusInGrid; z++)
                    {
                        int gridX = centerX + x;
                        int gridY = centerY + y;
                        int gridZ = centerZ + z;
                        
                        // Calcola distanza dal centro
                        float distance = Mathf.Sqrt(x * x + y * y + z * z);
                        
                        if (distance > radiusInGrid)
                            continue;
                        
                        // Calcola falloff
                        float normalizedDistance = distance / radiusInGrid;
                        float falloff = falloffCurve.Evaluate(normalizedDistance);
                        
                        // Ottieni nodo corrente
                        AuraNode node = auraGrid.GetNode(gridX, gridY, gridZ);
                        
                        // Applica modifiche
                        float strength = influenceStrength * falloff * Time.deltaTime;
                        
                        node.purity = Mathf.Clamp01(node.purity + purityModifier * strength);
                        node.energy = Mathf.Clamp01(node.energy + energyModifier * strength);
                        
                        // Aggiorna nodo
                        auraGrid.SetNode(gridX, gridY, gridZ, node);
                    }
                }
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // DEBUG
        // ═══════════════════════════════════════════════════════════
        
        private void OnDrawGizmosSelected()
        {
            // Visualizza area di influenza
            Gizmos.color = influenceType == InfluenceType.Source ? 
                new Color(0, 1, 0, 0.2f) : new Color(1, 0, 0, 0.2f);
            
            Gizmos.DrawWireSphere(transform.position, influenceRadius);
            
            // Visualizza forza
            Gizmos.color = influenceType == InfluenceType.Source ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, influenceRadius * 0.5f);
        }
        
        // ═══════════════════════════════════════════════════════════
        // ENUMS
        // ═══════════════════════════════════════════════════════════
        
        public enum InfluenceType
        {
            Source,      // Sorgente di purezza/energia
            Corruption,  // Sorgente di corruzione
            Neutral      // Influenza neutra
        }
    }
}
