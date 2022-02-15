using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class CycleGeometry
{
    public GameObject handles, lowerFork, fWheelVisual, RWheel, crank, lPedal, rPedal, fGear, rGear;
}
[System.Serializable]
public class PedalAdjustments
{
    public float crankRadius;
    public Vector3 lPedalOffset, rPedalOffset;
    public float pedalingSpeed;
}
[System.Serializable]
public class WheelFrictionSettings
{
    public PhysicMaterial fPhysicMaterial, rPhysicMaterial;
    public Vector2 fFriction, rFriction;
}
public class BicycleController : MonoBehaviour
{
    public CycleGeometry cycleGeometry;
    public GameObject fPhysicsWheel, rPhysicsWheel;
    public WheelFrictionSettings wheelFrictionSettings;
    public float steerAngle, axisAngle, leanAngle;
    public float topSpeed, torque, speedGain;
    public Vector3 COM;
    [HideInInspector]
    public bool isReversing;
    [Range(0, 4)]
    public float oscillationAmount;
    [Range(0, 1)]
    public float oscillationAffectSteerRatio;
    float oscillationSteerEffect;
    [HideInInspector]
    public float cycleOscillation;
    Rigidbody rb, fWheelRb, rWheelRb;
    float turnAngle;
    float xQuat, zQuat;
    [HideInInspector]
    public float crankSpeed, crankCurrentQuat, crankLastQuat;
    public PedalAdjustments pedalAdjustments;
    [HideInInspector]
    public float turnLeanAmount;
    RaycastHit hit;
    [HideInInspector]
    public float customSteerAxis, customLeanAxis, customAccelerationAxis;
    [HideInInspector]
    public float relaxedSpeed, initialTopSpeed, pickUpSpeed;
    Quaternion initialLowerForkLocalRotaion, initialHandlesRotation;

    //Ground Conformity
    public bool groundConformity;
    RaycastHit hitGround;
    Vector3 theRay;
    float groundZ;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = Mathf.Infinity;

        fWheelRb = fPhysicsWheel.GetComponent<Rigidbody>();
        fWheelRb.maxAngularVelocity = Mathf.Infinity;

        rWheelRb = rPhysicsWheel.GetComponent<Rigidbody>();
        rWheelRb.maxAngularVelocity = Mathf.Infinity;

        initialTopSpeed = topSpeed;
        relaxedSpeed = topSpeed / 2;

