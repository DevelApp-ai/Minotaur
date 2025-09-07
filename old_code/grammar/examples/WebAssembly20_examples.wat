;; WebAssembly 2.0 Grammar Test Examples
;; These examples demonstrate various WebAssembly 2.0+ language features that should be parsed correctly

;; Basic module structure
(module
  ;; Type definitions
  (type $add_type (func (param i32 i32) (result i32)))
  (type $callback_type (func (param i32) (result i32)))
  
  ;; Memory and table declarations
  (memory $mem 1 10)
  (table $table 10 funcref)
  
  ;; Global variables
  (global $counter (mut i32) (i32.const 0))
  (global $pi f64 (f64.const 3.14159))
  
  ;; Function imports
  (import "env" "print" (func $print (param i32)))
  (import "env" "memory" (memory 1))
  (import "env" "table" (table 10 funcref))
  
  ;; Basic arithmetic function
  (func $add (type $add_type)
    local.get 0
    local.get 1
    i32.add
  )
  
  ;; Function with local variables
  (func $factorial (param $n i32) (result i32)
    (local $result i32)
    (local $i i32)
    
    i32.const 1
    local.set $result
    
    i32.const 1
    local.set $i
    
    (loop $loop
      local.get $i
      local.get $n
      i32.le_s
      (if
        (then
          local.get $result
          local.get $i
          i32.mul
          local.set $result
          
          local.get $i
          i32.const 1
          i32.add
          local.set $i
          
          br $loop
        )
      )
    )
    
    local.get $result
  )
  
  ;; Control flow examples
  (func $control_flow (param $x i32) (result i32)
    local.get $x
    i32.const 0
    i32.eq
    (if (result i32)
      (then i32.const 1)
      (else
        local.get $x
        i32.const 1
        i32.sub
        call $factorial
        local.get $x
        i32.mul
      )
    )
  )
  
  ;; Memory operations
  (func $memory_ops (param $addr i32) (param $value i32)
    ;; Store value at address
    local.get $addr
    local.get $value
    i32.store
    
    ;; Load value from address
    local.get $addr
    i32.load
    drop
    
    ;; Store byte
    local.get $addr
    local.get $value
    i32.store8
    
    ;; Load byte (signed)
    local.get $addr
    i32.load8_s
    drop
    
    ;; Load byte (unsigned)
    local.get $addr
    i32.load8_u
    drop
  )
  
  ;; Table operations
  (func $table_ops (param $index i32) (param $func_ref funcref)
    ;; Set table element
    local.get $index
    local.get $func_ref
    table.set $table
    
    ;; Get table element
    local.get $index
    table.get $table
    drop
    
    ;; Get table size
    table.size $table
    drop
    
    ;; Grow table
    local.get $func_ref
    i32.const 5
    table.grow $table
    drop
  )
  
  ;; Global operations
  (func $global_ops
    ;; Get global value
    global.get $counter
    drop
    
    ;; Set global value
    global.get $counter
    i32.const 1
    i32.add
    global.set $counter
    
    ;; Get immutable global
    global.get $pi
    drop
  )
  
  ;; Export functions
  (export "add" (func $add))
  (export "factorial" (func $factorial))
  (export "memory" (memory $mem))
  (export "table" (table $table))
  (export "counter" (global $counter))
)

;; WebAssembly 2.0 Reference Types Example
(module
  ;; Reference type declarations
  (type $struct_type (struct (field i32) (field f64)))
  (type $array_type (array (mut i32)))
  
  ;; Function with reference types
  (func $ref_types_demo (param $ref anyref) (result anyref)
    ;; Null reference
    ref.null any
    drop
    
    ;; Check if reference is null
    local.get $ref
    ref.is_null
    (if
      (then
        ;; Return null reference
        ref.null any
        return
      )
    )
    
    ;; Function reference
    ref.func $ref_types_demo
    drop
    
    ;; Return the input reference
    local.get $ref
  )
  
  ;; Reference equality
  (func $ref_equality (param $ref1 anyref) (param $ref2 anyref) (result i32)
    local.get $ref1
    local.get $ref2
    ref.eq
  )
  
  ;; Reference casting and testing
  (func $ref_cast_test (param $ref anyref) (result i32)
    ;; Test if reference is of specific type
    local.get $ref
    ref.test (ref $struct_type)
    (if (result i32)
      (then
        ;; Cast reference to specific type
        local.get $ref
        ref.cast (ref $struct_type)
        drop
        i32.const 1
      )
      (else
        i32.const 0
      )
    )
  )
  
  (export "ref_demo" (func $ref_types_demo))
  (export "ref_eq" (func $ref_equality))
  (export "ref_cast" (func $ref_cast_test))
)

