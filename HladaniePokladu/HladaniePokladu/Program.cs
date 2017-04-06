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

                    if (SpracujVstup(ref settings, ref x, ref y, ref plocha)) return;
                    goto restart;
                }

                var result = CalculateFitness(plocha, settings, x, y, out var total, out var min, out var final);

                if (!result.IsCompleted)
                {
                    if (final == null || final.Item1.Poklady != plocha.PocetPokladov)
                        continue;

                    PrintResult(settings, generacia, final);

                    if (SpracujVstup(ref settings, ref x, ref y, ref plocha)) return;
                    goto restart;
                }

                var sorted = ZoradJedincov(min, ref total);

                if (settings.Output == OutputType.Top)
                {
                    PercentColor(sorted[0].Poklady, plocha.PocetPokladov);
                    Console.WriteLine($"{sorted[0].Fitness: 000;-000} {sorted[0].CountFitness(plocha, settings, x, y)}");
                }

                //Extra(sorted, ref total);
                VytvorNovuGeneraciu(settings, sorted, total);
                Swap(ref _aktualnaGeneracia, ref _novaGeneracia);

                if (settings.Output != OutputType.Result)
                {
                    NewGenerationSeparator(generacia);
                }
                else
                {
                    Console.CursorLeft = 0;
                    Console.Write($"gen: {generacia}");
                }
            }
        }

        /// <summary>
        ///     Spracuj vstup z klavesnice
        /// </summary>
        /// <param name="settings">Nastavenia, kt. sa mozu aktualizovat</param>
        /// <param name="x">X-ova zaciatocna poizica</param>
        /// <param name="y">Y-ova zaciatocna pozicia</param>
        /// <param name="plocha">Plocha, na kt. sa hladaju poklady</param>
        /// <returns>Ci sa stlacil ESC</returns>
        private static bool SpracujVstup(ref Settings settings, ref int x, ref int y, ref Plocha plocha)
        {
            for (;;)
            {
                Console.WriteLine();
                Console.WriteLine("ESC - pre ukoncenie programu");
                Console.WriteLine("S - pre znovu nacitanie nastaveni");
                Console.WriteLine("P - pre znovu nacitanie pokladov");
                Console.WriteLine("Hocico ine pre spustenie noveho hladania");
                Console.WriteLine();

                try
                {
                    var key = Console.ReadKey(true);
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (key.Key)
                    {
                        case ConsoleKey.Escape:
                            return true;
                        case ConsoleKey.S:
                            if (!Setup(out settings))
                                return true;
                            continue;
                        case ConsoleKey.P:
                            plocha = LoadPlocha(out x, out y);
                            continue;
                        default:
                            return false;
                    }
                }
                catch
                {
                    return true;
                }
            }
        }

