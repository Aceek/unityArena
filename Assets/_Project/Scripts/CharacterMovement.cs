using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gère le mouvement du personnage en utilisant le nouveau système d'input d'Unity.
/// Permet le déplacement horizontal, le saut à hauteur variable, le sprint et la descente rapide.
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

    [Tooltip("Multiplicateur pour réduire la hauteur du saut si la touche est relâchée")]
    [SerializeField] private float jumpCutMultiplier = 0.4f;

    [Tooltip("Force appliquée pour la descente rapide")]
    [SerializeField] private float fastFallForce = 10f;

    [Tooltip("Couche utilisée pour détecter le sol")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("Distance du raycast pour vérifier si le personnage touche le sol")]
    [SerializeField] private float groundCheckDistance = 0.1f;

    // Variables privées
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private bool isSprinting;
    private bool isGrounded;
    private bool isFastFalling;
    private float currentSpeed;

    [Header("Debug")]
    [Tooltip("Activer les logs de débogage pour la détection du sol")]
    [SerializeField] private bool debugGroundCheck = false;

    /// <summary>
    /// Initialise les composants et le système d'input.
    /// </summary>
    private void Awake()
    {
        // Initialiser le système d'input
        inputActions = new PlayerInputActions();

        // Vérifier le Rigidbody2D
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
        inputActions.Player.SetCallbacks(this);
        inputActions.Player.Enable();
    }

    /// <summary>
    /// Désactive les inputs et supprime les callbacks.
    /// </summary>
    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    /// <summary>
    /// Mise à jour de la physique à intervalle fixe.
    /// </summary>
    private void FixedUpdate()
    {
        CheckGrounded();
        Move();
        ApplyFastFall();
    }

    /// <summary>
    /// Vérifie si le personnage est en contact avec le sol en utilisant plusieurs raycasts.
    /// </summary>
    private void CheckGrounded()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError("Aucun Collider2D trouvé sur le personnage!");
            return;
        }

        // Calculer les positions des raycasts (gauche, centre, droite du collider)
        Vector2 bottomCenter = new Vector2(transform.position.x, transform.position.y - collider.bounds.extents.y);
        Vector2 bottomLeft = bottomCenter - new Vector2(collider.bounds.extents.x * 0.8f, 0);
        Vector2 bottomRight = bottomCenter + new Vector2(collider.bounds.extents.x * 0.8f, 0);

        // Lancer trois raycasts pour une détection plus robuste
        RaycastHit2D hitCenter = Physics2D.Raycast(bottomCenter, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitLeft = Physics2D.Raycast(bottomLeft, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(bottomRight, Vector2.down, groundCheckDistance, groundLayer);

        // Le personnage est au sol si au moins un raycast touche le sol
        isGrounded = hitCenter.collider != null || hitLeft.collider != null || hitRight.collider != null;

        // Débogage conditionnel
        if (debugGroundCheck)
        {
            Debug.Log($"isGrounded: {isGrounded}, hitCenter: {(hitCenter.collider != null ? hitCenter.collider.name : "null")}");
        }

        // Dessiner les raycasts pour le débogage visuel
        Color rayColor = isGrounded ? Color.green : Color.red;
        Debug.DrawRay(bottomCenter, Vector2.down * groundCheckDistance, rayColor);
        Debug.DrawRay(bottomLeft, Vector2.down * groundCheckDistance, rayColor);
        Debug.DrawRay(bottomRight, Vector2.down * groundCheckDistance, rayColor);

        // Dessiner une ligne jaune au bas du collider
        Debug.DrawLine(
            new Vector3(bottomLeft.x, bottomLeft.y, 0),
            new Vector3(bottomRight.x, bottomRight.y, 0),
            Color.yellow
        );
    }

    /// <summary>
    /// Applique le mouvement horizontal au personnage.
    /// </summary>
    private void Move()
    {
        currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);
        Vector2 velocity = new Vector2(moveInput.x * currentSpeed, rb.linearVelocity.y);
        rb.linearVelocity = velocity;

        // Retourner le sprite en fonction de la direction
        if (moveInput.x != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(moveInput.x), 1, 1);
        }
    }

    /// <summary>
    /// Applique la descente rapide si active.
    /// </summary>
    private void ApplyFastFall()
    {
        if (isFastFalling && !isGrounded)
        {
            // Appliquer une force vers le bas
            rb.AddForce(Vector2.down * fastFallForce, ForceMode2D.Force);
        }
    }

    /// <summary>
    /// Fait sauter le personnage si celui-ci est au sol.
    /// </summary>
    private void Jump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }

    #region Callbacks d'Input

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Jump();
        }
        else if (context.canceled && !isGrounded && rb.linearVelocity.y > 0)
        {
            // Réduire la vélocité verticale pour un saut à hauteur variable
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        isSprinting = context.performed;
    }

    public void OnFastFall(InputAction.CallbackContext context)
    {
        isFastFalling = context.performed && !isGrounded;
    }

    // Méthodes inutilisées de l'interface
    public void OnLook(InputAction.CallbackContext context) { }
    public void OnAttack(InputAction.CallbackContext context) { }
    public void OnInteract(InputAction.CallbackContext context) { }
    public void OnCrouch(InputAction.CallbackContext context) { }
    public void OnPrevious(InputAction.CallbackContext context) { }
    public void OnNext(InputAction.CallbackContext context) { }

    #endregion
}