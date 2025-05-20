using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "ArenaKnight/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Informations de base")]
    [Tooltip("Nom de l'arme affiché dans l'UI")]
    public string weaponName = "Arme";
    
    [Tooltip("Description de l'arme")]
    [TextArea(2, 5)]
    public string description = "Description de l'arme";
    
    [Header("Apparence")]
    [Tooltip("Sprite de l'arme affiché sur la map et dans l'UI")]
    public Sprite weaponSprite;
    
    [Header("Paramètres")]
    [Tooltip("Durée en secondes pendant laquelle l'arme reste équipée")]
    [Range(1f, 30f)]
    public float duration = 10f;
    
    [Tooltip("Type d'arme (corps à corps, distance, etc.)")]
    public WeaponType weaponType = WeaponType.MeleeWeapon;
    
    [Header("Paramètres de combat")]
    [Tooltip("Dégâts infligés par l'arme")]
    public float damage = 10f;
    
    [Tooltip("Portée de l'attaque (pour les armes de corps à corps)")]
    public float attackRange = 1.5f;
    
    [Tooltip("Vitesse d'attaque (attaques par seconde)")]
    [Range(0.1f, 5f)]
    public float attackSpeed = 1f;
}

// Énumération pour les différents types d'armes
public enum WeaponType
{
    MeleeWeapon,    // Arme de corps à corps
    RangedWeapon,   // Arme à distance
    SpecialWeapon   // Arme spéciale avec effets uniques
}