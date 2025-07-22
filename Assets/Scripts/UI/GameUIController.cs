using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUIController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI ammoText;
    public Slider healthBar; // Eski sistem (kaldırabiliriz)
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI weaponIndicator;

    [Header("Heart Health System")]
    public Image[] heartImages; // 5 heart image array

    [Header("Animation Settings")]
    public float healthBarAnimationSpeed = 3f; // Animation hızı

    // Private animation variables
    private float targetHealthValue = 1f;
    private float currentHealthValue = 1f;

    [Header("Components")]
    public WeaponController weaponController;
    public PlayerHealth playerHealth;
    public WeaponManager weaponManager;
    
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
        
        // Smooth health bar animation
        AnimateHealthBar();
    }

    void AnimateHealthBar()
    {
        if (healthBar != null)
        {
            // Lerp current value towards target
            currentHealthValue = Mathf.Lerp(currentHealthValue, targetHealthValue, 
                                           healthBarAnimationSpeed * Time.deltaTime);
            
            // Update health bar with smooth value
            healthBar.value = currentHealthValue;
        }
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
        // Set target value instead of direct assignment
        targetHealthValue = currentHealth / maxHealth;
        
        // Don't update healthBar.value directly anymore
        // Animation will handle it in AnimateHealthBar()
    }

    void UpdateHeartDisplay(float currentHealth, float maxHealth)
    {
        if (heartImages == null || heartImages.Length == 0) return;
        
        // Calculate hearts (each heart = 20 health for 100 max health)
        float healthPerHeart = maxHealth / heartImages.Length; // 100/5 = 20
        
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;
            
            float heartThreshold = (i + 1) * healthPerHeart; // 20, 40, 60, 80, 100
            
            if (currentHealth >= heartThreshold)
            {
                // Full heart - Red
                heartImages[i].color = Color.red;
            }
            else if (currentHealth > i * healthPerHeart)
            {
                // Partial heart - Orange (optional)
                heartImages[i].color = new Color(1f, 0.5f, 0f, 1f); // Orange
            }
            else
            {
                // Empty heart - Gray
                heartImages[i].color = Color.gray;
            }
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
