using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNet
{
    [Serializable]
    class Matrix
    {
        private bool transpose;
        private int rows;
        public int Rows
        {
            get
            {
                if (transpose)
                    return cols;
                return rows;
            }
            private set
            {
                rows = value;
            }
        }
        private int cols;
        public int Cols
        {
            get
            {
                if (transpose)
                    return rows;
                return cols;
            }
            private set
            {
                cols = value;
            }
        }

        private double[,] matrix;
        public double this[int i, int j = 0]
        {
            get
            {
                if(transpose)
                    return matrix[j, i];
                return matrix[i, j];
            }
            set
            {
                if(transpose)
                    matrix[j, i] = value;
                matrix[i, j] = value;
            }
        }

        public Matrix(int m, int n = 1)
        {
            matrix = new double[m, n];
            Rows = m;
            Cols = n;
        }

        public Matrix NewTranspose()
        {
            Matrix val = new Matrix(Cols, Rows);
            for (var i = 0; i < Rows; i++)
                for (var j = 0; j < Cols; j++)
                    val[j, i] = matrix[i, j];

            return val;
        }

        public void Transpose()
        {
            transpose = !transpose;
        }

        public Matrix Inverse()
        {
            throw new NotImplementedException();
        }
        public void Invert()
        {
            throw new NotImplementedException();
        }

        public double Sum()
        {
            double s = 0;
            for (var m = 0; m < Rows; m++)
                for (var n = 0; n < Cols; n++)
                    s += this[m, n];
            return s;
        }

        #region Operator Overloads
        public static Matrix operator *(Matrix m1, Matrix m2)
        {
            if (m1.Cols != m2.Rows)
                throw new ArgumentException("Cannot multiply an mxn matrix with a kxp matrix where n != k");
            Matrix val = new Matrix(m1.Rows, m2.Cols);

            for (var i = 0; i < m1.Rows; i++)
                for (var j = 0; j < m2.Cols; j++)
                    for (var k = 0; k < m1.Cols; k++)
                        val[i, j] += m1[i, k] * m2[k, j];

            return val;
        }

        public static Matrix operator +(Matrix m1, Matrix m2)
        {
            if (m1.Rows != m2.Rows || m1.Cols != m2.Cols)
                throw new ArgumentException("Cannot add matrices of differing dimension");
            Matrix val = new Matrix(m1.Rows,m1.Cols);
            for (var i = 0; i < m1.Rows; i++)
                for (var j = 0; j < m1.Cols; j++)
                    val[i, j] = m1[i, j] + m2[i, j];

            return val;
        }

        public static Matrix operator -(Matrix m1, Matrix m2)
        {
            if (m1.Rows != m2.Rows || m1.Cols != m2.Cols)
                throw new ArgumentException("Cannot add matrices of differing dimension");
            Matrix val = new Matrix(m1.Rows, m1.Cols);
            for (var i = 0; i < m1.Rows; i++)
                for (var j = 0; j < m1.Cols; j++)
                    val[i, j] = m1[i, j] - m2[i, j];

            return val;
        }

        public static Matrix operator *(Matrix m, double a)
        {
            Matrix val = new Matrix(m.Rows, m.Cols);
            for (var i = 0; i < m.Rows; i++)
                for (var j = 0; j < m.Cols; j++)
                    val[i, j] = m[i, j] * a;

            return val;
        }

        public static Matrix operator *(double a, Matrix m)
        {
            Matrix val = new Matrix(m.Rows, m.Cols);
            for (var i = 0; i < m.Rows; i++)
                for (var j = 0; j < m.Cols; j++)
                    val[i, j] = m[i, j] * a;

            return val;
        }

        public static Matrix operator /(Matrix m, double a)
        {
            Matrix val = new Matrix(m.Rows, m.Cols);
            for (var i = 0; i < m.Rows; i++)
                for (var j = 0; j < m.Cols; j++)
                    val[i, j] = m[i, j] / a;

            return val;
        }
        #endregion

        public static Matrix Hadmard(Matrix m1, Matrix m2)
        {
            if (m1.Rows != m2.Rows || m1.Cols != m2.Cols)
                throw new ArgumentException();
            Matrix val = new Matrix(m1.Rows, m1.Cols);

            for (var i = 0; i < m1.Rows; i++)
                for (var j = 0; j < m1.Cols; j++)
                    val[i, j] = m1[i, j] * m2[i, j];

            return val;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for(var m = 0; m < Rows; m++)
            {
                sb.Append("|");
                for (var n = 0; n < Cols - 1; n++)
                    sb.Append(string.Format("{0,0:G2}",matrix[m,n]) + ", ");
                sb.Append(string.Format("{0,0:G2}", matrix[m, Cols-1]) + "|\n");
            }

            return sb.ToString();
        }

        public void SetData(params double[] values)
        {
            for (var i = 0; i < values.Length; i++)
                this[i / Cols, i % Cols] = values[i];
        }
    }
}
