using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LabWork
{
    // Simple point struct to hold coordinates
    public readonly struct Point
    {
        public double X { get; }
        public double Y { get; }
        public Point(double x, double y) { X = x; Y = y; }
        public override string ToString() => $"({X}, {Y})";
    }

    internal static class GeometryUtils
    {
        public const double Epsilon = 1e-9;

        public static bool IsZero(double v) => Math.Abs(v) < Epsilon;
        public static double Distance(Point a, Point b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
        public static double Cross(Point a, Point b, Point c)
        {
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }
    }

    /// <summary>
    /// Interface for geometric shapes
    /// </summary>
    public interface IShape
    {
        string Describe();
        double CalculateArea();
        double CalculatePerimeter();
        void DisplayInfo();
    }

    // Base polygon model with ordering and area via shoelace
    public abstract class Polygon : IShape
    {
        private readonly List<Point> _vertices = new();
        public IReadOnlyList<Point> Vertices => _vertices;
        protected abstract int ExpectedVertexCount { get; }
        public int VertexCount => ExpectedVertexCount; // expose for UI

        // Virtual description to demonstrate dynamic dispatch
        public virtual string Describe() => $"Багатокутник на {VertexCount} вершинах";

        public void SetVertices(IEnumerable<Point> points)
        {
            if (points is null) throw new ArgumentException("Список вершин не може бути null.");
            var list = points.ToList();
            if (list.Count != ExpectedVertexCount)
                throw new ArgumentException($"Очікується {ExpectedVertexCount} вершин(и), отримано {list.Count}.");

            // Duplicate check (epsilon)
            for (int i = 0; i < list.Count; i++)
                for (int j = i + 1; j < list.Count; j++)
                    if (GeometryUtils.Distance(list[i], list[j]) < GeometryUtils.Epsilon)
                        throw new ArgumentException("Деякі вершини співпадають.");

            // Order CCW around centroid to ensure consistent area and validation
            var cx = list.Average(p => p.X);
            var cy = list.Average(p => p.Y);
            list = list
                .OrderBy(p => Math.Atan2(p.Y - cy, p.X - cx))
                .ToList();

            _vertices.Clear();
            _vertices.AddRange(list);

            ValidateAfterOrdering();
        }

        protected virtual void ValidateAfterOrdering() { }

        public virtual double CalculateArea()
        {
            // Shoelace formula (requires ordered polygon without self-intersections)
            double sum = 0;
            int n = _vertices.Count;
            for (int i = 0; i < n; i++)
            {
                var a = _vertices[i];
                var b = _vertices[(i + 1) % n];
                sum += a.X * b.Y - a.Y * b.X;
            }
            return Math.Abs(sum) / 2.0;
        }

        public virtual double CalculatePerimeter()
        {
            double perimeter = 0;
            int n = _vertices.Count;
            for (int i = 0; i < n; i++)
            {
                var a = _vertices[i];
                var b = _vertices[(i + 1) % n];
                perimeter += GeometryUtils.Distance(a, b);
            }
            return perimeter;
        }

        public virtual void DisplayInfo()
        {
            Console.WriteLine($"\n{new string('=', 60)}");
            Console.WriteLine($"Фігура: {Describe()}");
            Console.WriteLine($"Площа: {CalculateArea():F2}");
            Console.WriteLine($"Периметр: {CalculatePerimeter():F2}");
            Console.WriteLine($"{new string('=', 60)}");
        }
    }

    /// <summary>
    /// Triangle with non-collinearity validation
    /// </summary>
    public sealed class Triangle : Polygon
    {
        protected override int ExpectedVertexCount => 3;

        public override string Describe() => "Трикутник";

        protected override void ValidateAfterOrdering()
        {
            var a = Vertices[0];
            var b = Vertices[1];
            var c = Vertices[2];
            double area2 = GeometryUtils.Cross(a, b, c);
            if (GeometryUtils.IsZero(area2))
                throw new ArgumentException("Точки трикутника лежать на одній прямій.");
        }
    }

    /// <summary>
    /// Convex quadrilateral with convexity validation
    /// </summary>
    public sealed class ConvexQuadrilateral : Polygon
    {
        protected override int ExpectedVertexCount => 4;

        public override string Describe() => "Опуклий чотирикутник";

        protected override void ValidateAfterOrdering()
        {
            // Strict convexity: consistent cross product signs and no collinear adjacent triples
            int n = Vertices.Count;
            double? sign = null;
            for (int i = 0; i < n; i++)
            {
                var a = Vertices[i];
                var b = Vertices[(i + 1) % n];
                var c = Vertices[(i + 2) % n];
                double cross = GeometryUtils.Cross(a, b, c);
                if (GeometryUtils.IsZero(cross))
                    throw new ArgumentException("Суміжні вершини дають колінеарність — фігура не опукла.");
                double s = Math.Sign(cross);
                sign ??= s;
                if (Math.Sign(cross) != sign)
                    throw new ArgumentException("Чотирикутник не опуклий або вершини подані у невірному порядку.");
            }
        }
    }

    /// <summary>
    /// Service for console input
    /// </summary>
    internal static class InputService
    {
        public static List<Point> ReadVertices(int count, string label)
        {
            Console.WriteLine($"\nВведіть {count} вершини для '{label}':");
            var pts = new List<Point>(count);
            for (int i = 0; i < count; i++)
            {
                double x = ReadDouble($"  x{i + 1}: ");
                double y = ReadDouble($"  y{i + 1}: ");
                pts.Add(new Point(x, y));
            }
            return pts;
        }

        public static double ReadDouble(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s))
                {
                    Console.WriteLine("Введіть число.");
                    continue;
                }
                if (double.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                    return v;
                Console.WriteLine("Некоректне число. Спробуйте ще раз.");
            }
        }

        public static int ReadInt(string prompt, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                var s = Console.ReadLine();
                if (int.TryParse(s, out int v) && v >= min && v <= max)
                    return v;
                Console.WriteLine($"Введіть число від {min} до {max}.");
            }
        }
    }

    /// <summary>
    /// Service for console output
    /// </summary>
    internal static class OutputService
    {
        public static void PrintHeader(string title)
        {
            Console.WriteLine($"\n{new string('═', 62)}");
            Console.WriteLine($"  {title}");
            Console.WriteLine($"{new string('═', 62)}");
        }

        public static void PrintVertices(IReadOnlyList<Point> vertices, string title)
        {
            Console.WriteLine($"\n{title}");
            for (int i = 0; i < vertices.Count; i++)
                Console.WriteLine($"  Вершина {i + 1}: {vertices[i]}");
        }

        public static void PrintShapeInfo(IShape shape)
        {
            if (shape is Polygon polygon)
            {
                Console.WriteLine($"\nТип фігури: {shape.Describe()}");
                PrintVertices(polygon.Vertices, "Координати вершин:");
                Console.WriteLine($"Площа: {shape.CalculateArea():F2}");
                Console.WriteLine($"Периметр: {shape.CalculatePerimeter():F2}");
            }
            else
            {
                shape.DisplayInfo();
            }
        }

        public static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Помилка: {message}");
            Console.ResetColor();
        }

        public static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }

    internal static class Program
    {
        private static void Main()
        {
            OutputService.PrintHeader("ДЕМОНСТРАЦІЯ РОБОТИ З ІНТЕРФЕЙСАМИ ТА ПОЛІМОРФІЗМОМ");

            // Create shape using interface
            IShape shape = CreateShape();
            
            // Input vertices
            InputVertices(shape);

            // Display results using interface
            Console.WriteLine("\n" + new string('─', 62));
            OutputService.PrintShapeInfo(shape);
            Console.WriteLine(new string('─', 62));

            // Demonstrate polymorphism
            DemonstratePolymorphism(shape);

            Console.WriteLine("\n\nНатисніть Enter для завершення...");
            Console.ReadLine();
        }

        private static IShape CreateShape()
        {
            Console.WriteLine("\nОберіть тип фігури:");
            Console.WriteLine("  3 - Трикутник");
            Console.WriteLine("  4 - Опуклий чотирикутник");
            
            int choice = InputService.ReadInt("\nВаш вибір: ", 3, 4);
            
            IShape shape = choice == 3 ? new Triangle() : (IShape)new ConvexQuadrilateral();
            
            OutputService.PrintSuccess($"\n✓ Створено: {shape.Describe()}");
            return shape;
        }

        private static void InputVertices(IShape shape)
        {
            if (shape is not Polygon polygon)
                return;

            while (true)
            {
                try
                {
                    var pts = InputService.ReadVertices(polygon.VertexCount, shape.Describe().ToLower());
                    polygon.SetVertices(pts);
                    OutputService.PrintSuccess("\n✓ Вершини успішно встановлено!");
                    break;
                }
                catch (ArgumentException ex)
                {
                    OutputService.PrintError(ex.Message);
                }
            }
        }

        private static void DemonstratePolymorphism(IShape shape)
        {
            Console.WriteLine("\n" + new string('─', 62));
            Console.WriteLine("ДЕМОНСТРАЦІЯ ПОЛІМОРФІЗМУ");
            Console.WriteLine(new string('─', 62));
            
            Console.WriteLine($"Тип інтерфейсу: {nameof(IShape)}");
            Console.WriteLine($"Фактичний тип: {shape.GetType().Name}");
            Console.WriteLine($"Реалізує IShape: {shape is IShape}");
            Console.WriteLine($"\nВиклик через інтерфейс:");
            Console.WriteLine($"  {nameof(IShape.Describe)}() => \"{shape.Describe()}\"");
            Console.WriteLine($"  {nameof(IShape.CalculateArea)}() => {shape.CalculateArea():F2}");
            Console.WriteLine($"  {nameof(IShape.CalculatePerimeter)}() => {shape.CalculatePerimeter():F2}");
        }
    }
}
