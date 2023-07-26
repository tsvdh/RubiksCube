using UnityEngine;

public class CameraScript : MonoBehaviour
{
    // Update is called once per frame
    public void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
            transform.RotateAround(Vector3.zero, Vector3.up, 2);
        if (Input.GetKey(KeyCode.RightArrow))
            transform.RotateAround(Vector3.zero, Vector3.up, -2);
        if (Input.GetKey(KeyCode.UpArrow))
            transform.RotateAround(Vector3.zero, transform.right, 2);
        if (Input.GetKey(KeyCode.DownArrow))
            transform.RotateAround(Vector3.zero, transform.right, -2);
    }
}
