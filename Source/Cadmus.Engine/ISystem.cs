namespace Cadmus.Engine;

public interface ISystem
{
    int Order => 0;

    void OnStop();
    void OnStart();
    void Update(float deltaTime);
}
