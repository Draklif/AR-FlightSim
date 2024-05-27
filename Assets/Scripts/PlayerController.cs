using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.IO.Ports;

public class PlayerController : MonoBehaviour
{
    [SerializeField] new Camera camera;
    [SerializeField] PlaneBehaviour plane;
    [SerializeField] PlaneHUD planeHUD;
    [SerializeField] GameObject hudPause;
    [SerializeField] GameObject hudMain;
    [SerializeField] GameObject hudDeath;

    Vector3 controlInput;
    PlaneCamera planeCamera;
    SerialPort serialPort = new SerialPort("COM3", 9600);

    float serialThrottle;
    float serialRoll;
    float serialPitch;
    bool isPaused = false;

    public bool usingXbox = false;

    void Awake()
    {
        if (!usingXbox)
        {
            serialPort.Open();
            StartCoroutine(ReadDataFromSerialPort());
        }
    }

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

    private void FixedUpdate()
    {
        if (plane.IsDead)
        {
            hudDeath.SetActive(true);
        }
    }

    IEnumerator ReadDataFromSerialPort()
    {
        if (!usingXbox)
        {
            while (serialPort.IsOpen)
            {
                
                try
                {
                    string[] values = serialPort.ReadLine().Split(',');

                    serialPitch = (float.Parse(values[0])) / 100;
                    serialRoll = (float.Parse(values[1])) / 100;
                    serialThrottle = (float.Parse(values[2])) / 100;

                    plane.SetThrottleInput(serialThrottle);
                    controlInput = new Vector3(serialPitch, controlInput.y, -serialRoll);
                }
                catch 
                {
                    controlInput = new Vector3(0, controlInput.y, 0);
                    Debug.Log("Excepción producida. Error al leer.");
                }

                yield return new WaitForSeconds(.03f);
            }
        }
    }

    public void SetThrottleInput(InputAction.CallbackContext context)
    {
        if (usingXbox)
        {
            if (plane == null) return;

            plane.SetThrottleInput(context.ReadValue<float>());
        }
    }

    public void OnRollPitchInput(InputAction.CallbackContext context)
    {
        if (usingXbox)
        {
            if (plane == null) return;

            var input = context.ReadValue<Vector2>();
            controlInput = new Vector3(input.y, controlInput.y, -input.x);
        }
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

    public void OnPauseInput(InputAction.CallbackContext context)
    {
        Pause();
    }

    public void Pause()
    {
        isPaused = !isPaused;
        hudPause.SetActive(isPaused);
        if (!plane.IsDead) hudMain.SetActive(!isPaused);
        Time.timeScale = isPaused ? 0 : 1;
    }

    public void Reload(string sceneName)
    {
        Load(sceneName);
        if (!hudDeath.active)
        {
            Pause();
        }
    }

    public void Load(string sceneName)
    {
        serialPort.Close();
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    void Update()
    {
        if (plane != null) plane.SetControlInput(controlInput);
    }
}
