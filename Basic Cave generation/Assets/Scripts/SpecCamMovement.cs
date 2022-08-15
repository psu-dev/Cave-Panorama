using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecCamMovement : MonoBehaviour
{
    private bool mouse = false;

    public int arrowSpeed = 50;
    public int mouseSpeed = 2;

    public enum RotationAxes { MouseXandY = 0, MouseX = 1, MouseY = 2};
    public RotationAxes axes = RotationAxes.MouseXandY;

    public float minimumX = -360.0f;
    public float maximumX = 360.0f;
    public float minimumY = -60.0f;
    public float maximumY = 60.0f;
    float rotationY = 0.0f;

    void Update()
    {
        ArrowInputs();
        Cursor.visible = mouse;

        if(mouse == false)
        {
            MouseInputs();
        }
    }

    private void ArrowInputs()
    {
        // Arrow key inputs
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) { arrowSpeed = 100; }
        else { arrowSpeed = 50; }

        if (Input.GetKey(KeyCode.W)) { transform.position += transform.forward * arrowSpeed * Time.deltaTime; }
        if (Input.GetKey(KeyCode.A)) { transform.position += transform.right * (-arrowSpeed) * Time.deltaTime; }
        if (Input.GetKey(KeyCode.S)) { transform.position += transform.forward * (-arrowSpeed) * Time.deltaTime; }
        if (Input.GetKey(KeyCode.D)) { transform.position += transform.right * arrowSpeed * Time.deltaTime; }
        if (Input.GetKey(KeyCode.Space)) { transform.position += transform.up * arrowSpeed * Time.deltaTime; }
        if (Input.GetKey(KeyCode.LeftControl)) { transform.position += transform.up * (-arrowSpeed) * Time.deltaTime; }
        if (Input.GetKey(KeyCode.Escape)) 
        { 
            mouse = !mouse;
        }
    }

    private void MouseInputs()
    {
        // Mouse inputs
        if (axes == RotationAxes.MouseXandY)
        {
            float rotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * mouseSpeed;

            rotationY += Input.GetAxis("Mouse Y") * mouseSpeed;
            rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

            transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
        }

        else if (axes == RotationAxes.MouseX)
        {
            transform.Rotate(0, Input.GetAxis("Mouse X") * mouseSpeed, 0);
        }

        else
        {
            rotationY += Input.GetAxis("Mouse Y") * mouseSpeed;
            rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

            transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
        }
    }
}
