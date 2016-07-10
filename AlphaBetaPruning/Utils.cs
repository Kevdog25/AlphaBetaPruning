using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaBetaPruning
{
    static class Utils
    {
        static Random rand = new Random();

        public static int RandInt(int L, int R)
        {
            return (int)(rand.NextDouble() * (R - L)) + L;
        }
    }
}