;; WebAssembly 2.0 Garbage Collection Example
(module
  ;; Struct type definition
  (type $point (struct 
    (field $x (mut f64))
    (field $y (mut f64))
  ))
  
  ;; Array type definition
  (type $int_array (array (mut i32)))
  
  ;; Function to create a point
  (func $make_point (param $x f64) (param $y f64) (result (ref $point))
    local.get $x
    local.get $y
    struct.new $point
  )
  
  ;; Function to get point coordinates
  (func $get_point_x (param $point (ref $point)) (result f64)
    local.get $point
    struct.get $point $x
  )
  
  (func $get_point_y (param $point (ref $point)) (result f64)
    local.get $point
    struct.get $point $y
  )
  
  ;; Function to set point coordinates
  (func $set_point_x (param $point (ref $point)) (param $x f64)
    local.get $point
    local.get $x
    struct.set $point $x
  )
  
  (func $set_point_y (param $point (ref $point)) (param $y f64)
    local.get $point
    local.get $y
    struct.set $point $y
  )
  
  ;; Array operations
  (func $array_demo (param $size i32) (result (ref $int_array))
    ;; Create new array with default values
    local.get $size
    i32.const 0
    array.new $int_array
  )
  
  (func $array_get_set (param $arr (ref $int_array)) (param $index i32) (param $value i32) (result i32)
    ;; Set array element
    local.get $arr
    local.get $index
    local.get $value
    array.set $int_array
    
    ;; Get array element
    local.get $arr
    local.get $index
    array.get $int_array
  )
  
  (func $array_length (param $arr (ref $int_array)) (result i32)
    local.get $arr
    array.len
  )
  
  ;; i31 reference operations
  (func $i31_demo (param $value i32) (result i32)
    ;; Create i31 reference
    local.get $value
    i31.new
    
    ;; Extract signed value
    i31.get_s
  )
  
  (export "make_point" (func $make_point))
  (export "get_x" (func $get_point_x))
  (export "get_y" (func $get_point_y))
  (export "set_x" (func $set_point_x))
  (export "set_y" (func $set_point_y))
  (export "array_demo" (func $array_demo))
  (export "array_ops" (func $array_get_set))
  (export "array_len" (func $array_length))
  (export "i31_demo" (func $i31_demo))
)

;; WebAssembly 2.0 SIMD (Vector) Instructions Example
(module
  (memory 1)
  
  ;; SIMD vector operations
  (func $simd_demo (param $addr i32)
    ;; Load v128 vector from memory
    local.get $addr
    v128.load
    
    ;; Create v128 constant
    v128.const i32x4 1 2 3 4
    
    ;; Vector addition
    i32x4.add
    
    ;; Store result back to memory
    local.get $addr
    swap
    v128.store
  )
  
  ;; SIMD lane operations
  (func $simd_lanes
    ;; Create vector with splat
    i32.const 42
    i32x4.splat
    
    ;; Extract lane
    i32x4.extract_lane 0
    drop
    
    ;; Replace lane
    i32.const 100
    i32x4.splat
    i32.const 99
    i32x4.replace_lane 1
    drop
  )
  
  ;; SIMD shuffle operation
  (func $simd_shuffle
    ;; Create two vectors
    v128.const i8x16 0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15
    v128.const i8x16 16 17 18 19 20 21 22 23 24 25 26 27 28 29 30 31
    
    ;; Shuffle lanes
    i8x16.shuffle 0 16 1 17 2 18 3 19 4 20 5 21 6 22 7 23
    drop
  )
  
  ;; SIMD comparison operations
  (func $simd_compare
    ;; Create vectors
    v128.const i32x4 1 2 3 4
    v128.const i32x4 4 3 2 1
    
    ;; Compare equal
    i32x4.eq
    
    ;; Check if any lane is true
    v128.any_true
    drop
  )
  
  ;; SIMD floating-point operations
  (func $simd_float
    ;; Create float vectors
    v128.const f32x4 1.0 2.0 3.0 4.0
    v128.const f32x4 0.5 1.5 2.5 3.5
    
    ;; Floating-point multiplication
    f32x4.mul
    
    ;; Floating-point square root
    f32x4.sqrt
    drop
  )
  
  (export "simd_demo" (func $simd_demo))
  (export "simd_lanes" (func $simd_lanes))
  (export "simd_shuffle" (func $simd_shuffle))
  (export "simd_compare" (func $simd_compare))
  (export "simd_float" (func $simd_float))
)

