using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaBetaPruning.AILearner
{
    class DTActionClass : IActionClass
    {
        private List<Action> ValidActions;

        public DTActionClass(List<Action> actions)
        {
            ValidActions = actions;
        }

        /// <summary>
        /// Checks to see if the action is part of this action class.
        /// </summary>
        /// <param name="act">Actin to consider</param>
        /// <returns>true if it is, false otherwise</returns>
        public bool IsMember(Action act)
        {
            return ValidActions.Contains(act);
        }

        /// <summary>
        /// Reorders the actions in the provided list so that all the actions that 
        /// are a part of this action class are in the front of the list.
        /// </summary>
        /// <param name="actions">List of actions to consider</param>
        public void Reorder(List<Action> actions)
        {
            int index = 0;
            Action temp = null;
            for(var i = 0; i < actions.Count; i++)
            {
                if (IsMember(actions[i]))
                {
                    temp = actions[index];
                    actions[index] = actions[i];
                    actions[i] = temp;
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            foreach(Action a in ValidActions)
            {
                sb.Append(a.ToString());
                sb.Append(";");
            }
            sb.Append(")");

            return sb.ToString();
        }
    }
}
