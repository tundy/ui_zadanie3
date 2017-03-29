using System;

namespace HladaniePokladu
{
    internal class Plocha
    {
        internal readonly int Width;
        internal readonly int Height;
        internal readonly bool[,] Poklad;
        internal readonly int PocetPokladov;

        internal static Plocha CreatePlocha()
        {
            var parts = Console.ReadLine().Split(new[]{' '}, 3, StringSplitOptions.RemoveEmptyEntries);
            return new Plocha(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
        }

        private Plocha(int height, int width, int pocet)
        {
            Width = width;
            Height = height;
            Poklad = new bool[width,height];
            PocetPokladov = pocet;
            for (var i = 0; i < pocet; ++i)
            {
                var parts = Console.ReadLine().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                Poklad[int.Parse(parts[0]), int.Parse(parts[1])] = true;
            }
        }
    }
}