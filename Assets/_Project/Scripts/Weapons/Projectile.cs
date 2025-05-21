using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [Header("Paramètres")]
    [Tooltip("Vitesse du projectile (définie par WeaponData)")]
    [SerializeField] private float speed = 10f;
    
    [Tooltip("Dégâts infligés par le projectile")]
    [SerializeField] private float damage = 10f;
    
    [Tooltip("Durée de vie du projectile avant destruction (en secondes)")]
    [SerializeField] private float lifetime = 5f;
    
    private Rigidbody2D rb;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("[Projectile] Awake: Aucun Rigidbody2D trouvé !");
        }
    }
    
    private void Start()
    {
        // Détruire le projectile après sa durée de vie
        Destroy(gameObject, lifetime);
    }
    
    // Initialiser le projectile avec la direction, la vitesse et les dégâts
    public void Initialize(Vector2 direction, float projectileSpeed, float projectileDamage)
    {
        speed = projectileSpeed;
        damage = projectileDamage;
        
        // Appliquer la vitesse au Rigidbody2D
        rb.linearVelocity = direction.normalized * speed;
        
        // Orienter le sprite (optionnel, selon ton style visuel)
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Vérifier si le projectile touche un ennemi
        if (other.CompareTag("Enemy"))
        {
            // Appliquer des dégâts (à implémenter selon ton système d'ennemis)
            Debug.Log($"[Projectile] Ennemi touché avec {damage} dégâts");
            // Exemple : other.GetComponent<Enemy>().TakeDamage(damage);
            
            // Détruire le projectile après collision
            Destroy(gameObject);
        }
        // Optionnel : Détruire le projectile s'il touche un mur ou un obstacle
        else if (other.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}