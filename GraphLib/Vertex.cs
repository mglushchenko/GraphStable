using System;

[Serializable]
public class Vertex
{
    public double center_X, center_Y;
    public const double rad = 7;
    public int Number { get; set; }

    public bool receivedPt;
    public delegate void PointHandler(Vertex v);
    public event PointHandler PtReceived;

    /// <summary>
    /// When a point arrives, triggers an event to send it to all neighbouring vertices.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void OnPtArrival(object sender, EventArgs e)
    {
        if (!receivedPt)
        {
            PtReceived?.Invoke(this);
            receivedPt = true;
        }
        else receivedPt = false;
    }

    public Vertex() : this(0, 0, 0) { }

    public Vertex(double x, double y, int num)
    {
        center_X = x;
        center_Y = y;
        Number = num;
    }

    /// <summary>
    /// Checks if point is within this vertex on canvas.
    /// </summary>
    /// <param name="x">coordinate X</param>
    /// <param name="y">coordinate Y</param>
    /// <returns></returns>
    public bool ContainsPoint(double x, double y)
    {
        double distance = Math.Sqrt(Math.Pow(x - center_X, 2) + Math.Pow(y - center_Y, 2));
        return distance <= rad;
    }

    /// <summary>
    /// Calculates distance between two vertices.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public double GetDistance(Vertex v)
    {
        return Math.Sqrt(Math.Pow(center_X - v.center_X, 2) + Math.Pow(center_Y - v.center_Y, 2));
    }
}
