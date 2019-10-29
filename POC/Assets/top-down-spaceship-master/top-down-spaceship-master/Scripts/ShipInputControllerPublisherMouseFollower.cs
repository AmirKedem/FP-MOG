using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipInputControllerPublisherMouseFollower : MonoBehaviour {

	private ShipInputController inputController;
	public Camera referenceCamera;

	Vector3 mousePosition = Vector3.zero;

	// Use this for initialization
	void Start () {
		inputController = GetComponent<ShipInputController>();
	}
	
	// Update is called once per frame
	void Update () {
		Vector3 shipLocation = this.transform.position;
		var cameraRay = referenceCamera.ScreenPointToRay(Input.mousePosition);
		var rayIterationCount = referenceCamera.transform.position.y / -cameraRay.direction.y;
		var planeSpaceMouse = new Vector3(cameraRay.origin.x + cameraRay.direction.x * rayIterationCount, 0,
			cameraRay.origin.z + cameraRay.direction.z * rayIterationCount);
		mousePosition = planeSpaceMouse;
		
		var direction = (mousePosition - shipLocation);
		if (direction.magnitude > 1) {
			Debug.Log("normalized: " + direction);
			direction.Normalize();
		}
		inputController.horizontal = direction.x;
		inputController.vertical = direction.z;
	}

	void OnDrawGizmos() {
		Gizmos.color = Color.blue;
		Gizmos.DrawWireSphere(mousePosition, 0.5f);
	}
}
