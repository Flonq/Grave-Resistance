using UnityEngine;

public class WeaponModel : MonoBehaviour
{
    [Header("Weapon Model Settings")]
    public WeaponData weaponData;
    public Transform muzzlePoint;
    public Transform gripPoint;
    public Transform scopePoint;
    
    [Header("Animation")]
    public Animator weaponAnimator;
    public string fireAnimationName = "Fire";
    public string reloadAnimationName = "Reload";
    public string idleAnimationName = "Idle";
    
    [Header("Visual Effects")]
    public ParticleSystem muzzleFlash;
    public GameObject shellEjectPrefab;
    public Transform shellEjectPoint;
    
    [Header("Audio")]
    public AudioSource weaponAudioSource;
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    
    [Header("Low Poly Weapon Settings")]
    public bool useLowPolyModel = true;
    public GameObject lowPolyModelPrefab;
    
    [Header("M4 Animation Components")]
    public Transform magTransform; // Magazine
    public Transform boltTransform; // Bolt
    public Transform triggerTransform; // Trigger
    public Transform sightTransform; // Front Sight
    public Transform rearSightTransform; // Rear Sight

    [Header("M4 Animation Settings")]
    public float magEjectSpeed = 2f;
    public float magInsertSpeed = 1.5f;
    public float boltPullSpeed = 3f;
    public float boltReleaseSpeed = 2f;
    public Vector3 magEjectPosition = new Vector3(0, -0.2f, 0.1f);
    public Vector3 magInsertPosition = new Vector3(0, 0, 0);
    public Vector3 boltPulledPosition = new Vector3(0, 0, -0.1f);
    public Vector3 boltReleasedPosition = new Vector3(0, 0, 0);
    
    [Header("Aim Settings")]
    public Vector3 normalPosition = new Vector3(0.4f, 0.1f, 0.8f); // Normal pozisyon
    public Vector3 aimPosition = new Vector3(0.0f, -0.2f, 0.4f); // Aim pozisyonu
    public float aimSmoothSpeed = 8f; // Pozisyon geçiş hızı

    // Private variables
    private bool isActive = false;
    private Vector3 originalMagPosition;
    private Vector3 originalBoltPosition;
    private Vector3 currentPosition;
    private Vector3 targetPosition;
    
    void Start()
    {
        // WeaponData'yı kontrol et
        if (weaponData == null)
        {
            Debug.LogError("WeaponData is null in WeaponModel!");
            return;
        }
        
        // Pozisyon değişkenlerini başlat
        currentPosition = normalPosition;
        targetPosition = normalPosition;
        
        // Silahı aktif et ve pozisyonunu ayarla
        SetActive(true);
        
        // Debug: Pozisyon bilgilerini yazdır
        Debug.Log($"WeaponModel Start - Name: {weaponData.weaponName}");
        Debug.Log($"Normal Position: {normalPosition}");
        Debug.Log($"Aim Position: {aimPosition}");
        Debug.Log($"Current Position: {currentPosition}");
        Debug.Log($"Transform Position: {transform.localPosition}");
        Debug.Log($"Transform Scale: {transform.localScale}");
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
        
        // YENİ DEBUG: SetActive çağrısı
        Debug.Log($"WeaponModel SetActive: {active} for {weaponData?.weaponName}");
        
        // Renderer'ları bul ve aktif et
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = active;
            Debug.Log($"Renderer {renderer.name} set to: {active}");
        }
        
