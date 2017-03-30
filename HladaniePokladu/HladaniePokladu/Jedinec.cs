namespace HladaniePokladu
{
    internal partial class Jedinec
    {
        private readonly byte[] _bunky = new byte[64];
        internal int Fitness = 0;

        private Jedinec()
        {
        }

        internal Jedinec(int index)
        {
            for (var i = 0; i < index; i++)
                _bunky[i] = (byte)Rand.Next(256);
        }

        internal Jedinec(Jedinec old)
        {
            for (var i = 0; i < 64; i++)
                _bunky[i] = old._bunky[i];
        }
    }
}
