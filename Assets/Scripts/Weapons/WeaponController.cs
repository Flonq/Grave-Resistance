using UnityEngine;
using UnityEngine.InputSystem;
using static WeaponData;

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
    public CameraSwitcher cameraSwitcher;
    public GTAOrbitCamera tpsCamera;
    public Camera playerCamera;
    
    [Header("Weapon Prefabs")]
    public GameObject pistolPrefab; // M1911 prefab'ı
    public GameObject shotgunPrefab; // Bennelli_M4 prefab'ı
    public GameObject riflePrefab; // M4_8 prefab'ı
    
    [Header("Available Weapons")]
    public WeaponData[] availableWeapons; // 0=Pistol, 1=Shotgun, 2=Rifle
    
    // Components
    private AudioSource audioSource;
    private float nextFireTime;
    
    // Weapon instances
    private GameObject currentWeaponInstance;
    private int currentWeaponIndex = 0;
    
    [Header("Debug")]
    public bool enableDebug = true;
    
    [Header("FPS Weapon Positions")]
    // Normal pozisyonlar (aim almadığında)
    public Vector3 pistolNormalPosition = new Vector3(0.4f, -0.2f, 0.8f);
    public Vector3 shotgunNormalPosition = new Vector3(0.5f, -0.15f, 0.9f);
    public Vector3 rifleNormalPosition = new Vector3(0.35f, -0.25f, 0.7f);

    // Aim pozisyonları (sağ tık ile nişan alırken)
    public Vector3 pistolAimOffset = new Vector3(0.0f, -0.1f, 0.4f);
    public Vector3 shotgunAimOffset = new Vector3(0.0f, -0.05f, 0.5f);
    public Vector3 rifleAimOffset = new Vector3(0.0f, -0.15f, 0.3f);
    
    private Vector3 currentWeaponOffset; // Mevcut offset
    private Vector3 currentWeaponVelocity = Vector3.zero;
    private Vector3 currentWeaponPositionVelocity = Vector3.zero; // YENİ: Pozisyon için ayrı velocity
    private Quaternion currentWeaponRotationVelocity = Quaternion.identity;

    [Header("Smooth Settings")]
    [Range(5f, 25f)]
    public float smoothSpeed = 15f; // Smooth hızı
    
    void Start()
    {
        // Get components
        audioSource = GetComponent<AudioSource>();
        
        // Initialize shooting variables
        nextFireTime = 0f;
        
        // Get camera references
        if (cameraSwitcher == null)
            cameraSwitcher = FindFirstObjectByType<CameraSwitcher>();
        
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        
        // Initialize weapon system
        if (availableWeapons.Length > 0)
        {
            currentWeapon = availableWeapons[0];
            CreateWeaponInstance(0);
            Debug.Log("Initial weapon set to: " + currentWeapon.weaponName);
            
            // YENİ: Başlangıçta silah pozisyonunu ayarla
            UpdateWeaponPosition();
            
            // YENİ: Silahı görünür yap
            if (currentWeaponInstance != null)
            {
                currentWeaponInstance.SetActive(true);
                Debug.Log("Initial weapon activated and positioned");
            }
        }
    }
    
    void LateUpdate() // Update() yerine LateUpdate() kullan
    {
        HandleFireModeToggle();
        HandleAutoFire();
        
        // Her zaman silah pozisyonunu güncelle (FPS ve TPS için)
        CameraSwitcher cameraSwitcher = FindFirstObjectByType<CameraSwitcher>();
        if (cameraSwitcher != null)
        {
            UpdateWeaponPosition();
        }
    }
    
    // YENİ: Silah instance'ı oluştur
    void CreateWeaponInstance(int weaponIndex)
    {
        // Mevcut silahı yok et
        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance);
        }
        
        // Yeni silah oluştur
        GameObject weaponPrefab = null;
        
        switch (weaponIndex)
        {
            case 0: // Pistol
                weaponPrefab = pistolPrefab;
                break;
            case 1: // Shotgun
                weaponPrefab = shotgunPrefab;
                break;
            case 2: // Rifle
                weaponPrefab = riflePrefab;
                break;
        }
        
        if (weaponPrefab != null)
        {
            // Silahı oluştur
            currentWeaponInstance = Instantiate(weaponPrefab);
            
            // YENİ DEBUG: Silah oluşturulduktan sonra pozisyon kontrolü
            Debug.Log($"Weapon created - Position: {currentWeaponInstance.transform.position}");
            Debug.Log($"Weapon created - Local Position: {currentWeaponInstance.transform.localPosition}");
            Debug.Log($"Weapon created - Scale: {currentWeaponInstance.transform.localScale}");
            
            // WeaponModel component'i ekle
            WeaponModel weaponModel = currentWeaponInstance.AddComponent<WeaponModel>();
            weaponModel.weaponData = availableWeapons[weaponIndex];
            
            // YENİ DEBUG: WeaponModel ayarlandıktan sonra
            Debug.Log($"WeaponModel added for weapon: {availableWeapons[weaponIndex].weaponName}");
            
            // Pozisyonu ayarla
            UpdateWeaponPosition();
            
            Debug.Log($"Weapon instance created: {availableWeapons[weaponIndex].weaponName}");
        }
        else
        {
            Debug.LogError($"Weapon prefab not found for index: {weaponIndex}");
        }
    }
    
    // YENİ: Silah değiştir
    public void SwitchWeapon(int weaponIndex)
    {
        if (weaponIndex >= 0 && weaponIndex < availableWeapons.Length)
        {
            currentWeaponIndex = weaponIndex;
            currentWeapon = availableWeapons[weaponIndex];
            CreateWeaponInstance(weaponIndex);
            Debug.Log($"Switched to weapon: {currentWeapon.weaponName}");
        }
    }
    
    // YENİ: Silah pozisyonunu güncelle
    public void UpdateWeaponPosition()
    {
        if (currentWeaponInstance == null) return;
        
        CameraSwitcher cameraSwitcher = FindFirstObjectByType<CameraSwitcher>();
        if (cameraSwitcher == null) return;
        
        if (cameraSwitcher.currentMode == CameraSwitcher.CameraMode.FPS)
        {
            // FPS modunda circle sistemi kullan
            UpdateFPSWeaponPosition();
        }
        else
        {
            // TPS modunda player'a child olarak ekle
            if (currentWeaponInstance.transform.parent != transform)
            {
                currentWeaponInstance.transform.SetParent(transform, false);
                // TPS modunda silahı görünür yap
                currentWeaponInstance.SetActive(true);
            }
        }
    }

    private void UpdateFPSWeaponPosition()
    {
        if (currentWeaponInstance == null || playerCamera == null) return;

        bool isAiming = Mouse.current.rightButton.isPressed;

        Vector3 targetNormalPosition;
        Vector3 targetAimOffset;
        
        switch (currentWeaponIndex)
        {
            case 0: // Pistol
                targetNormalPosition = pistolNormalPosition;
                targetAimOffset = pistolAimOffset;
                break;
            case 1: // Shotgun
                targetNormalPosition = shotgunNormalPosition;
                targetAimOffset = shotgunAimOffset;
                break;
            case 2: // Rifle
                targetNormalPosition = rifleNormalPosition;
                targetAimOffset = rifleAimOffset;
                break;
            default:
                targetNormalPosition = pistolNormalPosition;
                targetAimOffset = pistolAimOffset;
                break;
        }

        Vector3 targetPosition = isAiming ? targetAimOffset : targetNormalPosition;

        // Sadece offset için smooth damping (nişan alma geçişi için)
        currentWeaponOffset = Vector3.SmoothDamp(currentWeaponOffset, targetPosition, ref currentWeaponVelocity, 1f / smoothSpeed);

        Vector3 weaponPosition = playerCamera.transform.position +
            playerCamera.transform.forward * currentWeaponOffset.z +
            playerCamera.transform.right * currentWeaponOffset.x +
            playerCamera.transform.up * currentWeaponOffset.y;

        // Direkt pozisyon ataması (sabit kalması için)
        currentWeaponInstance.transform.position = weaponPosition;

        // Direkt rotasyon ataması (sabit kalması için)
        Quaternion targetRotation = playerCamera.transform.rotation * Quaternion.Euler(0, 180, 0);
        currentWeaponInstance.transform.rotation = targetRotation;

        if (currentWeaponInstance.transform.parent != null)
        {
            currentWeaponInstance.transform.SetParent(null);
        }
    }
    
    void HandleFireModeToggle()
    {
        try
        {
            // B tuşu kontrolü
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame)
            {
                if (currentWeapon.fireMode == FireMode.Single)
                    currentWeapon.fireMode = FireMode.Auto;
                else
                    currentWeapon.fireMode = FireMode.Single;
                    
                Debug.Log($"Fire mode changed to: {currentWeapon.fireMode}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Fire mode toggle error: {e.Message}");
        }
    }
    
    void HandleAutoFire()
    {
        if (currentWeapon.fireMode == FireMode.Auto && 
            UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
        {
            Shoot();
        }
    }
    
    public void Shoot()
    {
        if (!CanShoot()) return;
        
        switch (currentWeapon.fireMode)
        {
            case FireMode.Single:
                PerformSingleShot();
                break;
            case FireMode.Auto:
                PerformSingleShot();
                break;
        }
    }
    
    void PerformSingleShot()
    {
        if (currentWeapon.isShotgun)
        {
            PerformShotgunShot();
        }
        else
        {
            PerformRaycast();
        }
        
        // Ammo decrement
        currentWeapon.currentAmmo--;
        
        // Visual and audio effects
        if (muzzleFlash != null)
            muzzleFlash.Play();
        
        if (audioSource != null && currentWeapon.fireSound != null)
            audioSource.PlayOneShot(currentWeapon.fireSound);
            
        // Update fire time
        nextFireTime = Time.time + currentWeapon.fireRate;

        // Recoil uygula
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.ApplyRecoil(currentWeapon.verticalRecoil, currentWeapon.horizontalRecoil);
        }
    }
    
    void PerformRaycast()
    {
        Camera activeCamera = GetActiveCamera();
        if (activeCamera == null) return;
        
        Vector3 rayOrigin = activeCamera.transform.position;
        Vector3 rayDirection = activeCamera.transform.forward;
        
        if (enableDebug)
            Debug.DrawRay(rayOrigin, rayDirection * currentWeapon.range, Color.red, 1f);
        
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, currentWeapon.range, enemyLayers))
        {
            // Hit enemy
            ZombieController zombie = hit.collider.GetComponent<ZombieController>();
            if (zombie != null)
            {
                zombie.TakeDamage(currentWeapon.damage);
                Debug.Log($"Hit zombie! Damage: {currentWeapon.damage}");
            }
            
            // Impact effect
            if (impactEffect != null)
            {
                GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 2f);
            }
        }
    }
    
    void PerformShotgunShot()
    {
        Camera activeCamera = GetActiveCamera();
        if (activeCamera == null) return;
        
        Vector3 rayOrigin = activeCamera.transform.position;
        
        for (int i = 0; i < currentWeapon.pelletCount; i++)
        {
            // Random spread
            Vector3 randomDirection = activeCamera.transform.forward;
            randomDirection += Random.insideUnitSphere * Mathf.Tan(currentWeapon.spreadAngle * Mathf.Deg2Rad);
            randomDirection.Normalize();
            
            if (enableDebug)
                Debug.DrawRay(rayOrigin, randomDirection * currentWeapon.range, Color.yellow, 1f);
            
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, randomDirection, out hit, currentWeapon.range, enemyLayers))
            {
                // Hit enemy
                ZombieController zombie = hit.collider.GetComponent<ZombieController>();
                if (zombie != null)
                {
                    zombie.TakeDamage(currentWeapon.damage);
                }
                
                // Impact effect
                if (impactEffect != null)
                {
                    GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(impact, 2f);
                }
            }
        }
        
        Debug.Log($"Shotgun fired {currentWeapon.pelletCount} pellets!");
    }
    
    public void StartReload()
    {
        if (CanReload())
        {
            // Reload süresini bekle
            Invoke(nameof(FinishReload), currentWeapon.reloadTime);
            
            Debug.Log("Reload başladı!");
        }
    }
    
    void FinishReload()
    {
        int ammoNeeded = currentWeapon.maxAmmo - currentWeapon.currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, currentWeapon.reserveAmmo);
        
        currentWeapon.currentAmmo += ammoToReload;
        currentWeapon.reserveAmmo -= ammoToReload;
        
        Debug.Log($"Reload complete! Ammo: {currentWeapon.currentAmmo}/{currentWeapon.maxAmmo}");
    }
    
    bool CanShoot()
    {
        return currentWeapon != null && 
               currentWeapon.currentAmmo > 0 && 
               Time.time >= nextFireTime;
    }
    
    bool CanReload()
    {
        return currentWeapon != null && 
               currentWeapon.currentAmmo < currentWeapon.maxAmmo && 
               currentWeapon.reserveAmmo > 0;
    }
    
    Camera GetActiveCamera()
    {
        if (cameraSwitcher != null)
        {
            return cameraSwitcher.currentMode == CameraSwitcher.CameraMode.FPS ? 
                   cameraSwitcher.fpsCamera : cameraSwitcher.tpsCamera;
        }
        return Camera.main;
    }
    
    // YENİ: Aim pozisyonu güncelleme fonksiyonu
    void UpdateWeaponAimPosition()
    {
        WeaponModel weaponModel = currentWeaponInstance?.GetComponent<WeaponModel>();
        if (weaponModel != null)
        {
            // PlayerController'dan aim durumunu al
            PlayerController playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                bool isAiming = playerController.IsAimingFPS();
                weaponModel.UpdateAimPosition(isAiming);
                weaponModel.UpdatePosition();
            }
        }
    }
    
    // Public getters
    public int GetCurrentAmmo() => currentWeapon != null ? currentWeapon.currentAmmo : 0;
    public int GetMaxAmmo() => currentWeapon != null ? currentWeapon.maxAmmo : 0;
    public int GetReserveAmmo() => currentWeapon != null ? currentWeapon.reserveAmmo : 0;
}
