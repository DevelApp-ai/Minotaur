using Minotaur.UI.Blazor.Components;
using Minotaur.Core;
using Minotaur.Editor;
using Minotaur.Plugins;
using Minotaur.GrammarGeneration;
using Minotaur.Parser;
using Minotaur.UI.Blazor.Api.Services;
using Minotaur.UI.Blazor.Api.Hubs;
using Minotaur.UI.Blazor.Api.GraphQL;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register Minotaur services
builder.Services.AddSingleton<LanguagePluginManager>();
builder.Services.AddScoped<GraphEditor>();
builder.Services.AddScoped<GrammarGenerator>();

// Register API services
builder.Services.AddSingleton<CognitiveGraphService>();
builder.Services.AddScoped<Minotaur.UI.Blazor.Services.CognitiveGraphApiService>();
builder.Services.AddScoped<Minotaur.UI.Blazor.Services.MarketplaceService>();
builder.Services.AddHttpClient();

// Add SignalR
builder.Services.AddSignalR();

// Add GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

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

// Map GraphQL endpoint
app.MapGraphQL("/graphql");

// Map SignalR hubs
app.MapHub<CognitiveGraphHub>("/cognitive-graph-hub");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
