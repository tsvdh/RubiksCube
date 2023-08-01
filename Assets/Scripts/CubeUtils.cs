using UnityEngine;

namespace CubeUtils
{
    public enum Color
    {
        Black,
        Blue,
        Green,
        Orange,
        Red,
        White,
        Yellow
    }

    public enum Direction
    {
        X, Y, Z
    }
    
    public struct Rotation
    {
        public Rotation(Direction direction, int position, float degrees, Vector3Int facingDirection)
        {
            Slice = new CubeSlice(direction, position);
            Degrees = degrees;
            FacingDirection = facingDirection;
        }

        public Rotation(CubeSlice slice, float degrees, Vector3Int facingDirection)
        {
            Slice = slice;
            Degrees = degrees;
            FacingDirection = facingDirection;
        }

        public Rotation(Vector3Int direction, float degrees, Vector3Int facingDirection)
        {
            Slice = new CubeSlice(direction);
            Degrees = degrees;
            FacingDirection = facingDirection;
        }

        public CubeSlice Slice;
        public float Degrees;
        public Vector3Int FacingDirection;
    }

    public static class Utils
    {
        public static Vector3Int GetLookDirection(Vector3Int facingDirection)
        {
            facingDirection.z *= -1;
            return facingDirection;
        }
    }
}
