"""
Python 3.11 Grammar Test Examples
These examples demonstrate various Python 3.11 language features that should be parsed correctly
"""

import asyncio
import tomllib
from typing import Self, TypeVar, TypeVarTuple, ParamSpec, LiteralString
from dataclasses import dataclass
from contextlib import asynccontextmanager
from collections.abc import Callable, Awaitable

# Type annotations and type hints (Python 3.11)
T = TypeVar('T')
Ts = TypeVarTuple('Ts')
P = ParamSpec('P')

# Self type (PEP 673)
class Builder:
    def __init__(self, value: int) -> None:
        self.value = value
    
    def add(self, other: int) -> Self:
        """Return a new instance of the same type"""
        return type(self)(self.value + other)
    
    def multiply(self, factor: int) -> Self:
        return type(self)(self.value * factor)

# Literal string type (PEP 675)
def execute_sql(query: LiteralString) -> None:
    """Only accepts literal strings to prevent SQL injection"""
    print(f"Executing: {query}")

# Variadic generics (PEP 646)
class Array:
    def __init__(self, *shape: *Ts) -> None:
        self.shape = shape
    
    def get_shape(self) -> tuple[*Ts]:
        return self.shape

# Required/NotRequired TypedDict items (PEP 655)
from typing import TypedDict, Required, NotRequired

class PersonDict(TypedDict):
    name: Required[str]
    age: Required[int]
    email: NotRequired[str]  # Optional field
    phone: NotRequired[str]  # Optional field

# Exception Groups and except* (PEP 654)
class DatabaseError(Exception):
    pass

class NetworkError(Exception):
    pass

class ValidationError(Exception):
    pass

async def process_data_with_exception_groups():
    """Demonstrate exception groups and except* syntax"""
    errors = []
    
    try:
        # Simulate multiple operations that might fail
        operations = [
            ("db_operation", DatabaseError("Connection failed")),
            ("network_operation", NetworkError("Timeout")),
            ("validation", ValidationError("Invalid data"))
        ]
        
        # Collect multiple exceptions
        for name, error in operations:
            errors.append(error)
        
        if errors:
            raise ExceptionGroup("Multiple operations failed", errors)
            
    except* DatabaseError as eg:
        print(f"Database errors: {eg.exceptions}")
        
    except* NetworkError as eg:
        print(f"Network errors: {eg.exceptions}")
        
    except* ValidationError as eg:
        print(f"Validation errors: {eg.exceptions}")

# Exception notes (PEP 678)
def demonstrate_exception_notes():
    """Show how to add notes to exceptions"""
    try:
        result = 10 / 0
    except ZeroDivisionError as e:
        e.add_note("This happened in the calculation module")
        e.add_note("Check the input parameters")
        raise

# Task Groups (Python 3.11)
async def fetch_data(url: str) -> str:
    """Simulate fetching data from a URL"""
    await asyncio.sleep(0.1)
    return f"Data from {url}"

async def process_urls_with_task_group():
    """Demonstrate task groups for concurrent operations"""
    urls = ["http://example.com", "http://test.com", "http://demo.com"]
    
    async with asyncio.TaskGroup() as tg:
        tasks = [tg.create_task(fetch_data(url)) for url in urls]
    
    # All tasks completed successfully
    results = [task.result() for task in tasks]
    return results

# Structural Pattern Matching (Enhanced in Python 3.11)
def analyze_data(data):
    """Demonstrate advanced pattern matching"""
    match data:
        # Literal patterns
        case 0:
            return "Zero"
        
        # Value patterns with guards
        case x if x < 0:
            return f"Negative: {x}"
        
        # Sequence patterns
        case []:
            return "Empty list"
        
        case [x]:
            return f"Single item: {x}"
        
        case [x, y]:
            return f"Two items: {x}, {y}"
        
        case [x, *rest]:
            return f"First: {x}, Rest: {rest}"
        
        # Mapping patterns
        case {"type": "user", "name": str(name)}:
            return f"User: {name}"
        
        case {"type": "admin", "permissions": list(perms)}:
            return f"Admin with permissions: {perms}"
        
        # Class patterns
        case Builder(value=v) if v > 100:
            return f"Large builder: {v}"
        
        case Builder(value=v):
            return f"Builder: {v}"
        
        # OR patterns
        case "start" | "begin" | "init":
            return "Starting operation"
        
        # AS patterns
        case {"data": list(items)} as full_dict:
            return f"Data list with {len(items)} items in {full_dict}"
        
        # Wildcard
        case _:
            return f"Unknown data type: {type(data)}"

# TOML support (Python 3.11)
def load_config_from_toml():
    """Demonstrate built-in TOML support"""
    toml_content = """
    [database]
    host = "localhost"
    port = 5432
    name = "myapp"
    
    [api]
    version = "v1"
    timeout = 30
    
    [[servers]]
    name = "web1"
    ip = "192.168.1.10"
    
    [[servers]]
    name = "web2"
    ip = "192.168.1.11"
    """
    
    # Parse TOML using built-in library
    config = tomllib.loads(toml_content)
    return config

