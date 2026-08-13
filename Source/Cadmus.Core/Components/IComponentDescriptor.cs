namespace Cadmus.Core.Components;

public interface IComponentDescriptor
{
    public long Get<T>();
    public long Get(Type componentType);
    public IComponentDescriptor Register<T>();
}
