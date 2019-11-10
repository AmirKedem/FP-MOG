using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSettings : MonoBehaviour
{
    [SerializeField] 
    float ticksPerSecond;
    void Awake()
    {
        Time.fixedDeltaTime = 1.0f / ticksPerSecond;
    }
}