# Enhanced error messages (Python 3.11)
def demonstrate_enhanced_errors():
    """Examples that show improved error location tracking"""
    
    # Fine-grained error locations
    data = {"users": [{"name": "Alice"}, {"name": "Bob"}]}
    
    try:
        # This will show exactly which part failed
        result = data["users"][0]["age"]  # KeyError with precise location
    except KeyError as e:
        print(f"Key error: {e}")
    
    try:
        # Complex expression with precise error location
        value = (data["users"][2]["name"].upper() + 
                data["settings"]["theme"].lower())
    except (KeyError, IndexError) as e:
        print(f"Error in complex expression: {e}")

# Dataclass improvements (Python 3.11)
@dataclass(slots=True)  # __slots__ support
class Point:
    x: float
    y: float
    
    def distance_from_origin(self) -> float:
        return (self.x ** 2 + self.y ** 2) ** 0.5

@dataclass
class Person:
    name: str
    age: int
    email: str | None = None  # Union syntax
    
    def __post_init__(self):
        if self.age < 0:
            raise ValueError("Age cannot be negative")

# Async context managers improvements
@asynccontextmanager
async def database_transaction():
    """Async context manager for database transactions"""
    print("Starting transaction")
    try:
        yield "transaction_context"
    except Exception:
        print("Rolling back transaction")
        raise
    else:
        print("Committing transaction")
    finally:
        print("Closing connection")

# String formatting improvements
def demonstrate_string_formatting():
    """Show enhanced string formatting in Python 3.11"""
    name = "Alice"
    age = 30
    
    # F-string improvements
    formatted = f"Name: {name!r}, Age: {age:>3}"
    
    # Nested f-strings
    width = 10
    precision = 2
    value = 3.14159
    nested = f"Value: {value:{width}.{precision}f}"
    
    return formatted, nested

# Type alias statement (Python 3.11)
type Vector = list[float]
type Matrix = list[Vector]
type JSONValue = str | int | float | bool | None | list['JSONValue'] | dict[str, 'JSONValue']

def process_matrix(matrix: Matrix) -> Vector:
    """Process a matrix and return a vector"""
    return [sum(row) for row in matrix]

# Generic type improvements
class Container[T]:
    """Generic container using new syntax"""
    def __init__(self, value: T) -> None:
        self.value = value
    
    def get(self) -> T:
        return self.value
    
    def set(self, value: T) -> None:
        self.value = value

class Pair[T, U]:
    """Generic pair with two type parameters"""
    def __init__(self, first: T, second: U) -> None:
        self.first = first
        self.second = second

# Callable improvements
def higher_order_function(
    func: Callable[[int, str], bool],
    value: int,
    text: str
) -> bool:
    """Function that takes another function as parameter"""
    return func(value, text)

# Async improvements
async def async_generator_example():
    """Async generator with enhanced features"""
    for i in range(5):
        await asyncio.sleep(0.1)
        yield f"Item {i}"

async def async_comprehension_example():
    """Async comprehensions and iterations"""
    # Async list comprehension
    results = [item async for item in async_generator_example()]
    
    # Async generator expression
    squared = (int(item.split()[-1]) ** 2 async for item in async_generator_example())
    
    return results, [x async for x in squared]

# Context manager improvements
class ResourceManager:
    """Enhanced context manager with better error handling"""
    
    def __init__(self, resource_name: str):
        self.resource_name = resource_name
        self.resource = None
    
    def __enter__(self):
        print(f"Acquiring {self.resource_name}")
        self.resource = f"Resource: {self.resource_name}"
        return self.resource
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        print(f"Releasing {self.resource_name}")
        if exc_type is not None:
            print(f"Exception occurred: {exc_type.__name__}: {exc_val}")
        return False  # Don't suppress exceptions

# Union type syntax improvements
def process_value(value: str | int | float) -> str:
    """Function using union type syntax"""
    match value:
        case str():
            return f"String: {value}"
        case int():
            return f"Integer: {value}"
        case float():
            return f"Float: {value:.2f}"
        case _:
            return "Unknown type"

# Walrus operator in comprehensions
def demonstrate_walrus_operator():
    """Show walrus operator usage"""
    # In list comprehensions
    data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
    squares = [y for x in data if (y := x * x) > 10]
    
    # In while loops
    results = []
    while (line := input("Enter a line (empty to stop): ").strip()):
        results.append(line.upper())
    
    return squares, results

# Positional-only and keyword-only parameters
def complex_function(
    pos_only: int,
    /,
    pos_or_kw: str,
    *,
    kw_only: bool,
    **kwargs
) -> dict:
    """Function with various parameter types"""
    return {
        "pos_only": pos_only,
        "pos_or_kw": pos_or_kw,
        "kw_only": kw_only,
        "kwargs": kwargs
    }

