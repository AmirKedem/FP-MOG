using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatternMover : MonoBehaviour {

	public float xPeriod;
	public float yPeriod;
	public float xAmplitude;
	public float yAmplitude;
	
	// Update is called once per frame
	void Update () {
		this.transform.position = new Vector3(Mathf.Sin(Time.time / xPeriod) * xAmplitude, 0,
			Mathf.Cos(Time.time / yPeriod) * yAmplitude);
	}
}
