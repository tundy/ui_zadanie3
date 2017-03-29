using System;

namespace HladaniePokladu
{
    internal class Program
    {
        private static void Swap(ref object param1, ref object param2)
        {
            var temp = param1;
            param1 = param2;
            param2 = temp;
        }

        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            var test = new Jedinec(16);
        }
    }
}