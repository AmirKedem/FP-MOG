using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipInputControllerPublisherFollower : MonoBehaviour {


	private ShipInputController inputController;
	public Transform followedObject;

	// Use this for initialization
	void Start () {
		inputController = GetComponent<ShipInputController>();
	}
	
	// Update is called once per frame
	void Update () {
		var difference = (followedObject.position - this.transform.position).normalized;
		inputController.horizontal = difference.x;
		inputController.vertical = difference.z;
	}
}
