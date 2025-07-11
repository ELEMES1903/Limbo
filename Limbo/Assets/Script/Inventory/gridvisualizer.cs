using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridVisualizer : MonoBehaviour
{

    public int rows = 5;
    public int columns = 5;
    public float tileSize = 50f;
    public float tileSpacing = 5f;
    public GameObject tilePrefab; // UI Image prefab with GridTile script attached
    public GridTile[,] grid;
    public bool isContainer;

    void Start()
    {
        CheckIfContainer();
        GenerateGrid();
    }
    public void CheckIfContainer()
    {
        DraggableItem di = transform.parent.GetComponent<DraggableItem>();

        if (di != null)
        {
           if (di.itemData.isContainer)
            {
                rows = di.itemData.containerRows;
                columns = di.itemData.containerColumns;
            } 
        }
    }
    public void GenerateGrid()
    {
        Transform found = transform.Find("gridParent");
        RectTransform gridParent = found.GetComponent<RectTransform>();

        if (tilePrefab == null || gridParent == null)
        {
            Debug.LogError("Assign tilePrefab and gridParent in inspector.");
            return;
        }

        grid = new GridTile[rows, columns];

        float width = columns * tileSize + (columns - 1) * tileSpacing;
        float height = rows * tileSize + (rows - 1) * tileSpacing;
        //gridParent.sizeDelta = new Vector2(width, height);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                GameObject tileObj = Instantiate(tilePrefab, gridParent);
                tileObj.name = $"Tile_{r}_{c}";

                RectTransform rect = tileObj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    float x = c * (tileSize + tileSpacing);
                    float y = -r * (tileSize + tileSpacing);
                    rect.anchoredPosition = new Vector2(x, y);
                    rect.sizeDelta = new Vector2(tileSize, tileSize);

                }

                GridTile tileScript = tileObj.GetComponent<GridTile>();
                if (tileScript != null)
                {
                    tileScript.row = r;
                    tileScript.column = c;
                    grid[r, c] = tileScript;
                    tileScript.parentGrid = this; // Add this line
                }
                else
                {
                    Debug.LogWarning("Tile prefab does not have a GridTile component.");
                }
            }
        }
    }
    public GridTile GetTile(int row, int col)
    {
        if (row < 0 || col < 0 || row >= rows || col >= columns)
            return null;
        return grid[row, col];
    }

    public List<GridTile> GetTilesForItem(int startRow, int startCol, int width, int height)
    {
        List<GridTile> tiles = new();

        for (int r = startRow; r < startRow + height; r++)
        {
            for (int c = startCol; c < startCol + width; c++)
            {
                GridTile tile = GetTile(r, c);
                if (tile == null)
                    return null; // Out of bounds
                tiles.Add(tile);
            }
        }

        return tiles;
    }

    public bool AreTilesFree(List<GridTile> tiles, DraggableItem ignoreItem = null)
    {
        foreach (var tile in tiles)
        {
            if (tile.currentItem != null && tile.currentItem != ignoreItem)
                return false;
        }
        return true;
    }

}
