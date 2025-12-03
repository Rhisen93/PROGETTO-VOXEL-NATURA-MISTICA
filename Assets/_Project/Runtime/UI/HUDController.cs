using UnityEngine;
using UnityEngine.UIElements;
using Elarion.Player;
using Elarion.VoxelEngine;
using Elarion.AuraSystem;

namespace Elarion.UI
{
    /// <summary>
    /// Controller principale dell'HUD.
    /// Gestisce l'interfaccia utente runtime con UI Toolkit.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class HUDController : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // CONFIGURAZIONE
        // ═══════════════════════════════════════════════════════════
        
        [Header("References")]
        [SerializeField] private FirstPersonController playerController;
        [SerializeField] private BlockInteraction blockInteraction;
        
        [Header("Settings")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private float updateInterval = 0.1f;
        
        // ═══════════════════════════════════════════════════════════
        // UI ELEMENTS
        // ═══════════════════════════════════════════════════════════
        
        private UIDocument uiDocument;
        private VisualElement root;
        
        // Top Bar
        private Label healthLabel;
        private Label positionLabel;
        private Label purityLabel;
        private Label energyLabel;
        
        // Bottom Bar
        private Label fpsLabel;
        private Label chunksLabel;
        private VisualElement[] hotbarSlots;
        
        // Notifications
        private VisualElement notificationArea;
        
        // ═══════════════════════════════════════════════════════════
        // RUNTIME DATA
        // ═══════════════════════════════════════════════════════════
        
        private float lastUpdateTime;
        private int currentHotbarIndex = 0;
        
        private WorldManager worldManager;
        private AuraGrid auraGrid;
        
        // FPS calculation
        private float deltaTime;
        
        // ═══════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════
        
        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            
            if (uiDocument == null)
            {
                Debug.LogError("HUDController: UIDocument component not found!");
                enabled = false;
                return;
            }
        }
        
        private void Start()
        {
            // Get references
            worldManager = WorldManager.Instance;
            auraGrid = AuraGrid.Instance;
            
            if (playerController == null)
                playerController = FindAnyObjectByType<FirstPersonController>();
            
            if (blockInteraction == null)
                blockInteraction = FindAnyObjectByType<BlockInteraction>();
            
            // Setup UI
            SetupUI();
        }
        
