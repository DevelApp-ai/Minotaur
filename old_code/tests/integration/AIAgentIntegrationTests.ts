/**
 * Comprehensive integration tests for AI Agent Support System in Minotaur.
 * Tests all components working together: MCP, context-aware parsing, refactoring, 
 * multi-language support, surgical operations, and AI agent integration.
 */

import { describe, test, expect, beforeEach, afterEach } from '@jest/globals';
import { AIAgentIntegration, AIAgentType, AIAgentProfile } from '../../src/agents/AIAgentIntegration';
import { MCPServer } from '../../src/mcp/MCPServer';
import { MCPConnectionManager } from '../../src/mcp/MCPConnectionManager';
import { MCPRequestHandler } from '../../src/mcp/MCPRequestHandler';
import { ContextManager } from '../../src/context/ContextManager';
import { LanguageManager, SupportedLanguage } from '../../src/languages/LanguageManager';
import { CrossLanguageOperations } from '../../src/languages/CrossLanguageOperations';
import { RefactoringEngine } from '../../src/refactoring/RefactoringEngine';
import { SurgicalRefactoring } from '../../src/surgical/SurgicalRefactoring';
import { MinotaurCLI } from '../../src/cli/MinotaurCLI';
import * as fs from 'fs';
import * as path from 'path';

