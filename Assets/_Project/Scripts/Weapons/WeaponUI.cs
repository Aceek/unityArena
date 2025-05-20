using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUI : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Référence au composant PlayerWeapon")]
    [SerializeField] private PlayerWeapon playerWeapon;
    
    [Header("Éléments d'UI")]
    [Tooltip("Image pour afficher le sprite de l'arme équipée")]
    [SerializeField] private Image weaponImage;
    
    [Tooltip("Barre de progression pour le timer de l'arme")]
    [SerializeField] private Image timerProgressBar;
    
    [Tooltip("Texte pour afficher le nom de l'arme")]
    [SerializeField] private TextMeshProUGUI weaponNameText;
    
    [Tooltip("Texte pour afficher le temps restant")]
    [SerializeField] private TextMeshProUGUI timerText;
    
    [Tooltip("Conteneur principal de l'UI des armes (pour l'activer/désactiver)")]
    [SerializeField] private GameObject weaponUIContainer;
    
    // Durée maximale de l'arme actuelle (pour calculer la progression)
    private float maxWeaponDuration;
    
    private void Awake()
    {
        // Vérifier si les références sont assignées
        if (playerWeapon == null)
        {
            playerWeapon = FindFirstObjectByType<PlayerWeapon>();
            if (playerWeapon == null)
            {
                Debug.LogError("[WeaponUI] Awake: Aucun PlayerWeapon trouvé ! Veuillez l'assigner manuellement.");
            }
        }
        
        // Désactiver l'UI au départ
        if (weaponUIContainer != null)
        {
            weaponUIContainer.SetActive(false);
        }
    }
    
    private void OnEnable()
    {
        // S'abonner aux événements du PlayerWeapon
        if (playerWeapon != null)
        {
            playerWeapon.OnWeaponEquipped.AddListener(HandleWeaponEquipped);
            playerWeapon.OnWeaponTimerUpdated.AddListener(HandleWeaponTimerUpdated);
            playerWeapon.OnWeaponUnequipped.AddListener(HandleWeaponUnequipped);
        }
    }
    
    private void OnDisable()
    {
        // Se désabonner des événements du PlayerWeapon
        if (playerWeapon != null)
        {
            playerWeapon.OnWeaponEquipped.RemoveListener(HandleWeaponEquipped);
            playerWeapon.OnWeaponTimerUpdated.RemoveListener(HandleWeaponTimerUpdated);
            playerWeapon.OnWeaponUnequipped.RemoveListener(HandleWeaponUnequipped);
        }
    }
    
    // Gérer l'équipement d'une nouvelle arme
    private void HandleWeaponEquipped(WeaponData weaponData)
    {
        // Activer l'UI
        if (weaponUIContainer != null)
        {
            weaponUIContainer.SetActive(true);
        }
        
        // Mettre à jour l'image de l'arme
        if (weaponImage != null && weaponData.weaponSprite != null)
        {
            weaponImage.sprite = weaponData.weaponSprite;
            weaponImage.enabled = true;
        }
        
        // Mettre à jour le nom de l'arme
        if (weaponNameText != null)
        {
            weaponNameText.text = weaponData.weaponName;
        }
        
        // Initialiser la barre de progression
        maxWeaponDuration = weaponData.duration;
        if (timerProgressBar != null)
        {
            timerProgressBar.fillAmount = 1f;
        }
        
        // Initialiser le texte du timer
        UpdateTimerText(maxWeaponDuration);
    }
    
    // Gérer la mise à jour du timer de l'arme
    private void HandleWeaponTimerUpdated(float remainingTime)
    {
        // Mettre à jour la barre de progression
        if (timerProgressBar != null && maxWeaponDuration > 0)
        {
            timerProgressBar.fillAmount = Mathf.Clamp01(remainingTime / maxWeaponDuration);
        }
        
        // Mettre à jour le texte du timer
        UpdateTimerText(remainingTime);
    }
    
    // Gérer le déséquipement de l'arme
    private void HandleWeaponUnequipped()
    {
        // Désactiver l'UI
        if (weaponUIContainer != null)
        {
            weaponUIContainer.SetActive(false);
        }
    }
    
    // Mettre à jour le texte du timer avec un format MM:SS
    private void UpdateTimerText(float timeInSeconds)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}