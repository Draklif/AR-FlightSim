using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlaneHUD : MonoBehaviour {
    [SerializeField] float updateRate;
    [SerializeField] float hudFocusDistance;
    [SerializeField] Compass compass;
    [SerializeField] PitchLadder pitchLadder;
    [SerializeField] Bar throttleBar;
    [SerializeField] Transform hudCenter;
    [SerializeField] Transform velocityMarker;
    [SerializeField] Transform altimeterMarker;
    [SerializeField] TMP_Text airspeed;
    [SerializeField] TMP_Text aoaIndicator;
    [SerializeField] TMP_Text gforceIndicator;
    [SerializeField] TMP_Text altitude;

    PlaneBehaviour plane;
    Transform planeTransform;
    new Camera camera;
    Transform cameraTransform;

    GameObject hudCenterGO;
    GameObject velocityMarkerGO;
    GameObject altimeterMarkerGO;

    float lastUpdateTime;

    const float metersToKnots = 1.94384f;
    const float metersToFeet = 3.28084f;

    void Start() {
        hudCenterGO = hudCenter.gameObject;
        velocityMarkerGO = velocityMarker.gameObject;
        altimeterMarkerGO = altimeterMarker.gameObject;
    }

    public void SetPlane(PlaneBehaviour plane) {
        this.plane = plane;

        if (plane == null) {
            planeTransform = null;
        }
        else {
            planeTransform = plane.GetComponent<Transform>();
        }

        if (compass != null)
        {
            compass.SetPlane(plane);
        }

        if (pitchLadder != null)
        {
            pitchLadder.SetPlane(plane);
        }
    }

    public void SetCamera(Camera camera) {
        this.camera = camera;

        if (camera == null) {
            cameraTransform = null;
        } else {
            cameraTransform = camera.GetComponent<Transform>();
        }

        if (compass != null)
        {
            compass.SetCamera(camera);
        }

        if (pitchLadder != null)
        {
            pitchLadder.SetCamera(camera);
        }
    }

    void UpdateMarkers() {
        var velocity = planeTransform.forward;

        if (plane.LocalVelocity.sqrMagnitude > 1) {
            velocity = plane.Rigidbody.velocity;
        }

        var hudPos = TransformToHUDSpace(plane.Rigidbody.position + velocity * hudFocusDistance);

        if (hudPos.z > 0) {
            velocityMarkerGO.SetActive(true);
            altimeterMarkerGO.SetActive(true);
            velocityMarker.localPosition = new Vector3(hudPos.x - 200f, hudPos.y, 0);
            altimeterMarker.localPosition = new Vector3(hudPos.x + 200f, hudPos.y, 0);
        } else {
            velocityMarkerGO.SetActive(false);
            altimeterMarkerGO.SetActive(false);
        }
    }

    void UpdateAirspeed() {
        var speed = plane.LocalVelocity.z * metersToKnots;
        airspeed.text = string.Format("{0:0}", speed);
    }

    void UpdateAOA() {
        aoaIndicator.text = string.Format("{0:0.0} AOA", plane.AngleOfAttack * Mathf.Rad2Deg);
    }

    void UpdateGForce() {
        var gforce = plane.LocalGForce.y / 9.81f;
        gforceIndicator.text = string.Format("{0:0.0} G", gforce);
    }
    void UpdateAltitude()
    {
        var altitude = plane.Rigidbody.position.y * metersToFeet;
        this.altitude.text = string.Format("{0:0}", altitude);
    }

    Vector3 TransformToHUDSpace(Vector3 worldSpace) {
        var screenSpace = camera.WorldToScreenPoint(worldSpace);
        return screenSpace - new Vector3(camera.pixelWidth / 2, camera.pixelHeight / 2);
    }

    void UpdateHUDCenter() {
        var rotation = cameraTransform.localEulerAngles;
        var hudPos = TransformToHUDSpace(planeTransform.position + planeTransform.forward * hudFocusDistance);

        if (hudPos.z > 0) {
            hudCenterGO.SetActive(true);
            hudCenter.localPosition = new Vector3(hudPos.x, hudPos.y, 0);
            hudCenter.localEulerAngles = new Vector3(0, 0, -rotation.z);
        } else {
            hudCenterGO.SetActive(false);
        }
    }

    void LateUpdate() {
        if (plane == null) return;
        if (camera == null) return;

        float degreesToPixels = camera.pixelHeight / camera.fieldOfView;

        throttleBar.SetValue(plane.Throttle);

        if (!plane.IsDead) {
            UpdateMarkers();
            UpdateHUDCenter();
        } else {
            hudCenterGO.SetActive(false);
            velocityMarkerGO.SetActive(false);
        }

        UpdateAirspeed();
        UpdateAltitude();

        if (Time.time > lastUpdateTime + (1f / updateRate)) {
            UpdateAOA();
            UpdateGForce();
            lastUpdateTime = Time.time;
        }
    }
}
