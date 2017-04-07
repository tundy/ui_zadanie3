namespace HladaniePokladu
{
    internal static partial class Program
    {
        private struct Stat
        {
            public readonly int Max;
            public readonly int Min;
            public readonly double Avg;
            public readonly double Median;

            public Stat(int max, double avg, int min, double median)
            {
                Max = max;
                Min = min;
                Avg = avg;
                Median = median;
            }
        }
    }
}