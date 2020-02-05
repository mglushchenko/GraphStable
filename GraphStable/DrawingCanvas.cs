using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

/// <summary>
/// Extends the Canvas class to operate with DrawingVisual objects.
/// </summary>
public class DrawingCanvas : Canvas
{
    List<Visual> visuals = new List<Visual>();
    protected override int VisualChildrenCount => visuals.Count;

    protected override Visual GetVisualChild(int index)
    {
        return visuals[index];
    }

    public void AddVisual(Visual visual)
    {
        visuals.Add(visual);
        base.AddVisualChild(visual);
        base.AddLogicalChild(visual);
    }

    public void DeleteVisual(Visual visual)
    {
        visuals.Remove(visual);
        base.RemoveVisualChild(visual);
        base.RemoveLogicalChild(visual);
    }

    public void Clear()
    {
        while (visuals.Count != 0)
        {
            DeleteVisual(visuals[visuals.Count - 1]);
        }
    }

    /// <summary>
    /// Checks if point belongs to any visual child (performs a hit test).
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public DrawingVisual GetVisual(Point point)
    {
        HitTestResult result = VisualTreeHelper.HitTest(this, point);
        if (result == null) return null;
        return result.VisualHit as DrawingVisual;
    }
}
