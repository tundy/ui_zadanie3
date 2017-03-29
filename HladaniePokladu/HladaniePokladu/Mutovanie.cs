using System;

namespace HladaniePokladu
{
    internal partial class Jedinec
    {
        internal Jedinec Mutuj()
        {
            var result = new Jedinec(this);
            var rand = new Random();
            var times = rand.Next(1, 4);
            for (var i = 0; i < times; i++)
            {
                var index = rand.Next(256);
                this[index] = (byte) rand.Next(256);
            }
            return result;
        }

        internal Jedinec Mutuj(Jedinec other)
        {
            var result = new Jedinec();
            var point = new Random().Next(1, 63);
            var i = 0;
            for (; i < point; i++)
                result[i] = Bunky[i];
            for (; i < 64; i++)
                result[i] = other[i];

            return result;
        }
    }
}