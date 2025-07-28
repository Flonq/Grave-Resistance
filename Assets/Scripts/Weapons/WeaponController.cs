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
    
    [Header("Weapon Models")]
    public WeaponModel[] weaponModels; // 0=Pistol, 1=Shotgun, 2=Rifle
    private WeaponModel currentWeaponModel;
    
    [Header("Available Weapons")]
    public WeaponData[] availableWeapons; // 0=Pistol, 1=Shotgun, 2=Rifle
    
    // Components
    private AudioSource audioSource;
    
    // Shooting variables
    private float nextFireTime;
    private bool isReloading;
    
    [Header("Debug")]
    public bool enablePlaceholders = true;

    [Header("Fire Mode")]
    public KeyCode fireModeToggleKey = KeyCode.B; // Fire mode değiştirme tuşu

    // Private variables

    void Start()
    {
        // Get components
        audioSource = GetComponent<AudioSource>();
        
        // Initialize shooting variables
        nextFireTime = 0f;
        isReloading = false;
        
        // Get camera references
        if (cameraSwitcher == null)
            cameraSwitcher = FindFirstObjectByType<CameraSwitcher>();
        
        if (tpsCamera == null)
            tpsCamera = FindFirstObjectByType<GTAOrbitCamera>();
        
        // YENİ: Silah modellerini oluştur ve ilk silahı yükle
        StartCoroutine(InitializeWeapons());
    }

    // YENİ: Silah başlatma coroutine'i
    System.Collections.IEnumerator InitializeWeapons()
    {
        // Bir frame bekle
        yield return null;
        
        // Silah modellerini oluştur
        CreatePlaceholderWeapons();
        
        // Bir frame daha bekle
        yield return null;
        
        // İlk silahı yükle
        if (availableWeapons.Length > 0)
        {
            SetCurrentWeapon(availableWeapons[0]);
            Debug.Log("First weapon loaded successfully");
        }
        
        // Pozisyonu güncelle
        UpdateWeaponPosition();
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
        HandleFireModeToggle();
        HandleAutoFire();
    }
    
    // YENİ: Fire mode değiştirme
    void HandleFireModeToggle()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (currentWeapon != null)
            {
                // Fire mode'u değiştir
                switch (currentWeapon.fireMode)
                {
                    case FireMode.Single:
                        currentWeapon.fireMode = FireMode.Auto;
                        Debug.Log("Fire Mode: Auto");
                        break;
                    case FireMode.Auto:
                        currentWeapon.fireMode = FireMode.Single;
                        Debug.Log("Fire Mode: Single");
                        break;
                }
            }
        }
    }

    // YENİ: Otomatik ateş
    void HandleAutoFire()
    {
        if (currentWeapon != null && currentWeapon.fireMode == FireMode.Auto)
        {
            if (UnityEngine.InputSystem.Mouse.current != null && 
                UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
            {
                if (CanShoot())
                {
                    Shoot();
                }
            }
        }
    }

    public void Shoot()
    {
        if (!CanShoot()) 
        {
            // Empty sound
            if (currentWeaponModel != null)
                currentWeaponModel.PlayEmptySound();
            return;
        }
        
        // Fire mode'a göre ateş et
        if (currentWeapon != null)
        {
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
    
    // YENİ: M4 reload animasyonu başlat
    if (currentWeaponModel != null)
    {
        currentWeaponModel.StartReloadAnimation();
        Debug.Log("M4 reload animation started!");
    }
    
    // Start reload coroutine
    Invoke(nameof(FinishReload), currentWeapon.reloadTime);
    
    // Weapon model reload animation
    if (currentWeaponModel != null)
        currentWeaponModel.PlayReloadAnimation();
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
        if (newWeapon == null) return;
        
        currentWeapon = newWeapon;
        
        // YENİ: Silah modelini hemen güncelle
        UpdateWeaponModel();
        
        // YENİ: Pozisyonu güncelle
        UpdateWeaponPosition();
        
        Debug.Log($"Current weapon set to: {newWeapon.weaponName}");
    }

    void UpdateWeaponModel()
    {
        // Tüm modelleri gizle
        foreach (WeaponModel model in weaponModels)
        {
            if (model != null)
            {
                model.SetActive(false);
            }
        }
        
        // Yeni modeli aktif et
        int weaponIndex = GetWeaponIndex(currentWeapon);
        if (weaponIndex >= 0 && weaponIndex < weaponModels.Length)
        {
            currentWeaponModel = weaponModels[weaponIndex];
            if (currentWeaponModel != null)
            {
                currentWeaponModel.SetActive(true);
                
                // YENİ: Doğrudan fonksiyon çağrısı yap
                if (currentWeapon != null)
                {
                    // SendMessage yerine doğrudan çağır
                    currentWeaponModel.SetWeaponPosition(currentWeapon.weaponName.ToLower());
                }
                
                Debug.Log($"Weapon model updated: {currentWeapon.weaponName}");
            }
        }
    }

    int GetWeaponIndex(WeaponData weapon)
    {
        if (weapon == null) return -1;
        
        // Weapon name'e göre index bul
        switch (weapon.weaponName.ToLower())
        {
            case "pistol": return 0;
            case "shotgun": return 1;
            case "rifle": return 2;
            default: return -1;
        }
    }

    void CreatePlaceholderWeapons()
    {
        weaponModels = new WeaponModel[3];
        
        // Pistol
        GameObject pistol = new GameObject("PistolModel");
        pistol.transform.SetParent(transform); // Player'a child olarak ekle
        WeaponModel pistolModel = pistol.AddComponent<WeaponModel>();
        if (availableWeapons != null && availableWeapons.Length > 0)
            pistolModel.weaponData = availableWeapons[0]; // Pistol ScriptableObject
        pistolModel.useLowPolyModel = true;
        weaponModels[0] = pistolModel;
        
        // Shotgun  
        GameObject shotgun = new GameObject("ShotgunModel");
        shotgun.transform.SetParent(transform); // Player'a child olarak ekle
        WeaponModel shotgunModel = shotgun.AddComponent<WeaponModel>();
        if (availableWeapons != null && availableWeapons.Length > 1)
            shotgunModel.weaponData = availableWeapons[1]; // Shotgun ScriptableObject
        shotgunModel.useLowPolyModel = true;
        weaponModels[1] = shotgunModel;
        
        // Rifle
        GameObject rifle = new GameObject("RifleModel");
        rifle.transform.SetParent(transform); // Player'a child olarak ekle
        WeaponModel rifleModel = rifle.AddComponent<WeaponModel>();
        if (availableWeapons != null && availableWeapons.Length > 2)
            rifleModel.weaponData = availableWeapons[2]; // Rifle ScriptableObject
        rifleModel.useLowPolyModel = true;
        weaponModels[2] = rifleModel;
        
        Debug.Log("Placeholder weapons created successfully!");
    }

    void RotatePlayerToShootDirection()
    {
        // Player'ı bul
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        
        // Kameranın baktığı yönü al
        Camera activeCamera = GetActiveCamera();
        if (activeCamera == null) return;
        
        // Kameranın forward vektörünü al (Y eksenini sıfırla)
        Vector3 shootDirection = activeCamera.transform.forward;
        shootDirection.y = 0;
        shootDirection = shootDirection.normalized;
        
        if (shootDirection.magnitude > 0.1f)
        {
            // ANINDA DÖNÜŞ - Slerp kullanmadan direkt rotasyon
            Quaternion targetRotation = Quaternion.LookRotation(shootDirection);
            player.transform.rotation = targetRotation;
            
            Debug.Log($"Player instantly rotated to: {shootDirection}");
        }
    }

    public void UpdateWeaponPosition()
    {
        if (cameraSwitcher == null) return;
        if (currentWeaponModel == null) return;
        
        try
        {
            // FPS modunda silahı kameraya bağla
            if (cameraSwitcher.currentMode == CameraSwitcher.CameraMode.FPS)
            {
                // Silahı FPS kameraya bağla
                if (cameraSwitcher.fpsCamera != null)
                {
                    // Güvenli parent değiştirme
                    if (currentWeaponModel.transform.parent != cameraSwitcher.fpsCamera.transform)
                    {
                        currentWeaponModel.transform.SetParent(cameraSwitcher.fpsCamera.transform, false);
                    }
                    
                    // FPS pozisyonu ayarla - namlu ileriye bakacak
                    currentWeaponModel.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
                    currentWeaponModel.transform.localRotation = Quaternion.Euler(0, 180, 0); // 180 derece döndür
                    currentWeaponModel.transform.localScale = new Vector3(1f, 1f, 1f);
                }
            }
            // TPS modunda silahı player'a bağla
            else if (cameraSwitcher.currentMode == CameraSwitcher.CameraMode.TPS)
            {
                // Güvenli parent değiştirme
                if (currentWeaponModel.transform.parent != transform)
                {
                    currentWeaponModel.transform.SetParent(transform, false);
                }
                
                // TPS pozisyonu ayarla - namlu ileriye bakacak
                currentWeaponModel.transform.localPosition = new Vector3(0.4f, 0.3f, 0.8f);
                currentWeaponModel.transform.localRotation = Quaternion.Euler(0, 180, 0); // 180 derece döndür
                currentWeaponModel.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Weapon position update failed: {e.Message}");
        }
    }

    // YENİ: Tek atış
    void PerformSingleShot()
    {
        if (CanShoot())
        {
            // Shotgun kontrolü
            if (currentWeapon.isShotgun)
            {
                PerformShotgunShot();
            }
            else
            {
                // Normal tek mermi
                PerformRaycast();
            }
            
            // Mermi azalt
            currentWeapon.currentAmmo--;
            
            // Set next fire time
            nextFireTime = Time.time + currentWeapon.fireRate;
            
            // Visual effects
            if (muzzleFlash != null)
                muzzleFlash.Play();
            
            // Audio
            if (audioSource != null && currentWeapon.fireSound != null)
                audioSource.PlayOneShot(currentWeapon.fireSound);
            
            // Weapon model animation
            if (currentWeaponModel != null)
                currentWeaponModel.PlayFireAnimation();
            
            // TPS modunda karakteri ateş yönüne döndür
            if (cameraSwitcher != null && cameraSwitcher.currentMode == CameraSwitcher.CameraMode.TPS)
            {
                RotatePlayerToShootDirection();
            }
            
            Debug.Log($"Shot fired! Ammo: {currentWeapon.currentAmmo}/{currentWeapon.maxAmmo}");
        }
    }

    // YENİ: Shotgun ateş fonksiyonu
    void PerformShotgunShot()
    {
        Vector3 rayOrigin;
        Vector3 baseDirection;
        
        // Aktif kamerayı al
        Camera activeCamera = GetActiveCamera();
        bool isTPSMode = cameraSwitcher != null && cameraSwitcher.currentMode == CameraSwitcher.CameraMode.TPS;
        
        if (activeCamera != null)
        {
            // Screen center'dan ray
            Ray centerRay = activeCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
            rayOrigin = centerRay.origin;
            baseDirection = centerRay.direction;
        }
        else
        {
            // Fallback
            rayOrigin = transform.position;
            baseDirection = transform.forward;
        }
        
        // Her pellet için raycast
        for (int i = 0; i < currentWeapon.pelletCount; i++)
        {
            // Rastgele spread açısı hesapla
            float randomSpread = Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle);
            
            // Spread'i uygula
            Vector3 spreadDirection = Quaternion.Euler(
                Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle),
                Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle),
                0
            ) * baseDirection;
            
            // Raycast
            RaycastHit hit;
            Debug.DrawRay(rayOrigin, spreadDirection * currentWeapon.range, Color.yellow, 0.5f);
            
            if (Physics.Raycast(rayOrigin, spreadDirection, out hit, currentWeapon.range, enemyLayers))
            {
                Debug.Log($"Shotgun pellet {i + 1} hit: {hit.collider.name}");
                
                // Zombi hasarı
                ZombieController zombie = hit.collider.GetComponent<ZombieController>();
                if (zombie != null)
                {
                    // Shotgun için daha düşük hasar (pellet başına)
                    float pelletDamage = currentWeapon.damage / currentWeapon.pelletCount;
                    zombie.TakeDamage(pelletDamage);
                    Debug.Log($"Zombie hit by pellet {i + 1} for {pelletDamage} damage!");
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
}
