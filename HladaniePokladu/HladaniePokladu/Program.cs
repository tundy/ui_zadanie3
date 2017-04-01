using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Serialization;

namespace HladaniePokladu
{
    internal static class Program
    {
        /// <summary>
        ///     Aktualne spracovavana generacia
        /// </summary>
        private static Jedinec[] _aktualnaGeneracia;

        /// <summary>
        ///     Pole kam sa premiestnuju novi jedinci
        /// </summary>
        private static Jedinec[] _novaGeneracia;

        /// <summary>
        ///     Casovec pre ukoncenie hladania
        /// </summary>
        private static readonly Timer StopTimer = new Timer {AutoReset = false};

        /// <summary>
        ///     Priznak pre hladanie
        /// </summary>
        private static bool _work = true;

        /// <summary>
        ///     Priznak ci sa pouziva casovac na ukoncenie hladania
        /// </summary>
        private static bool _timer;

        /// <summary>
        ///     Randomiser
        /// </summary>
        private static readonly Random Rand = new Random();

        private static readonly List<Stat> Stats = new List<Stat>();

        /// <summary>
        ///     Pomocna metoda pre vymanu novej generacie za aktualnu
        /// </summary>
        /// <param name="param1">Stara generacia</param>
        /// <param name="param2">Nova generacia</param>
        private static void Swap(ref Jedinec[] param1, ref Jedinec[] param2)
        {
            var temp = param1;
            param1 = param2;
            param2 = temp;
        }

        // ReSharper disable once UnusedMember.Local
        private static void Main()
        {
            if (!Init(out var plocha, out var x, out var y, out var settings)) return;

            restart:
            InitLoop(settings);
            for (var generacia = 1;;)
            {
                ++generacia;

                if (!_work || generacia >= settings.StopAfter.Hodnota)
                {
                    PrintStopped(plocha, settings, x, y, generacia);

                    if (Console.ReadKey(true).Key == ConsoleKey.Escape) return;
                    goto restart;
                }

                var result = CalculateFitness(plocha, settings, x, y, out var total, out var min, out var final);

                if (!result.IsCompleted)
                {
                    if (final == null || final.Item1.Poklady != plocha.PocetPokladov)
                        continue;

                    PrintResult(generacia, final);

                    if (Console.ReadKey(true).Key == ConsoleKey.Escape) return;
                    goto restart;
                }

                var sorted = ZoradJedincov(min, ref total);
                VytvorNovuGeneraciu(settings, sorted, total);
                Swap(ref _aktualnaGeneracia, ref _novaGeneracia);

                NewGenerationSeparator(generacia);
            }
        }

        /// <summary>
        ///     Informuj ze zacala nova generacia
        /// </summary>
        private static void NewGenerationSeparator(int generacia)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"################################### {generacia:000} ###################################");
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        ///     Zarovna fitness a zoradi jedincov
        /// </summary>
        /// <param name="min">Najmensia fitness hodnota</param>
        /// <param name="total">Suma vsetkych fitness</param>
        /// <returns>Vrati zoradeny zoznam jedincov</returns>
        private static Jedinec[] ZoradJedincov(int min, ref int total)
        {
            var sorted = _aktualnaGeneracia.OrderByDescending(jedinec => jedinec.Fitness).ToArray();
            var temp = Features.Quartiles(sorted);
            var stat = new Stat(sorted[0].Fitness, (double) total / sorted.Length, sorted.Last().Fitness,
                temp.Item1, temp.Item2, temp.Item3, Jedinec.BezMutacie, Jedinec.NahodnaBunka, Jedinec.XorBit);
            Stats.Add(stat);
            Jedinec.ClearCounters();

            --min;
            foreach (var jedinec in sorted)
                jedinec.Fitness -= min;
            total -= min * sorted.Length;
            return sorted;
        }

