using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNet
{
    class NeuralNetException : Exception
    {
        public NeuralNetException()
        {
        }

        public NeuralNetException(string message) : base(message)
        {
        }

        public NeuralNetException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NeuralNetException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