;; WebAssembly 2.0 Exception Handling Example
(module
  ;; Tag definition for exceptions
  (tag $division_by_zero (param i32))
  (tag $overflow (param i32 i32))
  
  ;; Function that may throw exceptions
  (func $divide (param $a i32) (param $b i32) (result i32)
    ;; Check for division by zero
    local.get $b
    i32.eqz
    (if
      (then
        local.get $a
        throw $division_by_zero
      )
    )
    
    ;; Perform division
    local.get $a
    local.get $b
    i32.div_s
  )
  
  ;; Function with exception handling
  (func $safe_divide (param $a i32) (param $b i32) (result i32)
    (try (result i32)
      (do
        local.get $a
        local.get $b
        call $divide
      )
      (catch $division_by_zero
        ;; Handle division by zero
        drop  ;; Drop the exception parameter
        i32.const 0
      )
      (catch_all
        ;; Handle any other exception
        i32.const -1
      )
    )
  )
  
  ;; Function demonstrating rethrow
  (func $rethrow_demo (param $a i32) (param $b i32) (result i32)
    (try (result i32)
      (do
        local.get $a
        local.get $b
        call $divide
      )
      (catch $division_by_zero
        ;; Log the error (simulated)
        local.get 0  ;; Get exception parameter
        call $print_error
        
        ;; Rethrow the exception
        rethrow 0
      )
    )
  )
  
  ;; Dummy error printing function
  (func $print_error (param $error_code i32)
    ;; Implementation would print error
    nop
  )
  
  (export "divide" (func $divide))
  (export "safe_divide" (func $safe_divide))
  (export "rethrow_demo" (func $rethrow_demo))
)

;; WebAssembly 2.0 Tail Calls Example
(module
  ;; Tail recursive factorial
  (func $factorial_tail (param $n i32) (param $acc i32) (result i32)
    local.get $n
    i32.const 0
    i32.eq
    (if (result i32)
      (then
        local.get $acc
      )
      (else
        local.get $n
        i32.const 1
        i32.sub
        local.get $acc
        local.get $n
        i32.mul
        return_call $factorial_tail  ;; Tail call
      )
    )
  )
  
  ;; Wrapper function
  (func $factorial (param $n i32) (result i32)
    local.get $n
    i32.const 1
    call $factorial_tail
  )
  
  ;; Mutual recursion with tail calls
  (func $is_even (param $n i32) (result i32)
    local.get $n
    i32.const 0
    i32.eq
    (if (result i32)
      (then
        i32.const 1
      )
      (else
        local.get $n
        i32.const 1
        i32.sub
        return_call $is_odd  ;; Tail call to other function
      )
    )
  )
  
  (func $is_odd (param $n i32) (result i32)
    local.get $n
    i32.const 0
    i32.eq
    (if (result i32)
      (then
        i32.const 0
      )
      (else
        local.get $n
        i32.const 1
        i32.sub
        return_call $is_even  ;; Tail call to other function
      )
    )
  )
  
  (export "factorial" (func $factorial))
  (export "is_even" (func $is_even))
  (export "is_odd" (func $is_odd))
)

