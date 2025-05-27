#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return
using System.Linq;

namespace DataVisualization.Charting
{
    // Stub for test compilation
    public static class ChartingHelper
    {
        public static void RenderChart() { }
    }

    public class Chart
    {
        public SeriesCollection Series { get; } = new SeriesCollection();
    }

    public class SeriesCollection : System.Collections.ObjectModel.Collection<Series>
    {
        public Series this[string name] => this.FirstOrDefault(s => s.Name == name);
    }

    public class Series
    {
        public string Name { get; set; }
        public PointsCollection Points { get; } = new PointsCollection();
        public Series() { }
        public Series(string name) { Name = name; }
    }

    public class PointsCollection : System.Collections.ObjectModel.Collection<DataPoint>
    {
    }

    public class DataPoint
    {
        public double XValue { get; set; }
        public double YValues { get; set; }
        public DataPoint() { }
        public DataPoint(double x, double y) { XValue = x; YValues = y; }
    }
}
