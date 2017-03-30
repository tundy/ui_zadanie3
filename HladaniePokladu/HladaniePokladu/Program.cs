using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace HladaniePokladu
{
    internal static class Program
    {
        private static void Swap(ref Jedinec[] param1, ref Jedinec[] param2)
        {
            var temp = param1;
            param1 = param2;
            param2 = temp;
        }

        private static Jedinec[] _aktualnaGeneracia;
        private static Jedinec[] _novaGeneracia;

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

            _aktualnaGeneracia = new Jedinec[settings.MaxJedincov];
            _novaGeneracia = new Jedinec[settings.MaxJedincov];

            WriteSettings(settings);
            WriteHelp();

            var rand = new Random();
            var plocha = Plocha.CreatePlocha();
            var parts = Console.ReadLine().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var x = int.Parse(parts[0]);
            var y = int.Parse(parts[1]);
            restart:
            for (var i = 0; i < settings.MaxJedincov; ++i)
                _aktualnaGeneracia[i] = new Jedinec(settings.InitRadnom);
            var generacia = 1;
            while (true)
            {
                if (generacia == settings.MaxGeneracii)
                {
                    Console.WriteLine();
                    Console.WriteLine($"Nenasiel som ciel po {settings.MaxGeneracii} generaciach.");
                    var jedinec = _aktualnaGeneracia[0];
                    var path = jedinec.CountFitness(plocha, x, y);
                    Console.WriteLine($"Poklady: {jedinec.Fitness} | Kroky: {path.Length - jedinec.Fitness} | Cesta: {path}");
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape) return;
                    goto restart;
                }
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
                    Console.WriteLine($"Gen: {generacia} | Kroky: {path.Length - jedinec.Fitness} | Cesta: {path}");
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape) return;
                    goto restart;
                }

                var sorted = _aktualnaGeneracia.OrderByDescending(jedinec => jedinec.Fitness).ToArray();

                var index = 0;
                if (settings.Elitarizmus.HasValue)
                {
                    var end = settings.Elitarizmus.Value.Typ == Type.Count
                        ? settings.Elitarizmus.Value.Hodnota
                        : settings.Elitarizmus.Value.Hodnota / settings.MaxJedincov * 100;
                    for (; index < (int)end; index++)
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
            Console.WriteLine($"Maximalny pocet generacii: {settings.MaxGeneracii}");
            Console.WriteLine($"Pocet nahodne inicializovanych buniek: {settings.InitRadnom}");
            Console.WriteLine($"Elitarizmus: {settings.Elitarizmus.HasValue}");
            if (settings.Elitarizmus.HasValue)
                Console.WriteLine(settings.Elitarizmus.Value.Typ == Type.Percent
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
                    Elitarizmus = new Elitarizmus(10, Type.Count),
                    BodKrizenia = new MaxMin(4, 60),
                    MaxGeneracii = 200,
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