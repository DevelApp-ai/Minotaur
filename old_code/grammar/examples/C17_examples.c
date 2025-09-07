/*
 * C17 Grammar Test Examples
 * These examples demonstrate various C17 language features that should be parsed correctly
 */

// Basic function definition
int main(void) {
    return 0;
}

// Variable declarations with different storage classes
static int global_var = 42;
extern double external_var;
_Thread_local int thread_var;

// Function with parameters
int add(int a, int b) {
    return a + b;
}

// Struct definition
struct Point {
    double x, y;
    int id;
};

// Union definition
union Data {
    int i;
    float f;
    char str[20];
};

// Enum definition
enum Color {
    RED = 1,
    GREEN,
    BLUE
};

// Function with complex types
struct Point* create_point(double x, double y) {
    struct Point* p = malloc(sizeof(struct Point));
    if (p != NULL) {
        p->x = x;
        p->y = y;
        p->id = 0;
    }
    return p;
}

// Array declarations
int numbers[10];
char matrix[3][4];
int *ptr_array[5];

// Function pointers
int (*operation)(int, int);
void (*callback)(void);

// Control structures
void control_examples(int n) {
    // If-else
    if (n > 0) {
        printf("Positive\n");
    } else if (n < 0) {
        printf("Negative\n");
    } else {
        printf("Zero\n");
    }
    
    // Switch
    switch (n % 3) {
        case 0:
            printf("Divisible by 3\n");
            break;
        case 1:
            printf("Remainder 1\n");
            break;
        default:
            printf("Remainder 2\n");
            break;
    }
    
    // Loops
    for (int i = 0; i < n; i++) {
        printf("%d ", i);
    }
    
    int j = 0;
    while (j < n) {
        j++;
    }
    
    do {
        n--;
    } while (n > 0);
}

// C11/C17 specific features
_Static_assert(sizeof(int) >= 4, "int must be at least 4 bytes");

// Atomic operations (C11)
#include <stdatomic.h>
_Atomic int atomic_counter = 0;

void atomic_example(void) {
    atomic_fetch_add(&atomic_counter, 1);
}

// Generic selection (C11)
#define TYPE_NAME(x) _Generic((x), \
    int: "int", \
    float: "float", \
    double: "double", \
    default: "unknown")

// Alignment specifiers (C11)
_Alignas(16) char aligned_buffer[64];

// Thread-local storage (C11)
_Thread_local int tls_var = 0;

// Complex numbers (C99/C11)
#include <complex.h>
double complex z = 1.0 + 2.0*I;

// Variable length arrays (C99)
void vla_example(int n) {
    int vla[n];
    for (int i = 0; i < n; i++) {
        vla[i] = i * i;
    }
}

// Designated initializers (C99)
struct Point origin = {.x = 0.0, .y = 0.0, .id = 1};
int sparse_array[100] = {[10] = 1, [20] = 2, [30] = 3};

// Compound literals (C99)
void compound_literal_example(void) {
    struct Point *p = &(struct Point){.x = 1.0, .y = 2.0, .id = 42};
    int *arr = (int[]){1, 2, 3, 4, 5};
}

// Flexible array members (C99)
struct Buffer {
    size_t size;
    char data[];
};

// Inline functions (C99)
inline int square(int x) {
    return x * x;
}

// Restrict qualifier (C99)
void copy_array(int * restrict dest, const int * restrict src, size_t n) {
    for (size_t i = 0; i < n; i++) {
        dest[i] = src[i];
    }
}

// Mixed declarations and statements (C99)
void mixed_example(void) {
    int a = 1;
    printf("a = %d\n", a);
    int b = 2;  // Declaration after statement
    printf("b = %d\n", b);
}

// Preprocessor features
#define MAX(a, b) ((a) > (b) ? (a) : (b))
#define STRINGIFY(x) #x
#define CONCAT(a, b) a ## b

// Function-like macros with variadic arguments
#define DEBUG_PRINT(fmt, ...) printf("[DEBUG] " fmt "\n", ##__VA_ARGS__)

// Standard library usage examples
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>

void library_examples(void) {
    // String operations
    char buffer[100];
    strcpy(buffer, "Hello, ");
    strcat(buffer, "World!");
    
    // Memory allocation
    int *dynamic_array = malloc(10 * sizeof(int));
    if (dynamic_array != NULL) {
        for (int i = 0; i < 10; i++) {
            dynamic_array[i] = i * i;
        }
        free(dynamic_array);
    }
    
    // Math functions
    double result = sqrt(16.0);
    double angle = sin(M_PI / 4);
}

// Error handling patterns
int divide_safe(int a, int b, int *result) {
    if (b == 0) {
        return -1;  // Error: division by zero
    }
    *result = a / b;
    return 0;  // Success
}

// Bit manipulation
void bit_operations(unsigned int value) {
    unsigned int mask = 0xFF;
    
    // Set bit
    value |= (1 << 3);
    
    // Clear bit
    value &= ~(1 << 3);
    
    // Toggle bit
    value ^= (1 << 3);
    
    // Check bit
    if (value & (1 << 3)) {
        printf("Bit 3 is set\n");
    }
    
    // Shift operations
    unsigned int left_shifted = value << 2;
    unsigned int right_shifted = value >> 2;
}

