using UnityEngine;
using UnityEngine.UIElements;

public class WeaponUIToolkit : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Référence au composant PlayerWeapon")]
    [SerializeField] private PlayerWeapon playerWeapon;
    
    [Header("UI Toolkit")]
    [Tooltip("Référence au fichier UXML pour l'interface des armes")]
    [SerializeField] private VisualTreeAsset weaponUITemplate;
    
    [Tooltip("Référence au fichier USS pour le style de l'interface")]
    [SerializeField] private StyleSheet weaponUIStyleSheet;
    
    // Références aux éléments UI Toolkit
    private VisualElement root;
    private VisualElement weaponUIContainer;
    private ProgressBar timerProgressBar;
    private Label weaponNameLabel;
    private Label timerLabel;
    
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
                Debug.LogError("[WeaponUIToolkit] Awake: Aucun PlayerWeapon trouvé ! Veuillez l'assigner manuellement.");
            }
        }
    }
    
    private void OnEnable()
    {
        // Initialiser l'UI Toolkit
        InitializeUI();
        
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
    
    private void InitializeUI()
    {
        // Obtenir la référence au Document UI Toolkit
        UIDocument document = GetComponent<UIDocument>();
        if (document == null)
        {
            document = gameObject.AddComponent<UIDocument>();
            
            // Assigner le template UXML si disponible
            if (weaponUITemplate != null)
            {
                document.visualTreeAsset = weaponUITemplate;
            }
            else
            {
                Debug.LogError("[WeaponUIToolkit] InitializeUI: Aucun template UXML assigné !");
            }
        }
        
        // Obtenir la racine de l'UI
        root = document.rootVisualElement;
        
        // Ajouter la feuille de style si disponible
        if (weaponUIStyleSheet != null && !root.styleSheets.Contains(weaponUIStyleSheet))
        {
            root.styleSheets.Add(weaponUIStyleSheet);
        }
        
        // Récupérer les références aux éléments d'UI
        weaponUIContainer = root.Q<VisualElement>("weapon-ui-container");
        timerProgressBar = root.Q<ProgressBar>("timer-progress-bar");
        weaponNameLabel = root.Q<Label>("weapon-name-label");
        timerLabel = root.Q<Label>("timer-label");
        
        // Vérifier que tous les éléments ont été trouvés
        if (weaponUIContainer == null)
        {
            Debug.LogError("[WeaponUIToolkit] InitializeUI: Élément 'weapon-ui-container' non trouvé dans le UXML !");
        }
        
        // Masquer l'UI au départ
        if (weaponUIContainer != null)
        {
            weaponUIContainer.style.display = DisplayStyle.None;
        }
    }
    
    // Gérer l'équipement d'une nouvelle arme
    private void HandleWeaponEquipped(WeaponData weaponData)
    {
        // Activer l'UI
        if (weaponUIContainer != null)
        {
            weaponUIContainer.style.display = DisplayStyle.Flex;
        }
        
        // Note: Nous n'affichons pas l'image de l'arme pour le moment
        
        // Mettre à jour le nom de l'arme
        if (weaponNameLabel != null)
        {
            weaponNameLabel.text = weaponData.weaponName;
        }
        
        // Initialiser la barre de progression
        maxWeaponDuration = weaponData.duration;
        if (timerProgressBar != null)
        {
            timerProgressBar.value = 100f; // UI Toolkit ProgressBar utilise 0-100 par défaut
            timerProgressBar.title = ""; // Effacer le titre par défaut
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
            timerProgressBar.value = (remainingTime / maxWeaponDuration) * 100f;
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
            weaponUIContainer.style.display = DisplayStyle.None;
        }
    }
    
    // Mettre à jour le texte du timer avec un format MM:SS
    private void UpdateTimerText(float timeInSeconds)
    {
        if (timerLabel != null)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
            timerLabel.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}