;; WebAssembly 2.0 Bulk Memory Operations Example
(module
  (memory $mem 1)
  (data $data "Hello, WebAssembly 2.0!")
  
  ;; Bulk memory operations
  (func $bulk_memory_demo
    ;; Initialize memory from data segment
    i32.const 0      ;; destination offset
    i32.const 0      ;; source offset in data segment
    i32.const 21     ;; size
    memory.init $data
    
    ;; Drop data segment (free memory)
    data.drop $data
    
    ;; Fill memory with value
    i32.const 100    ;; destination offset
    i32.const 42     ;; value to fill
    i32.const 50     ;; size
    memory.fill
    
    ;; Copy memory region
    i32.const 200    ;; destination offset
    i32.const 0      ;; source offset
    i32.const 21     ;; size
    memory.copy
  )
  
  (export "bulk_demo" (func $bulk_memory_demo))
  (export "memory" (memory $mem))
)

;; WebAssembly 2.0 Multiple Memories Example
(module
  ;; Multiple memory declarations
  (memory $heap 1 10)
  (memory $stack 1 5)
  
  ;; Function using multiple memories
  (func $multi_memory_demo (param $value i32)
    ;; Store in first memory
    i32.const 0
    local.get $value
    i32.store
    
    ;; Store in second memory
    i32.const 0
    local.get $value
    i32.const 2
    i32.mul
    i32.store $stack
    
    ;; Copy between memories
    i32.const 0      ;; destination in stack
    i32.const 0      ;; source in heap
    i32.const 4      ;; size
    memory.copy $stack $heap
  )
  
  (export "multi_mem_demo" (func $multi_memory_demo))
  (export "heap" (memory $heap))
  (export "stack" (memory $stack))
)

;; WebAssembly 2.0 Typed Function References Example
(module
  ;; Function type definitions
  (type $binary_op (func (param i32 i32) (result i32)))
  (type $unary_op (func (param i32) (result i32)))
  
  ;; Table with typed function references
  (table $ops 10 (ref $binary_op))
  
  ;; Binary operation functions
  (func $add (type $binary_op)
    local.get 0
    local.get 1
    i32.add
  )
  
  (func $mul (type $binary_op)
    local.get 0
    local.get 1
    i32.mul
  )
  
  (func $sub (type $binary_op)
    local.get 0
    local.get 1
    i32.sub
  )
  
  ;; Function that uses typed function references
  (func $call_operation (param $op_index i32) (param $a i32) (param $b i32) (result i32)
    local.get $a
    local.get $b
    local.get $op_index
    table.get $ops
    call_ref $binary_op
  )
  
  ;; Initialize table with function references
  (func $init_ops
    ;; Set add function
    i32.const 0
    ref.func $add
    table.set $ops
    
    ;; Set multiply function
    i32.const 1
    ref.func $mul
    table.set $ops
    
    ;; Set subtract function
    i32.const 2
    ref.func $sub
    table.set $ops
  )
  
  (export "call_op" (func $call_operation))
  (export "init_ops" (func $init_ops))
  (export "ops_table" (table $ops))
)

;; WebAssembly 2.0 Extended Constant Expressions Example
(module
  ;; Global with extended constant expression
  (global $computed_value i32 
    (i32.add 
      (i32.const 10) 
      (i32.const 20)
    )
  )
  
  ;; Global with reference constant
  (global $func_ref (ref $add_func) (ref.func $add_impl))
  
  ;; Function type for reference
  (type $add_func (func (param i32 i32) (result i32)))
  
  ;; Implementation function
  (func $add_impl (type $add_func)
    local.get 0
    local.get 1
    i32.add
  )
  
  ;; Struct with constant initialization
  (type $config (struct 
    (field $version i32)
    (field $flags i32)
  ))
  
  (global $app_config (ref $config)
    (struct.new $config
      (i32.const 1)
      (i32.const 0x0F)
    )
  )
  
  (export "computed_value" (global $computed_value))
  (export "func_ref" (global $func_ref))
  (export "config" (global $app_config))
)

