using System;

namespace AlphaBetaPruning
{
    class GameSpecificationException : Exception
    {
        public GameSpecificationException() : base()
        {

        }

        public GameSpecificationException(string message) : base(message)
        {

        }

        public GameSpecificationException(string message, Exception ex) : base(message,ex)
        {

        }
    }
}