# Main demonstration function
async def main():
    """Main function demonstrating Python 3.11 features"""
    print("Python 3.11 Features Demonstration")
    print("=" * 40)
    
    # Builder pattern with Self type
    builder = Builder(10)
    new_builder = builder.add(5).multiply(2)
    print(f"Builder result: {new_builder.value}")
    
    # Exception groups
    try:
        await process_data_with_exception_groups()
    except ExceptionGroup as eg:
        print(f"Caught exception group: {eg}")
    
    # Task groups
    try:
        results = await process_urls_with_task_group()
        print(f"Task group results: {results}")
    except* Exception as eg:
        print(f"Task group errors: {eg.exceptions}")
    
    # Pattern matching
    test_data = [
        42,
        -5,
        [],
        [1],
        [1, 2],
        [1, 2, 3, 4],
        {"type": "user", "name": "Alice"},
        {"type": "admin", "permissions": ["read", "write"]},
        Builder(150),
        "start",
        {"data": [1, 2, 3], "meta": "info"}
    ]
    
    for data in test_data:
        result = analyze_data(data)
        print(f"Pattern match for {data}: {result}")
    
    # TOML configuration
    config = load_config_from_toml()
    print(f"TOML config: {config}")
    
    # Enhanced error demonstration
    demonstrate_enhanced_errors()
    
    # String formatting
    formatted_strings = demonstrate_string_formatting()
    print(f"Formatted strings: {formatted_strings}")
    
    # Generic containers
    int_container = Container[int](42)
    str_container = Container[str]("Hello")
    pair = Pair[int, str](1, "one")
    
    print(f"Containers: {int_container.get()}, {str_container.get()}")
    print(f"Pair: ({pair.first}, {pair.second})")
    
    # Matrix processing
    matrix: Matrix = [[1.0, 2.0], [3.0, 4.0], [5.0, 6.0]]
    vector_result = process_matrix(matrix)
    print(f"Matrix sum: {vector_result}")
    
    # Async comprehensions
    async_results = await async_comprehension_example()
    print(f"Async comprehension results: {async_results}")
    
    # Union types
    values = ["hello", 42, 3.14]
    for value in values:
        result = process_value(value)
        print(f"Union type processing: {result}")
    
    # Complex function call
    func_result = complex_function(
        1,  # pos_only
        "test",  # pos_or_kw
        kw_only=True,
        extra="value"
    )
    print(f"Complex function result: {func_result}")
    
    # Context manager
    with ResourceManager("database") as resource:
        print(f"Using {resource}")
    
    print("\nAll Python 3.11 features demonstrated successfully!")

# Exception handling with notes
def divide_with_context(a: float, b: float) -> float:
    """Division with contextual error information"""
    try:
        return a / b
    except ZeroDivisionError as e:
        e.add_note(f"Division attempted with a={a}, b={b}")
        e.add_note("Consider checking input validation")
        raise

# Type checking improvements
def type_safe_function(items: list[str]) -> dict[str, int]:
    """Function with precise type annotations"""
    return {item: len(item) for item in items}

# Final example: Complex pattern matching with guards
def complex_pattern_matching(data):
    """Advanced pattern matching scenarios"""
    match data:
        # Nested patterns with guards
        case {"users": [{"name": str(name), "age": int(age)}]} if age >= 18:
            return f"Adult user: {name}"
        
        case {"users": [{"name": str(name), "age": int(age)}]} if age < 18:
            return f"Minor user: {name}"
        
        # Multiple patterns with OR
        case {"status": "active" | "pending" | "processing"}:
            return "System is operational"
        
        # Capture with star patterns
        case {"items": [first, *middle, last]} if len(middle) > 0:
            return f"List: first={first}, middle={middle}, last={last}"
        
        # Class patterns with attribute matching
        case Point(x=0, y=0):
            return "Origin point"
        
        case Point(x=x, y=y) if x == y:
            return f"Diagonal point: ({x}, {y})"
        
        # Mapping patterns with rest
        case {"required": value, **rest} if rest:
            return f"Required: {value}, Optional: {rest}"
        
        case _:
            return "No pattern matched"

if __name__ == "__main__":
    # Run the main demonstration
    asyncio.run(main())
    
    # Additional examples
    print("\nAdditional Examples:")
    
    # Exception with notes
    try:
        divide_with_context(10, 0)
    except ZeroDivisionError as e:
        print(f"Exception with notes: {e}")
        for note in e.__notes__:
            print(f"  Note: {note}")
    
    # Type-safe function
    words = ["hello", "world", "python"]
    word_lengths = type_safe_function(words)
    print(f"Word lengths: {word_lengths}")
    
    # Complex pattern matching
    test_cases = [
        {"users": [{"name": "Alice", "age": 25}]},
        {"users": [{"name": "Bob", "age": 16}]},
        {"status": "active"},
        {"items": [1, 2, 3, 4, 5]},
        Point(0, 0),
        Point(3, 3),
        {"required": "value", "optional1": "a", "optional2": "b"},
        "unmatched"
    ]
    
    for case in test_cases:
        result = complex_pattern_matching(case)
        print(f"Complex pattern: {case} -> {result}")

