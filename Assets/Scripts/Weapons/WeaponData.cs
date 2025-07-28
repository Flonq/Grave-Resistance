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

    [Header("Model Settings")]
    public GameObject weaponModelPrefab;
    public Vector3 modelPosition = Vector3.zero;
    public Vector3 modelRotation = Vector3.zero;
    public Vector3 modelScale = Vector3.one;

    [Header("Animation Settings")]
    public float fireAnimationSpeed = 1f;
    public float reloadAnimationSpeed = 1f;

    [Header("Fire Mode Settings")]
    public FireMode fireMode = FireMode.Single;

    [Header("Shotgun Spread Settings")]
    public int pelletCount = 8; // Kaç tane pellet atılacak
    public float spreadAngle = 15f; // Saçılma açısı (derece)
    public bool isShotgun = false; // Bu silah shotgun mu?
}

public enum WeaponType
{
    Melee,
    Ranged
}

public enum FireMode
{
    Single,     // Tek tek atış
    Auto        // Otomatik
}
