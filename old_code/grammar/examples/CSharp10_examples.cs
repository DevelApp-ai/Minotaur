/*
 * C# 10 Grammar Test Examples (.NET 6)
 * These examples demonstrate various C# 10 language features that should be parsed correctly
 */

// Global using directives (C# 10)
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using static System.Console;
global using static System.Math;

// File-scoped namespace declaration (C# 10)
namespace CSharp10Examples;

// Record structs (C# 10)
public readonly record struct Point(double X, double Y)
{
    public double DistanceFromOrigin => Sqrt(X * X + Y * Y);
    
    public static Point Origin => new(0, 0);
    
    // Deconstruction
    public void Deconstruct(out double x, out double y)
    {
        x = X;
        y = Y;
    }
}

public record struct MutablePoint(double X, double Y)
{
    public double X { get; set; } = X;
    public double Y { get; set; } = Y;
    
    public readonly double Distance => Sqrt(X * X + Y * Y);
}

// Record classes with enhanced features (C# 10)
public record Person(string FirstName, string LastName, int Age)
{
    // Sealed ToString override (C# 10)
    public sealed override string ToString()
    {
        return $"{FirstName} {LastName} (Age: {Age})";
    }
    
    // With expressions
    public Person WithAge(int newAge) => this with { Age = newAge };
}

public record Employee(string FirstName, string LastName, int Age, string Department, decimal Salary) 
    : Person(FirstName, LastName, Age)
{
    public string FullName => $"{FirstName} {LastName}";
    
    // Property patterns
    public bool IsHighEarner => Salary > 100000m;
}

// Nullable reference types (C# 8+, enhanced in C# 10)
public class NullableExamples
{
    public string? NullableString { get; set; }
    public string NonNullableString { get; set; } = string.Empty;
    
    public void ProcessString(string? input)
    {
        if (input is not null)
        {
            WriteLine($"Processing: {input.ToUpper()}");
        }
    }
    
    public string GetStringOrDefault(string? input) => input ?? "Default";
}

// Enhanced pattern matching (C# 10)
public static class PatternMatchingExamples
{
    // Property patterns
    public static string DescribePerson(Person person) => person switch
    {
        { Age: < 18 } => "Minor",
        { Age: >= 18 and < 65 } => "Adult",
        { Age: >= 65 } => "Senior",
        _ => "Unknown"
    };
    
    // Extended property patterns
    public static string DescribeEmployee(Employee emp) => emp switch
    {
        { Department: "Engineering", Salary: > 150000 } => "Senior Engineer",
        { Department: "Engineering", Salary: > 100000 } => "Engineer",
        { Department: "Sales", IsHighEarner: true } => "Senior Sales",
        { Age: < 30, Department: "Marketing" } => "Junior Marketing",
        _ => "Regular Employee"
    };
    
    // Relational patterns
    public static string ClassifyTemperature(double temp) => temp switch
    {
        < 0 => "Freezing",
        >= 0 and < 10 => "Cold",
        >= 10 and < 20 => "Cool",
        >= 20 and < 30 => "Warm",
        >= 30 => "Hot",
        double.NaN => "Invalid"
    };
    
    // Logical patterns with 'not'
    public static bool IsValidAge(int age) => age switch
    {
        not (< 0 or > 150) => true,
        _ => false
    };
    
    // Parenthesized patterns
    public static string AnalyzeNumber(int number) => number switch
    {
        (> 0 and < 10) or (> 90 and < 100) => "Edge range",
        >= 10 and <= 90 => "Normal range",
        _ => "Out of range"
    };
}

// Lambda improvements (C# 10)
public class LambdaImprovements
{
    public void DemonstrateNaturalTypes()
    {
        // Natural type inference for lambdas
        var parse = (string s) => int.Parse(s);
        var choose = (bool b) => b ? 1 : 0;
        
        // Lambda with explicit return type
        var parseWithReturn = string (object input) => input.ToString() ?? "";
        
        // Lambda with attributes
        var attributedLambda = [Obsolete] (int x) => x * 2;
    }
    
    public void DemonstrateMethodGroups()
    {
        // Method group to delegate conversion
        Func<string, int> parser = int.Parse;
        Action<string> writer = WriteLine;
        
        // Method group with natural type
        var converter = double.Parse;
    }
}

// Constant interpolated strings (C# 10)
public static class ConstantInterpolation
{
    private const string Name = "World";
    private const string Greeting = $"Hello, {Name}!";
    private const string Template = $"Welcome to {Name} version {"1.0"}";
    
    public const string ComplexConstant = $"Result: {42 + 8}";
}

