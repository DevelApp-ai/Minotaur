/*
 * Java 17 Grammar Test Examples
 * These examples demonstrate various Java 17 language features that should be parsed correctly
 */

package com.example.java17;

import java.util.*;
import java.util.stream.*;
import static java.lang.System.out;

// Java 17 Records
public record Point(int x, int y) {
    // Compact constructor
    public Point {
        if (x < 0 || y < 0) {
            throw new IllegalArgumentException("Coordinates must be non-negative");
        }
    }
    
    // Additional constructor
    public Point(int value) {
        this(value, value);
    }
    
    // Instance method
    public double distanceFromOrigin() {
        return Math.sqrt(x * x + y * y);
    }
}

// Java 17 Sealed Classes
public sealed class Shape permits Circle, Rectangle, Triangle {
    protected final String name;
    
    protected Shape(String name) {
        this.name = name;
    }
    
    public abstract double area();
}

// Permitted subclasses
public final class Circle extends Shape {
    private final double radius;
    
    public Circle(double radius) {
        super("Circle");
        this.radius = radius;
    }
    
    @Override
    public double area() {
        return Math.PI * radius * radius;
    }
}

public final class Rectangle extends Shape {
    private final double width, height;
    
    public Rectangle(double width, double height) {
        super("Rectangle");
        this.width = width;
        this.height = height;
    }
    
    @Override
    public double area() {
        return width * height;
    }
}

public non-sealed class Triangle extends Shape {
    private final double base, height;
    
    public Triangle(double base, double height) {
        super("Triangle");
        this.base = base;
        this.height = height;
    }
    
    @Override
    public double area() {
        return 0.5 * base * height;
    }
}

// Sealed interface
public sealed interface Vehicle permits Car, Truck, Motorcycle {
    String getBrand();
    int getMaxSpeed();
}

public record Car(String brand, int maxSpeed, int doors) implements Vehicle {
    @Override
    public String getBrand() { return brand; }
    
    @Override
    public int getMaxSpeed() { return maxSpeed; }
}

public record Truck(String brand, int maxSpeed, double cargoCapacity) implements Vehicle {
    @Override
    public String getBrand() { return brand; }
    
    @Override
    public int getMaxSpeed() { return maxSpeed; }
}

public record Motorcycle(String brand, int maxSpeed, boolean hasSidecar) implements Vehicle {
    @Override
    public String getBrand() { return brand; }
    
    @Override
    public int getMaxSpeed() { return maxSpeed; }
}

// Java 17 Pattern Matching for instanceof
public class PatternMatchingExamples {
    
    public static void patternMatchingInstanceof(Object obj) {
        // Pattern matching with instanceof
        if (obj instanceof String s) {
            System.out.println("String length: " + s.length());
        } else if (obj instanceof Integer i) {
            System.out.println("Integer value: " + i);
        } else if (obj instanceof List<?> list && !list.isEmpty()) {
            System.out.println("Non-empty list with " + list.size() + " elements");
        }
    }
    
    // Pattern matching in switch expressions (preview in Java 17)
    public static String formatValue(Object obj) {
        return switch (obj) {
            case Integer i -> "Integer: " + i;
            case String s -> "String: " + s;
            case Double d -> "Double: " + d;
            case null -> "null value";
            default -> "Unknown type: " + obj.getClass().getSimpleName();
        };
    }
    
    // Pattern matching with sealed classes
    public static double calculateArea(Shape shape) {
        return switch (shape) {
            case Circle c -> Math.PI * c.radius * c.radius;
            case Rectangle r -> r.width * r.height;
            case Triangle t -> 0.5 * t.base * t.height;
        };
    }
}

// Java 14+ Text Blocks (continued support in Java 17)
public class TextBlockExamples {
    
    public static final String JSON_TEMPLATE = """
        {
            "name": "%s",
            "age": %d,
            "address": {
                "street": "%s",
                "city": "%s"
            }
        }
        """;
    
    public static final String SQL_QUERY = """
        SELECT p.name, p.age, a.street, a.city
        FROM person p
        JOIN address a ON p.address_id = a.id
        WHERE p.age > 18
        ORDER BY p.name
        """;
    
    public static final String HTML_TEMPLATE = """
        <!DOCTYPE html>
        <html>
        <head>
            <title>%s</title>
        </head>
        <body>
            <h1>Welcome to %s</h1>
            <p>This is a text block example.</p>
        </body>
        </html>
        """;
}

