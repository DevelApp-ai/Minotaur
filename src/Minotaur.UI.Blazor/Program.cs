using Minotaur.UI.Blazor.Components;
using Minotaur.Core;
using Minotaur.Plugins;
using Minotaur.GrammarGeneration;
using Minotaur.Parser;

namespace Minotaur.UI.Blazor;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Register Minotaur services
        builder.Services.AddSingleton<LanguagePluginManager>();
        builder.Services.AddScoped<GrammarGenerator>();
        builder.Services.AddScoped<Minotaur.UI.Blazor.Services.AuthenticationService>();
        builder.Services.AddScoped<Minotaur.UI.Blazor.Services.MarketplaceService>();
        builder.Services.AddScoped<Minotaur.UI.Blazor.Services.TemplateService>();
        builder.Services.AddScoped<Minotaur.UI.Blazor.Services.GrammarSyntaxHighlightingService>();
        builder.Services.AddScoped<Minotaur.UI.Blazor.Services.GrammarCodeCompletionService>();
        builder.Services.AddScoped<Minotaur.UI.Blazor.Services.ProjectLoaderService>();
        builder.Services.AddHttpClient();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
