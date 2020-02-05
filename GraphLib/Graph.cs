using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
public class Graph
{
    public List<Vertex> vertices = new List<Vertex>();
    public List<Edge> edges = new List<Edge>();
    public List<Edge> splitEdges = new List<Edge>();
    public List<Vertex> splitVertices = new List<Vertex>();

    public Matrix matrix;
    public Matrix matrixP;
    public Matrix matrixS;

    public bool initialized = false;
    public bool drawn = false;
    public bool fromFile = false;

    public Vector stateVector;
    public Vector stateVectorP;
    public Vector stateVectorS;

    public Vertex starting;

    public event EventHandler StateVectorChanged;
    public event EventHandler StateVectorPChanged;
    public event EventHandler StateVectorSChanged;

    /// <summary>
    /// Forms graph matrix (binary).
    /// </summary>
    public void FormMatrix()
    {
        int size = 0;
        foreach (Edge edge in edges)
            size += edge.Weight;
        size *= 2;
        matrix = new Matrix(size);
        int k = edges.Count;

        for (int i = 0; i < k; i++)
        {
            List<Edge> split = DoubleAndSplit(edges[i]);
            for (int j = 0; j < split.Count; j++)
                split[j].parentEdgeNum = i;
            splitEdges.AddRange(split);
        }

        for (int i = 0; i < splitEdges.Count; i++)
            for (int j = 0; j < splitEdges.Count; j++)
                if (splitEdges[i].v2 == splitEdges[j].v1)
                    matrix[i, j] = 1;
    }

    /// <summary>
    /// Forms matrix P.
    /// </summary>
    public void FormMatrixP()
    {
        int n = matrix.rows;
        matrixP = new Matrix(n);
        for (int i = 0; i < n; i++)
        {
            double rowSum = matrix.GetRowSum(i);
            for (int j = 0; j < n; j++)
                matrixP[i, j] = matrix[i, j] / rowSum;
        }
    }

    /// <summary>
    /// Forms state vector P.
    /// </summary>
    public void FormStateVectorP()
    {
        stateVectorP = new Vector(stateVector.Size);
        double sum = stateVector.GetSum();
        for (int i = 0; i < stateVector.Size; i++)
            stateVectorP[i] = stateVector[i] / sum;
    }

    /// <summary>
    /// Forms scattering matrix.
    /// </summary>
    public void FormMatrixS()
    {
        int n = matrix.rows;
        matrixS = new Matrix(n);
        for (int i = 0; i < n; i++)
        {
            double rowSum = matrix.GetRowSum(i);

            if (rowSum == 1)
            {
                for (int j = 0; j < n; j++)
                    matrixS[i, j] = matrix[i, j];
            }
            else
            {
                for (int j = 0; j < n; j++)
                {
                    if (matrix[i, j] != 0)
                    {
                        if (splitEdges[i].parentEdgeNum == splitEdges[j].parentEdgeNum && i != j
                        && splitEdges[i].v1 == splitEdges[j].v2)
                            matrixS[i, j] = 1 - 2 * matrix[i, j] / rowSum;
                        else matrixS[i, j] = 2 * matrix[i, j] / rowSum;
                    }
                    else matrixS[i, j] = 0;
                }
            }
        }
    }

    /// <summary>
    /// Forms state vector S.
    /// </summary>
    public void FormStateVectorS()
    {
        stateVectorS = new Vector(stateVector);
    }

    /// <summary>
    /// Splits an arc into arcs of length=1.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    public List<Edge> Split(Edge edge)
    {
        List<Edge> split = new List<Edge>();
        if (edge.Weight > 1)
        {
            Vertex temp = new Vertex();
            Edge e = new Edge(temp, edge.v2, edge.Weight - 1);
            Edge e1 = new Edge(edge.v1, temp, 1);
            if (!splitVertices.Contains(edge.v1))
                splitVertices.Add(edge.v1);
            split.Add(e1);
            split.AddRange(Split(e));
        }
        else
        {
            split.Add(edge);
            if (!splitVertices.Contains(edge.v1))
                splitVertices.Add(edge.v1);
            if (!splitVertices.Contains(edge.v2))
                splitVertices.Add(edge.v2);
        }
        return split;
    }