// Java 11+ Local Variable Type Inference (var)
public class VarExamples {
    
    public void localVariableTypeInference() {
        // var with primitives
        var number = 42;
        var pi = 3.14159;
        var flag = true;
        
        // var with objects
        var message = "Hello, Java 17!";
        var list = new ArrayList<String>();
        var map = Map.of("key1", "value1", "key2", "value2");
        
        // var in enhanced for loops
        var numbers = List.of(1, 2, 3, 4, 5);
        for (var num : numbers) {
            System.out.println(num);
        }
        
        // var with lambda expressions
        var processor = (String s) -> s.toUpperCase();
        var result = processor.apply("hello world");
    }
}

// Java 8+ Lambda Expressions and Streams
public class LambdaAndStreamExamples {
    
    public void streamOperations() {
        var numbers = List.of(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        
        // Filter, map, and collect
        var evenSquares = numbers.stream()
            .filter(n -> n % 2 == 0)
            .map(n -> n * n)
            .collect(Collectors.toList());
        
        // Reduce operation
        var sum = numbers.stream()
            .reduce(0, Integer::sum);
        
        // Group by operation
        var words = List.of("apple", "banana", "cherry", "date", "elderberry");
        var groupedByLength = words.stream()
            .collect(Collectors.groupingBy(String::length));
        
        // Parallel stream
        var parallelSum = numbers.parallelStream()
            .mapToInt(Integer::intValue)
            .sum();
    }
    
    // Method references
    public void methodReferences() {
        var strings = List.of("hello", "world", "java", "17");
        
        // Static method reference
        strings.stream()
            .map(String::toUpperCase)
            .forEach(System.out::println);
        
        // Instance method reference
        var stringBuilder = new StringBuilder();
        strings.forEach(stringBuilder::append);
        
        // Constructor reference
        var lengths = strings.stream()
            .map(String::length)
            .collect(Collectors.toList());
    }
}

// Java 9+ Modules (module-info.java would be separate)
// This would typically be in module-info.java:
/*
module com.example.java17 {
    requires java.base;
    requires java.logging;
    exports com.example.java17;
}
*/

// Java 9+ Private methods in interfaces
public interface Calculator {
    
    default int addAndMultiply(int a, int b, int multiplier) {
        return multiply(add(a, b), multiplier);
    }
    
    default int subtractAndMultiply(int a, int b, int multiplier) {
        return multiply(subtract(a, b), multiplier);
    }
    
    private int add(int a, int b) {
        return a + b;
    }
    
    private int subtract(int a, int b) {
        return a - b;
    }
    
    private int multiply(int a, int b) {
        return a * b;
    }
}

// Java 8+ Functional Interfaces
@FunctionalInterface
public interface StringProcessor {
    String process(String input);
    
    default StringProcessor andThen(StringProcessor after) {
        return input -> after.process(this.process(input));
    }
}

// Java 17 Switch Expressions (finalized)
public class SwitchExpressionExamples {
    
    public enum Day {
        MONDAY, TUESDAY, WEDNESDAY, THURSDAY, FRIDAY, SATURDAY, SUNDAY
    }
    
    public String getDayType(Day day) {
        return switch (day) {
            case MONDAY, TUESDAY, WEDNESDAY, THURSDAY, FRIDAY -> "Weekday";
            case SATURDAY, SUNDAY -> "Weekend";
        };
    }
    
    public int getDaysUntilWeekend(Day day) {
        return switch (day) {
            case MONDAY -> 5;
            case TUESDAY -> 4;
            case WEDNESDAY -> 3;
            case THURSDAY -> 2;
            case FRIDAY -> 1;
            case SATURDAY, SUNDAY -> 0;
        };
    }
    
    // Switch with yield
    public String getSeasonDescription(String month) {
        return switch (month.toLowerCase()) {
            case "december", "january", "february" -> {
                yield "Winter - Cold season";
            }
            case "march", "april", "may" -> {
                yield "Spring - Blooming season";
            }
            case "june", "july", "august" -> {
                yield "Summer - Hot season";
            }
            case "september", "october", "november" -> {
                yield "Autumn - Falling leaves";
            }
            default -> {
                yield "Unknown month";
            }
        };
    }
}

// Java 17 Enhanced Enums
public enum Planet {
    MERCURY(3.303e+23, 2.4397e6),
    VENUS(4.869e+24, 6.0518e6),
    EARTH(5.976e+24, 6.37814e6),
    MARS(6.421e+23, 3.3972e6);
    
