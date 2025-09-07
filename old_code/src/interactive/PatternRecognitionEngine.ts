/**
 * Pattern Recognition and Mapping Engine
 *
 * This module provides advanced pattern recognition capabilities for the interactive
 * AST translation system. It learns from user interactions and builds a comprehensive
 * library of translation patterns that can be reused across projects.
 *
 * Key Features:
 * - Automatic pattern extraction from successful translations
 * - Similarity matching using AST structure analysis
 * - Machine learning-based pattern improvement
 * - Pattern generalization and specialization
 * - Performance optimization for pattern matching
 *
 * @author Minotaur Team
 * @since 2024
 */

import { ZeroCopyASTNode, ASTNodeType } from '../zerocopy/ast/ZeroCopyASTNode';
import {
  TranslationPattern,
  ASTPattern,
  PatternStructure,
  PatternElement,
  PatternConstraint,
  PatternVariable,
  TranslationStep,
  UserFeedback,
  UserAction,
} from './InteractiveASTTranslator';

export interface PatternMatch {
    pattern: TranslationPattern;
    confidence: number;
    similarity: number;
    variableBindings: Map<string, any>;
    structuralScore: number;
    semanticScore: number;
    contextScore: number;
}

export interface PatternExtractionResult {
    pattern: TranslationPattern;
    confidence: number;
    generalizationLevel: number;
    applicabilityScore: number;
    examples: PatternExample[];
}

export interface PatternExample {
    sourceNode: ZeroCopyASTNode;
    targetNode: ZeroCopyASTNode;
    context: ExtractionContext;
    quality: number;
}

export interface ExtractionContext {
    sourceLanguage: string;
    targetLanguage: string;
    framework?: string;
    domain?: string;
    complexity: number;
    userRating?: number;
}

export interface SimilarityMetrics {
    structural: number;
    semantic: number;
    lexical: number;
    contextual: number;
    overall: number;
}

export interface PatternCluster {
    id: string;
    centroid: TranslationPattern;
    patterns: TranslationPattern[];
    cohesion: number;
    coverage: number;
    quality: number;
}

export interface LearningStatistics {
    totalPatterns: number;
    successfulMatches: number;
    failedMatches: number;
    averageConfidence: number;
    improvementRate: number;
    lastUpdated: Date;
}

/**
 * Pattern Recognition Engine
 *
 * Core engine for recognizing, extracting, and managing translation patterns.
 * Uses advanced algorithms for pattern matching and machine learning for
 * continuous improvement.
 */
export class PatternRecognitionEngine {
  private patterns: Map<string, TranslationPattern>;
  private patternClusters: Map<string, PatternCluster>;
  private learningStatistics: LearningStatistics;
  private similarityCache: Map<string, SimilarityMetrics>;

  constructor() {
    this.patterns = new Map();
    this.patternClusters = new Map();
    this.learningStatistics = {
      totalPatterns: 0,
      successfulMatches: 0,
      failedMatches: 0,
      averageConfidence: 0,
      improvementRate: 0,
      lastUpdated: new Date(),
    };
    this.similarityCache = new Map();
  }

  /**
     * Find patterns that match a given AST node
     */
  findMatchingPatterns(
    sourceNode: ZeroCopyASTNode,
    sourceLanguage: string,
    targetLanguage: string,
    maxResults: number = 10,
  ): PatternMatch[] {
    const matches: PatternMatch[] = [];

    for (const pattern of this.patterns.values()) {
      // Check language compatibility
      if (pattern.sourceLanguage !== sourceLanguage ||
                pattern.targetLanguage !== targetLanguage) {
        continue;
      }

      // Calculate match confidence
      const match = this.calculatePatternMatch(pattern, sourceNode);
      if (match.confidence > 0.1) { // Minimum threshold
        matches.push(match);
      }
    }

    // Sort by confidence and return top results
    matches.sort((a, b) => b.confidence - a.confidence);
    return matches.slice(0, maxResults);
  }