        initialHandlesRotation = cycleGeometry.handles.transform.localRotation;
        initialLowerForkLocalRotaion = cycleGeometry.lowerFork.transform.localRotation;

    }

    void FixedUpdate()
    {

        //SteerControl
        GetSmoothRawAxis("Horizontal", ref customSteerAxis, 5, 5);
        fPhysicsWheel.transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y + customSteerAxis * steerAngle + oscillationSteerEffect, 0);
        fPhysicsWheel.GetComponent<ConfigurableJoint>().axis = new Vector3(0, 1, 0);
        fPhysicsWheel.GetComponent<ConfigurableJoint>().axis = new Vector3(1, 0, 0);


        //PowerControl
        if (!Input.GetKey(KeyCode.LeftShift))
            topSpeed = Mathf.Lerp(topSpeed, relaxedSpeed, Time.deltaTime);
        else
            topSpeed = Mathf.Lerp(topSpeed, initialTopSpeed, Time.deltaTime);

        if (rb.velocity.magnitude < topSpeed && Input.GetAxisRaw("Vertical") > 0)
            rWheelRb.AddTorque(transform.right * torque * Input.GetAxis("Vertical"));

        //Body
        rb.centerOfMass = COM;
        if (rb.velocity.magnitude < topSpeed && Input.GetAxisRaw("Vertical") > 0)
            rb.AddForce(transform.forward * speedGain);

        if (rb.velocity.magnitude < topSpeed * 0.05f && Input.GetAxisRaw("Vertical") < 0)
            rb.AddForce(-transform.forward * speedGain * 0.5f);

        if (transform.InverseTransformDirection(rb.velocity).z < 0)
            isReversing = true;
        else
            isReversing = false;

        if (Input.GetAxisRaw("Vertical") < 0 && isReversing == false)
            rb.AddForce(-transform.forward * speedGain * 2);

        //Handles
        cycleGeometry.handles.transform.localRotation = Quaternion.Euler(0, customSteerAxis * steerAngle + oscillationSteerEffect * 5, 0) * initialHandlesRotation;

        //LowerFork
        cycleGeometry.lowerFork.transform.localRotation = Quaternion.Euler(0, customSteerAxis * steerAngle + oscillationSteerEffect * 5, customSteerAxis * -axisAngle) * initialLowerForkLocalRotaion;

        //FWheelVisual
        xQuat = Mathf.Sin(Mathf.Deg2Rad * (transform.rotation.eulerAngles.y));
        zQuat = Mathf.Cos(Mathf.Deg2Rad * (transform.rotation.eulerAngles.y));
        cycleGeometry.fWheelVisual.transform.rotation = Quaternion.Euler(xQuat * (customSteerAxis * -axisAngle), customSteerAxis * steerAngle + oscillationSteerEffect * 5, zQuat * (customSteerAxis * -axisAngle));
        cycleGeometry.fWheelVisual.transform.GetChild(0).transform.localRotation = cycleGeometry.RWheel.transform.rotation;

        //Crank
        crankCurrentQuat = cycleGeometry.RWheel.transform.rotation.eulerAngles.x;
        if (Input.GetAxis("Vertical") > 0)
        {
            crankSpeed += Mathf.Sqrt(Input.GetAxis("Vertical") * Mathf.Abs(Mathf.DeltaAngle(crankCurrentQuat, crankLastQuat) * pedalAdjustments.pedalingSpeed));
            crankSpeed %= 360;
        }
        else if (Mathf.Floor(crankSpeed) > 10)
            crankSpeed += -6;
        crankLastQuat = crankCurrentQuat;
        cycleGeometry.crank.transform.localRotation = Quaternion.Euler(crankSpeed, 0, 0);

        //Pedals
        cycleGeometry.lPedal.transform.localPosition = pedalAdjustments.lPedalOffset + new Vector3(0, Mathf.Cos(Mathf.Deg2Rad * (crankSpeed + 180)) * pedalAdjustments.crankRadius, Mathf.Sin(Mathf.Deg2Rad * (crankSpeed + 180)) * pedalAdjustments.crankRadius);
        cycleGeometry.rPedal.transform.localPosition = pedalAdjustments.rPedalOffset + new Vector3(0, Mathf.Cos(Mathf.Deg2Rad * (crankSpeed)) * pedalAdjustments.crankRadius, Mathf.Sin(Mathf.Deg2Rad * (crankSpeed)) * pedalAdjustments.crankRadius);

        //FGear
        cycleGeometry.fGear.transform.rotation = cycleGeometry.crank.transform.rotation;
        //RGear
        cycleGeometry.rGear.transform.rotation = rPhysicsWheel.transform.rotation;

        //CycleOscillation
        if (Input.GetKey(KeyCode.LeftShift) && rb.velocity.magnitude > 5 && isReversing == false)
            pickUpSpeed += Time.deltaTime * 2;
        else
            pickUpSpeed -= Time.deltaTime * 2;

        pickUpSpeed = Mathf.Clamp(pickUpSpeed, 0.1f, 1);

        GetSmoothRawAxis("Vertical", ref customAccelerationAxis, 1, 1);
        cycleOscillation = -Mathf.Sin(Mathf.Deg2Rad * (crankSpeed + 90)) * (oscillationAmount * (Mathf.Clamp(topSpeed / rb.velocity.magnitude, 1f, 1.5f))) * pickUpSpeed;
        GetSmoothRawAxis("Horizontal", ref customLeanAxis, 1, 1f);
        turnLeanAmount = customLeanAxis * -leanAngle * Mathf.Clamp(rb.velocity.magnitude * 0.1f, 0, 1);
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, turnLeanAmount + cycleOscillation + GroundConformity(groundConformity));
        oscillationSteerEffect = cycleOscillation * Mathf.Clamp01(Input.GetAxis("Vertical")) * (oscillationAffectSteerRatio * (Mathf.Clamp(topSpeed / rb.velocity.magnitude, 1f, 1.5f)));

        //FrictionSettings
        wheelFrictionSettings.fPhysicMaterial.staticFriction = wheelFrictionSettings.fFriction.x;
        wheelFrictionSettings.fPhysicMaterial.dynamicFriction = wheelFrictionSettings.fFriction.y;
        wheelFrictionSettings.rPhysicMaterial.staticFriction = wheelFrictionSettings.rFriction.x;
        wheelFrictionSettings.rPhysicMaterial.dynamicFriction = wheelFrictionSettings.rFriction.y;

        if (Physics.Raycast(fPhysicsWheel.transform.position, Vector3.down, out hit, Mathf.Infinity))
            if (hit.distance < 0.5f)
            {
                Vector3 velf = fPhysicsWheel.transform.InverseTransformDirection(fPhysicsWheel.GetComponent<Rigidbody>().velocity);
                velf.x *= Mathf.Clamp01(1 / (wheelFrictionSettings.fFriction.x + wheelFrictionSettings.fFriction.y));
                fPhysicsWheel.GetComponent<Rigidbody>().velocity = fPhysicsWheel.transform.TransformDirection(velf);
            }
        if (Physics.Raycast(rPhysicsWheel.transform.position, Vector3.down, out hit, Mathf.Infinity))
            if (hit.distance < 0.5f)
            {
                Vector3 velr = rPhysicsWheel.transform.InverseTransformDirection(rPhysicsWheel.GetComponent<Rigidbody>().velocity);
                velr.x *= Mathf.Clamp01(1 / (wheelFrictionSettings.rFriction.x + wheelFrictionSettings.rFriction.y));
                rPhysicsWheel.GetComponent<Rigidbody>().velocity = rPhysicsWheel.transform.TransformDirection(velr);
            }

    }
    float GroundConformity(bool toggle)
    {
        if (toggle)
        {
            groundZ = transform.rotation.eulerAngles.z;
        }
        return groundZ;

    }
    float GetSmoothRawAxis(string name, ref float axis, float sensitivity, float gravity)
    {
        var r = Input.GetAxisRaw(name);
        var s = sensitivity;
        var g = gravity;
        var t = Time.unscaledDeltaTime;

        if (r != 0)
            axis = Mathf.Clamp(axis + r * s * t, -1f, 1f);
        else
            axis = Mathf.Clamp01(Mathf.Abs(axis) - g * t) * Mathf.Sign(axis);
        return axis;
    }

}

