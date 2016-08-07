using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaBetaPruning.Shared
{
    static class Utils
    {
        static Random rand = new Random();
        static float Ln2 = (float)Math.Log(2);

        public static float Log2(float x)
        {
            return (float)Math.Log(x) / Ln2;
        }

        public static int RandInt(int L, int R)
        {
            return (int)(rand.NextDouble() * (R - L)) + L;
        }
    }
}