    private final double mass;   // in kilograms
    private final double radius; // in meters
    
    Planet(double mass, double radius) {
        this.mass = mass;
        this.radius = radius;
    }
    
    public double mass() { return mass; }
    public double radius() { return radius; }
    
    // Universal gravitational constant
    public static final double G = 6.67300E-11;
    
    public double surfaceGravity() {
        return G * mass / (radius * radius);
    }
    
    public double surfaceWeight(double otherMass) {
        return otherMass * surfaceGravity();
    }
}

// Java 17 Annotations
@Target({ElementType.TYPE, ElementType.METHOD})
@Retention(RetentionPolicy.RUNTIME)
@Documented
public @interface ApiVersion {
    String value() default "1.0";
    String[] supportedVersions() default {};
    boolean deprecated() default false;
}

// Using annotations
@ApiVersion(value = "2.0", supportedVersions = {"1.0", "1.5", "2.0"})
public class ApiService {
    
    @ApiVersion("1.0")
    @Deprecated
    public void oldMethod() {
        // Legacy implementation
    }
    
    @ApiVersion(value = "2.0", deprecated = false)
    public void newMethod() {
        // New implementation
    }
}

// Java 17 Exception Handling
public class ExceptionHandlingExamples {
    
    // Multi-catch exception handling
    public void multiCatchExample() {
        try {
            // Some risky operations
            var result = Integer.parseInt("not a number");
            var array = new int[5];
            array[10] = result;
        } catch (NumberFormatException | ArrayIndexOutOfBoundsException e) {
            System.err.println("Error occurred: " + e.getMessage());
        }
    }
    
    // Try-with-resources
    public void tryWithResourcesExample() {
        try (var scanner = new Scanner(System.in);
             var writer = new PrintWriter("output.txt")) {
            
            var input = scanner.nextLine();
            writer.println(input.toUpperCase());
            
        } catch (Exception e) {
            System.err.println("Error: " + e.getMessage());
        }
    }
    
    // Custom exceptions
    public static class ValidationException extends Exception {
        public ValidationException(String message) {
            super(message);
        }
    }
    
    public void validateAge(int age) throws ValidationException {
        if (age < 0 || age > 150) {
            throw new ValidationException("Age must be between 0 and 150");
        }
    }
}

// Main class demonstrating Java 17 features
public class Java17Demo {
    
    public static void main(String[] args) {
        // Records
        var point = new Point(10, 20);
        System.out.println("Point: " + point);
        System.out.println("Distance from origin: " + point.distanceFromOrigin());
        
        // Sealed classes
        var shapes = List.of(
            new Circle(5.0),
            new Rectangle(4.0, 6.0),
            new Triangle(3.0, 8.0)
        );
        
        shapes.forEach(shape -> {
            System.out.println(shape.name + " area: " + shape.area());
        });
        
        // Pattern matching
        PatternMatchingExamples.patternMatchingInstanceof("Hello World");
        PatternMatchingExamples.patternMatchingInstanceof(42);
        PatternMatchingExamples.patternMatchingInstanceof(List.of(1, 2, 3));
        
        // Switch expressions
        var dayType = new SwitchExpressionExamples().getDayType(SwitchExpressionExamples.Day.FRIDAY);
        System.out.println("Friday is a: " + dayType);
        
        // Text blocks
        var json = String.format(TextBlockExamples.JSON_TEMPLATE, "John Doe", 30, "123 Main St", "Anytown");
        System.out.println("JSON:\n" + json);
        
        // Streams and lambdas
        var numbers = IntStream.rangeClosed(1, 10)
            .filter(n -> n % 2 == 0)
            .map(n -> n * n)
            .boxed()
            .collect(Collectors.toList());
        
        System.out.println("Even squares: " + numbers);
        
        // Vehicles with sealed interfaces
        var vehicles = List.of(
            new Car("Toyota", 180, 4),
            new Truck("Ford", 120, 5000.0),
            new Motorcycle("Harley", 200, false)
        );
        
        vehicles.forEach(vehicle -> {
            System.out.println(vehicle.getBrand() + " max speed: " + vehicle.getMaxSpeed());
        });
    }
}

