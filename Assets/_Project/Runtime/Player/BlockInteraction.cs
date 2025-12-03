using UnityEngine;
using UnityEngine.InputSystem;
using Elarion.VoxelEngine;

namespace Elarion.Player
{
    /// <summary>
    /// Sistema di interazione con i blocchi voxel.
    /// Gestisce piazzamento, rimozione e selezione blocchi.
    /// </summary>
    public class BlockInteraction : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // CONFIGURAZIONE
        // ═══════════════════════════════════════════════════════════
        
        [Header("Interaction Settings")]
        [SerializeField] private float maxReachDistance = 5f;
        [SerializeField] private LayerMask blockLayerMask;
        
        [Header("Block Selection")]
        [SerializeField] private ushort selectedBlockID = 2; // Default: Grass
        [SerializeField] private Material highlightMaterial;
        
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        
        // ═══════════════════════════════════════════════════════════
        // RUNTIME DATA
        // ═══════════════════════════════════════════════════════════
        
        private WorldManager worldManager;
        private bool placeBlockPressed;
        private bool removeBlockPressed;
        
        private Vector3Int? targetBlockPos;
        private Vector3Int? adjacentBlockPos;
        
        // ═══════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════
        
        private void Start()
        {
            worldManager = WorldManager.Instance;
            
            if (worldManager == null)
            {
                Debug.LogError("BlockInteraction: WorldManager not found!");
                enabled = false;
            }
            
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }
        
        private void Update()
        {
            UpdateTargetBlock();
            HandleBlockPlacement();
            HandleBlockRemoval();
        }
        
        // ═══════════════════════════════════════════════════════════
        // TARGET BLOCK DETECTION
        // ═══════════════════════════════════════════════════════════
        
        private void UpdateTargetBlock()
        {
            if (playerCamera == null)
                return;
            
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            
            if (Physics.Raycast(ray, out RaycastHit hit, maxReachDistance, blockLayerMask))
            {
                // Blocco colpito (per rimozione)
                targetBlockPos = Vector3Int.FloorToInt(hit.point - hit.normal * 0.5f);
                
                // Posizione adiacente (per piazzamento)
                adjacentBlockPos = Vector3Int.FloorToInt(hit.point + hit.normal * 0.5f);
            }
            else
            {
                targetBlockPos = null;
                adjacentBlockPos = null;
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // BLOCK PLACEMENT
        // ═══════════════════════════════════════════════════════════
        
        private void HandleBlockPlacement()
        {
            if (!placeBlockPressed || adjacentBlockPos == null)
                return;
            
            Vector3Int pos = adjacentBlockPos.Value;
            
            // Verifica che non si piazzi dentro il player
            if (!IsPositionInsidePlayer(pos))
            {
                worldManager.SetBlock(pos.x, pos.y, pos.z, new VoxelBlock(selectedBlockID));
                Debug.Log($"Placed block {selectedBlockID} at {pos}");
            }
            
            placeBlockPressed = false;
        }
        
        // ═══════════════════════════════════════════════════════════
        // BLOCK REMOVAL
        // ═══════════════════════════════════════════════════════════
        
        private void HandleBlockRemoval()
        {
            if (!removeBlockPressed || targetBlockPos == null)
                return;
            
            Vector3Int pos = targetBlockPos.Value;
            
            // Rimuovi blocco (imposta ad aria = 0)
            worldManager.SetBlock(pos.x, pos.y, pos.z, new VoxelBlock(0));
            Debug.Log($"Removed block at {pos}");
            
            removeBlockPressed = false;
        }
        
        // ═══════════════════════════════════════════════════════════
        // UTILITY
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Controlla se la posizione è dentro il player</summary>
        private bool IsPositionInsidePlayer(Vector3Int blockPos)
        {
            // Semplice check: se blocco troppo vicino al player
            Vector3 blockWorldPos = new Vector3(blockPos.x + 0.5f, blockPos.y + 0.5f, blockPos.z + 0.5f);
            float distance = Vector3.Distance(transform.position, blockWorldPos);
            
            return distance < 1.5f; // Raggio player
        }
        
        /// <summary>Cambia blocco selezionato</summary>
        public void SetSelectedBlock(ushort blockID)
        {
            selectedBlockID = blockID;
            Debug.Log($"Selected block: {blockID}");
        }
        
        /// <summary>Cicla al blocco successivo</summary>
        public void CycleBlockForward()
        {
            selectedBlockID++;
            if (selectedBlockID > 5) // Max block ID
                selectedBlockID = 1;
            
            Debug.Log($"Selected block: {selectedBlockID}");
        }
        
        /// <summary>Cicla al blocco precedente</summary>
        public void CycleBlockBackward()
        {
            selectedBlockID--;
            if (selectedBlockID < 1)
                selectedBlockID = 5; // Max block ID
            
            Debug.Log($"Selected block: {selectedBlockID}");
        }
        
        // ═══════════════════════════════════════════════════════════
        // INPUT CALLBACKS (Send Messages behavior)
        // ═══════════════════════════════════════════════════════════
        
        public void OnAttack(UnityEngine.InputSystem.InputValue value)
        {
            if (value.isPressed)
                placeBlockPressed = true;
        }
        
        public void OnInteract(UnityEngine.InputSystem.InputValue value)
        {
            if (value.isPressed)
                removeBlockPressed = true;
        }
        
        public void OnNext(UnityEngine.InputSystem.InputValue value)
        {
            if (value.isPressed)
                CycleBlockForward();
        }
        
        public void OnPrevious(UnityEngine.InputSystem.InputValue value)
        {
            if (value.isPressed)
                CycleBlockBackward();
        }
        
        // ═══════════════════════════════════════════════════════════
        // DEBUG
        // ═══════════════════════════════════════════════════════════
        
        private void OnDrawGizmos()
        {
            if (targetBlockPos.HasValue)
            {
                // Visualizza blocco target (rosso = rimuovi)
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(targetBlockPos.Value + Vector3.one * 0.5f, Vector3.one);
            }
            
            if (adjacentBlockPos.HasValue)
            {
                // Visualizza posizione piazzamento (verde)
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(adjacentBlockPos.Value + Vector3.one * 0.5f, Vector3.one * 1.02f);
            }
        }
        
        private void OnGUI()
        {
            // HUD temporaneo
            GUILayout.BeginArea(new Rect(10, 270, 300, 80));
            GUILayout.Label("═══ BLOCK INTERACTION ═══", GUI.skin.box);
            GUILayout.Label($"Selected Block: {selectedBlockID}");
            GUILayout.Label($"Target: {(targetBlockPos.HasValue ? targetBlockPos.Value.ToString() : "None")}");
            GUILayout.Label($"Scroll wheel to change block");
            GUILayout.EndArea();
        }
    }
}
