using System.Diagnostics;

namespace HladaniePokladu
{
    [DebuggerDisplay("{Fitness}")]
    internal partial class Jedinec
    {
        private readonly byte[] _bunky = new byte[64];
        internal int Fitness = 0;
        internal int Poklady = 0;

        private Jedinec()
        {
        }

        internal Jedinec(int index)
        {
            for (var i = 0; i < index; i++)
                _bunky[i] = (byte) Rand.Next(256);
            ++BezMutacie;
        }
    }
}