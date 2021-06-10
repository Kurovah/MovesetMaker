using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MMCamera : MonoBehaviour
{
    public Transform target, cameraTransform;
    public float verticalOffset = 6, zoom = 10, rotationSpeed = 0;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(!Input.GetKey(KeyCode.Q) && !Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Q) && Input.GetKey(KeyCode.E))
        {
            rotationSpeed = Mathf.Lerp(rotationSpeed, 0, 0.1f);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            rotationSpeed = Mathf.Lerp(rotationSpeed, 5, 0.1f);
        }
        if (Input.GetKey(KeyCode.E))
        {
            rotationSpeed = Mathf.Lerp(rotationSpeed, -5, 0.1f);
        }

        transform.Rotate(0, rotationSpeed, 0);
    }

    private void LateUpdate()
    {
        transform.position = target.transform.position;
        cameraTransform.localPosition = new Vector3(0, verticalOffset, -zoom);
        cameraTransform.LookAt(target);

    }
}
