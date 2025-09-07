/**
 * Go 1.19 Grammar Test Examples
 * These examples demonstrate various Go 1.19+ language features that should be parsed correctly
 */

package main

import (
	"context"
	"crypto/rand"
	"encoding/json"
	"errors"
	"fmt"
	"log"
	"net/http"
	"reflect"
	"runtime"
	"sync"
	"sync/atomic"
	"testing"
	"time"
)

// Go 1.19 features demonstration

// Generics (Go 1.18+, enhanced in 1.19)
type Ordered interface {
	~int | ~int8 | ~int16 | ~int32 | ~int64 |
		~uint | ~uint8 | ~uint16 | ~uint32 | ~uint64 | ~uintptr |
		~float32 | ~float64 |
		~string
}

type Comparable interface {
	comparable
}

// Generic functions
func Min[T Ordered](a, b T) T {
	if a < b {
		return a
	}
	return b
}

func Max[T Ordered](a, b T) T {
	if a > b {
		return a
	}
	return b
}

// Generic data structures
type Stack[T any] struct {
	items []T
	mu    sync.RWMutex
}

func NewStack[T any]() *Stack[T] {
	return &Stack[T]{
		items: make([]T, 0),
	}
}

func (s *Stack[T]) Push(item T) {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.items = append(s.items, item)
}

func (s *Stack[T]) Pop() (T, bool) {
	s.mu.Lock()
	defer s.mu.Unlock()
	
	var zero T
	if len(s.items) == 0 {
		return zero, false
	}
	
	item := s.items[len(s.items)-1]
	s.items = s.items[:len(s.items)-1]
	return item, true
}

func (s *Stack[T]) IsEmpty() bool {
	s.mu.RLock()
	defer s.mu.RUnlock()
	return len(s.items) == 0
}

// Generic map operations
type Map[K comparable, V any] struct {
	data map[K]V
	mu   sync.RWMutex
}

func NewMap[K comparable, V any]() *Map[K, V] {
	return &Map[K, V]{
		data: make(map[K]V),
	}
}

func (m *Map[K, V]) Set(key K, value V) {
	m.mu.Lock()
	defer m.mu.Unlock()
	m.data[key] = value
}

func (m *Map[K, V]) Get(key K) (V, bool) {
	m.mu.RLock()
	defer m.mu.RUnlock()
	value, exists := m.data[key]
	return value, exists
}

func (m *Map[K, V]) Delete(key K) {
	m.mu.Lock()
	defer m.mu.Unlock()
	delete(m.data, key)
}

func (m *Map[K, V]) Keys() []K {
	m.mu.RLock()
	defer m.mu.RUnlock()
	
	keys := make([]K, 0, len(m.data))
	for k := range m.data {
		keys = append(keys, k)
	}
	return keys
}

// Type constraints and interfaces
type Number interface {
	~int | ~int8 | ~int16 | ~int32 | ~int64 |
		~uint | ~uint8 | ~uint16 | ~uint32 | ~uint64 | ~uintptr |
		~float32 | ~float64
}

type Stringer interface {
	String() string
}

// Generic algorithms
func Map[T, U any](slice []T, fn func(T) U) []U {
	result := make([]U, len(slice))
	for i, v := range slice {
		result[i] = fn(v)
	}
	return result
}

func Filter[T any](slice []T, predicate func(T) bool) []T {
	var result []T
	for _, v := range slice {
		if predicate(v) {
			result = append(result, v)
		}
	}
	return result
}

func Reduce[T, U any](slice []T, initial U, fn func(U, T) U) U {
	result := initial
	for _, v := range slice {
		result = fn(result, v)
	}
	return result
}

// Advanced generic patterns
type Container[T any] interface {
	Add(T)
	Remove() (T, bool)
	Size() int
}

type Queue[T any] struct {
	items []T
	mu    sync.Mutex
}

func NewQueue[T any]() *Queue[T] {
	return &Queue[T]{
		items: make([]T, 0),
	}
}

