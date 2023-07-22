using System;
using System.Collections;
using System.Collections.Generic;
using CubeUtils;
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
            _sideColors.Add(Vector3Int.RoundToInt(dir), Color.Black);
            
            side.GetComponent<MeshRenderer>().material = _black;
        }
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
            
            if (Vector3Int.RoundToInt(dir).Equals(sideNormal))
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
                nonBlackColors.Add(Vector3Int.RoundToInt(transformedDir), entry.Value);
            }
        }
        return nonBlackColors;
    }

    public Vector3Int GetPosition()
    {
        return Vector3Int.RoundToInt(transform.position);
    }

    private int GetDirsNotZero()
    {
        Vector3Int pos = GetPosition();
        
        var total = 0;
        total += Math.Abs(pos.x);
        total += Math.Abs(pos.y);
        total += Math.Abs(pos.z);

        return total;
    }

    public bool IsCenter()
    {
        return GetDirsNotZero() == 1;
    }

    public bool IsEdge()
    {
        return GetDirsNotZero() == 2;
    }

    public bool IsCorner()
    {
        return GetDirsNotZero() == 3;
    }
    
    public List<CubeSlice> GetSlices()
    {
        Vector3Int partPos = GetPosition();
        return new List<CubeSlice>
        {
            new(Direction.X, partPos.x),
            new(Direction.Y, partPos.y),
            new(Direction.Z, partPos.z)
        };
    }
}
