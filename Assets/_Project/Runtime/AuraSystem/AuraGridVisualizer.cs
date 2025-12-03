using UnityEngine;

namespace Elarion.AuraSystem
{
    /// <summary>
    /// Script di test per visualizzare l'AuraGrid nell'editor.
    /// Mostra i valori di purezza/energia come gizmos colorati.
    /// </summary>
    public class AuraGridVisualizer : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [SerializeField] private bool showGrid = true;
        [SerializeField] private bool showOnlyActiveNodes = true;
        [SerializeField, Range(0.01f, 1f)] private float minValueToShow = 0.1f;
        
        [Header("Display")]
        [SerializeField] private int maxNodesToVisualize = 500;
        [SerializeField] private float nodeSize = 0.5f;
        
        [Header("Colors")]
        [SerializeField] private Color pureColor = Color.green;
        [SerializeField] private Color corruptedColor = Color.red;
        [SerializeField] private Color energyColor = Color.cyan;
        
        private AuraGrid auraGrid;
        
        private void Start()
        {
            auraGrid = AuraGrid.Instance;
            
            if (auraGrid == null)
            {
                Debug.LogWarning("AuraGridVisualizer: AuraGrid not found!");
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!showGrid || auraGrid == null || !auraGrid.IsInitialized)
                return;
            
            Vector3Int dims = auraGrid.GridDimensions;
            int gridResolution = auraGrid.GridResolution;
            
            int nodesDrawn = 0;
            
            // Campiona la griglia (troppi nodi per visualizzarli tutti)
            int step = Mathf.Max(1, dims.x / 20);
            
            for (int x = 0; x < dims.x; x += step)
            {
                for (int y = 0; y < dims.y; y += step)
                {
                    for (int z = 0; z < dims.z; z += step)
                    {
                        if (nodesDrawn >= maxNodesToVisualize)
                            return;
                        
                        AuraNode node = auraGrid.GetNode(x, y, z);
                        
                        // Salta nodi inattivi se richiesto
                        if (showOnlyActiveNodes && node.energy < minValueToShow && node.purity < 0.5f)
                            continue;
                        
                        // Calcola posizione mondo
                        Vector3 worldPos = new Vector3(
                            x * gridResolution,
                            y * gridResolution,
                            z * gridResolution
                        );
                        
                        // Calcola colore in base a purezza
                        Color nodeColor = Color.Lerp(corruptedColor, pureColor, node.purity);
                        
                        // Mix con energia
                        nodeColor = Color.Lerp(nodeColor, energyColor, node.energy * 0.5f);
                        nodeColor.a = 0.6f;
                        
                        Gizmos.color = nodeColor;
                        Gizmos.DrawSphere(worldPos, nodeSize);
                        
                        nodesDrawn++;
                    }
                }
            }
        }
    }
}