;; WebAssembly 2.0 Relaxed SIMD Example
(module
  (memory 1)
  
  ;; Relaxed SIMD operations for better performance
  (func $relaxed_simd_demo (param $addr i32)
    ;; Load vectors
    local.get $addr
    v128.load
    local.get $addr
    i32.const 16
    i32.add
    v128.load
    local.get $addr
    i32.const 32
    i32.add
    v128.load
    
    ;; Relaxed multiply-add (may use FMA if available)
    f32x4.relaxed_madd
    
    ;; Store result
    local.get $addr
    i32.const 48
    i32.add
    swap
    v128.store
  )
  
  ;; Relaxed lane select
  (func $relaxed_lane_select
    ;; Create vectors
    v128.const f32x4 1.0 2.0 3.0 4.0
    v128.const f32x4 5.0 6.0 7.0 8.0
    v128.const i32x4 0xFFFFFFFF 0 0xFFFFFFFF 0
    
    ;; Relaxed lane select (implementation-defined behavior)
    f32x4.relaxed_laneselect
    drop
  )
  
  (export "relaxed_demo" (func $relaxed_simd_demo))
  (export "relaxed_select" (func $relaxed_lane_select))
)

;; WebAssembly 2.0 JS String Builtins Example
(module
  ;; String operations (when supported by host)
  (func $string_demo (param $str_ref externref) (result externref)
    ;; Create string from UTF-8 bytes
    i32.const 0      ;; memory offset
    i32.const 13     ;; byte length
    string.new_utf8
    
    ;; Concatenate strings
    local.get $str_ref
    string.concat
    
    ;; Measure string length
    dup
    string.measure_utf8
    drop
    
    ;; Return concatenated string
  )
  
  ;; String encoding operations
  (func $string_encode (param $str_ref externref) (result i32)
    ;; Encode string to UTF-8 bytes in memory
    local.get $str_ref
    i32.const 100    ;; memory offset
    string.encode_utf8
  )
  
  ;; String comparison
  (func $string_compare (param $str1 externref) (param $str2 externref) (result i32)
    local.get $str1
    local.get $str2
    string.eq
  )
  
  (export "string_demo" (func $string_demo))
  (export "string_encode" (func $string_encode))
  (export "string_compare" (func $string_compare))
)

;; WebAssembly 2.0 Threads and Atomics Example
(module
  (memory $shared 1 10 shared)
  
  ;; Atomic memory operations
  (func $atomic_demo (param $addr i32) (param $value i32) (result i32)
    ;; Atomic load
    local.get $addr
    i32.atomic.load
    
    ;; Atomic store
    local.get $addr
    local.get $value
    i32.atomic.store
    
    ;; Atomic read-modify-write
    local.get $addr
    local.get $value
    i32.atomic.rmw.add
    
    ;; Atomic compare and exchange
    local.get $addr
    i32.const 0      ;; expected value
    local.get $value ;; new value
    i32.atomic.rmw.cmpxchg
  )
  
  ;; Thread synchronization
  (func $sync_demo (param $addr i32) (param $timeout i64) (result i32)
    ;; Wait for value change
    local.get $addr
    i32.const 0      ;; expected value
    local.get $timeout
    i32.atomic.wait
    
    ;; Notify waiting threads
    local.get $addr
    i32.const 1      ;; number of threads to wake
    atomic.notify
  )
  
  ;; Memory fence
  (func $fence_demo
    atomic.fence
  )
  
  (export "atomic_demo" (func $atomic_demo))
  (export "sync_demo" (func $sync_demo))
  (export "fence_demo" (func $fence_demo))
  (export "shared_memory" (memory $shared))
)

;; WebAssembly 2.0 Component Model Example
(component
  ;; Core module definition
  (core module $math
    (func $add (param i32 i32) (result i32)
      local.get 0
      local.get 1
      i32.add
    )
    (export "add" (func $add))
  )
  
  ;; Component function types
  (type $calculator (func (param "a" s32) (param "b" s32) (result s32)))
  
  ;; Component function implementation
  (func $component_add (type $calculator)
    (param $a s32) (param $b s32) (result s32)
    ;; Lift core function to component level
    local.get $a
    local.get $b
    (lift_core_func $math "add")
  )
  
  ;; Component exports
  (export "add" (func $component_add))
)

