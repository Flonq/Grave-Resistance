using UnityEngine;
using UnityEngine.InputSystem;
// YENİ: PlayerInputActions için using static ekle
using static PlayerInputActions;

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
    
    [Header("Slide Settings")]
    public float slideSpeed = 12f; // Slide hızı
    public float slideDuration = 1f; // Slide süresi
    public float slideHeight = 0.5f; // Slide sırasında yükseklik
    public float slideSmoothSpeed = 5f; // Slide geçiş hızı

    [Header("Crouch Settings")]
    public float crouchHeight = 1f;
    public float standHeight = 2f;
    public float crouchSpeed = 1.5f;
    public float crouchSmoothSpeed = 2f; // YENİ: 0.5f'den 2f'e çıkardık - daha hızlı eğilme

    // YENİ: Orijinal değerleri kaydet - SİL
    // private float originalYPosition;
    // private Vector3 originalCenter;
    
    // YENİ: Orijinal pozisyonu kaydet
    // private float originalYPosition;
    
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
    public Animator characterAnimator;
    
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
    
    // Slide variables - BUNLARI SİL
    // private bool isSliding = false;
    // private float slideTimer = 0f;
    // private float originalHeight;
    // private float originalFOV;
    // private Vector3 slideDirection;
    
    // Components
    private CharacterController controller;
    private PlayerInputActions inputActions;
    private Camera playerCamera;
    
    // Movement variables
    private Vector3 velocity;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isRunning = false;
    private bool isCrouching = false; // YENİ: Eğilme durumu
    private bool isSliding = false; // YENİ: Slide durumu
    private float currentSpeed;
    private float xRotation = 0f;
    
    // YENİ: Eğilme değişkenleri
    private float targetHeight;
    private float currentHeight;
    private Vector3 targetCenter;
    private Vector3 currentCenter;
    private float originalHeight; // YENİ: Orijinal yüksekliği kaydet
    private Vector3 originalCenter; // YENİ: Orijinal center'ı kaydet
    
    // Slide değişkenleri
    private float slideTimer = 0f;
    private Vector3 slideDirection = Vector3.zero;
    private float slideTargetHeight;
    private Vector3 slideTargetCenter;
    
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
            characterAnimator = GetComponentInChildren<Animator>();
        
        // Eğer yoksa oluştur
        if (characterAnimator == null)
            characterAnimator = gameObject.AddComponent<Animator>();
        
        // YENİ: FOV ayarları
        if (playerCamera != null)
        {
            currentFOV = normalFOV;
            targetFOV = normalFOV;
            playerCamera.fieldOfView = normalFOV;
        }
        
        currentSensitivity = normalSensitivity;
        
        // YENİ: Eğilme sistemini başlat
        InitializeCrouchSystem();
    }

    // YENİ: Eğilme sistemini başlat
    void InitializeCrouchSystem()
    {
        if (controller != null)
        {
            // YENİ: Orijinal değerleri kaydet
            originalHeight = controller.height;
            originalCenter = controller.center;
            
            // Başlangıç değerleri - Orijinal değerleri kullan
            targetHeight = originalHeight;
            currentHeight = originalHeight;
            targetCenter = originalCenter;
            currentCenter = originalCenter;
            
            // Eğilme durumunu sıfırla
            isCrouching = false;
            
        }
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
            inputActions.Player.Crouch.performed += OnCrouch; // YENİ: Eğilme input'u
        inputActions.Player.Fire.performed += OnFire;
        inputActions.Player.Reload.performed += OnReload;
        inputActions.Player.Pause.performed += OnPause;
        inputActions.Player.SwitchCamera.performed += OnSwitchCamera;
        inputActions.Player.Slide.performed += OnSlide; // YENİ: Slide input'u
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
            inputActions.Player.Crouch.performed -= OnCrouch; // YENİ: Eğilme input'u
        inputActions.Player.Fire.performed -= OnFire;
        inputActions.Player.Reload.performed -= OnReload;
        inputActions.Player.Pause.performed -= OnPause;
        inputActions.Player.SwitchCamera.performed -= OnSwitchCamera;
        inputActions.Player.Slide.performed -= OnSlide; // YENİ: Slide input'u
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
        HandleFPSAim();
        HandleRecoilRecovery();
        HandleCrouch(); // YENİ: Eğilme işlemi
        HandleSlide(); // YENİ: Slide işlemi
        
        // Animasyon parametrelerini güncelle
        UpdateAnimations();
    }
    
    // YENİ METOD: Manuel aim kontrolü
    void HandleAiming()
    {
        bool currentlyAiming = Mouse.current.rightButton.isPressed;
        
        if (currentlyAiming != wasAiming)
        {
            isAiming = currentlyAiming;
            
            if (tpsCamera != null)
            {
                tpsCamera.SetAiming(isAiming);
            }
            
            
            wasAiming = currentlyAiming;
        }
    }
    
    // Crouch ve Slide sırasında da normal animasyonlar çalışsın
    // Sadece hızı azalt
    void HandleMovement()
    {
        // Calculate current speed
        if (isSliding)
        {
            currentSpeed = slideSpeed;
        }
        else if (isCrouching)
        {
            currentSpeed = crouchSpeed;
        }
        else if (isRunning && !isAiming)
        {
            currentSpeed = runSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }
        
        // Aim alırken yavaşla
        if (isAiming && !isSliding)
            currentSpeed *= 0.5f;
        
        // Calculate movement direction
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        
        // Apply movement (sadece bizim input sistemimiz)
        if (!isSliding)
        {
            controller.Move(move * currentSpeed * Time.deltaTime);
        }
        
        // Hareket kesilince koşmayı durdur
        if (moveInput.magnitude < 0.1f)
        {
            isRunning = false;
        }
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
                }
                else
                {
                    // Normal moda geç
                    targetFOV = normalFOV;
                    currentSensitivity = normalSensitivity;
                    crosshair.SetActive(true);
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
            float randomHorizontal = Random.Range(-horizontalRecoil, horizontalRecoil);
            
            Vector2 newRecoil = new Vector2(
                randomHorizontal * horizontalRecoilMultiplier * 50f,
                verticalRecoil * recoilMultiplier * 100f
            );
            
            recoilStartPosition = currentRecoil;
            targetRecoil = currentRecoil + newRecoil;
            targetRecoil.y = Mathf.Clamp(targetRecoil.y, 0f, maxRecoilY);
            targetRecoil.x = Mathf.Clamp(targetRecoil.x, -maxRecoilX, maxRecoilX);
            
            recoilTimer = 0f;
            isRecoiling = true;
            isInRecoveryPhase = false;
            
        }
    }

    void HandleRecoilRecovery()
    {
        if (isRecoiling)
        {
            recoilTimer += Time.deltaTime;
            
            if (!isInRecoveryPhase)
            {
                float normalizedTime = recoilTimer / recoilDuration;
                if (normalizedTime >= 1f)
                {
                    isInRecoveryPhase = true;
                    recoilStartPosition = currentRecoil;
                    recoilTimer = 0f;
                }
                else
                {
                    float curveValue = recoilCurve.Evaluate(normalizedTime);
                    currentRecoil = Vector2.Lerp(recoilStartPosition, targetRecoil, curveValue);
                }
            }
            else
            {
                // Negatif recoil
                float recoveryTime = recoilTimer / (recoilDuration * 2f);
                Vector2 oldRecoil = currentRecoil;
                
                Vector2 negativeRecoil = new Vector2(0f, -targetRecoil.y * recoveryStrength);
                currentRecoil = Vector2.Lerp(recoilStartPosition, negativeRecoil, recoveryTime);
                
                if (recoveryTime >= 1f)
                {
                    currentRecoil = Vector2.zero;
                    isRecoiling = false;
                    isInRecoveryPhase = false;
                }
            }
        }
    }

    // YENİ: Slide işlemi
    void HandleSlide()
    {
        if (isSliding)
        {
            slideTimer += Time.deltaTime;
            
            // Slide süresi doldu mu?
            if (slideTimer >= slideDuration)
            {
                EndSlide();
            }
            else
            {
                // Slide sırasında hareket
                Vector3 slideMovement = slideDirection * slideSpeed * Time.deltaTime;
                controller.Move(slideMovement);
            }
        }
    }

    // YENİ: Slide başlat
    void StartSlide()
    {
        if (!isRunning || isSliding || isCrouching) return;
        
        isSliding = true;
        slideTimer = 0f;
        
        // Slide yönünü belirle (hareket yönü veya bakış yönü)
        if (moveInput.magnitude > 0.1f)
        {
            slideDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
        }
        else
        {
            slideDirection = transform.forward;
        }
        
        // Slide yüksekliğini ayarla
        slideTargetHeight = slideHeight;
        slideTargetCenter = new Vector3(0, slideHeight / 2, 0);
        
        Debug.Log("Slide başladı!");
    }

    // YENİ: Slide bitir - GÜNCELLENDİ
    void EndSlide()
    {
        isSliding = false;
        slideTimer = 0f;
        
        // YENİ: Slide bittikten sonra koşmayı durdur
        isRunning = false;
        
        // Normal yüksekliğe dön
        slideTargetHeight = originalHeight;
        slideTargetCenter = originalCenter;
        
        Debug.Log("Slide bitti! Koşma durduruldu.");
    }

    // BUNLARI SİL
    // void HandleSlide() { ... }
    // void StartSlide() { ... }
    // void EndSlide() { ... }
    
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
        // YENİ: Eğiliyken koşma başlatma
        if (context.performed && !isCrouching && !isSliding)
        {
            isRunning = !isRunning; // Toggle koşma durumu
        }
    }
    
    void OnRunEnd(InputAction.CallbackContext context)
    {
        // YENİ: Bu fonksiyonu kullanmıyoruz (toggle sistemi için)
        // Boş bırak veya sil
    }
    
    // BUNLARI SİL
    // void OnCrouch(InputAction.CallbackContext context) { ... }
    // void ToggleCrouch() { ... }
    // System.Collections.IEnumerator FixPositionAfterFrame() { ... }

    // YENİ: Eğilme input event - GÜNCELLENDİ
    void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // YENİ: Koşarken Ctrl basılırsa slide yap
            if (isRunning && !isSliding && !isCrouching)
            {
                StartSlide();
            }
            // YENİ: Koşmazken Ctrl basılırsa eğilme yap
            else if (!isRunning && !isSliding)
            {
                ToggleCrouch();
            }
        }
    }
    
    // YENİ: Eğilme toggle (güncellendi)
    void ToggleCrouch()
    {
        // Slide sırasında eğilme yapma
        if (isSliding) return;
        
        isCrouching = !isCrouching;
        
        if (isCrouching)
        {
            // Eğilme moduna geç
            targetHeight = crouchHeight;
            targetCenter = new Vector3(0, crouchHeight / 2, 0);
        }
        else
        {
            // Normal moda geç
            targetHeight = originalHeight;
            targetCenter = originalCenter;
        }
    }

    // YENİ: Eğilme işlemi (güncellendi)
    void HandleCrouch()
    {
        if (controller == null) return;
        
        // Slide sırasında farklı hedef değerler kullan
        float targetHeight = isSliding ? slideTargetHeight : this.targetHeight;
        Vector3 targetCenter = isSliding ? slideTargetCenter : this.targetCenter;
        
        // YENİ: Smooth geçiş yerine daha stabil bir yaklaşım
        float heightDifference = Mathf.Abs(currentHeight - targetHeight);
        float centerDifference = Vector3.Distance(currentCenter, targetCenter);
        
        // Eğer hedef değerlere çok yakınsa, direkt ata
        if (heightDifference < 0.01f && centerDifference < 0.01f)
        {
            currentHeight = targetHeight;
            currentCenter = targetCenter;
        }
        else
        {
            // YENİ: Smooth geçiş - doğrudan crouchSmoothSpeed kullan
            float smoothSpeed = isSliding ? slideSmoothSpeed : crouchSmoothSpeed;
            currentHeight = Mathf.MoveTowards(currentHeight, targetHeight, smoothSpeed * Time.deltaTime);
            currentCenter = Vector3.MoveTowards(currentCenter, targetCenter, smoothSpeed * Time.deltaTime);
        }
        
        // Controller'ı güncelle
        controller.height = currentHeight;
        controller.center = currentCenter;
    }

    // YENİ: Karakter modelinin scale'ini düzelt
    void FixCharacterModelScale(bool isNormalSize)
    {
        // Farklı olası yolları dene
        Transform[] possibleModels = {
            transform.Find("Player"),
            transform.Find("Character"),
            transform.Find("Model"),
            transform.Find("Body"),
            transform.GetChild(0) // İlk child
        };
        
        foreach (Transform model in possibleModels)
        {
            if (model != null)
            {
                if (isNormalSize)
                {
                    model.localScale = Vector3.one;
                }
                else
                {
                    model.localScale = new Vector3(1f, 0.7f, 1f);
                }
            }
        }
        
        // YENİ: Tüm child'ları da kontrol et
        foreach (Transform child in transform)
        {
            if (child.GetComponent<Renderer>() != null || child.GetComponent<SkinnedMeshRenderer>() != null)
            {
                if (isNormalSize)
                {
                    child.localScale = Vector3.one;
        }
        else
        {
                    child.localScale = new Vector3(1f, 0.7f, 1f);
                }
            }
        }
    }

    // YENİ: OnFire fonksiyonunu geri ekle
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
        }
    }
    
    // YENİ: Slide input event - SİL veya boş bırak
    void OnSlide(InputAction.CallbackContext context)
    {
        // YENİ: Bu fonksiyonu artık kullanmıyoruz
        // Slide işlemi OnCrouch içinde yapılıyor
    }
    
    // YENİ: Bir frame sonra pozisyonu düzelt
    // System.Collections.IEnumerator FixPositionAfterFrame()
    // {
    //     yield return new WaitForEndOfFrame();
        
    //     Vector3 newPosition = transform.position;
    //     newPosition.y = originalYPosition;
    //     transform.position = newPosition;
        
    //     Debug.Log($"Pozisyon düzeltildi: {transform.position.y}");
    // }
    
    // Public getters
    public bool IsAiming() => isAiming;
    public bool IsAimingFPS() => isAimingFPS;
    public bool IsCrouching() => isCrouching;
    public bool IsSliding() => isSliding;

    void UpdateAnimations()
    {
        if (characterAnimator == null) return;
        
        // Hareket hızı
        float speed = new Vector2(moveInput.x, moveInput.y).magnitude;
        
        // Animasyonlar
        characterAnimator.SetFloat("Speed", speed);
        characterAnimator.SetBool("IsRunning", isRunning);
        characterAnimator.SetBool("IsGrounded", controller.isGrounded);
        characterAnimator.SetBool("IsJumping", !controller.isGrounded && velocity.y > 0);
        
        Debug.Log($"Speed: {speed}, Running: {isRunning}, Grounded: {controller.isGrounded}, Jumping: {!controller.isGrounded && velocity.y > 0}");
    }
} 