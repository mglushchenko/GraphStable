using System;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SetEdgeWeightDialogBox;
using MatricesCharValuesForm;
using System.IO;
using Microsoft.Win32;
using System.Windows.Media.Animation;
using Wolfram.NETLink;
using System.Runtime.Serialization.Formatters.Binary;

namespace form_redesign
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        int numberOfVertices = 0;
        int stabilTime;

        DrawingCanvas drawingCanvas = new DrawingCanvas();
        DrawingCanvas modifiedCanvas = new DrawingCanvas();
        Graph graph;
        List<Label> weightLabels = new List<Label>();

        DispatcherTimer animationTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            inputGraph.Children.Add(drawingCanvas);
            modifiedGraph.Children.Add(modifiedCanvas);

            graph = new Graph();
            graph.StateVectorChanged += PrintVector;
            graph.StateVectorPChanged += PrintVectorP;
            graph.StateVectorSChanged += PrintVectorS;

            btnAnimate.IsEnabled = false;
            btnCalculate.IsEnabled = false;
            btnSave.IsEnabled = false;
            btnCharValues.IsEnabled = false;
            PlusMultBtn.IsChecked = true;

            animationTimer.Interval = TimeSpan.FromSeconds(1);
            animationTimer.Tick += DisplayAnimationTime;
        }

        /// <summary>
        /// Enables functions that be performed only if a graph is built.
        /// </summary>
        private void EnableButtons()
        {
            btnCalculate.IsEnabled = true;
            btnSave.IsEnabled = true;
        }

        /// <summary>
        /// If either 'vertex' or 'edge' drawing tool is selected, creates respective object.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void inputGraph_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (checkBoxVertex.IsChecked)
                CreateVertex(sender, e);
            if (checkBoxEdge.IsChecked)
                CreateEdge(sender, e);
        }

        /// <summary>
        /// Adds vertex to the graph and draws it on the canvas.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateVertex(object sender, MouseButtonEventArgs e)
        {
            Point current = e.GetPosition((UIElement)sender);
            double x = current.X;
            double y = current.Y;

            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                Vertex v = new Vertex(x, y, ++numberOfVertices);
                foreach (Vertex vertex in graph.vertices)
                    if (v.GetDistance(vertex) <= 2 * Vertex.rad)
                    {
                        MessageBox.Show("Don't put vertices on top of each other", "",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                SolidColorBrush brush = new SolidColorBrush(Colors.ForestGreen);
                SolidColorBrush contour = new SolidColorBrush(Colors.Black);
                Pen pen = new Pen(contour, 2);
                dc.DrawEllipse(brush, pen, current, 7, 7);

                graph.vertices.Add(v);
                if (numberOfVertices == 1) graph.starting = v;
            }
            drawingCanvas.AddVisual(visual);
        }

        bool firstVertexChosen = false;
        double x1, y1;

        /// <summary>
        /// If double click is performed on a vertex, makes this vertex the starting point 
        /// (after a confirmation from user).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void inputGraph_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                Point current = e.GetPosition((UIElement)sender);
                Vertex v = null;
                try
                {
                    v = graph.GetFromPoint(current.X, current.Y);
                    MessageBoxResult result = MessageBox.Show("Make this vertex the starting one?", "Confirm",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question);
                    if (result == MessageBoxResult.OK)
                    {
                        graph.starting = v;
                        graph.initialized = false;
                    }
                    firstVertexChosen = false;
                }
                catch (ArgumentException)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Connects the 2 vertices user has selected with an edge.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateEdge(object sender, MouseButtonEventArgs e)
        {
            Point current = e.GetPosition((UIElement)sender);

            if (!firstVertexChosen)
            {
                DrawingVisual v1 = drawingCanvas.GetVisual(current);
                if (v1 != null)
                {
                    x1 = current.X;
                    y1 = current.Y;
                    firstVertexChosen = true;
                }
                else return;
            }
            else
            {
                DrawingVisual v2 = drawingCanvas.GetVisual(current);
                if (v2 != null)
                {
                    double x2 = current.X;
                    double y2 = current.Y;
                    firstVertexChosen = false;

                    DrawingVisual visual = new DrawingVisual();
                    using (DrawingContext dc = visual.RenderOpen())
                    {
                        Vertex vertex1, vertex2;
                        try
                        {
                            vertex1 = graph.GetFromPoint(x1, y1);
                            vertex2 = graph.GetFromPoint(x2, y2);
                            if (vertex1 == vertex2)
                            {
                                MessageBox.Show("No loops allowed", "",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                        catch (ArgumentException)
                        {
                            MessageBox.Show("An edge must connect 2 vertices!", "",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        Point vert1 = new Point(vertex1.center_X, vertex1.center_Y);
                        Point vert2 = new Point(vertex2.center_X, vertex2.center_Y);

                        int weight = SetEdgeWeight();
                        Edge edge = new Edge(vertex1, vertex2, weight);
                        graph.edges.Add(edge);
                        graph.drawn = false;

                        SolidColorBrush contour = new SolidColorBrush(Colors.Black);
                        double thickness = 1.4;
                        if (graph.GetEdges(vertex1, vertex2).Count > 1)
                            thickness *= graph.GetEdges(vertex1, vertex2).Count;
                        Pen pen = new Pen(contour, thickness);
                        dc.DrawLine(pen, vert1, vert2);
                        CreateLabel(edge);

                    }
                    drawingCanvas.AddVisual(visual);
                    EnableButtons();
                }
                else
                {
                    firstVertexChosen = false;
                    return;
                }
            }
        }

        /// <summary>
        /// Adds a label indicating edge's weight to the canvas.
        /// </summary>
        /// <param name="edge">edge of the graph</param>
        private void CreateLabel(Edge edge)
        {
            double middle_x = (edge.v1.center_X + edge.v2.center_X) / 2;
            double middle_y = (edge.v1.center_Y + edge.v2.center_Y) / 2;
            Point middle = new Point(middle_x, middle_y);
            Label weightLabel = new Label();

            weightLabel.Content = edge.Weight;
            weightLabel.Height = weightLabel.Width = 30;
            inputGraph.Children.Add(weightLabel);
            weightLabels.Add(weightLabel);
            weightLabel.MouseDoubleClick += ChangeEdgeWeight;

            List<Edge> currentEdges = graph.GetEdges(edge.v1, edge.v2);
            int k = currentEdges.Count;
            double rad = 10;
            double[] x_coords = new double[k];
            double[] y_coords = new double[k];
            for (int i = 0; i < k; i++)
            {
                double cos = Math.Cos((i + 1) * 2 * Math.PI / k);
                double sin = Math.Sin((i + 1) * 2 * Math.PI / k);
                x_coords[i] = middle_x + rad * cos;
                y_coords[i] = middle_y + rad * sin;
            }

            for (int i = 0; i < currentEdges.IndexOf(edge) + 1; i++)
            {
                int j = graph.edges.IndexOf(currentEdges[i]);
                Canvas.SetLeft(weightLabels[j], x_coords[i] - 10);
                Canvas.SetTop(weightLabels[j], y_coords[i] - 10);
            }
        }

        /// <summary>
        /// If double click is performed on a weight label, user can change the weight of a respective edge.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeEdgeWeight(object sender, MouseButtonEventArgs e)
        {
            int weight = SetEdgeWeight();
            int index = weightLabels.IndexOf((Label)sender);
            graph.edges[index].Weight = weight;
            weightLabels[index].Content = weight;
            graph.initialized = false;
            graph.drawn = false;
        }

        /// <summary>
        /// Calls a dialog box for setting edge's weight.
        /// </summary>
        /// <returns></returns>
        private int SetEdgeWeight()
        {
            EdgeWeightDialogBox dialog = new EdgeWeightDialogBox();
            dialog.ShowDialog();
            if (dialog.result == true)
                return dialog.weight;
            else
                return 1;
        }

        /// <summary>
        /// Selects 'vertex' drawing tool.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxVertex_Click(object sender, RoutedEventArgs e)
        {
            checkBoxEdge.IsChecked = false;
        }

        /// <summary>
        /// Selects 'edge' drawing tool.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxEdge_Click(object sender, RoutedEventArgs e)
        {
            checkBoxVertex.IsChecked = false;
        }

        /// <summary>
        /// Clears all the currently displayed data, resets key values.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            drawingCanvas.Clear();
            modifiedCanvas.Clear();

            graph = new Graph();
            graph.StateVectorChanged += PrintVector;
            graph.StateVectorPChanged += PrintVectorP;
            graph.StateVectorSChanged += PrintVectorS;

            vectorsListBox.Items.Clear();
            vectorsPListBox.Items.Clear();
            vectorsSListBox.Items.Clear();

            foreach (Label label in weightLabels)
                inputGraph.Children.Remove(label);
            weightLabels.Clear();

            timeInfo.Content = "";
            timePInfo.Content = "";
            timeSInfo.Content = "";
            timeDataBlock.Text = "";

            inputGraph.Children.Clear();
            for (int i = 0; i < graph.vertices.Count; i++)
                graph.vertices[i].PtReceived -= FirePoints;
            inputGraph.Children.Add(drawingCanvas);
            inputGraph.Children.Add(hintBlock);

            modifiedGraph.Children.Clear();
            modifiedGraph.Children.Add(modifiedCanvas);

            power = 1;
            time = 0;

            btnSave.IsEnabled = false;
            btnCalculate.IsEnabled = false;
            btnAnimate.IsEnabled = false;
            btnCharValues.IsEnabled = false;
        }

        /// <summary>
        /// Calculate stabilization time of the graph using 3 different matrices,
        /// displays powers of state vectors.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            if (!graph.Connected())
            {
                MessageBox.Show($"Stabilization time can only be calculated for a connected graph", "Not connected",
                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (graph.edges.Count == 1)
            {
                MessageBox.Show("Initial graph must have more than one edge", "Not enough edges",
                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!graph.initialized)
                    graph.Initialize();

                if (graph.matrix.rows > 500)
                {
                    MessageBoxResult res = MessageBox.Show("Graph is too large. Calculations may never be completed. Proceed at your own risk?", "Not safe",
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                    if (res != MessageBoxResult.OK) return;
                }

                else if (graph.matrix.rows > 100)
                {
                    MessageBox.Show("Calculations may take a while, please be patient.", "Large graph matrix",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                vectorsListBox.Items.Clear();
                vectorsPListBox.Items.Clear();
                vectorsSListBox.Items.Clear();
                power = powerP = powerS = 1;

                FillListBoxes();

                bool plusMult = (bool)PlusMultBtn.IsChecked;

                int time = graph.CalculateStabilizationTime(plusMult, out double coeff);
                timeInfo.Content = $"time: {time}";
                if (!plusMult)
                    timeInfo.Content += $"\nproportionality coefficient: {coeff}";
                stabilTime = time;

                if (!plusMult)
                {
                    MessageBox.Show("Calculations in (MAX,+) are performed only on the binary matrix because the other two are non-integer",
                    "Integer matrices only", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                else
                {
                    int timeP = graph.CalculateStabilizationTimeP();
                    timePInfo.Content = $"time: {timeP}";

                    if (!graph.IsClean())
                    {
                        MessageBox.Show("This graph has at least one vertex with out-degree of 2. Calculations on matrix S won't be performed",
                        "Not a clean graph", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    else
                    {
                        int timeS = graph.CalculateStabilizationTimeS();
                        timeSInfo.Content = $"time: {timeS}";
                    }
                }

                if (!graph.drawn)
                {
                    modifiedCanvas.Clear();
                    modifiedGraph.Children.Clear();
                    modifiedGraph.Children.Add(modifiedCanvas);
                    DrawGraph(graph);
                }

                btnSave.IsEnabled = true;
                btnCharValues.IsEnabled = true;
                btnAnimate.IsEnabled = true;
                firstVertexChosen = false;

                graph.initialized = false;
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("Build a graph first!");
            }
        }

        /// <summary>
        /// Calculates characteristic values of matrices
        /// (using Wolfram Mathematica).
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        private Image GetEigenvalues(Matrix m)
        {
            MathKernel mk = new MathKernel();
            mk.ResultFormat = MathKernel.ResultFormatType.StandardForm;
            mk.Compute($"Tally[Round[Eigenvalues[{m.ToWMFormat()}],10^(-10)]]");

            Image image = new Image();
            image.Source = ToBitmapImage((System.Drawing.Bitmap)mk.Result);
            return image;
        }

        /// <summary>
        /// Converts Bitmap to BitmapImage to be used as an image source.
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        private BitmapImage ToBitmapImage(System.Drawing.Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        /// <summary>
        /// Displays matrices and initial state vectors.
        /// </summary>
        private void FillListBoxes()
        {
            vectorsListBox.Items.Add("calculations on binary matrix:\n");
            vectorsListBox.Items.Add("graph matrix:\n");
            vectorsListBox.Items.Add(graph.matrix + "\n");
            vectorsListBox.Items.Add($"v^1={graph.stateVector}");

            bool plusMult = (bool)PlusMultBtn.IsChecked;
            if (plusMult)
            {
                vectorsPListBox.Items.Add("calculations on matrix P:\n");
                vectorsPListBox.Items.Add("matrix P:\n");
                vectorsPListBox.Items.Add(graph.matrixP + "\n");
                vectorsPListBox.Items.Add($"v^1={graph.stateVectorP}");

                if (graph.IsClean())
                {
                    vectorsSListBox.Items.Add("calculations on matrix S:\n");
                    vectorsSListBox.Items.Add("matrix S:\n");
                    vectorsSListBox.Items.Add(graph.matrixS + "\n");
                    vectorsSListBox.Items.Add($"v^1={graph.stateVectorS}");
                }
            }
        }

        /// <summary>
        /// Saves current graph to a ".graph" file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!graph.initialized)
                graph.Initialize();

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.DefaultExt = ".graph";
            dialog.AddExtension = true;
            dialog.Filter = "Graph file (*.graph)|*.graph";
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    graph.SaveToFile(dialog.FileName);
                }
                catch (System.Runtime.Serialization.SerializationException)
                {
                    MessageBox.Show("Failed to save file", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
                MessageBox.Show("File saved successfully!", "Done",
                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Opens a previously created ".graph" file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Graph file (*.graph)|*.graph";
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string[] lines = File.ReadAllLines(dialog.FileName);
                    btnClear_Click(this, e);

                    BinaryFormatter bin = new BinaryFormatter();
                    FileStream stream = new FileStream(dialog.FileName, FileMode.Open);
                    try
                    {
                        graph = (Graph)bin.Deserialize(stream);
                    }
                    catch (System.Runtime.Serialization.SerializationException)
                    {
                        MessageBox.Show("Incorrect input data!", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    catch (System.Security.SecurityException)
                    {
                        MessageBox.Show("You don't have access to this file!", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    btnAnimate.IsEnabled = false;
                    btnCharValues.IsEnabled = false;

                    graph.StateVectorChanged += PrintVector;
                    graph.StateVectorPChanged += PrintVectorP;
                    graph.StateVectorSChanged += PrintVectorS;

                    DrawGraph(graph);
                    DrawFromFile(graph);
                    EnableButtons();
                    MessageBox.Show("File opened", "Done",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (IOException)
                {
                    MessageBox.Show("Failed to open file", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private int power = 1, powerP = 1, powerS = 1;

        /// <summary>
        /// Displays powers of the state vector.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintVector(object sender, EventArgs e)
        {
            vectorsListBox.Items.Add($"v^{++power} = {graph.stateVector}");
        }

        /// <summary>
        /// Displays powers of the state vector P.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintVectorP(object sender, EventArgs e)
        {
            vectorsPListBox.Items.Add($"v^{++powerP}={graph.stateVectorP}");
        }

        /// <summary>
        /// Displays powers of the state vector S.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PrintVectorS(object sender, EventArgs e)
        {
            vectorsSListBox.Items.Add($"v^{++powerS}={graph.stateVectorS}");
        }

        /// <summary>
        /// Launches the animation process.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAnimate_Click(object sender, RoutedEventArgs e)
        {
            if (!graph.Connected())
            {
                MessageBox.Show($"Animation can only be run on a connected graph", "Not connected",
                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (graph.edges.Count == 1)
            {
                MessageBox.Show("Initial graph must have more than one edge", "Not enough edges",
                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            timeDataBlock.Text = "time: 0";

            if (!graph.initialized)
                graph.Initialize();

            animationTimer.Start();
            FirePoints(graph.starting);
            for (int i = 0; i < graph.vertices.Count; i++)
                graph.vertices[i].PtReceived += FirePoints;
        }

        private int time = 0;

        /// <summary>
        /// Displays animation timer, checks if stabilization time is reached.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisplayAnimationTime(object sender, EventArgs e)
        {
            time++;
            timeDataBlock.Text = $"time: {time / (11 - speedBar.Value):f3}";
            if (time == stabilTime * (11 - speedBar.Value))
                AnimationStabil();
        }

        /// <summary>
        /// Stops animation, resets timer.
        /// </summary>
        private void AnimationStabil()
        {
            for (int i = 0; i < graph.vertices.Count; i++)
            {
                graph.vertices[i].PtReceived -= FirePoints;
                graph.vertices[i].receivedPt = false;
            }
            animationTimer.Stop();
            animationTimer = new DispatcherTimer();
            animationTimer.Interval = TimeSpan.FromSeconds(1);
            animationTimer.Tick += DisplayAnimationTime;
            time = 0;
        }

        /// <summary>
        /// Sends packets (shown as animated points) to all neighbours of vertex v.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private void FirePoints(Vertex v)
        {
            List<Vertex> neighbours = graph.GetNeighbours(v);
            v.receivedPt = true;

            for (int i = 0; i < neighbours.Count; i++)
            {
                List<Edge> adjEdges = graph.GetEdges(v, neighbours[i]);
                foreach (Edge edge in adjEdges)
                {
                    Ellipse ellipse = new Ellipse();
                    SolidColorBrush brush = new SolidColorBrush(Colors.Red);
                    ellipse.Fill = brush;
                    ellipse.Height = ellipse.Width = 10;
                    Point p1 = new Point(v.center_X - ellipse.Width / 2,
                    v.center_Y - ellipse.Height / 2);
                    Canvas.SetTop(ellipse, p1.Y);
                    Canvas.SetLeft(ellipse, p1.X);
                    inputGraph.Children.Add(ellipse);

                    Point p2 = new Point(neighbours[i].center_X - ellipse.Width / 2,
                    neighbours[i].center_Y - ellipse.Height / 2);

                    DoubleAnimation animationX = new DoubleAnimation();
                    DoubleAnimation animationY = new DoubleAnimation();

                    animationX.Completed += neighbours[i].OnPtArrival;
                    animationY.Completed += neighbours[i].OnPtArrival;

                    DispatcherTimer dt = new DispatcherTimer();
                    dt.Interval = TimeSpan.FromSeconds((11 - speedBar.Value) * edge.Weight);
                    dt.Tag = ellipse;
                    dt.Tick += DeletePoint;

                    animationX.From = p1.X; animationX.To = p2.X;
                    animationY.From = p1.Y; animationY.To = p2.Y;
                    animationX.Duration = TimeSpan.FromSeconds((11 - speedBar.Value) * edge.Weight);
                    animationY.Duration = TimeSpan.FromSeconds((11 - speedBar.Value) * edge.Weight);

                    dt.Start();
                    ellipse.BeginAnimation(Canvas.TopProperty, animationY);
                    ellipse.BeginAnimation(Canvas.LeftProperty, animationX);
                }

            }
        }

        /// <summary>
        /// Removes the animated point when it reaches its destination.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeletePoint(object sender, EventArgs e)
        {
            DispatcherTimer dt = (DispatcherTimer)sender;
            UIElement ellipse = (UIElement)dt.Tag;
            inputGraph.Children.Remove(ellipse);
            ellipse = null;
            dt.Stop();
            dt = null;
        }

        /// <summary>
        /// Displays characteristic values of matrices.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCharValues_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("This proccess may take some time (to launch Wolfram Mathematica). Proceed?", "Confirm",
            MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result != MessageBoxResult.OK) return;

            MatricesCharValues charValues = new MatricesCharValues();
            Image values = GetEigenvalues(graph.matrix);
            charValues.binCanvas.Children.Add(values);

            values = GetEigenvalues(graph.matrixP);
            charValues.canvasP.Children.Add(values);

            if (graph.IsClean())
            {
                values = GetEigenvalues(graph.matrixS);
                charValues.canvasS.Children.Add(values);
            }

            charValues.Show();
        }

        /// <summary>
        /// Draws graph with doubled and split edges.
        /// </summary>
        /// <param name="graph"></param>
        private void DrawGraph(Graph graph)
        {
            double x0 = modifiedGraph.ActualWidth / 2;
            double y0 = modifiedGraph.ActualHeight / 2;
            double rad = 100;
            int n = graph.splitVertices.Count;
            graph.drawn = true;

            double[] x_coords = new double[n];
            double[] y_coords = new double[n];
            for (int i = 0; i < n; i++)
            {
                double cos = Math.Cos((i + 1) * 2 * Math.PI / n);
                double sin = Math.Sin((i + 1) * 2 * Math.PI / n);
                x_coords[i] = x0 + rad * cos;
                y_coords[i] = y0 + rad * sin;
                Point current = new Point(x_coords[i], y_coords[i]);
                DrawingVisual visual = new DrawingVisual();
                using (DrawingContext dc = visual.RenderOpen())
                {
                    SolidColorBrush brush = new SolidColorBrush(Colors.ForestGreen);
                    SolidColorBrush contour = new SolidColorBrush(Colors.Black);
                    Pen pen = new Pen(contour, 2);
                    dc.DrawEllipse(brush, pen, current, 7, 7);
                }
                modifiedCanvas.AddVisual(visual);
            }

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    if (graph.AreNeighboursInSplit(i, j))
                    {
                        Point p1 = new Point(x_coords[i], y_coords[i]);
                        Point p2 = new Point(x_coords[j], y_coords[j]);
                        List<Edge> edges = graph.GetEdgesInSplit(graph.splitVertices[i], graph.splitVertices[j]);
                        if (edges.Count == 1)
                        {
                            DrawingVisual visual = new DrawingVisual();
                            using (DrawingContext dc = visual.RenderOpen())
                            {
                                SolidColorBrush contour = new SolidColorBrush(Colors.Black);
                                Pen pen = new Pen(contour, 2);

                                dc.DrawLine(pen, p1, p2);
                            }
                            modifiedCanvas.AddVisual(visual);
                            DrawArrow(p1, p2);
                        }
                        else
                        {
                            ArcSegment as1 = new ArcSegment();
                            as1.Point = p2;
                            as1.Size = new Size(200, 300);
                            as1.SweepDirection = SweepDirection.Clockwise;
                            as1.IsLargeArc = false;

                            PathFigure figure1 = new PathFigure();
                            figure1.StartPoint = p1;
                            figure1.Segments.Add(as1);

                            PathGeometry geometry1 = new PathGeometry();
                            geometry1.Figures.Add(figure1);

                            System.Windows.Shapes.Path path1 = new System.Windows.Shapes.Path();
                            path1.Data = geometry1;
                            path1.Stroke = new SolidColorBrush(Colors.Black);
                            path1.StrokeThickness = 1;

                            modifiedGraph.Children.Add(path1);

                            ArcSegment as2 = new ArcSegment();
                            as2.Point = p2;
                            as2.Size = new Size(200, 300);
                            as2.SweepDirection = SweepDirection.Counterclockwise;
                            as2.IsLargeArc = false;

                            PathFigure figure2 = new PathFigure();
                            figure2.StartPoint = p1;
                            figure2.Segments.Add(as2);

                            PathGeometry geometry2 = new PathGeometry();
                            geometry1.Figures.Add(figure2);

                            System.Windows.Shapes.Path path2 = new System.Windows.Shapes.Path();
                            path2.Data = geometry2;
                            path2.Stroke = new SolidColorBrush(Colors.Black);
                            path2.StrokeThickness = 1;

                            modifiedGraph.Children.Add(path2);
                        }
                    }
        }

        /// <summary>
        /// Draws an arrow on an edge.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        private void DrawArrow(Point p1, Point p2)
        {
            double x1 = p1.X, x2 = p2.X, y1 = p1.Y, y2 = p2.Y;

            if (Math.Abs(x2 - x1) < 0.1)
            {
                DrawVerticalArrow(p1, p2);
                return;
            }

            if (Math.Abs(y2 - y1) < 0.1)
            {
                DrawHorizontalArrow(p1, p2);
                return;
            }

            double k = (y2 - y1) / (x2 - x1);

            double b = y2 - k * x2;

            double[] p0_x = GetCoordFromLineAndDistance(k, b, p2, 10);
            double x0 = p0_x[0];
            double y0 = k * x0 + b;
            double len0 = Math.Sqrt(Math.Pow(x1 - x0, 2) + Math.Pow(y1 - y0, 2));
            double len2 = Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
            if (len0 > len2)
            {
                x0 = p0_x[1];
                y0 = k * x0 + b;
            }

            Point p0 = new Point(x0, y0);
            double k1 = -1 / k;
            double b1 = y0 - k1 * x0;

            double[] xcoords = GetCoordFromLineAndDistance(k1, b1, p0, 3);
            double ax = xcoords[0], cx = xcoords[1];

            double ay = k1 * ax + b1, cy = k1 * cx + b1;
            Point a = new Point(ax, ay);
            Point c = new Point(cx, cy);
            PointCollection points = new PointCollection();
            points.Add(p2); points.Add(a); points.Add(c);

            Polygon arrow = new Polygon();
            arrow.Stroke = new SolidColorBrush(Colors.Black);
            arrow.StrokeThickness = 3;
            arrow.Fill = new SolidColorBrush(Colors.Black);
            arrow.Points = points;
            modifiedGraph.Children.Add(arrow);
        }

        /// <summary>
        /// Draws an arrow on a vertical edge.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        private void DrawVerticalArrow(Point p1, Point p2)
        {
            double x1 = p1.X, y1 = p1.Y, x2 = p2.X, y2 = p2.Y;
            double y0;
            if (y2 > y1)
                y0 = y2 - 10;
            else
                y0 = y2 + 10;
            Point a = new Point(x1 + 3, y0);
            Point c = new Point(x1 - 3, y0);
            PointCollection points = new PointCollection();
            points.Add(p2); points.Add(a); points.Add(c);

            Polygon arrow = new Polygon();
            arrow.Stroke = new SolidColorBrush(Colors.Black);
            arrow.StrokeThickness = 3;
            arrow.Fill = new SolidColorBrush(Colors.Black);
            arrow.Points = points;
            modifiedGraph.Children.Add(arrow);
        }

        /// <summary>
        /// Displays help window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Help.HelpWindow help = new Help.HelpWindow();
            help.Show();
        }

        /// <summary>
        /// Calls help window if F1 key is pressed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
                btnHelp_Click(sender, new RoutedEventArgs());
        }

        /// <summary>
        /// Draws an arrow on a horizontal edge.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        private void DrawHorizontalArrow(Point p1, Point p2)
        {
            double x1 = p1.X, x2 = p2.X, y = p2.Y;

            double x0;
            if (x2 > x1)
                x0 = x2 - 10;
            else
                x0 = x2 + 10;

            Point a = new Point(x0, y + 3);
            Point c = new Point(x0, y - 3);
            PointCollection points = new PointCollection();
            points.Add(p2); points.Add(a); points.Add(c);

            Polygon arrow = new Polygon();
            arrow.Stroke = new SolidColorBrush(Colors.Black);
            arrow.StrokeThickness = 3;
            arrow.Fill = new SolidColorBrush(Colors.Black);
            arrow.Points = points;
            modifiedGraph.Children.Add(arrow);
        }

        private double[] GetCoordFromLineAndDistance(double k, double b, Point p0, double distance)
        {
            double p = Math.Pow(k, 2) + 1;
            double q = 2 * (-p0.X + k * b - k * p0.Y);
            double r = Math.Pow(p0.X, 2) + Math.Pow(b, 2) - 2 * b * p0.Y + Math.Pow(p0.Y, 2) - Math.Pow(distance, 2);

            double D = Math.Pow(q, 2) - 4 * p * r;
            double x1 = (-q + Math.Sqrt(D)) / (2 * p);
            double x2 = (-q - Math.Sqrt(D)) / (2 * p);
            return new double[] { x1, x2 };
        }

        /// <summary>
        /// Draws graph uploaded from file.
        /// </summary>
        /// <param name="graph"></param>
        private void DrawFromFile(Graph graph)
        {
            foreach (Vertex vertex in graph.vertices)
            {
                Point center = new Point(vertex.center_X, vertex.center_Y);
                DrawingVisual visual = new DrawingVisual();
                using (DrawingContext dc = visual.RenderOpen())
                {
                    SolidColorBrush brush = new SolidColorBrush(Colors.ForestGreen);
                    SolidColorBrush contour = new SolidColorBrush(Colors.Black);
                    Pen pen = new Pen(contour, 2);
                    dc.DrawEllipse(brush, pen, center, 7, 7);
                }
                drawingCanvas.AddVisual(visual);
            }

            foreach (Edge edge in graph.edges)
            {
                Point v1 = new Point(edge.v1.center_X, edge.v1.center_Y);
                Point v2 = new Point(edge.v2.center_X, edge.v2.center_Y);
                DrawingVisual visual = new DrawingVisual();
                using (DrawingContext dc = visual.RenderOpen())
                {
                    SolidColorBrush contour = new SolidColorBrush(Colors.Black);
                    Pen pen = new Pen(contour, 2);

                    dc.DrawLine(pen, v1, v2);
                }
                drawingCanvas.AddVisual(visual);
                CreateLabel(edge);
            }
        }
    }
}
