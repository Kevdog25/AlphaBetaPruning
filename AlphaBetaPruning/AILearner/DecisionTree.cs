using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlphaBetaPruning.GameDefinitions;

using Action = AlphaBetaPruning.Shared.Action;

namespace AlphaBetaPruning.AILearner
{
    abstract class DecisionTree
    {
        protected int NDimensions;

        protected class StandardizedState : IEquatable<StandardizedState>
        {
            private int[] values;

            public int Length
            {
                get { return values.Length; }
            }

            public int this[int i]
            {
                get
                {
                    return values[i];
                }
                set
                {
                    values[i] = value;
                }
            }

            public StandardizedState(int length)
            {
                values = new int[length];
            }

            #region IEquatable
            public bool Equals(StandardizedState other)
            {
                if (other.Length != Length)
                    return false;
                for (var i = 0; i < Length; i++)
                    if (other[i] != values[i])
                        return false;
                return true;
            }

            public override int GetHashCode()
            {
                int hash = 0;
                for (var i = 0; i < Length; i++)
                    hash += values[i].GetHashCode();

                return hash;
            }
            #endregion
        }

        protected class StateResponse : IEquatable<StateResponse>
        {
            public StandardizedState State;
            public Action Response { get; set; }

            public StateResponse(StandardizedState state, Action act = null)
            {
                Response = act;
                State = state;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as StateResponse);
            }

            #region IEquatable
            public bool Equals(StateResponse other)
            {
                return (State.Equals(other.State)) && (Response.Equals(other.Response));
            }

            public override int GetHashCode()
            {
                return Response.GetHashCode() + State.GetHashCode();
            }
            #endregion
        }

        #region Overridable Methods
        protected abstract IActionClass GenerateClassification(Dictionary<Action, float> dict);
        protected abstract StateResponse Convert(IGameState state, Action response = null);
        protected abstract StateResponse DeserializeItem(string item);
        protected abstract string SerializeItem(StateResponse item);
        protected virtual int[] AddTraceData(params int[] args) { return args; }
        #endregion
    }
}