/*
        private static void Extra(Jedinec[] sorted, ref int total)
        {
            var f = sorted.Length;
            foreach (var jedinec in sorted)
                jedinec.Fitness = f--;
            total = (1 + sorted.Length) * sorted.Length / 2;
        }
*/

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

            var count = sorted.Length;
            double median;
            if (count == 0)
                median = 0;
            else if (count % 2 == 0)
                median = (sorted[count / 2 - 1].Fitness + sorted[count / 2].Fitness) / 2d;
            else
                median = sorted[count / 2].Fitness;

            var stat = new Stat(sorted[0].Fitness, (double) total / sorted.Length, sorted.Last().Fitness,
                median, Jedinec.BezMutacie, Jedinec.NahodnaBunka, Jedinec.XorBit, Jedinec.XorBunka);
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
        /// <param name="settings">Nastavenia algoritmu</param>
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

            SaveStats(settings);
        }

        /// <summary>
        ///     Uspesne najdenie cesty
        /// </summary>
        /// <param name="settings">Nastavenia algoritmu</param>
        /// <param name="generacia">Cisl generacie, v kt. sa nasla cesta</param>
        /// <param name="final">Finalny jedinec a cesta</param>
        private static void PrintResult(Settings settings, int generacia, Tuple<Jedinec, string> final)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{Environment.NewLine}Nasiel som riesenie:");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(
                $"Gen: {generacia} | Kroky: {final.Item2.Length - final.Item1.Poklady} | Cesta: {final.Item2}");
            Console.ForegroundColor = ConsoleColor.White;

            SaveStats(settings);
        }

        /// <summary>
        ///     Ulozi statistiku do subora
        /// </summary>
        private static void SaveStats(Settings settings)
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                "Maximum\tPriemer\tMinimum\tMedian\tBez Mutacie\tNahodna Bunka\tXor Bunka\tXor Bit");
            foreach (var stat in Stats)
                sb.AppendLine(
                    $"{stat.Max}\t{stat.Avg}\t{stat.Min}\t{stat.Median}\t{stat.BezMutacie}\t{stat.NahodnaBunka}\t{stat.XorBunka}\t{stat.XorBit}");
            File.WriteAllText(settings.Stats, sb.ToString());
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
            try
            {
                if (!Setup(out settings))
                {
                    x = -1;
                    y = -1;
                    plocha = null;
                    return false;
                }

                plocha = LoadPlocha(out x, out y);
                return true;
            }
            catch
            {
                x = -1;
                y = -1;
                settings = null;
                plocha = null;
                return false;
            }
        }

        /// <summary>
        ///     Nacita plochu zo vstupu
        /// </summary>
        /// <param name="x">X-ova zaciatocna poizica</param>
        /// <param name="y">Y-ova zaciatocna pozicia</param>
        /// <returns>
        ///     Plocha, na kt. sa hladaju poklady<</returns>
        private static Plocha LoadPlocha(out int x, out int y)
        {
            WritePlochaHelp();

            var plocha = Plocha.CreatePlocha();
            if (plocha == null)
                throw new NullReferenceException();
            // ReSharper disable once PossibleNullReferenceException
            var parts = Console.ReadLine().Split(new[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
            x = int.Parse(parts[0]);
            y = int.Parse(parts[1]);
            return plocha;
        }

        /// <summary>
        ///     Nacitaj nastavenia
        /// </summary>
        /// <param name="settings">Nastavenia, kt. sa nactiaju zo suboru</param>
        /// <returns>Aktualne nastavenia</returns>
        private static bool Setup(out Settings settings)
        {
            LoadSettings(out settings);

            if (settings == null)
            {
                Console.WriteLine("Error loading settings");
                Console.WriteLine("Press any key");
                Console.ReadKey(true);
                return false;
            }

            WriteSettings(settings);

            StopTimer.Elapsed -= OnStopTimerOnElapsed;
            if (settings.StopAfter.Typ == StopType.Seconds)
            {
                StopTimer.Interval = settings.StopAfter.Hodnota * 1000;
                StopTimer.Elapsed += OnStopTimerOnElapsed;
                settings.StopAfter.Hodnota = int.MaxValue;
                _timer = true;
            }

            _aktualnaGeneracia = new Jedinec[settings.MaxJedincov];
            _novaGeneracia = new Jedinec[settings.MaxJedincov];
            return true;
        }

        /// <summary>
        ///     Zastav cyklus po skonceni timer-u
        /// </summary>
        /// <param name="sender">Objekt, kt. zavola handler</param>
        /// <param name="args">Argumenty timer-u</param>
        private static void OnStopTimerOnElapsed(object sender, ElapsedEventArgs args)
        {
            _work = false;
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
                    if (settings.Output == OutputType.All)
                    {
                        PercentColor(jedinec.Poklady, plocha.PocetPokladov);
                        Console.WriteLine($"{jedinec.Fitness: 000;-000} {path}");
                    }
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
                $"Pomer mutacii: {settings.PomerMutacie.BezMutacie}:{settings.PomerMutacie.NahodnaBunka}:{settings.PomerMutacie.XorNahodnaBunka}:{settings.PomerMutacie.XorNahodnyBit}");
            Console.WriteLine(
                $"FITNESS | Poklad: +{settings.Fitness.Poklad} | Krok: -{settings.Fitness.Krok} | Vyjdenie mimo mriezky: -{settings.Fitness.VyjdenieMimoMriezky}");
            Console.WriteLine($"Vystup statistiky: {settings.Stats}");
            Console.WriteLine();
        }

        /// <summary>
        ///     Vypis sposob nacitania Plochy
        /// </summary>
        private static void WritePlochaHelp()
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
                var stream = File.OpenRead("settings.xml");
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
                settings = Settings.DefaultSettings();
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
            public readonly int BezMutacie;
            public readonly int NahodnaBunka;
            public readonly int XorBit;
            public readonly int XorBunka;

            public Stat(int max, double avg, int min, double median, int bezMutacie,
                int nahodnaBunka, int xorBit, int xorBunka)
            {
                Max = max;
                Min = min;
                Avg = avg;
                Median = median;
                BezMutacie = bezMutacie;
                NahodnaBunka = nahodnaBunka;
                XorBit = xorBit;
                XorBunka = xorBunka;
            }
        }
    }
}