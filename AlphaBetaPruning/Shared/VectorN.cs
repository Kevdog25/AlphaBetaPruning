using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaBetaPruning.Shared
{
    [Serializable]
    class VectorN
    {
        public int N { get; private set; }

        private float[] values;
        public float this[int i]
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

        public VectorN(int n)
        {
            N = n;
            values = new float[n];
            for (var i = 0; i < N; i++)
                values[i] = 0;
        }

        public VectorN(List<float> v) : this(v.Count)
        {
            for (var i = 0; i < N; i++)
                values[i] = v[i];
        }

        public VectorN(float[] v) : this(v.Length)
        {
            for (var i = 0; i < N; i++)
                values[i] = v[i];
        }

        public float Normalize()
        {
            float norm = Length();
            for (var i = 0; i < N; i++)
                values[i] /= norm;

            return norm;
        }

        public float Length()
        {
            return (float)Math.Sqrt(Length2());
        }

        public float Length2()
        {
            float norm = 0;
            for (var i = 0; i < N; i++)
                norm += values[i] * values[i];
            return norm;
        }

        public static VectorN operator -(VectorN v1, VectorN v2)
        {
            VectorN ret = new VectorN(v1.N);
            for (var i = 0; i < v1.N; i++)
                ret[i] = v1[i] - v2[i];
            return ret;
        }

        public static VectorN operator +(VectorN v1, VectorN v2)
        {
            VectorN ret = new VectorN(v1.N);
            for (var i = 0; i < v1.N; i++)
                ret[i] = v1[i] + v2[i];
            return ret;
        }

        public static float operator *(VectorN v1, VectorN v2)
        {
            float v = 0;
            for (var i = 0; i < v1.N; i++)
                v += v2[i] * v1[i];
            return v;
        }

        public static VectorN operator *(VectorN v, float a)
        {
            VectorN ret = new VectorN(v.N);
            for (var i = 0; i < v.N; i++)
                ret[i] = a * v[i];
            return ret;
        }
        public static VectorN operator *(float a, VectorN v)
        {
            VectorN ret = new VectorN(v.N);
            for (var i = 0; i < v.N; i++)
                ret[i] = a * v[i];
            return ret;
        }

        public static VectorN operator /(VectorN v, float a)
        {
            VectorN ret = new VectorN(v.N);
            for (var i = 0; i < v.N; i++)
                ret[i] = v[i] / a;
            return ret;
        }

        public override bool Equals(object obj)
        {
            VectorN localObj = obj as VectorN;
            return Equals(localObj);
        }

        public bool Equals(VectorN other)
        {
            if (other == null || other.N != N)
                return false;
            for (var i = 0; i < N; i++)
                if (other[i] != values[i])
                    return false;
            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (int v in values)
                hash += v.GetHashCode();

            return hash;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("<");
            for (var i = 0; i < N - 1; i++)
                sb.Append(string.Format("{0},", values[i]));
            sb.Append(string.Format("{0}>",values[N-1]));
            return sb.ToString();
        }
    }
}
