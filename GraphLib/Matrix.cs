using System;

[Serializable]
public class Matrix
{
    protected double[,] matr;
    public int rows, columns;

    public Matrix(int rows, int columns)
    {
        this.rows = rows;
        this.columns = columns;
        matr = new double[rows, columns];
    }

    public Matrix(int n) : this(n, n) { }

    public double this[int i, int j]
    {
        get => matr[i, j];
        set => matr[i, j] = value;
    }

    public Matrix Multiply(Matrix m)
    {
        if (this.columns != m.rows)
            return null;

        Matrix res = new Matrix(rows, m.columns);
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < m.columns; j++)
            {
                double sum = 0;
                for (int k = 0; k < columns; k++)
                    sum += this[i, k] * m[k, j];
                res[i, j] = sum;
            }
        }
        return res;
    }

    public Matrix MultiplyMaxPlus(Matrix m)
    {
        if (this.columns != m.rows)
            return null;

        Matrix res = new Matrix(rows, m.columns);
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < m.columns; j++)
            {
                double max = this[i, 0] + m[0, j];
                for (int k = 0; k < columns; k++)
                    if ((this[i, k] + m[k, j]) > max)
                        max = this[i, k] + m[k, j];
                res[i, j] = max;
            }
        }
        return res;
    }

    public Vector Multiply(Vector v)
    {
        Matrix m = v;
        m = this.Multiply(m);
        return new Vector(m);
    }

    public Vector MultiplyMaxPlus(Vector v)
    {
        Matrix m = v;
        m = this.MultiplyMaxPlus(m);
        return new Vector(m);
    }

    protected Matrix Transpone()
    {
        Matrix newMatr = new Matrix(columns, rows);
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < columns; j++)
                newMatr[j, i] = matr[i, j];
        return newMatr;
    }

    /// <summary>
    /// Sums elements in the column.
    /// </summary>
    /// <param name="index">column number</param>
    /// <returns></returns>
    protected double GetColumnSum(int index)
    {
        double sum = 0;
        for (int i = 0; i < rows; i++)
            sum += this[i, index];
        return sum;
    }

    /// <summary>
    /// Sums elements in the row.
    /// </summary>
    /// <param name="index">row number</param>
    /// <returns></returns>
    public double GetRowSum(int index)
    {
        double sum = 0;
        for (int i = 0; i < columns; i++)
            sum += this[index, i];
        return sum;
    }

    public override string ToString()
    {
        string res = "";
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (this[i, j] == (int)this[i, j])
                    res += this[i, j] + " ";
                else
                    res += $"{this[i, j]:f3} ";
            }
            if (i != rows - 1)
                res += '\n';
        }
        return res;
    }

    /// <summary>
    /// Converts matrix to format used for matrices in Wolfram Methematica.
    /// </summary>
    /// <returns></returns>
    public string ToWMFormat()
    {
        string result = "";
        for (int i = 0; i < rows; i++)
        {
            result += "{";
            for (int j = 0; j < columns; j++)
            {
                string temp = this[i, j].ToString().Replace(",", ".");
                result += temp;
                if (j != columns - 1)
                    result += ",";
            }
            result += "}";
            if (i != rows - 1)
                result += ",";
        }
        return "{" + result + "}";
    }
}

