using System.Collections.Generic;
using Color = CubeUtils.Color;
using UnityEngine;

public class CubeBuilder : MonoBehaviour
{
    public GameObject cubePartPrefab;
    public static Transform Root;
    public static List<CubePart> Parts;

    // Start is called before the first frame update
    public void Start()
    {
        Parts = new List<CubePart>();
        Root = transform;
        
        for (int x = -1; x < 2; x++)
        {
            for (int y = -1; y < 2; y++)
            {
                for (int z = -1; z < 2; z++)
                {
                    GameObject cubePart = Instantiate(cubePartPrefab, new Vector3(x, y, z), Quaternion.identity);
                    cubePart.transform.parent = Root;

                    var cubePartComp = cubePart.GetComponent<CubePart>();
                    Parts.Add(cubePartComp);
                    
                    if (x == -1)
                        cubePartComp.ColorSide(Vector3Int.left, Color.Orange);
                    if (x == 1)
                        cubePartComp.ColorSide(Vector3Int.right, Color.Red);
                    if (y == -1)
                        cubePartComp.ColorSide(Vector3Int.down, Color.White);
                    if (y == 1) 
                        cubePartComp.ColorSide(Vector3Int.up, Color.Yellow);
                    if (z == -1) 
                        cubePartComp.ColorSide(Vector3Int.back, Color.Blue);
                    if (z == 1)
                        cubePartComp.ColorSide(Vector3Int.forward, Color.Green);
                }
            }
        }
    }
}