//***MOBILE CONTROLS****//

// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
// [System.Serializable]
// public class CycleGeometry
// {
//     public GameObject handles, lowerFork, fWheelVisual, RWheel, crank, lPedal, rPedal, fGear, rGear;
// }
// [System.Serializable]
// public class PedalAdjustments
// {
//     public float crankRadius;
//     public Vector3 lPedalOffset, rPedalOffset;
//     public float pedalingSpeed;
// }
// [System.Serializable]
// public class WheelFrictionSettings
// {
//     public PhysicMaterial fPhysicMaterial, rPhysicMaterial;
//     public Vector2 fFriction, rFriction;
// }
// public class BicycleController : MonoBehaviour
// {
//     public CycleGeometry cycleGeometry;
//     public GameObject fPhysicsWheel, rPhysicsWheel;
//     public WheelFrictionSettings wheelFrictionSettings;
//     public float steerAngle, axisAngle, leanAngle;
//     public float topSpeed, torque, speedGain;
//     public Vector3 COM;
//     [HideInInspector]
//     public bool isReversing;
//     [Range(0, 4)]
//     public float oscillationAmount;
//     [Range(0, 1)]
//     public float oscillationAffectSteerRatio;
//     float oscillationSteerEffect;
//     [HideInInspector]
//     public float cycleOscillation;
//     Rigidbody rb, fWheelRb, rWheelRb;
//     float turnAngle;
//     float xQuat, zQuat;
//     [HideInInspector]
//     public float crankSpeed, crankCurrentQuat, crankLastQuat;
//     public PedalAdjustments pedalAdjustments;
//     [HideInInspector]
//     public float turnLeanAmount;
//     RaycastHit hit;
//     [HideInInspector]
//     public float customSteerAxis, customLeanAxis, customAccelerationAxis;
//     [HideInInspector]
//     public float relaxedSpeed, initialTopSpeed, pickUpSpeed;
//     Quaternion initialLowerForkLocalRotaion, initialHandlesRotation;