  /**
     * Calculate how well a pattern matches a source node
     */
  private calculatePatternMatch(
    pattern: TranslationPattern,
    sourceNode: ZeroCopyASTNode,
  ): PatternMatch {
    // Check basic node type compatibility - convert to string for comparison
    if (pattern.sourcePattern.nodeType !== String(sourceNode.nodeType)) {
      return {
        pattern,
        confidence: 0,
        similarity: 0,
        variableBindings: new Map(),
        structuralScore: 0,
        semanticScore: 0,
        contextScore: 0,
      };
    }

    // Calculate structural similarity
    const structuralScore = this.calculateStructuralSimilarity(
      pattern.sourcePattern,
      sourceNode,
    );

    // Calculate semantic similarity
    const semanticScore = this.calculateSemanticSimilarity(
      pattern.sourcePattern,
      sourceNode,
    );

    // Calculate context similarity
    const contextScore = this.calculateContextSimilarity(
      pattern,
      sourceNode,
    );

    // Extract variable bindings
    const variableBindings = this.extractVariableBindings(
      pattern.sourcePattern,
      sourceNode,
    );

    // Calculate overall confidence
    const confidence = this.calculateOverallConfidence(
      structuralScore,
      semanticScore,
      contextScore,
      pattern.confidence,
      pattern.successRate,
    );

    return {
      pattern,
      confidence,
      similarity: (structuralScore + semanticScore + contextScore) / 3,
      variableBindings,
      structuralScore,
      semanticScore,
      contextScore,
    };
  }

  /**
     * Calculate structural similarity between pattern and node
     */
  private calculateStructuralSimilarity(
    pattern: ASTPattern,
    node: ZeroCopyASTNode,
  ): number {
    // Check if we have cached similarity
    const cacheKey = `${pattern.nodeType}_${node.nodeId}`;
    const cached = this.similarityCache.get(cacheKey);
    if (cached) {
      return cached.structural;
    }

    let score = 0;

    // Node type match (base score) - convert to string for comparison
    if (pattern.nodeType === String(node.nodeType)) {
      score += 0.3;
    }

    // Children count similarity
    const patternChildCount = this.getPatternChildCount(pattern.structure);
    const nodeChildCount = node.getChildren().length;
    const childCountSimilarity = 1 - Math.abs(patternChildCount - nodeChildCount) /
            Math.max(patternChildCount, nodeChildCount, 1);
    score += childCountSimilarity * 0.2;

    // Structure pattern matching
    const structureScore = this.matchStructurePattern(pattern.structure, node);
    score += structureScore * 0.3;

    // Constraint satisfaction
    const constraintScore = this.evaluateConstraints(pattern.constraints, node);
    score += constraintScore * 0.2;

    // Cache the result
    this.similarityCache.set(cacheKey, {
      structural: score,
      semantic: 0,
      lexical: 0,
      contextual: 0,
      overall: score,
    });

    return Math.min(score, 1.0);
  }

  /**
     * Get expected child count from pattern structure
     */
  private getPatternChildCount(structure: PatternStructure): number {
    switch (structure.type) {
      case 'sequence':
        return structure.elements.length;
      case 'choice':
        return 1; // One of the choices
      case 'optional':
        return structure.elements.length; // May or may not be present
      case 'repetition':
        return 0; // Variable number
      case 'group':
        return structure.elements.length;
      default:
        return 0;
    }
  }

  /**
     * Match structure pattern against node
     */
  private matchStructurePattern(
    structure: PatternStructure,
    node: ZeroCopyASTNode,
  ): number {
    const children = node.getChildren();

    switch (structure.type) {
      case 'sequence':
        return this.matchSequencePattern(structure.elements, children);
      case 'choice':
        return this.matchChoicePattern(structure.elements, children);
      case 'optional':
        return this.matchOptionalPattern(structure.elements, children);
      case 'repetition':
        return this.matchRepetitionPattern(structure.elements, children);
      case 'group':
        return this.matchGroupPattern(structure.elements, children);
      default:
        return 0;
    }
  }

  /**
     * Match sequence pattern
     */
  private matchSequencePattern(
    elements: PatternElement[],
    children: ZeroCopyASTNode[],
  ): number {
    if (elements.length !== children.length) {
      return 0.5; // Partial match possible
    }

    let totalScore = 0;
    for (let i = 0; i < elements.length; i++) {
      const elementScore = this.matchPatternElement(elements[i], children[i]);
      totalScore += elementScore;
    }

    return totalScore / elements.length;
  }

  /**
     * Match choice pattern
     */
  private matchChoicePattern(
    elements: PatternElement[],
    children: ZeroCopyASTNode[],
  ): number {
    if (children.length !== 1) {
      return 0;
    }

    let bestScore = 0;
    for (const element of elements) {
      const score = this.matchPatternElement(element, children[0]);
      bestScore = Math.max(bestScore, score);
    }

    return bestScore;
  }

  /**
     * Match optional pattern
     */
  private matchOptionalPattern(
    elements: PatternElement[],
    children: ZeroCopyASTNode[],
  ): number {
    if (children.length === 0) {
      return 1.0; // Optional element not present
    }

    return this.matchSequencePattern(elements, children);
  }

