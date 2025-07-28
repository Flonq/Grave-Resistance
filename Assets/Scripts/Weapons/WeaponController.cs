using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Settings")]
    public WeaponData currentWeapon;
    public Transform firePoint;
    public LayerMask enemyLayers = 1;
    
    [Header("Visual Effects")]
    public ParticleSystem muzzleFlash;
    public GameObject impactEffect;
    
    [Header("Camera References")]
    public CameraSwitcher cameraSwitcher; // Kamera mode kontrolü
    public GTAOrbitCamera tpsCamera; // TPS kamera referansı
    
    // Components
    private AudioSource audioSource;
    
    // Shooting variables
    private float nextFireTime;
    private bool isReloading;
    
    void Start()
    {
        // TPS kamera ve camera switcher bul
        if (tpsCamera == null)
            tpsCamera = FindFirstObjectByType<GTAOrbitCamera>();
            
        if (cameraSwitcher == null)
            cameraSwitcher = FindFirstObjectByType<CameraSwitcher>();
        
        audioSource = GetComponent<AudioSource>();
        
        // Initialize weapon
        if (currentWeapon != null)
        {
            currentWeapon.currentAmmo = currentWeapon.maxAmmo;
            currentWeapon.reserveAmmo = 48;
        }
        
        Debug.Log($"CameraSwitcher found: {cameraSwitcher != null}");
        Debug.Log($"TPS Camera found: {tpsCamera != null}");
    }
    
    // AKTİF KAMERAYI AL
    Camera GetActiveCamera()
    {
        if (cameraSwitcher == null) return Camera.main;
        
        switch (cameraSwitcher.currentMode)
        {
            case CameraSwitcher.CameraMode.FPS:
                return cameraSwitcher.fpsCamera;
            case CameraSwitcher.CameraMode.TPS:
                return cameraSwitcher.tpsCamera;
            default:
                return Camera.main;
        }
    }
    
    void Update()
    {
        // Input handling removed - will be called from PlayerController
    }
    
    public void Shoot()
    {
        if (!CanShoot()) return;
        
        // Set next fire time
        nextFireTime = Time.time + currentWeapon.fireRate;
        
        // Reduce ammo
        currentWeapon.currentAmmo--;
        
        // Raycast shooting
        PerformRaycast();
        
        // Visual effects
        if (muzzleFlash != null)
            muzzleFlash.Play();
        
        // Audio
        if (audioSource != null && currentWeapon.fireSound != null)
            audioSource.PlayOneShot(currentWeapon.fireSound);
    }
    
    void PerformRaycast()
    {
        Vector3 rayOrigin;
        Vector3 rayDirection;
        
        // Aktif kamerayı al
        Camera activeCamera = GetActiveCamera();
        bool isTPSMode = cameraSwitcher != null && cameraSwitcher.currentMode == CameraSwitcher.CameraMode.TPS;
        
        if (activeCamera != null)
        {
            // HER İKİ MODDA DA: Aktif kameradan screen center ray
            Ray centerRay = activeCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
            rayOrigin = centerRay.origin;
            rayDirection = centerRay.direction;
            
            Debug.Log($"{(isTPSMode ? "TPS" : "FPS")} Mode: Shooting from {activeCamera.name} screen center");
        }
        else
        {
            // Fallback: Transform forward
            rayOrigin = transform.position;
            rayDirection = transform.forward;
            
            Debug.LogWarning("No active camera found! Using transform forward");
        }
        
        RaycastHit hit;
        
        // Visualize the ray
        Debug.DrawRay(rayOrigin, rayDirection * currentWeapon.range, Color.red, 0.5f);
        
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, currentWeapon.range, enemyLayers))
        {
            Debug.Log($"Hit: {hit.collider.name} at {hit.point}");
            
            // Check if hit an enemy
            ZombieController zombie = hit.collider.GetComponent<ZombieController>();
            if (zombie != null)
            {
                zombie.TakeDamage(currentWeapon.damage);
                Debug.Log($"Zombie hit for {currentWeapon.damage} damage!");
            }
            
            // Spawn impact effect
            if (impactEffect != null)
            {
                GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 2f);
            }
            
            // TPS modunda firePoint'i hit noktasına çevir (görsel efekt için)
            if (isTPSMode && firePoint != null)
            {
                Vector3 directionToHit = (hit.point - firePoint.position).normalized;
                firePoint.LookAt(hit.point);
            }
        }
        else
        {
            // Hiçbir şeye çarpmadı - firePoint'i ray yönüne çevir
            if (isTPSMode && firePoint != null)
            {
                Vector3 targetPoint = rayOrigin + rayDirection * currentWeapon.range;
                firePoint.LookAt(targetPoint);
            }
        }
    }
    
    public void StartReload()
    {
        Debug.Log($"StartReload called! isReloading: {isReloading}, currentAmmo: {currentWeapon?.currentAmmo}, maxAmmo: {currentWeapon?.maxAmmo}, reserveAmmo: {currentWeapon?.reserveAmmo}");
        
        if (isReloading || currentWeapon.currentAmmo >= currentWeapon.maxAmmo || currentWeapon.reserveAmmo <= 0)
        {
            Debug.Log("Reload blocked!");
            return;
        }
        
        isReloading = true;
        
        // Start reload coroutine
        Invoke(nameof(FinishReload), currentWeapon.reloadTime);
    }
    
    void FinishReload()
    {
        int ammoNeeded = currentWeapon.maxAmmo - currentWeapon.currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, currentWeapon.reserveAmmo);
        
        currentWeapon.currentAmmo += ammoToReload;
        currentWeapon.reserveAmmo -= ammoToReload;
        
        isReloading = false;
        Debug.Log($"Reload complete! Ammo: {currentWeapon.currentAmmo}/{currentWeapon.maxAmmo}");
    }
    
    bool CanShoot()
    {
        return currentWeapon != null && 
               currentWeapon.currentAmmo > 0 && 
               Time.time >= nextFireTime && 
               !isReloading;
    }
    
    bool CanReload()
    {
        return currentWeapon != null && 
               currentWeapon.currentAmmo < currentWeapon.maxAmmo && 
               currentWeapon.reserveAmmo > 0 && 
               !isReloading;
    }
    
    // Public methods for UI
    public int GetCurrentAmmo() => currentWeapon != null ? currentWeapon.currentAmmo : 0;
    public int GetMaxAmmo() => currentWeapon != null ? currentWeapon.maxAmmo : 0;
    public int GetReserveAmmo() => currentWeapon != null ? currentWeapon.reserveAmmo : 0;
    public bool IsReloading() => isReloading;

    public void SetCurrentWeapon(WeaponData newWeapon)
    {
        currentWeapon = newWeapon;
        
        // Reset weapon to full ammo when switching
        if (currentWeapon != null)
        {
            currentWeapon.currentAmmo = currentWeapon.maxAmmo;
        }
    }
}
