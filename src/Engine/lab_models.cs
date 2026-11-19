using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhysicsLab
{
    internal sealed class Group
    {
        public readonly string Title, Hint;
        public readonly string[] Columns;
        public readonly string[][] Initial;
        public readonly bool FixedRows;

        public Group(string title, string hint, string columns, int rows, bool fixedRows = false)
        {
            Title = title;
            Hint = hint;
            Columns = columns.Split('|');
            Initial = Enumerable.Range(0, rows).Select(i => new string[Columns.Length]).ToArray();
            FixedRows = fixedRows;
        }

        public Group Seed(int column, params string[] values)
        {
            for (int i = 0; i < values.Length; i++) Initial[i][column] = values[i];
            return this;
        }
    }

    internal sealed class Option
    {
        public readonly string Label, Initial;
        public Option(string label, string initial = "") { Label = label; Initial = initial; }
    }

    internal sealed class Experiment
    {
        public readonly string Name, Pages, Intro;
        public readonly Option[] Options;
        public readonly Group[] Groups;
        public readonly Action<Input, Report> Calculate;
        public Experiment(string name, string pages, string intro, Option[] options, Action<Input, Report> calculate, params Group[] groups)
        {
            Name = name; Pages = pages; Intro = intro; Options = options; Groups = groups; Calculate = calculate;
        }
    }

    internal sealed class Datum
    {
        public readonly int Number;
        public readonly double?[] Values;
        public Datum(int number, double?[] values) { Number = number; Values = values; }
        public double this[int i] { get { return Values[i].Value; } }
    }

    internal sealed class Input
    {
        private readonly Experiment experiment;
        private readonly string[] options;
        private readonly List<string[][]> measurements;
        private readonly Report report;

        public Input(Experiment experiment, string[] options, List<string[][]> measurements, Report report)
        {
            this.experiment = experiment; this.options = options; this.measurements = measurements; this.report = report;
        }

        public static bool TryNumber(string text, out double number)
        {
            text = (text ?? "").Trim().Replace('−', '-').Replace(',', '.');
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number) &&
                !double.IsNaN(number) && !double.IsInfinity(number) && Math.Abs(number) <= 1e12 &&
                (number == 0 || Math.Abs(number) >= 1e-12);
        }

        public double? Setting(int index, Func<double, bool> condition, bool required = false)
        {
            string text = options[index];
            if (!required && string.IsNullOrWhiteSpace(text)) return null;
            double value;
            if (TryNumber(text, out value) && condition(value)) return value;
            report.Warn(experiment.Options[index].Label + " için geçerli bir değer gir.");
            return null;
        }

        public IEnumerable<Datum> Rows(int groupIndex, int[] required, int[] positive, params int[] nonNegative)
        {
            Group group = experiment.Groups[groupIndex];
            var rows = measurements[groupIndex];
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (!row.Any(cell => !string.IsNullOrWhiteSpace(cell))) continue;
                if (i < group.Initial.Length && row.Select((cell, col) =>
                    (cell ?? "").Trim() == (group.Initial[i][col] ?? "")).All(same => same)) continue;
                var numbers = new double?[row.Length];
                var errors = new List<string>();
                for (int col = 0; col < row.Length; col++)
                {
                    bool blank = string.IsNullOrWhiteSpace(row[col]);
                    if (blank && !required.Contains(col)) continue;
                    double value;
                    if (blank || !TryNumber(row[col], out value) || (positive.Contains(col) && value <= 0) ||
                        (nonNegative.Contains(col) && value < 0))
                        errors.Add(group.Columns[col]);
                    else numbers[col] = value;
                }
                if (errors.Count != 0)
                    report.Warn(string.Format("{0}, satır {1} · Kontrol et: {2}.", group.Title, i + 1, string.Join(", ", errors)));
                else yield return new Datum(i + 1, numbers);
            }
        }
    }

    internal sealed class ResultTable
    {
        public readonly string Title;
        public readonly string[] Headers;
        public readonly List<string[]> Rows = new List<string[]>();
        public ResultTable(string title, string headers) { Title = title; Headers = headers.Split('|'); }
        public void Add(params object[] cells) { Rows.Add(cells.Select(Format.Value).ToArray()); }
    }

    internal static class Format
    {
        public static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
        public static string Number(double x) { return x.ToString("G6", Turkish); }
        public static string Value(object x)
        {
            if (x == null) return "—";
            if (x is double) return Number((double)x);
            return Convert.ToString(x, Turkish);
        }

        public static string WithUncertainty(double value, double uncertainty)
        {
            if (uncertainty <= 0) return Number(value) + " ± 0";
            int places = -(int)Math.Floor(Math.Log10(uncertainty));
            var scale = Math.Pow(10, places);
            if (uncertainty * scale < 2) { places++; scale *= 10; }
            var rounded = Math.Round(uncertainty * scale, MidpointRounding.AwayFromZero) / scale;
            if (rounded * scale >= 10 && uncertainty * scale < 10) { places--; scale /= 10; }
            var result = Math.Round(value * scale, MidpointRounding.AwayFromZero) / scale;
            if (places > 10) return Number(result) + " ± " + Number(rounded);
            var style = "F" + Math.Max(0, places);
            return result.ToString(style, Turkish) + " ± " + rounded.ToString(style, Turkish);
        }
    }

    internal struct Point
    {
        public readonly double X, Y;
        public Point(double x, double y) { X = x; Y = y; }
    }

    internal enum TraceStyle { Points, Line, Guide, Vector }

    internal sealed class Trace
    {
        public readonly string Label;
        public readonly Point[] Points;
        public readonly TraceStyle Style;
        public Trace(string label, TraceStyle style, IEnumerable<Point> points)
        {
            Label = label; Style = style; Points = points.ToArray();
        }
    }

    internal sealed class Plot
    {
        public readonly string Title, X, Y, Caption;
        public readonly List<Trace> Traces = new List<Trace>();
        public bool EqualAxes;
        public Plot(string title, string x, string y, string caption)
        { Title = title; X = x; Y = y; Caption = caption; }
        public Plot Add(string label, TraceStyle style, IEnumerable<Point> points)
        {
            var trace = new Trace(label, style, points);
            if (trace.Points.Length > 0) Traces.Add(trace);
            return this;
        }
    }

    internal sealed class Report
    {
        public readonly List<KeyValuePair<string, string>> Notes = new List<KeyValuePair<string, string>>();
        public readonly List<ResultTable> Tables = new List<ResultTable>();
        public readonly List<Plot> Plots = new List<Plot>();
        public readonly List<string> Warnings = new List<string>();
        public void Note(string title, string text) { Notes.Add(new KeyValuePair<string, string>(title, text)); }
        public void Warn(string text) { if (!Warnings.Contains(text)) Warnings.Add(text); }
        public ResultTable Table(string title, string headers)
        {
            var table = new ResultTable(title, headers);
            Tables.Add(table);
            return table;
        }
        public Plot Chart(string title, string x, string y, string caption = "")
        {
            var plot = new Plot(title, x, y, caption);
            Plots.Add(plot);
            return plot;
        }
        public bool HasResults { get { return Tables.Any(t => t.Rows.Count > 0) || Plots.Any(p => p.Traces.Count > 0); } }
    }

    internal sealed class Fit
    {
        public readonly double Slope, Intercept;
        public readonly double? R2, SlopeError;
        private Fit(double slope, double intercept, double? r2, double? error)
        { Slope = slope; Intercept = intercept; R2 = r2; SlopeError = error; }

        public static Fit Linear(Point[] points)
        {
            if (points.Length < 2) return null;
            double x = points.Average(p => p.X), y = points.Average(p => p.Y);
            double dx2 = points.Sum(p => Square(p.X - x));
            if (dx2 == 0) return null;
            double slope = points.Sum(p => (p.X - x) * (p.Y - y)) / dx2;
            double intercept = y - slope * x;
            double dy2 = points.Sum(p => Square(p.Y - y));
            double scale = points.Max(p => Math.Max(Math.Abs(p.Y), Math.Abs(slope * p.X)));
            double roundoff = scale * 2e-15;
            if (Math.Abs(intercept) <= roundoff) intercept = 0;
            double residual = points.Sum(p => {
                double deviation = p.Y - (slope * p.X + intercept);
                return Math.Abs(deviation) <= roundoff ? 0 : Square(deviation);
            });
            return new Fit(slope, intercept, dy2 == 0 ? (double?)null : 1 - residual / dy2,
                points.Length > 2 ? (double?)Math.Sqrt(residual / ((points.Length - 2) * dx2)) : null);
        }

        public string Equation
        {
            get { return "y = " + Format.Number(Slope) + "x " + (Intercept < 0 ? "− " : "+ ") +
                Format.Number(Math.Abs(Intercept)) + (R2.HasValue ? "  |  R² = " + Format.Number(R2.Value) : ""); }
        }

        public IEnumerable<Point> Domain(Point[] points)
        {
            var min = points.Min(p => p.X);
            var max = points.Max(p => p.X);
            return new[] { new Point(min, Slope * min + Intercept), new Point(max, Slope * max + Intercept) };
        }

        public static double Square(double x) { return x * x; }
        public static double StandardError(double[] values)
        {
            double mean = values.Average();
            return Math.Sqrt(values.Sum(x => Square(x - mean)) / ((double)values.Length * (values.Length - 1)));
        }
    }
}
