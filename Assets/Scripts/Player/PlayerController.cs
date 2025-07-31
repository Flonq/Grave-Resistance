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
    public float recoilMultiplier = 4f;
    public float horizontalRecoilMultiplier = 2f;
    public float maxRecoilY = 30f;
    public float maxRecoilX = 15f;
    public float recoveryStrength = 0.3f; // YENİ: Recovery gücü (0.3f = %30)
    
    [Header("Recoil Curve Settings")]
    public AnimationCurve recoilCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Inspector'da ayarlanabilir
    public float recoilDuration = 0.3f; // Recoil'in tamamlanma süresi
    
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
    private float recoilTimer = 0f; // YENİ: Recoil timer
    private Vector2 recoilStartPosition; // YENİ: Başlangıç pozisyonu
    private bool isInRecoveryPhase = false; // YENİ: Recovery fazında mı?
    
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
            
            // YENİ: Recoil değerlerini çok daha büyük yap
            Vector2 newRecoil = new Vector2(
                randomHorizontal * horizontalRecoilMultiplier * 50f, // 50f çarpanı
                verticalRecoil * recoilMultiplier * 100f // 100f çarpanı
            );
            
            // Recoil'i başlat
            recoilStartPosition = currentRecoil;
            targetRecoil = currentRecoil + newRecoil;
            targetRecoil.y = Mathf.Clamp(targetRecoil.y, 0f, maxRecoilY);
            targetRecoil.x = Mathf.Clamp(targetRecoil.x, -maxRecoilX, maxRecoilX);
            
            // Timer'ı sıfırla ve recovery fazını kapat
            recoilTimer = 0f;
            isRecoiling = true;
            isInRecoveryPhase = false;
            
            Debug.Log($"Recoil başladı! Target: {targetRecoil}");
        }
    }

    void HandleRecoilRecovery()
    {
        if (isRecoiling)
        {
            recoilTimer += Time.deltaTime;
            
            if (!isInRecoveryPhase)
            {
                // İlk recoil fazı - yukarı gidiyor
                float normalizedTime = recoilTimer / recoilDuration;
                if (normalizedTime >= 1f)
                {
                    // İlk recoil bitti, recovery fazına geç
                    isInRecoveryPhase = true;
                    recoilStartPosition = currentRecoil;
                    recoilTimer = 0f;
                    Debug.Log($"Recovery başladı! Start: {recoilStartPosition}");
                }
                else
                {
                    float curveValue = recoilCurve.Evaluate(normalizedTime);
                    currentRecoil = Vector2.Lerp(recoilStartPosition, targetRecoil, curveValue);
                }
            }
            else
            {
                // Recovery fazı - negatif recoil uygula
                float recoveryTime = recoilTimer / (recoilDuration * 2f);
                Vector2 oldRecoil = currentRecoil;
                
                // YENİ: Negatif recoil değerini ayarlanabilir yap
                Vector2 negativeRecoil = new Vector2(0f, -targetRecoil.y * recoveryStrength); // 0.3f = recovery gücü
                currentRecoil = Vector2.Lerp(recoilStartPosition, negativeRecoil, recoveryTime);
                
                if (recoveryTime >= 1f)
                {
                    currentRecoil = Vector2.zero;
                    isRecoiling = false;
                    isInRecoveryPhase = false;
                    Debug.Log("Recovery bitti!");
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
    public bool IsAimingFPS() => isAimingFPS;
} 