using UnityEngine;
using UnityEngine.InputSystem;
// YENİ: PlayerInputActions için using static ekle
using static PlayerInputActions;

public class WeaponManager : MonoBehaviour
{
    [Header("Available Weapons")]
    public WeaponData[] availableWeapons; // 0=Pistol, 1=Shotgun, 2=Rifle
    
    [Header("Components")]
    public WeaponController weaponController;
    
    // Current weapon tracking
    private int currentWeaponIndex = 0;
    private PlayerInputActions inputActions;
    
    // Events
    public System.Action<WeaponData, int> OnWeaponChanged;
    
    void Awake()
    {
        // Initialize input - null check ekle
        try
        {
            inputActions = new PlayerInputActions();
            Debug.Log("WeaponManager InputActions initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize WeaponManager InputActions: {e.Message}");
            inputActions = null;
        }
    }
    
    void Start()
    {
        // YENİ: İlk silahı otomatik olarak yükle
        if (availableWeapons.Length > 0)
        {
            SwitchToWeapon(0); // İlk silahı (Pistol) yükle
            Debug.Log("WeaponManager: First weapon loaded");
        }
    }
    
    void OnEnable()
    {
        // Input actions'ı kontrol et
        if (inputActions != null)
        {
            inputActions.Player.Enable();
            
            // Subscribe to number key inputs
            inputActions.Player.Weapon1.performed += OnWeapon1;
            inputActions.Player.Weapon2.performed += OnWeapon2;
            inputActions.Player.Weapon3.performed += OnWeapon3;
            inputActions.Player.ScrollWeapon.performed += OnScrollWeapon;
        }
        else
        {
            Debug.LogWarning("InputActions is null in WeaponManager.OnEnable()");
        }
    }
    
    void OnDisable()
    {
        // Input actions'ı kontrol et
        if (inputActions != null)
        {
            inputActions.Player.Disable();
            
            // Unsubscribe
            inputActions.Player.Weapon1.performed -= OnWeapon1;
            inputActions.Player.Weapon2.performed -= OnWeapon2;
            inputActions.Player.Weapon3.performed -= OnWeapon3;
            inputActions.Player.ScrollWeapon.performed -= OnScrollWeapon;
        }
        else
        {
            Debug.LogWarning("InputActions is null in WeaponManager.OnDisable()");
        }
    }
    
    void OnWeapon1(InputAction.CallbackContext context)
    {
        SwitchToWeapon(0); // Pistol
    }
    
    void OnWeapon2(InputAction.CallbackContext context)
    {
        SwitchToWeapon(1); // Shotgun
    }
    
    void OnWeapon3(InputAction.CallbackContext context)
    {
        SwitchToWeapon(2); // Rifle
    }
    
    void OnScrollWeapon(InputAction.CallbackContext context)
    {
        float scrollValue = context.ReadValue<float>();
        
        if (scrollValue > 0) // Scroll up
        {
            NextWeapon();
        }
        else if (scrollValue < 0) // Scroll down
        {
            PreviousWeapon();
        }
    }
    
    public void SwitchToWeapon(int weaponIndex)
    {
        // Validate index
        if (weaponIndex < 0 || weaponIndex >= availableWeapons.Length)
            return;
        
        // Don't switch if already current weapon
        if (weaponIndex == currentWeaponIndex)
            return;
        
        // Switch weapon
        currentWeaponIndex = weaponIndex;
        WeaponData newWeapon = availableWeapons[currentWeaponIndex];
        
        // Update weapon controller
        if (weaponController != null)
        {
            weaponController.SwitchWeapon(weaponIndex); // YENİ: SwitchWeapon kullan
        }
        
        // Fire event
        OnWeaponChanged?.Invoke(newWeapon, currentWeaponIndex);
        
    }
    
    public void NextWeapon()
    {
        int nextIndex = (currentWeaponIndex + 1) % availableWeapons.Length;
        SwitchToWeapon(nextIndex);
    }
    
    public void PreviousWeapon()
    {
        int prevIndex = (currentWeaponIndex - 1 + availableWeapons.Length) % availableWeapons.Length;
        SwitchToWeapon(prevIndex);
    }
    
    // Getters
    public WeaponData GetCurrentWeapon() => availableWeapons[currentWeaponIndex];
    public int GetCurrentWeaponIndex() => currentWeaponIndex;
    public string GetCurrentWeaponName() => availableWeapons[currentWeaponIndex].weaponName;
}
