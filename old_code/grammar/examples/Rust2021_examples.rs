/**
 * Rust 2021 Grammar Test Examples
 * These examples demonstrate various Rust 2021 language features that should be parsed correctly
 */

// Basic module structure
use std::collections::HashMap;
use std::sync::{Arc, Mutex};
use std::thread;
use std::time::Duration;

// Rust 2021 edition features
#![allow(unused_variables)]

// Ownership and borrowing examples
fn ownership_examples() {
    // Basic ownership
    let s1 = String::from("hello");
    let s2 = s1; // s1 is moved to s2
    // println!("{}", s1); // This would cause a compile error
    
    // Borrowing
    let s3 = String::from("world");
    let len = calculate_length(&s3);
    println!("The length of '{}' is {}.", s3, len);
    
    // Mutable references
    let mut s4 = String::from("hello");
    change(&mut s4);
    println!("{}", s4);
}

fn calculate_length(s: &String) -> usize {
    s.len()
}

fn change(some_string: &mut String) {
    some_string.push_str(", world");
}

// Lifetimes
fn lifetime_examples() {
    let string1 = String::from("abcd");
    let string2 = "xyz";
    
    let result = longest(&string1, string2);
    println!("The longest string is {}", result);
}

fn longest<'a>(x: &'a str, y: &'a str) -> &'a str {
    if x.len() > y.len() {
        x
    } else {
        y
    }
}

// Struct with lifetimes
struct ImportantExcerpt<'a> {
    part: &'a str,
}

impl<'a> ImportantExcerpt<'a> {
    fn level(&self) -> i32 {
        3
    }
    
    fn announce_and_return_part(&self, announcement: &str) -> &str {
        println!("Attention please: {}", announcement);
        self.part
    }
}

// Traits and generics
trait Summary {
    fn summarize_author(&self) -> String;
    
    fn summarize(&self) -> String {
        format!("(Read more from {}...)", self.summarize_author())
    }
}

struct NewsArticle {
    headline: String,
    location: String,
    author: String,
    content: String,
}

impl Summary for NewsArticle {
    fn summarize_author(&self) -> String {
        format!("@{}", self.author)
    }
    
    fn summarize(&self) -> String {
        format!("{}, by {} ({})", self.headline, self.author, self.location)
    }
}

struct Tweet {
    username: String,
    content: String,
    reply: bool,
    retweet: bool,
}

impl Summary for Tweet {
    fn summarize_author(&self) -> String {
        format!("@{}", self.username)
    }
}

// Generic functions
fn notify<T: Summary>(item: &T) {
    println!("Breaking news! {}", item.summarize());
}

fn notify_multiple<T: Summary + std::fmt::Display>(item: &T) {
    println!("Breaking news! {}", item.summarize());
}

// Where clauses
fn some_function<T, U>(t: &T, u: &U) -> i32
where
    T: std::fmt::Display + Clone,
    U: Clone + std::fmt::Debug,
{
    42
}

// Associated types
trait Iterator {
    type Item;
    
    fn next(&mut self) -> Option<Self::Item>;
}

struct Counter {
    current: usize,
    max: usize,
}

impl Counter {
    fn new(max: usize) -> Counter {
        Counter { current: 0, max }
    }
}

impl Iterator for Counter {
    type Item = usize;
    
    fn next(&mut self) -> Option<Self::Item> {
        if self.current < self.max {
            let current = self.current;
            self.current += 1;
            Some(current)
        } else {
            None
        }
    }
}

// Const generics (Rust 2021 feature)
struct ArrayPair<T, const N: usize> {
    left: [T; N],
    right: [T; N],
}

impl<T: Default + Copy, const N: usize> ArrayPair<T, N> {
    fn new() -> Self {
        Self {
            left: [T::default(); N],
            right: [T::default(); N],
        }
    }
}

