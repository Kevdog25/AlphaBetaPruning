﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaBetaPruning.AILearner
{
    interface IActionClass
    {
        bool IsMember(Action act);
        void Reorder(List<Action> actions);
    }
}
