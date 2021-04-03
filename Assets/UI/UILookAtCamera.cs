using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILookAtCamera : MonoBehaviour
{
    private Transform m_MainCameraTransform;

    // Start is called before the first frame update
    void Start()
    {
        m_MainCameraTransform = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(transform.position + m_MainCameraTransform.rotation * -Vector3.forward);
    }

    public void EnableBehaviour()
    {
        enabled = true;
    }
}
