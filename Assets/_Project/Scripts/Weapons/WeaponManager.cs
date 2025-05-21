using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Liste des spawners d'armes dans la scène")]
    [SerializeField] private List<WeaponSpawner> spawners;
    
    [Tooltip("Prefab de l'arme à spawner sur la map")]
    [SerializeField] private GameObject weaponPickupPrefab;
    
    [Header("Configuration des armes")]
    [Tooltip("Liste des armes disponibles pour le spawn")]
    [SerializeField] private List<WeaponData> availableWeapons = new List<WeaponData>();
    
    [Header("Paramètres de spawn")]
    [Tooltip("Nombre maximum d'armes actives sur la map")]
    [Range(1, 10)]
    [SerializeField] private int maxActiveWeapons = 3;
    
    [Tooltip("Délai initial avant le premier spawn (en secondes)")]
    [Range(0f, 5f)]
    [SerializeField] private float initialSpawnDelay = 2f;
    
    private List<GameObject> activeWeapons = new List<GameObject>();
    
    public int ActiveWeaponsCount => activeWeapons.Count;
    
    private void Awake()
    {
        if (spawners == null || spawners.Count == 0)
        {
            Debug.LogError("[WeaponManager] Awake: Aucun spawner assigné !");
        }
        
        if (weaponPickupPrefab == null)
        {
            Debug.LogError("[WeaponManager] Awake: Aucun prefab d'arme assigné !");
        }
        
        if (availableWeapons.Count == 0)
        {
            Debug.LogWarning("[WeaponManager] Awake: Aucune arme disponible pour le spawn.");
        }
    }
    
    private void Start()
    {
        // Initialiser les spawners
        foreach (WeaponSpawner spawner in spawners)
        {
            spawner.Initialize(this);
        }
        
        // Lancer le spawn initial
        StartCoroutine(InitialSpawnRoutine());
    }
    
    private IEnumerator InitialSpawnRoutine()
    {
        yield return new WaitForSeconds(initialSpawnDelay);
        SpawnWeapons();
    }
    
    private void SpawnWeapons()
    {
        if (ActiveWeaponsCount >= maxActiveWeapons)
        {
            return;
        }
        
        // Mélanger les spawners pour un spawn aléatoire
        List<WeaponSpawner> availableSpawners = new List<WeaponSpawner>(spawners);
        availableSpawners.Shuffle();
        
        int weaponsToSpawn = Mathf.Min(maxActiveWeapons - ActiveWeaponsCount, availableSpawners.Count);
        for (int i = 0; i < weaponsToSpawn && availableWeapons.Count > 0; i++)
        {
            WeaponData weaponData = availableWeapons[Random.Range(0, availableWeapons.Count)];
            availableSpawners[i].SpawnWeapon(weaponData);
        }
    }
    
    public void OnWeaponPickedUp(GameObject weaponObject)
    {
        if (activeWeapons.Contains(weaponObject))
        {
            activeWeapons.Remove(weaponObject);
            Debug.Log($"[WeaponManager] Arme ramassée. Armes actives restantes : {activeWeapons.Count}");
        }
    }
    
    public void RequestWeaponSpawn(WeaponSpawner spawner)
    {
        if (ActiveWeaponsCount < maxActiveWeapons && availableWeapons.Count > 0)
        {
            WeaponData weaponData = availableWeapons[Random.Range(0, availableWeapons.Count)];
            spawner.SpawnWeapon(weaponData);
            activeWeapons.Add(spawner.GetComponentInChildren<WeaponPickup>().gameObject);
        }
    }
}

// Extension pour mélanger une liste
public static class ListExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}