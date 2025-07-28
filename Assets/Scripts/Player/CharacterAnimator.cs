using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    [Header("Body Parts")]
    public Transform upperBody; // Üst gövde (kapsülün üst yarısı)
    public Transform lowerBody; // Alt gövde (kapsülün alt yarısı)
    public Transform head;      // Baş (kamera)
    
    [Header("Rotation Settings")]
    public float upperBodyRotationSpeed = 5f;
    public float headRotationSpeed = 8f;
    public float maxUpperBodyAngle = 45f;
    public float maxHeadAngle = 80f;
    
    [Header("Smooth Settings")]
    public float returnSpeed = 2f;
    public float rotationSmoothness = 5f;
    
    // Private variables
    private float currentUpperBodyRotation = 0f;
    private float currentHeadRotation = 0f;
    private Vector2 lookInput;
    private bool isInitialized = false;
    
    void Start()
    {
        // Kapsülü iki parçaya böl
        SetupBodyParts();
        isInitialized = true;
    }
    
    void SetupBodyParts()
    {
        // Kapsülün üst yarısını üst gövde yap
        GameObject upperBodyObj = new GameObject("UpperBody");
        upperBodyObj.transform.SetParent(transform);
        upperBodyObj.transform.localPosition = new Vector3(0, 0.5f, 0);
        upperBody = upperBodyObj.transform;
        
        // Kapsülün alt yarısını alt gövde yap
        GameObject lowerBodyObj = new GameObject("LowerBody");
        lowerBodyObj.transform.SetParent(transform);
        lowerBodyObj.transform.localPosition = new Vector3(0, -0.5f, 0);
        lowerBody = lowerBodyObj.transform;
        
        // Kamerayı baş olarak kullan
        if (Camera.main != null)
            head = Camera.main.transform;
        
        Debug.Log("Character body parts setup completed!");
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        HandleBodyRotation();
    }
    
    void HandleBodyRotation()
    {
        // TPS modunda üst gövde rotasyonunu devre dışı bırak
        CameraSwitcher cameraSwitcher = FindFirstObjectByType<CameraSwitcher>();
        if (cameraSwitcher != null && cameraSwitcher.currentMode == CameraSwitcher.CameraMode.TPS)
        {
            // TPS modunda sadece baş rotasyonu
            HandleHeadRotationOnly();
            return;
        }
        
        // FPS modunda normal rotasyon
        HandleFullBodyRotation();
    }

    void HandleHeadRotationOnly()
    {
        // Input System kullanarak mouse input'u al
        Vector2 mouseInput = Vector2.zero;
        
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            mouseInput = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();
        }
        
        // Sadece baş rotasyonu (yukarı-aşağı)
        if (mouseInput.y != 0)
        {
            currentHeadRotation -= mouseInput.y * headRotationSpeed * Time.deltaTime;
            currentHeadRotation = Mathf.Clamp(currentHeadRotation, -maxHeadAngle, maxHeadAngle);
        }
        else
        {
            // Yavaşça sıfıra dön
            currentHeadRotation = Mathf.Lerp(currentHeadRotation, 0, Time.deltaTime * returnSpeed);
        }
        
        // Baş rotasyonunu yumuşak şekilde uygula
        if (head != null)
        {
            Quaternion targetHeadRotation = Quaternion.Euler(currentHeadRotation, 0, 0);
            head.localRotation = Quaternion.Slerp(head.localRotation, targetHeadRotation, Time.deltaTime * rotationSmoothness);
        }
    }

    void HandleFullBodyRotation()
    {
        // Input System kullanarak mouse input'u al
        Vector2 mouseInput = Vector2.zero;
        
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            mouseInput = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();
        }
        
        // Üst gövde rotasyonu (sağa-sola)
        if (mouseInput.x != 0)
        {
            currentUpperBodyRotation += mouseInput.x * upperBodyRotationSpeed * Time.deltaTime;
            currentUpperBodyRotation = Mathf.Clamp(currentUpperBodyRotation, -maxUpperBodyAngle, maxUpperBodyAngle);
        }
        else
        {
            // Yavaşça sıfıra dön
            currentUpperBodyRotation = Mathf.Lerp(currentUpperBodyRotation, 0, Time.deltaTime * returnSpeed);
        }
        
        // Baş rotasyonu (yukarı-aşağı)
        if (mouseInput.y != 0)
        {
            currentHeadRotation -= mouseInput.y * headRotationSpeed * Time.deltaTime;
            currentHeadRotation = Mathf.Clamp(currentHeadRotation, -maxHeadAngle, maxHeadAngle);
        }
        
        // Rotasyonları smooth olarak uygula
        ApplyRotations();
    }
    
    void ApplyRotations()
    {
        // Üst gövde rotasyonu
        if (upperBody != null)
        {
            Quaternion targetRotation = Quaternion.Euler(0, currentUpperBodyRotation, 0);
            upperBody.localRotation = Quaternion.Slerp(upperBody.localRotation, targetRotation, Time.deltaTime * rotationSmoothness);
        }
        
        // Baş rotasyonu
        if (head != null)
        {
            Quaternion targetHeadRotation = Quaternion.Euler(currentHeadRotation, 0, 0);
            head.localRotation = Quaternion.Slerp(head.localRotation, targetHeadRotation, Time.deltaTime * rotationSmoothness);
        }
    }
    
    // Public methods for external control
    public void SetLookInput(Vector2 input)
    {
        lookInput = input;
    }
    
    public float GetUpperBodyRotation()
    {
        return currentUpperBodyRotation;
    }
    
    public float GetHeadRotation()
    {
        return currentHeadRotation;
    }
} 