  /**
     * Match repetition pattern
     */
  private matchRepetitionPattern(
    elements: PatternElement[],
    children: ZeroCopyASTNode[],
  ): number {
    if (elements.length === 0) {
      return 1.0;
    }

    const elementPattern = elements[0];
    let totalScore = 0;

    for (const child of children) {
      totalScore += this.matchPatternElement(elementPattern, child);
    }

    return children.length > 0 ? totalScore / children.length : 1.0;
  }

  /**
     * Match group pattern
     */
  private matchGroupPattern(
    elements: PatternElement[],
    children: ZeroCopyASTNode[],
  ): number {
    return this.matchSequencePattern(elements, children);
  }

  /**
     * Match individual pattern element
     */
  private matchPatternElement(
    element: PatternElement,
    node: ZeroCopyASTNode,
  ): number {
    switch (element.type) {
      case 'node':
        return node.nodeType === element.value ? 1.0 : 0.0;
      case 'literal':
        return node.value === element.value ? 1.0 : 0.0;
      case 'variable':
        return this.matchVariableElement(element, node);
      case 'wildcard':
        return 1.0; // Wildcard matches anything
      default:
        return 0.0;
    }
  }

  /**
     * Match variable element
     */
  private matchVariableElement(
    element: PatternElement,
    node: ZeroCopyASTNode,
  ): number {
    if (!element.constraints) {
      return 1.0; // Unconstrained variable
    }

    let score = 1.0;
    for (const constraint of element.constraints) {
      if (!this.evaluateConstraint(constraint, node)) {
        score *= 0.5; // Reduce score for failed constraints
      }
    }

    return score;
  }

  /**
     * Evaluate pattern constraints
     */
  private evaluateConstraints(
    constraints: PatternConstraint[],
    node: ZeroCopyASTNode,
  ): number {
    if (constraints.length === 0) {
      return 1.0;
    }

    let satisfiedCount = 0;
    for (const constraint of constraints) {
      if (this.evaluateConstraint(constraint, node)) {
        satisfiedCount++;
      }
    }

    return satisfiedCount / constraints.length;
  }

  /**
     * Evaluate single constraint
     */
  private evaluateConstraint(
    constraint: PatternConstraint,
    node: ZeroCopyASTNode,
  ): boolean {
    const propertyValue = this.getNodeProperty(node, constraint.property);

    switch (constraint.operator) {
      case 'equals':
        return propertyValue === constraint.value;
      case 'contains':
        return String(propertyValue).includes(String(constraint.value));
      case 'matches':
        return new RegExp(String(constraint.value)).test(String(propertyValue));
      case 'exists':
        return propertyValue !== undefined && propertyValue !== null;
      case 'not_exists':
        return propertyValue === undefined || propertyValue === null;
      default:
        return false;
    }
  }

  /**
     * Get property value from node
     */
  private getNodeProperty(node: ZeroCopyASTNode, property: string): any {
    const parts = property.split('.');
    let value: any = node;

    for (const part of parts) {
      if (value && typeof value === 'object') {
        value = value[part];
      } else {
        return undefined;
      }
    }

    return value;
  }

  /**
     * Calculate semantic similarity
     */
  private calculateSemanticSimilarity(
    pattern: ASTPattern,
    node: ZeroCopyASTNode,
  ): number {
    // Semantic similarity based on:
    // 1. Variable names and identifiers
    // 2. Literal values and types
    // 3. Operation types and semantics
    // 4. Control flow patterns

    let score = 0;

    // Identifier similarity
    const identifierScore = this.calculateIdentifierSimilarity(pattern, node);
    score += identifierScore * 0.3;

    // Type similarity
    const typeScore = this.calculateTypeSimilarity(pattern, node);
    score += typeScore * 0.2;

    // Operation similarity
    const operationScore = this.calculateOperationSimilarity(pattern, node);
    score += operationScore * 0.3;

    // Control flow similarity
    const controlFlowScore = this.calculateControlFlowSimilarity(pattern, node);
    score += controlFlowScore * 0.2;

    return Math.min(score, 1.0);
  }

  /**
     * Calculate identifier similarity
     */
  private calculateIdentifierSimilarity(
    pattern: ASTPattern,
    node: ZeroCopyASTNode,
  ): number {
    // Extract identifiers from pattern and node
    const patternIdentifiers = this.extractIdentifiers(pattern);
    const nodeIdentifiers = this.extractNodeIdentifiers(node);

    if (patternIdentifiers.length === 0 && nodeIdentifiers.length === 0) {
      return 1.0;
    }

    if (patternIdentifiers.length === 0 || nodeIdentifiers.length === 0) {
      return 0.5;
    }

    // Calculate Jaccard similarity
    const intersection = patternIdentifiers.filter(id => nodeIdentifiers.includes(id));
    const union = [...new Set([...patternIdentifiers, ...nodeIdentifiers])];

    return intersection.length / union.length;
  }

