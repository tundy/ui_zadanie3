using System;

namespace HladaniePokladu
{
    internal partial class Jedinec
    {
        private static readonly Random Rand = new Random();
        public static int BezMutacie;
        public static int NahodnaBunka;
        public static int XorBit;

        public static void ClearCounters()
        {
            BezMutacie = 0;
            NahodnaBunka = 0;
            XorBit = 0;
        }

        internal void Mutuj(Settings settings)
        {
            var random = Rand.Next(settings.PomerMutacie.Total);
            var temp = 0;
            if (random < (temp += settings.PomerMutacie.BezMutacie))
            {
                ++BezMutacie;
            }
            else if (random < (temp += settings.PomerMutacie.NahodnaBunka))
            {
                ++NahodnaBunka;
                var index = Rand.Next(64);
                _bunky[index] = (byte) Rand.Next(256);
            }
            else //if (random < (temp += settings.PomerMutacie.XorNahodnyBit))
            {
                ++XorBit;
                var index = Rand.Next(64);
                var bit = 1 << Rand.Next(8);
                _bunky[index] ^= (byte) bit;
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