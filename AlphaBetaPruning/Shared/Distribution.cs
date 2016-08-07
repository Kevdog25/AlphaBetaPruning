using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaBetaPruning.Shared
{
    public class Distribution<T>
    {
        #region Private Fields
        private Dictionary<T, int> Dist;
        #endregion

        #region Properties
        public int NUnique { get; private set; }

        public int NElements { get; private set; }

        public bool IsDirty { get; private set; }

        public int this[T v]
        {
            get
            {
                int count;
                if (Dist.TryGetValue(v, out count))
                    return count;
                return 0;
            }
        }

        private float s;
        public float Entropy
        {
            get
            {
                if (!IsDirty)
                    return s;
                s = 0;
                foreach (KeyValuePair<T, int> pair in Dist)
                {
                    float p = (float)pair.Value / NElements;
                    s -= p * Utils.Log2(p);
                }
                IsDirty = false;
                return s;
            }
        }
        #endregion

        #region Constructors
        public Distribution()
        {
            IsDirty = true;
            Dist = new Dictionary<T, int>();
        }

        public Distribution(Dictionary<T, int> dist) : this()
        {
            Dist = dist;
            NElements = 0;
            foreach (KeyValuePair<T, int> pair in dist)
                NElements += pair.Value;
        }

        public Distribution(List<T> elements) : this()
        {
            for (var i = 0; i < elements.Count; i++)
                Add(elements[i]);
        }
        #endregion

        #region Public Methods
        public void Add(T e)
        {
            Add(e, 1);
        }

        public void Add(Distribution<T> other)
        {
            foreach (KeyValuePair<T, int> pair in other.Dist)
                Add(pair.Key, pair.Value);
        }

        public KeyValuePair<T,int>[] ToArray()
        {
            KeyValuePair<T, int>[] arr = new KeyValuePair<T, int>[Dist.Count];
            int i = 0;
            foreach(KeyValuePair<T,int> pair in Dist)
            {
                arr[i] = pair;
                i++;
            }

            return arr;
        }

        public bool Contains(T item)
        {
            return Dist.ContainsKey(item);
        }

        public T RemoveAny()
        {
            IsDirty = true;
            T key = Dist.Keys.First();
            int count = Dist[key];
            count--;

            if (count == 0)
            {
                Dist.Remove(key);
                NUnique--;
            }
            else
                Dist[key] = count;

            NElements--;
            return key;
        }
        #endregion

        #region Private Methods
        private void Add(T e, int inc)
        {
            IsDirty = true;
            int count;
            if (Dist.TryGetValue(e, out count))
                Dist[e] = count + inc;
            else
            {
                Dist.Add(e, inc);
                NUnique++;
            }
            NElements += inc;
        }
        #endregion
    }
}