// Improved definite assignment (C# 10)
public class DefiniteAssignmentImprovements
{
    public void ImprovedAnalysis()
    {
        string message;
        bool condition = Random.Shared.NextDouble() > 0.5;
        
        if (condition)
        {
            message = "True branch";
        }
        else
        {
            message = "False branch";
        }
        
        // C# 10 improves definite assignment analysis
        WriteLine(message); // No compiler error
    }
    
    public void NullStateAnalysis(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return;
        }
        
        // C# 10 knows input is not null here
        WriteLine(input.ToUpper());
    }
}

// Caller argument expression (C# 10)
public static class CallerArgumentExpressionExample
{
    public static void ValidateArgument<T>(T argument, [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
    
    public static void TestValidation()
    {
        string? value = null;
        ValidateArgument(value); // paramName will be "value"
        
        var person = new Person("John", "Doe", 30);
        ValidateArgument(person.FirstName); // paramName will be "person.FirstName"
    }
}

// AsyncMethodBuilder attribute (C# 10)
[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
public static async ValueTask<int> PooledAsyncMethod()
{
    await Task.Delay(100);
    return 42;
}

// Generic attributes (C# 11 preview, available in C# 10 with preview features)
#if PREVIEW_FEATURES
public class GenericAttribute<T> : Attribute
{
    public T Value { get; }
    
    public GenericAttribute(T value)
    {
        Value = value;
    }
}

[GenericAttribute<string>("test")]
[GenericAttribute<int>(42)]
public class AttributeTarget
{
    // Class with generic attributes
}
#endif

// Interpolated string handlers (C# 10)
public static class InterpolatedStringHandlers
{
    public static void LogMessage(string message)
    {
        WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
    }
    
    public static void ConditionalLogging(bool condition, string message)
    {
        if (condition)
        {
            // Interpolated string handler optimizes this
            WriteLine($"Debug: {message} at {DateTime.Now}");
        }
    }
}

// Struct improvements (C# 10)
public readonly struct ReadonlyStruct
{
    public readonly int Value;
    
    public ReadonlyStruct(int value)
    {
        Value = value;
    }
    
    // Readonly members
    public readonly override string ToString() => Value.ToString();
}

public struct StructWithParameterlessConstructor
{
    public int X { get; set; }
    public int Y { get; set; }
    
    // Parameterless constructor (C# 10)
    public StructWithParameterlessConstructor()
    {
        X = 0;
        Y = 0;
    }
    
    public StructWithParameterlessConstructor(int x, int y)
    {
        X = x;
        Y = y;
    }
}

// With expressions for anonymous types (C# 10)
public static class WithExpressions
{
    public static void DemonstrateWith()
    {
        var person = new Person("John", "Doe", 30);
        
        // With expression
        var olderPerson = person with { Age = 31 };
        var differentPerson = person with { FirstName = "Jane", Age = 25 };
        
        WriteLine($"Original: {person}");
        WriteLine($"Older: {olderPerson}");
        WriteLine($"Different: {differentPerson}");
    }
    
    public static void RecordStructWith()
    {
        var point = new Point(3.0, 4.0);
        var newPoint = point with { X = 5.0 };
        
        WriteLine($"Original: {point}");
        WriteLine($"New: {newPoint}");
    }
}

// Async improvements (C# 10)
public static class AsyncImprovements
{
    public static async Task<string> ProcessDataAsync(IEnumerable<string> data)
    {
        var results = new List<string>();
        
        await foreach (var item in ProcessItemsAsync(data))
        {
            results.Add(item);
        }
        
        return string.Join(", ", results);
    }
    
    private static async IAsyncEnumerable<string> ProcessItemsAsync(IEnumerable<string> items)
    {
        foreach (var item in items)
        {
            await Task.Delay(10); // Simulate async work
            yield return item.ToUpper();
        }
    }
}

// LINQ improvements (C# 10)
public static class LinqImprovements
{
    public static void DemonstrateNewMethods()
    {
        var numbers = Enumerable.Range(1, 10);
        
        // Chunk (groups elements into chunks)
        var chunks = numbers.Chunk(3);
        foreach (var chunk in chunks)
        {
            WriteLine($"Chunk: [{string.Join(", ", chunk)}]");
        }
        
        // DistinctBy
        var people = new[]
        {
            new Person("John", "Doe", 30),
            new Person("Jane", "Doe", 25),
            new Person("Bob", "Smith", 30)
        };
        
        var distinctByAge = people.DistinctBy(p => p.Age);
        foreach (var person in distinctByAge)
        {
            WriteLine($"Distinct by age: {person}");
        }
        
        // MaxBy/MinBy
        var oldest = people.MaxBy(p => p.Age);
        var youngest = people.MinBy(p => p.Age);
        
        WriteLine($"Oldest: {oldest}");
        WriteLine($"Youngest: {youngest}");
    }
}

// Span and Memory improvements (C# 10)
public static class SpanImprovements
{
    public static void DemonstrateSpanPatterns()
    {
        ReadOnlySpan<int> numbers = stackalloc int[] { 1, 2, 3, 4, 5 };
        
        // Pattern matching with spans
        var result = numbers switch
        {
            [1, 2, 3, 4, 5] => "Exact match",
            [1, .., 5] => "Starts with 1, ends with 5",
            [var first, ..] => $"Starts with {first}",
            [] => "Empty",
            _ => "Other"
        };
        
        WriteLine($"Span pattern result: {result}");
    }
}

// Main program demonstrating C# 10 features
public static class Program
{
    public static async Task Main(string[] args)
    {
        WriteLine("C# 10 Features Demo");
        WriteLine("==================");
        
        // Record structs
        var point = new Point(3.0, 4.0);
        WriteLine($"Point: {point}, Distance: {point.DistanceFromOrigin:F2}");
        
        var (x, y) = point;
        WriteLine($"Deconstructed: X={x}, Y={y}");
        
        // Record classes
        var person = new Person("John", "Doe", 30);
        var employee = new Employee("Jane", "Smith", 28, "Engineering", 120000m);
        
        WriteLine($"Person: {person}");
        WriteLine($"Employee: {employee}");
        WriteLine($"Employee description: {PatternMatchingExamples.DescribeEmployee(employee)}");
        
        // Pattern matching
        WriteLine($"Temperature 25°C: {PatternMatchingExamples.ClassifyTemperature(25)}");
        WriteLine($"Age 150 valid: {PatternMatchingExamples.IsValidAge(150)}");
        
        // Lambda improvements
        var lambdaDemo = new LambdaImprovements();
        lambdaDemo.DemonstrateNaturalTypes();
        
        // Constant interpolation
        WriteLine($"Constant greeting: {ConstantInterpolation.Greeting}");
        
        // With expressions
        WithExpressions.DemonstrateWith();
        WithExpressions.RecordStructWith();
        
        // Async improvements
        var data = new[] { "hello", "world", "csharp", "ten" };
        var result = await AsyncImprovements.ProcessDataAsync(data);
        WriteLine($"Async result: {result}");
        
        // LINQ improvements
        LinqImprovements.DemonstrateNewMethods();
        
        // Span improvements
        SpanImprovements.DemonstrateSpanPatterns();
        
        WriteLine("\nDemo completed!");
    }
}

// Additional C# 10 features

// Improved interpolated strings
public static class InterpolatedStringExamples
{
    public static void FormattingExamples()
    {
        var value = 123.456;
        var date = DateTime.Now;
        
        // Enhanced formatting
        WriteLine($"Value: {value:C}");
        WriteLine($"Date: {date:yyyy-MM-dd}");
        WriteLine($"Hex: {255:X}");
        
        // Conditional formatting
        var condition = true;
        WriteLine($"Result: {(condition ? "Success" : "Failure")}");
    }
}

// Namespace improvements
namespace NestedNamespace.SubNamespace
{
    public class NestedClass
    {
        public string Message => "From nested namespace";
    }
}

// Using declarations
public class UsingDeclarations
{
    public void FileProcessing()
    {
        using var file = File.OpenRead("example.txt");
        using var reader = new StreamReader(file);
        
        // File automatically disposed at end of method
        var content = reader.ReadToEnd();
        WriteLine($"File content length: {content.Length}");
    }
}

// Switch expressions with complex patterns
public static class ComplexSwitchExpressions
{
    public static string ProcessValue(object value) => value switch
    {
        int i when i > 0 => $"Positive integer: {i}",
        int i when i < 0 => $"Negative integer: {i}",
        int => "Zero",
        string s when !string.IsNullOrEmpty(s) => $"Non-empty string: {s}",
        string => "Empty or null string",
        Person { Age: > 65 } p => $"Senior person: {p.FirstName}",
        Person p => $"Person: {p.FirstName}",
        null => "Null value",
        _ => $"Unknown type: {value.GetType().Name}"
    };
}

// Target-typed new expressions
public static class TargetTypedNew
{
    public static void Examples()
    {
        // Target-typed new
        List<string> list = new();
        Dictionary<string, int> dict = new();
        
        // In method calls
        ProcessList(new List<int> { 1, 2, 3 });
        ProcessDict(new() { ["key"] = 42 });
    }
    
    private static void ProcessList(List<int> list) { }
    private static void ProcessDict(Dictionary<string, int> dict) { }
}

// Required members (C# 11 preview, available with preview features)
#if PREVIEW_FEATURES
public class RequiredMembersExample
{
    public required string Name { get; init; }
    public required int Age { get; init; }
    public string? Email { get; init; }
    
    public RequiredMembersExample()
    {
        // Constructor can be empty, required members must be set by caller
    }
}
#endif

