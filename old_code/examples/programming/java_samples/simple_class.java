// Simple Java Class Example
package com.example.demo;

import java.util.List;
import java.util.ArrayList;

/**
 * A simple Person class demonstrating basic Java syntax
 */
public class Person {
    // Private fields
    private String name;
    private int age;
    private String email;
    
    // Constructor
    public Person(String name, int age, String email) {
        this.name = name;
        this.age = age;
        this.email = email;
    }
    
    // Default constructor
    public Person() {
        this("Unknown", 0, "");
    }
    
    // Getter methods
    public String getName() {
        return name;
    }
    
    public int getAge() {
        return age;
    }
    
    public String getEmail() {
        return email;
    }
    
    // Setter methods
    public void setName(String name) {
        this.name = name;
    }
    
    public void setAge(int age) {
        if (age >= 0) {
            this.age = age;
        }
    }
    
    public void setEmail(String email) {
        this.email = email;
    }
    
    // Business methods
    public boolean isAdult() {
        return age >= 18;
    }
    
    public String getDisplayName() {
        return name + " (" + age + " years old)";
    }
    
    // Override toString method
    @Override
    public String toString() {
        return "Person{" +
                "name='" + name + '\'' +
                ", age=" + age +
                ", email='" + email + '\'' +
                '}';
    }
    
    // Static method
    public static List<Person> createSamplePersons() {
        List<Person> persons = new ArrayList<>();
        persons.add(new Person("John Doe", 30, "john@example.com"));
        persons.add(new Person("Jane Smith", 25, "jane@example.com"));
        persons.add(new Person("Bob Johnson", 35, "bob@example.com"));
        return persons;
    }
}

