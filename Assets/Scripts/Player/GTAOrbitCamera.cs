using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Concentric Circle TPS Camera System - Pivot and Camera move in sync
/// </summary>
public class GTAOrbitCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    
    [Header("Circle Settings")]
    public float pivotRadius = 2f;           
    public float cameraRadius = 5f;          
    public float heightOffset = 1.5f;        
    public float pivotAngleOffset = 0f;      // ← YENİ! Pivot açı offset'i
    public float sensitivity = 1.0f; // Bu değeri düşür
    public float smoothTime = 0.3f;
    
    [Header("Aim Settings")]
    public float aimPivotRadius = 1.5f;      // Aim modunda pivot yaklaşır
    public float aimCameraRadius = 3f;       // Aim modunda camera yaklaşır
    public float aimSmoothTime = 0.1f;
    
    [Header("Collision Settings")]
    public LayerMask collisionLayers = ~0;
    public float collisionRadius = 0.3f;
    
    [Header("Debug")]
    public bool showGizmos = true;
    
    // Rotation angles
    private float horizontalAngle = 0f;
    private float verticalAngle = 0f;
    
    // Aim system
    private bool isAiming = false;
    
    // Smooth movement
    private Vector3 cameraVelocity = Vector3.zero;
    
    // Current positions
    private Vector3 currentPivotPosition;
    private Vector3 currentCameraPosition;
    
    // Original values
    private float originalPivotRadius;
    private float originalCameraRadius;
    private float originalSmoothTime;

    void Start()
    {
        // Orijinal değerleri kaydet
        originalPivotRadius = pivotRadius;
        originalCameraRadius = cameraRadius;
        originalSmoothTime = smoothTime;
        
        // Player'ın başlangıç yönünü al ve pivot offset'i ekle
        if (target != null)
        {
            horizontalAngle = target.eulerAngles.y + 45f;  // ← 45° offset (sağ omuz için)
            
            // Player layer'ını collision'dan çıkar
            int playerLayer = target.gameObject.layer;
            collisionLayers = collisionLayers & ~(1 << playerLayer);
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleInput();
        UpdateConcentricCircles();
        ApplySmoothMovement();
    }
    
    void HandleInput()
    {
        // Mouse input al
        Vector2 mouseInput = Mouse.current.delta.ReadValue();
        float scrollInput = Mouse.current.scroll.ReadValue().y;
        
        // SENKRON AÇI GÜNCELLEMESİ - İkisi de aynı hızda döner!
        horizontalAngle += mouseInput.x * sensitivity * Time.deltaTime;
        verticalAngle -= mouseInput.y * sensitivity * Time.deltaTime;
        
        // Dikey açıyı sınırla
        verticalAngle = Mathf.Clamp(verticalAngle, -30f, 60f);
        
        // Zoom (aim modunda değilse)
        if (!isAiming)
        {
            cameraRadius = Mathf.Clamp(cameraRadius - scrollInput * 0.2f, 2f, 10f);
            pivotRadius = Mathf.Clamp(pivotRadius - scrollInput * 0.1f, 0.5f, 3f);
        }
    }
    
    void UpdateConcentricCircles()
    {
        // Aim moduna göre radius'ları ayarla
        float targetPivotRadius = isAiming ? aimPivotRadius : originalPivotRadius;
        float targetCameraRadius = isAiming ? aimCameraRadius : originalCameraRadius;
        float targetSmoothTime = isAiming ? aimSmoothTime : originalSmoothTime;
        
        // Radius'ları smooth olarak güncelle
        pivotRadius = Mathf.Lerp(pivotRadius, targetPivotRadius, Time.deltaTime * 8f);
        cameraRadius = Mathf.Lerp(cameraRadius, targetCameraRadius, Time.deltaTime * 8f);
        smoothTime = Mathf.Lerp(smoothTime, targetSmoothTime, Time.deltaTime * 8f);
        
        // Ortak merkez noktası
        Vector3 centerPoint = target.position + Vector3.up * heightOffset;
        
        // SENKRON ÇEMBER HAREKETİ - Pivot için açı offset'i ekle!
        float pivotAngle = horizontalAngle + pivotAngleOffset;  // ← Pivot'a offset ekle
        float cameraAngle = horizontalAngle;                    // ← Camera offset'siz
        
        Vector3 pivotDirection = Quaternion.Euler(verticalAngle, pivotAngle, 0) * Vector3.forward;
        Vector3 cameraDirection = Quaternion.Euler(verticalAngle, cameraAngle, 0) * Vector3.back;
        
        // KÜÇÜK ÇEMBER - Pivot pozisyonu (offset'li)
        currentPivotPosition = centerPoint + (pivotDirection * pivotRadius);
        
        // BÜYÜK ÇEMBER - Camera pozisyonu (offset'siz)
        Vector3 desiredCameraPosition = centerPoint + (cameraDirection * cameraRadius);
        
        // Collision check - Camera ile pivot arasında engel var mı?
        currentCameraPosition = CheckCameraPivotCollision(desiredCameraPosition);
    }
    
    Vector3 CheckCameraPivotCollision(Vector3 desiredCameraPosition)
    {
        Vector3 pivotToCameraDirection = desiredCameraPosition - currentPivotPosition;
        float maxDistance = pivotToCameraDirection.magnitude;
        
        if (maxDistance < 0.1f) return desiredCameraPosition;
        
        RaycastHit hitInfo;
        
        // Pivot'tan camera'ya raycast - Player'dan geçer mi?
        if (Physics.SphereCast(currentPivotPosition, collisionRadius, pivotToCameraDirection.normalized, out hitInfo, maxDistance, collisionLayers))
        {
            // Collision var - Camera'yı safe distance'a çek
            float safeDistance = hitInfo.distance - collisionRadius;
            safeDistance = Mathf.Max(safeDistance, 0.5f); // Minimum mesafe
            
            return currentPivotPosition + pivotToCameraDirection.normalized * safeDistance;
        }
        
        return desiredCameraPosition;
    }
    
    void ApplySmoothMovement()
    {
        // Camera pozisyonunu smooth olarak güncelle
        transform.position = Vector3.SmoothDamp(transform.position, currentCameraPosition, ref cameraVelocity, smoothTime);
        
        // Kamerayı SEMPRE pivot'a baktır
        transform.LookAt(currentPivotPosition);
    }
    
    // Public methods for external control
    public void SetAiming(bool aiming)
    {
        if (isAiming != aiming)
        {
            isAiming = aiming;
        }
    }
    
    // Crosshair pivot pozisyonunu gösterir
    public Ray GetCenterRay()
    {
        Camera cam = GetComponent<Camera>();
        return cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
    }
    
    public Vector3 GetPivotPosition() => currentPivotPosition;
    public Vector3 GetCameraForward() => transform.forward;
    
    // Public method - Kod ile pivot pozisyonunu değiştir
    public void SetPivotOffset(float angleOffset)
    {
        pivotAngleOffset = angleOffset;
    }

    // Önceden tanımlı pozisyonlar
    public void SetPivotToRightShoulder() => SetPivotOffset(45f);    // Sağ omuz
    public void SetPivotToCenter() => SetPivotOffset(0f);           // Tam arkada  
    public void SetPivotToLeftShoulder() => SetPivotOffset(-45f);   // Sol omuz
    public void SetPivotToFront() => SetPivotOffset(180f);          // Tam önde
    
    // Debug için
    void OnDrawGizmos()
    {
        if (!showGizmos || target == null) return;
        
        Vector3 centerPoint = target.position + Vector3.up * heightOffset;
        
        // Çemberleri çiz
        Gizmos.color = Color.yellow;
        DrawCircle(centerPoint, pivotRadius, 32); // Küçük çember
        
        Gizmos.color = Color.cyan;
        DrawCircle(centerPoint, cameraRadius, 32); // Büyük çember
        
        // Pivot pozisyonu
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(currentPivotPosition, 0.15f);
        
        // Camera pozisyonu
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(currentCameraPosition, 0.2f);
        
        // Pivot ile camera arasındaki sight line
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(currentPivotPosition, currentCameraPosition);
        
        // Merkez noktası
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(centerPoint, 0.1f);
    }
    
    void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + Vector3.forward * radius;
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i;
            Vector3 newPoint = center + Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    public bool IsIdle() => !isAiming;
    public bool IsMoving() => false;
    public bool IsAiming() => isAiming;
}

