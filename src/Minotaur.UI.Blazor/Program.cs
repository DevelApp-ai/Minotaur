using Minotaur.UI.Blazor.Components;
using Minotaur.Core;
using Minotaur.Editor;
using Minotaur.Plugins;
using Minotaur.GrammarGeneration;
using Minotaur.Parser;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register Minotaur services
builder.Services.AddSingleton<LanguagePluginManager>();
builder.Services.AddScoped<GraphEditor>();
builder.Services.AddScoped<GrammarGenerator>();

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
