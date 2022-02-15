using UnityEngine;
using System.Collections;
 
public class BicycleCamera : MonoBehaviour {
 
	public Transform target;
	public float distance = 20.0f;
	public float height = 5.0f;
	public float heightDamping = 2.0f;
 
	public float lookAtHeight = 0.0f;
 
	public Rigidbody parentRigidbody;
 
	public float rotationSnapTime = 0.3F;
 
	public float distanceSnapTime;
	public float distanceMultiplier;
 
	private Vector3 lookAtVector;
 
	private float usedDistance;
 
	float wantedRotationAngle;
	float wantedHeight;
 
	float currentRotationAngle;
	float currentHeight;

	Vector3 wantedPosition;
 
	private float yVelocity = 0.0F;
	private float zVelocity = 0.0F;
	PerfectMouseLook perfectMouseLook;

	
 
	void Start () {
		perfectMouseLook =  GetComponent<PerfectMouseLook>();
		target = GameObject.FindObjectOfType<BicycleController>().transform;
		parentRigidbody = target.GetComponent<Rigidbody>();
		lookAtVector =  new Vector3(0,lookAtHeight,0);
	}
 
	void LateUpdate () {
 
		wantedHeight = target.position.y + height;
		currentHeight = transform.position.y;
 
		wantedRotationAngle = target.eulerAngles.y;
		currentRotationAngle = transform.eulerAngles.y;
		if(perfectMouseLook.movement==false)
		currentRotationAngle = Mathf.SmoothDampAngle(currentRotationAngle, wantedRotationAngle, ref yVelocity, rotationSnapTime);
 
		currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.fixedDeltaTime);
 
		wantedPosition = target.position;
		wantedPosition.y = currentHeight;
 
		usedDistance = Mathf.SmoothDampAngle(usedDistance, distance + (parentRigidbody.velocity.magnitude * distanceMultiplier), ref zVelocity, distanceSnapTime); 
 
		wantedPosition += Quaternion.Euler(0, currentRotationAngle, 0) * new Vector3(0, 0, -usedDistance);
 
		transform.position = wantedPosition;
 
		transform.LookAt(target.position + lookAtVector);
 
	}
 
}