func (q *Queue[T]) Add(item T) {
	q.mu.Lock()
	defer q.mu.Unlock()
	q.items = append(q.items, item)
}

func (q *Queue[T]) Remove() (T, bool) {
	q.mu.Lock()
	defer q.mu.Unlock()
	
	var zero T
	if len(q.items) == 0 {
		return zero, false
	}
	
	item := q.items[0]
	q.items = q.items[1:]
	return item, true
}

func (q *Queue[T]) Size() int {
	q.mu.Lock()
	defer q.mu.Unlock()
	return len(q.items)
}

// Atomic operations (Go 1.19 enhanced)
type AtomicCounter struct {
	value atomic.Int64
}

func NewAtomicCounter() *AtomicCounter {
	return &AtomicCounter{}
}

func (c *AtomicCounter) Increment() int64 {
	return c.value.Add(1)
}

func (c *AtomicCounter) Decrement() int64 {
	return c.value.Add(-1)
}

func (c *AtomicCounter) Get() int64 {
	return c.value.Load()
}

func (c *AtomicCounter) Set(value int64) {
	c.value.Store(value)
}

func (c *AtomicCounter) CompareAndSwap(old, new int64) bool {
	return c.value.CompareAndSwap(old, new)
}

// Atomic pointer operations
type AtomicPointer[T any] struct {
	ptr atomic.Pointer[T]
}

func NewAtomicPointer[T any]() *AtomicPointer[T] {
	return &AtomicPointer[T]{}
}

func (ap *AtomicPointer[T]) Store(value *T) {
	ap.ptr.Store(value)
}

func (ap *AtomicPointer[T]) Load() *T {
	return ap.ptr.Load()
}

func (ap *AtomicPointer[T]) CompareAndSwap(old, new *T) bool {
	return ap.ptr.CompareAndSwap(old, new)
}

func (ap *AtomicPointer[T]) Swap(new *T) *T {
	return ap.ptr.Swap(new)
}

// Concurrency patterns
func WorkerPool[T any](jobs <-chan T, workers int, process func(T)) {
	var wg sync.WaitGroup
	
	for i := 0; i < workers; i++ {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for job := range jobs {
				process(job)
			}
		}()
	}
	
	wg.Wait()
}

// Fan-in pattern
func FanIn[T any](channels ...<-chan T) <-chan T {
	out := make(chan T)
	var wg sync.WaitGroup
	
	for _, ch := range channels {
		wg.Add(1)
		go func(c <-chan T) {
			defer wg.Done()
			for v := range c {
				out <- v
			}
		}(ch)
	}
	
	go func() {
		wg.Wait()
		close(out)
	}()
	
	return out
}

// Fan-out pattern
func FanOut[T any](in <-chan T, workers int) []<-chan T {
	channels := make([]<-chan T, workers)
	
	for i := 0; i < workers; i++ {
		ch := make(chan T)
		channels[i] = ch
		
		go func(out chan<- T, index int) {
			defer close(out)
			for v := range in {
				if hash(v)%workers == index {
					out <- v
				}
			}
		}(ch, i)
	}
	
	return channels
}

// Simple hash function for demonstration
func hash[T any](v T) int {
	return int(reflect.ValueOf(v).Pointer()) % 1000
}

// Pipeline pattern
func Pipeline[T, U, V any](
	input <-chan T,
	stage1 func(T) U,
	stage2 func(U) V,
) <-chan V {
	// Stage 1
	intermediate := make(chan U)
	go func() {
		defer close(intermediate)
		for v := range input {
			intermediate <- stage1(v)
		}
	}()
	
	// Stage 2
	output := make(chan V)
	go func() {
		defer close(output)
		for v := range intermediate {
			output <- stage2(v)
		}
	}()
	
	return output
}

