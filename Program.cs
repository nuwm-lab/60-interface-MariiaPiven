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
            // (b - a) x (c - a)
            return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        }
    }

    // Base polygon model with ordering and area via shoelace
    public abstract class Polygon
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

    internal static class ConsoleUI
    {
        public static List<Point> ReadVertices(int count, string label)
        {
            var pts = new List<Point>(count);
            for (int i = 0; i < count; i++)
            {
                double x = ReadDouble($"Введіть координату x{i + 1} ({label}): ");
                double y = ReadDouble($"Введіть координату y{i + 1} ({label}): ");
                pts.Add(new Point(x, y));
            }
            return pts;
        }

        public static void PrintVertices(IReadOnlyList<Point> vertices, string title)
        {
            Console.WriteLine(title);
            for (int i = 0; i < vertices.Count; i++)
                Console.WriteLine($"Вершина {i + 1}: {vertices[i]}");
        }

        private static double ReadDouble(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var s = Console.ReadLine();
                if (double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out double v))
                    return v;
                Console.WriteLine("Некоректне число. Спробуйте ще раз.");
            }
        }
    }

    internal static class Program
    {
        private static void Main()
        {
            // Поліморфне створення: користувач обирає фігуру в рантаймі
            Polygon shape;
            while (true)
            {
                Console.Write("Оберіть фігуру (3 - трикутник, 4 - опуклий чотирикутник): ");
                var s = Console.ReadLine();
                if (int.TryParse(s, out int n))
                {
                    if (n == 3) { shape = new Triangle(); break; }
                    if (n == 4) { shape = new ConvexQuadrilateral(); break; }
                }
                Console.WriteLine("Некоректний вибір. Вкажіть 3 або 4.\n");
            }

            // Ввід вершин із використанням базового посилання (демонстрація динамічного поліморфізму)
            while (true)
            {
                try
                {
                    var pts = ConsoleUI.ReadVertices(shape.VertexCount, shape.Describe().ToLower());
                    shape.SetVertices(pts); // викликає перевірки, специфічні для похідного класу
                    break;
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"Помилка: {ex.Message}\n");
                }
            }

            Console.WriteLine($"\n{shape.Describe()}");
            ConsoleUI.PrintVertices(shape.Vertices, "Координати вершин:");
            Console.WriteLine($"Площа: {shape.CalculateArea():F2}");

            // Підказка для експерименту: Змініть virtual Describe() на звичайний метод у базовому класі
            // і приберіть override у похідних (або замініть на 'new'). Тоді shape.Describe() при посиланні Polygon
            // завжди друкуватиме базовий текст, що демонструє різницю між віртуальним та невіртуальним викликом.
        }
    }
}