describe('AI Agent Integration Tests', () => {
  let aiAgentIntegration: AIAgentIntegration;
  let mcpServer: MCPServer;
  let connectionManager: MCPConnectionManager;
  let requestHandler: MCPRequestHandler;
  let contextManager: ContextManager;
  let languageManager: LanguageManager;
  let crossLanguageOps: CrossLanguageOperations;
  let refactoringEngine: RefactoringEngine;
  let surgicalRefactoring: SurgicalRefactoring;
  let cli: MinotaurCLI;
  
  const testDirectory = path.join(__dirname, 'test_files');
  const testFiles = {
    javascript: path.join(testDirectory, 'test.js'),
    typescript: path.join(testDirectory, 'test.ts'),
    python: path.join(testDirectory, 'test.py'),
    html: path.join(testDirectory, 'test.html'),
    mixed: path.join(testDirectory, 'mixed.html')
  };
  
  beforeEach(async () => {
    // Create test directory and files
    if (!fs.existsSync(testDirectory)) {
      fs.mkdirSync(testDirectory, { recursive: true });
    }
    
    // Create test files
    fs.writeFileSync(testFiles.javascript, `
function calculateSum(a, b) {
  const result = a + b;
  return result;
}

const numbers = [1, 2, 3, 4, 5];
const sum = numbers.reduce((acc, num) => acc + num, 0);
console.log('Sum:', sum);
    `);
    
    fs.writeFileSync(testFiles.typescript, `
interface User {
  id: number;
  name: string;
  email: string;
}

class UserService {
  private users: User[] = [];
  
  addUser(user: User): void {
    this.users.push(user);
  }
  
  findUser(id: number): User | undefined {
    return this.users.find(user => user.id === id);
  }
}
    `);
    
    fs.writeFileSync(testFiles.python, `
def calculate_fibonacci(n):
    if n <= 1:
        return n
    return calculate_fibonacci(n - 1) + calculate_fibonacci(n - 2)

class Calculator:
    def __init__(self):
        self.history = []
    
    def add(self, a, b):
        result = a + b
        self.history.append(f"{a} + {b} = {result}")
        return result
    `);
    
    fs.writeFileSync(testFiles.html, `
<!DOCTYPE html>
<html>
<head>
    <title>Test Page</title>
    <style>
        .container {
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>Welcome</h1>
        <p>This is a test page.</p>
    </div>
    <script>
        function greet(name) {
            return "Hello, " + name + "!";
        }
        
        document.addEventListener('DOMContentLoaded', function() {
            console.log(greet('World'));
        });
    </script>
</body>
</html>
    `);
    
    fs.writeFileSync(testFiles.mixed, `
<!DOCTYPE html>
<html>
<head>
    <style>
        .highlight { background-color: yellow; }
    </style>
</head>
<body>
    <div id="app"></div>
    <script>
        const data = { message: 'Hello Vue!' };
        const app = new Vue({
            el: '#app',
            data: data,
            methods: {
                updateMessage() {
                    this.message = 'Updated!';
                }
            }
        });
    </script>
</body>
</html>
    `);
    
    // Initialize components
    contextManager = new ContextManager();
    refactoringEngine = new RefactoringEngine(contextManager);
    languageManager = new LanguageManager(contextManager, refactoringEngine);
    crossLanguageOps = new CrossLanguageOperations(languageManager, refactoringEngine, contextManager);
    surgicalRefactoring = new SurgicalRefactoring(refactoringEngine, contextManager, languageManager);
    
    requestHandler = new MCPRequestHandler(contextManager, refactoringEngine, languageManager, crossLanguageOps);
    connectionManager = new MCPConnectionManager(null as any);
    mcpServer = new MCPServer(connectionManager, { port: 8081, host: 'localhost' });
    
    aiAgentIntegration = new AIAgentIntegration(
      mcpServer,
      connectionManager,
      requestHandler,
      contextManager,
      languageManager,
      crossLanguageOps,
      refactoringEngine,
      surgicalRefactoring
    );
    
    cli = new MinotaurCLI({ enableMCP: false });
  });
  
  afterEach(() => {
    // Clean up test files
    if (fs.existsSync(testDirectory)) {
      fs.rmSync(testDirectory, { recursive: true, force: true });
    }
  });
  
  describe('Language Detection and Analysis', () => {
    test('should detect JavaScript language correctly', () => {
      const content = fs.readFileSync(testFiles.javascript, 'utf-8');
      const detection = languageManager.detectLanguage(testFiles.javascript, content);
      
      expect(detection.language).toBe(SupportedLanguage.JAVASCRIPT);
      expect(detection.confidence).toBeGreaterThan(0.8);
    });
    
    test('should detect TypeScript language correctly', () => {
      const content = fs.readFileSync(testFiles.typescript, 'utf-8');
      const detection = languageManager.detectLanguage(testFiles.typescript, content);
      
      expect(detection.language).toBe(SupportedLanguage.TYPESCRIPT);
      expect(detection.confidence).toBeGreaterThan(0.8);
    });
    
    test('should detect Python language correctly', () => {
      const content = fs.readFileSync(testFiles.python, 'utf-8');
      const detection = languageManager.detectLanguage(testFiles.python, content);
      
      expect(detection.language).toBe(SupportedLanguage.PYTHON);
      expect(detection.confidence).toBeGreaterThan(0.8);
    });
    
    test('should detect embedded languages in HTML', () => {
      const content = fs.readFileSync(testFiles.html, 'utf-8');
      const detection = languageManager.detectLanguage(testFiles.html, content);
      
      expect(detection.language).toBe(SupportedLanguage.HTML);
      expect(detection.embeddedLanguages).toHaveLength(2); // CSS and JavaScript
      
      const embeddedLanguages = detection.embeddedLanguages.map(e => e.language);
      expect(embeddedLanguages).toContain(SupportedLanguage.CSS);
      expect(embeddedLanguages).toContain(SupportedLanguage.JAVASCRIPT);
    });
  });
  
  describe('Context-Aware Parsing', () => {
    test('should parse JavaScript context correctly', async () => {
      const content = fs.readFileSync(testFiles.javascript, 'utf-8');
      const context = contextManager.parseContent(testFiles.javascript, content);
      
      expect(context).toBeDefined();
      expect(context.file).toBe(testFiles.javascript);
      expect(context.language).toBe(SupportedLanguage.JAVASCRIPT);
    });
    
    test('should track symbol information', async () => {
      const content = fs.readFileSync(testFiles.typescript, 'utf-8');
      contextManager.parseContent(testFiles.typescript, content);
      
      const symbolTable = contextManager.getSymbolTable(testFiles.typescript);
      expect(symbolTable).toBeDefined();
      
      const symbols = symbolTable.getAllSymbols();
      expect(symbols.length).toBeGreaterThan(0);
      
      // Should find User interface and UserService class
      const userInterface = symbols.find(s => s.name === 'User');
      const userServiceClass = symbols.find(s => s.name === 'UserService');
      
      expect(userInterface).toBeDefined();
      expect(userServiceClass).toBeDefined();
    });
    
    test('should provide context at specific positions', async () => {
      const content = fs.readFileSync(testFiles.javascript, 'utf-8');
      contextManager.parseContent(testFiles.javascript, content);
      
      const position = { line: 2, column: 10, offset: 50 };
      const context = contextManager.getContextAt(testFiles.javascript, position);
      
      expect(context).toBeDefined();
      expect(context.position).toEqual(position);
    });
  });
  
  describe('Cross-Language Operations', () => {
    test('should extract embedded JavaScript from HTML', async () => {
      const request = {
        id: 'test-extract-1',
        type: 'extract_embedded_code' as any,
        sourceFile: testFiles.html,
        sourceLanguage: SupportedLanguage.HTML,
        position: { line: 15, column: 8, offset: 400 },
        parameters: {
          newFileName: path.join(testDirectory, 'extracted.js')
        },
        scope: {
          type: 'selection' as any,
          includeEmbedded: true,
          includeReferences: false,
          crossFileReferences: false
        }
      };
      
      const result = await crossLanguageOps.executeOperation(request);
      
      expect(result.success).toBe(true);
      expect(result.changes.length).toBeGreaterThan(0);
      expect(result.newFiles.length).toBe(1);
      expect(result.newFiles[0].language).toBe(SupportedLanguage.JAVASCRIPT);
    });
    
    test('should convert JavaScript to TypeScript', async () => {
      const request = {
        id: 'test-convert-1',
        type: 'convert_language' as any,
        sourceFile: testFiles.javascript,
        sourceLanguage: SupportedLanguage.JAVASCRIPT,
        targetLanguage: SupportedLanguage.TYPESCRIPT,
        position: { line: 1, column: 1, offset: 0 },
        parameters: {
          conversionOptions: {
            targetLanguage: SupportedLanguage.TYPESCRIPT,
            conversionStyle: 'idiomatic',
            preserveStructure: true,
            handleUnsupportedFeatures: 'comment',
            addTypeAnnotations: true,
            modernizeSyntax: true
          }
        },
        scope: {
          type: 'file' as any,
          includeEmbedded: true,
          includeReferences: false,
          crossFileReferences: false
        }
      };
      
      const result = await crossLanguageOps.executeOperation(request);
      
      expect(result.success).toBe(true);
      expect(result.metrics.languagesInvolved).toContain(SupportedLanguage.JAVASCRIPT);
      expect(result.metrics.languagesInvolved).toContain(SupportedLanguage.TYPESCRIPT);
    });
  });
  
  describe('Surgical Refactoring Operations', () => {
    test('should extract variable with surgical precision', async () => {
      const request = {
        id: 'test-surgical-1',
        type: 'extract_variable' as any,
        file: testFiles.javascript,
        language: SupportedLanguage.JAVASCRIPT,
        position: { line: 2, column: 18, offset: 45 },
        endPosition: { line: 2, column: 23, offset: 50 },
        parameters: {
          symbolName: 'extractedSum'
        },
        options: {
          preview: false,
          dryRun: false,
          validateOnly: false,
          minimizeChanges: true,
          preserveWhitespace: true,
          maintainLineNumbers: true,
          generateUndo: true,
          confidenceThreshold: 0.8,
          maxScopeExpansion: 2
        }
      };
      
      const result = await surgicalRefactoring.executeSurgicalOperation(request);
      
      expect(result.success).toBe(true);
      expect(result.changes.length).toBe(2); // Insert declaration + replace expression
      expect(result.validation.syntaxValid).toBe(true);
      expect(result.validation.semanticsValid).toBe(true);
      expect(result.metrics.confidenceScore).toBeGreaterThan(0.8);
    });
    
    test('should rename symbol across all references', async () => {
      const request = {
        id: 'test-surgical-2',
        type: 'rename_symbol' as any,
        file: testFiles.typescript,
        language: SupportedLanguage.TYPESCRIPT,
        position: { line: 7, column: 8, offset: 120 },
        parameters: {
          symbolName: 'users',
          newName: 'userList'
        },
        options: {
          preview: false,
          dryRun: false,
          validateOnly: false,
          minimizeChanges: true,
          preserveWhitespace: true,
          maintainLineNumbers: true,
          generateUndo: true,
          confidenceThreshold: 0.9,
          maxScopeExpansion: 1
        }
      };
      
      const result = await surgicalRefactoring.executeSurgicalOperation(request);
      
      expect(result.success).toBe(true);
      expect(result.changes.length).toBeGreaterThan(0);
      expect(result.validation.referencesValid).toBe(true);
      expect(result.metrics.confidenceScore).toBeGreaterThan(0.9);
    });
    
    test('should extract function with dependency analysis', async () => {
      const request = {
        id: 'test-surgical-3',
        type: 'extract_function' as any,
        file: testFiles.python,
        language: SupportedLanguage.PYTHON,
        position: { line: 2, column: 4, offset: 30 },
        endPosition: { line: 4, column: 60, offset: 120 },
        parameters: {
          symbolName: 'fibonacciHelper'
        },
        options: {
          preview: false,
          dryRun: false,
          validateOnly: false,
          minimizeChanges: true,
          preserveWhitespace: true,
          maintainLineNumbers: false,
          generateUndo: true,
          confidenceThreshold: 0.85,
          maxScopeExpansion: 3
        }
      };
      
      const result = await surgicalRefactoring.executeSurgicalOperation(request);
      
      expect(result.success).toBe(true);
      expect(result.changes.length).toBe(2); // Insert function + replace with call
      expect(result.validation.grammarCompliant).toBe(true);
      expect(result.metrics.complexityReduction).toBeGreaterThan(0);
    });
  });
  
  describe('AI Agent Integration', () => {
    test('should register AI agent successfully', async () => {
      const agentProfile: AIAgentProfile = {
        id: 'test-agent-1',
        name: 'Test Code Assistant',
        type: AIAgentType.CODE_ASSISTANT,
        version: '1.0.0',
        capabilities: {
          supportedLanguages: [SupportedLanguage.JAVASCRIPT, SupportedLanguage.TYPESCRIPT],
          supportedOperations: ['refactor', 'analyze', 'suggest'],
          contextAwareness: true,
          crossLanguageSupport: false,
          realTimeOperations: true,
          surgicalPrecision: true,
          batchProcessing: false,
          learningCapability: true,
          customizationSupport: true
        },
        preferences: {
          preferredLanguages: [SupportedLanguage.TYPESCRIPT],
          operationStyle: 'moderate',
          confidenceThreshold: 0.8,
          contextDepth: 3,
          validationLevel: 'standard',
          outputFormat: 'detailed',
          errorHandling: 'tolerant',
          learningMode: true
        },
        performance: {
          operationsCompleted: 0,
          successRate: 1.0,
          averageResponseTime: 0,
          averageConfidence: 0,
          errorRate: 0,
          learningProgress: 0,
          userSatisfaction: 0,
          lastActive: Date.now()
        },
        metadata: {}
      };
      
      const success = await aiAgentIntegration.registerAgent(agentProfile);
      
      expect(success).toBe(true);
      
      const registeredAgents = aiAgentIntegration.getRegisteredAgents();
      expect(registeredAgents).toHaveLength(1);
      expect(registeredAgents[0].id).toBe('test-agent-1');
    });
    
    test('should process agent operation request', async () => {
      // First register an agent
      const agentProfile: AIAgentProfile = {
        id: 'test-agent-2',
        name: 'Test Refactoring Specialist',
        type: AIAgentType.REFACTORING_SPECIALIST,
        version: '1.0.0',
        capabilities: {
          supportedLanguages: [SupportedLanguage.JAVASCRIPT, SupportedLanguage.TYPESCRIPT, SupportedLanguage.PYTHON],
          supportedOperations: ['refactor', 'surgical', 'cross_language'],
          contextAwareness: true,
          crossLanguageSupport: true,
          realTimeOperations: true,
          surgicalPrecision: true,
          batchProcessing: true,
          learningCapability: true,
          customizationSupport: true
        },
        preferences: {
          preferredLanguages: [SupportedLanguage.TYPESCRIPT, SupportedLanguage.JAVASCRIPT],
          operationStyle: 'conservative',
          confidenceThreshold: 0.9,
          contextDepth: 5,
          validationLevel: 'strict',
          outputFormat: 'verbose',
          errorHandling: 'strict',
          learningMode: true
        },
        performance: {
          operationsCompleted: 0,
          successRate: 1.0,
          averageResponseTime: 0,
          averageConfidence: 0,
          errorRate: 0,
          learningProgress: 0,
          userSatisfaction: 0,
          lastActive: Date.now()
        },
        metadata: {}
      };
      
      await aiAgentIntegration.registerAgent(agentProfile);
      
      // Process an operation request
      const request = {
        id: 'test-request-1',
        agentId: 'test-agent-2',
        type: 'operation' as any,
        operation: 'refactor',
        parameters: {
          file: testFiles.javascript,
          operation: 'extract_variable',
          position: { line: 2, column: 18, offset: 45 },
          symbolName: 'extractedValue'
        },
        priority: 'normal' as any
      };
      
      const response = await aiAgentIntegration.processAgentRequest(request);
      
      expect(response.success).toBe(true);
      expect(response.agentId).toBe('test-agent-2');
      expect(response.confidence).toBeGreaterThan(0);
      expect(response.executionTime).toBeGreaterThan(0);
    });
    
    test('should generate intelligent suggestions', async () => {
      // Register a code assistant agent
      const agentProfile: AIAgentProfile = {
        id: 'test-agent-3',
        name: 'Test Code Assistant',
        type: AIAgentType.CODE_ASSISTANT,
        version: '1.0.0',
        capabilities: {
          supportedLanguages: [SupportedLanguage.JAVASCRIPT, SupportedLanguage.TYPESCRIPT],
          supportedOperations: ['suggest', 'analyze'],
          contextAwareness: true,
          crossLanguageSupport: false,
          realTimeOperations: true,
          surgicalPrecision: false,
          batchProcessing: false,
          learningCapability: true,
          customizationSupport: true
        },
        preferences: {
          preferredLanguages: [SupportedLanguage.JAVASCRIPT],
          operationStyle: 'moderate',
          confidenceThreshold: 0.7,
          contextDepth: 2,
          validationLevel: 'basic',
          outputFormat: 'minimal',
          errorHandling: 'adaptive',
          learningMode: true
        },
        performance: {
          operationsCompleted: 0,
          successRate: 1.0,
          averageResponseTime: 0,
          averageConfidence: 0,
          errorRate: 0,
          learningProgress: 0,
          userSatisfaction: 0,
          lastActive: Date.now()
        },
        metadata: {}
      };
      
      await aiAgentIntegration.registerAgent(agentProfile);
      
      const suggestions = await aiAgentIntegration.getIntelligentSuggestions(
        'test-agent-3',
        testFiles.javascript,
        { line: 2, column: 10, offset: 40 }
      );
      
      expect(suggestions).toBeDefined();
      expect(suggestions.length).toBeGreaterThan(0);
      expect(suggestions.length).toBeLessThanOrEqual(3); // Minimal output format
    });
    
    test('should track agent performance metrics', async () => {
      const agentProfile: AIAgentProfile = {
        id: 'test-agent-4',
        name: 'Test Performance Agent',
        type: AIAgentType.GENERAL_PURPOSE,
        version: '1.0.0',
        capabilities: {
          supportedLanguages: [SupportedLanguage.JAVASCRIPT],
          supportedOperations: ['analyze'],
          contextAwareness: true,
          crossLanguageSupport: false,
          realTimeOperations: true,
          surgicalPrecision: false,
          batchProcessing: false,
          learningCapability: true,
          customizationSupport: false
        },
        preferences: {
          preferredLanguages: [SupportedLanguage.JAVASCRIPT],
          operationStyle: 'moderate',
          confidenceThreshold: 0.8,
          contextDepth: 2,
          validationLevel: 'standard',
          outputFormat: 'detailed',
          errorHandling: 'tolerant',
          learningMode: true
        },
        performance: {
          operationsCompleted: 0,
          successRate: 1.0,
          averageResponseTime: 0,
          averageConfidence: 0,
          errorRate: 0,
          learningProgress: 0,
          userSatisfaction: 0,
          lastActive: Date.now()
        },
        metadata: {}
      };
      
      await aiAgentIntegration.registerAgent(agentProfile);
      
      // Process multiple requests to build performance data
      for (let i = 0; i < 5; i++) {
        const request = {
          id: `test-request-${i}`,
          agentId: 'test-agent-4',
          type: 'analysis' as any,
          parameters: {
            file: testFiles.javascript,
            analysisType: 'complexity'
          },
          priority: 'normal' as any
        };
        
        await aiAgentIntegration.processAgentRequest(request);
      }
      
      const analytics = aiAgentIntegration.getAgentAnalytics('test-agent-4');
      
      expect(analytics).toBeDefined();
      expect(analytics.performance.operationsCompleted).toBe(5);
      expect(analytics.performance.successRate).toBe(1.0);
      expect(analytics.performance.averageResponseTime).toBeGreaterThan(0);
      expect(analytics.context.operationHistory).toHaveLength(5);
    });
    
    test('should record and analyze learning data', async () => {
      const agentProfile: AIAgentProfile = {
        id: 'test-agent-5',
        name: 'Test Learning Agent',
        type: AIAgentType.CODE_ASSISTANT,
        version: '1.0.0',
        capabilities: {
          supportedLanguages: [SupportedLanguage.JAVASCRIPT],
          supportedOperations: ['suggest'],
          contextAwareness: true,
          crossLanguageSupport: false,
          realTimeOperations: true,
          surgicalPrecision: false,
          batchProcessing: false,
          learningCapability: true,
          customizationSupport: true
        },
        preferences: {
          preferredLanguages: [SupportedLanguage.JAVASCRIPT],
          operationStyle: 'moderate',
          confidenceThreshold: 0.8,
          contextDepth: 2,
          validationLevel: 'standard',
          outputFormat: 'detailed',
          errorHandling: 'adaptive',
          learningMode: true
        },
        performance: {
          operationsCompleted: 0,
          successRate: 1.0,
          averageResponseTime: 0,
          averageConfidence: 0,
          errorRate: 0,
          learningProgress: 0,
          userSatisfaction: 0,
          lastActive: Date.now()
        },
        metadata: {}
      };
      
      await aiAgentIntegration.registerAgent(agentProfile);
      
      // Record learning data
      const learningData = {
        agentId: 'test-agent-5',
        operation: 'suggest',
        input: { file: testFiles.javascript, position: { line: 1, column: 1, offset: 0 } },
        output: ['Extract this function', 'Add type annotations'],
        feedback: 'positive' as any,
        contextFactors: ['javascript', 'function_context'],
        timestamp: Date.now()
      };
      
      aiAgentIntegration.recordLearningData(learningData);
      
      const allLearningData = aiAgentIntegration.getLearningData('test-agent-5');
      expect(allLearningData).toHaveLength(1);
      expect(allLearningData[0].feedback).toBe('positive');
      
      const analytics = aiAgentIntegration.getAgentAnalytics('test-agent-5');
      expect(analytics.learningData.totalEntries).toBe(1);
      expect(analytics.learningData.positiveFeedback).toBe(1);
      expect(analytics.performance.learningProgress).toBeGreaterThan(0);
    });
  });
  
  describe('CLI Integration', () => {
    test('should detect language via CLI', async () => {
      const result = await cli['executeCommand']('language:detect', {
        file: testFiles.javascript,
        content: true,
        embedded: false
      });
      
      expect(result.success).toBe(true);
      expect(result.result.language).toBe(SupportedLanguage.JAVASCRIPT);
      expect(result.result.confidence).toBeGreaterThan(0.8);
    });
    
    test('should list supported languages via CLI', async () => {
      const result = await cli['executeCommand']('language:list', {
        detailed: false
      });
      
      expect(result.success).toBe(true);
      expect(result.result.supportedLanguages).toBeDefined();
      expect(result.result.supportedLanguages.length).toBeGreaterThan(10);
    });
    
    test('should perform refactoring via CLI', async () => {
      const result = await cli['executeCommand']('refactor:extract-variable', {
        file: testFiles.javascript,
        position: { line: 2, column: 18 },
        endPosition: { line: 2, column: 23 },
        variableName: 'extractedSum',
        preview: true
      });
      
      expect(result.success).toBe(true);
      expect(result.result).toBeDefined();
    });
    
    test('should analyze code complexity via CLI', async () => {
      const result = await cli['executeCommand']('analyze:complexity', {
        file: testFiles.javascript,
        metrics: 'cyclomatic'
      });
      
      expect(result.success).toBe(true);
      expect(result.result.complexity).toBeDefined();
    });
    
    test('should convert language via CLI', async () => {
      const outputFile = path.join(testDirectory, 'converted.ts');
      
      const result = await cli['executeCommand']('cross-lang:convert', {
        file: testFiles.javascript,
        targetLanguage: SupportedLanguage.TYPESCRIPT,
        output: outputFile,
        style: 'idiomatic',
        preview: true
      });
      
      expect(result.success).toBe(true);
      expect(result.result.success).toBe(true);
    });
  });
  
  describe('End-to-End Integration', () => {
    test('should perform complete workflow: detect -> analyze -> refactor -> validate', async () => {
      // Step 1: Detect language
      const content = fs.readFileSync(testFiles.javascript, 'utf-8');
      const detection = languageManager.detectLanguage(testFiles.javascript, content);
      
      expect(detection.language).toBe(SupportedLanguage.JAVASCRIPT);
      
      // Step 2: Parse and analyze context
      const parseContext = contextManager.parseContent(testFiles.javascript, content);
      expect(parseContext).toBeDefined();
      
      const symbolTable = contextManager.getSymbolTable(testFiles.javascript);
      expect(symbolTable).toBeDefined();
      
      // Step 3: Perform surgical refactoring
      const surgicalRequest = {
        id: 'e2e-test-1',
        type: 'extract_variable' as any,
        file: testFiles.javascript,
        language: SupportedLanguage.JAVASCRIPT,
        position: { line: 2, column: 18, offset: 45 },
        endPosition: { line: 2, column: 23, offset: 50 },
        parameters: {
          symbolName: 'sum'
        },
        options: {
          preview: false,
          dryRun: false,
          validateOnly: false,
          minimizeChanges: true,
          preserveWhitespace: true,
          maintainLineNumbers: true,
          generateUndo: true,
          confidenceThreshold: 0.8,
          maxScopeExpansion: 2
        }
      };
      
      const surgicalResult = await surgicalRefactoring.executeSurgicalOperation(surgicalRequest);
      expect(surgicalResult.success).toBe(true);
      expect(surgicalResult.validation.syntaxValid).toBe(true);
      
      // Step 4: Validate with AI agent
      const agentProfile: AIAgentProfile = {
        id: 'e2e-agent',
        name: 'E2E Test Agent',
        type: AIAgentType.CODE_REVIEWER,
        version: '1.0.0',
        capabilities: {
          supportedLanguages: [SupportedLanguage.JAVASCRIPT],
          supportedOperations: ['validate', 'review'],
          contextAwareness: true,
          crossLanguageSupport: false,
          realTimeOperations: true,
          surgicalPrecision: true,
          batchProcessing: false,
          learningCapability: true,
          customizationSupport: true
        },
        preferences: {
          preferredLanguages: [SupportedLanguage.JAVASCRIPT],
          operationStyle: 'conservative',
          confidenceThreshold: 0.9,
          contextDepth: 3,
          validationLevel: 'strict',
          outputFormat: 'detailed',
          errorHandling: 'strict',
          learningMode: true
        },
        performance: {
          operationsCompleted: 0,
          successRate: 1.0,
          averageResponseTime: 0,
          averageConfidence: 0,
          errorRate: 0,
          learningProgress: 0,
          userSatisfaction: 0,
          lastActive: Date.now()
        },
        metadata: {}
      };
      
      await aiAgentIntegration.registerAgent(agentProfile);
      
      const agentRequest = {
        id: 'e2e-validation',
        agentId: 'e2e-agent',
        type: 'analysis' as any,
        parameters: {
          file: testFiles.javascript,
          changes: surgicalResult.changes,
          validationType: 'post_refactoring'
        },
        priority: 'high' as any
      };
      
      const agentResponse = await aiAgentIntegration.processAgentRequest(agentRequest);
      expect(agentResponse.success).toBe(true);
      expect(agentResponse.confidence).toBeGreaterThan(0.8);
    });
    
    test('should handle multi-language project with embedded code', async () => {
      // Analyze mixed HTML file with embedded CSS and JavaScript
      const content = fs.readFileSync(testFiles.mixed, 'utf-8');
      const detection = languageManager.detectLanguage(testFiles.mixed, content);
      
      expect(detection.language).toBe(SupportedLanguage.HTML);
      expect(detection.embeddedLanguages.length).toBeGreaterThan(0);
      
      // Extract embedded JavaScript
      const extractRequest = {
        id: 'multi-lang-extract',
        type: 'extract_embedded_code' as any,
        sourceFile: testFiles.mixed,
        sourceLanguage: SupportedLanguage.HTML,
        position: { line: 10, column: 8, offset: 200 },
        parameters: {
          newFileName: path.join(testDirectory, 'extracted_vue.js')
        },
        scope: {
          type: 'selection' as any,
          includeEmbedded: true,
          includeReferences: false,
          crossFileReferences: false
        }
      };
      
      const extractResult = await crossLanguageOps.executeOperation(extractRequest);
      expect(extractResult.success).toBe(true);
      expect(extractResult.newFiles.length).toBe(1);
      
      // Register a language converter agent
      const converterAgent: AIAgentProfile = {
        id: 'converter-agent',
        name: 'Language Converter',
        type: AIAgentType.LANGUAGE_CONVERTER,
        version: '1.0.0',
        capabilities: {
          supportedLanguages: [SupportedLanguage.JAVASCRIPT, SupportedLanguage.TYPESCRIPT, SupportedLanguage.HTML],
          supportedOperations: ['convert', 'extract', 'inline'],
          contextAwareness: true,
          crossLanguageSupport: true,
          realTimeOperations: true,
          surgicalPrecision: false,
          batchProcessing: true,
          learningCapability: true,
          customizationSupport: true
        },
        preferences: {
          preferredLanguages: [SupportedLanguage.TYPESCRIPT],
          operationStyle: 'moderate',
          confidenceThreshold: 0.8,
          contextDepth: 4,
          validationLevel: 'standard',
          outputFormat: 'verbose',
          errorHandling: 'adaptive',
          learningMode: true
        },
        performance: {
          operationsCompleted: 0,
          successRate: 1.0,
          averageResponseTime: 0,
          averageConfidence: 0,
          errorRate: 0,
          learningProgress: 0,
          userSatisfaction: 0,
          lastActive: Date.now()
        },
        metadata: {}
      };
      
      await aiAgentIntegration.registerAgent(converterAgent);
      
      // Get suggestions for the extracted JavaScript
      const suggestions = await aiAgentIntegration.getIntelligentSuggestions(
        'converter-agent',
        extractResult.newFiles[0].path,
        { line: 1, column: 1, offset: 0 }
      );
      
      expect(suggestions).toBeDefined();
      expect(suggestions.length).toBeGreaterThan(0);
    });
  });
  
  describe('Performance and Stress Tests', () => {
    test('should handle multiple concurrent agent requests', async () => {
      // Register multiple agents
      const agents = [];
      for (let i = 0; i < 5; i++) {
        const agentProfile: AIAgentProfile = {
          id: `stress-agent-${i}`,
          name: `Stress Test Agent ${i}`,
          type: AIAgentType.GENERAL_PURPOSE,
          version: '1.0.0',
          capabilities: {
            supportedLanguages: [SupportedLanguage.JAVASCRIPT],
            supportedOperations: ['analyze'],
            contextAwareness: true,
            crossLanguageSupport: false,
            realTimeOperations: true,
            surgicalPrecision: false,
            batchProcessing: false,
            learningCapability: false,
            customizationSupport: false
          },
          preferences: {
            preferredLanguages: [SupportedLanguage.JAVASCRIPT],
            operationStyle: 'moderate',
            confidenceThreshold: 0.8,
            contextDepth: 2,
            validationLevel: 'basic',
            outputFormat: 'minimal',
            errorHandling: 'tolerant',
            learningMode: false
          },
          performance: {
            operationsCompleted: 0,
            successRate: 1.0,
            averageResponseTime: 0,
            averageConfidence: 0,
            errorRate: 0,
            learningProgress: 0,
            userSatisfaction: 0,
            lastActive: Date.now()
          },
          metadata: {}
        };
        
        await aiAgentIntegration.registerAgent(agentProfile);
        agents.push(agentProfile);
      }
      
      // Process concurrent requests
      const requests = agents.map((agent, i) => ({
        id: `stress-request-${i}`,
        agentId: agent.id,
        type: 'analysis' as any,
        parameters: {
          file: testFiles.javascript,
          analysisType: 'basic'
        },
        priority: 'normal' as any
      }));
      
      const startTime = Date.now();
      const responses = await Promise.all(
        requests.map(request => aiAgentIntegration.processAgentRequest(request))
      );
      const endTime = Date.now();
      
      expect(responses).toHaveLength(5);
      responses.forEach(response => {
        expect(response.success).toBe(true);
      });
      
      const totalTime = endTime - startTime;
      expect(totalTime).toBeLessThan(10000); // Should complete within 10 seconds
    });
    
    test('should maintain performance with large files', async () => {
      // Create a large JavaScript file
      const largeContent = `
// Large JavaScript file for performance testing
${Array(1000).fill(0).map((_, i) => `
function testFunction${i}(param1, param2) {
  const result = param1 + param2 + ${i};
  const intermediate = result * 2;
  return intermediate / 3;
}

const testVariable${i} = testFunction${i}(${i}, ${i + 1});
console.log('Result ${i}:', testVariable${i});
`).join('\n')}
      `;
      
      const largeFile = path.join(testDirectory, 'large_test.js');
      fs.writeFileSync(largeFile, largeContent);
      
      const startTime = Date.now();
      
      // Test language detection
      const detection = languageManager.detectLanguage(largeFile, largeContent);
      expect(detection.language).toBe(SupportedLanguage.JAVASCRIPT);
      
      // Test context parsing
      const parseContext = contextManager.parseContent(largeFile, largeContent);
      expect(parseContext).toBeDefined();
      
      // Test symbol table generation
      const symbolTable = contextManager.getSymbolTable(largeFile);
      expect(symbolTable).toBeDefined();
      
      const endTime = Date.now();
      const processingTime = endTime - startTime;
      
      expect(processingTime).toBeLessThan(5000); // Should complete within 5 seconds
      
      // Clean up
      fs.unlinkSync(largeFile);
    });
  });
});

// Export test utilities for other test files
export {
  testFiles,
  AIAgentIntegration,
  SupportedLanguage,
  AIAgentType
};