// Context usage patterns
func ContextExample() {
	// Context with timeout
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()
	
	// Context with value
	ctx = context.WithValue(ctx, "requestID", "12345")
	
	// Context with cancellation
	ctx, cancel = context.WithCancel(ctx)
	defer cancel()
	
	// Use context in goroutine
	go func(ctx context.Context) {
		select {
		case <-time.After(10 * time.Second):
			fmt.Println("Work completed")
		case <-ctx.Done():
			fmt.Printf("Work cancelled: %v\n", ctx.Err())
		}
	}(ctx)
	
	// Simulate work
	time.Sleep(1 * time.Second)
	cancel() // Cancel the work
}

// Error handling patterns
type CustomError struct {
	Code    int
	Message string
	Cause   error
}

func (e *CustomError) Error() string {
	return fmt.Sprintf("error %d: %s", e.Code, e.Message)
}

func (e *CustomError) Unwrap() error {
	return e.Cause
}

func ErrorHandlingExample() {
	// Error wrapping
	originalErr := errors.New("original error")
	wrappedErr := fmt.Errorf("wrapped: %w", originalErr)
	
	// Error unwrapping
	unwrapped := errors.Unwrap(wrappedErr)
	fmt.Printf("Unwrapped: %v\n", unwrapped)
	
	// Error checking
	if errors.Is(wrappedErr, originalErr) {
		fmt.Println("Error is original error")
	}
	
	// Error type assertion
	var customErr *CustomError
	if errors.As(wrappedErr, &customErr) {
		fmt.Printf("Custom error: %v\n", customErr)
	}
	
	// Custom error with cause
	customErr = &CustomError{
		Code:    500,
		Message: "internal server error",
		Cause:   originalErr,
	}
	
	fmt.Printf("Custom error: %v\n", customErr)
	fmt.Printf("Cause: %v\n", customErr.Unwrap())
}

// JSON handling
type Person struct {
	Name    string    `json:"name"`
	Age     int       `json:"age"`
	Email   string    `json:"email,omitempty"`
	Created time.Time `json:"created"`
}

func JSONExample() {
	person := Person{
		Name:    "John Doe",
		Age:     30,
		Email:   "john@example.com",
		Created: time.Now(),
	}
	
	// Marshal to JSON
	data, err := json.Marshal(person)
	if err != nil {
		log.Printf("Error marshaling JSON: %v", err)
		return
	}
	
	fmt.Printf("JSON: %s\n", data)
	
	// Unmarshal from JSON
	var decoded Person
	err = json.Unmarshal(data, &decoded)
	if err != nil {
		log.Printf("Error unmarshaling JSON: %v", err)
		return
	}
	
	fmt.Printf("Decoded: %+v\n", decoded)
}

// HTTP server example
func HTTPServerExample() {
	// Handler function
	handler := func(w http.ResponseWriter, r *http.Request) {
		person := Person{
			Name:    "API User",
			Age:     25,
			Email:   "api@example.com",
			Created: time.Now(),
		}
		
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(person)
	}
	
	// Register handler
	http.HandleFunc("/api/person", handler)
	
	// Start server (commented out for testing)
	// log.Println("Server starting on :8080")
	// log.Fatal(http.ListenAndServe(":8080", nil))
}

// Reflection example
func ReflectionExample() {
	person := Person{
		Name:    "Reflection Test",
		Age:     35,
		Email:   "reflect@example.com",
		Created: time.Now(),
	}
	
	// Get type information
	t := reflect.TypeOf(person)
	fmt.Printf("Type: %s\n", t.Name())
	
	// Get value information
	v := reflect.ValueOf(person)
	fmt.Printf("Value: %v\n", v)
	
	// Iterate over fields
	for i := 0; i < t.NumField(); i++ {
		field := t.Field(i)
		value := v.Field(i)
		fmt.Printf("Field: %s, Type: %s, Value: %v\n", 
			field.Name, field.Type, value.Interface())
	}
}

