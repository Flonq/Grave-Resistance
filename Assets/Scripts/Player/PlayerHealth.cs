using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    
    [Header("Damage Settings")]
    public float damageFlashDuration = 0.1f;
    
    // Events
    public System.Action<float, float> OnHealthChanged; // currentHealth, maxHealth
    public System.Action OnPlayerDeath;
    
    // Private variables
    private float lastDamageTime;
    private bool isDead = false;
    
    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    void Update()
    {
        // Auto health regeneration kaldırıldı
        // Manual health pickup sistemi kullanılacak
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        // lastDamageTime = Time.time; // KALDIR (regen için gereksiz)
        
        // Stop health regeneration kodları KALDIR
        
        // Trigger events
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        Debug.Log($"Player healed {amount}! Health: {currentHealth}/{maxHealth}");
    }
    
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        // Trigger GameManager Game Over
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
        
        OnPlayerDeath?.Invoke();
    }
    
    // Public getters
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public bool IsDead() => isDead;
    
    // Reset health (for respawn)
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
