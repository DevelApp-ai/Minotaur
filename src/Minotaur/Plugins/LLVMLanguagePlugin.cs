/*
 * This file is part of Minotaur.
 * 
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Minotaur is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with Minotaur. If not, see <https://www.gnu.org/licenses/>.
 */

using Minotaur.Core;

namespace Minotaur.Plugins;

/// <summary>
/// LLVM language plugin for generating LLVM IR from cognitive graphs.
/// This plugin implements the LLVM intermediate language feasibility report specifications,
/// generating native parsers through LLVM IR compilation.
/// </summary>
public class LLVMLanguagePlugin : ILanguagePlugin
{
    public string LanguageId => "llvm";
    public string DisplayName => "LLVM IR";
    public string[] SupportedExtensions => new[] { ".ll", ".bc" };

    public async Task<string> UnparseAsync(CognitiveGraphNode graph)
    {
        // Generate LLVM IR from cognitive graph
        var visitor = new LLVMUnparseVisitor();
        visitor.Visit(graph);

        await Task.CompletedTask;
        return visitor.GetGeneratedLLVMIR();
    }

    public async Task<CompilerBackendRules> GenerateCompilerBackendRulesAsync()
    {
        var rules = new CompilerBackendRules
        {
            LanguageId = LanguageId
        };

        // Add LLVM IR generation rules for parser components
        rules.GenerationRules.AddRange(new[]
        {
            new CodeGenerationRule
            {
                NodeType = "parser_function",
                GenerationTemplate = "define %ast_node* @parse_{rule_name}(%parser_state* %state) {\nentry:\n{body}\nret %ast_node* %result\n}",
                GenerationHints = new Dictionary<string, object>
                {
                    ["SSAForm"] = true,
                    ["OptimizationLevel"] = "O2",
                    ["TargetTriple"] = "x86_64-unknown-linux-gnu"
                }
            },
            new CodeGenerationRule
            {
                NodeType = "lexer_function",
                GenerationTemplate = "define i32 @lex_{token_name}(%lexer_state* %state) {\nentry:\n{body}\nret i32 %token_type\n}",
                GenerationHints = new Dictionary<string, object>
                {
                    ["StateTransition"] = true,
                    ["FastPath"] = true
                }
            },
            new CodeGenerationRule
            {
                NodeType = "ast_creation",
                GenerationTemplate = "define %ast_node* @create_{node_type}({parameters}) {\nentry:\n{allocation}\n{initialization}\nret %ast_node* %node\n}",
                GenerationHints = new Dictionary<string, object>
                {
                    ["MemoryManagement"] = "malloc",
                    ["ZeroCopy"] = true
                }
            },
            new CodeGenerationRule
            {
                NodeType = "state_machine",
                GenerationTemplate = "define i32 @state_machine_%{name}(%parser_context* %ctx) {\nentry:\n%current_state = load i32, i32* getelementptr inbounds (%parser_context, %parser_context* %ctx, i32 0, i32 0)\nswitch i32 %current_state, label %error_state [\n{state_cases}\n]\n{state_blocks}\n}",
                GenerationHints = new Dictionary<string, object>
                {
                    ["ControlFlowOptimization"] = true,
                    ["BranchPrediction"] = true
                }
            },
            new CodeGenerationRule
            {
                NodeType = "error_recovery",
                GenerationTemplate = "define i32 @error_recovery(%parser_state* %state, i32 %error_code) {\nentry:\n{sync_token_check}\n{recovery_strategy}\nret i32 %recovery_result\n}",
                GenerationHints = new Dictionary<string, object>
                {
                    ["ErrorStrategy"] = "PanicMode",
                    ["SynchronizationTokens"] = new[] { "semicolon", "rbrace", "eof" }
                }
            }
        });

        // Add LLVM-specific template rules
        rules.TemplateRules.AddRange(new[]
        {
            new TemplateRule
            {
                TemplateName = "module_header",
                TemplateContent = "; Generated LLVM IR for {grammar_name} parser\n; Target: {target_triple}\n; Generated: {timestamp}\n\ntarget datalayout = \"{data_layout}\"\ntarget triple = \"{target_triple}\"\n",
                RequiredParameters = new List<string> { "grammar_name", "target_triple", "timestamp", "data_layout" },
                TemplateMetadata = new Dictionary<string, object>
                {
                    ["DefaultTargetTriple"] = "x86_64-unknown-linux-gnu",
                    ["DefaultDataLayout"] = "e-m:e-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
                }
            },
            new TemplateRule
            {
                TemplateName = "type_definitions",
                TemplateContent = "; Type definitions for parser data structures\n%parser_state = type { i32, i8*, i32, i32, i32 }\n%token = type { i32, i8*, i32, i32 }\n%ast_node = type { i32, i8*, %ast_node**, i32 }\n%parser_context = type { %parser_state*, %token*, %ast_node* }\n",
                RequiredParameters = new List<string>(),
                TemplateMetadata = new Dictionary<string, object>
                {
                    ["StructAlignment"] = 8,
                    ["PointerSize"] = 64
                }
            },
            new TemplateRule
            {
                TemplateName = "external_functions",
                TemplateContent = "; External function declarations\ndeclare i8* @malloc(i64) nounwind\ndeclare void @free(i8*) nounwind\ndeclare i8* @memcpy(i8*, i8*, i64) nounwind\ndeclare i32 @printf(i8*, ...) nounwind\n",
                RequiredParameters = new List<string>(),
                TemplateMetadata = new Dictionary<string, object>
                {
                    ["CallingConvention"] = "ccc",
                    ["MemoryManagement"] = "manual"
                }
            },
            new TemplateRule
            {
                TemplateName = "optimization_attributes",
                TemplateContent = "; Function attributes for optimization\nattributes #0 = { nounwind uwtable \"frame-pointer\"=\"non-leaf\" \"min-legal-vector-width\"=\"0\" \"no-trapping-math\"=\"true\" \"stack-protector-buffer-size\"=\"8\" \"target-cpu\"=\"x86-64\" \"target-features\"=\"+cx8,+fxsr,+mmx,+sse,+sse2,+x87\" \"tune-cpu\"=\"generic\" }\nattributes #1 = { \"frame-pointer\"=\"non-leaf\" \"no-trapping-math\"=\"true\" \"stack-protector-buffer-size\"=\"8\" \"target-cpu\"=\"x86-64\" \"target-features\"=\"+cx8,+fxsr,+mmx,+sse,+sse2,+x87\" \"tune-cpu\"=\"generic\" }\n",
                RequiredParameters = new List<string>(),
                TemplateMetadata = new Dictionary<string, object>
                {
                    ["OptimizationLevel"] = "O2",
                    ["TargetCPU"] = "x86-64"
                }
            }
        });

        // Add LLVM backend metadata
        rules.BackendMetadata = new Dictionary<string, object>
        {
            ["CompilerType"] = "LLVM",
            ["IRVersion"] = "15.0",
            ["TargetArchitectures"] = new[] { "x86_64", "aarch64", "arm", "riscv64", "wasm32" },
            ["OptimizationPasses"] = new[]
            {
                "mem2reg", "instcombine", "reassociate", "gvn", "simplifycfg",
                "tailcallelim", "inline", "argpromotion", "scalarrepl", "earlyCSE",
                "correlated-propagation", "loop-unroll"
            },
            ["CrossCompilation"] = true,
            ["NativeCodeGeneration"] = true,
            ["PerformanceOptimization"] = "aggressive",
            ["MemoryModel"] = "zero-copy",
            ["ErrorRecovery"] = "panic-mode-with-synchronization",
            ["DebuggingSupport"] = true,
            ["ProfilingSupport"] = true
        };

        await Task.CompletedTask;
        return rules;
    }