// Memory management and GC
func MemoryExample() {
	// Get memory statistics
	var m runtime.MemStats
	runtime.ReadMemStats(&m)
	
	fmt.Printf("Allocated memory: %d KB\n", m.Alloc/1024)
	fmt.Printf("Total allocations: %d\n", m.TotalAlloc/1024)
	fmt.Printf("System memory: %d KB\n", m.Sys/1024)
	fmt.Printf("Number of GC cycles: %d\n", m.NumGC)
	
	// Force garbage collection
	runtime.GC()
	
	// Read stats again
	runtime.ReadMemStats(&m)
	fmt.Printf("After GC - Allocated memory: %d KB\n", m.Alloc/1024)
}

// Cryptographic random numbers
func CryptoExample() {
	// Generate random bytes
	randomBytes := make([]byte, 32)
	_, err := rand.Read(randomBytes)
	if err != nil {
		log.Printf("Error generating random bytes: %v", err)
		return
	}
	
	fmt.Printf("Random bytes: %x\n", randomBytes)
	
	// Generate random integer
	randomInt := int64(0)
	for i := 0; i < 8; i++ {
		randomInt = (randomInt << 8) | int64(randomBytes[i])
	}
	
	fmt.Printf("Random integer: %d\n", randomInt)
}

// Testing examples
func TestExample(t *testing.T) {
	// Test generic functions
	result := Min(5, 3)
	if result != 3 {
		t.Errorf("Expected 3, got %d", result)
	}
	
	// Test generic data structures
	stack := NewStack[int]()
	stack.Push(1)
	stack.Push(2)
	stack.Push(3)
	
	value, ok := stack.Pop()
	if !ok || value != 3 {
		t.Errorf("Expected 3, got %d", value)
	}
}

func BenchmarkStack(b *testing.B) {
	stack := NewStack[int]()
	
	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		stack.Push(i)
		stack.Pop()
	}
}

func ExampleMin() {
	result := Min(10, 5)
	fmt.Println(result)
	// Output: 5
}

// Fuzzing example (Go 1.18+)
func FuzzMin(f *testing.F) {
	f.Add(1, 2)
	f.Add(5, 3)
	f.Add(-1, -2)
	
	f.Fuzz(func(t *testing.T, a, b int) {
		result := Min(a, b)
		if a <= b && result != a {
			t.Errorf("Min(%d, %d) = %d, expected %d", a, b, result, a)
		}
		if b < a && result != b {
			t.Errorf("Min(%d, %d) = %d, expected %d", a, b, result, b)
		}
	})
}

// Advanced interface usage
type Shape interface {
	Area() float64
	Perimeter() float64
}

type Circle struct {
	Radius float64
}

func (c Circle) Area() float64 {
	return 3.14159 * c.Radius * c.Radius
}

func (c Circle) Perimeter() float64 {
	return 2 * 3.14159 * c.Radius
}

type Rectangle struct {
	Width, Height float64
}

func (r Rectangle) Area() float64 {
	return r.Width * r.Height
}

func (r Rectangle) Perimeter() float64 {
	return 2 * (r.Width + r.Height)
}

// Generic shape operations
func CalculateShapeMetrics[T Shape](shapes []T) (totalArea, totalPerimeter float64) {
	for _, shape := range shapes {
		totalArea += shape.Area()
		totalPerimeter += shape.Perimeter()
	}
	return
}

// Type switches with generics
func ProcessValue[T any](value T) string {
	switch v := any(value).(type) {
	case string:
		return fmt.Sprintf("String: %s", v)
	case int:
		return fmt.Sprintf("Integer: %d", v)
	case float64:
		return fmt.Sprintf("Float: %.2f", v)
	case bool:
		return fmt.Sprintf("Boolean: %t", v)
	default:
		return fmt.Sprintf("Unknown type: %T", v)
	}
}

