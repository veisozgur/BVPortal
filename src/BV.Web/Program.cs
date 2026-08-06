using BV.Web.Components;
using BV.Web.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient("BV.Api", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7001/");
});

builder.Services.AddScoped<ProtectedLocalStorage>();
builder.Services.AddScoped<AuthSession>();
builder.Services.AddScoped<AuthSessionStore>();
builder.Services.AddScoped<AuthApiClient>();
builder.Services.AddScoped<QuoteApiClient>();
builder.Services.AddScoped<ProfileApiClient>();
builder.Services.AddScoped<AdminApiClient>();
builder.Services.AddScoped<AdminCatalogApiClient>();
builder.Services.AddScoped<AdminNotificationApiClient>();
builder.Services.AddScoped<IntegrationStatusApiClient>();
builder.Services.AddScoped<ExcelQuoteImporter>();
builder.Services.AddScoped<CatalogApiClient>();
builder.Services.AddScoped<CatalogSelection>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