        /// <summary>
        ///     Vytvori novu generaciu jedincov
        /// </summary>
        /// <param name="settings">Nastavenia algoritmu</param>
        /// <param name="sorted">Zoradeny zoznam jedincov</param>
        /// <param name="total">Fitness suma</param>
        private static void VytvorNovuGeneraciu(Settings settings, Jedinec[] sorted, int total)
        {
            var index = VyberElitu(settings, sorted);
            for (; index < settings.MaxJedincov; index++)
            {
                var a = NajdiJedinca(sorted, Rand.Next(total));
                var b = NajdiJedinca(sorted, Rand.Next(total));
                var novyJedinec = a.Krizenie(b, settings);
                novyJedinec.Mutuj(settings);
                _novaGeneracia[index] = novyJedinec;
            }
        }

        /// <summary>
        ///     Na zaklade nastaveni prida Elitu do novej generacie
        /// </summary>
        /// <param name="settings">Nastavenia algoritmu</param>
        /// <param name="sorted">Zoradeny zoznam jedincov</param>
        /// <returns></returns>
        private static int VyberElitu(Settings settings, IReadOnlyList<Jedinec> sorted)
        {
            if (!settings.Elitarizmus.HasValue) return 0;

            var index = 0;
            var end = settings.Elitarizmus.Value.Typ == EliteType.Count
                ? settings.Elitarizmus.Value.Hodnota
                : settings.Elitarizmus.Value.Hodnota / 100 * settings.MaxJedincov;
            for (; index < (int) end; index++)
                _novaGeneracia[index] = sorted[index];
            return index;
        }

        /// <summary>
        ///     Neuspesne najdenie cesty
        /// </summary>
        /// <param name="plocha">Plocha, na kt. sa hladal poklad</param>
        /// <param name="settings"></param>
        /// <param name="x">X-ova suradnica zaciatku</param>
        /// <param name="y">Y-ova suradnica zaciatku</param>
        /// <param name="generacia">Cislo generacie, v kt. sa to zastavilo</param>
        private static void PrintStopped(Plocha plocha, Settings settings, int x, int y, int generacia)
        {
            var sorted = _aktualnaGeneracia.OrderByDescending(j => j.Fitness).ToArray();
            var jedinec = sorted[0];
            var path = jedinec.CountFitness(plocha, settings, x, y);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            if (_timer) Console.WriteLine("TimedOut");
            Console.WriteLine($"Nenasiel som ciel po {generacia} generaciach.");
            PercentColor(jedinec.Poklady, plocha.PocetPokladov);
            Console.WriteLine($"Poklady: {jedinec.Poklady} | Kroky: {path.Length - jedinec.Poklady} | Cesta: {path}");
            Console.ForegroundColor = ConsoleColor.White;

            SaveStats();
        }

        /// <summary>
        ///     Uspesne najdenie cesty
        /// </summary>
        /// <param name="generacia">Cisl generacie, v kt. sa nasla cesta</param>
        /// <param name="final">Finalny jedinec a cesta</param>
        private static void PrintResult(int generacia, Tuple<Jedinec, string> final)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{Environment.NewLine}Nasiel som riesenie:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(
                $"Gen: {generacia} | Kroky: {final.Item2.Length - final.Item1.Poklady} | Cesta: {final.Item2}");
            Console.ForegroundColor = ConsoleColor.White;