// Advanced pattern matching
enum Message {
    Quit,
    Move { x: i32, y: i32 },
    Write(String),
    ChangeColor(i32, i32, i32),
}

fn pattern_matching_examples() {
    let msg = Message::ChangeColor(0, 160, 255);
    
    match msg {
        Message::Quit => {
            println!("The Quit variant has no data to destructure.")
        }
        Message::Move { x, y } => {
            println!("Move in the x direction {} and in the y direction {}", x, y);
        }
        Message::Write(text) => println!("Text message: {}", text),
        Message::ChangeColor(r, g, b) => {
            println!("Change the color to red {}, green {}, and blue {}", r, g, b)
        }
    }
    
    // Match guards
    let num = Some(4);
    
    match num {
        Some(x) if x < 5 => println!("less than five: {}", x),
        Some(x) => println!("{}", x),
        None => (),
    }
    
    // @ bindings
    enum MessageType {
        Hello { id: i32 },
    }
    
    let msg = MessageType::Hello { id: 5 };
    
    match msg {
        MessageType::Hello {
            id: id_variable @ 3..=7,
        } => println!("Found an id in range: {}", id_variable),
        MessageType::Hello { id: 10..=12 } => {
            println!("Found an id in another range")
        }
        MessageType::Hello { id } => println!("Found some other id: {}", id),
    }
}

// Error handling
use std::fs::File;
use std::io::{self, Read};

fn read_username_from_file() -> Result<String, io::Error> {
    let mut username_file = File::open("hello.txt")?;
    let mut username = String::new();
    username_file.read_to_string(&mut username)?;
    Ok(username)
}

// Custom error types
use std::fmt;

#[derive(Debug)]
struct CustomError {
    details: String,
}

impl CustomError {
    fn new(msg: &str) -> CustomError {
        CustomError {
            details: msg.to_string(),
        }
    }
}

impl fmt::Display for CustomError {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result {
        write!(f, "{}", self.details)
    }
}

impl std::error::Error for CustomError {
    fn description(&self) -> &str {
        &self.details
    }
}

// Async/await (Rust 2018+, enhanced in 2021)
use std::future::Future;
use std::pin::Pin;
use std::task::{Context, Poll};

async fn async_examples() {
    let result = fetch_data().await;
    println!("Fetched: {}", result);
    
    // Concurrent execution
    let future1 = fetch_data();
    let future2 = fetch_data();
    
    let (result1, result2) = futures::join!(future1, future2);
    println!("Results: {} and {}", result1, result2);
}

async fn fetch_data() -> String {
    // Simulate async work
    tokio::time::sleep(Duration::from_millis(100)).await;
    "data".to_string()
}

// Custom Future implementation
struct TimerFuture {
    shared_state: Arc<Mutex<SharedState>>,
}

struct SharedState {
    completed: bool,
    waker: Option<std::task::Waker>,
}

impl Future for TimerFuture {
    type Output = ();
    
    fn poll(self: Pin<&mut Self>, cx: &mut Context<'_>) -> Poll<Self::Output> {
        let mut shared_state = self.shared_state.lock().unwrap();
        if shared_state.completed {
            Poll::Ready(())
        } else {
            shared_state.waker = Some(cx.waker().clone());
            Poll::Pending
        }
    }
}

impl TimerFuture {
    pub fn new(duration: Duration) -> Self {
        let shared_state = Arc::new(Mutex::new(SharedState {
            completed: false,
            waker: None,
        }));
        
        let thread_shared_state = shared_state.clone();
        thread::spawn(move || {
            thread::sleep(duration);
            let mut shared_state = thread_shared_state.lock().unwrap();
            shared_state.completed = true;
            if let Some(waker) = shared_state.waker.take() {
                waker.wake()
            }
        });
        
        TimerFuture { shared_state }
    }
}

// Macros
macro_rules! vec_of_strings {
    ($($x:expr),*) => {
        vec![$(String::from($x)),*]
    };
}

