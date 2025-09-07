/**
 * JavaScript ES2022 Grammar Test Examples
 * These examples demonstrate various JavaScript ES2022 language features that should be parsed correctly
 */

// Top-level await (ES2022)
const response = await fetch('https://api.example.com/data');
const data = await response.json();
console.log('Data loaded:', data);

// Private class fields and methods (ES2022)
class BankAccount {
    // Private fields
    #balance = 0;
    #accountNumber;
    
    // Private method
    #validateAmount(amount) {
        return amount > 0 && typeof amount === 'number';
    }
    
    constructor(initialBalance, accountNumber) {
        if (this.#validateAmount(initialBalance)) {
            this.#balance = initialBalance;
        }
        this.#accountNumber = accountNumber;
    }
    
    // Public method accessing private fields
    deposit(amount) {
        if (this.#validateAmount(amount)) {
            this.#balance += amount;
            return this.#balance;
        }
        throw new Error('Invalid deposit amount');
    }
    
    withdraw(amount) {
        if (this.#validateAmount(amount) && amount <= this.#balance) {
            this.#balance -= amount;
            return this.#balance;
        }
        throw new Error('Invalid withdrawal amount');
    }
    
    getBalance() {
        return this.#balance;
    }
    
    // Private field access in methods
    transfer(targetAccount, amount) {
        if (this.#validateAmount(amount) && amount <= this.#balance) {
            this.#balance -= amount;
            targetAccount.deposit(amount);
            return true;
        }
        return false;
    }
}

// Class static initialization blocks (ES2022)
class DatabaseConnection {
    static #instances = [];
    static #maxConnections = 10;
    
    // Static initialization block
    static {
        console.log('DatabaseConnection class initialized');
        this.#instances = [];
        
        // Setup cleanup on process exit
        if (typeof process !== 'undefined') {
            process.on('exit', () => {
                this.#instances.forEach(instance => instance.close());
            });
        }
    }
    
    constructor(connectionString) {
        if (DatabaseConnection.#instances.length >= DatabaseConnection.#maxConnections) {
            throw new Error('Maximum connections exceeded');
        }
        
        this.connectionString = connectionString;
        this.isConnected = false;
        DatabaseConnection.#instances.push(this);
    }
    
    static getActiveConnections() {
        return DatabaseConnection.#instances.length;
    }
    
    connect() {
        this.isConnected = true;
        console.log(`Connected to ${this.connectionString}`);
    }
    
    close() {
        this.isConnected = false;
        const index = DatabaseConnection.#instances.indexOf(this);
        if (index > -1) {
            DatabaseConnection.#instances.splice(index, 1);
        }
    }
}

// Error.cause for enhanced error handling (ES2022)
function processUserData(userData) {
    try {
        return JSON.parse(userData);
    } catch (originalError) {
        throw new Error('Failed to process user data', {
            cause: originalError
        });
    }
}

function handleUserRequest(request) {
    try {
        const userData = processUserData(request.body);
        return { success: true, data: userData };
    } catch (error) {
        console.error('Request failed:', error.message);
        if (error.cause) {
            console.error('Original cause:', error.cause.message);
        }
        throw new Error('User request processing failed', {
            cause: error
        });
    }
}

// Object.hasOwn() method (ES2022)
const userPreferences = {
    theme: 'dark',
    language: 'en',
    notifications: true
};

// Using Object.hasOwn instead of hasOwnProperty
function getUserPreference(key) {
    if (Object.hasOwn(userPreferences, key)) {
        return userPreferences[key];
    }
    return null;
}

// Safe property checking
function mergeUserSettings(defaultSettings, userSettings) {
    const merged = { ...defaultSettings };
    
    for (const key in userSettings) {
        if (Object.hasOwn(userSettings, key)) {
            merged[key] = userSettings[key];
        }
    }
    
    return merged;
}

// Array.prototype.at() method (ES2022)
const fruits = ['apple', 'banana', 'cherry', 'date', 'elderberry'];

// Positive indexing
console.log(fruits.at(0));  // 'apple'
console.log(fruits.at(2));  // 'cherry'

// Negative indexing (new feature)
console.log(fruits.at(-1)); // 'elderberry'
console.log(fruits.at(-2)); // 'date'

// String.prototype.at() method (ES2022)
const message = 'Hello, World!';

console.log(message.at(0));   // 'H'
console.log(message.at(-1));  // '!'
console.log(message.at(-6));  // 'W'

// RegExp match indices (ES2022)
const regex = /(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})/d;
const dateString = '2022-12-25';
const match = regex.exec(dateString);

if (match) {
    console.log('Full match:', match[0]);
    console.log('Match indices:', match.indices);
    console.log('Year group:', match.groups.year);
    console.log('Year indices:', match.indices.groups.year);
}

// Enhanced numeric separators (ES2022)
const largeNumber = 1_000_000_000;
const binaryNumber = 0b1010_0001;
const hexNumber = 0xFF_EC_DE_5E;
const bigIntNumber = 123_456_789_123_456_789n;

