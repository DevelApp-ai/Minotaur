/**
 * PromptFileProcessor - File-based Prompt Processing
 * 
 * Processes prompt files in multiple formats (text, JSON, YAML) to extract
 * correction instructions for the Project Golem agentic system.
 * 
 * Supported formats:
 * - Plain text: Natural language instructions
 * - JSON: Structured instruction objects
 * - YAML: Human-readable structured instructions
 */

import * as fs from 'fs/promises';
import * as path from 'path';
import { AgenticSystem } from '../evaluation/AgenticSystem';

export interface PromptInstruction {
  type: 'fix_error' | 'refactor' | 'optimize' | 'add_feature' | 'remove_code' | 'format_code';
  description: string;
  targetLocation?: string;
  priority?: 'low' | 'medium' | 'high';
  parameters?: Record<string, any>;
  expectedOutcome?: string;
}

export interface ProcessedPrompt {
  instructions: PromptInstruction[];
  metadata: {
    format: 'text' | 'json' | 'yaml';
    totalInstructions: number;
    processingTime: number;
    sourceFile: string;
  };
}

export interface PromptApplicationResult {
  success: boolean;
  originalCode: string;
  finalCode: string;
  appliedInstructions: number;
  skippedInstructions: number;
  executionTime: number;
  errors: string[];
  instructionResults: InstructionResult[];
}

export interface InstructionResult {
  instruction: PromptInstruction;
  success: boolean;
  appliedCode: string;
  executionTime: number;
  error?: string;
  confidence: number;
}

/**
 * PromptFileProcessor - Main prompt processing class
 */
export class PromptFileProcessor {
  private supportedExtensions = ['.txt', '.json', '.yaml', '.yml', '.md'];
  private instructionPatterns = {
    fix: /(?:fix|correct|repair)\s+(.+)/i,
    refactor: /(?:refactor|restructure|reorganize)\s+(.+)/i,
    optimize: /(?:optimize|improve|enhance)\s+(.+)/i,
    add: /(?:add|insert|include)\s+(.+)/i,
    remove: /(?:remove|delete|eliminate)\s+(.+)/i,
    format: /(?:format|style|indent)\s+(.+)/i,
  };

  constructor() {}

