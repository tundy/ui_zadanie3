using System;
using System.Text;

namespace HladaniePokladu
{
    internal partial class Jedinec
    {
        private const int MaxInstrukcii = 500;

        internal string CountFitness(Plocha plocha, int x, int y)
        {
            Fitness = 0;
            var working = (byte[]) _bunky.Clone();
            var poklady = (bool[,]) plocha.Poklad.Clone();
            var path = new StringBuilder();
            var index = 0;
            for (var i = 0; i < MaxInstrukcii; i++)
            {
                if (index >= 64) index = 0;
                var value = working[index];
                switch (value & 0b11_000000)
                {
                    case 0b00_000000:
                        unchecked
                        {
                            ++working[value & 0b00_111111];
                        }
                        break;
                    case 0b01_000000:
                        unchecked
                        {
                            --working[value & 0b00_111111];
                        }
                        break;
                    case 0b10_000000:
                        index = value & 0b00_111111;
                        continue;
                    case 0b11_000000:
                        if (AddStep(plocha, ref x, ref y, working[value & 0b00_111111] & 0b11, path))
                            return path.ToString();
                        if (poklady[x, y])
                        {
                            path.Append('$');
                            if (++Fitness == plocha.PocetPokladov) return path.ToString();
                            poklady[x, y] = false;
                        }
                        break;
                    default:
                        throw new Exception();
                }
                ++index;
            }
            return path.ToString();
        }

        private static bool AddStep(Plocha plocha, ref int x, ref int y, int val, StringBuilder path)
        {
            char step;
            switch (val)
            {
                case 0b00:
                    step = 'H';
                    --y;
                    break;
                case 0b01:
                    step = 'D';
                    ++y;
                    break;
                case 0b10:
                    step = 'P';
                    ++x;
                    break;
                case 0b11:
                    step = 'L';
                    --x;
                    break;
                default:
                    throw new Exception();
            }
            if (x < 0 || x >= plocha.Width || y < 0 || y >= plocha.Height) return true;
            path.Append(step);
            return false;
        }
    }
}