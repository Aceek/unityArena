using UnityEngine;
using UnityEngine.Tilemaps;

public class WrapAroundManager : MonoBehaviour
{
    [SerializeField] private Tilemap mainTilemap; // Tilemap principale pour calculer les dimensions
    [SerializeField] private string wrapTag = "WrapAround"; // Tag pour identifier les objets à gérer

    private Vector2 mapSize; // Taille de la map en unités
    private Vector2 mapMin; // Coin inférieur gauche de la map
    private Vector2 mapMax; // Coin supérieur droit de la map

    private void Awake()
    {
        // Trouver automatiquement la Tilemap si non assignée
        if (mainTilemap == null)
        {
            mainTilemap = FindFirstObjectByType<Tilemap>();
            if (mainTilemap == null)
            {
                Debug.LogError("Aucune Tilemap trouvée dans la scène ! Veuillez assigner une Tilemap au WrapAroundManager.");
                return;
            }
        }

        CalculateMapBounds();
    }

    private void CalculateMapBounds()
    {
        // Obtenir les limites initiales de la grille
        BoundsInt bounds = mainTilemap.cellBounds;
        int minX = bounds.xMax;
        int maxX = bounds.xMin;
        int minY = bounds.yMax;
        int maxY = bounds.yMin;

        // Parcourir toutes les cellules pour trouver les limites réelles avec des tuiles
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (mainTilemap.HasTile(pos))
            {
                minX = Mathf.Min(minX, pos.x);
                maxX = Mathf.Max(maxX, pos.x);
                minY = Mathf.Min(minY, pos.y);
                maxY = Mathf.Max(maxY, pos.y);
            }
        }

        // Convertir les limites en positions dans le monde
        Vector3 cellSize = mainTilemap.cellSize; // Taille d'une cellule (ex. 1x1 unité avec PPU = 128)
        mapMin = mainTilemap.CellToWorld(new Vector3Int(minX, minY, 0));
        mapMax = mainTilemap.CellToWorld(new Vector3Int(maxX + 1, maxY + 1, 0)); // +1 pour inclure la dernière tuile

        // Calculer la taille de la map
        mapSize = new Vector2(mapMax.x - mapMin.x, mapMax.y - mapMin.y);

        Debug.Log($"Taille de la map calculée : {mapSize.x}x{mapSize.y} unités (min: {mapMin}, max: {mapMax})");
    }

    private void LateUpdate()
    {
        // Trouver tous les objets avec le tag "WrapAround"
        GameObject[] wrapObjects = GameObject.FindGameObjectsWithTag(wrapTag);
        foreach (GameObject obj in wrapObjects)
        {
            WrapObject(obj);
        }
    }

    private void WrapObject(GameObject obj)
    {
        Vector3 position = obj.transform.position;

        // Continuité horizontale
        if (position.x > mapMax.x)
        {
            position.x = mapMin.x + (position.x - mapMax.x);
        }
        else if (position.x < mapMin.x)
        {
            position.x = mapMax.x - (mapMin.x - position.x);
        }

        // Continuité verticale
        if (position.y > mapMax.y)
        {
            position.y = mapMin.y + (position.y - mapMax.y);
        }
        else if (position.y < mapMin.y)
        {
            position.y = mapMax.y - (mapMin.y - position.y);
        }

        obj.transform.position = position;
    }
}