macro_rules! create_function {
    ($func_name:ident) => {
        fn $func_name() {
            println!("You called {:?}()", stringify!($func_name));
        }
    };
}

create_function!(foo);
create_function!(bar);

// Procedural macro example (would be in a separate crate)
// #[derive(Debug, Clone)]
// struct MyStruct {
//     field: String,
// }

// Advanced closures
fn closure_examples() {
    let expensive_closure = |num: u32| -> u32 {
        println!("calculating slowly...");
        thread::sleep(Duration::from_secs(2));
        num
    };
    
    // Fn traits
    fn call_with_one<F>(func: F) -> usize
    where
        F: Fn(usize) -> usize,
    {
        func(1)
    }
    
    let double = |x| x * 2;
    assert_eq!(2, call_with_one(double));
    
    // Move closures
    let x = vec![1, 2, 3];
    let equal_to_x = move |z| z == x;
    // println!("can't use x here: {:?}", x); // This would error
    let y = vec![1, 2, 3];
    assert!(equal_to_x(y));
}

// Smart pointers
fn smart_pointer_examples() {
    // Box<T>
    let b = Box::new(5);
    println!("b = {}", b);
    
    // Rc<T> for multiple ownership
    use std::rc::Rc;
    let a = Rc::new(5);
    let b = Rc::clone(&a);
    let c = Rc::clone(&a);
    println!("Reference count: {}", Rc::strong_count(&a));
    
    // RefCell<T> for interior mutability
    use std::cell::RefCell;
    let value = RefCell::new(5);
    *value.borrow_mut() += 10;
    println!("value: {:?}", value);
}

// Rust 2021 specific features
fn rust_2021_features() {
    // Disjoint capture in closures
    let mut name = String::from("Peter");
    let mut age = 42;
    
    let closure = || {
        println!("name: {}", name); // Only captures `name`
    };
    
    age += 1; // This is allowed because `age` is not captured
    closure();
    
    // IntoIterator for arrays
    for item in [1, 2, 3] {
        println!("item: {}", item);
    }
    
    // Panic macro consistency
    let condition = true;
    if !condition {
        panic!("This is a panic message");
    }
    
    // Or patterns in macro_rules!
    macro_rules! matches {
        ($expr:expr, $( $pattern:pat )|+ $( if $guard: expr )? $(,)?) => {
            match $expr {
                $( $pattern )|+ $( if $guard )? => true,
                _ => false
            }
        }
    }
    
    let value = Some(42);
    assert!(matches!(value, Some(x) if x > 40));
}

// Advanced trait usage
trait Draw {
    fn draw(&self);
}

struct Screen {
    components: Vec<Box<dyn Draw>>,
}

impl Screen {
    fn run(&self) {
        for component in self.components.iter() {
            component.draw();
        }
    }
}

struct Button {
    width: u32,
    height: u32,
    label: String,
}

impl Draw for Button {
    fn draw(&self) {
        println!("Drawing button: {}", self.label);
    }
}

struct SelectBox {
    width: u32,
    height: u32,
    options: Vec<String>,
}

impl Draw for SelectBox {
    fn draw(&self) {
        println!("Drawing select box with {} options", self.options.len());
    }
}

// State pattern
trait State {
    fn request_review(self: Box<Self>) -> Box<dyn State>;
    fn approve(self: Box<Self>) -> Box<dyn State>;
    fn content<'a>(&self, _post: &'a Post) -> &'a str {
        ""
    }
}

struct Draft {}

impl State for Draft {
    fn request_review(self: Box<Self>) -> Box<dyn State> {
        Box::new(PendingReview {})
    }
    
    fn approve(self: Box<Self>) -> Box<dyn State> {
        self
    }
}

struct PendingReview {}

impl State for PendingReview {
    fn request_review(self: Box<Self>) -> Box<dyn State> {
        self
    }
    
    fn approve(self: Box<Self>) -> Box<dyn State> {
        Box::new(Published {})
    }
}

