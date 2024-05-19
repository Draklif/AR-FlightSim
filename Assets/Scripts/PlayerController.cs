using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] new Camera camera;
    [SerializeField] PlaneBehaviour plane;
    [SerializeField] PlaneHUD planeHUD;

    Vector3 controlInput;
    PlaneCamera planeCamera;

    void Start()
    {
        planeCamera = GetComponent<PlaneCamera>();

        if (planeHUD != null)
        {
            planeHUD.SetPlane(plane);
            planeHUD.SetCamera(camera);
        }

        planeCamera.SetPlane(plane);
    }

    public void SetThrottleInput(InputAction.CallbackContext context)
    {
        if (plane == null) return;

        Debug.Log(context.ReadValue<float>());
        plane.SetThrottleInput(context.ReadValue<float>());
    }

    public void OnRollPitchInput(InputAction.CallbackContext context)
    {
        if (plane == null) return;

        var input = context.ReadValue<Vector2>();
        controlInput = new Vector3(input.y, controlInput.y, -input.x);
    }

    public void OnYawInput(InputAction.CallbackContext context)
    {
        if (plane == null) return;

        var input = context.ReadValue<float>();
        controlInput = new Vector3(controlInput.x, input, controlInput.z);
    }

    public void OnCameraInput(InputAction.CallbackContext context)
    {
        if (plane == null) return;

        var input = context.ReadValue<Vector2>();
        planeCamera.SetInput(input);
    }

    public void OnFlapsInput(InputAction.CallbackContext context)
    {
        if (plane == null) return;

        if (context.phase == InputActionPhase.Performed)
        {
            plane.ToggleFlaps();
        }
    }

    void Update()
    {
        if (plane != null) plane.SetControlInput(controlInput);
    }
}
