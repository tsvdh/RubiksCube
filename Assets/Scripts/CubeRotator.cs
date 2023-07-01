using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeRotator : MonoBehaviour
{
    // Start is called before the first frame update
    public void Start()
    { 
        
    }

    // Update is called once per frame
    public void FixedUpdate()
    {
        foreach (Transform child in transform)
        {
            if (Math.Round(child.position.x, 0) == -1)
            {
                child.RotateAround(Vector3.zero, Vector3.left, 1);
                
            }
        }
    }
}
