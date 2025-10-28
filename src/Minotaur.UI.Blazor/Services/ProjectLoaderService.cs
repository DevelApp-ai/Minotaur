using System;
using System.Collections.Generic;
using SystemIO = System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Minotaur.UI.Blazor.Services
{
    /// <summary>
    /// Service for loading and parsing entire projects at once
    /// Phase 2 Implementation: Bulk Project Loading
    /// </summary>
    public class ProjectLoaderService
    {
        public event EventHandler<ProjectLoadProgressEventArgs>? ProgressChanged;

        public class ProjectLoadProgressEventArgs : EventArgs
        {
            public int FilesProcessed { get; set; }
            public int TotalFiles { get; set; }
            public string CurrentFile { get; set; } = "";
            public double PercentageComplete => TotalFiles > 0 ? (double)FilesProcessed / TotalFiles * 100 : 0;
        }

        public class ProjectMetrics
        {
            public int TotalFiles { get; set; }
            public int TotalLines { get; set; }
            public Dictionary<string, int> FilesByLanguage { get; set; } = new();
            public Dictionary<string, int> ComplexityByFile { get; set; } = new();
            public List<string> Dependencies { get; set; } = new();
        }

        /// <summary>
        /// Load and parse an entire project directory
        /// </summary>
        public async Task<ProjectMetrics> LoadProjectAsync(string projectPath, string[] fileExtensions)
        {
            var metrics = new ProjectMetrics();

            if (!SystemIO.Directory.Exists(projectPath))
                throw new SystemIO.DirectoryNotFoundException($"Project path not found: {projectPath}");

            // Get all files matching extensions
            var files = fileExtensions
                .SelectMany(ext => SystemIO.Directory.GetFiles(projectPath, $"*{ext}", SystemIO.SearchOption.AllDirectories))
                .ToList();

            metrics.TotalFiles = files.Count;

            // Process files in parallel for performance
            var processed = 0;
            var tasks = files.Select(async (file, index) =>
            {
                await ProcessFileAsync(file, metrics);
                processed++;

                // Report progress
                ProgressChanged?.Invoke(this, new ProjectLoadProgressEventArgs
                {
                    FilesProcessed = processed,
                    TotalFiles = files.Count,
                    CurrentFile = SystemIO.Path.GetFileName(file)
                });
            });

            await Task.WhenAll(tasks);

            return metrics;
        }

        private async Task ProcessFileAsync(string filePath, ProjectMetrics metrics)
        {
            try
            {
                var lines = await SystemIO.File.ReadAllLinesAsync(filePath);
                metrics.TotalLines += lines.Length;

                // Detect language
                var extension = SystemIO.Path.GetExtension(filePath);
                if (!metrics.FilesByLanguage.ContainsKey(extension))
                    metrics.FilesByLanguage[extension] = 0;
                metrics.FilesByLanguage[extension]++;

                // Calculate basic complexity (simple heuristic: count of control flow statements)
                var complexity = CalculateComplexity(lines);
                metrics.ComplexityByFile[SystemIO.Path.GetFileName(filePath)] = complexity;
            }
            catch (Exception)
            {
                // Skip files that can't be read
            }
        }

        private int CalculateComplexity(string[] lines)
        {
            var complexity = 0;
            var controlFlowKeywords = new[] { "if", "for", "while", "switch", "case", "catch", "&&", "||" };

            foreach (var line in lines)
            {
                foreach (var keyword in controlFlowKeywords)
                {
                    if (line.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        complexity++;
                }
            }

            return complexity;
        }
    }
}
