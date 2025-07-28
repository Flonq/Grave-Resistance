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
    
    // Private variables
    private bool isActive = false;
    private Vector3 originalMagPosition;
    private Vector3 originalBoltPosition;
    private bool isReloading = false;
    
    void Start()
    {
        // Weapon model'i başlangıçta gizle
        SetActive(false);
        
        // Audio source'u otomatik bul
        if (weaponAudioSource == null)
            weaponAudioSource = GetComponent<AudioSource>();
        
        // YENİ: M4 component'lerini otomatik bul
        FindM4Components();
        
        // Low Poly model kullanılıyorsa oluştur
        if (useLowPolyModel)
        {
            CreatePlaceholder();
        }
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
        gameObject.SetActive(active);
        
        if (active)
        {
            // YENİ: Aktif olduğunda pozisyonu güncelle
            if (weaponData != null)
            {
                SetWeaponPosition(weaponData.weaponName.ToLower());
                Debug.Log($"Weapon activated: {weaponData.weaponName}");
            }
            
            if (weaponAnimator != null)
            {
                weaponAnimator.Play(idleAnimationName);
            }
        }
    }
    
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
    
    public void PlayReloadAnimation()
    {
        if (!isActive || weaponAnimator == null) return;
        
        weaponAnimator.Play(reloadAnimationName);
        
        // Reload sound
        if (weaponAudioSource != null && reloadSound != null)
            weaponAudioSource.PlayOneShot(reloadSound);
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
        
        // Silah tipine göre pozisyon ve rotasyon ayarla
        switch (weaponName)
        {
            case "pistol":
                transform.localPosition = new Vector3(0.4f, 0.1f, 0.8f);
                transform.localRotation = Quaternion.Euler(0, 180, 0);
                transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
                break;
                
            case "shotgun":
                transform.localPosition = new Vector3(0.4f, 0.1f, 0.8f);
                transform.localRotation = Quaternion.Euler(0, 180, 0);
                transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
                break;
                
            case "rifle":
                transform.localPosition = new Vector3(0.4f, 0.1f, 0.8f);
                transform.localRotation = Quaternion.Euler(0, 180, 0);
                transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
                break;
        }
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
            
            Debug.Log($"Low Poly model loaded: {weaponName}");
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
                break;
                
            case "shotgun":
                modelPrefab = Resources.Load<GameObject>("Weapons/Bennelli_M4");
                break;
                
            case "rifle":
                modelPrefab = Resources.Load<GameObject>("Weapons/M4_8");
                break;
        }
        
        // Eğer bulunamazsa debug mesajı
        if (modelPrefab == null)
        {
            Debug.LogWarning($"Model not found for: {weaponName}");
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

    // YENİ: Reload animasyon başlat
    public void StartReloadAnimation()
    {
        if (isReloading) return;
        
        isReloading = true;
        StartCoroutine(ReloadAnimationSequence());
    }

    // YENİ: Reload animasyon sırası
    System.Collections.IEnumerator ReloadAnimationSequence()
    {
        Debug.Log("Starting M4 reload animation...");
        
        // 1. Mag'i çıkar
        yield return StartCoroutine(EjectMagazine());
        
        // 2. Bolt'u geri çek
        yield return StartCoroutine(PullBolt());
        
        // 3. Yeni mag'i tak
        yield return StartCoroutine(InsertMagazine());
        
        // 4. Bolt'u serbest bırak
        yield return StartCoroutine(ReleaseBolt());
        
        isReloading = false;
        Debug.Log("M4 reload animation completed!");
    }

    // YENİ: Magazine çıkarma animasyonu
    System.Collections.IEnumerator EjectMagazine()
    {
        if (magTransform == null) yield break;
        
        Debug.Log("Ejecting magazine...");
        
        Vector3 startPos = magTransform.localPosition;
        Vector3 endPos = magEjectPosition;
        float duration = 1f / magEjectSpeed;
        
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;
            magTransform.localPosition = Vector3.Lerp(startPos, endPos, progress);
            yield return null;
        }
        
        magTransform.localPosition = endPos;
        Debug.Log("Magazine ejected!");
    }

    // YENİ: Magazine takma animasyonu
    System.Collections.IEnumerator InsertMagazine()
    {
        if (magTransform == null) yield break;
        
        Debug.Log("Inserting magazine...");
        
        Vector3 startPos = magEjectPosition;
        Vector3 endPos = magInsertPosition;
        float duration = 1f / magInsertSpeed;
        
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;
            magTransform.localPosition = Vector3.Lerp(startPos, endPos, progress);
            yield return null;
        }
        
        magTransform.localPosition = endPos;
        Debug.Log("Magazine inserted!");
    }

    // YENİ: Bolt çekme animasyonu
    System.Collections.IEnumerator PullBolt()
    {
        if (boltTransform == null) yield break;
        
        Debug.Log("Pulling bolt...");
        
        Vector3 startPos = boltTransform.localPosition;
        Vector3 endPos = boltPulledPosition;
        float duration = 1f / boltPullSpeed;
        
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;
            boltTransform.localPosition = Vector3.Lerp(startPos, endPos, progress);
            yield return null;
        }
        
        boltTransform.localPosition = endPos;
        Debug.Log("Bolt pulled!");
    }

    // YENİ: Bolt serbest bırakma animasyonu
    System.Collections.IEnumerator ReleaseBolt()
    {
        if (boltTransform == null) yield break;
        
        Debug.Log("Releasing bolt...");
        
        Vector3 startPos = boltPulledPosition;
        Vector3 endPos = boltReleasedPosition;
        float duration = 1f / boltReleaseSpeed;
        
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float progress = t / duration;
            boltTransform.localPosition = Vector3.Lerp(startPos, endPos, progress);
            yield return null;
        }
        
        boltTransform.localPosition = endPos;
        Debug.Log("Bolt released!");
    }
} 