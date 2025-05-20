using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Référence au WrapAroundManager pour obtenir les dimensions de la map")]
    [SerializeField] private WrapAroundManager wrapAroundManager;
    
    [Tooltip("Prefab de l'arme à spawner sur la map")]
    [SerializeField] private GameObject weaponPickupPrefab;
    
    [Header("Configuration des armes")]
    [Tooltip("Liste des armes disponibles pour le spawn")]
    [SerializeField] private List<WeaponData> availableWeapons = new List<WeaponData>();
    
    [Header("Paramètres de spawn")]
    [Tooltip("Nombre maximum d'armes actives sur la map")]
    [Range(1, 10)]
    [SerializeField] private int maxActiveWeapons = 3;
    
    [Tooltip("Délai minimum entre les spawns d'armes (en secondes)")]
    [Range(1f, 30f)]
    [SerializeField] private float minSpawnDelay = 5f;
    
    [Tooltip("Délai maximum entre les spawns d'armes (en secondes)")]
    [Range(1f, 30f)]
    [SerializeField] private float maxSpawnDelay = 15f;
    
    [Tooltip("Distance minimale de spawn par rapport aux bords de la map")]
    [Range(0.5f, 5f)]
    [SerializeField] private float borderOffset = 1f;
    
    // Liste des armes actuellement actives sur la map
    private List<GameObject> activeWeapons = new List<GameObject>();
    
    // Propriété pour accéder au nombre d'armes actives
    public int ActiveWeaponsCount => activeWeapons.Count;
    
    private void Start()
    {
        // Trouver automatiquement le WrapAroundManager si non assigné
        if (wrapAroundManager == null)
        {
            wrapAroundManager = FindFirstObjectByType<WrapAroundManager>();
            if (wrapAroundManager == null)
            {
                Debug.LogError("[WeaponManager] Start: Aucun WrapAroundManager trouvé dans la scène ! Veuillez l'assigner manuellement.");
                return;
            }
        }
        
        // Vérifier si le prefab d'arme est assigné
        if (weaponPickupPrefab == null)
        {
            Debug.LogError("[WeaponManager] Start: Aucun prefab d'arme assigné ! Veuillez assigner un prefab avec le composant WeaponPickup.");
            return;
        }
        
        // Vérifier si des armes sont disponibles
        if (availableWeapons.Count == 0)
        {
            Debug.LogWarning("[WeaponManager] Start: Aucune arme disponible pour le spawn. Veuillez assigner des WeaponData.");
            return;
        }
        
        // Démarrer la coroutine de spawn d'armes
        StartCoroutine(SpawnWeaponsRoutine());
    }
    
    private IEnumerator SpawnWeaponsRoutine()
    {
        // Attendre que le WrapAroundManager soit initialisé
        while (!wrapAroundManager.IsInitialized())
        {
            yield return new WaitForSeconds(0.5f);
            Debug.Log("[WeaponManager] En attente de l'initialisation du WrapAroundManager...");
        }
        
        while (true)
        {
            // Vérifier si on peut spawner une nouvelle arme
            if (activeWeapons.Count < maxActiveWeapons)
            {
                SpawnWeapon();
            }
            
            // Attendre un délai aléatoire avant le prochain spawn
            float delay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(delay);
        }
    }
    
    private void SpawnWeapon()
    {
        // Vérifier si le WrapAroundManager est initialisé
        if (!wrapAroundManager.IsInitialized())
        {
            Debug.LogWarning("[WeaponManager] SpawnWeapon: WrapAroundManager non initialisé. Impossible de spawner une arme.");
            return;
        }
        
        // Obtenir les dimensions de la map
        Vector2 mapMin = wrapAroundManager.MapMin;
        Vector2 mapMax = wrapAroundManager.MapMax;
        
        // Calculer une position aléatoire dans les limites de la map
        float x = Random.Range(mapMin.x + borderOffset, mapMax.x - borderOffset);
        float y = Random.Range(mapMin.y + borderOffset, mapMax.y - borderOffset);
        Vector2 spawnPosition = new Vector2(x, y);
        
        // Sélectionner une arme aléatoire
        WeaponData weaponData = availableWeapons[Random.Range(0, availableWeapons.Count)];
        
        // Instancier le prefab d'arme
        GameObject weaponInstance = Instantiate(weaponPickupPrefab, spawnPosition, Quaternion.identity);
        
        // Configurer le WeaponPickup avec les données de l'arme
        WeaponPickup weaponPickup = weaponInstance.GetComponent<WeaponPickup>();
        if (weaponPickup != null)
        {
            weaponPickup.Initialize(weaponData, this);
        }
        else
        {
            Debug.LogError("[WeaponManager] SpawnWeapon: Le prefab d'arme ne contient pas de composant WeaponPickup !");
            Destroy(weaponInstance);
            return;
        }
        
        // Ajouter l'arme à la liste des armes actives
        activeWeapons.Add(weaponInstance);
        
        Debug.Log($"[WeaponManager] Arme '{weaponData.weaponName}' spawnée à la position {spawnPosition}");
    }
    
    // Méthode appelée par WeaponPickup lorsqu'une arme est ramassée
    public void OnWeaponPickedUp(GameObject weaponObject)
    {
        if (activeWeapons.Contains(weaponObject))
        {
            activeWeapons.Remove(weaponObject);
            Debug.Log($"[WeaponManager] Arme ramassée. Armes actives restantes : {activeWeapons.Count}");
        }
    }
}