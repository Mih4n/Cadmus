using System.Numerics;
using System.Runtime.InteropServices;
using Cadmus.Core.Storage;
using Cadmus.Engine;
using Snake.Components;

namespace Snake.Systems;

public class InputSystem(IEntityStorage storage) : ISystem
{
    private char lastKey = '\0';
    private Task backgroundTask = Task.CompletedTask;

    public void OnStart()
    {
        Console.WriteLine("InputSystem started");
        backgroundTask = Task.Run(() =>
        {
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    lastKey = Console.ReadKey(true).KeyChar;
                }
            }
        });
    }

    public void OnStop()
    {
    }

    private static readonly HashSet<char> keyMap = [
        'w', 'a', 's', 'd'
    ];

    private char keyBuffer = '\0';

    public void Update(float deltaTime)
    {
        if (lastKey == '\0') return;
        if (!keyMap.Contains(lastKey)) return;

        keyBuffer = lastKey;
        storage.Query<Position>(ChangePosition);
    }

    private unsafe void ChangePosition(Span<Position> positions)
    {
        if (keyBuffer == '\0') return;

        var changeEven = false;
        var vectorSize = Vector<int>.Count;
        var scalarDelta = 0;

        Span<int> template = stackalloc int[vectorSize];
        Span<int> flatData = MemoryMarshal.Cast<Position, int>(positions);

        if (keyBuffer is 'w' or 's')
        {
            changeEven = false;
            var delta = keyBuffer == 'w' ? 1 : -1;
            for (int i = 1; i < vectorSize; i += 2)
                template[i] = delta;
            scalarDelta = delta;
        }
        else if (keyBuffer is 'a' or 'd')
        {
            changeEven = true;
            var delta = keyBuffer == 'a' ? -1 : 1;
            for (int i = 0; i < vectorSize; i += 2)
                template[i] = delta;
            scalarDelta = delta;
        }

        var addVector = new Vector<int>(template);
        for (int i = 0; i <= flatData.Length - vectorSize; i += vectorSize)
        {
            var vec = new Vector<int>(flatData[i..vectorSize]);

            vec += addVector;
            vec.CopyTo(flatData[i..vectorSize]);
        }

        if (changeEven)
            for (int j = 0; j < flatData.Length; j += 2)
                flatData[j] += scalarDelta;
        else
            for (int j = 1; j < flatData.Length; j += 2)
                flatData[j] += scalarDelta;
    }
}
