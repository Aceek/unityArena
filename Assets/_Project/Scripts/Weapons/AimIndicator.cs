using UnityEngine;

public class AimIndicator : MonoBehaviour
{
    [Tooltip("Distance du viseur par rapport au joueur")]
    [SerializeField] private float distanceFromPlayer = 1f;
    
    [Tooltip("Référence au joueur pour obtenir la direction de visée")]
    [SerializeField] private CharacterMovement player;
    
    private void Awake()
    {
        if (player == null)
        {
            player = GetComponentInParent<CharacterMovement>();
            if (player == null)
            {
                Debug.LogError("[AimIndicator] Awake: Aucun CharacterMovement trouvé !");
            }
        }
    }
    
    private void Update()
    {
        if (player == null)
            return;
            
        Vector2 direction = player.LookInput;
        if (direction == Vector2.zero)
        {
            // Si pas de visée, masquer le viseur
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        transform.localPosition = direction.normalized * distanceFromPlayer;
    }
}