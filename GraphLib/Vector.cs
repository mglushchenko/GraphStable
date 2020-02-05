using System;

[Serializable]
public class Vector : Matrix
{
    /// <summary>
    /// Forms vector from matrix with one column
    /// </summary>
    /// <param name="m"></param>
    public Vector(Matrix m) : base(m.rows, 1)
    {
        if (m.columns != 1) throw new ArgumentException();

        for (int i = 0; i < m.rows; i++)
            this[i] = m[i, 0];
    }

    /// <summary>
    /// Copying constructor
    /// </summary>
    /// <param name="v"></param>
    public Vector(Vector v) : this(v.Size)
    {
        for (int i = 0; i < v.Size; i++)
            this[i] = v[i];
    }

    public Vector(int n) : base(n, 1) { }

    public double this[int i]
    {
        get => matr[i, 0];
        set => matr[i, 0] = value;
    }

    /// <summary>
    /// Sum of all elements.
    /// </summary>
    /// <returns></returns>
    public double GetSum()
    {
        return base.GetColumnSum(0);
    }

    public int Size { get => rows; }

    /// <summary>
    /// Counts zero elements.
    /// </summary>
    /// <returns></returns>
    public int NumberOfZeroes()
    {
        int count = 0;
        for (int i = 0; i < rows; i++)
            if (this[i, 0] == 0)
                count++;
        return count;
    }

    /// <summary>
    /// Counts non-zero elements.
    /// </summary>
    /// <returns></returns>
    public int NumberOfPoints()
    {
        return Size - NumberOfZeroes();
    }

    public override string ToString()
    {
        Matrix m = this.Transpone();
        string res = m.ToString();
        return "(" + res + ")";
    }

    /// <summary>
    /// Checks if two vectors are proportional in (MAX,+).
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public bool IsProportionalTo(Vector v, out double coeff)
    {
        if (this.Size != v.Size)
            throw new ArgumentException("incorrect vector length!");

        double dif = this[0] - v[0];
        coeff = Math.Abs(dif);
        bool res = true;
        for (int i = 1; i < Size; i++)
        {
            double currDif = this[i] - v[i];
            if (Math.Abs(currDif - dif) > Math.Pow(10, -5))
            {
                res = false;
                coeff = 0;
                break;
            }
        }
        return res;
    }
}

