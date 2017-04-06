using System;

namespace HladaniePokladu
{
    internal partial class Jedinec
    {
        private static readonly Random Rand = new Random();

        internal void Mutuj(Settings settings)
        {
            var random = Rand.Next(settings.PomerMutacie.Total);
            var temp = 0;
            if (random < (temp += settings.PomerMutacie.BezMutacie))
            {
            }
            else if (random < (temp += settings.PomerMutacie.NahodnaBunka))
            {
                var index = Rand.Next(64);
                _bunky[index] = (byte) Rand.Next(256);
            }
            else if (random < (temp += settings.PomerMutacie.XorNahodnyBit))
            {
                var index = Rand.Next(64);
                var bit = 1 << Rand.Next(8);
                _bunky[index] ^= (byte) bit;
            }
            else //if (random < (temp += settings.PomerMutacie.XorNahodnaBunka))
            {
                var index = Rand.Next(64);
                _bunky[index] ^= 0xFF;
            }
        }

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