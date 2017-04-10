using System;
using System.IO;
using System.Xml.Serialization;

namespace HladaniePokladu
{
    internal static partial class Program
    {
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
                Console.WriteLine("ESC/K - pre ukoncenie programu");
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
                        case ConsoleKey.K:
                        case ConsoleKey.Escape:
                            return true;
                        case ConsoleKey.S:
                            if (!Setup(out settings))
                                return true;
                            continue;
                        case ConsoleKey.P:
                            plocha = LoadPlocha(settings, out x, out y);
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

                plocha = LoadPlocha(settings, out x, out y);
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
        /// <param name="settings"></param>
        /// <param name="x">X-ova zaciatocna poizica</param>
        /// <param name="y">Y-ova zaciatocna pozicia</param>
        /// <returns>
        ///     Plocha, na kt. sa hladaju poklady</returns>
        private static Plocha LoadPlocha(Settings settings, out int x, out int y)
        {
            WritePlochaHelp();

            var stream = File.OpenText(settings.Plocha);

            var plocha = Plocha.CreatePlocha(stream);
            if (plocha == null)
                throw new NullReferenceException();
            // ReSharper disable once PossibleNullReferenceException
            var parts = stream.ReadLine().Split(new[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
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
    }
}