    /// <summary>
    /// Splits an edge into two arcs, splits each arc.
    /// </summary>
    /// <param name="edge"></param>
    /// <returns></returns>
    public List<Edge> DoubleAndSplit(Edge edge)
    {
        Edge e1 = edge;
        Edge e2 = new Edge(edge.v2, edge.v1, edge.Weight);
        List<Edge> split1 = Split(e1);
        List<Edge> split2 = Split(e2);
        List<Edge> res = new List<Edge>(split1);
        res.AddRange(split2);
        return res;
    }

    /// <summary>
    /// Returns maximum edge weight.
    /// </summary>
    /// <returns></returns>
    public int GetMaxEdgeWeight()
    {
        int max = 1;
        foreach (Edge edge in edges)
            if (edge.Weight > max)
                max = edge.Weight;
        return max;
    }

    /// <summary>
    /// Forms state vector for chosen starting vertex v.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="size"></param>
    public void FormStateVector(Vertex v, int size)
    {
        stateVector = new Vector(size);
        for (int i = 0; i < splitEdges.Count; i++)
            if (splitEdges[i].v1 == v)
                stateVector[i] = 1;
    }

    /// <summary>
    /// Checks if two vertices are connected by an edge.
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    /// <returns></returns>
    public bool AreNeighbours(int i, int j)
    {
        foreach (Edge edge in edges)
            if (edge.v1 == vertices[i] && edge.v2 == vertices[j] ||
            edge.v1 == vertices[j] && edge.v2 == vertices[i])
                return true;
        return false;
    }

    /// <summary>
    /// Finds all edges connecting two vertices.
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <returns></returns>
    public List<Edge> GetEdges(Vertex v1, Vertex v2)
    {
        List<Edge> adjEdges = new List<Edge>();
        foreach (Edge edge in edges)
            if (edge.v1 == v1 && edge.v2 == v2 ||
            edge.v1 == v2 && edge.v2 == v1)
                adjEdges.Add(edge);
        return adjEdges;
    }

    /// <summary>
    /// Finds all arcs connecting two vertices in modified graph.
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <returns></returns>
    public List<Edge> GetEdgesInSplit(Vertex v1, Vertex v2)
    {
        List<Edge> adjEdges = new List<Edge>();
        foreach (Edge edge in splitEdges)
            if (edge.v1 == v1 && edge.v2 == v2 ||
            edge.v1 == v2 && edge.v2 == v1)
                adjEdges.Add(edge);
        return adjEdges;
    }

    /// <summary>
    /// Finds all neighbours of vertex v.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public List<Vertex> GetNeighbours(Vertex v)
    {
        List<Vertex> neighbours = new List<Vertex>();
        for (int i = 0; i < vertices.Count; i++)
        {
            if (AreNeighbours(vertices.IndexOf(v), i))
                neighbours.Add(vertices[i]);
        }
        return neighbours;
    }

    /// <summary>
    /// Checks if two vertices are connected by an arc in modified graph.
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    /// <returns></returns>
    public bool AreNeighboursInSplit(int i, int j)
    {
        foreach (Edge edge in splitEdges)
            if (edge.v1 == splitVertices[i] && edge.v2 == splitVertices[j])
                return true;
        return false;
    }

    /// <summary>
    /// Checks if point on the canvas belongs to any of the graph's vertices.
    /// </summary>
    /// <param name="x">coordinate X</param>
    /// <param name="y">coordinate Y</param>
    /// <returns></returns>
    public Vertex GetFromPoint(double x, double y)
    {
        foreach (Vertex vertex in vertices)
            if (vertex.ContainsPoint(x, y))
                return vertex;
        throw new ArgumentException("point doesn't belong to any vertex");
    }

    /// <summary>
    /// Calculates graph stabilization time.
    /// </summary>
    /// <param name="plusMult">ring over which calculations are performed</param>
    /// <returns></returns>
    public int CalculateStabilizationTime(bool plusMult, out double c)
    {
        if (!initialized)
            Initialize();
        int count = 1;
        do
        {
            if (plusMult)
                stateVector = matrix.Multiply(stateVector);
            else stateVector = matrix.MultiplyMaxPlus(stateVector);

            StateVectorChanged?.Invoke(this, EventArgs.Empty);
            count++;
        } while (!Stabilized(plusMult, out c));
        return count;
    }

