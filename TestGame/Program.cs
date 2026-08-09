using Cadmus.App;
using Cadmus.Engine.Extensions;
using Microsoft.Extensions.DependencyInjection;
using TestGame;
using TestGame.Snake;
using TestGame.Snake.Systems;

var builder = CadmusApplication.CreateBuilder();

builder.ConfigureWindow(window =>
{
    window.Title = "Cadmus Snake";
    window.Width = 2560;
    window.Height = 1440;
});

builder.Services.AddSingleton(new SnakeSettings());

builder.Services.AddScene<SnakeScene>("Game");

// Gameplay is these systems and nothing else; they find their entities by component.
builder.Services.AddSystem<PlayerInputSystem>();
builder.Services.AddSystem<SnakeMovementSystem>();
builder.Services.AddSystem<GameFlowSystem>();
builder.Services.AddSystem<FoodSystem>();
builder.Services.AddSystem<BoardPresentationSystem>();
builder.Services.AddSystem<WindowTitleSystem>();

builder.UseGame<SnakeGame>();

await using var app = builder.Build();
await app.RunAsync();
