using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipInputControllerPublisher : MonoBehaviour {

	private ShipInputController inputController;

	// Use this for initialization
	void Start () {
		inputController = GetComponent<ShipInputController>();
	}
	
	// Update is called once per frame
	void Update () {
		inputController.horizontal = Input.GetAxis("Horizontal");
		inputController.vertical = Input.GetAxis("Vertical");
	}

}
