using UnityEngine;
using UnityEngine.InputSystem;

namespace Elarion.Player
{
    /// <summary>
    /// Controller first-person per il player.
    /// Gestisce movimento, rotazione camera, gravità e collisioni.
    /// Compatibile con Unity Input System.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════
        // CONFIGURAZIONE
        // ═══════════════════════════════════════════════════════════
        
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float crouchSpeed = 2.5f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 10f;
        
        [Header("Jump & Gravity")]
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundedGravity = -2f;
        
        [Header("Camera")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float lookUpLimit = 80f;
        [SerializeField] private float lookDownLimit = -80f;
        
        [Header("Crouch")]
        [SerializeField] private float standingHeight = 2f;
        [SerializeField] private float crouchHeight = 1f;
        [SerializeField] private float crouchTransitionSpeed = 10f;
        
        [Header("Ground Check")]
        [SerializeField] private float groundCheckDistance = 0.3f;
        [SerializeField] private LayerMask groundMask;
        
        // ═══════════════════════════════════════════════════════════
        // COMPONENTI
        // ═══════════════════════════════════════════════════════════
        
        private CharacterController characterController;
        
        // ═══════════════════════════════════════════════════════════
        // STATO MOVIMENTO
        // ═══════════════════════════════════════════════════════════
        
        private Vector3 currentVelocity;
        private Vector3 targetVelocity;
        private float verticalVelocity;
        
        private bool isGrounded;
        private bool isSprinting;
        private bool isCrouching;
        private float currentHeight;
        
        // ═══════════════════════════════════════════════════════════
        // STATO CAMERA
        // ═══════════════════════════════════════════════════════════
        
        private float cameraPitch = 0f;
        private Vector2 lookInput;
        
        // ═══════════════════════════════════════════════════════════
        // INPUT
        // ═══════════════════════════════════════════════════════════
        
        private Vector2 moveInput;
        private bool jumpPressed;
        private bool sprintHeld;
        private bool crouchHeld;
        
        // ═══════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════
        
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            currentHeight = standingHeight;
            
            // Trova camera se non assegnata
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main?.transform;
                if (cameraTransform == null)
                {
                    Debug.LogError("FirstPersonController: No camera assigned or found!");
                }
            }
            
            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        private void Update()
        {
            HandleGroundCheck();
            HandleCrouch();
            HandleMovement();
            HandleJump();
            HandleGravity();
            HandleCameraRotation();
            
            // Applica movimento
            characterController.Move(currentVelocity * Time.deltaTime);
            
            // Input toggle cursor
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ToggleCursor();
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // GROUND CHECK
        // ═══════════════════════════════════════════════════════════
        
        private void HandleGroundCheck()
        {
            // Raycast dal centro verso il basso
            Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
            isGrounded = Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance, groundMask);
            
            // Fallback: usa CharacterController.isGrounded
            if (!isGrounded)
            {
                isGrounded = characterController.isGrounded;
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // MOVIMENTO
        // ═══════════════════════════════════════════════════════════
        
        private void HandleMovement()
        {
            // Calcola direzione movimento
            Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            moveDirection.y = 0;
            
            // Determina velocità target
            float targetSpeed = walkSpeed;
            
            if (isCrouching)
                targetSpeed = crouchSpeed;
            else if (isSprinting && !isCrouching)
                targetSpeed = sprintSpeed;
            
            // Calcola velocità target
            targetVelocity = moveDirection.normalized * targetSpeed;
            
            // Interpolazione smooth
            float lerpSpeed = moveInput.magnitude > 0.1f ? acceleration : deceleration;
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, lerpSpeed * Time.deltaTime);
            
            // Mantieni velocità verticale, aggiorna orizzontale
            currentVelocity.x = horizontalVelocity.x;
            currentVelocity.z = horizontalVelocity.z;
        }
        
        // ═══════════════════════════════════════════════════════════
        // JUMP
        // ═══════════════════════════════════════════════════════════
        
        private void HandleJump()
        {
            if (jumpPressed && isGrounded && !isCrouching)
            {
                // Formula: v = sqrt(2 * h * g)
                verticalVelocity = Mathf.Sqrt(2 * jumpHeight * Mathf.Abs(gravity));
                jumpPressed = false; // Consuma input
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // GRAVITY
        // ═══════════════════════════════════════════════════════════
        
        private void HandleGravity()
        {
            if (isGrounded && verticalVelocity < 0)
            {
                verticalVelocity = groundedGravity; // Piccola forza per tenere a terra
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }
            
            currentVelocity.y = verticalVelocity;
        }
        
        // ═══════════════════════════════════════════════════════════
        // CROUCH
        // ═══════════════════════════════════════════════════════════
        
        private void HandleCrouch()
        {
            float targetHeight = crouchHeld ? crouchHeight : standingHeight;
            
            // Se sta passando da crouch a standing, controlla se c'è spazio
            if (!crouchHeld && isCrouching)
            {
                if (!CanStandUp())
                {
                    targetHeight = crouchHeight;
                    crouchHeld = true; // Forza crouch
                }
            }
            
            // Interpola altezza
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
            characterController.height = currentHeight;
            
            // Aggiorna centro collider
            characterController.center = Vector3.up * (currentHeight / 2f);
            
            isCrouching = currentHeight < standingHeight - 0.1f;
        }
        
        /// <summary>Controlla se c'è spazio sopra per alzarsi</summary>
        private bool CanStandUp()
        {
            Vector3 rayOrigin = transform.position + Vector3.up * crouchHeight;
            float checkDistance = standingHeight - crouchHeight + 0.2f;
            
            return !Physics.Raycast(rayOrigin, Vector3.up, checkDistance, groundMask);
        }
        
        // ═══════════════════════════════════════════════════════════
        // CAMERA ROTATION
        // ═══════════════════════════════════════════════════════════
        
        private void HandleCameraRotation()
        {
            if (cameraTransform == null)
                return;
            
            // Rotazione orizzontale (Y-axis) - ruota il player
            transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);
            
            // Rotazione verticale (X-axis) - ruota la camera
            cameraPitch -= lookInput.y * mouseSensitivity;
            cameraPitch = Mathf.Clamp(cameraPitch, lookDownLimit, lookUpLimit);
            
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
        }
        
        // ═══════════════════════════════════════════════════════════
        // INPUT CALLBACKS (Unity Input System - Send Messages)
        // ═══════════════════════════════════════════════════════════
        
        // Send Messages behavior richiede metodi con InputValue invece di CallbackContext
        public void OnMove(UnityEngine.InputSystem.InputValue value)
        {
            moveInput = value.Get<Vector2>();
        }
        
        public void OnLook(UnityEngine.InputSystem.InputValue value)
        {
            lookInput = value.Get<Vector2>();
        }
        
        public void OnJump(UnityEngine.InputSystem.InputValue value)
        {
            if (value.isPressed)
                jumpPressed = true;
        }
        
        public void OnSprint(UnityEngine.InputSystem.InputValue value)
        {
            sprintHeld = value.isPressed;
            isSprinting = sprintHeld;
        }
        
        public void OnCrouch(UnityEngine.InputSystem.InputValue value)
        {
            if (value.isPressed)
            {
                crouchHeld = !crouchHeld; // Toggle crouch
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // UTILITY
        // ═══════════════════════════════════════════════════════════
        
        private void ToggleCursor()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // DEBUG
        // ═══════════════════════════════════════════════════════════
        
        private void OnDrawGizmosSelected()
        {
            // Visualizza ground check
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * groundCheckDistance);
        }
        
        // ═══════════════════════════════════════════════════════════
        // PUBLIC GETTERS
        // ═══════════════════════════════════════════════════════════
        
        public bool IsGrounded => isGrounded;
        public bool IsSprinting => isSprinting;
        public bool IsCrouching => isCrouching;
        public float CurrentSpeed => new Vector3(currentVelocity.x, 0, currentVelocity.z).magnitude;
    }
}
