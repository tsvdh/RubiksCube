using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubePart : MonoBehaviour
{
    private Dictionary<Vector3Int, CubeColor> _sideColors;

    private Material _black;
    private Material _blue;
    private Material _green;
    private Material _orange;
    private Material _red;
    private Material _white;
    private Material _yellow;

    public void Awake()
    {
        _sideColors = new Dictionary<Vector3Int, CubeColor>();
        
        _black = Resources.Load<Material>("Materials/Black");
        _blue = Resources.Load<Material>("Materials/Blue");
        _green = Resources.Load<Material>("Materials/Green");
        _orange = Resources.Load<Material>("Materials/Orange");
        _red = Resources.Load<Material>("Materials/Red");
        _white = Resources.Load<Material>("Materials/White");
        _yellow = Resources.Load<Material>("Materials/Yellow");
        
        foreach (Transform side in transform.Find("Sides"))
        {
            Mesh mesh = side.GetComponent<MeshFilter>().mesh;
            
            Vector3 dir = mesh.normals[0];
            dir = side.localToWorldMatrix.MultiplyVector(dir);
            _sideColors.Add(RoundVector3(dir), CubeColor.Black);
            
            side.GetComponent<MeshRenderer>().material = _black;
        }
    }

    // Start is called before the first frame update
    public void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    private static Vector3Int RoundVector3(Vector3 vector)
    {
        return new Vector3Int
        {
            x = Mathf.RoundToInt(vector.x),
            y = Mathf.RoundToInt(vector.y),
            z = Mathf.RoundToInt(vector.z)
        };
    }
    
    private Material GetMaterial(CubeColor color)
    {
        return color switch
        {
            CubeColor.Black => _black,
            CubeColor.Blue => _blue,
            CubeColor.Green => _green,
            CubeColor.Orange => _orange,
            CubeColor.Red => _red,
            CubeColor.White => _white,
            CubeColor.Yellow => _yellow,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void ColorSide(Vector3Int sideNormal, CubeColor color)
    {
        foreach (Transform side in transform.Find("Sides"))
        {
            Vector3 dir = side.GetComponent<MeshFilter>().mesh.normals[0];
            dir = side.localToWorldMatrix.MultiplyVector(dir);
            
            if (RoundVector3(dir).Equals(sideNormal))
            {
                side.GetComponent<MeshRenderer>().material = GetMaterial(color);
                _sideColors[sideNormal] = color;
                return;
            }                
        }
    }
}
