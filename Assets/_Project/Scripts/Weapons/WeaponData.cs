using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Informations générales")]
    [Tooltip("Nom de l'arme")]
    public string weaponName = "New Weapon";
    
    [Tooltip("Type d'arme")]
    public WeaponType weaponType = WeaponType.MeleeWeapon;
    
    [Tooltip("Sprite de l'arme")]
    public Sprite weaponSprite;
    
    [Header("Statistiques de l'arme")]
    [Tooltip("Dégâts infligés par l'arme")]
    [Range(0f, 100f)]
    public float damage = 10f;
    
    [Tooltip("Portée de l'attaque (pour les armes de mêlée)")]
    [Range(0f, 5f)]
    public float attackRange = 1f;
    
    [Tooltip("Durée pendant laquelle l'arme reste équipée (en secondes, 0 pour illimité)")]
    [Range(0f, 60f)]
    public float duration = 10f;
    
    [Header("Paramètres des projectiles (pour les armes à distance)")]
    [Tooltip("Prefab du projectile (pour les armes à distance)")]
    public GameObject projectilePrefab;
    
    [Tooltip("Vitesse du projectile")]
    [Range(0f, 50f)]
    public float projectileSpeed = 10f;
    
    [Header("Paramètres visuels")]
    [Tooltip("Échelle du sprite de l'arme")]
    public Vector3 weaponScale = Vector3.one;
    
    [Tooltip("Offset de position de l'arme par rapport au joueur")]
    public Vector3 weaponPositionOffset = new Vector3(0.5f, 0f, 0f);
}

public enum WeaponType
{
    MeleeWeapon,
    RangedWeapon,
    SpecialWeapon
}