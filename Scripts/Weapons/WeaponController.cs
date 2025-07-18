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
    
    // Components
    private Camera playerCamera;
    private AudioSource audioSource;
    
    // Shooting variables
    private float nextFireTime;
    private bool isReloading;
    
    void Start()
    {
        // Get components
        playerCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();
        
        // Initialize weapon - RESET VALUES!
        if (currentWeapon != null)
        {
            // Reset to original values every game start
            currentWeapon.currentAmmo = currentWeapon.maxAmmo;
            currentWeapon.reserveAmmo = 48; // Or whatever starting reserve you want
            
            Debug.Log($"Weapon initialized: {currentWeapon.currentAmmo}/{currentWeapon.reserveAmmo}");
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
        // Get shooting direction from camera center
        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;
        
        RaycastHit hit;
        
        // Visualize the ray
        Debug.DrawRay(rayOrigin, rayDirection * currentWeapon.range, Color.red, 0.1f);
        
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, currentWeapon.range, enemyLayers))
        {
            Debug.Log($"Hit: {hit.collider.name}");
            
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
}
