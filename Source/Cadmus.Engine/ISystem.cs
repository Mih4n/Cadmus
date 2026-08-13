namespace Cadmus.Engine;

public interface ISystem
{
    int Order => 0;

    void Update(float deltaTime);
}
