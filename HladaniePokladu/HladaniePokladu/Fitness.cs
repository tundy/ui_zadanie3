using System;
using System.Text;

namespace HladaniePokladu
{
    internal partial class Jedinec
    {
        private const int MaxInstrukcii = 500;

        private const int Increment = 0b00_000000;
        private const int Decrement = 0b01_000000;
        private const int Jump = 0b10_000000;
        private const int Print = 0b11_000000;

        private static int GetAddress(int value) => value & 0b00_111111;
        private static int GetInstruction(int value) => value & 0b11_000000;

        internal string DoStuff(Plocha plocha, Settings settings, int x, int y)
        {
            Fitness = 0;
            Poklady = 0;
            var working = (byte[]) _bunky.Clone();
            var poklady = (bool[,]) plocha.Poklad.Clone();
            var path = new StringBuilder();
            for (int i = 0, index = 0; i < MaxInstrukcii; i++)
            {
                if (index >= 64) index = 0;
                var value = working[index];
                var ins = GetInstruction(value);
                switch (ins)
                {
                    case Increment:
                        unchecked
                        {
                            ++working[GetAddress(value)];
                        }
                        break;
                    case Decrement:
                        unchecked
                        {
                            --working[GetAddress(value)];
                        }
                        break;
                    case Jump:
                        index = GetAddress(value);
                        continue;
                    case Print:
                        if (AddStep(plocha, ref x, ref y, working[GetAddress(value)] & 0b11, path))
                        {
                            Fitness -= settings.Fitness.VyjdenieMimoMriezky;
                            return path.ToString();
                        }
                        Fitness -= settings.Fitness.Krok;
                        if (poklady[x, y])
                        {
                            path.Append('$');
                            Fitness += settings.Fitness.Poklad;
                            if (++Poklady == plocha.PocetPokladov) return path.ToString();
                            poklady[x, y] = false;
                        }
                        break;
                    default:
                        throw new Exception("Unknown instruction");
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