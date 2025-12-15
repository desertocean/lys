using UnityEngine;
using UnityEngine.EventSystems;

public class CircularScrollItem : MonoBehaviour, IPointerClickHandler
{
    private int index;
    private int totalCount;

    public void Initialize(int itemIndex, int total)
    {
        index = itemIndex;
        totalCount = total;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"点击了项目 {index}");
 
    }
}