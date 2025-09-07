/*
 * C++20 Grammar Test Examples
 * These examples demonstrate various C++20 language features that should be parsed correctly
 */

#include <iostream>
#include <vector>
#include <concepts>
#include <ranges>
#include <coroutine>
#include <format>

// C++20 Modules (if supported)
// export module math;

// C++20 Concepts
template<typename T>
concept Integral = std::is_integral_v<T>;

template<typename T>
concept Addable = requires(T a, T b) {
    a + b;
};

template<Integral T>
T add(T a, T b) {
    return a + b;
}

// C++20 Requires expressions
template<typename T>
requires Addable<T>
auto safe_add(T a, T b) -> T {
    return a + b;
}

// C++20 Abbreviated function templates
auto multiply(Integral auto a, Integral auto b) {
    return a * b;
}

// C++20 Class template argument deduction for aliases
template<typename T>
using Vec = std::vector<T>;

// C++20 Three-way comparison operator
#include <compare>

struct Point {
    int x, y;
    
    auto operator<=>(const Point& other) const = default;
};

// C++20 Designated initializers
struct Config {
    int width = 800;
    int height = 600;
    bool fullscreen = false;
};

void designated_init_example() {
    Config cfg{.width = 1920, .height = 1080, .fullscreen = true};
}

// C++20 Lambda improvements
void lambda_examples() {
    // Template lambdas
    auto generic_lambda = []<typename T>(T value) {
        return value * 2;
    };
    
    // Pack expansion in lambda init capture
    auto pack_lambda = [](auto... args) {
        return [args...] { return (args + ...); };
    };
    
    // Default constructible and assignable stateless lambdas
    auto stateless = [](int x) { return x * x; };
    decltype(stateless) another_lambda;
}

// C++20 Coroutines
#include <coroutine>

struct Generator {
    struct promise_type {
        int current_value;
        
        Generator get_return_object() {
            return Generator{std::coroutine_handle<promise_type>::from_promise(*this)};
        }
        
        std::suspend_always initial_suspend() { return {}; }
        std::suspend_always final_suspend() noexcept { return {}; }
        void unhandled_exception() {}
        
        std::suspend_always yield_value(int value) {
            current_value = value;
            return {};
        }
        
        void return_void() {}
    };
    
    std::coroutine_handle<promise_type> h;
    
    Generator(std::coroutine_handle<promise_type> handle) : h(handle) {}
    ~Generator() { if (h) h.destroy(); }
    
    bool next() {
        h.resume();
        return !h.done();
    }
    
    int value() {
        return h.promise().current_value;
    }
};

Generator fibonacci() {
    int a = 0, b = 1;
    while (true) {
        co_yield a;
        auto temp = a;
        a = b;
        b = temp + b;
    }
}

// C++20 Ranges
#include <ranges>
#include <algorithm>

