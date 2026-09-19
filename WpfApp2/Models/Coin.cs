using System.Windows.Media;

namespace MazePhysicsGame.Models;

public class Coin
{
    public int X { get; set; }
    public int Y { get; set; }
    public CellType Type { get; set; }
    public int VariantIndex { get; set; }
    public ImageSource? Icon { get; set; }
    public bool Collected { get; set; }
}