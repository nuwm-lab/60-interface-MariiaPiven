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

    // Абстрактний базовий клас для всіх геометричних фігур
    public abstract class GeometricShape
    {
        protected string _name;
        protected DateTime _createdAt;

        // Конструктор абстрактного класу
        protected GeometricShape(string name)
        {
            _name = name;
            _createdAt = DateTime.Now;
            Console.WriteLine($"[Конструктор GeometricShape] Створено об'єкт '{_name}' о {_createdAt:HH:mm:ss}");
        }

        // Деструктор (фіналізатор)
        ~GeometricShape()
        {
            Console.WriteLine($"[Деструктор GeometricShape] Видалення об'єкта '{_name}'");
        }

        // Абстрактні методи, які мають бути реалізовані в похідних класах
        public abstract double CalculateArea();
        public abstract double CalculatePerimeter();
        public abstract string Describe();

        // Загальний метод для виведення інформації
        public virtual void DisplayInfo()
        {
            Console.WriteLine($"\n{'=',40}");
            Console.WriteLine($"Фігура: {Describe()}");
            Console.WriteLine($"Час створення: {_createdAt:HH:mm:ss.fff}");
            Console.WriteLine($"Площа: {CalculateArea():F2}");
            Console.WriteLine($"Периметр: {CalculatePerimeter():F2}");
            Console.WriteLine($"{'=',40}");
        }
    }

    // Base polygon model with ordering and area via shoelace
    public abstract class Polygon : GeometricShape
    {
        private readonly List<Point> _vertices = new();
        public IReadOnlyList<Point> Vertices => _vertices;
        protected abstract int ExpectedVertexCount { get; }
        public int VertexCount => ExpectedVertexCount; // expose for UI

        // Конструктор базового класу Polygon
        protected Polygon(string name) : base(name)
        {
            Console.WriteLine($"[Конструктор Polygon] Ініціалізація багатокутника '{name}'");
        }

        // Деструктор Polygon
        ~Polygon()
        {
            Console.WriteLine($"[Деструктор Polygon] Очищення ресурсів багатокутника '{_name}'");
        }

        // Virtual description to demonstrate dynamic dispatch
        public override string Describe() => $"Багатокутник на {VertexCount} вершинах";

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

        public override double CalculateArea()
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

        public override double CalculatePerimeter()
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

        public override void DisplayInfo()
        {
            base.DisplayInfo();
            ConsoleUI.PrintVertices(Vertices, "Координати вершин:");
        }
    }

    /// <summary>
    /// Triangle with non-collinearity validation
    /// </summary>
    public sealed class Triangle : Polygon
    {
        protected override int ExpectedVertexCount => 3;

        // Конструктор трикутника
        public Triangle() : base("Трикутник")
        {
            Console.WriteLine($"[Конструктор Triangle] Трикутник готовий до використання");
        }

        // Деструктор трикутника
        ~Triangle()
        {
            Console.WriteLine($"[Деструктор Triangle] Знищення трикутника");
        }

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

        // Конструктор опуклого чотирикутника
        public ConvexQuadrilateral() : base("Опуклий чотирикутник")
        {
            Console.WriteLine($"[Конструктор ConvexQuadrilateral] Опуклий чотирикутник готовий до використання");
        }

        // Деструктор опуклого чотирикутника
        ~ConvexQuadrilateral()
        {
            Console.WriteLine($"[Деструктор ConvexQuadrilateral] Знищення опуклого чотирикутника");
        }

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
        // Допоміжний метод для демонстрації життєвого циклу трикутника
        private static void CreateAndDestroyTriangle()
        {
            Console.WriteLine(">>> Викликаємо конструктори:");
            var tempTriangle = new Triangle();
            tempTriangle.SetVertices(new[] { new Point(0, 0), new Point(4, 0), new Point(2, 3) });
            Console.WriteLine($"✓ Площа створеного трикутника: {tempTriangle.CalculateArea():F2}");
            Console.WriteLine(">>> Об'єкт виходить за межі області видимості...");
        } // об'єкт стає недоступним, але деструктор викличеться пізніше

        // Допоміжний метод для демонстрації життєвого циклу чотирикутника
        private static void CreateAndDestroyQuadrilateral()
        {
            Console.WriteLine(">>> Викликаємо конструктори:");
            var tempQuad = new ConvexQuadrilateral();
            tempQuad.SetVertices(new[] { new Point(0, 0), new Point(3, 0), new Point(3, 3), new Point(0, 3) });
            Console.WriteLine($"✓ Площа створеного чотирикутника: {tempQuad.CalculateArea():F2}");
            Console.WriteLine(">>> Об'єкт виходить за межі області видимості...");
        } // об'єкт стає недоступним, але деструктор викличеться пізніше

        private static void Main()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ДЕМОНСТРАЦІЯ РОБОТИ З АБСТРАКТНИМИ КЛАСАМИ ТА ПОЛІМОРФІЗМОМ  ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

            // Створюємо масив геометричних фігур для демонстрації поліморфізму
            List<GeometricShape> shapes = new List<GeometricShape>();

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

            Console.WriteLine();

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

            // Додаємо створену фігуру до списку
            shapes.Add(shape);

            Console.WriteLine("\n" + new string('─', 60));
            Console.WriteLine("РЕЗУЛЬТАТИ ОБЧИСЛЕНЬ");
            Console.WriteLine(new string('─', 60));

            // Демонстрація поліморфізму через абстрактний клас
            foreach (var geomShape in shapes)
            {
                // Використовуємо метод базового абстрактного класу
                geomShape.DisplayInfo();
            }

            Console.WriteLine("\n" + new string('─', 60));
            Console.WriteLine("ДОДАТКОВА ІНФОРМАЦІЯ ПРО ПОЛІМОРФІЗМ");
            Console.WriteLine(new string('─', 60));
            Console.WriteLine($"Тип змінної: {nameof(GeometricShape)}");
            Console.WriteLine($"Фактичний тип об'єкта: {shape.GetType().Name}");
            Console.WriteLine($"Метод Describe() викликається динамічно: {shape.Describe()}");

            Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  ДЕМОНСТРАЦІЯ РОБОТИ КОНСТРУКТОРІВ ТА ДЕСТРУКТОРІВ        ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine("\nСтворимо кілька додаткових об'єктів для демонстрації:");
            
            Console.WriteLine("\n--- СТВОРЕННЯ ТРИКУТНИКА ---");
            CreateAndDestroyTriangle();
            
            Console.WriteLine("\n--- Форсуємо збирання сміття для виклику деструкторів ---");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            Console.WriteLine("\n--- СТВОРЕННЯ ЧОТИРИКУТНИКА ---");
            CreateAndDestroyQuadrilateral();
            
            Console.WriteLine("\n--- Форсуємо збирання сміття для виклику деструкторів ---");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Console.WriteLine("\n\nНатисніть будь-яку клавішу для завершення програми...");
            Console.ReadKey();

            // Підказка для експерименту: Змініть virtual Describe() на звичайний метод у базовому класі
            // і приберіть override у похідних (або замініть на 'new'). Тоді shape.Describe() при посиланні Polygon
            // завжди друкуватиме базовий текст, що демонструє різницю між віртуальним та невіртуальним викликом.
        }
    }
}
