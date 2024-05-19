using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneBehaviour : MonoBehaviour
{
    // Privates
    [SerializeField] float healthMax;
    [SerializeField] float health;
    [SerializeField] float thrustMax;
    [SerializeField] float throttleSpeed;
    [SerializeField] float gLimit;
    [SerializeField] float gLimitPitch;  
    [SerializeField] bool flapsDeployed;

    float throttleInput;
    Vector3 lastVelocity;
    Vector3 controlInput;

    // Lift
    [SerializeField] float liftPower;
    [SerializeField] AnimationCurve liftAOACurve;
    [SerializeField] float inducedDrag;
    [SerializeField] AnimationCurve inducedDragCurve;
    [SerializeField] float rudderPower;
    [SerializeField] AnimationCurve rudderAOACurve;
    [SerializeField] AnimationCurve rudderInducedDragCurve;
    [SerializeField] float flapsLiftPower;
    [SerializeField] float flapsAOABias;
    [SerializeField] float flapsDrag;
    [SerializeField] float flapsRetractSpeed;

    // Steering
    [SerializeField] Vector3 turnSpeed;
    [SerializeField] Vector3 turnAcceleration;
    [SerializeField] AnimationCurve steeringCurve;

    // Drag
    [SerializeField] AnimationCurve dragForward;
    [SerializeField] AnimationCurve dragBack;
    [SerializeField] AnimationCurve dragLeft;
    [SerializeField] AnimationCurve dragRight;
    [SerializeField] AnimationCurve dragTop;
    [SerializeField] AnimationCurve dragBottom;
    [SerializeField] Vector3 angularDrag;
    [SerializeField] float airbrakeDrag;

    // Getter / Setter
    public Rigidbody Rigidbody { get; private set; }
    public float Throttle { get; private set; }
    public Vector3 EffectiveInput { get; private set; }
    public Vector3 Velocity { get; private set; }
    public Vector3 LocalVelocity { get; private set; }
    public Vector3 LocalGForce { get; private set; }
    public Vector3 LocalAngularVelocity { get; private set; }
    public float AngleOfAttack { get; private set; }
    public float AngleOfAttackYaw { get; private set; }

    // Flags
    public bool IsDead { get; private set; }
    public bool AirbrakeDeployed { get; private set; }
    public bool FlapsDeployed
    {
        get
        {
            return flapsDeployed;
        }
        private set
        {
            flapsDeployed = value;

            foreach (Collider collider in landingGearColliders)
            {
                collider.enabled = value;
            }

            foreach (GameObject wheel in landingGearWheels)
            {
                wheel.SetActive(value);
            }
        }
    }

    // Plane params
    public float MaxHealth
    {
        get
        {
            return healthMax;
        }
        set
        {
            healthMax = Mathf.Max(0, value);
        }
    }
    public float Health
    {
        get
        {
            return health;
        }
        private set
        {
            health = Mathf.Clamp(value, 0, healthMax);

            if (health == 0 && MaxHealth != 0 && !IsDead)
            {
                Die();
            }
        }
    }

    // Plane components
    [SerializeField] List<Collider> landingGearColliders;
    [SerializeField] List<GameObject> landingGearWheels;
    [SerializeField] PhysicMaterial landingGearBrakesMaterial;
    [SerializeField] List<GameObject> planeComponents;
    PhysicMaterial landingGearDefaultMaterial;

    // Default functions
    void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();

        if (landingGearColliders.Count > 0) landingGearDefaultMaterial = landingGearColliders[0].sharedMaterial;
    }

    void FixedUpdate()
    {
        float dt = Time.deltaTime;

        CalculateState(dt);
        CalculateGForce(dt);

        if (LocalVelocity.z > flapsRetractSpeed) FlapsDeployed = false;

        UpdateThrottle(dt);

        Rigidbody.AddRelativeForce(Throttle * thrustMax * Vector3.forward);

        UpdateLift();

        if (!IsDead) UpdateSteering(dt);

        UpdateDrag();

        CalculateState(dt);

        if (transform.position.y < -10) 
        {
            Health = 0;

            Rigidbody.isKinematic = true;
            Rigidbody.rotation = Quaternion.Euler(0, Rigidbody.rotation.eulerAngles.y, 0);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contactPoint = collision.contacts[i];

            if (landingGearColliders.Contains(contactPoint.thisCollider)) return;

            Health = 0;

            Rigidbody.isKinematic = true;
            Rigidbody.position = contactPoint.point;
            Rigidbody.rotation = Quaternion.Euler(0, Rigidbody.rotation.eulerAngles.y, 0);
        }
    }

    // Custom functions
    void Die()
    {
        throttleInput = 0;
        Throttle = 0;
        IsDead = true;

        foreach (var component in planeComponents)
        {
            component.SetActive(false);
        }
    }

    // Calculating functions
    void CalculateState(float dt)
    {
        Quaternion inverseRot = Quaternion.Inverse(Rigidbody.rotation);
        Velocity = Rigidbody.velocity;

        LocalVelocity = inverseRot * Velocity;
        LocalAngularVelocity = inverseRot * Rigidbody.angularVelocity;

        if (LocalVelocity.sqrMagnitude < 0.1f)
        {
            AngleOfAttack = 0;
            AngleOfAttackYaw = 0;
            return;
        }

        AngleOfAttack = Mathf.Atan2(-LocalVelocity.y, LocalVelocity.z);
        AngleOfAttackYaw = Mathf.Atan2(LocalVelocity.x, LocalVelocity.z);
    }

    void CalculateGForce(float dt)
    {
        Quaternion invRotation = Quaternion.Inverse(Rigidbody.rotation);
        Vector3 acceleration = (Velocity - lastVelocity) / dt;
        LocalGForce = invRotation * acceleration;
        lastVelocity = Velocity;
    }

    Vector3 CalculateLift(float angleOfAttack, Vector3 rightAxis, float liftPower, AnimationCurve aoaCurve, AnimationCurve inducedDragCurve)
    {
        Vector3 liftVelocity = Vector3.ProjectOnPlane(LocalVelocity, rightAxis);

        float liftCoefficient = aoaCurve.Evaluate(angleOfAttack * Mathf.Rad2Deg);
        float liftMagnituce = liftVelocity.sqrMagnitude * liftCoefficient * liftPower;

        Vector3 liftDirection = Vector3.Cross(liftVelocity.normalized, rightAxis);
        Vector3 lift = liftDirection * liftMagnituce;

        float dragForce = liftCoefficient * liftCoefficient;
        Vector3 dragDirection = -liftVelocity.normalized;
        Vector3 totalDrag = dragDirection * liftVelocity.sqrMagnitude * dragForce * inducedDrag * inducedDragCurve.Evaluate(Mathf.Max(0, LocalVelocity.z));

        return lift + totalDrag;
    }

    float CalculateGLimiter(Vector3 controlInput, Vector3 maxAngularVelocity)
    {
        if (controlInput.magnitude < 0.01f) return 1;

        //if the player gives input with magnitude less than 1, scale up their input so that magnitude == 1
        Vector3 maxInput = controlInput.normalized;

        Vector3 limit = Utilities.Scale6(maxInput,
            gLimit, gLimitPitch,    //pitch down, pitch up
            gLimit, gLimit,         //yaw
            gLimit, gLimit          //roll
        ) * 9.81f;

        Vector3 maxGForce = Vector3.Cross(Vector3.Scale(maxInput, maxAngularVelocity), LocalVelocity);

        if (maxGForce.magnitude > limit.magnitude) return limit.magnitude / maxGForce.magnitude;

        return 1;
    }

    float CalculateSteering(float dt, float angularVelocity, float targetVelocity, float acceleration)
    {
        float error = targetVelocity - angularVelocity;
        return Mathf.Clamp(error, -acceleration * dt, acceleration * dt);
    }

    // Updating functions
    void UpdateThrottle(float dt)
    {
        float target = 0;
        if (throttleInput > 0) target = 1;

        Throttle = Utilities.MoveTo(Throttle, target, throttleSpeed * Mathf.Abs(throttleInput), dt);

        AirbrakeDeployed = Throttle == 0 && throttleInput == -1;

        if (AirbrakeDeployed)
        {
            foreach (Collider gear in landingGearColliders)
            {
                gear.sharedMaterial = landingGearBrakesMaterial;
            }
        }
        else
        {
            foreach (Collider gear in landingGearColliders)
            {
                gear.sharedMaterial = landingGearDefaultMaterial;
            }
        }
    }

    void UpdateLift()
    {
        if (LocalVelocity.sqrMagnitude < 1f) return;

        float actualFlapsLiftPower = FlapsDeployed ? flapsLiftPower : 0;
        float actualFlapsAOABias = FlapsDeployed ? flapsAOABias : 0;

        Vector3 liftForce = CalculateLift(
            AngleOfAttack + (actualFlapsAOABias * Mathf.Deg2Rad), Vector3.right,
            liftPower + actualFlapsLiftPower,
            liftAOACurve,
            inducedDragCurve
        );

        Vector3 yawForce = CalculateLift(AngleOfAttackYaw, Vector3.up, rudderPower, rudderAOACurve, rudderInducedDragCurve);

        Debug.Log("yaw >" + yawForce);
        Debug.Log("lift >" + liftForce);

        Rigidbody.AddRelativeForce(liftForce);
        Rigidbody.AddRelativeForce(yawForce);
    }

    void UpdateDrag()
    {
        var lv = LocalVelocity;
        var lv2 = lv.sqrMagnitude;  //velocity squared

        float actualAirbrakeDrag = AirbrakeDeployed ? airbrakeDrag : 0;
        float actualFlapsDrag = FlapsDeployed ? flapsDrag : 0;

        //calculate coefficient of drag depending on direction on velocity
        Vector3 coefficient = Utilities.Scale6(
            lv.normalized,
            dragRight.Evaluate(Mathf.Abs(lv.x)), dragLeft.Evaluate(Mathf.Abs(lv.x)),
            dragTop.Evaluate(Mathf.Abs(lv.y)), dragBottom.Evaluate(Mathf.Abs(lv.y)),
            dragForward.Evaluate(Mathf.Abs(lv.z)) + actualAirbrakeDrag + actualFlapsDrag,
            dragBack.Evaluate(Mathf.Abs(lv.z))
        );

        Vector3 dragForce = coefficient.magnitude * lv2 * -lv.normalized;    //drag is opposite direction of velocity

        Rigidbody.AddRelativeForce(dragForce);

        var av = LocalAngularVelocity;
        var dragForceSquared = av.sqrMagnitude * -av.normalized;    //squared, opposite direction of angular velocity
        Rigidbody.AddRelativeTorque(Vector3.Scale(dragForceSquared, angularDrag), ForceMode.Acceleration);  //ignore rigidbody mass
    }

    void UpdateSteering(float dt)
    {
        var speed = Mathf.Max(0, LocalVelocity.z);
        var steeringPower = steeringCurve.Evaluate(speed);

        var gForceScaling = CalculateGLimiter(controlInput, turnSpeed * Mathf.Deg2Rad * steeringPower);

        var targetAV = Vector3.Scale(controlInput, turnSpeed * steeringPower * gForceScaling);
        var av = LocalAngularVelocity * Mathf.Rad2Deg;

        var correction = new Vector3(
            CalculateSteering(dt, av.x, targetAV.x, turnAcceleration.x * steeringPower),
            CalculateSteering(dt, av.y, targetAV.y, turnAcceleration.y * steeringPower),
            CalculateSteering(dt, av.z, targetAV.z, turnAcceleration.z * steeringPower)
        );

        Rigidbody.AddRelativeTorque(correction * Mathf.Deg2Rad, ForceMode.VelocityChange);    //ignore rigidbody mass

        var correctionInput = new Vector3(
            Mathf.Clamp((targetAV.x - av.x) / turnAcceleration.x, -1, 1),
            Mathf.Clamp((targetAV.y - av.y) / turnAcceleration.y, -1, 1),
            Mathf.Clamp((targetAV.z - av.z) / turnAcceleration.z, -1, 1)
        );

        var effectiveInput = (correctionInput + controlInput) * gForceScaling;

        EffectiveInput = new Vector3(
            Mathf.Clamp(effectiveInput.x, -1, 1),
            Mathf.Clamp(effectiveInput.y, -1, 1),
            Mathf.Clamp(effectiveInput.z, -1, 1)
        );
    }

    // Controls
    public void SetControlInput(Vector3 input)
    {
        if (!IsDead) controlInput = input;
    }

    public void SetThrottleInput(float input)
    {
        if (!IsDead) throttleInput = input;
    }
    public void ToggleFlaps()
    {
        if (LocalVelocity.z < flapsRetractSpeed) FlapsDeployed = !FlapsDeployed;
    }

}
