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
    [SerializeField] private float fastFallForce = 7f;

    [Tooltip("Couche utilisée pour détecter le sol")]
    [SerializeField] private LayerMask groundLayer;

    [Tooltip("Distance du raycast pour vérifier si le personnage touche le sol")]
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Tooltip("Distance horizontale pour vérifier les collisions latérales")]
    [SerializeField] private float sideCheckDistance = 0.05f;

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
        inputActions = new PlayerInputActions();
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
    /// Vérifie si le personnage est en contact avec le sol ou un bloc latéral.
    /// </summary>
    private void CheckGrounded()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError("Aucun Collider2D trouvé sur le personnage!");
            return;
        }

        Vector2 bottomCenter = new Vector2(transform.position.x, transform.position.y - collider.bounds.extents.y);
        Vector2 bottomLeft = bottomCenter - new Vector2(collider.bounds.extents.x * 0.8f, 0);
        Vector2 bottomRight = bottomCenter + new Vector2(collider.bounds.extents.x * 0.8f, 0);

        RaycastHit2D hitCenter = Physics2D.Raycast(bottomCenter, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitLeft = Physics2D.Raycast(bottomLeft, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(bottomRight, Vector2.down, groundCheckDistance, groundLayer);

        isGrounded = hitCenter.collider != null || hitLeft.collider != null || hitRight.collider != null;

        // Vérifier les collisions latérales
        RaycastHit2D hitSideLeft = Physics2D.Raycast(bottomCenter, Vector2.left, sideCheckDistance, groundLayer);
        RaycastHit2D hitSideRight = Physics2D.Raycast(bottomCenter, Vector2.right, sideCheckDistance, groundLayer);
        bool isAgainstWall = hitSideLeft.collider != null || hitSideRight.collider != null;

        if (debugGroundCheck)
        {
            Debug.Log($"isGrounded: {isGrounded}, isAgainstWall: {isAgainstWall}, hitCenter: {(hitCenter.collider != null ? hitCenter.collider.name : "null")}");
        }

        Color rayColor = isGrounded ? Color.green : Color.red;
        Debug.DrawRay(bottomCenter, Vector2.down * groundCheckDistance, rayColor);
        Debug.DrawRay(bottomLeft, Vector2.down * groundCheckDistance, rayColor);
        Debug.DrawRay(bottomRight, Vector2.down * groundCheckDistance, rayColor);
        Debug.DrawRay(bottomCenter, Vector2.left * sideCheckDistance, isAgainstWall ? Color.yellow : Color.white);
        Debug.DrawRay(bottomCenter, Vector2.right * sideCheckDistance, isAgainstWall ? Color.yellow : Color.white);

        Debug.DrawLine(bottomLeft, bottomRight, Color.yellow);
    }

    /// <summary>
    /// Applique le mouvement horizontal au personnage.
    /// </summary>
    private void Move()
    {
        currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);
        Vector2 velocity = new Vector2(moveInput.x * currentSpeed, rb.linearVelocity.y);

        // Vérifier les collisions latérales
        Collider2D collider = GetComponent<Collider2D>();
        Vector2 bottomCenter = new Vector2(transform.position.x, transform.position.y - collider.bounds.extents.y);
        RaycastHit2D hitSideLeft = Physics2D.Raycast(bottomCenter, Vector2.left, sideCheckDistance, groundLayer);
        RaycastHit2D hitSideRight = Physics2D.Raycast(bottomCenter, Vector2.right, sideCheckDistance, groundLayer);
        bool isAgainstWall = hitSideLeft.collider != null || hitSideRight.collider != null;

        // Si contre un mur et en l'air, arrêter le mouvement horizontal pour forcer la chute
        if (isAgainstWall && !isGrounded && Mathf.Abs(moveInput.x) > 0)
        {
            velocity.x = 0; // Arrêter le mouvement horizontal
        }

        rb.linearVelocity = velocity;

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

    public void OnLook(InputAction.CallbackContext context) { }
    public void OnAttack(InputAction.CallbackContext context) { }
    public void OnInteract(InputAction.CallbackContext context) { }
    public void OnCrouch(InputAction.CallbackContext context) { }
    public void OnPrevious(InputAction.CallbackContext context) { }
    public void OnNext(InputAction.CallbackContext context) { }

    #endregion
}