struct Published {}

impl State for Published {
    fn request_review(self: Box<Self>) -> Box<dyn State> {
        self
    }
    
    fn approve(self: Box<Self>) -> Box<dyn State> {
        self
    }
    
    fn content<'a>(&self, post: &'a Post) -> &'a str {
        &post.content
    }
}

struct Post {
    state: Option<Box<dyn State>>,
    content: String,
}

impl Post {
    fn new() -> Post {
        Post {
            state: Some(Box::new(Draft {})),
            content: String::new(),
        }
    }
    
    fn add_text(&mut self, text: &str) {
        self.content.push_str(text);
    }
    
    fn content(&self) -> &str {
        self.state.as_ref().unwrap().content(self)
    }
    
    fn request_review(&mut self) {
        if let Some(s) = self.state.take() {
            self.state = Some(s.request_review())
        }
    }
    
    fn approve(&mut self) {
        if let Some(s) = self.state.take() {
            self.state = Some(s.approve())
        }
    }
}

// Unsafe Rust
unsafe fn dangerous() {
    println!("This is unsafe code");
}

fn unsafe_examples() {
    let mut num = 5;
    
    let r1 = &num as *const i32;
    let r2 = &mut num as *mut i32;
    
    unsafe {
        println!("r1 is: {}", *r1);
        println!("r2 is: {}", *r2);
        dangerous();
    }
    
    // Unsafe trait
    unsafe trait Foo {
        // methods go here
    }
    
    unsafe impl Foo for i32 {
        // method implementations go here
    }
}

// Testing
#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn it_works() {
        let result = 2 + 2;
        assert_eq!(result, 4);
    }
    
    #[test]
    fn exploration() {
        assert_eq!(2 + 2, 4);
    }
    
    #[test]
    fn another() {
        panic!("Make this test fail");
    }
    
    #[test]
    fn larger_can_hold_smaller() {
        let larger = Rectangle {
            width: 8,
            height: 7,
        };
        let smaller = Rectangle {
            width: 5,
            height: 1,
        };
        
        assert!(larger.can_hold(&smaller));
    }
    
    #[test]
    fn smaller_cannot_hold_larger() {
        let larger = Rectangle {
            width: 8,
            height: 7,
        };
        let smaller = Rectangle {
            width: 5,
            height: 1,
        };
        
        assert!(!smaller.can_hold(&larger));
    }
    
    #[test]
    fn it_adds_two() {
        assert_eq!(4, add_two(2));
    }
    
    #[test]
    #[should_panic(expected = "Guess value must be less than or equal to 100")]
    fn greater_than_100() {
        Guess::new(200);
    }
    
    #[test]
    fn it_works_with_result() -> Result<(), String> {
        if 2 + 2 == 4 {
            Ok(())
        } else {
            Err(String::from("two plus two does not equal four"))
        }
    }
}

#[derive(Debug)]
struct Rectangle {
    width: u32,
    height: u32,
}

impl Rectangle {
    fn can_hold(&self, other: &Rectangle) -> bool {
        self.width > other.width && self.height > other.height
    }
}

fn add_two(a: i32) -> i32 {
    a + 2
}

struct Guess {
    value: i32,
}

impl Guess {
    fn new(value: i32) -> Guess {
        if value < 1 {
            panic!(
                "Guess value must be greater than or equal to 1, got {}.",
                value
            );
        } else if value > 100 {
            panic!(
                "Guess value must be less than or equal to 100, got {}.",
                value
            );
        }
        
        Guess { value }
    }
}

