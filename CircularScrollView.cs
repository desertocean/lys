using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CircularScrollView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("基础设置")]
    [SerializeField] private float radius = 200f; // 圆形半径
    [SerializeField] private int itemCount = 13; // 项目数量
    [SerializeField] private GameObject itemPrefab; // 项目预制体


    private List<CircularScrollItem> items = new List<CircularScrollItem>();
    private float currentRotation = 0f;
    private float rotationVelocity = 0f;
    private bool isDragging = false;
    private Vector2 lastDragPosition;
    private RectTransform rectTransform;
    private Vector2 centerOffset = new Vector2(100, 100);

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        InitializeItems();
    }

    void InitializeItems()
    {
        // 清除旧项目
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        items.Clear();

        // 创建新项目
        for (int i = 0; i < itemCount; i++)
        {
            GameObject newItem = CreateItem(i);
            if (i==0 || i== itemCount-1) {
                newItem.SetActive(false);
            }
            CircularScrollItem scrollItem = newItem.AddComponent<CircularScrollItem>();
            scrollItem.Initialize(i, itemCount);
            items.Add(scrollItem);
        }

        UpdateItemPositions();
    }

    GameObject CreateItem(int index)
    {
        GameObject item;

        if (itemPrefab != null)
        {
            item = Instantiate(itemPrefab, transform);
        }
        else
        {
            // 创建默认圆圈项目
            item = new GameObject($"Item_{index}");
            item.transform.SetParent(transform, false);

            // 添加 Image 组件用于显示圆圈
            Image image = item.AddComponent<Image>();
            image.sprite = CreateCircleSprite();
            image.color = Color.HSVToRGB((float)index / itemCount, 0.8f, 1f);

        }

        return item;
    }

    Sprite CreateCircleSprite()
    {
        // 创建一个简单的圆形精灵
        Texture2D texture = new Texture2D(128, 128);
        Color[] pixels = new Color[128 * 128];

        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float dx = x - 64;
                float dy = y - 64;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                if (distance <= 60)
                {
                    pixels[y * 128 + x] = Color.white;
                }
                else
                {
                    pixels[y * 128 + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
    }

 

    void UpdateItemPositions()
    {
        for (int i = 0; i < items.Count; i++)
        {
            float angle = (360f / itemCount) * i + currentRotation;
            float radian = angle * Mathf.Deg2Rad;
            Vector3 position = new Vector3(Mathf.Sin(radian) * radius+ centerOffset.x,Mathf.Cos(radian) * radius+ centerOffset.y, 0);
            items[i].transform.localPosition = position;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        lastDragPosition = eventData.position;
        rotationVelocity = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            Vector2 currentDragPosition = eventData.position;
            Vector3 vectorA = currentDragPosition - centerOffset;
            Vector3 vectorB = lastDragPosition - centerOffset;
            Vector2 dirA = new Vector2(vectorA.x, vectorA.y);
            Vector2 dirB = new Vector2(vectorB.x, vectorB.y);
            currentRotation += Vector2.SignedAngle(dirA, dirB);
            lastDragPosition = currentDragPosition;
            UpdateItemPositions();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }
 
}