  /**
     * Extract identifiers from pattern
     */
  private extractIdentifiers(pattern: ASTPattern): string[] {
    const identifiers: string[] = [];

    // Extract from variables
    for (const variable of pattern.variables) {
      if (variable.type === 'identifier') {
        identifiers.push(variable.name);
      }
    }

    // Extract from structure elements
    this.extractIdentifiersFromStructure(pattern.structure, identifiers);

    return identifiers;
  }

  /**
     * Extract identifiers from structure
     */
  private extractIdentifiersFromStructure(
    structure: PatternStructure,
    identifiers: string[],
  ): void {
    for (const element of structure.elements) {
      if (element.type === 'literal' && typeof element.value === 'string') {
        // Check if it looks like an identifier
        if (/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(element.value)) {
          identifiers.push(element.value);
        }
      }
    }
  }

  /**
     * Extract identifiers from node
     */
  private extractNodeIdentifiers(node: ZeroCopyASTNode): string[] {
    const identifiers: string[] = [];

    // Traverse node and extract identifier values
    this.traverseNodeForIdentifiers(node, identifiers);

    return identifiers;
  }

  /**
     * Traverse node to find identifiers
     */
  private traverseNodeForIdentifiers(
    node: ZeroCopyASTNode,
    identifiers: string[],
  ): void {
    // Check if current node is an identifier - convert enum to string
    if (String(node.nodeType) === 'Identifier' && typeof (node as any).value === 'string') {
      identifiers.push((node as any).value);
    }

    // Recursively check children
    for (const child of node.getChildren()) {
      this.traverseNodeForIdentifiers(child, identifiers);
    }
  }

  /**
     * Calculate type similarity
     */
  private calculateTypeSimilarity(
    pattern: ASTPattern,
    node: ZeroCopyASTNode,
  ): number {
    // Extract type information from pattern and node
    const patternTypes = this.extractTypes(pattern);
    const nodeTypes = this.extractNodeTypes(node);

    if (patternTypes.length === 0 && nodeTypes.length === 0) {
      return 1.0;
    }

    if (patternTypes.length === 0 || nodeTypes.length === 0) {
      return 0.5;
    }

    // Calculate type compatibility
    let compatibleCount = 0;
    for (const patternType of patternTypes) {
      for (const nodeType of nodeTypes) {
        if (this.areTypesCompatible(patternType, nodeType)) {
          compatibleCount++;
          break;
        }
      }
    }

    return compatibleCount / Math.max(patternTypes.length, nodeTypes.length);
  }

  /**
     * Extract types from pattern
     */
  private extractTypes(pattern: ASTPattern): string[] {
    const types: string[] = [];

    // Extract from variables
    for (const variable of pattern.variables) {
      types.push(variable.type);
    }

    return types;
  }

  /**
     * Extract types from node
     */
  private extractNodeTypes(node: ZeroCopyASTNode): string[] {
    const types: string[] = [];

    // Add node type - convert enum to string
    types.push(String(node.nodeType));

    // Extract types from children
    for (const child of node.getChildren()) {
      types.push(String(child.nodeType));
    }

    return types;
  }

  /**
     * Check if types are compatible
     */
  private areTypesCompatible(type1: string, type2: string): boolean {
    // Exact match
    if (type1 === type2) {
      return true;
    }

    // Type hierarchy compatibility
    const compatibilityMap: { [key: string]: string[] } = {
      'Expression': ['Identifier', 'Literal', 'BinaryExpression', 'UnaryExpression'],
      'Statement': ['ExpressionStatement', 'IfStatement', 'ForStatement', 'WhileStatement'],
      'Declaration': ['FunctionDeclaration', 'VariableDeclaration', 'ClassDeclaration'],
    };

    for (const [parent, children] of Object.entries(compatibilityMap)) {
      if (type1 === parent && children.includes(type2)) {
        return true;
      }
      if (type2 === parent && children.includes(type1)) {
        return true;
      }
    }

    return false;
  }

  /**
     * Calculate operation similarity
     */
  private calculateOperationSimilarity(
    pattern: ASTPattern,
    node: ZeroCopyASTNode,
  ): number {
    // Extract operations from pattern and node
    const patternOps = this.extractOperations(pattern);
    const nodeOps = this.extractNodeOperations(node);

    if (patternOps.length === 0 && nodeOps.length === 0) {
      return 1.0;
    }

    if (patternOps.length === 0 || nodeOps.length === 0) {
      return 0.5;
    }

    // Calculate operation similarity
    const intersection = patternOps.filter(op => nodeOps.includes(op));
    const union = [...new Set([...patternOps, ...nodeOps])];

    return intersection.length / union.length;
  }

