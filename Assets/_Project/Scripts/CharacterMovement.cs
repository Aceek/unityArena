using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Gère le mouvement du personnage et la visée en utilisant le nouveau système d'input d'Unity.
/// </summary>
public class CharacterMovement : MonoBehaviour, PlayerInputActions.IPlayerActions
{
    [Header("Références")]
    [Tooltip("Référence au composant Rigidbody2D du personnage")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("Référence au composant Animator du personnage")]
    [SerializeField] private Animator animator;

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
    [SerializeField] private float groundCheckDistance = 0.15f;

    [Tooltip("Distance horizontale pour vérifier les collisions latérales")]
    [SerializeField] private float sideCheckDistance = 0.05f;

    [Tooltip("Délai après un saut pendant lequel on ignore la détection du sol")]
    [SerializeField] private float jumpGraceTime = 0.1f;

    // Variables privées
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput; // Nouvelle variable pour la visée
    private bool isSprinting;
    private bool isGrounded;
    private bool isFastFalling;
    private float currentSpeed;
    private float lastJumpTime;
    private bool ignoreGroundCheck;

    [Header("Debug")]
    [Tooltip("Activer les logs de débogage pour la détection du sol")]
    [SerializeField] private bool debugGroundCheck = false;

    // Propriété publique pour accéder à la direction de visée
    public Vector2 LookInput => lookInput;

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
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Aucun Animator trouvé sur le personnage. Veuillez en ajouter un.");
            }
        }
    }

    private void OnEnable()
    {
        inputActions.Player.SetCallbacks(this);
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        Move();
        ApplyFastFall();
    }

    private void CheckGrounded()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError("Aucun Collider2D trouvé sur le personnage!");
            return;
        }

        if (ignoreGroundCheck && Time.time - lastJumpTime < jumpGraceTime)
        {
            isGrounded = false;
            animator.SetBool("isGrounded", isGrounded);
            return;
        }
        else
        {
            ignoreGroundCheck = false;
        }

        Vector2 bottomCenter = new Vector2(transform.position.x, transform.position.y - collider.bounds.extents.y);
        Vector2 bottomLeft = bottomCenter - new Vector2(collider.bounds.extents.x * 0.8f, 0);
        Vector2 bottomRight = bottomCenter + new Vector2(collider.bounds.extents.x * 0.8f, 0);

        RaycastHit2D hitCenter = Physics2D.Raycast(bottomCenter, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitLeft = Physics2D.Raycast(bottomLeft, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(bottomRight, Vector2.down, groundCheckDistance, groundLayer);

        isGrounded = hitCenter.collider != null || hitLeft.collider != null || hitRight.collider != null;

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

        animator.SetBool("isGrounded", isGrounded);

        if (isGrounded)
        {
            animator.SetBool("isJumping", false);
        }
    }

    private void Move()
    {
        currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);
        Vector2 velocity = new Vector2(moveInput.x * currentSpeed, rb.linearVelocity.y);

        Collider2D collider = GetComponent<Collider2D>();
        Vector2 bottomCenter = new Vector2(transform.position.x, transform.position.y - collider.bounds.extents.y);
        RaycastHit2D hitSideLeft = Physics2D.Raycast(bottomCenter, Vector2.left, sideCheckDistance, groundLayer);
        RaycastHit2D hitSideRight = Physics2D.Raycast(bottomCenter, Vector2.right, sideCheckDistance, groundLayer);
        bool isAgainstWall = hitSideLeft.collider != null || hitSideRight.collider != null;

        if (isAgainstWall && !isGrounded && Mathf.Abs(moveInput.x) > 0)
        {
            velocity.x = 0;
        }

        rb.linearVelocity = velocity;

        bool isMoving = Mathf.Abs(moveInput.x) > 0;
        animator.SetBool("isRunning", isMoving);

        if (moveInput.x != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(moveInput.x), 1, 1);
        }
    }

    private void ApplyFastFall()
    {
        if (isFastFalling && !isGrounded)
        {
            rb.AddForce(Vector2.down * fastFallForce, ForceMode2D.Force);
        }
    }

    private void Jump()
    {
        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            animator.SetBool("isJumping", true);
            lastJumpTime = Time.time;
            ignoreGroundCheck = true;
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

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnAttack(InputAction.CallbackContext context) { }
    public void OnInteract(InputAction.CallbackContext context) { }
    public void OnCrouch(InputAction.CallbackContext context) { }
    public void OnPrevious(InputAction.CallbackContext context) { }
    public void OnNext(InputAction.CallbackContext context) { }

    #endregion
}