//     //Ground Conformity
//     public bool groundConformity;
//     RaycastHit hitGround;
//     Vector3 theRay;
//     float groundZ;

//     [Header("Mobile Controls")]
//     public float sensitivityMobile = 5.0f;
//     public float rangeMobile = 5.0f;
//     private bool touchStart = false;
//     private Vector2 pointA;
//     private Vector2 pointB;
//     public Transform circle;
//     Vector2 clampedDirection;
//     public Transform outerCircle;

//     void Start()
//     {
//         rb = GetComponent<Rigidbody>();
//         rb.maxAngularVelocity = Mathf.Infinity;

//         fWheelRb = fPhysicsWheel.GetComponent<Rigidbody>();
//         fWheelRb.maxAngularVelocity = Mathf.Infinity;

//         rWheelRb = rPhysicsWheel.GetComponent<Rigidbody>();
//         rWheelRb.maxAngularVelocity = Mathf.Infinity;

//         initialTopSpeed = topSpeed;
//         relaxedSpeed = topSpeed / 2;

//         initialHandlesRotation = cycleGeometry.handles.transform.localRotation;
//         initialLowerForkLocalRotaion = cycleGeometry.lowerFork.transform.localRotation;

//     }

//     void Update () {
//         if(Input.GetMouseButtonDown(0)){
//             pointA = new Vector2(Input.mousePosition.x*sensitivityMobile, Input.mousePosition.y*sensitivityMobile);
//         }
//         if(Input.GetMouseButton(0)){
//             touchStart = true;
//             pointB = new Vector2(Input.mousePosition.x*sensitivityMobile, Input.mousePosition.y*sensitivityMobile);
//         }else{
//             touchStart = false;
//         }
        
// 	}

//     void FixedUpdate()
//     {
//         //Mobile Controls
//         if(touchStart){
//             Vector2 offset = pointA - pointB;
//             Vector2 direction = Vector2.ClampMagnitude(offset, rangeMobile);
//             clampedDirection = (direction *-1)/rangeMobile;
//             circle.transform.position = new Vector2(-outerCircle.transform.position.x + direction.x,-outerCircle.transform.position.y+ direction.y) * -1;
//         }
//         else{
//             clampedDirection = Vector2.zero;
//             circle.transform.position = new Vector2(Mathf.Lerp(circle.transform.position.x,outerCircle.transform.position.x,Time.deltaTime*10),Mathf.Lerp(circle.transform.position.y,outerCircle.transform.position.y,Time.deltaTime*10));
//         }
//         //SteerControl
//         GetSmoothRawAxis("Horizontal", ref customSteerAxis, 3, 3);
//         fPhysicsWheel.transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y + customSteerAxis * steerAngle + oscillationSteerEffect, 0);
//         fPhysicsWheel.GetComponent<ConfigurableJoint>().axis = new Vector3(0, 1, 0);
//         fPhysicsWheel.GetComponent<ConfigurableJoint>().axis = new Vector3(1, 0, 0);