    /// <summary>
    /// Calculates graph stabilization time (using matrix P).
    /// </summary>
    /// <param name="plusMult">ring over which calculations are performed</param>
    /// <returns></returns>
    public int CalculateStabilizationTimeP()
    {
        int count = 1;
        do
        {
            stateVectorP = matrixP.Multiply(stateVectorP);
            StateVectorPChanged?.Invoke(this, EventArgs.Empty);
            count++;
        } while (!StabilizedP());
        return count;
    }

    /// <summary>
    /// Calculates graph stabilization time (using matrix S).
    /// </summary>
    /// <param name="plusMult">ring over which calculations are performed</param>
    /// <returns></returns>
    public int CalculateStabilizationTimeS()
    {
        int count = 1;
        do
        {
            stateVectorS = matrixS.Multiply(stateVectorS);
            StateVectorSChanged?.Invoke(this, EventArgs.Empty);
            count++;
        } while (!StabilizedS());
        return count;
    }

    /// <summary>
    /// Initializes matrices and state vectors.
    /// </summary>
    public void Initialize()
    {
        if (!fromFile)
        {
            splitEdges = new List<Edge>();
            splitVertices = new List<Vertex>();
            FormMatrix();
        }
        if (starting == null)
            starting = vertices[0];
        FormStateVector(starting, matrix.rows);

        FormMatrixP();
        FormStateVectorP();

        FormMatrixS();
        FormStateVectorS();

        initialized = true;
    }

    /// <summary>
    /// Checks if number of points has stabilized.
    /// </summary>
    /// <param name="plusMult">ring over which calculations are performed</param>
    /// <returns></returns>
    private bool Stabilized(bool plusMult, out double c)
    {
        Vector temp = stateVector;
        c = 0;

        if (!plusMult)
        {
            for (int i = 0; i < 2; i++)
                temp = matrix.MultiplyMaxPlus(temp);
            return temp.IsProportionalTo(stateVector, out c);
        }

        for (int i = 0; i < GetMaxEdgeWeight() * 2; i++)
        {
            temp = matrix.Multiply(temp);
        }
        return temp.NumberOfPoints() == stateVector.NumberOfPoints();
    }

    /// <summary>
    /// Checks if number of points has stabilized (for state vector P).
    /// </summary>
    /// <param name="plusMult">ring over which calculations are performed</param>
    /// <returns></returns>
    private bool StabilizedP()
    {
        Vector temp = stateVectorP;
        for (int i = 0; i < GetMaxEdgeWeight() * 2; i++)
        {
            temp = matrixP.Multiply(temp);
        }
        return temp.NumberOfPoints() == stateVectorP.NumberOfPoints();
    }

    /// <summary>
    /// Checks if number of points has stabilized (for state vector S).
    /// </summary>
    /// <param name="plusMult">ring over which calculations are performed</param>
    /// <returns></returns>
    private bool StabilizedS()
    {
        Vector temp = stateVectorS;
        for (int i = 0; i < GetMaxEdgeWeight() * 2; i++)
        {
            temp = matrixS.Multiply(temp);
        }
        return temp.NumberOfPoints() == stateVectorS.NumberOfPoints();
    }

    /// <summary>
    /// Checks if graph is connected using depth-first search.
    /// </summary>
    /// <returns></returns>
    public bool Connected()
    {
        List<Vertex> visited = DFS(0, new List<Vertex>());
        return (visited.Count == vertices.Count);
    }

    /// <summary>
    /// Checks if graph has vertices with out-degree=2.
    /// </summary>
    /// <returns></returns>
    public bool IsClean()
    {
        for (int i = 0; i < matrix.rows; i++)
            if (matrix.GetRowSum(i) == 2)
                return false;
        return true;
    }

    /// <summary>
    /// Performs a depth-first search to check for connectivity.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="visited"></param>
    /// <returns></returns>
    private List<Vertex> DFS(int index, List<Vertex> visited)
    {
        visited.Add(vertices[index]);
        for (int i = 0; i < vertices.Count; i++)
            if (i != index && AreNeighbours(i, index) && !visited.Contains(vertices[i]))
                DFS(i, visited);
        return visited;
    }

    /// <summary>
    /// Saves graph to file using binary serialization.
    /// </summary>
    /// <param name="filename"></param>
    public void SaveToFile(string filename)
    {
        StateVectorChanged = null; StateVectorPChanged = null; StateVectorSChanged = null;

        BinaryFormatter bin = new BinaryFormatter();
        FileStream stream = new FileStream(filename, FileMode.Create);
        bin.Serialize(stream, this);
    }
}