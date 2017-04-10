using System;
using System.IO;

namespace HladaniePokladu
{
    internal class Plocha
    {
        internal readonly int Height;
        internal readonly int PocetPokladov;
        internal readonly bool[,] Poklad;
        internal readonly int Width;

        private Plocha(TextReader stream, int width, int height, int pocet)
        {
            Width = width;
            Height = height;
            Poklad = new bool[width, height];
            PocetPokladov = pocet;
            for (var i = 0; i < pocet; ++i)
            {
                var parts = stream.ReadLine().Split(new[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
                Poklad[int.Parse(parts[0]), int.Parse(parts[1])] = true;
            }
        }

        internal static Plocha CreatePlocha(TextReader stream)
        {
            try
            {
                var parts = stream.ReadLine().Split(new[] {' '}, 3, StringSplitOptions.RemoveEmptyEntries);
                return new Plocha(stream, int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
            }
            catch
            {
                return null;
            }
        }
    }
}