        private void Update()
        {
            // FPS tracking
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            
            // Update UI at intervals
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateUI();
                lastUpdateTime = Time.time;
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // UI SETUP
        // ═══════════════════════════════════════════════════════════
        
        private void SetupUI()
        {
            root = uiDocument.rootVisualElement;
            
            if (root == null)
            {
                Debug.LogError("HUDController: Root visual element not found!");
                return;
            }
            
            // Get UI elements
            healthLabel = root.Q<Label>("HealthLabel");
            positionLabel = root.Q<Label>("PositionLabel");
            purityLabel = root.Q<Label>("PurityLabel");
            energyLabel = root.Q<Label>("EnergyLabel");
            fpsLabel = root.Q<Label>("FPSLabel");
            chunksLabel = root.Q<Label>("ChunksLabel");
            notificationArea = root.Q<VisualElement>("NotificationArea");
            
            // Setup hotbar
            hotbarSlots = new VisualElement[5];
            for (int i = 0; i < 5; i++)
            {
                hotbarSlots[i] = root.Q<VisualElement>($"HotbarSlot{i}");
            }
            
            // Initial update
            UpdateHotbarSelection(0);
        }
        
        // ═══════════════════════════════════════════════════════════
        // UI UPDATE
        // ═══════════════════════════════════════════════════════════
        
        private void UpdateUI()
        {
            UpdatePlayerStats();
            UpdateAuraInfo();
            UpdateDebugInfo();
        }
        
        private void UpdatePlayerStats()
        {
            if (playerController == null)
                return;
            
            // Position
            Vector3 pos = playerController.transform.position;
            if (positionLabel != null)
            {
                positionLabel.text = $"Position: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})";
            }
            
            // Health (placeholder - implementare sistema vita)
            if (healthLabel != null)
            {
                string status = "";
                if (playerController.IsSprinting) status += " [SPRINT]";
                if (playerController.IsCrouching) status += " [CROUCH]";
                
                healthLabel.text = $"Health: 100{status}";
            }
        }
        
        private void UpdateAuraInfo()
        {
            if (auraGrid == null || !auraGrid.IsInitialized || playerController == null)
                return;
            
            // Get aura at player position
            Vector3 playerPos = playerController.transform.position;
            int gridResolution = auraGrid.GridResolution;
            
            int gridX = Mathf.FloorToInt(playerPos.x / gridResolution);
            int gridY = Mathf.FloorToInt(playerPos.y / gridResolution);
            int gridZ = Mathf.FloorToInt(playerPos.z / gridResolution);
            
            AuraNode node = auraGrid.GetNode(gridX, gridY, gridZ);
            
            if (purityLabel != null)
            {
                float purity = node.purity * 100f;
                purityLabel.text = $"Aura Purity: {purity:F0}%";
                
                // Color based on purity
                if (purity > 70)
                    purityLabel.style.color = new Color(0.6f, 1f, 0.6f);
                else if (purity > 40)
                    purityLabel.style.color = new Color(1f, 1f, 0.6f);
                else
                    purityLabel.style.color = new Color(1f, 0.6f, 0.6f);
            }
            
            if (energyLabel != null)
            {
                float energy = node.energy * 100f;
                energyLabel.text = $"Energy: {energy:F0}%";
                
                // Color based on energy
                energyLabel.style.color = new Color(0.4f, 0.8f, 1f, 0.5f + node.energy * 0.5f);
            }
        }
        
        private void UpdateDebugInfo()
        {
            if (!showDebugInfo)
                return;
            
            // FPS
            if (fpsLabel != null)
            {
                float fps = 1.0f / deltaTime;
                fpsLabel.text = $"FPS: {fps:F0}";
            }
            
            // Chunks
            if (chunksLabel != null && worldManager != null)
            {
                // TODO: esporre conteggio chunk da WorldManager
                chunksLabel.text = "Chunks: N/A";
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // HOTBAR
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Aggiorna selezione hotbar</summary>
        public void UpdateHotbarSelection(int index)
        {
            if (hotbarSlots == null || hotbarSlots.Length == 0)
                return;
            
            // Remove previous selection
            if (currentHotbarIndex >= 0 && currentHotbarIndex < hotbarSlots.Length && hotbarSlots[currentHotbarIndex] != null)
            {
                hotbarSlots[currentHotbarIndex].RemoveFromClassList("hotbar-slot-selected");
            }
            
            // Add new selection
            currentHotbarIndex = index;
            if (currentHotbarIndex >= 0 && currentHotbarIndex < hotbarSlots.Length && hotbarSlots[currentHotbarIndex] != null)
            {
                hotbarSlots[currentHotbarIndex].AddToClassList("hotbar-slot-selected");
            }
        }
        
        /// <summary>Aggiorna nome blocco in uno slot</summary>
        public void SetHotbarBlockName(int index, string blockName)
        {
            if (index < 0 || index >= hotbarSlots.Length)
                return;
            
            Label label = hotbarSlots[index]?.Q<Label>("BlockName" + index);
            if (label != null)
            {
                label.text = blockName;
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // NOTIFICATIONS
        // ═══════════════════════════════════════════════════════════
        
        /// <summary>Mostra una notifica temporanea</summary>
        public void ShowNotification(string message, float duration = 3f)
        {
            if (notificationArea == null)
                return;
            
            // Create notification element
            VisualElement notification = new VisualElement();
            notification.AddToClassList("notification");
            
            Label notificationText = new Label(message);
            notificationText.AddToClassList("notification-text");
            
            notification.Add(notificationText);
            notificationArea.Add(notification);
            
            // Auto-remove after duration
            float startTime = Time.time;
            notification.schedule.Execute(() =>
            {
                if (Time.time - startTime >= duration)
                {
                    // Fade out
                    notification.AddToClassList("notification-fade");
                    notification.style.opacity = 0;
                    
                    // Remove after fade
                    notification.schedule.Execute(() =>
                    {
                        notificationArea.Remove(notification);
                    }).StartingIn(500);
                }
            }).Every(100);
        }
        
        // ═══════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════
        
        public void SetHealth(int health)
        {
            if (healthLabel != null)
                healthLabel.text = $"Health: {health}";
        }
        
        public void ToggleDebugInfo()
        {
            showDebugInfo = !showDebugInfo;
            
            if (!showDebugInfo)
            {
                if (fpsLabel != null) fpsLabel.text = "";
                if (chunksLabel != null) chunksLabel.text = "";
            }
        }
    }
}
