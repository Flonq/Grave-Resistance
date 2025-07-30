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
    
    [Header("FPS Aim Settings")]
    public float aimZoomFOV = 30f; // Nişan alırken FOV
    public float normalFOV = 60f; // Normal FOV
    public float aimSmoothSpeed = 8f; // Zoom geçiş hızı
    public float aimSensitivity = 5f; // Nişan alırken mouse hassasiyeti (normalden düşük)
    public float normalSensitivity = 10f; // Normal mouse hassasiyeti
    
    [Header("TPS Camera Settings")]
    public float tpsSensitivity = 2f; // TPS kamera için ayrı hassasiyet
    
    [Header("UI References")]
    public GameObject crosshair;
    
    [Header("Recoil Settings")]
    public float recoilMultiplier = 4f;        // Recoil çarpanı
    public float horizontalRecoilMultiplier = 2f; // Horizontal recoil çarpanı
    public float maxRecoilY = 30f;             // Maximum vertical recoil
    public float maxRecoilX = 15f;             // Maximum horizontal recoil
    public float recoverySpeed = 4f;           // Recovery hızı
    public float minRecoverySpeed = 1f;        // Minimum recovery hızı
    public float maxRecoverySpeed = 8f;        // Maximum recovery hızı
    
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
    
    private bool isAimingFPS = false;
    private float currentFOV;
    private float targetFOV;
    private float currentSensitivity;
    
    // Recoil variables
    private Vector2 currentRecoil = Vector2.zero;
    private Vector2 targetRecoil = Vector2.zero;
    private bool isRecoiling = false;
    
    void Awake()
    {
        // Get components
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        
        // TPS kamera bul
        if (tpsCamera == null)
            tpsCamera = FindFirstObjectByType<GTAOrbitCamera>();
        
        // Initialize input - null check ekle
        try
        {
            inputActions = new PlayerInputActions();
            Debug.Log("PlayerInputActions initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize PlayerInputActions: {e.Message}");
            inputActions = null;
        }
        
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
        
        // YENİ: FOV ayarları
        if (playerCamera != null)
        {
            currentFOV = normalFOV;
            targetFOV = normalFOV;
            playerCamera.fieldOfView = normalFOV;
        }
        
        currentSensitivity = normalSensitivity;
    }
    
    void OnEnable()
    {
        // Input actions'ı kontrol et
        if (inputActions != null)
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
            inputActions.Player.Reload.performed += OnReload;
            inputActions.Player.Pause.performed += OnPause;
            inputActions.Player.SwitchCamera.performed += OnSwitchCamera;
        }
        else
        {
            Debug.LogWarning("InputActions is null in PlayerController.OnEnable()");
        }
    }
    
    void OnDisable()
    {
        // Input actions'ı kontrol et
        if (inputActions != null)
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
        else
        {
            Debug.LogWarning("InputActions is null in PlayerController.OnDisable()");
        }
    }
    
    void Update()
    {
        HandleMovement();
        HandleLook();
        HandleGravity();
        HandleTPSRotation();
        HandleAiming();
        HandleFPSAim(); // YENİ: FPS aim kontrolü
        HandleRecoilRecovery(); // YENİ: Recoil kurtarma
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
            float mouseX = lookInput.x * currentSensitivity * Time.deltaTime;
            float mouseY = lookInput.y * currentSensitivity * Time.deltaTime;
            
            // Recoil'i ekle
            mouseX += currentRecoil.x * Time.deltaTime;
            mouseY += currentRecoil.y * Time.deltaTime;
            
            // Rotate the player body horizontally
            transform.Rotate(Vector3.up * mouseX);
            
            // Rotate the camera vertically
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -lookDownLimit, lookUpLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        else
        {
            // TPS modunda TPS hassasiyetini kullan
            float mouseX = lookInput.x * tpsSensitivity * Time.deltaTime;
            float mouseY = lookInput.y * tpsSensitivity * Time.deltaTime;
            
            // TPS kamera rotasyonu (GTAOrbitCamera zaten kendi hassasiyetini kullanıyor)
            // Burada sadece player rotasyonu için kullan
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
    
    // YENİ: FPS Aim kontrolü
    void HandleFPSAim()
    {
        // Sadece FPS modunda çalış
        if (cameraSwitcher != null && cameraSwitcher.currentMode == CameraSwitcher.CameraMode.FPS)
        {
            // Sağ tık ile aim
            bool aimInput = Mouse.current.rightButton.isPressed;
            
            if (aimInput != isAimingFPS)
            {
                isAimingFPS = aimInput;
                
                if (isAimingFPS)
                {
                    // Aim moduna geç
                    targetFOV = aimZoomFOV;
                    currentSensitivity = aimSensitivity;
                    crosshair.SetActive(false);
                    Debug.Log("FPS Aim ON - Sensitivity: " + currentSensitivity);
                }
                else
                {
                    // Normal moda geç
                    targetFOV = normalFOV;
                    currentSensitivity = normalSensitivity;
                    crosshair.SetActive(true);
                    Debug.Log("FPS Aim OFF - Sensitivity: " + currentSensitivity);
                }
            }
            
            // FOV'u smooth olarak güncelle
            if (playerCamera != null)
            {
                currentFOV = Mathf.Lerp(currentFOV, targetFOV, aimSmoothSpeed * Time.deltaTime);
                playerCamera.fieldOfView = currentFOV;
            }
        }
        else
        {
            // TPS modunda normal hassasiyet
            currentSensitivity = normalSensitivity;
            
            // TPS modunda FOV'u sabit tut
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = normalFOV;
            }
        }
    }

    // Recoil sistemi
    public void ApplyRecoil(float verticalRecoil, float horizontalRecoil)
    {
        if (cameraSwitcher != null && cameraSwitcher.currentMode == CameraSwitcher.CameraMode.FPS)
        {
            // Rastgele horizontal recoil
            float randomHorizontal = Random.Range(-horizontalRecoil, horizontalRecoil);
            
            // Recoil'i ekle (direkt ata değil, ekle)
            currentRecoil.y += verticalRecoil * recoilMultiplier;
            currentRecoil.x += randomHorizontal * horizontalRecoilMultiplier;
            
            // Maximum recoil'i sınırla
            currentRecoil.y = Mathf.Clamp(currentRecoil.y, 0f, maxRecoilY);
            currentRecoil.x = Mathf.Clamp(currentRecoil.x, -maxRecoilX, maxRecoilX);
            
            isRecoiling = true;
        }
    }

    void HandleRecoilRecovery()
    {
        if (isRecoiling && currentRecoil.magnitude > 0.1f)
        {
            // Parabolik smooth - başta yavaş, sonra hızlı
            float currentRecoverySpeed = Mathf.Lerp(minRecoverySpeed, maxRecoverySpeed, currentRecoil.magnitude / maxRecoilY);
            currentRecoil = Vector2.Lerp(currentRecoil, Vector2.zero, currentRecoverySpeed * Time.deltaTime);
        }
        else
        {
            currentRecoil = Vector2.zero;
            isRecoiling = false;
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
    public bool IsAimingFPS() => isAimingFPS;
} 