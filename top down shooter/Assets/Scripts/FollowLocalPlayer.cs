using UnityEngine;

public class FollowLocalPlayer : MonoBehaviour
{
    public Transform localPlayerTransform;
    public float smoothTime = 0.3F;
    private Vector3 velocity = Vector3.zero;

    void Update()
    {
        // Define a target position above and behind the target transform
        Vector3 targetPosition = localPlayerTransform.TransformPoint(new Vector3(0, 0, -10));

        // Smoothly move the camera towards that target position
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}
