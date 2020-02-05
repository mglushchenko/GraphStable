using System;
using System.Collections.Generic;

[Serializable]
public class Edge
{
    public Vertex v1, v2;
    public int Weight { get; set; }

    /// <summary>
    /// In modified graph: ID of the edge which was doubled and split to get the current one.
    /// </summary>
    public int parentEdgeNum;

    public Edge(Vertex v1, Vertex v2, int weight)
    {
        this.v1 = v1;
        this.v2 = v2;
        Weight = weight;
    }

    public Edge(Vertex v1, Vertex v2) : this(v1, v2, 1) { }

    public Edge() { }
}