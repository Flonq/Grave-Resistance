using UnityEngine;
using UnityEngine.InputSystem;

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
        // Initialize input
        inputActions = new PlayerInputActions();
    }
    
    void Start()
    {
        // Start with first weapon (Pistol)
        if (availableWeapons.Length > 0)
        {
            SwitchToWeapon(0);
        }
    }
    
    void OnEnable()
    {
        inputActions.Player.Enable();
        
        // Subscribe to number key inputs
        inputActions.Player.Weapon1.performed += OnWeapon1;
        inputActions.Player.Weapon2.performed += OnWeapon2;
        inputActions.Player.Weapon3.performed += OnWeapon3;
        inputActions.Player.ScrollWeapon.performed += OnScrollWeapon;
    }
    
    void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Disable();
            
            // Unsubscribe
            inputActions.Player.Weapon1.performed -= OnWeapon1;
            inputActions.Player.Weapon2.performed -= OnWeapon2;
            inputActions.Player.Weapon3.performed -= OnWeapon3;
            inputActions.Player.ScrollWeapon.performed -= OnScrollWeapon;
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
            weaponController.SetCurrentWeapon(newWeapon);
        }
        
        // Fire event
        OnWeaponChanged?.Invoke(newWeapon, currentWeaponIndex);
        
        Debug.Log($"Switched to: {newWeapon.weaponName}");
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