  /**
     * Extract operations from pattern
     */
  private extractOperations(pattern: ASTPattern): string[] {
    const operations: string[] = [];

    // Extract from constraints that specify operations
    for (const constraint of pattern.constraints) {
      if (constraint.property === 'operator' && constraint.type === 'value') {
        operations.push(String(constraint.value));
      }
    }

    return operations;
  }

  /**
     * Extract operations from node
     */
  private extractNodeOperations(node: ZeroCopyASTNode): string[] {
    const operations: string[] = [];

    // Check if node has operator property - convert enum to string first
    if (String(node.nodeType).includes('Expression') || String(node.nodeType).includes('Operator')) {
      const operator = this.getNodeProperty(node, 'operator');
      if (operator) {
        operations.push(String(operator));
      }
    }

    // Recursively check children
    for (const child of node.getChildren()) {
      operations.push(...this.extractNodeOperations(child));
    }

    return operations;
  }

  /**
     * Calculate control flow similarity
     */
  private calculateControlFlowSimilarity(
    pattern: ASTPattern,
    node: ZeroCopyASTNode,
  ): number {
    // Extract control flow patterns
    const patternFlow = this.extractControlFlow(pattern);
    const nodeFlow = this.extractNodeControlFlow(node);

    if (patternFlow.length === 0 && nodeFlow.length === 0) {
      return 1.0;
    }

    if (patternFlow.length === 0 || nodeFlow.length === 0) {
      return 0.5;
    }

    // Calculate control flow similarity
    const intersection = patternFlow.filter(flow => nodeFlow.includes(flow));
    const union = [...new Set([...patternFlow, ...nodeFlow])];

    return intersection.length / union.length;
  }

  /**
     * Extract control flow from pattern
     */
  private extractControlFlow(pattern: ASTPattern): string[] {
    const controlFlow: string[] = [];

    // Check node type for control flow constructs
    const controlFlowTypes = [
      'IfStatement', 'ForStatement', 'WhileStatement', 'DoWhileStatement',
      'SwitchStatement', 'TryStatement', 'ThrowStatement', 'ReturnStatement',
    ];

    if (controlFlowTypes.includes(pattern.nodeType)) {
      controlFlow.push(pattern.nodeType);
    }

    return controlFlow;
  }

  /**
     * Extract control flow from node
     */
  private extractNodeControlFlow(node: ZeroCopyASTNode): string[] {
    const controlFlow: string[] = [];

    // Check current node
    const controlFlowTypes = [
      'IfStatement', 'ForStatement', 'WhileStatement', 'DoWhileStatement',
      'SwitchStatement', 'TryStatement', 'ThrowStatement', 'ReturnStatement',
    ];

    if (controlFlowTypes.includes(String(node.nodeType))) {
      controlFlow.push(String(node.nodeType));
    }

    // Recursively check children
    for (const child of node.getChildren()) {
      controlFlow.push(...this.extractNodeControlFlow(child));
    }

    return controlFlow;
  }

  /**
     * Calculate context similarity
     */
  private calculateContextSimilarity(
    pattern: TranslationPattern,
    node: ZeroCopyASTNode,
  ): number {
    // Context similarity based on:
    // 1. Pattern usage history
    // 2. Node context in AST
    // 3. Surrounding patterns

    let score = 0;

    // Usage history score
    const usageScore = Math.min(pattern.usageCount / 100, 1.0);
    score += usageScore * 0.4;

    // Success rate score
    score += pattern.successRate * 0.4;

    // Complexity match score
    const complexityScore = this.calculateComplexityMatch(pattern, node);
    score += complexityScore * 0.2;

    return Math.min(score, 1.0);
  }

  /**
     * Calculate complexity match
     */
  private calculateComplexityMatch(
    pattern: TranslationPattern,
    node: ZeroCopyASTNode,
  ): number {
    const patternComplexity = pattern.metadata.complexity;
    const nodeComplexity = this.calculateNodeComplexity(node);

    // Prefer patterns with similar complexity
    const complexityDiff = Math.abs(patternComplexity - nodeComplexity);
    return Math.max(0, 1 - complexityDiff / 10);
  }

