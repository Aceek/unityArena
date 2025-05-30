using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Référence au SpriteRenderer pour afficher l'arme équipée")]
    [SerializeField] private SpriteRenderer weaponSpriteRenderer;
    
    [Tooltip("Référence à la caméra principale pour la visée avec la souris")]
    [SerializeField] private Camera mainCamera;
    
    [Header("Paramètres")]
    [Tooltip("Distance supplémentaire pour le spawn du projectile afin d'éviter les collisions immédiates")]
    [SerializeField] private float projectileSpawnDistance = 0.3f;
    
    [Tooltip("Délai entre les attaques (en secondes)")]
    [Range(0.1f, 2f)]
    [SerializeField] private float attackCooldown = 0.5f;
    
    [Tooltip("Référence à l'action d'attaque dans le Input System")]
    [SerializeField] private InputActionReference attackAction;
    
    [Header("Événements")]
    public UnityEvent<WeaponData> OnWeaponEquipped;
    public UnityEvent<float> OnWeaponTimerUpdated;
    public UnityEvent OnWeaponUnequipped;
    public UnityEvent OnWeaponAttack;
    
    private WeaponData equippedWeapon;
    private float weaponTimer;
    private bool canAttack = true;
    
    public bool HasWeapon => equippedWeapon != null;
    public WeaponData EquippedWeapon => equippedWeapon;
    
    private void Awake()
    {
        if (weaponSpriteRenderer == null)
        {
            GameObject weaponObject = new GameObject("EquippedWeapon");
            weaponObject.transform.SetParent(transform);
            weaponObject.transform.localPosition = Vector3.zero;
            
            weaponSpriteRenderer = weaponObject.AddComponent<SpriteRenderer>();
            weaponSpriteRenderer.sortingOrder = 1;
        }
        
        if (weaponSpriteRenderer != null)
        {
            weaponSpriteRenderer.enabled = false;
        }
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[PlayerWeapon] Awake: Aucune caméra principale trouvée !");
            }
        }
        
        if (attackAction != null)
        {
            attackAction.action.Enable();
            attackAction.action.performed += OnAttackPerformed;
        }
        else
        {
            Debug.LogWarning("[PlayerWeapon] Awake: Aucune action d'attaque assignée !");
        }
    }
    
    private void OnDestroy()
    {
        if (attackAction != null)
        {
            attackAction.action.performed -= OnAttackPerformed;
        }
    }
    
    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (HasWeapon && canAttack)
        {
            Attack();
        }
    }
    
    private void Update()
    {
        if (HasWeapon)
        {
            weaponTimer -= Time.deltaTime;
            OnWeaponTimerUpdated?.Invoke(weaponTimer);
            
            if (weaponTimer <= 0)
            {
                UnequipWeapon();
            }
            
            UpdateWeaponRotation();
        }
    }
    
    public void EquipWeapon(WeaponData weaponData)
    {
        if (HasWeapon)
        {
            UnequipWeapon();
        }
        
        equippedWeapon = weaponData;
        weaponTimer = weaponData.duration;
        
        if (weaponSpriteRenderer != null && weaponData.weaponSprite != null)
        {
            weaponSpriteRenderer.sprite = weaponData.weaponSprite;
            weaponSpriteRenderer.transform.localScale = weaponData.weaponScale;
            weaponSpriteRenderer.transform.localPosition = weaponData.weaponPositionOffset;
            weaponSpriteRenderer.enabled = true;
        }
        
        OnWeaponEquipped?.Invoke(weaponData);
        
        Debug.Log($"[PlayerWeapon] Arme '{weaponData.weaponName}' équipée pour {weaponData.duration} secondes");
    }
    
    public void UnequipWeapon()
    {
        if (HasWeapon)
        {
            Debug.Log($"[PlayerWeapon] Arme '{equippedWeapon.weaponName}' déséquipée");
            if (weaponSpriteRenderer != null)
            {
                weaponSpriteRenderer.enabled = false;
            }
            equippedWeapon = null;
            weaponTimer = 0;
            OnWeaponUnequipped?.Invoke();
        }
    }
    
    private void Attack()
    {
        if (!HasWeapon || !canAttack)
            return;
        
        Debug.Log($"[PlayerWeapon] Attaque avec '{equippedWeapon.weaponName}'");
        OnWeaponAttack?.Invoke();
        
        switch (equippedWeapon.weaponType)
        {
            case WeaponType.MeleeWeapon:
                StartCoroutine(PerformMeleeAttack());
                break;
                
            case WeaponType.RangedWeapon:
                StartCoroutine(PerformRangedAttack());
                break;
                
            case WeaponType.SpecialWeapon:
                Debug.Log("[PlayerWeapon] Attaque spéciale non implémentée");
                break;
        }
        
        StartCoroutine(AttackCooldown());
    }
    
    private IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }
    
    private IEnumerator PerformMeleeAttack()
    {
        Quaternion originalRotation = weaponSpriteRenderer.transform.localRotation;
        float attackDuration = 0.2f;
        float elapsedTime = 0;
        
        while (elapsedTime < attackDuration)
        {
            float rotationAngle = Mathf.Lerp(0, 90, elapsedTime / attackDuration);
            weaponSpriteRenderer.transform.localRotation = originalRotation * Quaternion.Euler(0, 0, rotationAngle);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, equippedWeapon.attackRange);
        foreach (Collider2D hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Enemy"))
            {
                Debug.Log($"[PlayerWeapon] Ennemi touché avec {equippedWeapon.damage} dégâts");
            }
        }
        
        elapsedTime = 0;
        while (elapsedTime < attackDuration)
        {
            float rotationAngle = Mathf.Lerp(90, 0, elapsedTime / attackDuration);
            weaponSpriteRenderer.transform.localRotation = originalRotation * Quaternion.Euler(0, 0, rotationAngle);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        weaponSpriteRenderer.transform.localRotation = originalRotation;
    }
    
    private IEnumerator PerformRangedAttack()
    {
        if (equippedWeapon.projectilePrefab == null)
        {
            Debug.LogError("[PlayerWeapon] PerformRangedAttack: Aucun prefab de projectile assigné !");
            yield break;
        }
        
        Vector2 direction = GetAimDirection();
        if (direction == Vector2.zero)
        {
            direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        }
        
        // Vérifier si la direction de tir est opposée à l'orientation du personnage
        if ((direction.x > 0 && transform.localScale.x < 0) || (direction.x < 0 && transform.localScale.x > 0))
        {
            transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
            Debug.Log($"[PlayerWeapon] Personnage retourné pour aligner avec la direction de tir: {direction}");
        }
        
        Vector3 spawnPosition = transform.position + equippedWeapon.weaponPositionOffset + (Vector3)(direction.normalized * projectileSpawnDistance);
        
        GameObject projectile = Instantiate(equippedWeapon.projectilePrefab, spawnPosition, Quaternion.identity);
        
        Projectile projectileScript = projectile.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(direction, equippedWeapon.projectileSpeed, equippedWeapon.damage);
        }
        else
        {
            Debug.LogError("[PlayerWeapon] PerformRangedAttack: Le prefab de projectile n'a pas de composant Projectile !");
            Destroy(projectile);
        }
        
        Quaternion originalRotation = weaponSpriteRenderer.transform.localRotation;
        float recoilDuration = 0.1f;
        float elapsedTime = 0;
        
        while (elapsedTime < recoilDuration)
        {
            float recoilAngle = Mathf.Lerp(0, -10, elapsedTime / recoilDuration);
            weaponSpriteRenderer.transform.localRotation = originalRotation * Quaternion.Euler(0, 0, recoilAngle);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        elapsedTime = 0;
        while (elapsedTime < recoilDuration)
        {
            float recoilAngle = Mathf.Lerp(-10, 0, elapsedTime / recoilDuration);
            weaponSpriteRenderer.transform.localRotation = originalRotation * Quaternion.Euler(0, 0, recoilAngle);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        weaponSpriteRenderer.transform.localRotation = originalRotation;
    }
    
    private Vector2 GetAimDirection()
    {
        CharacterMovement movement = GetComponent<CharacterMovement>();
        if (movement == null)
        {
            Debug.LogError("[PlayerWeapon] GetAimDirection: Aucun CharacterMovement trouvé !");
            return Vector2.zero;
        }
        
        Vector2 lookInput = movement.LookInput;
        
        if (Mouse.current != null && Mouse.current.position.ReadValue() != Vector2.zero)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, mainCamera.nearClipPlane));
            return (worldPosition - transform.position).normalized;
        }
        
        return lookInput.normalized;
    }
    
    private void UpdateWeaponRotation()
    {
        if (!HasWeapon || weaponSpriteRenderer == null)
            return;
            
        Vector2 direction = GetAimDirection();
        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            weaponSpriteRenderer.transform.localRotation = Quaternion.Euler(0, 0, angle);
            Vector3 position = equippedWeapon.weaponPositionOffset;
            if (transform.localScale.x < 0)
            {
                position.x = -position.x;
            }
            weaponSpriteRenderer.transform.localPosition = position;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (HasWeapon)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, equippedWeapon.attackRange);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + equippedWeapon.weaponPositionOffset, 0.1f);
        }
    }
}