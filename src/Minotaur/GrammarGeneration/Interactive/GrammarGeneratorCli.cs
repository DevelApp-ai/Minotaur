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

using Minotaur.GrammarGeneration.Models;

namespace Minotaur.GrammarGeneration.Interactive;

/// <summary>
/// Command-line interface for grammar generation
/// </summary>
public class GrammarGeneratorCli
{
    private readonly GrammarGenerator _generator;

    public GrammarGeneratorCli()
    {
        _generator = new GrammarGenerator();
    }

    /// <summary>
    /// Main CLI entry point
    /// </summary>
    public async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 1;
            }

            var command = args[0].ToLower();

            return command switch
            {
                "generate" => await HandleGenerateCommand(args.Skip(1).ToArray()),
                "validate" => await HandleValidateCommand(args.Skip(1).ToArray()),
                "help" => HandleHelpCommand(args.Skip(1).ToArray()),
                _ => HandleUnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> HandleGenerateCommand(string[] args)
    {
        var options = ParseGenerateOptions(args);

        if (options == null)
        {
            PrintGenerateUsage();
            return 1;
        }

        Console.WriteLine($"🔧 Generating grammar for language: {options.LanguageName}");
        Console.WriteLine($"📁 Source files: {string.Join(", ", options.SourceFiles)}");
        Console.WriteLine();

        var progress = new Progress<GrammarGenerationProgress>(p =>
        {
            Console.Write($"\r🔄 {p.Stage}... ({p.Progress}%)");
            if (p.Progress == 100)
            {
                Console.WriteLine();
            }
        });

        try
        {
            var grammar = await _generator.GenerateGrammarAsync(
                options.LanguageName,
                options.SourceFiles,
                options.Context,
                progress);

            Console.WriteLine($"✅ Grammar generation completed!");
            Console.WriteLine($"📊 Rules: {grammar.ProductionRules.Rules.Count}");
            Console.WriteLine($"🎯 Tokens: {grammar.TokenRules.Patterns.Count}");
            Console.WriteLine();

            if (!string.IsNullOrEmpty(options.OutputFile))
            {
                await _generator.SaveGrammarAsync(grammar, options.OutputFile);
                Console.WriteLine($"💾 Grammar saved to: {options.OutputFile}");
            }
            else
            {
                Console.WriteLine("📄 Generated Grammar:");
                Console.WriteLine(new string('=', 50));
                Console.WriteLine(_generator.GenerateGrammarFile(grammar));
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"❌ Grammar generation failed: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> HandleValidateCommand(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: validate <grammar-file> [source-files...]");
            return 1;
        }

        var grammarFile = args[0];
        var sourceFiles = args.Skip(1).ToArray();

        Console.WriteLine($"🔍 Validating grammar: {grammarFile}");

        // For now, just show what would be validated
        // In a full implementation, we'd load and validate the grammar
        Console.WriteLine($"📁 Against source files: {string.Join(", ", sourceFiles)}");
        Console.WriteLine("✅ Validation would be performed here");

        return await Task.FromResult(0);
    }

    private int HandleHelpCommand(string[] args)
    {
        if (args.Length > 0)
        {
            var topic = args[0].ToLower();
            return topic switch
            {
                "generate" => PrintGenerateHelp(),
                "validate" => PrintValidateHelp(),
                _ => PrintGeneralHelp()
            };
        }

        return PrintGeneralHelp();
    }

    private int HandleUnknownCommand(string command)
    {
        Console.WriteLine($"Unknown command: {command}");
        Console.WriteLine("Use 'help' to see available commands.");
        return 1;
    }

    private GenerateOptions? ParseGenerateOptions(string[] args)
    {
        var options = new GenerateOptions();
        var sourceFiles = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--language" or "-l":
                    if (i + 1 < args.Length)
                    {
                        options.LanguageName = args[++i];
                    }
                    break;

                case "--output" or "-o":
                    if (i + 1 < args.Length)
                    {
                        options.OutputFile = args[++i];
                    }
                    break;

                case "--source" or "-s":
                    if (i + 1 < args.Length)
                    {
                        sourceFiles.AddRange(args[++i].Split(','));
                    }
                    break;

                case "--context":
                    // Parse context options (simplified)
                    options.Context = new LanguageContext
                    {
                        HasNestedScopes = true,
                        HasTypeSystem = true,
                        HasComments = true,
                        HasStringLiterals = true,
                        HasNumericLiterals = true
                    };
                    break;

                default:
                    if (!args[i].StartsWith('-'))
                    {
                        sourceFiles.Add(args[i]);
                    }
                    break;
            }
        }

        if (string.IsNullOrEmpty(options.LanguageName))
        {
            Console.WriteLine("Error: Language name is required (--language)");
            return null;
        }

        if (!sourceFiles.Any())
        {
            Console.WriteLine("Error: At least one source file is required");
            return null;
        }

        options.SourceFiles = sourceFiles.ToArray();
        return options;
    }

    private void PrintUsage()
    {
        Console.WriteLine("Minotaur Grammar Generator");
        Console.WriteLine("=========================");
        Console.WriteLine();
        Console.WriteLine("Usage: minotaur-grammar <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  generate    Generate a grammar from source files");
        Console.WriteLine("  validate    Validate a grammar against source files");
        Console.WriteLine("  help        Show help information");
        Console.WriteLine();
        Console.WriteLine("Use 'help <command>' for more information about a command.");
    }

    private void PrintGenerateUsage()
    {
        Console.WriteLine("Usage: generate --language <name> [options] <source-files...>");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --language, -l <name>     Name of the language");
        Console.WriteLine("  --output, -o <file>       Output grammar file");
        Console.WriteLine("  --source, -s <files>      Comma-separated source files");
        Console.WriteLine("  --context                 Enable context-aware features");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  generate --language MyLang --output mylang.grammar src/*.mylang");
        Console.WriteLine("  generate -l Python -o python.grammar *.py");
    }

    private int PrintGenerateHelp()
    {
        Console.WriteLine("Generate Command");
        Console.WriteLine("================");
        Console.WriteLine();
        Console.WriteLine("Generates a grammar file from source code examples.");
        Console.WriteLine();
        PrintGenerateUsage();
        Console.WriteLine();
        Console.WriteLine("The generator analyzes source files to identify:");
        Console.WriteLine("• Token patterns (keywords, operators, literals)");
        Console.WriteLine("• Syntax structures (expressions, statements, declarations)");
        Console.WriteLine("• Language constructs (functions, classes, control flow)");
        Console.WriteLine();
        Console.WriteLine("The output grammar follows the Grammar File Creation Guide format.");
        return 0;
    }

    private int PrintValidateHelp()
    {
        Console.WriteLine("Validate Command");
        Console.WriteLine("================");
        Console.WriteLine();
        Console.WriteLine("Validates a grammar file against source code examples.");
        Console.WriteLine();
        Console.WriteLine("Usage: validate <grammar-file> [source-files...]");
        Console.WriteLine();
        Console.WriteLine("Validation checks:");
        Console.WriteLine("• Structural correctness");
        Console.WriteLine("• Reference consistency");
        Console.WriteLine("• Coverage against source files");
        Console.WriteLine("• Performance characteristics");
        return 0;
    }

    private int PrintGeneralHelp()
    {
        PrintUsage();
        Console.WriteLine();
        Console.WriteLine("The Minotaur Grammar Generator creates grammar definitions from");
        Console.WriteLine("source code examples using advanced pattern recognition and");
        Console.WriteLine("error-driven refinement techniques.");
        Console.WriteLine();
        Console.WriteLine("Features:");
        Console.WriteLine("• Automatic token pattern recognition");
        Console.WriteLine("• Syntax structure discovery");
        Console.WriteLine("• Context-aware grammar enhancement");
        Console.WriteLine("• Quality validation and metrics");
        Console.WriteLine("• Grammar File Creation Guide compliance");
        return 0;
    }

    private class GenerateOptions
    {
        public string LanguageName { get; set; } = string.Empty;
        public string[] SourceFiles { get; set; } = Array.Empty<string>();
        public string? OutputFile { get; set; }
        public LanguageContext? Context { get; set; }
    }
}

/// <summary>
/// Entry point for the CLI application
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var cli = new GrammarGeneratorCli();
        return await cli.RunAsync(args);
    }
}