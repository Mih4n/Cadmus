using Cadmus.App;
using Cadmus.Engine.Extensions;
using Microsoft.Extensions.DependencyInjection;
using TestGame;
using TestGame.Snake;

var builder = CadmusApplication.CreateBuilder();

builder.ConfigureWindow(window =>
{
    window.Title = "Cadmus Snake";
    window.Width = 2560;
    window.Height = 1440;
});

builder.Services.AddSingleton(new SnakeSettings());

builder.Services.AddEntity<SnakeEntity>();
builder.Services.AddEntity<FoodEntity>();

builder.Services.AddScene<SnakeScene>("Game");

builder.UseGame<SnakeGame>();

await using var app = builder.Build();
await app.RunAsync();