void ranges_example() {
    std::vector<int> numbers{1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
    
    // Range-based algorithms
    auto even_numbers = numbers 
        | std::views::filter([](int n) { return n % 2 == 0; })
        | std::views::transform([](int n) { return n * n; });
    
    // Range-based for loop with ranges
    for (auto n : even_numbers) {
        std::cout << n << " ";
    }
}

// C++20 consteval and constinit
consteval int compile_time_factorial(int n) {
    return n <= 1 ? 1 : n * compile_time_factorial(n - 1);
}

constinit int global_value = compile_time_factorial(5);

// C++20 using enum
enum class Color { Red, Green, Blue };

void using_enum_example() {
    using enum Color;
    Color c = Red;  // No need for Color::Red
}

// C++20 char8_t
void char8_example() {
    char8_t utf8_char = u8'A';
    const char8_t* utf8_string = u8"Hello, UTF-8!";
}

// C++20 Bit operations
#include <bit>

void bit_operations() {
    unsigned int value = 42;
    
    // Bit counting
    int pop_count = std::popcount(value);
    int leading_zeros = std::countl_zero(value);
    int trailing_zeros = std::countr_zero(value);
    
    // Bit rotation
    auto rotated_left = std::rotl(value, 2);
    auto rotated_right = std::rotr(value, 2);
    
    // Power of 2 operations
    bool is_power_of_2 = std::has_single_bit(value);
    auto next_power_of_2 = std::bit_ceil(value);
}

// C++20 Mathematical constants
#include <numbers>

void math_constants() {
    double pi = std::numbers::pi;
    double e = std::numbers::e;
    double sqrt2 = std::numbers::sqrt2;
}

// C++20 Format library
#include <format>

void format_example() {
    std::string name = "World";
    int number = 42;
    
    std::string formatted = std::format("Hello, {}! The answer is {}.", name, number);
    std::cout << formatted << std::endl;
}

// C++20 Calendar and timezone
#include <chrono>

void chrono_example() {
    using namespace std::chrono;
    
    // Calendar types
    auto today = year_month_day{year{2023}, month{12}, day{25}};
    auto christmas = December/25/2023;
    
    // Time zones (if available)
    // auto tz = locate_zone("America/New_York");
}

// C++20 Span
#include <span>

void span_example(std::span<int> data) {
    for (auto& element : data) {
        element *= 2;
    }
}

// C++20 Source location
#include <source_location>

void log_function(const std::string& message, 
                 const std::source_location& location = std::source_location::current()) {
    std::cout << "File: " << location.file_name() 
              << " Line: " << location.line() 
              << " Function: " << location.function_name() 
              << " Message: " << message << std::endl;
}

// C++20 Atomic operations
#include <atomic>

void atomic_example() {
    std::atomic<int> counter{0};
    
    // Atomic operations
    counter.fetch_add(1);
    counter.compare_exchange_weak(counter, 10);
    
    // Atomic wait/notify
    counter.wait(0);
    counter.notify_one();
}

// C++20 Jthread
#include <thread>

void jthread_example() {
    std::jthread worker([](std::stop_token token) {
        while (!token.stop_requested()) {
            // Do work
            std::this_thread::sleep_for(std::chrono::milliseconds(100));
        }
    });
    
    // Automatic joining and stopping
}

// C++20 Barrier and Latch
#include <barrier>
#include <latch>

void synchronization_example() {
    std::latch work_done{3};
    std::barrier sync_point{3};
    
    // Use in multithreaded context
}

// C++20 Template parameter lists for lambdas
void template_lambda_example() {
    auto lambda = []<typename T, int N>(std::array<T, N> arr) {
        return arr.size();
    };
    
    std::array<int, 5> arr{1, 2, 3, 4, 5};
    auto size = lambda(arr);
}

// C++20 Class types in non-type template parameters
template<auto Value>
struct ValueHolder {
    static constexpr auto value = Value;
};

struct Point3D {
    int x, y, z;
    constexpr Point3D(int x, int y, int z) : x(x), y(y), z(z) {}
};

void nttp_class_example() {
    constexpr Point3D origin{0, 0, 0};
    ValueHolder<origin> holder;
}

// C++20 Aggregate initialization improvements
struct Base {
    int x;
};

struct Derived : Base {
    int y;
};

void aggregate_example() {
    Derived d{{42}, 24};  // Aggregate initialization with base class
}

// C++20 Feature test macros
void feature_test_example() {
    #ifdef __cpp_concepts
        // Concepts are available
    #endif
    
    #ifdef __cpp_coroutines
        // Coroutines are available
    #endif
    
    #ifdef __cpp_modules
        // Modules are available
    #endif
}

// C++20 constexpr improvements
#include <vector>
#include <algorithm>

constexpr std::vector<int> constexpr_vector_example() {
    std::vector<int> vec{1, 2, 3, 4, 5};
    std::sort(vec.begin(), vec.end(), std::greater<int>());
    return vec;
}

// C++20 Immediate functions (consteval)
consteval int immediate_function(int x) {
    return x * x;
}

void consteval_example() {
    constexpr int result = immediate_function(5);  // Evaluated at compile time
}

// C++20 std::is_constant_evaluated
constexpr int conditional_computation(int x) {
    if (std::is_constant_evaluated()) {
        // Compile-time computation
        return x * x;
    } else {
        // Runtime computation
        return x + x;
    }
}

// Main function demonstrating various features
int main() {
    // Concepts
    auto result1 = add(5, 3);
    auto result2 = safe_add(10, 20);
    auto result3 = multiply(4, 7);
    
    // Designated initializers
    designated_init_example();
    
    // Lambda improvements
    lambda_examples();
    
    // Coroutines
    auto gen = fibonacci();
    for (int i = 0; i < 10; ++i) {
        if (gen.next()) {
            std::cout << gen.value() << " ";
        }
    }
    
    // Ranges
    ranges_example();
    
    // Three-way comparison
    Point p1{1, 2};
    Point p2{3, 4};
    auto cmp = p1 <=> p2;
    
    // Format library
    format_example();
    
    // Span
    std::vector<int> data{1, 2, 3, 4, 5};
    span_example(data);
    
    // Source location
    log_function("This is a test message");
    
    return 0;
}

