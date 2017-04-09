using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HladaniePokladu
{
    internal static partial class Program
    {
        private static readonly List<Stat> Stats = new List<Stat>();

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
            Console.WriteLine($"Typ selekcie: {settings.SelectionType}");
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
            var path = jedinec.DoStuff(plocha, settings, x, y);

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
        ///     Ulozi statistiku do subora
        /// </summary>
        private static void SaveStats(Settings settings)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Maximum\tPriemer\tMinimum\tMedian");
            foreach (var stat in Stats)
                sb.AppendLine($"{stat.Max}\t{stat.Avg}\t{stat.Min}\t{stat.Median}");
            File.WriteAllText(settings.Stats, sb.ToString());
        }
    }
}