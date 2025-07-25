using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// GTA SA tarzı orbital camera sistemi
/// </summary>
public class GTAOrbitCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField]
    private FocusPoint _target;

    [Header("Camera Settings")]
    [SerializeField]
    private float _distance = 3f;
    [SerializeField]
    private float _damping = 5f;
    [SerializeField]
    private float _mouseSensitivity = 2f;

    [Header("State Settings")]
    public CameraState currentState = CameraState.Idle;
    public enum CameraState { Idle, Moving, Aiming }

    // Camera angles
    private Quaternion _pitch;
    private Quaternion _yaw;

    // Target values
    private Quaternion _targetRotation;
    private Vector3 _targetPosition;

    // Input detection
    private bool _isMoving = false;
    private bool _isAiming = false;

    // Player reference
    private Transform _playerTransform;

    // YENİ AÇI DEĞİŞKENLERİ EKLE:
    private float horizontalAngle = 0f;
    private float verticalAngle = 20f;

    public FocusPoint Target
    {
        get { return _target; }
        set { _target = value; }
    }

    public float Yaw
    {
        get { return _yaw.eulerAngles.y; }
        private set { _yaw = Quaternion.Euler(0, value, 0); }
    }

    public float Pitch
    {
        get { return _pitch.eulerAngles.x; }
        private set { _pitch = Quaternion.Euler(value, 0, 0); }
    }

    public void HandleInput(Vector2 mouseInput, bool movementInput, bool aimInput)
    {
        _isMoving = movementInput;
        _isAiming = aimInput;

        // State determination
        CameraState newState = DetermineState();
        if (newState != currentState)
        {
            SetState(newState);
        }

        // Mouse input handling
        Move(mouseInput.x * _mouseSensitivity, mouseInput.y * _mouseSensitivity);
    }

    private CameraState DetermineState()
    {
        if (_isAiming) return CameraState.Aiming;
        if (_isMoving) return CameraState.Moving;
        return CameraState.Idle;
    }

    private void SetState(CameraState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case CameraState.Idle:
                // Idle state - orbital freedom
                break;
            case CameraState.Moving:
                // Player rotation to camera direction
                if (_playerTransform != null)
                {
                    float targetYaw = _yaw.eulerAngles.y;
                    _playerTransform.rotation = Quaternion.Euler(0, targetYaw, 0);
                }
                break;
            case CameraState.Aiming:
                // Aim mode - closer camera, precision
                break;
        }
    }

    public void Move(float yawDelta, float pitchDelta)
    {
        _yaw = _yaw * Quaternion.Euler(0, yawDelta, 0);
        _pitch = _pitch * Quaternion.Euler(-pitchDelta, 0, 0); // Negative for natural mouse feel

        ApplyConstraints();
    }

    private void ApplyConstraints()
    {
        if (_target == null) return;

        Quaternion targetYaw = Quaternion.Euler(0, _target.transform.rotation.eulerAngles.y, 0);
        Quaternion targetPitch = Quaternion.Euler(_target.transform.rotation.eulerAngles.x, 0, 0);

        float yawDifference = Quaternion.Angle(_yaw, targetYaw);
        float pitchDifference = Quaternion.Angle(_pitch, targetPitch);

        float yawOverflow = yawDifference - _target.YawLimit;
        float pitchOverflow = pitchDifference - _target.PitchLimit;

        if (yawOverflow > 0) { _yaw = Quaternion.Slerp(_yaw, targetYaw, yawOverflow / yawDifference); }
        if (pitchOverflow > 0) { _pitch = Quaternion.Slerp(_pitch, targetPitch, pitchOverflow / pitchDifference); }
    }

    void Awake()
    {
        // Initialize angles to current rotation
        _pitch = Quaternion.Euler(this.transform.rotation.eulerAngles.x, 0, 0);
        _yaw = Quaternion.Euler(0, this.transform.rotation.eulerAngles.y, 0);

        // Get player reference
        if (_target != null)
        {
            _playerTransform = _target.transform;
        }
    }

    void LateUpdate()
    {
        if (_target == null) return;

        // Calculate target rotation and position
        _targetRotation = _yaw * _pitch;
        _targetPosition = _target.transform.position + _targetRotation * (-Vector3.forward * _distance);

        // Apply smooth damping
        this.transform.rotation = Quaternion.Lerp(this.transform.rotation, _targetRotation, 
            Mathf.Clamp01(Time.smoothDeltaTime * _damping));

        // Position camera at distance from target
        Vector3 offset = this.transform.rotation * (-Vector3.forward * _distance);
        this.transform.position = _target.transform.position + offset;
    }

    // Public methods for external control
    public bool IsIdle() => currentState == CameraState.Idle;
    public bool IsMoving() => currentState == CameraState.Moving;
    public bool IsAiming() => currentState == CameraState.Aiming;

    // Update method'unda mouse input var mı?
    void Update()
    {
        // Mouse input alma
        Vector2 mouseInput = Mouse.current.delta.ReadValue();
        
        // ORBITAL LOGIC:
        if (_target != null)
        {
            // Mouse input'u açılara çevir
            horizontalAngle += mouseInput.x * _mouseSensitivity * Time.deltaTime;
            verticalAngle -= mouseInput.y * _mouseSensitivity * Time.deltaTime;
            
            // Dikey açıyı sınırla
            verticalAngle = Mathf.Clamp(verticalAngle, -30f, 60f);
            
            // Target pozisyonu
            Vector3 targetPos = _target.transform.position;
            
            // Kamera pozisyonunu hesapla
            Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);
            Vector3 direction = rotation * Vector3.back;
            Vector3 cameraPosition = targetPos + direction * _distance;
            
            // Pozisyonu ve rotasyonu uygula
            transform.position = Vector3.Lerp(transform.position, cameraPosition, _damping * Time.deltaTime);
            transform.LookAt(targetPos);
        }
    }
}
