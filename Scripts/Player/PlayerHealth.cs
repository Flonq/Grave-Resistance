using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float healthRegenRate = 5f; // Health per second
    public float healthRegenDelay = 3f; // Delay after taking damage
    
    [Header("Damage Settings")]
    public float damageFlashDuration = 0.1f;
    
    // Events
    public System.Action<float, float> OnHealthChanged; // currentHealth, maxHealth
    public System.Action OnPlayerDeath;
    
    // Private variables
    private float lastDamageTime;
    private bool isDead = false;
    private Coroutine regenCoroutine;
    
    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    void Update()
    {
        // Auto health regeneration
        if (!isDead && currentHealth < maxHealth && Time.time - lastDamageTime > healthRegenDelay)
        {
            if (regenCoroutine == null)
            {
                regenCoroutine = StartCoroutine(RegenerateHealth());
            }
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        lastDamageTime = Time.time;
        
        // Stop health regeneration
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }
        
        // Trigger events
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        Debug.Log($"Player took {damage} damage! Health: {currentHealth}/{maxHealth}");
        
        // Check for death
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
    
    IEnumerator RegenerateHealth()
    {
        while (currentHealth < maxHealth && !isDead)
        {
            currentHealth += healthRegenRate * Time.deltaTime;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            yield return null;
        }
        
        regenCoroutine = null;
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