//         //PowerControl
//         if (clampedDirection.y<0.8f)
//             topSpeed = Mathf.Lerp(topSpeed, relaxedSpeed, Time.deltaTime);
//         else
//             topSpeed = Mathf.Lerp(topSpeed, initialTopSpeed, Time.deltaTime);

//         if (rb.velocity.magnitude < topSpeed && clampedDirection.y > 0)
//             rWheelRb.AddTorque(transform.right * torque * clampedDirection.y);

//         //Body
//         rb.centerOfMass = COM;
//         if (rb.velocity.magnitude < topSpeed && clampedDirection.y > 0)
//             rb.AddForce(transform.forward * speedGain);

//         if (rb.velocity.magnitude < topSpeed * 0.05f && clampedDirection.y < 0)
//             rb.AddForce(-transform.forward * speedGain * 0.5f);

//         if (transform.InverseTransformDirection(rb.velocity).z < 0)
//             isReversing = true;
//         else
//             isReversing = false;

//         if (clampedDirection.y < 0 && isReversing == false)
//             rb.AddForce(-transform.forward * speedGain * 2);

//         //Handles
//         cycleGeometry.handles.transform.localRotation = Quaternion.Euler(0, customSteerAxis * steerAngle + oscillationSteerEffect * 5, 0) * initialHandlesRotation;

//         //LowerFork
//         cycleGeometry.lowerFork.transform.localRotation = Quaternion.Euler(0, customSteerAxis * steerAngle + oscillationSteerEffect * 5, customSteerAxis * -axisAngle) * initialLowerForkLocalRotaion;

//         //FWheelVisual
//         xQuat = Mathf.Sin(Mathf.Deg2Rad * (transform.rotation.eulerAngles.y));
//         zQuat = Mathf.Cos(Mathf.Deg2Rad * (transform.rotation.eulerAngles.y));
//         cycleGeometry.fWheelVisual.transform.rotation = Quaternion.Euler(xQuat * (customSteerAxis * -axisAngle), customSteerAxis * steerAngle + oscillationSteerEffect * 5, zQuat * (customSteerAxis * -axisAngle));
//         cycleGeometry.fWheelVisual.transform.GetChild(0).transform.localRotation = cycleGeometry.RWheel.transform.rotation;

//         //Crank
//         crankCurrentQuat = cycleGeometry.RWheel.transform.rotation.eulerAngles.x;
//         if (clampedDirection.y > 0)
//         {
//             crankSpeed += Mathf.Sqrt(clampedDirection.y * Mathf.Abs(Mathf.DeltaAngle(crankCurrentQuat, crankLastQuat) * pedalAdjustments.pedalingSpeed));
//             crankSpeed %= 360;
//         }
//         else if (Mathf.Floor(crankSpeed) > 10)
//             crankSpeed += -6;
//         crankLastQuat = crankCurrentQuat;
//         cycleGeometry.crank.transform.localRotation = Quaternion.Euler(crankSpeed, 0, 0);

//         //Pedals
//         cycleGeometry.lPedal.transform.localPosition = pedalAdjustments.lPedalOffset + new Vector3(0, Mathf.Cos(Mathf.Deg2Rad * (crankSpeed + 180)) * pedalAdjustments.crankRadius, Mathf.Sin(Mathf.Deg2Rad * (crankSpeed + 180)) * pedalAdjustments.crankRadius);
//         cycleGeometry.rPedal.transform.localPosition = pedalAdjustments.rPedalOffset + new Vector3(0, Mathf.Cos(Mathf.Deg2Rad * (crankSpeed)) * pedalAdjustments.crankRadius, Mathf.Sin(Mathf.Deg2Rad * (crankSpeed)) * pedalAdjustments.crankRadius);

//         //FGear
//         cycleGeometry.fGear.transform.rotation = cycleGeometry.crank.transform.rotation;
//         //RGear
//         cycleGeometry.rGear.transform.rotation = rPhysicsWheel.transform.rotation;