  /**
     * Calculate node complexity
     */
  private calculateNodeComplexity(node: ZeroCopyASTNode): number {
    let complexity = 1; // Base complexity

    // Add complexity for children
    for (const child of node.getChildren()) {
      complexity += this.calculateNodeComplexity(child);
    }

    // Add complexity for control flow
    const controlFlowTypes = [
      'IfStatement', 'ForStatement', 'WhileStatement', 'SwitchStatement',
    ];
    if (controlFlowTypes.includes(String(node.nodeType))) {
      complexity += 2;
    }

    return complexity;
  }

  /**
     * Extract variable bindings from pattern match
     */
  private extractVariableBindings(
    pattern: ASTPattern,
    node: ZeroCopyASTNode,
  ): Map<string, any> {
    const bindings = new Map<string, any>();

    // Extract bindings for each variable in the pattern
    for (const variable of pattern.variables) {
      const value = this.extractVariableValue(variable, node);
      if (value !== undefined) {
        bindings.set(variable.name, value);
      }
    }

    return bindings;
  }

  /**
     * Extract value for a pattern variable
     */
  private extractVariableValue(
    variable: PatternVariable,
    node: ZeroCopyASTNode,
  ): any {
    // Try to extract value based on variable constraints
    for (const constraint of variable.constraints) {
      if (constraint.type === 'attribute') {
        const value = this.getNodeProperty(node, constraint.property);
        if (value !== undefined) {
          return value;
        }
      }
    }

    // Default to node value or type
    return node.value || node.nodeType;
  }

  /**
     * Calculate overall confidence score
     */
  private calculateOverallConfidence(
    structuralScore: number,
    semanticScore: number,
    contextScore: number,
    patternConfidence: number,
    patternSuccessRate: number,
  ): number {
    // Weighted combination of all scores
    const matchScore = (structuralScore * 0.4 + semanticScore * 0.3 + contextScore * 0.3);
    const patternScore = (patternConfidence * 0.6 + patternSuccessRate * 0.4);

    return (matchScore * 0.7 + patternScore * 0.3);
  }

  /**
     * Extract pattern from successful translation
     */
  extractPattern(
    sourceNode: ZeroCopyASTNode,
    targetNode: ZeroCopyASTNode,
    context: ExtractionContext,
    feedback?: UserFeedback,
  ): PatternExtractionResult {
    // Generate pattern ID
    const patternId = this.generatePatternId(sourceNode, targetNode, context);

    // Extract source pattern
    const sourcePattern = this.extractASTPattern(sourceNode, 'source');

    // Extract target pattern
    const targetPattern = this.extractASTPattern(targetNode, 'target');

    // Calculate confidence based on context and feedback
    const confidence = this.calculateExtractionConfidence(context, feedback);

    // Determine generalization level
    const generalizationLevel = this.calculateGeneralizationLevel(sourceNode, targetNode);

    // Calculate applicability score
    const applicabilityScore = this.calculateApplicabilityScore(sourceNode, targetNode, context);

    // Create pattern
    const pattern: TranslationPattern = {
      id: patternId,
      name: this.generatePatternName(sourceNode, targetNode),
      description: this.generatePatternDescription(sourceNode, targetNode),
      sourceLanguage: context.sourceLanguage,
      targetLanguage: context.targetLanguage,
      sourcePattern,
      targetPattern,
      confidence,
      usageCount: 1,
      successRate: feedback?.action === UserAction.ACCEPT ? 1.0 : 0.5,
      metadata: {
        author: 'system',
        version: '1.0',
        tags: this.generatePatternTags(sourceNode, targetNode),
        complexity: context.complexity,
        performance: {
          averageTime: 0,
          memoryUsage: 0,
          successRate: feedback?.action === UserAction.ACCEPT ? 1.0 : 0.5,
          errorRate: 0,
        },
        examples: [{
          sourceCode: this.nodeToCode(sourceNode),
          targetCode: this.nodeToCode(targetNode),
          description: 'Initial extraction example',
          context: context.domain || 'general',
        }],
      },
    };

    return {
      pattern,
      confidence,
      generalizationLevel,
      applicabilityScore,
      examples: [{
        sourceNode,
        targetNode,
        context,
        quality: confidence,
      }],
    };
  }

  /**
     * Extract AST pattern from node
     */
  private extractASTPattern(
    node: ZeroCopyASTNode,
    role: 'source' | 'target',
  ): ASTPattern {
    // Extract structure
    const structure = this.extractPatternStructure(node);

    // Extract constraints
    const constraints = this.extractPatternConstraints(node);

    // Extract variables
    const variables = this.extractPatternVariables(node);

    return {
      nodeType: String(node.nodeType),
      structure,
      constraints,
      variables,
    };
  }

