using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Apply script on camera to enable gyro control
public class GyroControl : MonoBehaviour
{
    bool initialized = false;
    
    Quaternion yaw;
    Quaternion pitchRotation = Quaternion.Euler(-90f, 0f, 0f);

    private void Awake()
    {
        Input.gyro.enabled = true;
    }

    void Start()
    {
        StartCoroutine(WaitForGyro());
    }

    IEnumerator WaitForGyro()
    {
        float test = Input.gyro.rotationRate.magnitude;
        yield return new WaitUntil(() => test != Input.gyro.rotationRate.magnitude);
        yaw = transform.rotation;
        initialized = true;
        Debug.Log("Gyro initialized");
    }

    // https://gamedev.stackexchange.com/questions/136174/im-rotating-an-object-on-two-axes-so-why-does-it-keep-twisting-around-the-thir#answer-136175
    // https://answers.unity.com/questions/927515/accelerometer-calibration-2.html

    void Update()
    {
        if (!initialized)
            return;

        yaw *= Quaternion.Euler(0f, -Input.gyro.rotationRateUnbiased.y * Mathf.Rad2Deg * Time.deltaTime, 0f);
        transform.rotation = Quaternion.LookRotation(yaw * (pitchRotation * Input.gyro.gravity));
    }

    private void OnApplicationQuit()
    {
        Input.gyro.enabled = false;
    }
}