    public CodeFormattingOptions GetFormattingOptions()
    {
        return new CodeFormattingOptions
        {
            IndentStyle = "spaces",
            IndentSize = 2,
            LineEnding = "\n",
            InsertTrailingNewline = true,
            MaxLineLength = 120,
            CosmeticOptions = new Dictionary<string, object>
            {
                ["CommentStyle"] = "semicolon", // LLVM IR comments start with ;
                ["AlignInstructions"] = true,
                ["CompactMetadata"] = false,
                ["ExplicitTypes"] = true, // LLVM IR requires explicit types
                ["SSANaming"] = "descriptive", // Use descriptive SSA value names
                ["BasicBlockNaming"] = "semantic" // Use semantic basic block names
            }
        };
    }

    public async Task<UnparseValidationResult> ValidateGraphForUnparsingAsync(CognitiveGraphNode graph)
    {
        var result = new UnparseValidationResult { CanUnparse = true };

        if (graph == null)
        {
            result.CanUnparse = false;
            result.Errors.Add(new UnparseValidationError
            {
                Message = "Cannot generate LLVM IR from null cognitive graph",
                NodeId = "null",
                NodeType = "null",
                Severity = "Error"
            });
            return result;
        }

        // Validate that the graph represents a parseable structure
        var validator = new LLVMIRValidationVisitor();
        validator.Visit(graph);

        if (validator.HasStructuralIssues)
        {
            result.CanUnparse = false;
            result.Errors.AddRange(validator.ValidationErrors);
        }

        if (validator.HasPerformanceWarnings)
        {
            result.Warnings.AddRange(validator.PerformanceWarnings);
        }

        // Check for LLVM-specific requirements
        if (!ContainsParserEntryPoint(graph))
        {
            result.Warnings.Add(new UnparseValidationWarning
            {
                Message = "No parser entry point detected - will generate basic IR structure",
                NodeId = graph.GetHashCode().ToString(),
                NodeType = graph.GetType().Name
            });
        }

        if (!ContainsErrorHandling(graph))
        {
            result.Warnings.Add(new UnparseValidationWarning
            {
                Message = "No error handling detected - will generate basic error recovery",
                NodeId = graph.GetHashCode().ToString(),
                NodeType = graph.GetType().Name
            });
        }

        await Task.CompletedTask;
        return result;
    }

    private bool ContainsParserEntryPoint(CognitiveGraphNode graph)
    {
        // Check if the graph contains nodes that represent parser entry points
        var visitor = new EntryPointDetectionVisitor();
        visitor.Visit(graph);
        return visitor.HasEntryPoint;
    }

    private bool ContainsErrorHandling(CognitiveGraphNode graph)
    {
        // Check if the graph contains error handling constructs
        var visitor = new ErrorHandlingDetectionVisitor();
        visitor.Visit(graph);
        return visitor.HasErrorHandling;
    }
}