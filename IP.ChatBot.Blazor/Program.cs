using IP.ChatBot.Blazor.Services;
using IP_ChatBot_Blazor.Components;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddScoped<IChatBotService, ChatBotService>();
builder.Services.AddSingleton<LoginUser>();

builder.Services.AddLogging(builder =>
{
    builder.AddConsole();
});

builder.Services.AddHttpClient("ChatbotService",sb =>
{
    sb.BaseAddress = new Uri("https://localhost:7154/ChatBot/");
});

builder.Services.AddMudServices();

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
