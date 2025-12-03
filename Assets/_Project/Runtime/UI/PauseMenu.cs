using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityCursor = UnityEngine.Cursor;

namespace Elarion.UI
{
    /// <summary>
    /// Menu pausa con UI Toolkit.
    /// Gestisce pausa, opzioni e uscita dal gioco.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // CONFIGURAZIONE
        // ═══════════════════════════════════════════════════════════
        
        [Header("References")]
        [SerializeField] private UIDocument pauseMenuDocument;
        
        [Header("Settings")]
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
        
        // ═══════════════════════════════════════════════════════════
        // STATE
        // ═══════════════════════════════════════════════════════════
        
        private bool isPaused = false;
        private VisualElement root;
        private VisualElement menuPanel;
        
        // ═══════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════
        
        private void Start()
        {
            if (pauseMenuDocument == null)
            {
                Debug.LogWarning("PauseMenu: No UIDocument assigned");
                return;
            }
            
            root = pauseMenuDocument.rootVisualElement;
            menuPanel = root.Q<VisualElement>("PauseMenuPanel");
            
            // Setup buttons
            Button resumeButton = root.Q<Button>("ResumeButton");
            Button optionsButton = root.Q<Button>("OptionsButton");
            Button quitButton = root.Q<Button>("QuitButton");
            
            if (resumeButton != null)
                resumeButton.clicked += ResumeGame;
            
            if (optionsButton != null)
                optionsButton.clicked += OpenOptions;
            
            if (quitButton != null)
                quitButton.clicked += QuitGame;
            
            // Hide menu initially
            HideMenu();
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(pauseKey))
            {
                TogglePause();
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // PAUSE LOGIC
        // ═══════════════════════════════════════════════════════════
        
        public void TogglePause()
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
        
        private void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;
            
            ShowMenu();
            
            // Unlock cursor
            UnityCursor.lockState = CursorLockMode.None;
            UnityCursor.visible = true;
        }
        
        private void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
            
            HideMenu();
            
            // Lock cursor
            UnityCursor.lockState = CursorLockMode.Locked;
            UnityCursor.visible = false;
        }
        
        // ═══════════════════════════════════════════════════════════
        // MENU ACTIONS
        // ═══════════════════════════════════════════════════════════
        
        private void OpenOptions()
        {
            Debug.Log("Options menu - TODO");
            // TODO: implementare menu opzioni
        }
        
        private void QuitGame()
        {
            Debug.Log("Quitting game...");
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        // ═══════════════════════════════════════════════════════════
        // UI VISIBILITY
        // ═══════════════════════════════════════════════════════════
        
        private void ShowMenu()
        {
            if (menuPanel != null)
                menuPanel.style.display = DisplayStyle.Flex;
        }
        
        private void HideMenu()
        {
            if (menuPanel != null)
                menuPanel.style.display = DisplayStyle.None;
        }
    }
}
