using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [Header("Camera References")]
    public Camera fpsCamera;
    public Camera tpsCamera;
    
    [Header("Settings")]
    public CameraMode currentMode = CameraMode.FPS;
    
    public enum CameraMode { FPS, TPS }
    
    void Start()
    {
        // Start in FPS mode
        SetCameraMode(CameraMode.FPS);
    }
    
    public void ToggleCameraMode()
    {
        CameraMode newMode = (currentMode == CameraMode.FPS) ? CameraMode.TPS : CameraMode.FPS;
        SetCameraMode(newMode);
    }
    
    public void SetCameraMode(CameraMode mode)
    {
        currentMode = mode;
        
        switch (mode)
        {
            case CameraMode.FPS:
                fpsCamera.enabled = true;
                tpsCamera.enabled = false;
                break;
            case CameraMode.TPS:
                fpsCamera.enabled = false;
                tpsCamera.enabled = true;
                break;
        }
        
        // YENİ: Silah pozisyonunu güncelle
        UpdateWeaponPosition();
    }
    
    void UpdateWeaponPosition()
    {
        // WeaponController'ı bul ve pozisyonu güncelle
        WeaponController weaponController = FindFirstObjectByType<WeaponController>();
        if (weaponController != null)
        {
            // Güvenli çağrı
            try
            {
                weaponController.UpdateWeaponPosition();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Weapon position update failed: {e.Message}");
            }
        }
    }
}
