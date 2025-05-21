using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class CharacterMovement : MonoBehaviour, PlayerInputActions.IPlayerActions
{
    #region États du personnage
    // Machine à états pour gérer les différents états du personnage
    private enum PlayerState 
    { 
        Idle,       // Au repos
        Running,    // En déplacement
        Jumping,    // En saut ascendant
        Falling,    // En chute
        Sliding,    // En glissade
        FastFalling // En descente rapide
    }
    private PlayerState currentState = PlayerState.Idle;
    #endregion

    #region Références de composants
    [Header("Références")]
    [Tooltip("Référence au composant Rigidbody2D du personnage")]
    [SerializeField] private Rigidbody2D rb;

    [Tooltip("Référence au composant Animator du personnage")]
    [SerializeField] private Animator animator;

    [Tooltip("Système de particules pour l'effet de glissade")]
    [SerializeField] private ParticleSystem slideVFX;

    [Tooltip("Point de vérification du sol (à ajouter comme enfant du personnage)")]
    [SerializeField] private Transform groundCheckPoint;
    #endregion

    #region Paramètres de mouvement
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

    [Tooltip("Largeur de la boîte de détection du sol")]
    [SerializeField] private float groundCheckWidth = 0.8f;

    [Tooltip("Hauteur de la boîte de détection du sol")]
    [SerializeField] private float groundCheckHeight = 0.1f;

    [Tooltip("Distance horizontale pour vérifier les collisions latérales")]
    [SerializeField] private float sideCheckDistance = 0.05f;
    #endregion

    #region Paramètres de Coyote Time et Jump Buffer
    [Header("Paramètres de Coyote Time et Jump Buffer")]
    [Tooltip("Durée pendant laquelle le joueur peut encore sauter après avoir quitté une plateforme")]
    [SerializeField] private float coyoteTime = 0.1f;
    private float coyoteTimeCounter;

    [Tooltip("Durée pendant laquelle l'input de saut est mémorisé avant d'atterrir")]
    [SerializeField] private float jumpBufferTime = 0.1f;
    private float jumpBufferCounter;
    #endregion

    #region Paramètres de Glissade
    [Header("Paramètres de Glissade")]
    [Tooltip("Multiplicateur de vitesse pour la glissade (basé sur la vitesse actuelle)")]
    [SerializeField] private float slideSpeedMultiplier = 1.5f;

    [Tooltip("Durée de la glissade (en secondes)")]
    [SerializeField] private float slideDuration = 0.5f;

    [Tooltip("Délai avant de pouvoir glisser à nouveau (en secondes)")]
    [SerializeField] private float slideCooldown = 1f;

    [Tooltip("Position relative du VFX de glissade par rapport au personnage")]
    [SerializeField] private Vector3 slideVFXOffset = new Vector3(0f, -0.5f, 0f);
    #endregion

    #region Paramètres de Saut pendant Glissade
    [Header("Paramètres de Saut pendant Glissade")]
    [Tooltip("Multiplicateur de la force de saut pendant une glissade")]
    [SerializeField] private float slideJumpMultiplier = 1.3f;

    [Tooltip("Force horizontale supplémentaire pour le saut pendant une glissade")]
    [SerializeField] private float slideJumpForwardForce = 5f;
    #endregion

    #region Variables privées
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isSprinting;
    private bool isGrounded;
    private bool isFastFalling;
    private float currentSpeed;
    private float lastJumpTime;
    private bool isSliding;
    private float lastSlideTime;
    private Coroutine slideCoroutine;
    private bool wasSliding; // Pour détecter si on était en glissade avant un saut
    private int facingDirection = 1; // 1 = droite, -1 = gauche
    #endregion

    #region Debug
    [Header("Debug")]
    [Tooltip("Activer les logs de débogage pour la détection du sol")]
    [SerializeField] private bool debugGroundCheck = false;
    #endregion

    public Vector2 LookInput => lookInput;

    #region Méthodes Unity
    private void Awake()
    {
        // Initialisation des composants
        inputActions = new PlayerInputActions();
        
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
            
        if (animator == null)
            animator = GetComponent<Animator>();
            
        if (groundCheckPoint == null)
        {
            // Créer un point de vérification du sol s'il n'existe pas
            GameObject checkPoint = new GameObject("GroundCheckPoint");
            checkPoint.transform.parent = transform;
            checkPoint.transform.localPosition = new Vector3(0, -GetComponent<Collider2D>().bounds.extents.y, 0);
            groundCheckPoint = checkPoint.transform;
            Debug.Log("Point de vérification du sol créé automatiquement. Vous pouvez ajuster sa position dans l'inspecteur.");
        }
    }

    private void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new PlayerInputActions();
        }
        inputActions.Player.SetCallbacks(this);
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Update()
    {
        // Gestion du Coyote Time et Jump Buffer
        UpdateTimers();
        
        // Mise à jour de l'état du personnage
        UpdateState();
    }

    private void FixedUpdate()
    {
        // Vérification du sol
        CheckGrounded();
        
        // Appliquer les comportements en fonction de l'état
        ApplyStateEffects();
    }
    #endregion

    #region Gestion des états
    private void UpdateState()
    {
        // Déterminer le nouvel état
        PlayerState newState = DetermineState();
        
        // Si l'état a changé
        if (newState != currentState)
        {
            // Sortir de l'état actuel
            ExitState(currentState);
            
            // Entrer dans le nouvel état
            EnterState(newState);
            
            // Mettre à jour l'état actuel
            currentState = newState;
        }
    }

    private PlayerState DetermineState()
    {
        // Déterminer l'état en fonction des conditions actuelles
        if (isSliding)
            return PlayerState.Sliding;
            
        if (isFastFalling && !isGrounded)
            return PlayerState.FastFalling;
            
        if (!isGrounded)
        {
            if (rb.linearVelocity.y > 0.1f)
                return PlayerState.Jumping;
            else
                return PlayerState.Falling;
        }
        
        if (Mathf.Abs(moveInput.x) > 0.1f)
            return PlayerState.Running;
            
        return PlayerState.Idle;
    }

    private void EnterState(PlayerState state)
    {
        // Actions à effectuer lors de l'entrée dans un état
        switch (state)
        {
            case PlayerState.Idle:
                animator.SetBool("isRunning", false);
                break;
                
            case PlayerState.Running:
                animator.SetBool("isRunning", true);
                break;
                
            case PlayerState.Jumping:
                animator.SetBool("isJumping", true);
                break;
                
            case PlayerState.Falling:
                animator.SetBool("isJumping", true);
                break;
                
            case PlayerState.Sliding:
                animator.SetBool("isSliding", true);
                break;
                
            case PlayerState.FastFalling:
                // Actions spécifiques pour la descente rapide
                break;
        }
    }

    private void ExitState(PlayerState state)
    {
        // Actions à effectuer lors de la sortie d'un état
        switch (state)
        {
            case PlayerState.Idle:
                // Rien de spécial à faire
                break;
                
            case PlayerState.Running:
                // Rien de spécial à faire
                break;
                
            case PlayerState.Jumping:
                // Rien de spécial à faire
                break;
                
            case PlayerState.Falling:
                if (isGrounded)
                    animator.SetBool("isJumping", false);
                break;
                
            case PlayerState.Sliding:
                animator.SetBool("isSliding", false);
                if (slideVFX != null)
                    slideVFX.Stop();
                break;
                
            case PlayerState.FastFalling:
                // Rien de spécial à faire
                break;
        }
    }

    private void ApplyStateEffects()
    {
        // Appliquer les effets en fonction de l'état actuel
        switch (currentState)
        {
            case PlayerState.Idle:
                // Rien de spécial à faire
                break;
                
            case PlayerState.Running:
                Move();
                break;
                
            case PlayerState.Jumping:
                Move(); // Permettre le contrôle en l'air
                break;
                
            case PlayerState.Falling:
                Move(); // Permettre le contrôle en l'air
                break;
                
            case PlayerState.Sliding:
                // La glissade est gérée par la coroutine PerformSlide
                break;
                
            case PlayerState.FastFalling:
                Move(); // Permettre le contrôle en l'air
                ApplyFastFall();
                break;
        }
    }
    #endregion

    #region Timers et compteurs
    private void UpdateTimers()
    {
        // Gestion du Coyote Time
        if (isGrounded)
            coyoteTimeCounter = coyoteTime;
        else
            coyoteTimeCounter -= Time.deltaTime;
            
        // Gestion du Jump Buffer
        if (jumpBufferCounter > 0)
            jumpBufferCounter -= Time.deltaTime;
    }
    #endregion

    #region Détection du sol
    private void CheckGrounded()
    {
        // Vérifier si le personnage est au sol en utilisant OverlapBox
        Collider2D[] colliders = Physics2D.OverlapBoxAll(
            groundCheckPoint.position,
            new Vector2(groundCheckWidth, groundCheckHeight),
            0f,
            groundLayer
        );
        
        // Le personnage est au sol s'il y a au moins une collision
        bool wasGrounded = isGrounded;
        isGrounded = colliders.Length > 0;
        
        // Si on vient d'atterrir
        if (!wasGrounded && isGrounded)
        {
            // Exécuter le saut si le buffer de saut est actif
            if (jumpBufferCounter > 0)
            {
                Jump();
                jumpBufferCounter = 0;
            }
        }
        
        // Vérifier les collisions latérales
        CheckWallCollisions();
        
        // Mettre à jour l'animator
        animator.SetBool("isGrounded", isGrounded);
        
        // Debug
        if (debugGroundCheck)
        {
            Color rayColor = isGrounded ? Color.green : Color.red;
            // Visualiser la boîte de détection du sol
            Vector3 boxCenter = groundCheckPoint.position;
            Vector3 boxSize = new Vector3(groundCheckWidth, groundCheckHeight, 0.1f);
            Debug.DrawLine(boxCenter + new Vector3(-boxSize.x/2, -boxSize.y/2, 0), boxCenter + new Vector3(boxSize.x/2, -boxSize.y/2, 0), rayColor);
            Debug.DrawLine(boxCenter + new Vector3(boxSize.x/2, -boxSize.y/2, 0), boxCenter + new Vector3(boxSize.x/2, boxSize.y/2, 0), rayColor);
            Debug.DrawLine(boxCenter + new Vector3(boxSize.x/2, boxSize.y/2, 0), boxCenter + new Vector3(-boxSize.x/2, boxSize.y/2, 0), rayColor);
            Debug.DrawLine(boxCenter + new Vector3(-boxSize.x/2, boxSize.y/2, 0), boxCenter + new Vector3(-boxSize.x/2, -boxSize.y/2, 0), rayColor);
        }
    }

    private void CheckWallCollisions()
    {
        // Vérifier les collisions latérales
        Collider2D collider = GetComponent<Collider2D>();
        Vector2 bottomCenter = new Vector2(transform.position.x, transform.position.y - collider.bounds.extents.y);
        RaycastHit2D hitSideLeft = Physics2D.Raycast(bottomCenter, Vector2.left, sideCheckDistance, groundLayer);
        RaycastHit2D hitSideRight = Physics2D.Raycast(bottomCenter, Vector2.right, sideCheckDistance, groundLayer);
        
        // Debug
        if (debugGroundCheck)
        {
            bool isAgainstWall = hitSideLeft.collider != null || hitSideRight.collider != null;
            Color wallColor = isAgainstWall ? Color.yellow : Color.white;
            Debug.DrawRay(bottomCenter, Vector2.left * sideCheckDistance, hitSideLeft.collider != null ? Color.yellow : Color.white);
            Debug.DrawRay(bottomCenter, Vector2.right * sideCheckDistance, hitSideRight.collider != null ? Color.yellow : Color.white);
        }
    }
    #endregion

    #region Mouvement
    private void Move()
    {
        // Calculer la vitesse actuelle
        currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);
        
        // Calculer la vélocité
        Vector2 velocity = new Vector2(moveInput.x * currentSpeed, rb.linearVelocity.y);
        
        // Vérifier les collisions latérales
        Collider2D collider = GetComponent<Collider2D>();
        Vector2 bottomCenter = new Vector2(transform.position.x, transform.position.y - collider.bounds.extents.y);
        RaycastHit2D hitSideLeft = Physics2D.Raycast(bottomCenter, Vector2.left, sideCheckDistance, groundLayer);
        RaycastHit2D hitSideRight = Physics2D.Raycast(bottomCenter, Vector2.right, sideCheckDistance, groundLayer);
        bool isAgainstWall = hitSideLeft.collider != null || hitSideRight.collider != null;
        
        // Empêcher de "grimper" les murs
        if (isAgainstWall && !isGrounded && Mathf.Abs(moveInput.x) > 0)
        {
            velocity.x = 0;
        }
        
        // Appliquer la vélocité
        rb.linearVelocity = velocity;
        
        // Mettre à jour la direction du personnage
        if (moveInput.x != 0)
        {
            facingDirection = (int)Mathf.Sign(moveInput.x);
            transform.localScale = new Vector3(facingDirection, 1, 1);
        }
    }

    private void ApplyFastFall()
    {
        if (isFastFalling && !isGrounded)
        {
            rb.AddForce(Vector2.down * fastFallForce, ForceMode2D.Force);
        }
    }
    #endregion

    #region Saut
    private void Jump()
    {
        // Vérifier si on peut sauter (au sol ou en coyote time, ou en glissade)
        if (isGrounded || coyoteTimeCounter > 0 || isSliding)
        {
            // Stocker l'état de glissade avant de l'arrêter
            wasSliding = isSliding;
            
            // Arrêter la glissade si en cours
            if (isSliding && slideCoroutine != null)
            {
                StopCoroutine(slideCoroutine);
                isSliding = false;
                animator.SetBool("isSliding", false);
                if (slideVFX != null)
                {
                    slideVFX.Stop();
                }
                Debug.Log("[CharacterMovement] Glissade arrêtée pour saut");
            }

            // Calculer la force de saut
            float appliedJumpForce = wasSliding ? jumpForce * slideJumpMultiplier : jumpForce;
            
            if (wasSliding)
            {
                // Si le joueur appuie sur une touche de direction, utiliser cette direction
                // Sinon, utiliser la direction actuelle du personnage
                float direction = Mathf.Abs(moveInput.x) > 0.1f ? 
                    Mathf.Sign(moveInput.x) : 
                    facingDirection;
                
                // Augmenter significativement la force horizontale pendant un slide
                float forwardForce = slideJumpForwardForce * 5f; // Multiplié par 5 pour un effet très prononcé
                
                // Réinitialiser complètement la vélocité avant d'appliquer les forces
                rb.linearVelocity = Vector2.zero;
                
                // Appliquer la force verticale (saut)
                rb.AddForce(Vector2.up * appliedJumpForce, ForceMode2D.Impulse);
                
                // Appliquer la force horizontale en tenant compte de la direction
                Vector2 horizontalForce = new Vector2(direction * forwardForce, 0);
                rb.AddForce(horizontalForce, ForceMode2D.Impulse);
                
                // Définir directement une vélocité horizontale minimale pour garantir le mouvement
                Vector2 currentVel = rb.linearVelocity;
                float minHorizontalSpeed = 8f * direction; // Augmenté à 8 pour garantir un mouvement horizontal fort
                if (Mathf.Abs(currentVel.x) < Mathf.Abs(minHorizontalSpeed))
                {
                    rb.linearVelocity = new Vector2(minHorizontalSpeed, currentVel.y);
                }
                
                Debug.Log($"[CharacterMovement] Saut pendant glissade: jumpForce={appliedJumpForce}, " +
                          $"forwardForce={forwardForce}, direction={direction}, " +
                          $"velocity après={rb.linearVelocity}");
            }
            else
            {
                // Comportement normal de saut
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * appliedJumpForce, ForceMode2D.Impulse);
                Debug.Log($"[CharacterMovement] Saut standard: force={appliedJumpForce}");
            }

            // Mettre à jour l'animator et les variables
            animator.SetBool("isJumping", true);
            lastJumpTime = Time.time;
            coyoteTimeCounter = 0; // Réinitialiser le coyote time
        }
        else
        {
            // Si on ne peut pas sauter maintenant, mémoriser l'input pour le jump buffer
            jumpBufferCounter = jumpBufferTime;
        }
    }

    private void CutJump()
    {
        // Réduire la hauteur du saut si le bouton est relâché en plein saut
        if (!isGrounded && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }
    #endregion

    #region Glissade
    private void Slide()
    {
        if (isGrounded && !isSliding && Time.time - lastSlideTime >= slideCooldown)
        {
            slideCoroutine = StartCoroutine(PerformSlide());
        }
    }

    private IEnumerator PerformSlide()
    {
        isSliding = true;
        lastSlideTime = Time.time;
        animator.SetBool("isSliding", true);

        // Calculer la vitesse de glissade basée sur la vitesse actuelle
        float slideSpeed = currentSpeed * slideSpeedMultiplier;
        float slideDirection = moveInput.x != 0 ? Mathf.Sign(moveInput.x) : facingDirection;
        Vector2 slideVelocity = new Vector2(slideDirection * slideSpeed, rb.linearVelocity.y);

        // Activer le VFX
        if (slideVFX != null)
        {
            slideVFX.transform.localPosition = slideVFXOffset;
            slideVFX.Play();
        }

        Debug.Log($"[CharacterMovement] Début glissade: speed={slideSpeed}, direction={slideDirection}");

        float elapsedTime = 0f;
        while (elapsedTime < slideDuration)
        {
            rb.linearVelocity = slideVelocity;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Arrêter la glissade
        isSliding = false;
        animator.SetBool("isSliding", false);
        if (slideVFX != null)
        {
            slideVFX.Stop();
        }
        slideCoroutine = null;
        Debug.Log("[CharacterMovement] Fin glissade");
    }
    #endregion

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
        else if (context.canceled)
        {
            CutJump();
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

    public void OnSlide(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Slide();
        }
    }

    public void OnAttack(InputAction.CallbackContext context) { }
    public void OnInteract(InputAction.CallbackContext context) { }
    public void OnCrouch(InputAction.CallbackContext context) { }
    public void OnPrevious(InputAction.CallbackContext context) { }
    public void OnNext(InputAction.CallbackContext context) { }
    #endregion

    #region Gizmos
    private void OnDrawGizmos()
    {
        // Dessiner la boîte de détection du sol dans l'éditeur
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheckPoint.position, new Vector3(groundCheckWidth, groundCheckHeight, 0.1f));
        }
    }
    #endregion
}