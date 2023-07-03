using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Color = CubeUtils.Color;

public class CubePart : MonoBehaviour
{
    private Dictionary<Vector3Int, Color> _sideColors;

    private Material _black;
    private Material _blue;
    private Material _green;
    private Material _orange;
    private Material _red;
    private Material _white;
    private Material _yellow;

    public void Awake()
    {
        _sideColors = new Dictionary<Vector3Int, Color>();
        
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
            _sideColors.Add(RoundVector3(dir), Color.Black);
            
            side.GetComponent<MeshRenderer>().material = _black;
        }
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
    
    private Material GetMaterial(Color color)
    {
        return color switch
        {
            Color.Black => _black,
            Color.Blue => _blue,
            Color.Green => _green,
            Color.Orange => _orange,
            Color.Red => _red,
            Color.White => _white,
            Color.Yellow => _yellow,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void ColorSide(Vector3Int sideNormal, Color color)
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

    public Dictionary<Vector3Int, Color> GetSideColors()
    {
        var nonBlackColors = new Dictionary<Vector3Int, Color>();
        foreach (KeyValuePair<Vector3Int, Color> entry in _sideColors)
        {
            if (entry.Value != Color.Black)
            {
                Vector3 transformedDir = transform.localToWorldMatrix.MultiplyVector(entry.Key);
                nonBlackColors.Add(RoundVector3(transformedDir), entry.Value);
            }
        }
        return nonBlackColors;
    }

    public Vector3Int GetPosition()
    {
        return RoundVector3(transform.position);
    }
}
