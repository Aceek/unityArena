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
    
    // Données de l'arme
    private WeaponData weaponData;
    
    // Référence au WeaponManager
    private WeaponManager weaponManager;
    
    // Position initiale pour le mouvement de flottement
    private Vector3 initialPosition;
    
    private void Awake()
    {
        // Récupérer le SpriteRenderer si non assigné
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // S'assurer que le GameObject a un Collider2D configuré comme trigger
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
        // Enregistrer la position initiale pour le mouvement de flottement
        initialPosition = transform.position;
    }
    
    private void Update()
    {
        // Faire tourner l'arme
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        
        // Faire flotter l'arme
        float newY = initialPosition.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
    
    // Initialiser le pickup avec les données de l'arme
    public void Initialize(WeaponData data, WeaponManager manager)
    {
        weaponData = data;
        weaponManager = manager;
        
        // Configurer le sprite
        if (spriteRenderer != null && weaponData != null && weaponData.weaponSprite != null)
        {
            spriteRenderer.sprite = weaponData.weaponSprite;
        }
        else
        {
            Debug.LogWarning("[WeaponPickup] Initialize: Sprite de l'arme non défini !");
        }
    }
    
    // Détecter quand un joueur entre en contact avec l'arme
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Vérifier si c'est le joueur qui touche l'arme
        PlayerWeapon playerWeapon = other.GetComponent<PlayerWeapon>();
        if (playerWeapon != null && weaponData != null)
        {
            // Équiper l'arme au joueur
            playerWeapon.EquipWeapon(weaponData);
            
            // Notifier le WeaponManager que l'arme a été ramassée
            if (weaponManager != null)
            {
                weaponManager.OnWeaponPickedUp(gameObject);
            }
            
            // Désactiver l'objet (plutôt que de le détruire pour une réutilisation potentielle)
            gameObject.SetActive(false);
            
            Debug.Log($"[WeaponPickup] Arme '{weaponData.weaponName}' ramassée par le joueur");
        }
    }
}