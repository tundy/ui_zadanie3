using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Serialization;

namespace HladaniePokladu
{
    internal static class Program
    {
        private static Jedinec[] _aktualnaGeneracia;
        private static Jedinec[] _novaGeneracia;
        private static readonly Timer StopTimer = new Timer {AutoReset = false};
        private static bool _work = true;
        private static bool _timer;

        private static void Swap(ref Jedinec[] param1, ref Jedinec[] param2)
        {
            var temp = param1;
            param1 = param2;
            param2 = temp;
        }

        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            LoadSettings(out var settings);
            if (settings == null)
            {
                Console.WriteLine("Error loading settings");
                Console.WriteLine("Press any key");
                Console.ReadKey(true);
                return;
            }

            WriteSettings(settings);

            if (settings.StopAfter.Typ == StopType.Seconds)
            {
                StopTimer.Interval = settings.StopAfter.Hodnota * 1000;
                StopTimer.Elapsed += (sender, args) => { _work = false; };
                settings.StopAfter.Hodnota = int.MaxValue;
                _timer = true;
            }

            _aktualnaGeneracia = new Jedinec[settings.MaxJedincov];
            _novaGeneracia = new Jedinec[settings.MaxJedincov];

            WriteHelp();

            var rand = new Random();
            var plocha = Plocha.CreatePlocha();
            var parts = Console.ReadLine().Split(new[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
            var x = int.Parse(parts[0]);
            var y = int.Parse(parts[1]);
            restart:
            if (_timer)
            {
                StopTimer.Stop();
                _work = true;
                StopTimer.Start();
            }
            for (var i = 0; i < settings.MaxJedincov; ++i)
                _aktualnaGeneracia[i] = new Jedinec(settings.InitRadnom);
            var generacia = 1;
            while (true)
            {
                if (!_work || generacia >= settings.StopAfter.Hodnota)
                {
                    var jedinec = _aktualnaGeneracia[0];
                    var path = jedinec.CountFitness(plocha, x, y);
                    Console.WriteLine();
                    if (_timer) Console.WriteLine("TimedOut");
                    Console.WriteLine($"Nenasiel som ciel po {generacia} generaciach.");
                    PercentColor(jedinec.Fitness, plocha.PocetPokladov);
                    Console.WriteLine(
                        $"Poklady: {jedinec.Fitness} | Kroky: {path.Length - jedinec.Fitness} | Cesta: {path}");
                    Console.ForegroundColor = ConsoleColor.White;
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape) return;
                    goto restart;
                }
                var total = 0;
                var locker = new object();
                var writeLocker = new object();
                var final = "";
                var result = Parallel.ForEach(_aktualnaGeneracia, (jedinec, state) =>
                {
                    var path = jedinec.CountFitness(plocha, x, y);
                    lock (writeLocker)
                    {
                        PercentColor(jedinec.Fitness, plocha.PocetPokladov);
                        Console.WriteLine($"{jedinec.Fitness} {path}");
                        total += jedinec.Fitness;
                    }

                    if (jedinec.Fitness != plocha.PocetPokladov || state.IsStopped) return;
                    state.Stop();
                    lock (locker)
                    {
                        final = $"| Kroky: {path.Length - jedinec.Fitness} | Cesta: {path}";
                    }
                });

                if (!result.IsCompleted)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine();
                    Console.WriteLine("Nasiel som riesenie:");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Gen: {generacia} {final}");
                    Console.ForegroundColor = ConsoleColor.White;
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape) return;
                    goto restart;
                }

                var sorted = _aktualnaGeneracia.OrderByDescending(jedinec => jedinec.Fitness).ToArray();

                var index = 0;
                if (settings.Elitarizmus.HasValue)
                {
                    var end = settings.Elitarizmus.Value.Typ == EliteType.Count
                        ? settings.Elitarizmus.Value.Hodnota
                        : settings.Elitarizmus.Value.Hodnota / settings.MaxJedincov * 100;
                    for (; index < (int) end; index++)
                        _novaGeneracia[index] = sorted[index];
                }

                for (; index < settings.MaxJedincov; index++)
                {
                    var a = ZatocRuletou(sorted, rand.Next(total));
                    var b = ZatocRuletou(sorted, rand.Next(total));
                    var novyJedinec = a.Krizenie(b, settings);
                    if (rand.Next(2) == 0)
                        novyJedinec.Mutuj();
                    _novaGeneracia[index] = novyJedinec;
                }

                Swap(ref _aktualnaGeneracia, ref _novaGeneracia);
                ++generacia;

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("######################################################################");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private static void WriteSettings(Settings settings)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Nacitane nastavenia:");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"Pocet jedincov pre jednu generaciu: {settings.MaxJedincov}");
            Console.WriteLine(settings.StopAfter.Typ == StopType.Generations
                ? $"Maximalny pocet generacii: {settings.StopAfter.Hodnota}"
                : $"Maximalny cas hladania: {settings.StopAfter.Hodnota} sec");
            Console.WriteLine($"Pocet nahodne inicializovanych buniek: {settings.InitRadnom}");
            Console.WriteLine($"Elitarizmus: {settings.Elitarizmus.HasValue}");
            if (settings.Elitarizmus.HasValue)
                Console.WriteLine(settings.Elitarizmus.Value.Typ == EliteType.Percent
                    ? $"Top {settings.Elitarizmus.Value.Hodnota} percent"
                    : $"Top {settings.Elitarizmus.Value.Hodnota} jedincov");
            Console.WriteLine($"Minimalny index pre bod krizenia: {settings.BodKrizenia.Min}");
            Console.WriteLine($"Maximalny index pre bod krizenia: {settings.BodKrizenia.Max}");
            Console.WriteLine();
        }

        private static void WriteHelp()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("<Sirka> <Vyska> <PocetPokladov>");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("<x> <y> // pokladu 1");
            Console.WriteLine("<x> <y> // pokladu 2");
            Console.WriteLine("...");
            Console.WriteLine("<x> <y> // pokladu n");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("<x> <y> // Zaciatocna pozicia");
            Console.WriteLine();
        }

        private static void LoadSettings(out Settings settings)
        {
            var serializer = new XmlSerializer(typeof(Settings));
            if (File.Exists("settings.xml"))
            {
                var stream = File.Open("settings.xml", FileMode.Open);
                settings = serializer.Deserialize(stream) as Settings;
            }
            else
            {
                settings = new Settings
                {
                    Elitarizmus = new Elitarizmus(10, EliteType.Count),
                    BodKrizenia = new MaxMin(4, 60),
                    StopAfter = new StopAfter(200, StopType.Generations),
                    MaxJedincov = 100,
                    InitRadnom = 16
                };
                var stream = File.Open("settings.xml", FileMode.Create);
                serializer.Serialize(stream, settings);
                stream.Close();
            }
        }

        private static Jedinec ZatocRuletou(Jedinec[] sorted, int ruleta)
        {
            var last = 0;
            foreach (var jedinec in sorted)
            {
                if (ruleta < jedinec.Fitness + last)
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
            else if (percent < 40)
                Console.ForegroundColor = ConsoleColor.Red;
            else if (percent < 60)
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            else if (percent < 80)
                Console.ForegroundColor = ConsoleColor.Yellow;
            else if (percent < 100)
                Console.ForegroundColor = ConsoleColor.DarkGreen;
            else
                Console.ForegroundColor = ConsoleColor.Green;
        }
    }
}