;; Advanced WebAssembly 2.0 Features Showcase
(module
  ;; Custom annotations
  (@name "advanced_features_module")
  (@version "2.0")
  
  ;; Memory with custom page size (if supported)
  (memory $custom_mem 1 (@custom_page_size 8192))
  
  ;; Function with optimization hints
  (func $optimized_function (param $n i32) (result i32)
    (@optimize "speed")
    (@profile "enter")
    
    ;; Branch with hinting
    local.get $n
    i32.const 0
    i32.eq
    (br_if 0 (@likely))  ;; Hint that branch is likely taken
    
    ;; Computation
    local.get $n
    local.get $n
    i32.mul
    
    (@profile "exit")
  )
  
  ;; Function with security annotation
  (func $secure_function (param $data externref) (result i32)
    (@security "trusted")
    (@validate "strict")
    
    ;; Secure computation
    local.get $data
    ref.is_null
    (if (result i32)
      (then i32.const 0)
      (else i32.const 1)
    )
  )
  
  ;; Resource-limited function
  (func $limited_function (param $size i32)
    (@limit "memory" 1048576)  ;; 1MB memory limit
    (@limit "time" 1000)       ;; 1 second time limit
    
    ;; Memory allocation simulation
    local.get $size
    i32.const 4
    i32.mul
    memory.grow
    drop
  )
  
  ;; Target-specific function
  (func $simd_optimized (param $addr i32)
    (@target "x86_64" "simd" "avx2")
    
    ;; SIMD operations optimized for x86_64 with AVX2
    local.get $addr
    v128.load
    local.get $addr
    i32.const 16
    i32.add
    v128.load
    i32x4.add
    local.get $addr
    i32.const 32
    i32.add
    swap
    v128.store
  )
  
  (export "optimized" (func $optimized_function))
  (export "secure" (func $secure_function))
  (export "limited" (func $limited_function))
  (export "simd_opt" (func $simd_optimized))
)

;; WebAssembly Interface Types (WIT) Integration Example
(@wit
  (world "calculator-world"
    (import "math" (interface
      (func "sin" (param "x" f64) (result f64))
      (func "cos" (param "x" f64) (result f64))
    ))
    (export "calculator" (interface
      (type "operation" (enum "add" "subtract" "multiply" "divide"))
      (func "calculate" 
        (param "op" operation) 
        (param "a" f64) 
        (param "b" f64) 
        (result f64)
      )
    ))
  )
)

;; Final comprehensive example combining multiple WebAssembly 2.0 features
(module $comprehensive_example
  ;; Type definitions
  (type $point (struct (field $x f64) (field $y f64)))
  (type $callback (func (param (ref $point)) (result f64)))
  
  ;; Memory and tables
  (memory $main_memory 2 100 shared)
  (table $callbacks 10 (ref $callback))
  
  ;; Exception tags
  (tag $math_error (param i32))
  
  ;; Global state
  (global $point_count (mut i32) (i32.const 0))
  
  ;; Advanced function combining multiple features
  (func $advanced_computation 
    (param $points (ref (array (ref $point))))
    (param $callback_index i32)
    (result f64)
    
    (local $sum f64)
    (local $i i32)
    (local $len i32)
    (local $point (ref $point))
    (local $callback (ref $callback))
    
    ;; Initialize
    f64.const 0.0
    local.set $sum
    i32.const 0
    local.set $i
    
    ;; Get array length
    local.get $points
    array.len
    local.set $len
    
    ;; Get callback function
    local.get $callback_index
    table.get $callbacks
    local.set $callback
    
    ;; Exception handling with loop
    (try (result f64)
      (do
        ;; Process each point
        (loop $process_loop
          ;; Check bounds
          local.get $i
          local.get $len
          i32.ge_u
          br_if $process_loop
          
          ;; Get point from array
          local.get $points
          local.get $i
          array.get (array (ref $point))
          local.set $point
          
          ;; Call callback with point
          local.get $point
          local.get $callback
          call_ref $callback
          
          ;; Add to sum
          local.get $sum
          f64.add
          local.set $sum
          
          ;; Increment counter
          local.get $i
          i32.const 1
          i32.add
          local.set $i
          
          br $process_loop
        )
        
        ;; Return sum
        local.get $sum
      )
      (catch $math_error
        ;; Handle math error
        drop
        f64.const -1.0
      )
      (catch_all
        ;; Handle any other error
        f64.const -2.0
      )
    )
  )
  
  (export "advanced_computation" (func $advanced_computation))
  (export "memory" (memory $main_memory))
  (export "callbacks" (table $callbacks))
)

