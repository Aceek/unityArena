using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Référence au SpriteRenderer pour afficher l'arme équipée")]
    [SerializeField] private SpriteRenderer weaponSpriteRenderer;
    
    [Header("Paramètres")]
    [Tooltip("Position de l'arme par rapport au joueur")]
    [SerializeField] private Vector3 weaponOffset = new Vector3(0.5f, 0f, 0f);
    
    [Tooltip("Délai entre les attaques (en secondes)")]
    [Range(0.1f, 2f)]
    [SerializeField] private float attackCooldown = 0.5f;
    
    [Tooltip("Référence à l'action d'attaque dans le Input System")]
    [SerializeField] private InputActionReference attackAction;
    
    // Événements pour la communication avec d'autres composants
    [Header("Événements")]
    public UnityEvent<WeaponData> OnWeaponEquipped;
    public UnityEvent<float> OnWeaponTimerUpdated;
    public UnityEvent OnWeaponUnequipped;
    public UnityEvent OnWeaponAttack;
    
    // Données de l'arme actuellement équipée
    private WeaponData equippedWeapon;
    
    // Timer pour la durée de l'arme
    private float weaponTimer;
    
    // Flag pour savoir si le joueur peut attaquer
    private bool canAttack = true;
    
    // Propriété pour vérifier si le joueur a une arme équipée
    public bool HasWeapon => equippedWeapon != null;
    
    // Propriété pour accéder aux données de l'arme équipée
    public WeaponData EquippedWeapon => equippedWeapon;
    
    private void Awake()
    {
        // Initialiser le SpriteRenderer de l'arme s'il n'est pas assigné
        if (weaponSpriteRenderer == null)
        {
            // Créer un GameObject enfant pour l'arme si nécessaire
            GameObject weaponObject = new GameObject("EquippedWeapon");
            weaponObject.transform.SetParent(transform);
            weaponObject.transform.localPosition = weaponOffset;
            
            // Ajouter un SpriteRenderer
            weaponSpriteRenderer = weaponObject.AddComponent<SpriteRenderer>();
            weaponSpriteRenderer.sortingOrder = 1; // S'assurer que l'arme est au-dessus du joueur
        }
        
        // Désactiver le SpriteRenderer au départ (pas d'arme équipée)
        if (weaponSpriteRenderer != null)
        {
            weaponSpriteRenderer.enabled = false;
        }
        
        // Activer l'action d'attaque si elle est assignée
        if (attackAction != null)
        {
            attackAction.action.Enable();
            attackAction.action.performed += OnAttackPerformed;
        }
        else
        {
            Debug.LogWarning("[PlayerWeapon] Awake: Aucune action d'attaque assignée ! Veuillez assigner une InputActionReference.");
        }
    }
    
    private void OnDestroy()
    {
        // Se désabonner de l'événement d'attaque
        if (attackAction != null)
        {
            attackAction.action.performed -= OnAttackPerformed;
        }
    }
    
    // Méthode appelée lorsque l'action d'attaque est déclenchée
    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (HasWeapon && canAttack)
        {
            Attack();
        }
    }
    
    private void Update()
    {
        // Si le joueur a une arme équipée
        if (HasWeapon)
        {
            // Mettre à jour le timer
            weaponTimer -= Time.deltaTime;
            
            // Notifier les observateurs (UI) du changement de timer
            OnWeaponTimerUpdated?.Invoke(weaponTimer);
            
            // Vérifier si le timer est écoulé
            if (weaponTimer <= 0)
            {
                UnequipWeapon();
            }
            
            // Note: L'attaque est maintenant gérée par l'événement OnAttackPerformed
        }
    }
    
    // Équiper une nouvelle arme
    public void EquipWeapon(WeaponData weaponData)
    {
        // Déséquiper l'arme actuelle si nécessaire
        if (HasWeapon)
        {
            UnequipWeapon();
        }
        
        // Équiper la nouvelle arme
        equippedWeapon = weaponData;
        weaponTimer = weaponData.duration;
        
        // Mettre à jour le sprite de l'arme
        if (weaponSpriteRenderer != null && weaponData.weaponSprite != null)
        {
            weaponSpriteRenderer.sprite = weaponData.weaponSprite;
            weaponSpriteRenderer.enabled = true;
        }
        
        // Notifier les observateurs (UI) que l'arme a été équipée
        OnWeaponEquipped?.Invoke(weaponData);
        
        Debug.Log($"[PlayerWeapon] Arme '{weaponData.weaponName}' équipée pour {weaponData.duration} secondes");
    }
    
    // Déséquiper l'arme actuelle
    public void UnequipWeapon()
    {
        if (HasWeapon)
        {
            Debug.Log($"[PlayerWeapon] Arme '{equippedWeapon.weaponName}' déséquipée");
            
            // Désactiver le sprite de l'arme
            if (weaponSpriteRenderer != null)
            {
                weaponSpriteRenderer.enabled = false;
            }
            
            // Réinitialiser les données
            equippedWeapon = null;
            weaponTimer = 0;
            
            // Notifier les observateurs (UI) que l'arme a été déséquipée
            OnWeaponUnequipped?.Invoke();
        }
    }
    
    // Effectuer une attaque avec l'arme équipée
    private void Attack()
    {
        if (!HasWeapon || !canAttack)
            return;
        
        // Déclencher l'attaque
        Debug.Log($"[PlayerWeapon] Attaque avec '{equippedWeapon.weaponName}'");
        
        // Notifier les observateurs de l'attaque
        OnWeaponAttack?.Invoke();
        
        // Implémenter la logique d'attaque selon le type d'arme
        switch (equippedWeapon.weaponType)
        {
            case WeaponType.MeleeWeapon:
                StartCoroutine(PerformMeleeAttack());
                break;
                
            case WeaponType.RangedWeapon:
                // À implémenter plus tard
                Debug.Log("[PlayerWeapon] Attaque à distance non implémentée");
                break;
                
            case WeaponType.SpecialWeapon:
                // À implémenter plus tard
                Debug.Log("[PlayerWeapon] Attaque spéciale non implémentée");
                break;
        }
        
        // Démarrer le cooldown d'attaque
        StartCoroutine(AttackCooldown());
    }
    
    // Coroutine pour le cooldown entre les attaques
    private IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }
    
    // Coroutine pour effectuer une attaque de corps à corps
    private IEnumerator PerformMeleeAttack()
    {
        // Animation simple pour l'attaque (rotation de l'arme)
        Quaternion originalRotation = weaponSpriteRenderer.transform.localRotation;
        float attackDuration = 0.2f;
        float elapsedTime = 0;
        
        // Rotation de l'arme pour simuler un coup
        while (elapsedTime < attackDuration)
        {
            float rotationAngle = Mathf.Lerp(0, 90, elapsedTime / attackDuration);
            weaponSpriteRenderer.transform.localRotation = originalRotation * Quaternion.Euler(0, 0, rotationAngle);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Détecter les ennemis dans la portée d'attaque
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, equippedWeapon.attackRange);
        foreach (Collider2D hitCollider in hitColliders)
        {
            // Vérifier si c'est un ennemi
            // Note: À adapter selon votre système d'ennemis
            if (hitCollider.CompareTag("Enemy"))
            {
                // Appliquer des dégâts à l'ennemi
                // Note: À adapter selon votre système de dégâts
                Debug.Log($"[PlayerWeapon] Ennemi touché avec {equippedWeapon.damage} dégâts");
                
                // Exemple: hitCollider.GetComponent<Enemy>().TakeDamage(equippedWeapon.damage);
            }
        }
        
        // Retour à la rotation d'origine
        elapsedTime = 0;
        while (elapsedTime < attackDuration)
        {
            float rotationAngle = Mathf.Lerp(90, 0, elapsedTime / attackDuration);
            weaponSpriteRenderer.transform.localRotation = originalRotation * Quaternion.Euler(0, 0, rotationAngle);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // S'assurer que l'arme revient exactement à sa rotation d'origine
        weaponSpriteRenderer.transform.localRotation = originalRotation;
    }
    
    // Méthode pour dessiner la portée d'attaque dans l'éditeur (debug)
    private void OnDrawGizmosSelected()
    {
        if (HasWeapon)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, equippedWeapon.attackRange);
        }
    }
}