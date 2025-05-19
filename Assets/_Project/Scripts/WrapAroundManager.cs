using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class WrapAroundManager : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private Tilemap mainTilemap; // Tilemap principale pour calculer les dimensions
    [SerializeField] private string wrapTag = "WrapAround"; // Tag pour identifier les objets à gérer

    [Header("Paramètres de transition")]
    [Tooltip("Durée de l'effet de transition en secondes")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float transitionDuration = 0.15f;
    
    [Tooltip("Activer l'effet de ralentissement temporaire")]
    [SerializeField] private bool useSlowdownEffect = true;
    
    [Tooltip("Multiplicateur de vitesse pendant la transition (0.1-1)")]
    [Range(0.1f, 1f)]
    [SerializeField] private float slowdownFactor = 0.5f;

    private Vector2 mapSize; // Taille de la map en unités
    private Vector2 mapMin; // Coin inférieur gauche de la map
    private Vector2 mapMax; // Coin supérieur droit de la map

    private bool hasCalculatedBoundsSuccessfully = false;
    
    // Propriétés publiques pour accéder aux dimensions de la map
    public Vector2 MapSize => mapSize;
    public Vector2 MapMin => mapMin;
    public Vector2 MapMax => mapMax;
    
    // Propriété publique pour accéder au tag
    public string WrapTag => wrapTag;
    
    // Dictionnaire pour suivre les objets en cours de transition
    private Dictionary<GameObject, bool> objectsInTransition = new Dictionary<GameObject, bool>();

    private void Awake()
    {
        // Trouver automatiquement la Tilemap si non assignée
        if (mainTilemap == null)
        {
            mainTilemap = FindFirstObjectByType<Tilemap>();
            if (mainTilemap == null)
            {
                Debug.LogError("[WrapAroundManager] Awake: Aucune Tilemap trouvée dans la scène ! Veuillez assigner une Tilemap au WrapAroundManager.");
                hasCalculatedBoundsSuccessfully = false;
                return;
            }
        }
        CalculateMapBounds();
    }

    // OnValidate est appelé dans l'éditeur lorsque le script est chargé ou qu'une valeur est modifiée dans l'inspecteur.
    private void OnValidate()
    {
        // Pour forcer le recalcul si on est en mode éditeur et que les choses ont pu changer
        if (Application.isEditor && !Application.isPlaying)
        {
            if (mainTilemap == null)
            {
                // Tenter de le trouver si on est dans OnValidate
                var foundTilemap = FindFirstObjectByType<Tilemap>();
                if (foundTilemap != null) {
                    mainTilemap = foundTilemap;
                }
            }
            // Il est important d'appeler CalculateMapBounds même si mainTilemap est encore null
            // pour que hasCalculatedBoundsSuccessfully soit correctement mis à false.
            CalculateMapBounds();
        }
    }

    private void CalculateMapBounds()
    {
        if (mainTilemap == null)
        {
            // Ne pas logguer d'erreur ici si on est en mode éditeur et que OnValidate peut le régler,
            // mais s'assurer que l'état est invalide.
            if (Application.isPlaying) // Logguer l'erreur seulement en mode Play si la tilemap est manquante au calcul.
            {
                Debug.LogError("[WrapAroundManager] CalculateMapBounds: mainTilemap est null. Impossible de calculer les limites.");
            }
            mapSize = Vector2.zero;
            mapMin = Vector2.zero;
            mapMax = Vector2.zero;
            hasCalculatedBoundsSuccessfully = false;
            return;
        }

        BoundsInt bounds = mainTilemap.cellBounds;
        int minX = bounds.xMax;
        int maxX = bounds.xMin;
        int minY = bounds.yMax;
        int maxY = bounds.yMin;

        bool foundAnyTile = false;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (mainTilemap.HasTile(pos))
            {
                foundAnyTile = true;
                minX = Mathf.Min(minX, pos.x);
                maxX = Mathf.Max(maxX, pos.x);
                minY = Mathf.Min(minY, pos.y);
                maxY = Mathf.Max(maxY, pos.y);
            }
        }

        if (!foundAnyTile && bounds.size.x == 0 && bounds.size.y == 0 && bounds.size.z == 0) {
             // Si la tilemap est complètement vide ou non initialisée.
            mapMin = Vector2.zero;
            mapMax = Vector2.zero;
            mapSize = Vector2.zero;
            hasCalculatedBoundsSuccessfully = false;
            // Debug.LogWarning($"[WrapAroundManager] CalculateMapBounds: Aucune tuile trouvée ou limites de tilemap invalides. MapSize sera (0,0).");
            return;
        }
        
        mapMin = mainTilemap.CellToWorld(new Vector3Int(minX, minY, 0));
        mapMax = mainTilemap.CellToWorld(new Vector3Int(maxX + 1, maxY + 1, 0));
        mapSize = new Vector2(mapMax.x - mapMin.x, mapMax.y - mapMin.y);

        if (mapSize.x > 0 && mapSize.y > 0)
        {
            hasCalculatedBoundsSuccessfully = true;
            Debug.Log($"[WrapAroundManager] Taille de la map calculée : {mapSize.x}x{mapSize.y} unités (min: {mapMin}, max: {mapMax})");
        }
        else
        {
            hasCalculatedBoundsSuccessfully = false;
            // Ne pas spammer en mode éditeur si OnValidate est appelé souvent.
            // En mode Play, cela pourrait être un avertissement utile.
            if (Application.isPlaying) {
                 Debug.LogWarning($"[WrapAroundManager] CalculateMapBounds: Calculated mapSize is invalid ({mapSize}). This might be due to an empty or misconfigured Tilemap.");
            }
            // S'assurer que mapSize est (0,0) si le calcul échoue pour une raison quelconque.
            if (!(mapSize.x > 0 && mapSize.y > 0)) {
                mapSize = Vector2.zero;
            }
        }
    }

    public bool IsInitialized()
    {
        // Vérifie si le calcul a été tenté avec succès ET que les dimensions sont valides.
        // CalculateMapBounds met mapSize à zero si invalide.
        return hasCalculatedBoundsSuccessfully && mapSize.x > 0 && mapSize.y > 0;
    }

    private void LateUpdate()
    {
        // Trouver tous les objets avec le tag "WrapAround"
        GameObject[] wrapObjects = GameObject.FindGameObjectsWithTag(wrapTag);
        
        foreach (GameObject obj in wrapObjects)
        {
            // Vérifier si l'objet est déjà en transition
            if (objectsInTransition.ContainsKey(obj) && objectsInTransition[obj])
            {
                continue; // Ignorer les objets déjà en transition
            }
            
            // Vérifier si l'objet doit être téléporté
            CheckAndWrapObject(obj);
        }
    }
    
    private void CheckAndWrapObject(GameObject obj)
    {
        Vector3 position = obj.transform.position;
        Vector3 wrappedPosition = position;
        bool needsWrapping = false;

        // Continuité horizontale
        if (position.x > mapMax.x)
        {
            wrappedPosition.x = mapMin.x + (position.x - mapMax.x);
            needsWrapping = true;
        }
        else if (position.x < mapMin.x)
        {
            wrappedPosition.x = mapMax.x - (mapMin.x - position.x);
            needsWrapping = true;
        }

        // Continuité verticale
        if (position.y > mapMax.y)
        {
            wrappedPosition.y = mapMin.y + (position.y - mapMax.y);
            needsWrapping = true;
        }
        else if (position.y < mapMin.y)
        {
            wrappedPosition.y = mapMax.y - (mapMin.y - position.y);
            needsWrapping = true;
        }

        // Si l'objet doit être téléporté, appliquer la transition
        if (needsWrapping)
        {
            StartCoroutine(TransitionObject(obj, wrappedPosition));
        }
    }
    
    private IEnumerator TransitionObject(GameObject obj, Vector3 targetPosition)
    {
        // Marquer l'objet comme étant en transition
        objectsInTransition[obj] = true;
        
        // Sauvegarder la vitesse originale si c'est un personnage avec Rigidbody2D
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        Vector2 originalVelocity = Vector2.zero;
        
        if (rb != null)
        {
            originalVelocity = rb.linearVelocity;
            
            // Ralentir temporairement si l'effet est activé
            if (useSlowdownEffect)
            {
                rb.linearVelocity *= slowdownFactor;
            }
        }
        
        // Attendre la durée de la transition
        yield return new WaitForSeconds(transitionDuration);
        
        // Téléporter l'objet
        obj.transform.position = targetPosition;
        
        // Restaurer la vitesse originale
        if (rb != null && useSlowdownEffect)
        {
            rb.linearVelocity = originalVelocity;
        }
        
        // Marquer l'objet comme n'étant plus en transition
        objectsInTransition[obj] = false;
    }
}