  /**
     * Extract pattern structure from node
     */
  private extractPatternStructure(node: ZeroCopyASTNode): PatternStructure {
    const children = node.getChildren();
    const elements: PatternElement[] = [];

    for (const child of children) {
      elements.push({
        type: 'node',
        value: child.nodeType,
        constraints: this.extractElementConstraints(child),
      });
    }

    return {
      type: 'sequence',
      elements,
    };
  }

  /**
     * Extract constraints for pattern element
     */
  private extractElementConstraints(node: ZeroCopyASTNode): PatternConstraint[] {
    const constraints: PatternConstraint[] = [];

    // Add type constraint
    constraints.push({
      type: 'type',
      property: 'nodeType',
      operator: 'equals',
      value: node.nodeType,
    });

    // Add value constraint if node has a value
    if (node.value !== null && node.value !== undefined) {
      constraints.push({
        type: 'value',
        property: 'value',
        operator: 'equals',
        value: node.value,
      });
    }

    return constraints;
  }

  /**
     * Extract pattern constraints from node
     */
  private extractPatternConstraints(node: ZeroCopyASTNode): PatternConstraint[] {
    const constraints: PatternConstraint[] = [];

    // Add node type constraint
    constraints.push({
      type: 'type',
      property: 'nodeType',
      operator: 'equals',
      value: node.nodeType,
    });

    // Add specific constraints based on node type
    if (node.nodeType === ASTNodeType.BINARY_OP) {
      const operator = this.getNodeProperty(node, 'operator');
      if (operator) {
        constraints.push({
          type: 'attribute',
          property: 'operator',
          operator: 'equals',
          value: operator,
        });
      }
    }

    return constraints;
  }

  /**
     * Extract pattern variables from node
     */
  private extractPatternVariables(node: ZeroCopyASTNode): PatternVariable[] {
    const variables: PatternVariable[] = [];

    // Extract variables from identifiers and literals
    this.extractVariablesFromNode(node, variables);

    return variables;
  }

  /**
     * Extract variables from node recursively
     */
  private extractVariablesFromNode(
    node: ZeroCopyASTNode,
    variables: PatternVariable[],
  ): void {
    // Check if node represents a variable - convert enum to string
    if (String(node.nodeType) === 'Identifier' && typeof (node as any).value === 'string') {
      variables.push({
        name: (node as any).value,
        type: 'identifier',
        constraints: [{
          type: 'type',
          property: 'nodeType',
          operator: 'equals',
          value: 'Identifier',
        }],
      });
    }

    // Check children
    for (const child of node.getChildren()) {
      this.extractVariablesFromNode(child, variables);
    }
  }

  /**
     * Calculate extraction confidence
     */
  private calculateExtractionConfidence(
    context: ExtractionContext,
    feedback?: UserFeedback,
  ): number {
    let confidence = 0.5; // Base confidence

    // Adjust based on user feedback
    if (feedback) {
      switch (feedback.action) {
        case UserAction.ACCEPT:
          confidence += 0.3;
          break;
        case UserAction.MODIFY:
          confidence += 0.1;
          break;
        case UserAction.REJECT:
          confidence -= 0.2;
          break;
      }

      // Adjust based on rating
      if (feedback.rating !== undefined) {
        confidence += (feedback.rating - 3) * 0.1; // Rating 1-5, center at 3
      }
    }

    // Adjust based on context complexity
    confidence += Math.max(0, (5 - context.complexity) * 0.05);

    // Adjust based on user rating
    if (context.userRating !== undefined) {
      confidence += (context.userRating - 3) * 0.1;
    }

    return Math.max(0, Math.min(1, confidence));
  }

  /**
     * Calculate generalization level
     */
  private calculateGeneralizationLevel(
    sourceNode: ZeroCopyASTNode,
    targetNode: ZeroCopyASTNode,
  ): number {
    // Higher generalization for simpler, more common patterns
    const sourceComplexity = this.calculateNodeComplexity(sourceNode);
    const targetComplexity = this.calculateNodeComplexity(targetNode);
    const avgComplexity = (sourceComplexity + targetComplexity) / 2;

    return Math.max(0, Math.min(1, (10 - avgComplexity) / 10));
  }

  /**
     * Calculate applicability score
     */
  private calculateApplicabilityScore(
    sourceNode: ZeroCopyASTNode,
    targetNode: ZeroCopyASTNode,
    context: ExtractionContext,
  ): number {
    let score = 0.5; // Base score

    // Common node types are more applicable
    const commonTypes = [
      ASTNodeType.IF_STATEMENT, ASTNodeType.FOR_LOOP, ASTNodeType.FUNCTION_DECLARATION,
      ASTNodeType.VARIABLE_DECLARATION, ASTNodeType.BINARY_OP, ASTNodeType.FUNCTION_CALL,
    ];

    if (commonTypes.includes(sourceNode.nodeType)) {
      score += 0.2;
    }

    // Simpler patterns are more applicable
    const complexity = context.complexity;
    score += Math.max(0, (5 - complexity) * 0.1);

    return Math.max(0, Math.min(1, score));
  }

