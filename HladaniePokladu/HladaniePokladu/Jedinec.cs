using System.Diagnostics;

namespace HladaniePokladu
{
    [DebuggerDisplay("{Fitness}")]
    internal partial class Jedinec
    {
        /// <summary>
        ///     Pamatovy priestor
        /// </summary>
        private readonly byte[] _bunky = new byte[64];

        /// <summary>
        ///     Fitness jedinca
        /// </summary>
        internal int Fitness = 0;

        /// <summary>
        ///     Pocet najdneych pokladov
        /// </summary>
        internal int Poklady = 0;

        /// <summary>
        ///     Vytvor prazdneho jedinca
        /// </summary>
        private Jedinec()
        {
        }

        /// <summary>
        ///     Vygeneruj jedinca
        /// </summary>
        /// <param name="index">Po ktory index sa budu generovat bunky</param>
        internal Jedinec(int index)
        {
            for (var i = 0; i < index; i++)
                _bunky[i] = (byte) Rand.Next(256);
        }
    }
}