using Cadmus.App;
using Cadmus.Engine.Extensions;

var builder = CadmusApplication.CreateBuilder();

builder.ConfigureWindow(window =>
{
    window.Title = "Cadmus Snake";
    window.Width = 2560;
    window.Height = 1440;
});

builder.Services.AddScene<>();