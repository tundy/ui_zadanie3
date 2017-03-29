using System;
using System.Linq;

namespace HladaniePokladu
{
    internal class Program
    {
        public const int PocetJedincov = 100;
        public const int MaxGeneracii = 200;
        private static void Swap(ref Jedinec[] param1, ref Jedinec[] param2)
        {
            var temp = param1;
            param1 = param2;
            param2 = temp;
        }

        private static Jedinec[] _aktualnaGeneracia = new Jedinec[PocetJedincov];
        private static Jedinec[] _novaGeneracia = new Jedinec[PocetJedincov];

        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            var plocha = Plocha.CreatePlocha();
            var parts = Console.ReadLine().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var x = int.Parse(parts[0]);
            var y = int.Parse(parts[1]);
            restart:
            for (var i = 0; i < PocetJedincov; ++i)
                _aktualnaGeneracia[i] = new Jedinec(16);
            var generacia = 1;
            while (true)
            {
                if (generacia == MaxGeneracii)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Nenasiel som ciel po {MaxGeneracii} generaciach.");
                    var jedinec = _aktualnaGeneracia[0];
                    var path = jedinec.CountFitness(plocha, x, y);
                    Console.WriteLine($"Poklady: {jedinec.Fitness} | Cesta: {path}");
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape) return;
                    goto restart;
                }
                var rand = new Random();
                var total = 0;
                foreach (var jedinec in _aktualnaGeneracia)
                {
                    var path = jedinec.CountFitness(plocha, x, y);
                    PercentColor(jedinec.Fitness, plocha.PocetPokladov);
                    Console.WriteLine($"{jedinec.Fitness} {path}");
                    Console.ForegroundColor = ConsoleColor.White;
                    total += jedinec.Fitness;
                    if (jedinec.Fitness != plocha.PocetPokladov) continue;
                    Console.WriteLine();
                    Console.WriteLine($"Gen: {generacia} | Cesta: {path}");
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape) return;
                    goto restart;
                }
                var sorted = _aktualnaGeneracia.OrderByDescending(jedinec => jedinec.Fitness).ToArray();

                var index = 0;
                for (;index < (int) (PocetJedincov * 0.1); index++)
                    _novaGeneracia[index] = sorted[index];


                for (; index < PocetJedincov; index++)
                {
                    var a = ZatocRuletou(sorted, rand.Next(total));
                    var b = ZatocRuletou(sorted, rand.Next(total));
                    _novaGeneracia[index++] = a.Mutuj(b);
                    if (index >= PocetJedincov) break;
                    _novaGeneracia[index++] = a.Mutuj();
                    if (index >= PocetJedincov) break;
                    _novaGeneracia[index] = b.Mutuj();
                }
                Swap(ref _aktualnaGeneracia, ref _novaGeneracia);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("######################################################################");
                Console.ForegroundColor = ConsoleColor.White;
                ++generacia;
            }
        }

        private static Jedinec ZatocRuletou(Jedinec[] sorted, int ruleta)
        {
            var last = 0;
            foreach (var jedinec in sorted)
            {
                if (ruleta >= last && ruleta < jedinec.Fitness + last)
                    return jedinec;
                last += jedinec.Fitness;
            }
            return sorted.Last();
        }

        private static void PercentColor(int fitness, int pocetPokladov)
        {
            var percent = fitness / (double) pocetPokladov * 100;
            if (percent < 20)
                Console.ForegroundColor = ConsoleColor.DarkRed;
            else if(percent < 40)
                Console.ForegroundColor = ConsoleColor.Red;
            else if(percent < 60)
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            else if(percent < 80)
                Console.ForegroundColor = ConsoleColor.Yellow;
            else if(percent < 100)
                Console.ForegroundColor = ConsoleColor.DarkGreen;
            else
                Console.ForegroundColor = ConsoleColor.Green;
        }
    }
}