  /**
     * Generate pattern ID
     */
  private generatePatternId(
    sourceNode: ZeroCopyASTNode,
    targetNode: ZeroCopyASTNode,
    context: ExtractionContext,
  ): string {
    const sourceType = sourceNode.nodeType;
    const targetType = targetNode.nodeType;
    const languages = `${context.sourceLanguage}_to_${context.targetLanguage}`;
    const timestamp = Date.now();

    return `${languages}_${sourceType}_${targetType}_${timestamp}`;
  }

  /**
     * Generate pattern name
     */
  private generatePatternName(
    sourceNode: ZeroCopyASTNode,
    targetNode: ZeroCopyASTNode,
  ): string {
    return `${sourceNode.nodeType} to ${targetNode.nodeType}`;
  }

  /**
     * Generate pattern description
     */
  private generatePatternDescription(
    sourceNode: ZeroCopyASTNode,
    targetNode: ZeroCopyASTNode,
  ): string {
    return `Translates ${sourceNode.nodeType} constructs to ${targetNode.nodeType} equivalents`;
  }

  /**
     * Generate pattern tags
     */
  private generatePatternTags(
    sourceNode: ZeroCopyASTNode,
    targetNode: ZeroCopyASTNode,
  ): string[] {
    const tags: string[] = [];

    // Add node type tags
    tags.push(ASTNodeType[sourceNode.nodeType].toLowerCase());
    tags.push(ASTNodeType[targetNode.nodeType].toLowerCase());

    // Add category tags
    const categories = this.categorizeNodeType(ASTNodeType[sourceNode.nodeType]);
    tags.push(...categories);

    return tags;
  }

  /**
     * Categorize node type
     */
  private categorizeNodeType(nodeType: string): string[] {
    const categories: string[] = [];

    if (nodeType.includes('Statement')) {
      categories.push('statement');
    }
    if (nodeType.includes('Expression')) {
      categories.push('expression');
    }
    if (nodeType.includes('Declaration')) {
      categories.push('declaration');
    }
    if (nodeType.includes('Function')) {
      categories.push('function');
    }
    if (nodeType.includes('Class')) {
      categories.push('class');
    }

    return categories;
  }

  /**
     * Convert node to code string
     */
  private nodeToCode(node: ZeroCopyASTNode): string {
    // This is a simplified implementation
    // In practice, this would use a proper code generator
    return `${node.nodeType}(${node.value || ''})`;
  }

  /**
     * Add pattern to library
     */
  addPattern(pattern: TranslationPattern): void {
    this.patterns.set(pattern.id, pattern);
    this.learningStatistics.totalPatterns++;
    this.updateLearningStatistics();
  }

  /**
     * Update pattern in library
     */
  updatePattern(pattern: TranslationPattern): void {
    this.patterns.set(pattern.id, pattern);
    this.updateLearningStatistics();
  }

  /**
     * Remove pattern from library
     */
  removePattern(patternId: string): boolean {
    const removed = this.patterns.delete(patternId);
    if (removed) {
      this.learningStatistics.totalPatterns--;
      this.updateLearningStatistics();
    }
    return removed;
  }

  /**
     * Get pattern by ID
     */
  getPattern(patternId: string): TranslationPattern | undefined {
    return this.patterns.get(patternId);
  }

  /**
     * Get all patterns
     */
  getAllPatterns(): TranslationPattern[] {
    return Array.from(this.patterns.values());
  }

  /**
     * Update learning statistics
     */
  private updateLearningStatistics(): void {
    const patterns = Array.from(this.patterns.values());

    if (patterns.length > 0) {
      this.learningStatistics.averageConfidence =
                patterns.reduce((sum, p) => sum + p.confidence, 0) / patterns.length;
    }

    this.learningStatistics.lastUpdated = new Date();
  }

  /**
     * Get learning statistics
     */
  getLearningStatistics(): LearningStatistics {
    return { ...this.learningStatistics };
  }

  /**
     * Clear similarity cache
     */
  clearCache(): void {
    this.similarityCache.clear();
  }

  /**
     * Get cache statistics
     */
  getCacheStatistics(): { size: number; hitRate: number } {
    return {
      size: this.similarityCache.size,
      hitRate: 0, // Would need to track hits/misses for actual hit rate
    };
  }
}