        // MeshRenderer'ları da kontrol et
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            meshRenderer.enabled = active;
            Debug.Log($"MeshRenderer {meshRenderer.name} set to: {active}");
        }
        
        // Pozisyonu ayarla
        if (active)
        {
            SetWeaponPosition(weaponData?.weaponName ?? "unknown");
        }
    }
    
    
    public void PlayEmptySound()
    {
        if (weaponAudioSource != null && emptySound != null)
            weaponAudioSource.PlayOneShot(emptySound);
    }
    
    // Getters
    public Transform GetMuzzlePoint() => muzzlePoint;
    public Transform GetGripPoint() => gripPoint;
    public Transform GetScopePoint() => scopePoint;
    public bool IsActive() => isActive;

    public void PlaySwitchAnimation()
    {
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("Switch");
        }
    }

    public void SetWeaponPosition(string weaponName)
    {
        Debug.Log($"Setting weapon position for: {weaponName}");
        
        // Kamera modunu kontrol et
        CameraSwitcher cameraSwitcher = FindFirstObjectByType<CameraSwitcher>();
        bool isFPSMode = cameraSwitcher != null && cameraSwitcher.currentMode == CameraSwitcher.CameraMode.FPS;
        
        // Silah tipine göre pozisyon ve rotasyon ayarla
        switch (weaponName.ToLower())
        {
            case "pistol":
            case "shotgun":
            case "rifle":
                // FPS modunda pozisyonu sabit tut
                if (isFPSMode)
                {
                    transform.localPosition = normalPosition;
                    transform.localRotation = Quaternion.Euler(0, 180, 0);
                    transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
                }
                else
                {
                    // TPS modunda normal pozisyon
                    transform.localPosition = normalPosition;
                    transform.localRotation = Quaternion.Euler(0, 180, 0);
                    transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
                }
                break;
                
            default:
                Debug.LogWarning($"Unknown weapon name: {weaponName}");
                transform.localPosition = normalPosition;
                transform.localRotation = Quaternion.Euler(0, 180, 0);
                transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
                break;
        }
        
        // Pozisyon değişkenlerini güncelle
        currentPosition = normalPosition;
        targetPosition = normalPosition;
        
        Debug.Log($"Weapon position set to: {normalPosition}");
        Debug.Log($"FPS Mode: {isFPSMode}");
    }

    void CreatePlaceholder()
    {
        // Sadece Low Poly model kullan
        if (useLowPolyModel)
        {
            LoadLowPolyModel();
        }
        else
        {
            // Basit fallback
            CreateSimpleFallback();
        }
    }

    void LoadLowPolyModel()
    {
        // WeaponData'dan silah adını al
        string weaponName = weaponData != null ? weaponData.weaponName.ToLower() : "pistol";
        
        Debug.Log($"Loading Low Poly model for: {weaponName}");
        
        // Asset paketinden model yükle
        GameObject modelPrefab = LoadWeaponModelFromAssets(weaponName);
        
        if (modelPrefab != null)
        {
            // Model'i instantiate et
            GameObject model = Instantiate(modelPrefab, transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            
            // Silah tipine göre pozisyon ayarla
            SetWeaponPosition(weaponName);
            
            // Muzzle point'i bul
            FindMuzzlePoint();
            
            Debug.Log($"Low Poly model loaded successfully: {weaponName}");
        }
        else
        {
            Debug.LogWarning($"Low Poly model not found for: {weaponName}");
            // Fallback: Basit bir küp oluştur
            CreateSimpleFallback();
        }
    }

    GameObject LoadWeaponModelFromAssets(string weaponName)
    {
        GameObject modelPrefab = null;
        
        // Belirlediğiniz modellere göre yükle
        switch (weaponName)
        {
            case "pistol":
                modelPrefab = Resources.Load<GameObject>("Weapons/M1911");
                if (modelPrefab == null)
                    modelPrefab = Resources.Load<GameObject>("M1911");
                break;
                
            case "shotgun":
                modelPrefab = Resources.Load<GameObject>("Weapons/Bennelli_M4");
                if (modelPrefab == null)
                    modelPrefab = Resources.Load<GameObject>("Bennelli_M4");
                break;
                
            case "rifle":
                modelPrefab = Resources.Load<GameObject>("Weapons/M4_8");
                if (modelPrefab == null)
                    modelPrefab = Resources.Load<GameObject>("M4_8");
                break;
        }
        
        // Eğer bulunamazsa debug mesajı
        if (modelPrefab == null)
        {
            Debug.LogWarning($"Model not found for: {weaponName}. Trying alternative paths...");
            
            // Alternatif yollar dene
            modelPrefab = Resources.Load<GameObject>(weaponName);
            if (modelPrefab == null)
            {
                Debug.LogError($"No model found for weapon: {weaponName}");
            }
        }
        else
        {
            Debug.Log($"Model loaded successfully: {weaponName}");
        }
        
        return modelPrefab;
    }

    void CreateSimpleFallback()
    {
        // Basit bir küp oluştur
        GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fallback.transform.SetParent(transform);
        fallback.transform.localPosition = Vector3.zero;
        fallback.transform.localScale = new Vector3(0.1f, 0.1f, 0.3f);
        
        // Muzzle point oluştur
        GameObject muzzle = new GameObject("MuzzlePoint");
        muzzle.transform.SetParent(transform);
        muzzle.transform.localPosition = new Vector3(0, 0, 0.2f);
        muzzlePoint = muzzle.transform;
        
        // Material
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = Color.gray;
        fallback.GetComponent<Renderer>().material = mat;
        
        Debug.Log("Simple fallback model created");
    }

    void FindMuzzlePoint()
    {
        // Model'de muzzle point'i bul
        Transform[] allChildren = GetComponentsInChildren<Transform>();
        
        foreach (Transform child in allChildren)
        {
            // M1911 için
            if (child.name.ToLower().Contains("muzzle") || 
                child.name.ToLower().Contains("barrel") ||
                child.name.ToLower().Contains("tip") ||
                child.name.ToLower().Contains("end"))
            {
                muzzlePoint = child;
                Debug.Log($"Muzzle point found: {child.name}");
                break;
            }
        }
        
        // Bulunamazsa otomatik oluştur
        if (muzzlePoint == null)
        {
            GameObject muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(transform);
            
            // Silah tipine göre pozisyon ayarla
            if (weaponData != null)
            {
                switch (weaponData.weaponName.ToLower())
                {
                    case "pistol":
                        muzzle.transform.localPosition = new Vector3(0, 0, 0.3f);
                        break;
                    case "shotgun":
                        muzzle.transform.localPosition = new Vector3(0, 0, 0.4f);
                        break;
                    case "rifle":
                        muzzle.transform.localPosition = new Vector3(0, 0, 0.5f);
                        break;
                }
            }
            else
            {
                muzzle.transform.localPosition = new Vector3(0, 0, 0.3f);
            }
            
            muzzlePoint = muzzle.transform;
            Debug.Log("Muzzle point created automatically");
        }
    }

    // YENİ: M4 component'lerini bul
    void FindM4Components()
    {
        if (magTransform == null)
            magTransform = transform.Find("Mag");
        
        if (boltTransform == null)
            boltTransform = transform.Find("Bolt");
        
        if (triggerTransform == null)
            triggerTransform = transform.Find("Trigger");
        
        if (sightTransform == null)
            sightTransform = transform.Find("Sight");
        
        if (rearSightTransform == null)
            rearSightTransform = transform.Find("Rear_Sight");
        
        // Orijinal pozisyonları kaydet
        if (magTransform != null)
            originalMagPosition = magTransform.localPosition;
        
        if (boltTransform != null)
            originalBoltPosition = boltTransform.localPosition;
        
        Debug.Log("M4 components found and initialized!");
    }

    // Eski PlayFireAnimation() fonksiyonunu geri ekleyin:
    public void PlayFireAnimation()
    {
        if (!isActive || weaponAnimator == null) return;
        
        weaponAnimator.Play(fireAnimationName);
        
        // Muzzle flash
        if (muzzleFlash != null)
            muzzleFlash.Play();
        
        // Fire sound
        if (weaponAudioSource != null && fireSound != null)
            weaponAudioSource.PlayOneShot(fireSound);
        
        // Shell eject
        if (shellEjectPrefab != null && shellEjectPoint != null)
        {
            GameObject shell = Instantiate(shellEjectPrefab, shellEjectPoint.position, shellEjectPoint.rotation);
            Destroy(shell, 3f);
        }
    }

    // Eski PlayReloadAnimation() fonksiyonunu geri ekleyin:
    public void PlayReloadAnimation()
    {
        if (!isActive || weaponAnimator == null) return;
        
        weaponAnimator.Play(reloadAnimationName);
        
        // Reload sound
        if (weaponAudioSource != null && reloadSound != null)
            weaponAudioSource.PlayOneShot(reloadSound);
    }

    // YENİ: Aim pozisyonu güncelleme fonksiyonu
    public void UpdateAimPosition(bool isAiming)
    {
        if (isAiming)
        {
            targetPosition = aimPosition;
        }
        else
        {
            targetPosition = normalPosition;
        }
    }

    // YENİ: Pozisyon güncelleme fonksiyonu
    public void UpdatePosition()
    {
        // Kamera modunu kontrol et
        CameraSwitcher cameraSwitcher = FindFirstObjectByType<CameraSwitcher>();
        if (cameraSwitcher != null && cameraSwitcher.currentMode == CameraSwitcher.CameraMode.FPS)
        {
            // FPS modunda pozisyonu hiç değiştirme - sabit tut!
            // Sadece rotasyonu sabit tut
            transform.localRotation = Quaternion.Euler(0, 180, 0);
            transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
        }
        else
        {
            // TPS modunda pozisyonu smooth olarak güncelle
            currentPosition = Vector3.Lerp(currentPosition, targetPosition, aimSmoothSpeed * Time.deltaTime);
            transform.localPosition = currentPosition;
            
            // Rotasyonu sabit tut
            transform.localRotation = Quaternion.Euler(0, 180, 0);
            transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
        }
    }
} 