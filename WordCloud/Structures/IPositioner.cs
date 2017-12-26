namespace WordCloud.Structures
{
    internal interface IPositioner
    {
        double Delta { get; set; }
        double Chord { get; set; }
        double StartX { get; set; }
        double StartY { get; set; }
        bool GetNextPoint(out double x, out double y);
    }
}