// Channel operations
func ChannelExample() {
	// Buffered channel
	ch := make(chan int, 10)
	
	// Send values
	go func() {
		for i := 0; i < 5; i++ {
			ch <- i
		}
		close(ch)
	}()
	
	// Receive values
	for value := range ch {
		fmt.Printf("Received: %d\n", value)
	}
	
	// Select statement
	ch1 := make(chan string)
	ch2 := make(chan string)
	
	go func() {
		time.Sleep(1 * time.Second)
		ch1 <- "from ch1"
	}()
	
	go func() {
		time.Sleep(2 * time.Second)
		ch2 <- "from ch2"
	}()
	
	for i := 0; i < 2; i++ {
		select {
		case msg1 := <-ch1:
			fmt.Println("Received", msg1)
		case msg2 := <-ch2:
			fmt.Println("Received", msg2)
		case <-time.After(3 * time.Second):
			fmt.Println("Timeout")
		}
	}
}

// Defer and panic/recover
func DeferPanicRecoverExample() {
	defer func() {
		if r := recover(); r != nil {
			fmt.Printf("Recovered from panic: %v\n", r)
		}
	}()
	
	defer fmt.Println("This will be printed first")
	defer fmt.Println("This will be printed second")
	
	fmt.Println("About to panic")
	panic("Something went wrong!")
}

// Method sets and embedding
type Writer interface {
	Write([]byte) (int, error)
}

type Logger struct {
	prefix string
}

func (l *Logger) Write(data []byte) (int, error) {
	prefixed := fmt.Sprintf("[%s] %s", l.prefix, string(data))
	return fmt.Print(prefixed)
}

type FileLogger struct {
	Logger
	filename string
}

func (fl *FileLogger) WriteToFile(data []byte) error {
	// Simulate writing to file
	fmt.Printf("Writing to file %s: %s", fl.filename, string(data))
	return nil
}

// Constants and iota
const (
	StatusPending = iota
	StatusRunning
	StatusCompleted
	StatusFailed
)

const (
	KB = 1024
	MB = KB * 1024
	GB = MB * 1024
	TB = GB * 1024
)

// Type aliases and definitions
type UserID int64
type Email string
type Timestamp int64

type User struct {
	ID       UserID    `json:"id"`
	Email    Email     `json:"email"`
	Created  Timestamp `json:"created"`
	Active   bool      `json:"active"`
}

// Methods on custom types
func (u UserID) String() string {
	return fmt.Sprintf("user-%d", int64(u))
}

func (e Email) IsValid() bool {
	return len(string(e)) > 0 && 
		   len(string(e)) < 255 && 
		   fmt.Sprintf("%s", e) != ""
}

func (t Timestamp) Time() time.Time {
	return time.Unix(int64(t), 0)
}

