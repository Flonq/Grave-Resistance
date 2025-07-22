using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUIController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI ammoText;
    public Slider healthBar;
    public TextMeshProUGUI scoreText; // YENİ
    public TextMeshProUGUI weaponIndicator; // YENİ
    public WeaponController weaponController;
    public PlayerHealth playerHealth;
    public WeaponManager weaponManager; // YENİ
    
    void Start()
    {
        // Subscribe to events
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthUI;
        }
        
        // Subscribe to GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += UpdateScoreUI;
            GameManager.Instance.OnKillCountChanged += UpdateScoreUI;
        }
        
        // Subscribe to weapon changes
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged += UpdateWeaponUI;
        }
    }
    
    void Update()
    {
        UpdateAmmoUI();
    }
    
    void UpdateAmmoUI()
    {
        if (weaponController != null && ammoText != null)
        {
            int current = weaponController.GetCurrentAmmo();
            int reserve = weaponController.GetReserveAmmo();
            ammoText.text = $"{current} / {reserve}";
        }
    }
    
    void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
    }
    
    void UpdateScoreUI(int value)
    {
        if (scoreText != null && GameManager.Instance != null)
        {
            scoreText.text = $"Kills: {GameManager.Instance.killCount} | Score: {GameManager.Instance.playerScore}";
        }
    }
    
    void UpdateWeaponUI(WeaponData weapon, int index)
    {
        if (weaponIndicator != null)
        {
            weaponIndicator.text = $"{index + 1}. {weapon.weaponName}";
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from health events
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthUI;
        }
        
        // Unsubscribe from GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= UpdateScoreUI;
            GameManager.Instance.OnKillCountChanged -= UpdateScoreUI;
        }
        
        // Unsubscribe from weapon events
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged -= UpdateWeaponUI;
        }
    }
}