console.log('Large number:', largeNumber);
console.log('Binary:', binaryNumber);
console.log('Hex:', hexNumber);
console.log('BigInt:', bigIntNumber);

// Private field access patterns
class SecureStorage {
    #data = new Map();
    #encryptionKey;
    
    constructor(encryptionKey) {
        this.#encryptionKey = encryptionKey;
    }
    
    // Private field in operator (ES2022)
    hasSecureData(key) {
        return #data in this && this.#data.has(key);
    }
    
    store(key, value) {
        if (!this.#data) {
            this.#data = new Map();
        }
        this.#data.set(key, this.#encrypt(value));
    }
    
    retrieve(key) {
        if (this.#data.has(key)) {
            return this.#decrypt(this.#data.get(key));
        }
        return null;
    }
    
    #encrypt(data) {
        // Simple encryption simulation
        return btoa(JSON.stringify(data) + this.#encryptionKey);
    }
    
    #decrypt(encryptedData) {
        try {
            const decrypted = atob(encryptedData);
            return JSON.parse(decrypted.slice(0, -this.#encryptionKey.length));
        } catch {
            throw new Error('Decryption failed');
        }
    }
}

// Async/await with top-level await
async function fetchUserData(userId) {
    const response = await fetch(`/api/users/${userId}`);
    if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`, {
            cause: new Error(`Failed to fetch user ${userId}`)
        });
    }
    return response.json();
}

// Top-level await usage
const currentUser = await fetchUserData('123');
console.log('Current user loaded:', currentUser);

// Enhanced error handling with cause chaining
async function processMultipleUsers(userIds) {
    const results = [];
    const errors = [];
    
    for (const userId of userIds) {
        try {
            const userData = await fetchUserData(userId);
            results.push(userData);
        } catch (error) {
            const enhancedError = new Error(`Failed to process user ${userId}`, {
                cause: error
            });
            errors.push(enhancedError);
        }
    }
    
    if (errors.length > 0) {
        throw new Error('Some users could not be processed', {
            cause: errors
        });
    }
    
    return results;
}

// Class with static blocks and private fields
class ConfigManager {
    static #config = {};
    static #initialized = false;
    
    // Static initialization block
    static {
        console.log('Initializing ConfigManager...');
        
        // Load configuration
        this.#config = {
            apiUrl: process.env.API_URL || 'http://localhost:3000',
            timeout: parseInt(process.env.TIMEOUT) || 5000,
            retries: parseInt(process.env.RETRIES) || 3
        };
        
        this.#initialized = true;
        console.log('ConfigManager initialized with config:', this.#config);
    }
    
    static getConfig(key) {
        if (!this.#initialized) {
            throw new Error('ConfigManager not initialized');
        }
        
        if (Object.hasOwn(this.#config, key)) {
            return this.#config[key];
        }
        
        return null;
    }
    
    static updateConfig(key, value) {
        if (!this.#initialized) {
            throw new Error('ConfigManager not initialized');
        }
        
        this.#config[key] = value;
    }
}

// Advanced private field usage
class EventEmitter {
    #listeners = new Map();
    #maxListeners = 10;
    
    constructor(maxListeners = 10) {
        this.#maxListeners = maxListeners;
    }
    
    on(event, listener) {
        if (!this.#listeners.has(event)) {
            this.#listeners.set(event, []);
        }
        
        const eventListeners = this.#listeners.get(event);
        if (eventListeners.length >= this.#maxListeners) {
            throw new Error(`Maximum listeners (${this.#maxListeners}) exceeded for event: ${event}`);
        }
        
        eventListeners.push(listener);
    }
    
    emit(event, ...args) {
        if (this.#listeners.has(event)) {
            const eventListeners = this.#listeners.get(event);
            eventListeners.forEach(listener => {
                try {
                    listener(...args);
                } catch (error) {
                    const emitError = new Error(`Error in event listener for '${event}'`, {
                        cause: error
                    });
                    console.error(emitError);
                }
            });
        }
    }
    
    // Private field access check
    hasListeners(event) {
        return #listeners in this && this.#listeners.has(event) && this.#listeners.get(event).length > 0;
    }
    
    removeAllListeners(event) {
        if (event) {
            this.#listeners.delete(event);
        } else {
            this.#listeners.clear();
        }
    }
}

// Module-level code with top-level await
const config = await import('./config.json', { assert: { type: 'json' } });
const database = new DatabaseConnection(config.default.databaseUrl);
await database.connect();

// Enhanced array methods
const numbers = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

// Using at() method for safe indexing
function getElementSafely(array, index) {
    // Handle both positive and negative indices
    return array.at(index) ?? 'Index out of bounds';
}

console.log(getElementSafely(numbers, 0));   // 1
console.log(getElementSafely(numbers, -1));  // 10
console.log(getElementSafely(numbers, 100)); // 'Index out of bounds'

// Object property existence checking
const apiResponse = {
    data: { users: [] },
    meta: { total: 0 },
    errors: null
};

function processApiResponse(response) {
    const result = {};
    
    // Using Object.hasOwn for safe property checking
    if (Object.hasOwn(response, 'data')) {
        result.data = response.data;
    }
    
    if (Object.hasOwn(response, 'meta')) {
        result.metadata = response.meta;
    }
    
    if (Object.hasOwn(response, 'errors') && response.errors) {
        throw new Error('API returned errors', {
            cause: response.errors
        });
    }
    
    return result;
}

// Private methods in classes
class Calculator {
    #history = [];
    
