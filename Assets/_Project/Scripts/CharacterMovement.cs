using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gère le mouvement du personnage en utilisant le nouveau système d'input d'Unity.
/// Ce script permet au personnage de se déplacer horizontalement, sauter et sprinter.
/// </summary>
public class CharacterMovement : MonoBehaviour, PlayerInputActions.IPlayerActions
{
    [Header("Références")]
    [Tooltip("Référence au composant Rigidbody2D du personnage")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Paramètres de Mouvement")]
    [Tooltip("Vitesse de déplacement normale du personnage")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("Multiplicateur de vitesse lors du sprint")]
    [SerializeField] private float sprintMultiplier = 1.5f;

    [Tooltip("Force appliquée lors du saut")]
    [SerializeField] private float jumpForce = 10f;

    [Tooltip("Couche utilisée pour détecter le sol")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("Distance du raycast pour vérifier si le personnage touche le sol")]
    [SerializeField] private float groundCheckDistance = 0.1f;

    // Variables privées
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private bool isSprinting;
    private bool isGrounded;
    private float currentSpeed;

    /// <summary>
    /// Initialise les composants et le système d'input.
    /// </summary>
    private void Awake()
    {
        // Initialiser le système d'input
        inputActions = new PlayerInputActions();

        // Si le Rigidbody2D n'est pas assigné, essayer de le récupérer automatiquement
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogError("Aucun Rigidbody2D trouvé sur le personnage. Veuillez en ajouter un.");
            }
        }
    }

    /// <summary>
    /// Active les inputs et enregistre les callbacks.
    /// </summary>
    private void OnEnable()
    {
        // Enregistrer cette classe pour recevoir les callbacks d'input
        inputActions.Player.SetCallbacks(this);

        // Activer la map d'actions "Player"
        inputActions.Player.Enable();
    }

    /// <summary>
    /// Désactive les inputs et supprime les callbacks.
    /// </summary>
    private void OnDisable()
    {
        // Désactiver la map d'actions "Player"
        inputActions.Player.Disable();
    }

    /// <summary>
    /// Mise à jour de la physique à intervalle fixe.
    /// </summary>
    private void FixedUpdate()
    {
        // Vérifier si le personnage est au sol
        CheckGrounded();

        // Appliquer le mouvement horizontal
        Move();
    }

    /// <summary>
    /// Vérifie si le personnage est en contact avec le sol.
    /// </summary>
    private void CheckGrounded()
    {
        // Obtenir le Collider2D du personnage (si présent)
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError("Aucun Collider2D trouvé sur le personnage!");
            return;
        }
        
        // Calculer la position du bas du collider
        Vector2 bottomPosition = new Vector2(
            transform.position.x,
            transform.position.y - collider.bounds.extents.y
        );
        
        // Lancer un raycast depuis le bas du collider vers le bas pour détecter le sol
        RaycastHit2D hit = Physics2D.Raycast(
            bottomPosition,  // Partir du bas du collider
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );
        
        // Mettre à jour l'état "au sol"
        isGrounded = hit.collider != null;
        
        // Afficher l'état de détection du sol dans la console
        Debug.Log($"isGrounded: {isGrounded}, hit: {(hit.collider != null ? hit.collider.name : "null")}, position: {bottomPosition}");
        
        // Dessiner le raycast pour le débogage visuel
        Debug.DrawRay(bottomPosition, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
        
        // Dessiner une ligne jaune au bas du collider pour visualisation
        Debug.DrawLine(
            new Vector3(bottomPosition.x - collider.bounds.extents.x, bottomPosition.y, 0),
            new Vector3(bottomPosition.x + collider.bounds.extents.x, bottomPosition.y, 0),
            Color.yellow
        );
    }

    /// <summary>
    /// Applique le mouvement horizontal au personnage.
    /// </summary>
    private void Move()
    {
        // Calculer la vitesse actuelle en fonction du sprint
        currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);

        // Créer le vecteur de vélocité
        Vector2 velocity = new Vector2(moveInput.x * currentSpeed, rb.linearVelocity.y);
        
        // Appliquer la vélocité au Rigidbody2D
        rb.linearVelocity = velocity;

        // Retourner le sprite en fonction de la direction (si nécessaire)
        if (moveInput.x != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(moveInput.x), 1, 1);
        }
    }

    /// <summary>
    /// Fait sauter le personnage si celui-ci est au sol.
    /// </summary>
    private void Jump()
    {
        if (isGrounded)
        {
            // Appliquer une force vers le haut pour sauter
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // Réinitialiser la vélocité verticale
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }

    #region Callbacks d'Input

    /// <summary>
    /// Callback appelé lorsque l'action Move est déclenchée.
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        // Lire la valeur de l'input de mouvement (Vector2)
        moveInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Callback appelé lorsque l'action Jump est déclenchée.
    /// </summary>
    public void OnJump(InputAction.CallbackContext context)
    {
        // Vérifier si le bouton vient d'être pressé
        if (context.performed)
        {
            Jump();
        }
    }

    /// <summary>
    /// Callback appelé lorsque l'action Sprint est déclenchée.
    /// </summary>
    public void OnSprint(InputAction.CallbackContext context)
    {
        // Mettre à jour l'état du sprint
        if (context.performed)
        {
            isSprinting = true;
        }
        else if (context.canceled)
        {
            isSprinting = false;
        }
    }

    // Implémentation des autres méthodes de l'interface IPlayerActions
    // Ces méthodes sont requises par l'interface mais ne sont pas utilisées dans ce script
    public void OnLook(InputAction.CallbackContext context) { }
    public void OnAttack(InputAction.CallbackContext context) { }
    public void OnInteract(InputAction.CallbackContext context) { }
    public void OnCrouch(InputAction.CallbackContext context) { }
    public void OnPrevious(InputAction.CallbackContext context) { }
    public void OnNext(InputAction.CallbackContext context) { }

    #endregion
}
