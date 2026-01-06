 
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem;


[AddComponentMenu("JU TPS/Mobile/GyroLook")]
public class GyroLook : OnScreenControl
{
 

    [Range(0.0f, 10.0f)] public float XSensitivity = 1f;
    [Range(0.0f, 10.0f)] public float YSensitivity = 1f;

    [SerializeField, InputControl(layout = "Vector2")]
    private string _controlPath;

    protected override string controlPathInternal
    {
        get => _controlPath;
        set => _controlPath = value;
    }
    void Awake()
    {   
        if (UnityEngine.InputSystem.Gyroscope.current != null) {
            InputSystem.EnableDevice(UnityEngine.InputSystem.Gyroscope.current);
        }

    }

 
        void Update()
    {
        if (UnityEngine.InputSystem.Gyroscope.current != null)
        {

            Vector3 angularVelocity =UnityEngine.InputSystem.Gyroscope.current.angularVelocity.ReadValue();
            SendValueToControl(new Vector2(-angularVelocity.y * YSensitivity, angularVelocity.x * XSensitivity));
        }


    }


    void OnDestroy()
    {
        if (UnityEngine.InputSystem.Gyroscope.current != null)
        {
            InputSystem.DisableDevice(UnityEngine.InputSystem.Gyroscope.current);
        }
    }






}
