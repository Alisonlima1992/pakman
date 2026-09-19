using System.Windows.Media;

namespace MazePhysicsGame.Models;

public class Enemy
{
    public double X { get; set; }             // ← было int
    public double Y { get; set; }             // ← было int
    public int Direction { get; set; }
    public int IconVariant { get; set; }
    public ImageSource? Icon { get; set; }
    public bool Caught { get; set; }
}