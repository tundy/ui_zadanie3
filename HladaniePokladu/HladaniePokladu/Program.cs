using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace HladaniePokladu
{
    internal static partial class Program
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
            // Krok 1 až 4 podľa dokumentácie
            if (!Init(out var plocha, out var x, out var y, out var settings)) return;

            for (;;)
            {
                // (5) vytvor prvú generáciu
                InitLoop(settings);
                for (var generacia = 0;;)
                {
                    ++generacia;

                    // (6) Ak presiel urceny cas na hladanie alebo presiahol max limit generacii: Skonci
                    if (!_work || generacia >= settings.StopAfter.Hodnota)
                    {
                        // Vypis najlepsi vysledok
                        PrintStopped(plocha, settings, x, y, generacia);

                        // Nehcaj pouzivatela rozhodnut co dalej
                        if (SpracujVstup(ref settings, ref x, ref y, ref plocha)) return;
                        break;
                    }

                    // (7) Urči Fitness pre aktuálnu generáciu
                    var result = CalculateFitness(plocha, settings, x, y, out var final);

                    // (8)
                    if (!result.IsCompleted)
                    {
                        // (8.1)
                        if (final == null || final.Item1.Poklady != plocha.PocetPokladov)
                        {
                            // niekde je chyba alebo som musel skoncit
                            _work = false;
                            continue;
                        }

                        // (8.2)
                        PrintResult(settings, generacia, final);

                        // (8.3)
                        if (SpracujVstup(ref settings, ref x, ref y, ref plocha)) return;
                        break;
                    }

                    // (9)
                    var sorted = ZoradJedincov(out var total);

                    if (settings.Output == OutputType.Top)
                    {
                        PercentColor(sorted[0].Poklady, plocha.PocetPokladov);
                        Console.WriteLine(
                            $"{sorted[0].Fitness: 000;-000} {sorted[0].DoStuff(plocha, settings, x, y)}");
                    }

                    // (10)
                    var index = VyberElitu(settings, sorted);
                    VytvorNovuGeneraciu(settings, _aktualnaGeneracia, index, total);
                    // (13)
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
        }

        /// <summary>
        ///     Zarovna fitness a zoradi jedincov
        /// </summary>
        /// <param name="total">Suma vsetkych fitness</param>
        /// <returns>Vrati zoradeny zoznam jedincov</returns>
        private static Jedinec[] ZoradJedincov(out int total)
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
            var max = sorted[0].Fitness;
            var min = sorted.Last().Fitness;
            var sum = 0;

            total = 0;
            --min;
            foreach (var jedinec in sorted)
            {
                sum += jedinec.Fitness;
                jedinec.Fitness -= min;
                total += jedinec.Fitness;
            }

            var stat = new Stat(max, (double) sum / sorted.Length, ++min, median);
            Stats.Add(stat);


            return sorted;
        }

        /// <summary>
        ///     Vytvori novu generaciu jedincov
        /// </summary>
        /// <param name="settings">Nastavenia algoritmu</param>
        /// <param name="populacia">Zoradeny zoznam jedincov</param>
        /// <param name="index">Index od kt pokracuje naplnat novu generaciu</param>
        /// <param name="total">Fitness suma</param>
        private static void VytvorNovuGeneraciu(Settings settings, Jedinec[] populacia, int index, int total)
        {
            /*Parallel.For(index, settings.MaxJedincov,
                i =>
                {
                    Jedinec a, b;
                    // (11)
                    if (settings.SelectionType == SelectionType.Ruleta)
                        Ruleta(populacia, total, out a, out b);
                    else
                        Turnaj(populacia, out a, out b);

                    var novyJedinec = a.Krizenie(b, settings);
                    // (12)
                    novyJedinec.Mutuj(settings);
                    _novaGeneracia[i] = novyJedinec;
                }
                );*/
            for(var i = index; i < settings.MaxJedincov; i++)
            {
                Jedinec a, b;
                // (11)
                if (settings.SelectionType == SelectionType.Ruleta)
                    Ruleta(populacia, total, out a, out b);
                else
                    Turnaj(populacia, out a, out b);

                var novyJedinec = a.Krizenie(b, settings);
                // (12)
                novyJedinec.Mutuj(settings);
                _novaGeneracia[i] = novyJedinec;
            }
        }

        /// <summary>
        /// Vyber rodicov pomocou turnaja
        /// </summary>
        /// <param name="populacia">Pole jedincov</param>
        /// <param name="a">Prvy rodic</param>
        /// <param name="b">Druhy rodic</param>
        private static void Turnaj(IReadOnlyList<Jedinec> populacia, out Jedinec a, out Jedinec b)
        {
            var vyber = new List<Jedinec>(4);
            while (vyber.Count < 4)
            {
                var dalsi = populacia[Rand.Next(populacia.Count)];
                if (vyber.Contains(dalsi)) continue;
                vyber.Add(dalsi);
            }

            vyber.Sort((jedinec1, jedinec2) => jedinec1.Fitness.CompareTo(jedinec2.Fitness));

            a = vyber[3];
            b = vyber[2];
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
            end = Math.Min(end, settings.MaxJedincov);
            for (; index < (int) end; index++)
                _novaGeneracia[index] = sorted[index];
            return index;
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
        ///     Zastav cyklus po skonceni timer-u
        /// </summary>
        /// <param name="sender">Objekt, kt. zavola handler</param>
        /// <param name="args">Argumenty timer-u</param>
        private static void OnStopTimerOnElapsed(object sender, ElapsedEventArgs args)
        {
            _work = false;
        }

        /// <summary>
        ///     Spusti parallerny vypocet fitness
        /// </summary>
        /// <param name="plocha">Plocha, na kt. sa hladaju poklady</param>
        /// <param name="settings"></param>
        /// <param name="x">X-ova zaciatocna suradnica</param>
        /// <param name="y">Y-ova zaciatocna suradnica</param>
        /// <param name="final">Jedinec, kt. sa podarilo najst cestu + cesta</param>
        /// <returns>Vysledok Parallel loop-u</returns>
        private static ParallelLoopResult CalculateFitness(Plocha plocha, Settings settings, int x, int y,
            out Tuple<Jedinec, string> final)
        {
            Tuple<Jedinec, string> tempFinal = null;

            var writeLocker = new object();
            var locker = new object();

            // (7)
            var result = Parallel.ForEach(_aktualnaGeneracia, (jedinec, state) =>
            {
                // (7.1)
                if (!_work) state.Stop();
                // (7.2)
                var path = jedinec.DoStuff(plocha, settings, x, y);
                if (settings.Output == OutputType.All)
                {
                    PercentColor(jedinec.Poklady, plocha.PocetPokladov);
                    lock (writeLocker)
                    {
                        Console.WriteLine($"{jedinec.Fitness: 000;-000} {path}");
                    }
                }

                // (7.3)
                if (jedinec.Poklady != plocha.PocetPokladov || state.IsStopped) return;
                state.Stop();
                lock (locker)
                {
                    tempFinal = new Tuple<Jedinec, string>(jedinec, path);
                }
            });
            final = tempFinal;
            return result;
        }

        /// <summary>
        ///     Hlada jedincov zo zoradeneho pola pomocou rulety
        /// </summary>
        /// <param name="sorted">Zoradene pole jedincov</param>
        /// <param name="total">Suma fitness vsetkych jedincov</param>
        /// <param name="a">Rodic A</param>
        /// <param name="b">Rodic B</param>
        private static void Ruleta(IReadOnlyList<Jedinec> sorted, int total, out Jedinec a, out Jedinec b)
        {
            var last = 0;
            var index = Rand.Next(total);
            a = null;
            var skip = -1;
            for (var i = 0; i < sorted.Count; i++)
            {
                if (index < sorted[i].Fitness + last)
                {
                    a = sorted[i];
                    skip = i;
                    break;
                }
                last += sorted[i].Fitness;
            }
            if (a == null)
            {
                a = sorted.Last();
                skip = sorted.Count - 1;
            }
            total -= a.Fitness;

            last = 0;
            index = Rand.Next(total);
            b = null;
            for (var i = 0; i < sorted.Count; i++)
            {
                if(i == skip) continue;
                if (index < sorted[i].Fitness + last)
                {
                    b = sorted[i];
                    break;
                }
                last += sorted[i].Fitness;
            }
            if(b == null)
                b = sorted.Last();
        }
    }
}