  /**
   * Process prompt file and extract instructions
   */
  async processPromptFile(promptPath: string): Promise<ProcessedPrompt> {
    const startTime = Date.now();
    
    try {
      // Validate file exists and is supported
      await this.validatePromptFile(promptPath);
      
      // Read file content
      const content = await fs.readFile(promptPath, 'utf-8');
      
      // Determine format and parse
      const format = this.detectFormat(promptPath, content);
      const instructions = await this.parseInstructions(content, format);
      
      const processingTime = Date.now() - startTime;
      
      return {
        instructions,
        metadata: {
          format,
          totalInstructions: instructions.length,
          processingTime,
          sourceFile: promptPath,
        },
      };
      
    } catch (error) {
      throw new Error(`Failed to process prompt file ${promptPath}: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  /**
   * Apply prompt instructions to source code using agentic system
   */
  async applyPromptInstructions(
    sourceCode: string,
    instructions: PromptInstruction[],
    agenticSystem: AgenticSystem,
    userId: string = 'prompt-user',
  ): Promise<PromptApplicationResult> {
    
    const startTime = Date.now();
    let currentCode = sourceCode;
    const instructionResults: InstructionResult[] = [];
    let appliedCount = 0;
    let skippedCount = 0;
    const errors: string[] = [];
    
    console.log(`🎯 Applying ${instructions.length} prompt instructions...`);
    
    // Sort instructions by priority
    const sortedInstructions = this.sortInstructionsByPriority(instructions);
    
    for (let i = 0; i < sortedInstructions.length; i++) {
      const instruction = sortedInstructions[i];
      const instructionStartTime = Date.now();
      
      try {
        console.log(`📝 Instruction ${i + 1}/${sortedInstructions.length}: ${instruction.description}`);
        
        // Create instruction-specific prompt
        const instructionPrompt = this.createInstructionPrompt(instruction, currentCode);
        
        // Apply instruction using agentic system
        const result = await agenticSystem.correctErrors(
          currentCode,
          userId,
          `prompt-instruction-${i}-${Date.now()}`,
        );
        
        const instructionTime = Date.now() - instructionStartTime;
        
        if (result.success && result.correctedCode && result.correctedCode !== currentCode) {
          // Instruction was successfully applied
          currentCode = result.correctedCode;
          appliedCount++;
          
          instructionResults.push({
            instruction,
            success: true,
            appliedCode: currentCode,
            executionTime: instructionTime,
            confidence: result.deterministicRatio,
          });
          
          console.log(`✅ Applied (${instructionTime}ms, confidence: ${(result.deterministicRatio * 100).toFixed(1)}%)`);
          
        } else {
          // Instruction could not be applied
          skippedCount++;
          const error = `No changes applied for instruction: ${instruction.description}`;
          
          instructionResults.push({
            instruction,
            success: false,
            appliedCode: currentCode,
            executionTime: instructionTime,
            error,
            confidence: 0,
          });
          
          console.log(`⚠️  Skipped: ${error}`);
        }
        
      } catch (error) {
        // Instruction failed with error
        skippedCount++;
        const errorMessage = error instanceof Error ? error.message : String(error);
        errors.push(`Instruction ${i + 1} failed: ${errorMessage}`);
        
        instructionResults.push({
          instruction,
          success: false,
          appliedCode: currentCode,
          executionTime: Date.now() - instructionStartTime,
          error: errorMessage,
          confidence: 0,
        });
        
        console.log(`❌ Failed: ${errorMessage}`);
      }
    }
    
    const totalTime = Date.now() - startTime;
    
    console.log('\n📊 Prompt application complete:');
    console.log(`  Applied: ${appliedCount}/${instructions.length}`);
    console.log(`  Skipped: ${skippedCount}/${instructions.length}`);
    console.log(`  Total time: ${totalTime}ms`);
    
    return {
      success: appliedCount > 0,
      originalCode: sourceCode,
      finalCode: currentCode,
      appliedInstructions: appliedCount,
      skippedInstructions: skippedCount,
      executionTime: totalTime,
      errors,
      instructionResults,
    };
  }

  /**
   * Validate prompt file
   */
  private async validatePromptFile(promptPath: string): Promise<void> {
    try {
      const stats = await fs.stat(promptPath);
      
      if (!stats.isFile()) {
        throw new Error('Path is not a file');
      }
      
      const ext = path.extname(promptPath).toLowerCase();
      if (!this.supportedExtensions.includes(ext)) {
        throw new Error(`Unsupported file extension: ${ext}. Supported: ${this.supportedExtensions.join(', ')}`);
      }
      
    } catch (error) {
      if (error instanceof Error && error.message.includes('ENOENT')) {
        throw new Error('File does not exist');
      }
      throw error;
    }
  }

  /**
   * Detect prompt file format
   */
  private detectFormat(filePath: string, content: string): 'text' | 'json' | 'yaml' {
    const ext = path.extname(filePath).toLowerCase();
    
    // Check by extension first
    if (ext === '.json') {
      return 'json';
    } else if (ext === '.yaml' || ext === '.yml') {
      return 'yaml';
    }
    
    // Check by content
    const trimmedContent = content.trim();
    
    if (trimmedContent.startsWith('{') || trimmedContent.startsWith('[')) {
      return 'json';
    }
    
    if (trimmedContent.includes('---') || /^\w+:\s*/.test(trimmedContent)) {
      return 'yaml';
    }
    
    return 'text';
  }

  /**
   * Parse instructions based on format
   */
  private async parseInstructions(content: string, format: 'text' | 'json' | 'yaml'): Promise<PromptInstruction[]> {
    switch (format) {
      case 'json':
        return this.parseJSONPrompt(content);
      case 'yaml':
        return this.parseYAMLPrompt(content);
      case 'text':
      default:
        return this.parseTextPrompt(content);
    }
  }

  /**
   * Parse JSON prompt format
   */
  private parseJSONPrompt(content: string): PromptInstruction[] {
    try {
      const data = JSON.parse(content);
      
      if (Array.isArray(data)) {
        return data.map(this.validateInstruction);
      } else if (data.instructions && Array.isArray(data.instructions)) {
        return data.instructions.map(this.validateInstruction);
      } else {
        throw new Error('JSON must contain an array of instructions or an object with "instructions" array');
      }
      
    } catch (error) {
      throw new Error(`Invalid JSON format: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  /**
   * Parse YAML prompt format
   */
  private parseYAMLPrompt(content: string): PromptInstruction[] {
    try {
      // Simple YAML parsing (would use a proper YAML library in production)
      const lines = content.split('\n').filter(line => line.trim());
      const instructions: PromptInstruction[] = [];
      let currentInstruction: Partial<PromptInstruction> = {};
      
      for (const line of lines) {
        const trimmed = line.trim();
        
        if (trimmed.startsWith('- ') || trimmed === '-') {
          // New instruction
          if (currentInstruction.description) {
            instructions.push(this.validateInstruction(currentInstruction));
          }
          currentInstruction = {};
          
          if (trimmed.length > 2) {
            currentInstruction.description = trimmed.substring(2);
            currentInstruction.type = this.inferInstructionType(currentInstruction.description);
          }
        } else if (trimmed.includes(':')) {
          // Property
          const [key, ...valueParts] = trimmed.split(':');
          const value = valueParts.join(':').trim();
          
          switch (key.trim()) {
            case 'type':
              currentInstruction.type = value as any;
              break;
            case 'description':
              currentInstruction.description = value;
              break;
            case 'target':
            case 'targetLocation':
              currentInstruction.targetLocation = value;
              break;
            case 'priority':
              currentInstruction.priority = value as any;
              break;
          }
        }
      }
      
      // Add last instruction
      if (currentInstruction.description) {
        instructions.push(this.validateInstruction(currentInstruction));
      }
      
      return instructions;
      
    } catch (error) {
      throw new Error(`Invalid YAML format: ${error instanceof Error ? error.message : String(error)}`);
    }
  }

  /**
   * Parse text prompt format
   */
  private parseTextPrompt(content: string): PromptInstruction[] {
    const lines = content.split('\n').filter(line => line.trim());
    const instructions: PromptInstruction[] = [];
    
    for (const line of lines) {
      const trimmed = line.trim();
      
      // Skip empty lines and comments
      if (!trimmed || trimmed.startsWith('#') || trimmed.startsWith('//')) {
        continue;
      }
      
      // Parse instruction
      const type = this.inferInstructionType(trimmed);
      const description = trimmed;
      
      instructions.push({
        type,
        description,
        priority: 'medium',
      });
    }
    
    return instructions;
  }

  /**
   * Infer instruction type from description
   */
  private inferInstructionType(description: string): PromptInstruction['type'] {
    const lower = description.toLowerCase();
    
    for (const [type, pattern] of Object.entries(this.instructionPatterns)) {
      if (pattern.test(lower)) {
        switch (type) {
          case 'fix': return 'fix_error';
          case 'refactor': return 'refactor';
          case 'optimize': return 'optimize';
          case 'add': return 'add_feature';
          case 'remove': return 'remove_code';
          case 'format': return 'format_code';
        }
      }
    }
    
    // Default inference based on keywords
    if (lower.includes('error') || lower.includes('bug') || lower.includes('fix')) {
      return 'fix_error';
    } else if (lower.includes('refactor') || lower.includes('restructure')) {
      return 'refactor';
    } else if (lower.includes('optimize') || lower.includes('performance')) {
      return 'optimize';
    } else if (lower.includes('add') || lower.includes('implement')) {
      return 'add_feature';
    } else if (lower.includes('remove') || lower.includes('delete')) {
      return 'remove_code';
    } else if (lower.includes('format') || lower.includes('style')) {
      return 'format_code';
    }
    
    return 'fix_error'; // Default
  }

  /**
   * Validate instruction object
   */
  private validateInstruction(instruction: any): PromptInstruction {
    if (!instruction.description) {
      throw new Error('Instruction must have a description');
    }
    
    return {
      type: instruction.type || 'fix_error',
      description: instruction.description,
      targetLocation: instruction.targetLocation,
      priority: instruction.priority || 'medium',
      parameters: instruction.parameters,
      expectedOutcome: instruction.expectedOutcome,
    };
  }

  /**
   * Sort instructions by priority
   */
  private sortInstructionsByPriority(instructions: PromptInstruction[]): PromptInstruction[] {
    const priorityOrder = { high: 0, medium: 1, low: 2 };
    
    return [...instructions].sort((a, b) => {
      const aPriority = priorityOrder[a.priority || 'medium'];
      const bPriority = priorityOrder[b.priority || 'medium'];
      return aPriority - bPriority;
    });
  }

  /**
   * Create instruction-specific prompt for agentic system
   */
  private createInstructionPrompt(instruction: PromptInstruction, currentCode: string): string {
    let prompt = 'Please apply the following instruction to the code:\n\n';
    prompt += `Instruction: ${instruction.description}\n`;
    prompt += `Type: ${instruction.type}\n`;
    
    if (instruction.targetLocation) {
      prompt += `Target: ${instruction.targetLocation}\n`;
    }
    
    if (instruction.expectedOutcome) {
      prompt += `Expected outcome: ${instruction.expectedOutcome}\n`;
    }
    
    prompt += `\nCurrent code:\n\`\`\`python\n${currentCode}\n\`\`\`\n\n`;
    prompt += 'Please return the modified code with the instruction applied.';
    
    return prompt;
  }

  /**
   * Generate example prompt files for different formats
   */
  async generateExamplePrompts(outputDir: string): Promise<void> {
    await fs.mkdir(outputDir, { recursive: true });
    
    // Text format example
    const textExample = `# Example prompt file (text format)
Fix the syntax error in the if statement
Add missing import for math module
Optimize the loop performance
Format the code with proper indentation`;
    
    await fs.writeFile(path.join(outputDir, 'example.txt'), textExample);
    
    // JSON format example
    const jsonExample = {
      instructions: [
        {
          type: 'fix_error',
          description: 'Fix the syntax error in the if statement',
          priority: 'high',
          targetLocation: 'line 5',
        },
        {
          type: 'add_feature',
          description: 'Add missing import for math module',
          priority: 'medium',
          expectedOutcome: 'import math statement at the top',
        },
        {
          type: 'optimize',
          description: 'Optimize the loop performance',
          priority: 'low',
          parameters: {
            method: 'vectorization',
          },
        },
      ],
    };
    
    await fs.writeFile(path.join(outputDir, 'example.json'), JSON.stringify(jsonExample, null, 2));
    
    // YAML format example
    const yamlExample = `instructions:
  - type: fix_error
    description: Fix the syntax error in the if statement
    priority: high
    targetLocation: line 5
    
  - type: add_feature
    description: Add missing import for math module
    priority: medium
    expectedOutcome: import math statement at the top
    
  - type: optimize
    description: Optimize the loop performance
    priority: low`;
    
    await fs.writeFile(path.join(outputDir, 'example.yaml'), yamlExample);
    
    console.log(`📝 Example prompt files generated in: ${outputDir}`);
  }

  /**
   * Get supported file extensions
   */
  getSupportedExtensions(): string[] {
    return [...this.supportedExtensions];
  }

  /**
   * Get instruction type patterns
   */
  getInstructionPatterns(): Record<string, RegExp> {
    return { ...this.instructionPatterns };
  }
}

