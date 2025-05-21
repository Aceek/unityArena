using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("Effets visuels")]
    [Tooltip("Vitesse de rotation de l'arme sur la map")]
    [Range(0f, 360f)]
    [SerializeField] private float rotationSpeed = 90f;
    
    [Tooltip("Amplitude du mouvement de flottement")]
    [Range(0f, 1f)]
    [SerializeField] private float floatAmplitude = 0.2f;
    
    [Tooltip("Fréquence du mouvement de flottement")]
    [Range(0.1f, 5f)]
    [SerializeField] private float floatFrequency = 1f;
    
    private WeaponData weaponData;
    private WeaponManager weaponManager;
    private Vector3 initialPosition;
    
    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        else
        {
            Debug.LogError("[WeaponPickup] Awake: Aucun Collider2D trouvé ! Veuillez ajouter un Collider2D au prefab d'arme.");
        }
    }
    
    private void Start()
    {
        initialPosition = transform.position;
    }
    
    private void Update()
    {
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        float newY = initialPosition.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
    
    public void Initialize(WeaponData data, WeaponManager manager)
    {
        weaponData = data;
        weaponManager = manager;
        
        if (spriteRenderer != null && weaponData != null && weaponData.weaponSprite != null)
        {
            spriteRenderer.sprite = weaponData.weaponSprite;
            spriteRenderer.transform.localScale = weaponData.weaponScale;
        }
        else
        {
            Debug.LogWarning("[WeaponPickup] Initialize: Sprite de l'arme non défini !");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerWeapon playerWeapon = other.GetComponent<PlayerWeapon>();
        if (playerWeapon != null && weaponData != null)
        {
            playerWeapon.EquipWeapon(weaponData);
            Debug.Log($"[WeaponPickup] Arme '{weaponData.weaponName}' ramassée par le joueur");
            
            // Notifier le WeaponSpawner parent
            WeaponSpawner spawner = GetComponentInParent<WeaponSpawner>();
            if (spawner != null)
            {
                spawner.OnWeaponPickedUp();
            }
            
            // Notifier le WeaponManager
            if (weaponManager != null)
            {
                weaponManager.OnWeaponPickedUp(gameObject);
            }
            
            // Désactiver l'objet
            gameObject.SetActive(false);
        }
    }
}