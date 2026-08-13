namespace Cadmus.Core.Storage;

public interface IComponentArray
{
    int Count { get; }
    void SwapRemoveAt(int row);
    void CopyRowTo(int row, IComponentArray destination);
}
