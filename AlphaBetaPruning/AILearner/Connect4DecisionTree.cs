using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaBetaPruning.AILearner
{
    class Connect4DecisionTree : DecisionTree
    {
        public Connect4DecisionTree(string fp)
        {
            LoadFromDatabase(fp);
            
            BuildTree();
        }

        public Connect4DecisionTree()
        {

        }

        #region Overriden
        protected override StateResponse Convert(IGameState inState, Action response = null)
        {
            Connect4.Connect4State state = cast(inState);
            StateResponse sr = new StateResponse(43,response);
            for(var i = 0; i < 7; i++)
            {
                for(var j = 0; j < 6; j++)
                {
                    sr[i * 6 + j] = state.board[i, 1 + j];
                }
            }
            sr[42] = (int)state.toMove;

            return sr;
        }

        protected override IActionClass GenerateClassification(Dictionary<Action, float> dict)
        {
            DTActionClass ac = new DTActionClass(dict.Keys.ToList());
            return ac;
        }

        protected override StateResponse DeserializeItem(string item)
        {
            string[] split = item.Split(';');
            string[] values = split[0].Split(',');
            string[] response = split[1].Split(',');

            Connect4.Connect4Action act = 
                new Connect4.Connect4Action(null, 
                    int.Parse(response[0]), 
                    (Connect4.Player)int.Parse(response[1])
                    );

            StateResponse sr = new StateResponse(values.Length, act);
            for(var i = 0; i < sr.Dimensions; i++)
            {
                sr[i] = int.Parse(values[i]);
            }
            return sr;
        }

        protected override string SerializeItem(StateResponse item)
        {
            StringBuilder sb = new StringBuilder();
            Connect4.Connect4Action act = item.Response as Connect4.Connect4Action;
            for(var i = 0; i < item.Dimensions-1; i++)
            {
                sb.Append(item[i] + ",");
            }
            sb.Append(item[item.Dimensions - 1]);
            if(act != null)
            {
                sb.Append(";");
                sb.Append(act.Column + "," + (int)act.MovingPlayer + "\n");
            }

            return sb.ToString();
        }
        #endregion

        #region Private Methods
        private Connect4.Connect4State cast(IGameState inS)
        {
            Connect4.Connect4State s = inS as Connect4.Connect4State;
            if (s == null)
                throw new GameSpecificationException(string.Format("Cannot cast {0} to {1}",inS.GetType(),s.GetType()));

            return s;
        }
        #endregion
    }
}
