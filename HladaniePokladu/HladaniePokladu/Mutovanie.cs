using System;

namespace HladaniePokladu
{
    internal partial class Jedinec
    {
        internal void Mutuj()
        {
            var times = Rand.Next(1, 4);
            for (var i = 0; i < times; i++)
            {
                var index = Rand.Next(64);
                Bunky[index] = (byte)Rand.Next(256);
            }
        }

        private static readonly Random Rand = new Random();

        internal Jedinec Mutuj(Jedinec other)
        {
            var result = new Jedinec();
            var point = Rand.Next(4, 60);
            var i = 0;
            for (; i < point; i++)
                result[i] = Bunky[i];
            for (; i < 64; i++)
                result[i] = other[i];

            return result;
        }
    }
}