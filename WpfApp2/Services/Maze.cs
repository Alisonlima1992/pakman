using System.Collections.Generic;
using MazePhysicsGame.Models;

namespace MazePhysicsGame.Services;

public class Maze
{
    public int Width { get; }
    public int Height { get; }
    public CellType[,] Cells { get; }
    public List<Coin> Coins { get; } = new();

    private readonly int[,] _raw = new int[,]
    {
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
        {1,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,1},
        {1,0,1,0,1,0,1,1,1,0,1,0,1,1,1,1,0,1,0,1,1},
        {1,0,1,0,0,0,1,0,0,0,1,0,1,0,0,1,0,0,0,0,1},
        {1,0,1,1,1,0,1,0,1,1,1,0,1,0,1,1,1,1,1,0,1},
        {1,0,0,0,1,0,0,0,1,0,0,0,1,0,0,0,0,0,1,0,1},
        {1,1,1,0,1,1,1,0,1,0,1,1,1,1,1,0,1,0,1,0,1},
        {1,0,0,0,0,0,1,0,0,0,1,0,0,0,0,0,1,0,0,0,1},
        {1,0,1,1,1,0,1,1,1,0,1,0,1,1,1,1,1,1,1,0,1},
        {1,0,1,0,0,0,0,0,1,0,0,0,1,0,0,0,0,0,0,0,1},
        {1,0,1,0,1,1,1,0,1,1,1,1,1,0,1,1,1,1,1,0,1},
        {1,0,0,0,1,0,0,0,0,0,0,0,0,0,1,0,0,0,0,0,1},
        {1,0,1,1,1,0,1,1,1,1,1,0,1,1,1,0,1,1,1,0,1},
        {1,0,1,0,0,0,1,0,0,0,1,0,0,0,0,0,1,0,0,0,1},
        {1,0,1,0,1,1,1,0,1,0,1,1,1,1,1,0,1,0,1,1,1},
        {1,0,0,0,1,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,1},
        {1,0,1,1,1,0,1,0,1,1,1,1,1,0,1,1,1,1,1,0,1},
        {1,0,1,0,0,0,1,0,0,0,0,0,1,0,0,0,1,0,0,0,1},
        {1,0,1,0,1,1,1,1,1,0,1,0,1,1,1,0,1,0,1,0,1},
        {1,0,0,0,0,0,0,0,1,0,1,0,0,0,1,0,0,0,1,0,1},
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
    };

    public Maze()
    {
        Height = _raw.GetLength(0);
        Width = _raw.GetLength(1);
        Cells = new CellType[Height, Width];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                Cells[y, x] = _raw[y, x] == 1 ? CellType.Wall : CellType.Floor;
    }

    public bool IsWall(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return true;
        return Cells[y, x] == CellType.Wall;
    }

    public List<(int X, int Y)> FreeCells()
    {
        var list = new List<(int, int)>();
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                if (Cells[y, x] == CellType.Floor) list.Add((x, y));
        return list;
    }
}