using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AlphaBetaPruning.Shared;
using Action = AlphaBetaPruning.Shared.Action;

namespace AlphaBetaPruning.AILearner
{
    class DTActionClass : IActionClass
    {
        private SortedList<int,Action> ValidActions;

        private DTActionClass()
        {
            ValidActions = new SortedList<int, Action>();
        }

        public DTActionClass(List<Action> actions) : this()
        {
            for(var i = 0; i < actions.Count; i++)
                ValidActions.Add(i, actions[i]);
        }

        public DTActionClass(Distribution<Action> dist) : this()
        {
            KeyValuePair<Action, int>[] arr = dist.ToArray();
            for(var i = 0; i < arr.Length; i++)
                ValidActions.Add(arr[i].Value, arr[i].Key);
        }

        /// <summary>
        /// Checks to see if the action is part of this action class.
        /// </summary>
        /// <param name="act">Actin to consider</param>
        /// <returns>true if it is, false otherwise</returns>
        public bool IsMember(Action act)
        {
            return ValidActions.ContainsValue(act);
        }

        /// <summary>
        /// Reorders the actions in the provided list so that all the actions that 
        /// are a part of this action class are in the front of the list.
        /// </summary>
        /// <param name="actions">List of actions to consider</param>
        public void Reorder(List<Action> actions)
        {
            int index = 0;
            for(var i = 0; i < ValidActions.Count; i++)
            {
                if (actions.Remove(ValidActions[i]))
                {
                    actions.Insert(index, ValidActions[i]);
                    index++;
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            foreach(KeyValuePair<int, Action> pair in ValidActions)
            {
                sb.Append(pair.Value.ToString());
                sb.Append(";");
            }
            sb.Append(")");

            return sb.ToString();
        }
    }
}
