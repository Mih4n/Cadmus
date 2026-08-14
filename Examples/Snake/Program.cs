using Cadmus.Core.Components;
using Cadmus.Core.Entities;
using Cadmus.Core.Storage;
using Cadmus.Engine;
using Microsoft.Extensions.DependencyInjection;
using Snake.Components;
using Snake.Systems;

var builder = CadmusApp.CreateBuilder();
var services = builder.Services;

services.AddSingleton<ISystem, InputSystem>();
services.AddSingleton<ISystem, OutputSystem>();

var app = builder.Build();

var scope = app.Services.CreateScope();
var world = scope.ServiceProvider.GetRequiredService<World>();
var components = scope.ServiceProvider.GetRequiredService<IComponentDescriptor>();
var systems = scope.ServiceProvider.GetServices<ISystem>();

components
    .Register<Position>();

world
    .Spawn()
    .WithName("Entity")
    .AddComponent(new Position(0, 0))
    .Build();

foreach (var system in systems)
{
    system.OnStart();
}

var stopwatch = new System.Diagnostics.Stopwatch();
stopwatch.Start();
var elapsed = 0.0;
while (true)
{
    Console.WriteLine($"Elapsed: {elapsed}");
    elapsed = stopwatch.Elapsed.Microseconds;
    app.Update((float)elapsed);
    Task.Delay(1000).Wait();
}
