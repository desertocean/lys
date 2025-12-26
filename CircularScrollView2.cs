using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
/// <summary>
/// 预制件圆形排列控制器 - 专门处理已有子物体的预制件
/// </summary>
public class PrefabCircleArrangement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("圆形排列设置")]
    [SerializeField] private float radius = 160f;           // UI使用像素单位
    [SerializeField] private bool arrangeOnStart = true;
    [SerializeField] private float startAngle = 0f;        // 起始角度

    [Header("凸显效果设置")]
    [SerializeField] private float selectedScale = 1.1f;   // 选中时的缩放
    [SerializeField] private float unselectedScale = 1.0f; // 未选中时的缩放
    [SerializeField] private float unselectedAlpha = 0.4f; // 未选中时的透明度
    [SerializeField] private float animationSpeed = 8f;    // 动画速度

    [Header("箭头")]
    [SerializeField] private RectTransform arrow ;    // 动画速度

    // 子物体信息
    private RectTransform[] childTransforms;
    private Image[] childImages;
    private Vector2[] originalSizes;
    private Color[] originalColors;
    private int selectedIndex = -1; // 当前选中的索引，-1表示没有选中

    // 目标状态
    private Vector2[] targetSizes;
    private Color[] targetColors;
    private Vector2[] targetPositions;

 
    private float rotationVelocity = 0f;
    private bool isDragging = false;
    private Vector2 lastDragPosition;

    private float detectionAngle = 30f;
    private int current_index = 0;
    private int last_index = 0;
    void Start()
    {
        // 初始化所有子物体
        InitializeChildren();

        if (arrangeOnStart)
        {
            ArrangeInCircle();
        }
    }

    void Update()
    {
        // 平滑过渡到目标状态
        //UpdateAnimation();
    }

    /// <summary>
    /// 初始化子物体数组
    /// </summary>
    private void InitializeChildren()
    {
        int childCount = transform.childCount;

        // 初始化数组
        childTransforms = new RectTransform[childCount];
        childImages = new Image[childCount];

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            childTransforms[i] = child.GetComponent<RectTransform>();
            childImages[i] = child.GetComponent<Image>();


        }

        // 默认不选中任何物体
        //ResetAllToNormal();
        HighlightChild(0);
    }

    /// <summary>
    /// 将子物体排列成圆形（针对UI）
    /// </summary>
    public void ArrangeInCircle()
    {
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            float angle = (360f / childCount) * i;
            float radian = angle * Mathf.Deg2Rad;

            Vector3 position = new Vector3(Mathf.Sin(radian) * radius, Mathf.Cos(radian) * radius, 0);
            childTransforms[i].localPosition = position;
        }
    }

    /// <summary>
    /// 凸显指定索引的子物体
    /// </summary>
    /// <param name="index">子物体索引（0开始）</param>
    public void HighlightChild(int index)
    {
 
        selectedIndex = index;

        // 更新所有子物体的目标状态
        for (int i = 0; i < childTransforms.Length; i++)
        {
            Vector3 scale= Vector3.one*0.6f ;
            Color color=new Color(childImages[i].color.r,
                                          childImages[i].color.g,
                                          childImages[i].color.b,
                                          0.3f);
            if (i == selectedIndex)
            {
                scale= scale * 1.2f;
                color = new Color(childImages[i].color.r,
                                          childImages[i].color.g,
                                          childImages[i].color.b,
                                          1.0f);
            }
 
            childTransforms[i].localScale = scale;
            childImages[i].color = color;
            foreach (Transform child in childTransforms[i])
            {
                child.localScale = scale;
                child.GetComponent<Image>().color = color;
            }

        }
 
 
    }
 
 
    /// <summary>
    /// 重置所有子物体到正常状态
    /// </summary>
    private void ResetAllToNormal()
    {
        HighlightChild(-1);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        lastDragPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            AngleToPosiotn(eventData.position);

        }
    }

    private void AngleToPosiotn(Vector2 position)
    {

        Vector2 arrowPos = new Vector2(arrow.position.x, arrow.position.y);
        Vector3 vectorA = position - arrowPos;
        Vector2 dirA = new Vector2(vectorA.x, vectorA.y).normalized;
        float currentRotation = -Vector2.SignedAngle(dirA, new Vector2(0, 1));
        lastDragPosition = position;
        arrow.localEulerAngles = new Vector3(0, 0, currentRotation);
        int closestIndex = GetPointedChildIndex(currentRotation);
        HighlightChild(closestIndex);
        last_index = closestIndex;
    }
    public int GetPointedChildIndex(float currentRotation)
    {
        int closestIndex = -1;
        float smallestAngleDiff = float.MaxValue;

        for (int i = 0; i < childTransforms.Length; i++)
        {
            float radian = (currentRotation-90) * Mathf.Deg2Rad;
            Vector3 arrow_vec= new Vector3(Mathf.Cos(radian),Mathf.Sin(radian) , 0);
            Vector3 toChild = (arrow.position-childTransforms[i].position).normalized;
            Vector3 from_Child = arrow_vec.normalized;
            float angleDiff = Vector3.Angle(from_Child, toChild);
            if (angleDiff <= detectionAngle && angleDiff < smallestAngleDiff)
            {
                smallestAngleDiff = angleDiff;
                closestIndex = i;
            }
        }
        return  closestIndex ;
    }

    private void AngleToIndex(int index) {

        AngleToPosiotn(childTransforms[index].position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        AngleToIndex(last_index);
 
    }

    /// <summary>
    /// 在编辑器中使用
    /// </summary>
#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            // 在编辑器中预览排列
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    InitializeChildren();
                    ArrangeInCircle();
 
                }
            };
        }
    }
#endif
}