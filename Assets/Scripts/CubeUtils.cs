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

        public Rotation(Direction direction, int position, int degrees, Vector3Int facingDirection)
        {
            Slice = new CubeSlice(direction, position);
            Degrees = degrees;
            FacingDirection = facingDirection;
        }

        public Rotation(CubeSlice slice, int degrees, Vector3Int facingDirection)
        {
            Slice = slice;
            Degrees = degrees;
            FacingDirection = facingDirection;
        }

        public Rotation(Vector3Int direction, int degrees, Vector3Int facingDirection)
        {
            Slice = new CubeSlice(direction);
            Degrees = degrees;
            FacingDirection = facingDirection;
        }

        public CubeSlice Slice;
        public int Degrees;
        public Vector3Int FacingDirection;
    }
}
