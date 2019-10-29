using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipRigidbodyMovementController : MonoBehaviour {

	public float velocity;
	public float rotationSpeed;
	public ShipMovementType movementType;

	private ShipInputController inputController;
	private new Rigidbody rigidbody;

	// Use this for initialization
	void Start () {
		inputController = GetComponent<ShipInputController>();
		rigidbody = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
		switch(movementType) {
			case ShipMovementType.ManualRotation:
				UpdateMoveManualRotation();
				return;
			case ShipMovementType.AdaptiveRotation:
				UpdateMoveAdaptiveRotation();
				return;
			case ShipMovementType.Slide:
			default:
				UpdateMoveSlide();
				return;
		}
	}

    private void UpdateMoveSlide()
    {
		this.rigidbody.AddForce(inputController.horizontal * Vector3.right * velocity * Time.deltaTime);
		this.rigidbody.AddForce(inputController.vertical * Vector3.forward * velocity * Time.deltaTime);
    }

    private void UpdateMoveManualRotation()
    {
		this.rigidbody.AddForce(inputController.vertical * this.transform.forward * velocity * Time.deltaTime);
		var rotationAmount = rotationSpeed * Time.deltaTime * inputController.horizontal;
		this.rigidbody.AddTorque(0, rotationAmount, 0);
    }

    private void UpdateMoveAdaptiveRotation()
    {
		var inputDirection = new Vector3(inputController.horizontal, 0, inputController.vertical);
		var thrust = Vector3.Dot(inputDirection.normalized, this.transform.forward);
		var rotation = Vector3.Dot(inputDirection.normalized, this.transform.right);
		
		this.rigidbody.AddForce(thrust * inputDirection.magnitude *
				this.transform.forward * velocity * Time.deltaTime);
		var rotationAmount = rotationSpeed * Time.deltaTime * rotation;
		this.rigidbody.AddTorque(0, rotationAmount, 0);
    }
}
