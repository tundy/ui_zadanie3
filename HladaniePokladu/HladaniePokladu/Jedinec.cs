using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace HladaniePokladu
{
    internal partial class Jedinec
    {
        internal byte[] Bunky = new byte[64];
        internal int Fitness = 0;

        internal Jedinec()
        {
        }

        internal Jedinec(int index)
        {
            var rand = new Random();
            for (var i = 0; i < index; i++)
            {
                Bunky[i] = (byte)rand.Next(256);
            }
        }

        internal Jedinec(Jedinec old)
        {
            for (var i = 0; i < 64; i++)
                Bunky[i] = old[i];
        }

        public byte this[int index]
        {
            get { return Bunky[index]; }
            set { Bunky[index] = value; }
        }
    }
}