// Main function demonstrating various features
fn main() {
    println!("=== Rust 2021 Features Demonstration ===");
    
    // Basic examples
    ownership_examples();
    lifetime_examples();
    pattern_matching_examples();
    closure_examples();
    smart_pointer_examples();
    rust_2021_features();
    
    // Trait objects
    let screen = Screen {
        components: vec![
            Box::new(SelectBox {
                width: 75,
                height: 10,
                options: vec![
                    String::from("Yes"),
                    String::from("Maybe"),
                    String::from("No"),
                ],
            }),
            Box::new(Button {
                width: 50,
                height: 10,
                label: String::from("OK"),
            }),
        ],
    };
    
    screen.run();
    
    // State pattern
    let mut post = Post::new();
    
    post.add_text("I ate a salad for lunch today");
    assert_eq!("", post.content());
    
    post.request_review();
    assert_eq!("", post.content());
    
    post.approve();
    assert_eq!("I ate a salad for lunch today", post.content());
    
    // Unsafe code
    unsafe_examples();
    
    // Macros
    let strings = vec_of_strings!("hello", "world", "rust");
    println!("Created strings: {:?}", strings);
    
    foo();
    bar();
    
    // Const generics
    let _array_pair: ArrayPair<i32, 5> = ArrayPair::new();
    
    // Traits and generics
    let tweet = Tweet {
        username: String::from("horse_ebooks"),
        content: String::from("of course, as you probably already know, people"),
        reply: false,
        retweet: false,
    };
    
    println!("1 new tweet: {}", tweet.summarize());
    
    let article = NewsArticle {
        headline: String::from("Penguins win the Stanley Cup Championship!"),
        location: String::from("Pittsburgh, PA, USA"),
        author: String::from("Iceburgh"),
        content: String::from(
            "The Pittsburgh Penguins once again are the best \
             hockey team in the NHL.",
        ),
    };
    
    println!("New article available! {}", article.summarize());
    
    notify(&tweet);
    
    // Iterator
    let mut counter = Counter::new(5);
    while let Some(value) = counter.next() {
        println!("Counter: {}", value);
    }
    
    println!("=== Rust 2021 Features Demo Complete ===");
}

// Additional advanced features
mod advanced_features {
    use std::ops::Add;
    
    // Operator overloading
    #[derive(Debug, Copy, Clone, PartialEq)]
    struct Point {
        x: i32,
        y: i32,
    }
    
    impl Add for Point {
        type Output = Point;
        
        fn add(self, other: Point) -> Point {
            Point {
                x: self.x + other.x,
                y: self.y + other.y,
            }
        }
    }
    
    // Newtype pattern
    struct Millimeters(u32);
    struct Meters(u32);
    
    impl Add<Meters> for Millimeters {
        type Output = Millimeters;
        
        fn add(self, other: Meters) -> Millimeters {
            Millimeters(self.0 + (other.0 * 1000))
        }
    }
    
    // Associated types vs generics
    trait Iterator<T> {
        fn next(&mut self) -> Option<T>;
    }
    
    trait IteratorAssoc {
        type Item;
        fn next(&mut self) -> Option<Self::Item>;
    }
    
    // Default generic type parameters
    trait Add2<Rhs = Self> {
        type Output;
        
        fn add(self, rhs: Rhs) -> Self::Output;
    }
    
    // Fully qualified syntax
    trait Pilot {
        fn fly(&self);
    }
    
    trait Wizard {
        fn fly(&self);
    }
    
    struct Human;
    
    impl Pilot for Human {
        fn fly(&self) {
            println!("This is your captain speaking.");
        }
    }
    
    impl Wizard for Human {
        fn fly(&self) {
            println!("Up!");
        }
    }
    
    impl Human {
        fn fly(&self) {
            println!("*waving arms furiously*");
        }
    }
    
    pub fn demonstrate_advanced_features() {
        let point1 = Point { x: 1, y: 0 };
        let point2 = Point { x: 2, y: 3 };
        let point3 = point1 + point2;
        println!("Point addition: {:?}", point3);
        
        let person = Human;
        person.fly();
        Pilot::fly(&person);
        Wizard::fly(&person);
        <Human as Pilot>::fly(&person);
    }
}

// Export for external use
pub use advanced_features::*;

