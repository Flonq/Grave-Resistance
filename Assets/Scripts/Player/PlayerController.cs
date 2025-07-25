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
    
    void Awake()
    {
        // Get components
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        
        // Initialize input
        inputActions = new PlayerInputActions();
        
        // Cursor settings
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        inputActions.Player.Aim.performed += OnAim; // Reload için kullanacağız
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
        inputActions.Player.Aim.performed -= OnAim;
        inputActions.Player.Reload.performed -= OnReload;
        inputActions.Player.Pause.performed -= OnPause;
        inputActions.Player.SwitchCamera.performed -= OnSwitchCamera;
    }
    
    void Update()
    {
        HandleMovement();
        HandleLook();
        HandleGravity();
    }
    
    void HandleMovement()
    {
        // Calculate current speed
        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (isRunning)
            currentSpeed = runSpeed;
        else
            currentSpeed = walkSpeed;
        
        // Calculate movement direction
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        
        // Apply movement
        controller.Move(move * currentSpeed * Time.deltaTime);
    }
    
    void HandleLook()
    {
        // Normal FPS look (her zaman çalışır)
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;
        
        // Rotate the player body horizontally
        transform.Rotate(Vector3.up * mouseX);
        
        // Rotate the camera vertically (sadece FPS kamera için)
        if (cameraSwitcher != null && cameraSwitcher.currentMode == CameraSwitcher.CameraMode.FPS)
        {
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -lookDownLimit, lookUpLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        
        // Input'u sıfırla
        lookInput = Vector2.zero;
    }
    
    void HandleGravity()
    {
        // Ground check
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small negative value to stay grounded
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
            // Stand up
            controller.height = standHeight;
            isCrouching = false;
        }
        else
        {
            // Crouch down
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
        Debug.Log("OnReload called!"); // Debug eklendi
        
        if (context.performed && weaponController != null)
        {
            Debug.Log("Calling weaponController.StartReload()"); // Debug eklendi
            weaponController.StartReload();
        }
    }

    void OnAim(InputAction.CallbackContext context)
{
    Debug.Log("OnAim called!"); // Debug eklendi
    
    if (context.performed)
    {
        Debug.Log("Starting aim...");
        // TODO: Implement proper aim system
        // - FOV change for zoom
        // - Crosshair change
        // - Movement speed reduction
    }
    else if (context.canceled)
    {
        Debug.Log("Stopping aim...");
        // TODO: Return to normal view
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
} 