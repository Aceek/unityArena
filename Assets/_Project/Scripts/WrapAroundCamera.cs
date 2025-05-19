using UnityEngine;

public class WrapAroundCamera : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private WrapAroundManager wrapAroundManager;
    [SerializeField] private Camera mainCamera;

    [Header("Paramètres de débogage")]
    [SerializeField] private Color debugColor = new Color(1f, 0f, 1f, 0.5f); // Rose semi-transparent par défaut
    [SerializeField] private float edgeThreshold = 0.1f; // Distance du bord à partir de laquelle on considère être sur un edge (en pourcentage de la taille de la vue)

    // Variables privées pour stocker les informations sur les bords
    private bool isNearLeftEdge = false;
    private bool isNearRightEdge = false;
    private bool isNearTopEdge = false;
    private bool isNearBottomEdge = false;

    private Vector2 mapMin;
    private Vector2 mapMax;
    private Vector2 mapSize;

    private void Awake()
    {
        // Si la caméra n'est pas assignée, utiliser celle de cet objet
        if (mainCamera == null)
        {
            mainCamera = GetComponent<Camera>();
            if (mainCamera == null)
            {
                Debug.LogError("[WrapAroundCamera] Aucune caméra trouvée sur cet objet. Veuillez assigner une caméra.");
                enabled = false;
                return;
            }
        }

        // Si le WrapAroundManager n'est pas assigné, essayer de le trouver dans la scène
        if (wrapAroundManager == null)
        {
            wrapAroundManager = FindFirstObjectByType<WrapAroundManager>();
            if (wrapAroundManager == null)
            {
                Debug.LogError("[WrapAroundCamera] Aucun WrapAroundManager trouvé dans la scène. Veuillez en assigner un.");
                enabled = false;
                return;
            }
        }
    }

    private void Start()
    {
        // Vérifier que le WrapAroundManager est initialisé
        if (!wrapAroundManager.IsInitialized())
        {
            Debug.LogError("[WrapAroundCamera] Le WrapAroundManager n'est pas correctement initialisé.");
            enabled = false;
            return;
        }

        // Récupérer les dimensions de la map
        mapMin = wrapAroundManager.MapMin;
        mapMax = wrapAroundManager.MapMax;
        mapSize = wrapAroundManager.MapSize;

        Debug.Log($"[WrapAroundCamera] Dimensions de la map récupérées: min={mapMin}, max={mapMax}, size={mapSize}");
    }

    private void LateUpdate()
    {
        // Mettre à jour la détection des bords
        UpdateEdgeDetection();
    }

    private void UpdateEdgeDetection()
    {
        // Calculer la position de la caméra et sa vue
        Vector3 cameraPosition = transform.position;
        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        // Calculer les limites de la vue de la caméra
        float leftBound = cameraPosition.x - cameraWidth / 2;
        float rightBound = cameraPosition.x + cameraWidth / 2;
        float bottomBound = cameraPosition.y - cameraHeight / 2;
        float topBound = cameraPosition.y + cameraHeight / 2;

        // Calculer les seuils en unités du monde
        float thresholdX = cameraWidth * edgeThreshold;
        float thresholdY = cameraHeight * edgeThreshold;

        // Vérifier si la caméra est près des bords de la map
        isNearLeftEdge = leftBound < mapMin.x + thresholdX;
        isNearRightEdge = rightBound > mapMax.x - thresholdX;
        isNearBottomEdge = bottomBound < mapMin.y + thresholdY;
        isNearTopEdge = topBound > mapMax.y - thresholdY;

        // Afficher des informations de débogage
        if (isNearLeftEdge || isNearRightEdge || isNearTopEdge || isNearBottomEdge)
        {
            string edges = "";
            if (isNearLeftEdge) edges += "Gauche ";
            if (isNearRightEdge) edges += "Droite ";
            if (isNearTopEdge) edges += "Haut ";
            if (isNearBottomEdge) edges += "Bas ";
            
            Debug.Log($"[WrapAroundCamera] Près des bords: {edges}");
        }
    }

    // Cette méthode est appelée après que toutes les caméras ont fini de rendre la scène
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Si nous ne sommes près d'aucun bord, rendre normalement
        if (!isNearLeftEdge && !isNearRightEdge && !isNearTopEdge && !isNearBottomEdge)
        {
            Graphics.Blit(source, destination);
            return;
        }

        // Créer un matériau temporaire pour le rendu
        Material debugMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        debugMaterial.SetColor("_Color", debugColor);

        // Copier d'abord la source vers la destination
        Graphics.Blit(source, destination);

        // Dessiner des rectangles colorés pour les zones "vides"
        GL.PushMatrix();
        GL.LoadOrtho();
        debugMaterial.SetPass(0);
        GL.Begin(GL.QUADS);

        float cameraHeight = 2f * mainCamera.orthographicSize;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        Vector3 cameraPosition = transform.position;

        // Calculer les limites de la vue de la caméra en coordonnées normalisées (0-1)
        float leftEdgeWorld = cameraPosition.x - cameraWidth / 2;
        float rightEdgeWorld = cameraPosition.x + cameraWidth / 2;
        float bottomEdgeWorld = cameraPosition.y - cameraHeight / 2;
        float topEdgeWorld = cameraPosition.y + cameraHeight / 2;

        // Convertir les limites de la map en coordonnées normalisées de la vue de la caméra
        float mapMinXNorm = Mathf.InverseLerp(leftEdgeWorld, rightEdgeWorld, mapMin.x);
        float mapMaxXNorm = Mathf.InverseLerp(leftEdgeWorld, rightEdgeWorld, mapMax.x);
        float mapMinYNorm = Mathf.InverseLerp(bottomEdgeWorld, topEdgeWorld, mapMin.y);
        float mapMaxYNorm = Mathf.InverseLerp(bottomEdgeWorld, topEdgeWorld, mapMax.y);

        // Dessiner des rectangles pour les zones hors de la map
        if (isNearLeftEdge)
        {
            // Rectangle à gauche
            GL.Color(debugColor);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(mapMinXNorm, 0, 0);
            GL.Vertex3(mapMinXNorm, 1, 0);
            GL.Vertex3(0, 1, 0);
        }

        if (isNearRightEdge)
        {
            // Rectangle à droite
            GL.Color(debugColor);
            GL.Vertex3(mapMaxXNorm, 0, 0);
            GL.Vertex3(1, 0, 0);
            GL.Vertex3(1, 1, 0);
            GL.Vertex3(mapMaxXNorm, 1, 0);
        }

        if (isNearBottomEdge)
        {
            // Rectangle en bas
            GL.Color(debugColor);
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(1, 0, 0);
            GL.Vertex3(1, mapMinYNorm, 0);
            GL.Vertex3(0, mapMinYNorm, 0);
        }

        if (isNearTopEdge)
        {
            // Rectangle en haut
            GL.Color(debugColor);
            GL.Vertex3(0, mapMaxYNorm, 0);
            GL.Vertex3(1, mapMaxYNorm, 0);
            GL.Vertex3(1, 1, 0);
            GL.Vertex3(0, 1, 0);
        }

        GL.End();
        GL.PopMatrix();
    }
}