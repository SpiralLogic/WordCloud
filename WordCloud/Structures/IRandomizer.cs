namespace WordCloud.Structures
{
    public interface IRandomizer
    {
        int RandomInt(int max);
        int RandomInt(int min, int max);
    }
}