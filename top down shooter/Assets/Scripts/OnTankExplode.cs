using UnityEngine;

public class OnTankExplode : MonoBehaviour
{
    [SerializeField] GameObject explosionPrefab;
    private void OnDestroy()
    {
        // Particle effect
        var effectPosition = transform.position + new Vector3(0, 0, 1);
        var effectRotation = Quaternion.Euler(0, 0, Random.Range(0.0f, 360.0f));
        var effect = GameObject.Instantiate(explosionPrefab, effectPosition, effectRotation);
        GameObject.Destroy(effect, 3);
    }
}
