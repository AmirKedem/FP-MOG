using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BallExampleScript : MonoBehaviour
{
    public Vector3 applyForce = new Vector3(0f, 20f, 15f);

    private Scene sceneMain;
    private Scene scenePrediction;
    private PhysicsScene scenePredictionPhysics;
    private PhysicsScene sceneMainPhysics;

    private void Start()
    {
        Physics.autoSimulation = false;
        sceneMain = SceneManager.CreateScene("MainScene");
        sceneMainPhysics = sceneMain.GetPhysicsScene();

        CreateSceneParameters sceneParam = new CreateSceneParameters(LocalPhysicsMode.Physics3D);
        scenePrediction = SceneManager.CreateScene("ScenePredicitonPhysics", sceneParam);
        scenePredictionPhysics = scenePrediction.GetPhysicsScene();
    }

    private void FixedUpdate()
    {
        if (!sceneMainPhysics.IsValid())
            return;

        sceneMainPhysics.Simulate(Time.fixedDeltaTime);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            ShootBall();
    }

    private void ShootBall()
    {
        if (!sceneMainPhysics.IsValid() || !scenePredictionPhysics.IsValid())
            return;

        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        SceneManager.MoveGameObjectToScene(ball, sceneMain);
        ball.AddComponent<Rigidbody>().AddForce(applyForce, ForceMode.Impulse);

        GameObject predictionBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        SceneManager.MoveGameObjectToScene(predictionBall, scenePrediction);
        predictionBall.AddComponent<Rigidbody>().AddForce(applyForce, ForceMode.Impulse);

        Material redMaterial = new Material(Shader.Find("Diffuse"));
        redMaterial.color = Color.red;
        for (int i = 0; i < 500; i++)
        {
            scenePredictionPhysics.Simulate(Time.fixedDeltaTime);

            GameObject pathMarkSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pathMarkSphere.GetComponent<Collider>().isTrigger = true;
            pathMarkSphere.transform.localScale = new Vector3(.2f, .2f, .2f);
            pathMarkSphere.transform.position = predictionBall.transform.position;
            pathMarkSphere.GetComponent<MeshRenderer>().material = redMaterial;
            SceneManager.MoveGameObjectToScene(pathMarkSphere, scenePrediction);
        }

        Destroy(predictionBall);

        Debug.Break();
    }
}

