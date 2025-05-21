using UnityEngine;
using System.Collections;

public class WeaponSpawner : MonoBehaviour
{
    [Header("Paramètres du spawner")]
    [Tooltip("Offset de position pour l'arme par rapport au socle")]
    [SerializeField] private Vector3 weaponOffset = new Vector3(0f, 0.5f, 0f);
    
    [Tooltip("Délai minimum avant le respawn de l'arme (en secondes)")]
    [Range(1f, 30f)]
    [SerializeField] private float minRespawnDelay = 5f;
    
    [Tooltip("Délai maximum avant le respawn de l'arme (en secondes)")]
    [Range(1f, 30f)]
    [SerializeField] private float maxRespawnDelay = 15f;
    
    [Header("Références")]
    [Tooltip("SpriteRenderer du socle (reste visible)")]
    [SerializeField] private SpriteRenderer baseSpriteRenderer;
    
    [Tooltip("Prefab de l'arme à spawner")]
    [SerializeField] private GameObject weaponPickupPrefab;
    
    private GameObject currentWeapon;
    private WeaponManager weaponManager;
    private bool isSpawning = false;
    
    private void Awake()
    {
        if (baseSpriteRenderer == null)
        {
            Debug.LogError("[WeaponSpawner] Awake: Aucun SpriteRenderer assigné pour le socle !");
        }
        
        if (weaponPickupPrefab == null)
        {
            Debug.LogError("[WeaponSpawner] Awake: Aucun prefab d'arme assigné !");
        }
    }
    
    // Initialiser le spawner avec le WeaponManager
    public void Initialize(WeaponManager manager)
    {
        weaponManager = manager;
    }
    
    // Faire spawner une arme avec WeaponData
    public void SpawnWeapon(WeaponData weaponData)
    {
        if (isSpawning || currentWeapon != null)
        {
            return;
        }
        
        if (weaponData == null)
        {
            Debug.LogWarning("[WeaponSpawner] SpawnWeapon: Aucune WeaponData fournie !");
            return;
        }
        
        isSpawning = true;
        
        // Instancier l'arme
        currentWeapon = Instantiate(weaponPickupPrefab, transform.position + weaponOffset, Quaternion.identity);
        currentWeapon.transform.SetParent(transform);
        
        // Configurer le WeaponPickup
        WeaponPickup pickup = currentWeapon.GetComponent<WeaponPickup>();
        if (pickup != null)
        {
            pickup.Initialize(weaponData, weaponManager);
        }
        else
        {
            Debug.LogError("[WeaponSpawner] SpawnWeapon: Le prefab d'arme n'a pas de composant WeaponPickup !");
            Destroy(currentWeapon);
            isSpawning = false;
            return;
        }
        
        Debug.Log($"[WeaponSpawner] Arme '{weaponData.weaponName}' spawnée à {transform.position + weaponOffset}");
        isSpawning = false;
    }
    
    // Appelé par WeaponPickup lorsque l'arme est ramassée
    public void OnWeaponPickedUp()
    {
        if (currentWeapon != null)
        {
            currentWeapon = null;
            Debug.Log($"[WeaponSpawner] Arme ramassée, socle reste à {transform.position}");
            
            if (minRespawnDelay > 0 && maxRespawnDelay > 0)
            {
                StartCoroutine(RespawnWeapon());
            }
        }
    }
    
    private IEnumerator RespawnWeapon()
    {
        float delay = Random.Range(minRespawnDelay, maxRespawnDelay);
        yield return new WaitForSeconds(delay);
        
        // Demander au WeaponManager une nouvelle arme
        if (weaponManager != null)
        {
            weaponManager.RequestWeaponSpawn(this);
        }
    }
}