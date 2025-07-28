using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    
    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public float lookUpLimit = 80f;
    public float lookDownLimit = 80f;
    
    [Header("Crouch Settings")]
    public float crouchHeight = 1f;
    public float standHeight = 2f;
    public float crouchSpeed = 2.5f;
    
    [Header("Combat")]
    public WeaponController weaponController;
    
    [Header("Camera")]
    public CameraSwitcher cameraSwitcher;
    public GTAOrbitCamera tpsCamera;
    
    [Header("TPS Settings")]
    public float characterRotationSpeed = 10f;
    
    [Header("Body Rotation")]
    public Transform upperBody;
    public Transform lowerBody;
    public float bodyRotationSpeed = 5f;
    public float maxBodyAngle = 45f;
    
    [Header("Character Animation")]
    public CharacterAnimator characterAnimator;
    
    // Components
    private CharacterController controller;
    private PlayerInputActions inputActions;
    private Camera playerCamera;
    
    // Movement variables
    private Vector3 velocity;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isRunning;
    private bool isCrouching;
    private float currentSpeed;
    private float xRotation = 0f;
    
    // TPS Aiming
    private bool isAiming = false;
    private bool wasAiming = false; // Önceki frame'de aim alıyor muydu
    
    void Awake()
    {
        // Get components
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        
        // TPS kamera bul
        if (tpsCamera == null)
            tpsCamera = FindFirstObjectByType<GTAOrbitCamera>();
        
        // Initialize input
        inputActions = new PlayerInputActions();
        
        // Cursor settings
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void Start()
    {
        // Character animator'ı otomatik bul
        if (characterAnimator == null)
            characterAnimator = GetComponent<CharacterAnimator>();
        
        // Eğer yoksa oluştur
        if (characterAnimator == null)
            characterAnimator = gameObject.AddComponent<CharacterAnimator>();
    }
    
    void OnEnable()
    {
        inputActions.Player.Enable();
        
        // Subscribe to input events
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Look.performed += OnLook;
        inputActions.Player.Jump.performed += OnJump;
        inputActions.Player.Run.performed += OnRun;
        inputActions.Player.Run.canceled += OnRunEnd;
        inputActions.Player.Crouch.performed += OnCrouch;
        inputActions.Player.Fire.performed += OnFire;
        // Aim event'lerini kaldırıyoruz - manuel kontrol yapacağız
        inputActions.Player.Reload.performed += OnReload;
        inputActions.Player.Pause.performed += OnPause;
        inputActions.Player.SwitchCamera.performed += OnSwitchCamera;
    }
    
    void OnDisable()
    {
        inputActions.Player.Disable();
        
        // Unsubscribe from input events
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Look.performed -= OnLook;
        inputActions.Player.Jump.performed -= OnJump;
        inputActions.Player.Run.performed -= OnRun;
        inputActions.Player.Run.canceled -= OnRunEnd;
        inputActions.Player.Crouch.performed -= OnCrouch;
        inputActions.Player.Fire.performed -= OnFire;
        inputActions.Player.Reload.performed -= OnReload;
        inputActions.Player.Pause.performed -= OnPause;
        inputActions.Player.SwitchCamera.performed -= OnSwitchCamera;
    }
    
    void Update()
    {
        HandleMovement();
        HandleLook();
        HandleGravity();
        HandleTPSRotation();
        HandleAiming(); // YENİ: Manuel aim kontrolü
    }
    
    // YENİ METOD: Manuel aim kontrolü
    void HandleAiming()
    {
        // Sağ tık durumunu kontrol et
        bool currentlyAiming = Mouse.current.rightButton.isPressed;
        
        // Durum değişti mi?
        if (currentlyAiming != wasAiming)
        {
            isAiming = currentlyAiming;
            
            if (tpsCamera != null)
            {
                tpsCamera.SetAiming(isAiming);
            }
            
            Debug.Log($"Aim mode changed: {(isAiming ? "ON" : "OFF")}");
            
            wasAiming = currentlyAiming;
        }
    }
    
    void HandleMovement()
    {
        // Calculate current speed
        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (isRunning && !isAiming)
            currentSpeed = runSpeed;
        else
            currentSpeed = walkSpeed;
        
        // Aim alırken yavaşla
        if (isAiming)
            currentSpeed *= 0.5f;
        
        // Calculate movement direction
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        
        // Apply movement
        controller.Move(move * currentSpeed * Time.deltaTime);
    }
    
    void HandleLook()
    {
        // Normal FPS look (sadece FPS modunda)
        if (cameraSwitcher != null && cameraSwitcher.currentMode == CameraSwitcher.CameraMode.FPS)
        {
            float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
            float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;
            
            // Rotate the player body horizontally
            transform.Rotate(Vector3.up * mouseX);
            
            // Rotate the camera vertically
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -lookDownLimit, lookUpLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        
        // Input'u sıfırla
        lookInput = Vector2.zero;
    }
    
    void HandleTPSRotation()
    {
        if (cameraSwitcher != null && cameraSwitcher.currentMode == CameraSwitcher.CameraMode.TPS && tpsCamera != null)
        {
            if (moveInput.magnitude > 0.1f || isAiming)
            {
                Vector3 cameraForward = tpsCamera.transform.forward;
                cameraForward.y = 0;
                cameraForward = cameraForward.normalized;

                if (cameraForward.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(cameraForward);

                    // Dönüş hızları (derece/saniye)
                    float fireTurnSpeed = 360f; // Ateş sırasında çok hızlı döner
                    float normalTurnSpeed = 120f; // Normalde daha yavaş döner

                    float turnSpeed = Mouse.current.leftButton.isPressed ? fireTurnSpeed : normalTurnSpeed;

                    // RotateTowards ile yumuşak ve kontrollü dönüş
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, characterRotationSpeed * Time.deltaTime);
                }
            }
        }
    }
    
    void HandleGravity()
    {
        // Ground check
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    
    // Input Event Handlers
    void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
    
    void OnJump(InputAction.CallbackContext context)
    {
        if (controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
    
    void OnRun(InputAction.CallbackContext context)
    {
        isRunning = true;
    }
    
    void OnRunEnd(InputAction.CallbackContext context)
    {
        isRunning = false;
    }
    
    void OnCrouch(InputAction.CallbackContext context)
    {
        ToggleCrouch();
    }
    
    void ToggleCrouch()
    {
        if (isCrouching)
        {
            controller.height = standHeight;
            isCrouching = false;
        }
        else
        {
            controller.height = crouchHeight;
            isCrouching = true;
        }
    }

    void OnFire(InputAction.CallbackContext context)
    {
        if (context.performed && weaponController != null)
        {
            weaponController.Shoot();
        }
    }

    void OnReload(InputAction.CallbackContext context)
    {
        if (context.performed && weaponController != null)
        {
            weaponController.StartReload();
        }
    }

    void OnPause(InputAction.CallbackContext context)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
        }
    }

    void OnSwitchCamera(InputAction.CallbackContext context)
    {
        if (cameraSwitcher != null)
        {
            cameraSwitcher.ToggleCameraMode();
            Debug.Log($"Camera switched to: {cameraSwitcher.currentMode}");
        }
    }
    
    // Public getters
    public bool IsAiming() => isAiming;
} 