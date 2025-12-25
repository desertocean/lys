using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 预制件圆形排列控制器 - 专门处理已有子物体的预制件
/// </summary>
public class PrefabCircleArrangement : MonoBehaviour
{
    [Header("圆形排列设置")]
    [SerializeField] private float radius = 200f;           // UI使用像素单位
    [SerializeField] private bool arrangeOnStart = true;
    [SerializeField] private float startAngle = 0f;        // 起始角度

    [Header("凸显效果设置")]
    [SerializeField] private float selectedScale = 1.1f;   // 选中时的缩放
    [SerializeField] private float unselectedScale = 1.0f; // 未选中时的缩放
    [SerializeField] private float unselectedAlpha = 0.4f; // 未选中时的透明度
    [SerializeField] private float animationSpeed = 8f;    // 动画速度

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


    private float currentRotation = 0f;
    private float rotationVelocity = 0f;
    private bool isDragging = false;
    private Vector2 lastDragPosition;


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
        HighlightChild(2);
    }

    /// <summary>
    /// 将子物体排列成圆形（针对UI）
    /// </summary>
    public void ArrangeInCircle()
    {
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            float angle = (360f / childCount) * i + currentRotation;
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
            Vector3 scale= Vector3.one ;
            Color color=new Color(childImages[i].color.r,
                                          childImages[i].color.g,
                                          childImages[i].color.b,
                                          0.3f);
            if (i == selectedIndex)
            {
                scale=Vector3.one * 1.2f;
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

    /// <summary>
    /// 更新动画效果
    /// </summary>
    private void UpdateAnimation()
    {
        if (childTransforms == null) return;

        float delta = Time.deltaTime * animationSpeed;

        for (int i = 0; i < childTransforms.Length; i++)
        {
            if (childTransforms[i] != null)
            {
                // 平滑过渡尺寸
                childTransforms[i].sizeDelta = Vector2.Lerp(
                    childTransforms[i].sizeDelta,
                    targetSizes[i],
                    delta
                );

                // 平滑过渡位置（如果需要的话）
                if (childTransforms[i].anchoredPosition != targetPositions[i])
                {
                    childTransforms[i].anchoredPosition = Vector2.Lerp(
                        childTransforms[i].anchoredPosition,
                        targetPositions[i],
                        delta
                    );
                }
            }

            if (childImages[i] != null)
            {
                // 平滑过渡颜色
                childImages[i].color = Color.Lerp(
                    childImages[i].color,
                    targetColors[i],
                    delta
                );
            }
        }
    }

    /// <summary>
    /// 立即完成所有动画（无过渡）
    /// </summary>
    public void InstantApplyEffects()
    {
        for (int i = 0; i < childTransforms.Length; i++)
        {
            if (childTransforms[i] != null)
            {
                childTransforms[i].sizeDelta = targetSizes[i];
                childTransforms[i].anchoredPosition = targetPositions[i];
            }

            if (childImages[i] != null)
            {
                childImages[i].color = targetColors[i];
            }
        }
    }

    /// <summary>
    /// 设置圆形半径并重新排列
    /// </summary>
    public void SetRadius(float newRadius)
    {
        radius = Mathf.Max(0, newRadius);
        ArrangeInCircle();
    }

    /// <summary>
    /// 设置起始角度并重新排列
    /// </summary>
    public void SetStartAngle(float angle)
    {
        startAngle = angle;
        ArrangeInCircle();
    }

    /// <summary>
    /// 获取当前选中的索引
    /// </summary>
    public int GetSelectedIndex()
    {
        return selectedIndex;
    }

    /// <summary>
    /// 获取当前选中的Transform
    /// </summary>
    public RectTransform GetSelectedTransform()
    {
        if (selectedIndex >= 0 && selectedIndex < childTransforms.Length)
        {
            return childTransforms[selectedIndex];
        }
        return null;
    }

    /// <summary>
    /// 获取子物体数量
    /// </summary>
    public int GetChildCount()
    {
        return childTransforms != null ? childTransforms.Length : 0;
    }

    /// <summary>
    /// 打印所有子物体信息（调试用）
    /// </summary>
    public void PrintChildrenInfo()
    {
        Debug.Log($"子物体数量: {GetChildCount()}");
        for (int i = 0; i < GetChildCount(); i++)
        {
            if (childTransforms[i] != null)
            {
                Debug.Log($"[{i}] {childTransforms[i].name} - 位置: {childTransforms[i].anchoredPosition}");
            }
        }
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