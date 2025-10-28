using Microsoft.Playwright;
using System.Text;

namespace UIFlowDocumentation;

class Program
{
    private static readonly List<PageInfo> Pages = new()
    {
        new PageInfo("/", "Home", "Landing page with overview of Minotaur features"),
        new PageInfo("/grammar-editor", "Grammar Editor", "Interactive grammar editing and validation interface"),
        new PageInfo("/marketplace", "Marketplace", "Browse and download grammar templates and plugins"),
        new PageInfo("/tutorial", "Interactive Tutorial", "Step-by-step interactive tutorial for learning Minotaur"),
        new PageInfo("/step-parser", "StepParser Integration", "Parse source code to cognitive graphs and visualize"),
        new PageInfo("/symbolic-analyzer", "Symbolic Analyzer", "Advanced code analysis and verification tools"),
        new PageInfo("/version-control", "Version Control", "Grammar version control and history management"),
        new PageInfo("/plugin-manager", "Plugin Manager", "Manage language plugins and extensions"),
        new PageInfo("/project-manager", "Project Manager", "Manage grammar projects and configurations"),
        new PageInfo("/counter", "Counter Demo", "Simple counter demo page"),
        new PageInfo("/weather", "Weather Demo", "Weather forecast demo page")
    };

    static async Task<int> Main(string[] args)
    {
        var baseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
        var outputDir = args.Length > 1 ? args[1] : "docs/ui-screenshots";

        Console.WriteLine("=== Minotaur UI Flow Documentation Tool ===");
        Console.WriteLine($"Base URL: {baseUrl}");
        Console.WriteLine($"Output Directory: {outputDir}");
        Console.WriteLine();

        // Create output directory
        Directory.CreateDirectory(outputDir);

        // Install Playwright browsers if needed
        Console.WriteLine("Checking Playwright browsers...");
        var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
        if (exitCode != 0)
        {
            Console.WriteLine("Warning: Failed to install Playwright browsers. They may already be installed.");
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            DeviceScaleFactor = 1
        });

        var page = await context.NewPageAsync();

        var markdown = new StringBuilder();
        markdown.AppendLine("# Minotaur UI Flow Documentation");
        markdown.AppendLine();
        markdown.AppendLine("This document provides a comprehensive overview of all implemented UI pages in the Minotaur web application.");
        markdown.AppendLine();
        markdown.AppendLine($"**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        markdown.AppendLine();
        markdown.AppendLine("## Table of Contents");
        markdown.AppendLine();

        for (int i = 0; i < Pages.Count; i++)
        {
            markdown.AppendLine($"{i + 1}. [{Pages[i].Title}](#{Pages[i].Title.Replace(" ", "-").ToLower()})");
        }

        markdown.AppendLine();
        markdown.AppendLine("---");
        markdown.AppendLine();

        int pageNum = 1;
        foreach (var pageInfo in Pages)
        {
            Console.WriteLine($"[{pageNum}/{Pages.Count}] Capturing: {pageInfo.Title} ({pageInfo.Url})");
            
            try
            {
                // Navigate to page
                var response = await page.GotoAsync($"{baseUrl}{pageInfo.Url}", new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30000
                });

                if (response?.Status != 200)
                {
                    Console.WriteLine($"  Warning: Got status {response?.Status}");
                }

                // Wait a bit for any dynamic content to load
                await page.WaitForTimeoutAsync(2000);

                // Take screenshot
                var screenshotFileName = $"{pageNum:D2}_{SanitizeFileName(pageInfo.Title)}.png";
                var screenshotPath = Path.Combine(outputDir, screenshotFileName);
                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = screenshotPath,
                    FullPage = true
                });

                Console.WriteLine($"  ✓ Screenshot saved: {screenshotPath}");

                // Add to markdown
                markdown.AppendLine($"## {pageNum}. {pageInfo.Title}");
                markdown.AppendLine();
                markdown.AppendLine($"**URL:** `{pageInfo.Url}`");
                markdown.AppendLine();
                markdown.AppendLine($"**Description:** {pageInfo.Description}");
                markdown.AppendLine();
                markdown.AppendLine($"![{pageInfo.Title}]({screenshotFileName})");
                markdown.AppendLine();
                markdown.AppendLine("---");
                markdown.AppendLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error: {ex.Message}");
                markdown.AppendLine($"## {pageNum}. {pageInfo.Title}");
                markdown.AppendLine();
                markdown.AppendLine($"**URL:** `{pageInfo.Url}`");
                markdown.AppendLine();
                markdown.AppendLine($"**Description:** {pageInfo.Description}");
                markdown.AppendLine();
                markdown.AppendLine($"*Error capturing screenshot: {ex.Message}*");
                markdown.AppendLine();
                markdown.AppendLine("---");
                markdown.AppendLine();
            }

            pageNum++;
        }

        // Save markdown documentation
        var markdownPath = Path.Combine(outputDir, "UI_FLOW.md");
        await File.WriteAllTextAsync(markdownPath, markdown.ToString());
        Console.WriteLine();
        Console.WriteLine($"✓ Documentation saved: {markdownPath}");
        Console.WriteLine();
        Console.WriteLine("=== UI Flow Documentation Complete ===");

        return 0;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries))
            .Replace(" ", "_");
    }

    private record PageInfo(string Url, string Title, string Description);
}
