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
        public Rotation(Direction direction, int position, int degrees)
        {
            Slice = new CubeSlice(direction, position);
            Degrees = degrees;
        }

        public Rotation(CubeSlice slice, int degrees)
        {
            Slice = slice;
            Degrees = degrees;
        }

        public CubeSlice Slice;
        public int Degrees;
    }
}