// Main function demonstrating all features
func main() {
	fmt.Println("=== Go 1.19 Features Demonstration ===")
	
	// Generic functions
	fmt.Printf("Min(10, 5) = %d\n", Min(10, 5))
	fmt.Printf("Max(3.14, 2.71) = %.2f\n", Max(3.14, 2.71))
	
	// Generic data structures
	stack := NewStack[string]()
	stack.Push("hello")
	stack.Push("world")
	
	if value, ok := stack.Pop(); ok {
		fmt.Printf("Popped from stack: %s\n", value)
	}
	
	// Generic map
	userMap := NewMap[UserID, User]()
	user := User{
		ID:      UserID(1),
		Email:   Email("user@example.com"),
		Created: Timestamp(time.Now().Unix()),
		Active:  true,
	}
	userMap.Set(user.ID, user)
	
	if retrievedUser, exists := userMap.Get(UserID(1)); exists {
		fmt.Printf("Retrieved user: %+v\n", retrievedUser)
	}
	
	// Atomic operations
	counter := NewAtomicCounter()
	counter.Increment()
	counter.Increment()
	fmt.Printf("Counter value: %d\n", counter.Get())
	
	// Context example
	ContextExample()
	
	// Error handling
	ErrorHandlingExample()
	
	// JSON handling
	JSONExample()
	
	// Reflection
	ReflectionExample()
	
	// Memory statistics
	MemoryExample()
	
	// Cryptographic random
	CryptoExample()
	
	// Shape calculations
	shapes := []Shape{
		Circle{Radius: 5},
		Rectangle{Width: 10, Height: 5},
	}
	
	totalArea, totalPerimeter := CalculateShapeMetrics(shapes)
	fmt.Printf("Total area: %.2f, Total perimeter: %.2f\n", totalArea, totalPerimeter)
	
	// Type processing
	fmt.Println(ProcessValue("hello"))
	fmt.Println(ProcessValue(42))
	fmt.Println(ProcessValue(3.14))
	fmt.Println(ProcessValue(true))
	
	// Channel operations
	ChannelExample()
	
	// Defer, panic, recover
	DeferPanicRecoverExample()
	
	// Custom types
	userID := UserID(123)
	email := Email("test@example.com")
	timestamp := Timestamp(time.Now().Unix())
	
	fmt.Printf("UserID: %s\n", userID.String())
	fmt.Printf("Email valid: %t\n", email.IsValid())
	fmt.Printf("Timestamp: %s\n", timestamp.Time().Format(time.RFC3339))
	
	// Generic algorithms
	numbers := []int{1, 2, 3, 4, 5}
	doubled := Map(numbers, func(n int) int { return n * 2 })
	fmt.Printf("Doubled: %v\n", doubled)
	
	evens := Filter(numbers, func(n int) bool { return n%2 == 0 })
	fmt.Printf("Evens: %v\n", evens)
	
	sum := Reduce(numbers, 0, func(acc, n int) int { return acc + n })
	fmt.Printf("Sum: %d\n", sum)
	
	// Worker pool example
	jobs := make(chan int, 100)
	go func() {
		for i := 0; i < 10; i++ {
			jobs <- i
		}
		close(jobs)
	}()
	
	WorkerPool(jobs, 3, func(job int) {
		fmt.Printf("Processing job %d\n", job)
		time.Sleep(100 * time.Millisecond)
	})
	
	fmt.Println("=== Go 1.19 Features Demo Complete ===")
}

// Additional advanced features
func init() {
	fmt.Println("Package initialized")
}

// Build constraints example
//go:build unix

// Go generate example
//go:generate go run generate.go

// Benchmark for performance testing
func BenchmarkGenericMap(b *testing.B) {
	data := make([]int, 1000)
	for i := range data {
		data[i] = i
	}
	
	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		_ = Map(data, func(n int) int { return n * 2 })
	}
}

// Example of workspace features
// go.work file would contain:
// go 1.19
// use ./module1
// use ./module2

// Example of module features  
// go.mod file would contain:
// module example.com/go119demo
// go 1.19
// require (
//     github.com/some/dependency v1.2.3
// )

// Performance optimizations demonstration
func PerformanceExample() {
	// Jump table optimization for large switch statements
	value := 42
	
	switch value {
	case 1, 2, 3, 4, 5:
		fmt.Println("Small numbers")
	case 10, 20, 30, 40, 50:
		fmt.Println("Multiples of 10")
	case 42:
		fmt.Println("The answer to everything")
	case 100, 200, 300, 400, 500:
		fmt.Println("Hundreds")
	default:
		fmt.Println("Other number")
	}
	
	// Soft memory limit (would be set via GOMEMLIMIT environment variable)
	// This demonstrates the runtime's ability to respect memory limits
	var memStats runtime.MemStats
	runtime.ReadMemStats(&memStats)
	fmt.Printf("Current memory usage: %d bytes\n", memStats.Alloc)
}

// Documentation comments with enhanced formatting
// This function demonstrates the new doc comment features in Go 1.19.
//
// It supports:
//   - Lists with proper formatting
//   - Links to other functions like [Min] and [Max]
//   - Code blocks and examples
//
// Example usage:
//
//	result := DocumentedFunction(42)
//	fmt.Println(result)
//
// For more information, see the Go documentation at https://go.dev/doc/
func DocumentedFunction(input int) int {
	return input * 2
}