//         //CycleOscillation
//         if (clampedDirection.y>0.8f && rb.velocity.magnitude > 5 && isReversing == false)
//             pickUpSpeed += Time.deltaTime * 2;
//         else
//             pickUpSpeed -= Time.deltaTime * 2;

//         pickUpSpeed = Mathf.Clamp(pickUpSpeed, 0.1f, 1);

//         GetSmoothRawAxis("Vertical", ref customAccelerationAxis, 1, 1);
//         cycleOscillation = -Mathf.Sin(Mathf.Deg2Rad * (crankSpeed + 90)) * (oscillationAmount * (Mathf.Clamp(topSpeed / rb.velocity.magnitude, 1f, 1.5f))) * pickUpSpeed;
//         GetSmoothRawAxis("Horizontal", ref customLeanAxis, 1, 1f);
//         turnLeanAmount = customLeanAxis * -leanAngle * Mathf.Clamp(rb.velocity.magnitude * 0.1f, 0, 1);
//         transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, turnLeanAmount + cycleOscillation + GroundConformity(groundConformity));
//         oscillationSteerEffect = cycleOscillation * Mathf.Clamp01(clampedDirection.y) * (oscillationAffectSteerRatio * (Mathf.Clamp(topSpeed / rb.velocity.magnitude, 1f, 1.5f)));

//         //FrictionSettings
//         wheelFrictionSettings.fPhysicMaterial.staticFriction = wheelFrictionSettings.fFriction.x;
//         wheelFrictionSettings.fPhysicMaterial.dynamicFriction = wheelFrictionSettings.fFriction.y;
//         wheelFrictionSettings.rPhysicMaterial.staticFriction = wheelFrictionSettings.rFriction.x;
//         wheelFrictionSettings.rPhysicMaterial.dynamicFriction = wheelFrictionSettings.rFriction.y;

//         if (Physics.Raycast(fPhysicsWheel.transform.position, Vector3.down, out hit, Mathf.Infinity))
//             if (hit.distance < 0.5f)
//             {
//                 Vector3 velf = fPhysicsWheel.transform.InverseTransformDirection(fPhysicsWheel.GetComponent<Rigidbody>().velocity);
//                 velf.x *= Mathf.Clamp01(1 / (wheelFrictionSettings.fFriction.x + wheelFrictionSettings.fFriction.y));
//                 fPhysicsWheel.GetComponent<Rigidbody>().velocity = fPhysicsWheel.transform.TransformDirection(velf);
//             }
//         if (Physics.Raycast(rPhysicsWheel.transform.position, Vector3.down, out hit, Mathf.Infinity))
//             if (hit.distance < 0.5f)
//             {
//                 Vector3 velr = rPhysicsWheel.transform.InverseTransformDirection(rPhysicsWheel.GetComponent<Rigidbody>().velocity);
//                 velr.x *= Mathf.Clamp01(1 / (wheelFrictionSettings.rFriction.x + wheelFrictionSettings.rFriction.y));
//                 rPhysicsWheel.GetComponent<Rigidbody>().velocity = rPhysicsWheel.transform.TransformDirection(velr);
//             }

//     }
//     float GroundConformity(bool toggle)
//     {
//         if (toggle)
//         {
//             groundZ = transform.rotation.eulerAngles.z;
//         }
//         return groundZ;

//     }
//     float GetSmoothRawAxis(string name, ref float axis, float sensitivity, float gravity)
//     {
//         var r = name=="Vertical"?clampedDirection.y:clampedDirection.x;
//         var s = sensitivity;
//         var g = gravity;
//         var t = Time.unscaledDeltaTime;

//         if (r != 0)
//             axis = Mathf.Clamp(axis + r * s * t, -1f, 1f);
//         else
//             axis = Mathf.Clamp01(Mathf.Abs(axis) - g * t) * Mathf.Sign(axis);
//         return axis;
//     }
// }