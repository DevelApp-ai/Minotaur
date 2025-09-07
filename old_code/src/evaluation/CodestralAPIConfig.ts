/**
 * Codestral API Configuration Helper
 * 
 * Provides proper configuration for the Codestral API integration
 * using the correct endpoint and API key with optimized token limits.
 */

import { MistralAPIConfig, createDefaultMistralConfig } from './MistralAPIClient';

/**
 * Create Codestral API configuration with the provided API key
 */
export function createCodestralConfig(apiKey?: string): MistralAPIConfig {
    // Use provided API key or environment variable
    const key = apiKey || process.env.CODESTRAL_API_KEY || process.env.MISTRAL_API_KEY || 'wXvVGk9dTtqSVZbqLOmOGEXHLdkpOhuA';
    
    const config = createDefaultMistralConfig(key);
    
    // Override with Codestral-specific settings optimized for 1 req/sec limit
    return {
        ...config,
        baseURL: 'https://api.mistral.ai/v1', // Correct working endpoint
        model: 'codestral-latest',
        maxRetries: 3,
        retryDelay: 2000, // 2 second delay between retries
        timeout: 120000, // 2 minutes for large responses
        rateLimit: {
            requestsPerMinute: 60,        // 1 req/sec (API limit)
            requestsPerHour: 3600,        // 1 req/sec sustained
            tokensPerMinute: 1920000,     // 60 requests × 32000 tokens
            tokensPerHour: 115200000,     // 3600 requests × 32000 tokens
            burstLimit: 1,                // Strict 1 per second
            adaptiveThrottling: true,
        },
        logLevel: 'info',
    };
}

/**
 * Get optimal request configuration for code generation
 */
export function getOptimalRequestConfig() {
    return {
        maxTokens: 32000,           // Use full context window
        temperature: 0.1,           // Low temperature for consistent code
        topP: 0.95,                // Slightly focused sampling
        safePrompt: false,          // Allow all code generation
    };
}

/**
 * Environment variable names for API configuration
 */
export const API_ENV_VARS = {
    CODESTRAL_API_KEY: 'CODESTRAL_API_KEY',
    MISTRAL_API_KEY: 'MISTRAL_API_KEY',
    BASELINE_TESTS: 'BASELINE_TESTS',
    EVALUATION_MODE: 'EVALUATION_MODE',
    PYTHON_PATH: 'PYTHON_PATH',
} as const;

/**
 * Get API key from environment or use default
 */
export function getAPIKey(): string {
    return process.env.CODESTRAL_API_KEY || 
           process.env.MISTRAL_API_KEY || 
           'wXvVGk9dTtqSVZbqLOmOGEXHLdkpOhuA';
}

/**
 * Get evaluation configuration from environment
 */
export function getEvaluationConfig() {
    return {
        baselineTests: process.env.BASELINE_TESTS ? parseInt(process.env.BASELINE_TESTS, 10) : 10,
        evaluationMode: process.env.EVALUATION_MODE || 'quick',
        pythonPath: process.env.PYTHON_PATH || 'python3',
        apiKey: getAPIKey(),
    };
}

