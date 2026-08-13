using Microsoft.Extensions.DependencyInjection;

namespace Cadmus.Engine;

public sealed class CadmusBuilder
{
    public IServiceCollection Services { get; } = new ServiceCollection();

    internal CadmusBuilder()
    {
        Services.AddCadmusEngine();
    }

    public CadmusApp Build()
    {
        var provider = Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
        });

        return new CadmusApp(provider);
    }
}
