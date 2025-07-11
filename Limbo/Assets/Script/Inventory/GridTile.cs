using UnityEngine;
using UnityEngine.EventSystems;

public class GridTile : MonoBehaviour, IPointerClickHandler
{
    public int row;
    public int column;
    public GridVisualizer parentGrid;


    public DraggableItem currentItem = null;

    public bool IsOccupied => currentItem != null;

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"Tile clicked at position: Row {row}, Column {column}");
    }
}
