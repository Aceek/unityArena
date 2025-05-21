using UnityEngine;
using UnityEngine.InputSystem;

public class AimIndicator : MonoBehaviour
{
    [Tooltip("Distance du viseur par rapport au joueur")]
    [SerializeField] private float distanceFromPlayer = 1.5f;
    
    [Tooltip("Référence au joueur pour obtenir la position de base")]
    [SerializeField] private CharacterMovement player;
    
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    
    private void Awake()
    {
        // Récupérer le SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("[AimIndicator] Awake: Aucun SpriteRenderer trouvé sur AimIndicator !");
        }
        
        // Trouver la caméra principale
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[AimIndicator] Awake: Aucune caméra principale trouvée !");
        }
        
        // Vérifier la référence au joueur
        if (player == null)
        {
            player = GetComponentInParent<CharacterMovement>();
            if (player == null)
            {
                Debug.LogError("[AimIndicator] Awake: Aucun CharacterMovement trouvé dans les parents !");
            }
        }
        
        // Activer le viseur par défaut
        gameObject.SetActive(true);
    }
    
    private void Update()
    {
        if (player == null || mainCamera == null)
        {
            Debug.LogWarning("[AimIndicator] Update: Références manquantes (joueur ou caméra) !");
            gameObject.SetActive(false);
            return;
        }
        
        // Obtenir la direction de visée basée sur la souris
        Vector2 direction = GetAimDirection();
        
        // Si pas de direction de visée, utiliser l'orientation du joueur
        if (direction == Vector2.zero)
        {
            direction = player.transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            Debug.Log("[AimIndicator] Aucune direction de visée détectée, utilisant orientation du joueur");
        }
        
        // Positionner le viseur en espace monde
        Vector2 playerPosition = player.transform.position;
        transform.position = playerPosition + (direction.normalized * distanceFromPlayer);
        
        // Orienter le sprite pour suivre la direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
    
    private Vector2 GetAimDirection()
    {
        // Priorité à la souris
        if (Mouse.current != null && Mouse.current.position.ReadValue() != Vector2.zero)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, mainCamera.nearClipPlane));
            Vector2 direction = ((Vector2)worldPosition - (Vector2)player.transform.position).normalized;
            return direction;
        }
        
        // Fallback au joystick (optionnel)
        if (player != null)
        {
            Vector2 lookInput = player.LookInput;
            return lookInput.normalized;
        }
        
        return Vector2.zero;
    }
}