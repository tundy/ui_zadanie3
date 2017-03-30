using System.Xml.Serialization;

namespace HladaniePokladu
{
    public class Settings
    {
        [XmlElement] public StopAfter StopAfter;

        [XmlElement(IsNullable = true)] public Elitarizmus? Elitarizmus;

        [XmlElement] public MaxMin BodKrizenia;

        [XmlAttribute] public int InitRadnom;

        [XmlAttribute] public int MaxJedincov;
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