            SaveStats();
        }

        /// <summary>
        ///     Ulozi statistiku do subora
        /// </summary>
        private static void SaveStats()
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                "Maximum\tPriemer\tMinimum\tHorny Kvartil\tMedian\tDolny Kvartil\tBez Mutacie\tNahodna Bunka\tXor Bit");
            foreach (var stat in Stats)
                sb.AppendLine(
                    $"{stat.Max}\t{stat.Avg}\t{stat.Min}\t{stat.Uq}\t{stat.Median}\t{stat.Lq}\t{stat.BezMutacie}\t{stat.NahodnaBunka}\t{stat.XorBit}");
            File.WriteAllText("stats.txt", sb.ToString());
            Jedinec.ClearCounters();
        }

        /// <summary>
        ///     Ukony potrebne pred prvym spustenim cyklu
        /// </summary>
        /// <param name="settings"></param>
        private static void InitLoop(Settings settings)
        {
            CreateFirstGeneration(settings);
            ResetTimer();
            Stats.Clear();
        }

        /// <summary>
        ///     Ak bolo nastavene ukoncenie po X sekundach spusti casovac
        /// </summary>
        private static void ResetTimer()
        {
            if (!_timer) return;
            StopTimer.Stop();
            _work = true;
            StopTimer.Start();
        }

        /// <summary>
        ///     Vytvor prvu generaciu jedincov
        /// </summary>
        /// <param name="settings"></param>
        private static void CreateFirstGeneration(Settings settings)
        {
            for (var i = 0; i < settings.MaxJedincov; ++i)
                _aktualnaGeneracia[i] = new Jedinec(settings.InitRadnom);
        }

        /// <summary>
        ///     Nacitaj nastavenia a plochu
        /// </summary>
        /// <param name="plocha">Plocha, na kt. sa hladaju poklady</param>
        /// <param name="x">X-ova zaciatocna poizica</param>
        /// <param name="y">Y-ova zaciatocna pozicia</param>
        /// <param name="settings">Nacitane nastavenia</param>
        /// <returns>Ci sa podarilo uspesne nacitat nastavenia</returns>
        private static bool Init(out Plocha plocha, out int x, out int y, out Settings settings)
        {
            LoadSettings(out settings);

            if (settings == null)
            {
                Console.WriteLine("Error loading settings");
                Console.WriteLine("Press any key");
                Console.ReadKey(true);
                x = -1;
                y = -1;
                plocha = null;
                return false;
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

            plocha = Plocha.CreatePlocha();
            // ReSharper disable once PossibleNullReferenceException
            var parts = Console.ReadLine().Split(new[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
            x = int.Parse(parts[0]);
            y = int.Parse(parts[1]);
            return true;
        }

        /// <summary>
        ///     Spussti parallerny vypocet fitness
        /// </summary>
        /// <param name="plocha">Plocha, na kt. sa hladaju poklady</param>
        /// <param name="settings"></param>
        /// <param name="x">X-ova zaciatocna suradnica</param>
        /// <param name="y">Y-ova zaciatocna suradnica</param>
        /// <param name="total">Suma fitness vsetkych jedincov</param>
        /// <param name="min">Najmensi fitness</param>
        /// <param name="final">Jedinec, kt. sa podarilo najst cestu + cesta</param>
        /// <returns>Vysledok Parallel loop-u</returns>
        private static ParallelLoopResult CalculateFitness(Plocha plocha, Settings settings, int x, int y, out int total,
            out int min, out Tuple<Jedinec, string> final)
        {
            var tempTotal = 0;
            var tempMin = int.MaxValue;
            Tuple<Jedinec, string> tempFinal = null;

            var writeLocker = new object();
            var locker = new object();

            var result = Parallel.ForEach(_aktualnaGeneracia, (jedinec, state) =>
            {
                if (!_work) state.Stop();
                var path = jedinec.CountFitness(plocha, settings, x, y);
                lock (writeLocker)
                {
                    PercentColor(jedinec.Poklady, plocha.PocetPokladov);
                    Console.WriteLine($"{jedinec.Fitness: 000;-000} {path}");
                    // ReSharper disable AccessToModifiedClosure
                    tempTotal += jedinec.Fitness;
                    tempMin = Math.Min(jedinec.Fitness, tempMin);
                    // ReSharper restore AccessToModifiedClosure
                }

                if (jedinec.Poklady != plocha.PocetPokladov || state.IsStopped) return;
                state.Stop();
                lock (locker)
                {
                    tempFinal = new Tuple<Jedinec, string>(jedinec, path);
                }
            });
            total = tempTotal;
            min = tempMin;
            final = tempFinal;
            return result;
        }

        /// <summary>
        ///     Vypis aktualne nastavenia algoritmu
        /// </summary>
        /// <param name="settings"></param>
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
            Console.WriteLine($"Elitarizmus: {(settings.Elitarizmus.HasValue ? "povoleny" : "zakazany")}");
            if (settings.Elitarizmus.HasValue)
                Console.WriteLine(settings.Elitarizmus.Value.Typ == EliteType.Percent
                    ? $"Top {settings.Elitarizmus.Value.Hodnota} percent"
                    : $"Top {settings.Elitarizmus.Value.Hodnota} jedincov");
            Console.WriteLine($"Minimalny index pre bod krizenia: {settings.BodKrizenia.Min}");
            Console.WriteLine($"Maximalny index pre bod krizenia: {settings.BodKrizenia.Max}");
            Console.WriteLine(
                $"Pomer mutacii: {settings.PomerMutacie.BezMutacie}:{settings.PomerMutacie.NahodnaBunka}:{settings.PomerMutacie.XorNahodnyBit}");
            Console.WriteLine(
                $"FITNESS | Poklad: +{settings.Fitness.Poklad} | Krok: -{settings.Fitness.Krok} | Vyjdenie mimo mriezky: -{settings.Fitness.VyjdenieMimoMriezky}");
            Console.WriteLine();
        }

        /// <summary>
        ///     Vypis sposob nacitania Plochy
        /// </summary>
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

        /// <summary>
        ///     Nacitaj nastavenia zo suboru
        /// </summary>
        /// <param name="settings">Vysledne nastavenia</param>
        private static void LoadSettings(out Settings settings)
        {
            var serializer = new XmlSerializer(typeof(Settings));
            if (File.Exists("settings.xml"))
            {
                var stream = File.Open("settings.xml", FileMode.Open);
                try
                {
                    settings = serializer.Deserialize(stream) as Settings;
                }
                catch
                {
                    settings = null;
                }
                stream.Close();
            }
            else
            {
                settings = new Settings
                {
                    Elitarizmus = new Elitarizmus(10, EliteType.Percent),
                    BodKrizenia = new MaxMin(14, 50),
                    StopAfter = new StopAfter(20, StopType.Seconds),
                    Fitness = new Fitness(100, 1, 5),
                    MaxJedincov = 250,
                    InitRadnom = 16,
                    PomerMutacie = new Mutation(1, 2, 3)
                };
                var stream = File.Open("settings.xml", FileMode.Create);
                serializer.Serialize(stream, settings);
                stream.Close();
            }
        }

        /// <summary>
        ///     Hlada jedinca zo zoradene pola na zaklade vstupnej hodnoty
        /// </summary>
        /// <param name="sorted">Zoradene pole jedincov</param>
        /// <param name="ruleta">nahodne vygenerovane cislo</param>
        /// <returns>Vrati jedinca na zaklade hodnoty rulety</returns>
        private static Jedinec NajdiJedinca(Jedinec[] sorted, int ruleta)
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

        /// <summary>
        ///     Zafarbi vystup na zaklade poctu najdenych pokladov
        /// </summary>
        /// <param name="najdenePoklady">Pocet najdenych pokladov</param>
        /// <param name="pocetPokladov">Celkovy pocet pokladov</param>
        private static void PercentColor(int najdenePoklady, int pocetPokladov)
        {
            var percent = najdenePoklady / (double) pocetPokladov * 100;
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

        private struct Stat
        {
            public readonly int Max;
            public readonly int Min;
            public readonly double Avg;
            public readonly double Median;
            public readonly double Uq;
            public readonly double Lq;
            public readonly int BezMutacie;
            public readonly int NahodnaBunka;
            public readonly int XorBit;

            public Stat(int max, double avg, int min, double uq, double median, double lq, int bezMutacie,
                int nahodnaBunka, int xorBit)
            {
                Max = max;
                Min = min;
                Avg = avg;
                Median = median;
                Uq = uq;
                Lq = lq;
                BezMutacie = bezMutacie;
                NahodnaBunka = nahodnaBunka;
                XorBit = xorBit;
            }
        }
    }
}