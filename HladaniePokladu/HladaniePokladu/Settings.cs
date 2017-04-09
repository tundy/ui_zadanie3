using System.Xml.Serialization;

namespace HladaniePokladu
{
    public class Settings
    {
        [XmlElement] public StopAfter StopAfter;
        [XmlElement(IsNullable = true)] public Elitarizmus? Elitarizmus;
        [XmlElement] public MaxMin BodKrizenia;

        [XmlElement] public Fitness Fitness;

        [XmlAttribute] public int InitRadnom;
        [XmlAttribute] public int MaxJedincov;
        [XmlAttribute] public SelectionType SelectionType;
        [XmlAttribute] public OutputType Output;
        [XmlElement] public Mutation PomerMutacie;
        [XmlAttribute] public string Stats;


        public static Settings DefaultSettings() => new Settings
        {
            Elitarizmus = new Elitarizmus(10, EliteType.Percent),
            Output = OutputType.Result,
            SelectionType = SelectionType.Ruleta,
            BodKrizenia = new MaxMin(24, 40),
            StopAfter = new StopAfter(5, StopType.Seconds),
            Fitness = new Fitness(100, 1, 5),
            MaxJedincov = 250,
            InitRadnom = 48,
            PomerMutacie = new Mutation(90, 2, 3, 5),
            Stats = "stats3.txt"
        };
    }

    public enum SelectionType
    {
        Ruleta,
        Turnaj
    }

    public enum OutputType
    {
        All,
        Top,
        Result
    }

    public struct Fitness
    {
        [XmlAttribute] public int Poklad;
        [XmlAttribute] public int Krok;
        [XmlAttribute] public int VyjdenieMimoMriezky;

        public Fitness(int poklad, int krok, int mimoMriezky)
        {
            Poklad = poklad;
            Krok = krok;
            VyjdenieMimoMriezky = mimoMriezky;
        }
    }

    public class Mutation
    {
        private int _bezMutacie;
        private int _nahodnaBunka;
        private int _xorNahodnaBunka;
        private int _xorNahodnyBit;
        [XmlIgnore] public int Total;

        public Mutation()
        {
        }

        public Mutation(int bezMutacie, int nahodnaBunka, int xorNahodnaBunka, int xorNahodnyBit)
        {
            _bezMutacie = bezMutacie;
            _nahodnaBunka = nahodnaBunka;
            _xorNahodnyBit = xorNahodnyBit;
            _xorNahodnaBunka = xorNahodnaBunka;
            Total = bezMutacie + nahodnaBunka + xorNahodnyBit + xorNahodnaBunka;
        }

        [XmlAttribute]
        public int BezMutacie
        {
            get { return _bezMutacie; }
            set
            {
                Total -= _bezMutacie;
                _bezMutacie = value;
                Total += value;
            }
        }

        [XmlAttribute]
        public int NahodnaBunka
        {
            get { return _nahodnaBunka; }
            set
            {
                Total -= _nahodnaBunka;
                _nahodnaBunka = value;
                Total += value;
            }
        }

        [XmlAttribute]
        public int XorNahodnaBunka
        {
            get { return _xorNahodnaBunka; }
            set
            {
                Total -= _xorNahodnaBunka;
                _xorNahodnaBunka = value;
                Total += value;
            }
        }

        [XmlAttribute]
        public int XorNahodnyBit
        {
            get { return _xorNahodnyBit; }
            set
            {
                Total -= _xorNahodnyBit;
                _xorNahodnyBit = value;
                Total += value;
            }
        }
    }

    public struct StopAfter
    {
        [XmlAttribute] public int Hodnota;
        [XmlAttribute] public StopType Typ;

        public StopAfter(int value, StopType type)
        {
            Hodnota = value;
            Typ = type;
        }
    }

    public enum StopType
    {
        [XmlEnum(Name = "Gens")] Generations,
        [XmlEnum(Name = "Secs")] Seconds
    }

    public struct MaxMin
    {
        public MaxMin(int min, int max)
        {
            Min = min;
            Max = max;
        }

        [XmlAttribute] public int Min;
        [XmlAttribute] public int Max;
    }

    public struct Elitarizmus
    {
        public Elitarizmus(double hodnota, EliteType type)
        {
            Hodnota = hodnota;
            Typ = type;
        }

        [XmlAttribute] public double Hodnota;

        [XmlAttribute] public EliteType Typ;
    }

    public enum EliteType
    {
        [XmlEnum(Name = "Percenta")] Percent,
        [XmlEnum(Name = "Pocet")] Count
    }
}