    // Private method for validation
    #validateNumbers(...numbers) {
        return numbers.every(num => typeof num === 'number' && !isNaN(num));
    }
    
    // Private method for logging
    #logOperation(operation, operands, result) {
        this.#history.push({
            operation,
            operands: [...operands],
            result,
            timestamp: new Date().toISOString()
        });
    }
    
    add(...numbers) {
        if (!this.#validateNumbers(...numbers)) {
            throw new Error('Invalid numbers provided', {
                cause: new Error(`Invalid operands: ${numbers}`)
            });
        }
        
        const result = numbers.reduce((sum, num) => sum + num, 0);
        this.#logOperation('add', numbers, result);
        return result;
    }
    
    multiply(...numbers) {
        if (!this.#validateNumbers(...numbers)) {
            throw new Error('Invalid numbers provided');
        }
        
        const result = numbers.reduce((product, num) => product * num, 1);
        this.#logOperation('multiply', numbers, result);
        return result;
    }
    
    getHistory() {
        // Return a copy to prevent external modification
        return [...this.#history];
    }
    
    clearHistory() {
        this.#history = [];
    }
}

// Advanced error handling with cause chains
async function complexOperation() {
    try {
        const step1Result = await performStep1();
        const step2Result = await performStep2(step1Result);
        const step3Result = await performStep3(step2Result);
        return step3Result;
    } catch (error) {
        throw new Error('Complex operation failed', {
            cause: error
        });
    }
}

async function performStep1() {
    try {
        // Simulate some async operation
        await new Promise(resolve => setTimeout(resolve, 100));
        return 'step1-data';
    } catch (error) {
        throw new Error('Step 1 failed', { cause: error });
    }
}

async function performStep2(data) {
    try {
        if (!data) {
            throw new Error('No data provided to step 2');
        }
        return `${data}-step2`;
    } catch (error) {
        throw new Error('Step 2 failed', { cause: error });
    }
}

async function performStep3(data) {
    try {
        if (!data.includes('step2')) {
            throw new Error('Invalid data format in step 3');
        }
        return `${data}-step3`;
    } catch (error) {
        throw new Error('Step 3 failed', { cause: error });
    }
}

// Usage examples
async function demonstrateES2022Features() {
    console.log('=== ES2022 Features Demonstration ===');
    
    // Private fields and methods
    const account = new BankAccount(1000, 'ACC-123');
    console.log('Initial balance:', account.getBalance());
    account.deposit(500);
    console.log('After deposit:', account.getBalance());
    
    // Static initialization blocks
    const db1 = new DatabaseConnection('postgres://localhost:5432/db1');
    const db2 = new DatabaseConnection('postgres://localhost:5432/db2');
    console.log('Active connections:', DatabaseConnection.getActiveConnections());
    
    // Error.cause
    try {
        await complexOperation();
    } catch (error) {
        console.error('Operation failed:', error.message);
        let currentError = error;
        while (currentError.cause) {
            console.error('Caused by:', currentError.cause.message);
            currentError = currentError.cause;
        }
    }
    
    // Object.hasOwn
    const settings = mergeUserSettings(
        { theme: 'light', lang: 'en' },
        { theme: 'dark', notifications: true }
    );
    console.log('Merged settings:', settings);
    
    // Array.at() and String.at()
    const items = ['first', 'second', 'third', 'fourth', 'last'];
    console.log('Last item:', items.at(-1));
    console.log('Second to last:', items.at(-2));
    
    const text = 'JavaScript';
    console.log('Last character:', text.at(-1));
    console.log('First character:', text.at(0));
    
    // Private field access
    const storage = new SecureStorage('secret-key');
    storage.store('user-data', { name: 'John', age: 30 });
    console.log('Stored data:', storage.retrieve('user-data'));
    
    // Calculator with private methods
    const calc = new Calculator();
    console.log('Addition result:', calc.add(10, 20, 30));
    console.log('Multiplication result:', calc.multiply(2, 3, 4));
    console.log('Calculation history:', calc.getHistory());
    
    // Event emitter with private fields
    const emitter = new EventEmitter();
    emitter.on('test', data => console.log('Event received:', data));
    emitter.emit('test', 'Hello ES2022!');
    
    console.log('=== ES2022 Features Demo Complete ===');
}

// Top-level await for demo execution
await demonstrateES2022Features();

// Export for module usage
export {
    BankAccount,
    DatabaseConnection,
    SecureStorage,
    Calculator,
    EventEmitter,
    ConfigManager,
    processUserData,
    handleUserRequest,
    getUserPreference,
    mergeUserSettings,
    getElementSafely,
    processApiResponse,
    complexOperation
};

