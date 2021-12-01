namespace SynAudio.Models.Config
{
    public struct RectangleD
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;

        public RectangleD(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public override bool Equals(object obj)
        {
            return obj is RectangleD && this == (RectangleD)obj;
        }
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Width.GetHashCode() ^ Height.GetHashCode();
        }
        public static bool operator ==(RectangleD a, RectangleD b)
        {
            return a.X == b.X && a.Y == b.Y && a.Width == b.Width && a.Height == b.Height;
        }
        public static bool operator !=(RectangleD a, RectangleD b)
        {
            return !(a == b);
        }
    }
}
