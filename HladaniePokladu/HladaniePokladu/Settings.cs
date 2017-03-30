using System.Xml.Serialization;

namespace HladaniePokladu
{
    public class Settings
    {
        [XmlAttribute]
        public int MaxJedincov;
        [XmlAttribute]
        public int MaxGeneracii;
        [XmlAttribute]
        public int InitRadnom;

        [XmlElement(IsNullable = true)]
        public Elitarizmus? Elitarizmus;

        [XmlElement]
        public MaxMin BodKrizenia;
    }

    public struct MaxMin
    {
        public MaxMin(int min, int max)
        {
            Min = min;
            Max = max;
        }

        [XmlAttribute]
        public int Min;
        [XmlAttribute]
        public int Max;
    }

    public struct Elitarizmus
    {
        public Elitarizmus(double hodnota, Type type)
        {
            Hodnota = hodnota;
            Typ = type;
        }

        [XmlAttribute]
        public double Hodnota;

        [XmlAttribute]
        public Type Typ;
    }

    public enum Type
    {
        [XmlEnum(Name = "Percenta")]
        Percent,
        [XmlEnum(Name = "Pocet")]
        Count
    }
}
