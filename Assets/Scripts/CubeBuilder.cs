using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeBuilder : MonoBehaviour
{
    public GameObject cubePartPrefab;
    
    
    

    // Start is called before the first frame update
    public void Start()
    {
        for (int x = -1; x < 2; x++)
        {
            for (int y = -1; y < 2; y++)
            {
                for (int z = -1; z < 2; z++)
                {
                    GameObject cubePart = Instantiate(cubePartPrefab, new Vector3(x, y, z), Quaternion.identity);
                    cubePart.transform.parent = transform;

                    var cubePartComp = cubePart.GetComponent<CubePart>();
                    if (x == -1)
                        cubePartComp.ColorSide(Vector3Int.left, CubeColor.Red);
                    if (x == 1)
                        cubePartComp.ColorSide(Vector3Int.right, CubeColor.Orange);
                    if (y == -1)
                        cubePartComp.ColorSide(Vector3Int.down, CubeColor.Yellow);
                    if (y == 1) 
                        cubePartComp.ColorSide(Vector3Int.up, CubeColor.White);
                    if (z == -1) 
                        cubePartComp.ColorSide(Vector3Int.back, CubeColor.Blue);
                    if (z == 1)
                        cubePartComp.ColorSide(Vector3Int.forward, CubeColor.Green);
                }
            }
        }
    }
}
