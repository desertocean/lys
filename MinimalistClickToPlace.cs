using UnityEngine;
using UnityEngine.InputSystem;

public class MinimalistClickToPlace : MonoBehaviour
{
    [Header("必要引用")]
    public GameObject objectPrefab;  // 预制件
    public Camera targetCamera;      // 相机（必须设置）

    private InputAction clickAction;

    void Start()
    {
        // 必须设置相机
        if (targetCamera == null)
        {
            Debug.LogError("请设置 Camera！");
            return;
        }

        // 设置输入
        clickAction = new InputAction(binding: "<Mouse>/leftButton");
        clickAction.performed += ctx => PlaceObject();
        clickAction.Enable();
    }

    void PlaceObject()
    {
        if (objectPrefab == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = targetCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject obj = Instantiate(objectPrefab, hit.point, Quaternion.identity);
            Destroy(obj, 4f);  // 10秒后删除
        }
    }

    void OnDestroy()
    {
        clickAction?.Dispose();
    }
}