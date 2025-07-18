using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon Info")]
    public string weaponName = "Pistol";
    public WeaponType weaponType = WeaponType.Ranged;
    
    [Header("Damage Settings")]
    public float damage = 25f;
    public float range = 50f;
    public float fireRate = 0.5f; // Time between shots
    
    [Header("Ammo Settings")]
    public int maxAmmo = 30;
    public int currentAmmo = 30;
    public int reserveAmmo = 90;
    public float reloadTime = 2f;
    
    [Header("Visual & Audio")]
    public GameObject weaponPrefab;
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public ParticleSystem muzzleFlash;
}

public enum WeaponType
{
    Melee,
    Ranged
}
