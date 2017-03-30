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
                _bunky[index] = (byte)Rand.Next(256);
            }
        }

        private static readonly Random Rand = new Random();

        internal Jedinec Krizenie(Jedinec other, Settings settings)
        {
            var result = new Jedinec();
            var point = Rand.Next(settings.BodKrizenia.Min, settings.BodKrizenia.Max);
            var i = 0;
            for (; i < point; i++)
                result._bunky[i] = _bunky[i];
            for (; i < 64; i++)
                result._bunky[i] = other._bunky[i];

            return result;
        }
    }
}