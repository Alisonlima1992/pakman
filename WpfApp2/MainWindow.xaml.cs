using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MazePhysicsGame.Models;
using MazePhysicsGame.Services;

namespace MazePhysicsGame;

public partial class MainWindow : Window
{
    // ================= КОНСТАНТЫ =================
    private int CellSize = 32;
    private const int QuizCount = 4;
    private const int LabCount = 4;
    private const int TrapCount = 2;
    private const int EnemyCount = 5;
    private const double RespawnSeconds = 120.0;

    // Скорости
    private const double PlayerBaseSpeed = 6.5;   // клеток/сек
    private const double EnemyBaseSpeed = 3.5;   // клеток/сек

    // Хитбокс: половина стороны квадрата вокруг центра
    private const double PlayerHalf = 0.3;       // 70% клетки
    private const double EnemyHalf = 0.3;

    // Размер спрайта на экране (доля клетки)
    private const double PlayerSpriteScale = 2.00;   // игрок чуть больше клетки
    private const double EnemySpriteScale = 1.10;    // враги тоже
    private const double CoinSpriteScale = 1.00;     // монеты в клетку

    // ================= СОСТОЯНИЕ =================
    private Maze _maze = null!;
    private readonly List<Enemy> _enemies = new();

    // Позиции — ЦЕНТРЫ спрайтов, в клетках (1.5 = центр клетки с координатами (1,1))
    private double _playerX = 1.5;
    private double _playerY = 1.5;
    private FrameworkElement? _playerAnchor;

    private readonly List<FrameworkElement> _coinAnchors = new();
    private readonly List<FrameworkElement> _enemyAnchors = new();

    private double _speedMultiplier = 1.0;

    private bool _paused = false;
    private bool _gameOver = false;

    private int _score = 0;
    private int _coinsCollected = 0;
    private int _enemiesCaught = 0;

    private double _respawnTimer = RespawnSeconds;

    private readonly DispatcherTimer _timer;
    private DateTime _lastTick = DateTime.Now;
    private readonly Random _rng = new();

    // ================= ИКОНКИ =================
    private readonly ImageSource?[] _quizIcons = new ImageSource?[QuizCount];
    private readonly ImageSource?[] _labIcons = new ImageSource?[LabCount];
    private readonly ImageSource?[] _trapIcons = new ImageSource?[TrapCount];
    private readonly ImageSource?[] _enemyIcons = new ImageSource?[EnemyCount];
    private ImageSource? _playerIcon;

    // ================= АКТИВНЫЕ ЗАДАНИЯ =================
    private Coin? _activeCoin;
    private PhysicsLabTask? _activeLabTask;
    private TrapTask? _activeTrapTask;

    // ================= КОНСТРУКТОР =================
    public MainWindow()
    {
        InitializeComponent();

        WindowState = WindowState.Maximized;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;

        LoadIcons();
        StartNewGame();

        // ~120 FPS
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(8) };
        _timer.Tick += GameLoop;
        _timer.Start();

        SizeChanged += (_, _) => RecomputeCellSize();
        Loaded += (_, _) => RecomputeCellSize();
    }

    // ================= ЗАГРУЗКА ИКОНОК =================
    private void LoadIcons()
    {
        for (int i = 0; i < QuizCount; i++)
            _quizIcons[i] = TryLoad($"pack://application:,,,/Assets/quiz{i + 1}.png");
        for (int i = 0; i < LabCount; i++)
            _labIcons[i] = TryLoad($"pack://application:,,,/Assets/lab{i + 1}.png");
        for (int i = 0; i < TrapCount; i++)
            _trapIcons[i] = TryLoad($"pack://application:,,,/Assets/trap{i + 1}.png");
        for (int i = 0; i < EnemyCount; i++)
            _enemyIcons[i] = TryLoad($"pack://application:,,,/Assets/enemy{i + 1}.png");
        _playerIcon = TryLoad("pack://application:,,,/Assets/player.png");
    }

    private static ImageSource? TryLoad(string uri)
    {
        try { return new BitmapImage(new Uri(uri, UriKind.Absolute)); }
        catch { return null; }
    }

    private UIElement CreateSprite(ImageSource? icon, Brush fallback, double size)
    {
        if (icon != null)
            return new Image { Source = icon, Width = size, Height = size };
        return new System.Windows.Shapes.Ellipse { Width = size, Height = size, Fill = fallback };
    }

    // ================= ПЕРЕСЧЁТ РАЗМЕРА КЛЕТКИ =================
    private void RecomputeCellSize()
    {
        if (_maze == null) return;

        double availW = ActualWidth > 0 ? ActualWidth - 20 : SystemParameters.PrimaryScreenWidth - 20;
        double availH = ActualHeight > 0 ? ActualHeight - 20 : SystemParameters.PrimaryScreenHeight - 20;

        int byW = Math.Max(8, (int)(availW / _maze.Width));   // по ширине
        int byH = Math.Max(8, (int)(availH / _maze.Height));  // по высоте
        CellSize = Math.Max(8, Math.Min(byW, byH));

        GameCanvas.Width = _maze.Width * CellSize;
        GameCanvas.Height = _maze.Height * CellSize;

        RedrawAll();
    }

    private void RedrawAll()
    {
        if (_maze == null) return;
        DrawMaze();
        DrawCoins();
        DrawPlayer();
        DrawEnemies();
    }

    // ================= СТАРТ ИГРЫ =================
    private void StartNewGame()
    {
        _maze = new Maze();
        _maze.Coins.Clear();
        _enemies.Clear();
        _playerX = 1.5;
        _playerY = 1.5;
        _score = 0;
        _coinsCollected = 0;
        _enemiesCaught = 0;
        _speedMultiplier = 1.0;
        _gameOver = false;
        _paused = false;
        _respawnTimer = RespawnSeconds;
        _activeCoin = null;
        _activeLabTask = null;
        _activeTrapTask = null;

        QuizOverlay.Visibility = Visibility.Collapsed;
        LabOverlay.Visibility = Visibility.Collapsed;
        TrapOverlay.Visibility = Visibility.Collapsed;

        StatusText.Text = "Соберите монеты и поймайте 5 противников!";

        SpawnCoins();
        SpawnEnemies();

        RecomputeCellSize();
        RedrawAll();
        UpdateHud();
    }

    private void SpawnCoins()
    {
        var free = _maze.FreeCells();
        free.RemoveAll(c => c.X == 1 && c.Y == 1);

        for (int i = 0; i < QuizCount; i++)
            AddCoin(CellType.CoinQuiz, i, _quizIcons[i], free);

        for (int i = 0; i < LabCount; i++)
            AddCoin(CellType.CoinLab, i, _labIcons[i], free);

        for (int i = 0; i < TrapCount; i++)
            AddCoin(CellType.CoinTrap, i, _trapIcons[i], free);
    }

    private void AddCoin(CellType type, int variant, ImageSource? icon, List<(int X, int Y)> free)
    {
        if (free.Count == 0) return;
        int idx = _rng.Next(free.Count);
        var cell = free[idx];
        free.RemoveAt(idx);

        var coin = new Coin
        {
            X = cell.X,
            Y = cell.Y,
            Type = type,
            VariantIndex = variant,
            Icon = icon
        };
        _maze.Coins.Add(coin);
        _maze.Cells[cell.Y, cell.X] = type;
    }

    private void SpawnEnemies()
    {
        var free = _maze.FreeCells();
        free.RemoveAll(c => Math.Abs(c.X - 1) < 3 && Math.Abs(c.Y - 1) < 3);

        for (int i = 0; i < EnemyCount && free.Count > 0; i++)
        {
            int idx = _rng.Next(free.Count);
            var cell = free[idx];
            free.RemoveAt(idx);

            _enemies.Add(new Enemy
            {
                X = cell.X + 0.5,   // центр клетки
                Y = cell.Y + 0.5,
                Direction = _rng.Next(4),
                IconVariant = i,
                Icon = _enemyIcons[i],
                Caught = false
            });
        }
    }

    // ================= РИСОВАНИЕ =================
    private void DrawMaze()
    {
        GameCanvas.Children.Clear();
        _coinAnchors.Clear();
        _enemyAnchors.Clear();
        _playerAnchor = null;

        for (int y = 0; y < _maze.Height; y++)
            for (int x = 0; x < _maze.Width; x++)
            {
                if (_maze.Cells[y, x] == CellType.Wall)
                {
                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Fill = new SolidColorBrush(Color.FromRgb(40, 60, 130)),
                        Stroke = new SolidColorBrush(Color.FromRgb(90, 130, 220)),
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(rect, x * CellSize);
                    Canvas.SetTop(rect, y * CellSize);
                    GameCanvas.Children.Add(rect);
                }
                else
                {
                    var bg = new System.Windows.Shapes.Rectangle
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Fill = new SolidColorBrush(Color.FromRgb(20, 22, 30))
                    };
                    Canvas.SetLeft(bg, x * CellSize);
                    Canvas.SetTop(bg, y * CellSize);
                    GameCanvas.Children.Add(bg);
                }
            }
    }

    // Вспомогательный метод: центрирование спрайта по его центру (X, Y) в клетках
    private void PositionSpriteCentered(FrameworkElement fe, double centerX, double centerY, double spriteSize)
    {
        Canvas.SetLeft(fe, centerX * CellSize - spriteSize / 2.0);
        Canvas.SetTop(fe, centerY * CellSize - spriteSize / 2.0);
    }

    private void DrawCoins()
    {
        foreach (var a in _coinAnchors) GameCanvas.Children.Remove(a);
        _coinAnchors.Clear();

        double coinSize = CellSize * CoinSpriteScale;

        foreach (var coin in _maze.Coins)
        {
            if (coin.Collected) continue;

            var fallback = coin.Type switch
            {
                CellType.CoinQuiz => Brushes.Gold,
                CellType.CoinLab => Brushes.DeepSkyBlue,
                CellType.CoinTrap => Brushes.Crimson,
                _ => Brushes.Gold
            };

            var sprite = CreateSprite(coin.Icon, fallback, coinSize);
            if (sprite is FrameworkElement fe)
            {
                // Центр клетки: coin.X + 0.5, coin.Y + 0.5
                PositionSpriteCentered(fe, coin.X + 0.5, coin.Y + 0.5, coinSize);
                GameCanvas.Children.Add(fe);
                _coinAnchors.Add(fe);
            }
        }
    }

    private void DrawPlayer()
    {
        double spriteSize = CellSize * PlayerSpriteScale;

        if (_playerAnchor == null || !GameCanvas.Children.Contains(_playerAnchor))
        {
            var sprite = CreateSprite(_playerIcon, Brushes.LimeGreen, spriteSize);
            _playerAnchor = sprite as FrameworkElement;
            if (_playerAnchor != null) GameCanvas.Children.Add(_playerAnchor);
        }
        if (_playerAnchor != null)
        {
            // При изменении размера клетки спрайт мог получить старый размер — подгоняем
            _playerAnchor.Width = spriteSize;
            _playerAnchor.Height = spriteSize;

            PositionSpriteCentered(_playerAnchor, _playerX, _playerY, spriteSize);
            _playerAnchor.Visibility = Visibility.Visible;
        }
    }

    private void DrawEnemies()
    {
        double spriteSize = CellSize * EnemySpriteScale;

        bool needRecreate =
            _enemyAnchors.Count != _enemies.Count ||
            _enemyAnchors.Any(a => !GameCanvas.Children.Contains(a));

        if (needRecreate)
        {
            foreach (var a in _enemyAnchors) GameCanvas.Children.Remove(a);
            _enemyAnchors.Clear();

            foreach (var e in _enemies)
            {
                var sprite = CreateSprite(e.Icon, Brushes.OrangeRed, spriteSize);
                if (sprite is FrameworkElement fe)
                {
                    GameCanvas.Children.Add(fe);
                    _enemyAnchors.Add(fe);
                }
            }
        }

        for (int i = 0; i < _enemies.Count && i < _enemyAnchors.Count; i++)
        {
            var e = _enemies[i];
            var anchor = _enemyAnchors[i];
            anchor.Visibility = e.Caught ? Visibility.Collapsed : Visibility.Visible;
            anchor.Width = spriteSize;
            anchor.Height = spriteSize;
            PositionSpriteCentered(anchor, e.X, e.Y, spriteSize);
        }
    }

    // ================= ИГРОВОЙ ЦИКЛ =================
    private void GameLoop(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        double dt = (now - _lastTick).TotalSeconds;
        _lastTick = now;

        if (dt > 0.1) dt = 0.1;

        if (_paused || _gameOver) return;

        UpdatePlayer(dt);
        UpdateEnemies(dt);
        CheckPlayerCatchEnemy();

        if (_enemiesCaught < EnemyCount)
        {
            _respawnTimer -= dt;
            if (_respawnTimer <= 0)
                RespawnUncaughtEnemies();
        }

        DrawPlayer();
        DrawEnemies();
        UpdateHud();
    }

    // ================= ОТЗЫВЧИВОЕ ДВИЖЕНИЕ ИГРОКА =================
    private void UpdatePlayer(double dt)
    {
        double dx = 0, dy = 0;
        if (Keyboard.IsKeyDown(Key.Left) || Keyboard.IsKeyDown(Key.A)) dx -= 1;
        if (Keyboard.IsKeyDown(Key.Right) || Keyboard.IsKeyDown(Key.D)) dx += 1;
        if (Keyboard.IsKeyDown(Key.Up) || Keyboard.IsKeyDown(Key.W)) dy -= 1;
        if (Keyboard.IsKeyDown(Key.Down) || Keyboard.IsKeyDown(Key.S)) dy += 1;

        if (dx == 0 && dy == 0) return;

        // Нормализация диагонали
        double len = Math.Sqrt(dx * dx + dy * dy);
        dx /= len;
        dy /= len;

        double speed = PlayerBaseSpeed * _speedMultiplier;
        double totalStepX = dx * speed * dt;
        double totalStepY = dy * speed * dt;

        // Разбиваем на подшаги длиной не больше MaxSubStep
        // Максимум 0.4 клетки, чтобы не проскочить угол стены
        const double MaxSubStep = 0.4;

        double remaining = Math.Max(Math.Abs(totalStepX), Math.Abs(totalStepY));
        int steps = Math.Max(1, (int)Math.Ceiling(remaining / MaxSubStep));
        double sx = totalStepX / steps;
        double sy = totalStepY / steps;

        for (int i = 0; i < steps; i++)
        {
            // Сначала пробуем по X
            if (sx != 0)
            {
                double newX = _playerX + sx;
                if (CanFit(newX, _playerY, PlayerHalf))
                    _playerX = newX;
                else
                    sx = 0; // стена по X — стоп по X, продолжаем по Y
            }

            // Потом по Y
            if (sy != 0)
            {
                double newY = _playerY + sy;
                if (CanFit(_playerX, newY, PlayerHalf))
                    _playerY = newY;
                else
                    sy = 0; // стена по Y
            }

            // Если оба направления уперлись — выходим
            if (sx == 0 && sy == 0) break;
        }

        // Собираем монету
        int cellX = (int)Math.Floor(_playerX);
        int cellY = (int)Math.Floor(_playerY);
        TryCollectCoin(cellX, cellY);
    }

    // Может ли квадрат со стороной 2*half и центром (cx, cy) поместиться, не касаясь стен?
    private bool CanFit(double cx, double cy, double half)
    {
        double left = cx - half;
        double right = cx + half;
        double top = cy - half;
        double bottom = cy + half;

        int lx = (int)Math.Floor(left);
        int rx = (int)Math.Floor(right);
        int ty = (int)Math.Floor(top);
        int by = (int)Math.Floor(bottom);

        for (int x = lx; x <= rx; x++)
            for (int y = ty; y <= by; y++)
                if (_maze.IsWall(x, y)) return false;

        return true;
    }

    // ================= ПЛАВНОЕ ДВИЖЕНИЕ ВРАГОВ =================
    private void UpdateEnemies(double dt)
    {
        foreach (var enemy in _enemies)
        {
            if (enemy.Caught) continue;

            // Текущая клетка (целочисленная)
            int curCellX = (int)Math.Floor(enemy.X);
            int curCellY = (int)Math.Floor(enemy.Y);

            double centerX = curCellX + 0.5;
            double centerY = curCellY + 0.5;

            // Насколько враг близок к центру текущей клетки
            bool atCenter = Math.Abs(enemy.X - centerX) < 0.05 &&
                            Math.Abs(enemy.Y - centerY) < 0.05;

            if (atCenter)
            {
                // Снапим точно в центр
                enemy.X = centerX;
                enemy.Y = centerY;

                // Собираем свободные направления
                var freeDirs = new List<int>();
                for (int d = 0; d < 4; d++)
                {
                    int nx = curCellX + DirX(d);
                    int ny = curCellY + DirY(d);
                    if (!_maze.IsWall(nx, ny))
                        freeDirs.Add(d);
                }

                if (freeDirs.Count == 0) continue; // тупик

                // Выбираем направление, максимизирующее расстояние до игрока
                int bestDir = freeDirs[0];
                double bestScore = double.NegativeInfinity;

                foreach (int d in freeDirs)
                {
                    int nx = curCellX + DirX(d);
                    int ny = curCellY + DirY(d);

                    double score = Math.Abs(nx - _playerX) + Math.Abs(ny - _playerY);
                    score += _rng.NextDouble() * 1.5;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestDir = d;
                    }
                }

                enemy.Direction = bestDir;
            }

            // Целевая клетка — соседняя в направлении enemy.Direction
            int dir = enemy.Direction;
            int targetCellX = curCellX + DirX(dir);
            int targetCellY = curCellY + DirY(dir);

            // Проверка: если целевая клетка — стена, остаёмся в центре текущей
            if (_maze.IsWall(targetCellX, targetCellY))
            {
                enemy.X = centerX;
                enemy.Y = centerY;
                continue;
            }

            // Двигаем врага к центру целевой клетки
            double tx = targetCellX + 0.5;
            double ty = targetCellY + 0.5;

            double step = EnemyBaseSpeed * dt;
            double ddx = tx - enemy.X;
            double ddy = ty - enemy.Y;
            double dist = Math.Sqrt(ddx * ddx + ddy * ddy);

            if (dist <= step)
            {
                // Дошли до центра целевой клетки
                enemy.X = tx;
                enemy.Y = ty;
            }
            else
            {
                double nx = enemy.X + ddx / dist * step;
                double ny = enemy.Y + ddy / dist * step;

                // Дополнительная защита: проверяем, что новая позиция не попадает в стену
                int checkCellX = (int)Math.Floor(nx);
                int checkCellY = (int)Math.Floor(ny);

                if (_maze.IsWall(checkCellX, checkCellY))
                {
                    // Не двигаемся — стоим на месте и ждём следующего кадра
                    enemy.X = centerX;
                    enemy.Y = centerY;
                }
                else
                {
                    enemy.X = nx;
                    enemy.Y = ny;
                }
            }
        }
    }

    private static int DirX(int d) => d switch { 0 => 0, 1 => 1, 2 => 0, 3 => -1, _ => 0 };
    private static int DirY(int d) => d switch { 0 => -1, 1 => 0, 2 => 1, 3 => 0, _ => 0 };

    // ================= ЛОВЛЯ ВРАГА =================
    private void CheckPlayerCatchEnemy()
    {
        foreach (var enemy in _enemies)
        {
            if (enemy.Caught) continue;

            double ddx = enemy.X - _playerX;
            double ddy = enemy.Y - _playerY;
            double dist = Math.Sqrt(ddx * ddx + ddy * ddy);

            if (dist < 0.5)
            {
                enemy.Caught = true;
                _enemiesCaught++;
                _score += 200;
                StatusText.Text = $"Противник №{enemy.IconVariant + 1} пойман! ({_enemiesCaught}/{EnemyCount})";

                if (_enemiesCaught >= EnemyCount)
                {
                    _gameOver = true;
                    _paused = false;
                    StatusText.Text = "Все противники пойманы! Игра перезапускается...";

                    MessageBox.Show(
                        $"Все {EnemyCount} противников пойманы!\n" +
                        $"Счёт: {_score}\n\n" +
                        "Нажмите ОК, чтобы начать заново.",
                        "Победа!");

                    StartNewGame();
                }
                return;
            }
        }
    }

    // ================= РЕСПАВН ВРАГОВ =================
    private void RespawnUncaughtEnemies()
    {
        var free = _maze.FreeCells();
        free.RemoveAll(c => Math.Abs(c.X - (int)_playerX) < 4 && Math.Abs(c.Y - (int)_playerY) < 4);
        free.RemoveAll(c => _maze.Coins.Any(co => !co.Collected && co.X == c.X && co.Y == c.Y));

        foreach (var enemy in _enemies)
        {
            if (enemy.Caught) continue;
            if (free.Count == 0) break;

            int idx = _rng.Next(free.Count);
            var cell = free[idx];
            free.RemoveAt(idx);

            enemy.X = cell.X + 0.5;
            enemy.Y = cell.Y + 0.5;
            enemy.Direction = _rng.Next(4);
        }

        _respawnTimer = RespawnSeconds;
        StatusText.Text = "Время вышло! Непойманные противники телепортировались.";
    }

    // ================= СБОР МОНЕТ =================
    private void TryCollectCoin(int x, int y)
    {
        var coin = _maze.Coins.FirstOrDefault(c => !c.Collected && c.X == x && c.Y == y);
        if (coin == null) return;

        _paused = true;

        switch (coin.Type)
        {
            case CellType.CoinQuiz: ShowQuiz(coin); break;
            case CellType.CoinLab: ShowLab(coin); break;
            case CellType.CoinTrap: ShowTrap(coin); break;
        }
    }

    // ================= ВИКТОРИНА =================
    private void ShowQuiz(Coin coin)
    {
        _activeCoin = coin;
        var q = PhysicsQuiz.GetRandom();

        QuizTitle.Text = "Вопрос по физике";
        QuizQuestionText.Text = q.Text;
        QuizFeedback.Text = "";
        QuizOptions.Items.Clear();

        for (int i = 0; i < q.Options.Length; i++)
        {
            int idx = i;
            var btn = new Button
            {
                Content = $"{i + 1}. {q.Options[i]}",
                Margin = new Thickness(0, 4, 0, 4),
                Padding = new Thickness(10, 8, 10, 8),
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            btn.Click += (_, _) => AnswerQuiz(idx == q.CorrectIndex, coin);
            QuizOptions.Items.Add(btn);
        }

        QuizOverlay.Visibility = Visibility.Visible;
    }

    private void AnswerQuiz(bool correct, Coin coin)
    {
        QuizOverlay.Visibility = Visibility.Collapsed;

        if (correct)
        {
            _speedMultiplier = Math.Min(_speedMultiplier + 0.5, 3.0);
            _score += 300;
            StatusText.Text = "Ловушка обезврежена! +300 очков и ускорение.";
        }
        else
        {
            _score -= 30;
            StatusText.Text = "Неправильно. Монета потеряна.";
        }
        coin.Collected = true;
        _coinsCollected++;
        DrawCoins();
        UpdateHud();

        _activeCoin = null;
        _paused = false;
        CheckWin();
    }

    // ================= ЛАБОРАТОРИЯ =================
    private void ShowLab(Coin coin)
    {
        _activeCoin = coin;
        var task = PhysicsLabTask.Tasks[coin.VariantIndex];
        _activeLabTask = task;

        LabTitle.Text = $"Лабораторная №{coin.VariantIndex + 1}: " + task.Title;
        LabDescription.Text = task.Description;
        LabFeedback.Text = "";

        VoltageSlider.Value = 0;
        ResistanceSlider.Value = 0;
        FrequencySlider.Value = 0;
        CircuitClosed.IsChecked = false;

        VoltageValueText.Text = "0";
        ResistanceValueText.Text = "0";
        FrequencyValueText.Text = "0";

        LabOverlay.Visibility = Visibility.Visible;
    }

    private void VoltageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VoltageValueText != null)
            VoltageValueText.Text = ((int)Math.Round(e.NewValue)).ToString();
    }

    private void ResistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ResistanceValueText != null)
            ResistanceValueText.Text = ((int)Math.Round(e.NewValue)).ToString();
    }

    private void FrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FrequencyValueText != null)
            FrequencyValueText.Text = ((int)Math.Round(e.NewValue)).ToString();
    }

    private void TrapVoltageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TrapVoltageValueText != null)
            TrapVoltageValueText.Text = ((int)Math.Round(e.NewValue)).ToString();
    }

    private void LabCheck_Click(object sender, RoutedEventArgs e)
    {
        if (_activeCoin == null || _activeLabTask == null) return;

        var setup = new LabSetup
        {
            Voltage = VoltageSlider.Value,
            Resistance = ResistanceSlider.Value,
            Frequency = FrequencySlider.Value,
            CircuitClosed = CircuitClosed.IsChecked == true
        };

        bool correct = setup.IsValid(_activeLabTask);
        LabOverlay.Visibility = Visibility.Collapsed;

        if (correct)
        {
            _speedMultiplier = Math.Min(_speedMultiplier + 0.35, 3.0);
            _score += 150;
            StatusText.Text = "Установка настроена верно! Ускорение увеличено.";
        }
        else
        {
            _score -= 40;
            StatusText.Text = "Неверная настройка установки.";
        }
        _activeCoin.Collected = true;
        _coinsCollected++;
        DrawCoins();
        UpdateHud();

        _activeCoin = null;
        _activeLabTask = null;
        _paused = false;
        CheckWin();
    }

    private void LabCancel_Click(object sender, RoutedEventArgs e)
    {
        LabOverlay.Visibility = Visibility.Collapsed;
        if (_activeCoin != null)
        {
            _activeCoin.Collected = true;
            _coinsCollected++;
            DrawCoins();
            UpdateHud();
        }
        _activeCoin = null;
        _activeLabTask = null;
        _paused = false;
        CheckWin();
    }

    // ================= ЛОВУШКА =================
    private void ShowTrap(Coin coin)
    {
        _activeCoin = coin;
        var task = TrapTask.Tasks[coin.VariantIndex];
        _activeTrapTask = task;

        TrapTitle.Text = task.Title;
        TrapDescription.Text = task.Description;
        TrapFeedback.Text = "";
        TrapVoltageSlider.Value = 0;
        TrapVoltageValueText.Text = "0";

        TrapOverlay.Visibility = Visibility.Visible;
    }

    private void TrapCheck_Click(object sender, RoutedEventArgs e)
    {
        if (_activeCoin == null || _activeTrapTask == null) return;

        double v = TrapVoltageSlider.Value;
        TrapOverlay.Visibility = Visibility.Collapsed;

        bool correct = Math.Abs(v - _activeTrapTask.TargetVoltage) < 0.5;

        _activeCoin.Collected = true;
        _coinsCollected++;
        DrawCoins();
        UpdateHud();

        _activeCoin = null;
        _activeTrapTask = null;
        _paused = false;

        if (correct)
        {
            _score -= 100;
            StatusText.Text = "Ловушка сработала! Штраф -100 очков.";
        }
        else
        {
            _score -= 25;
            StatusText.Text = $"Неверное напряжение ({v:F0} В). Ловушка не сработала, монета потеряна.";
        }

        CheckWin();
    }

    private void TrapCancel_Click(object sender, RoutedEventArgs e)
    {
        TrapOverlay.Visibility = Visibility.Collapsed;
        if (_activeCoin != null)
        {
            _activeCoin.Collected = true;
            _coinsCollected++;
            DrawCoins();
            UpdateHud();
        }
        _activeCoin = null;
        _activeTrapTask = null;
        _paused = false;
    }

    // ================= ПОБЕДА / HUD =================
    private void CheckWin()
    {
        int totalCoins = QuizCount + LabCount + TrapCount;
        if (_coinsCollected >= totalCoins && _enemiesCaught >= EnemyCount && !_gameOver)
        {
            _gameOver = true;
            StatusText.Text = "Победа! Все монеты собраны и все противники пойманы.";
            MessageBox.Show($"Победа! Счёт: {_score}", "Игра пройдена");
        }
    }

    private void UpdateHud()
    {
        ScoreText.Text = $"Счёт: {_score}";
        CoinsText.Text = $"Монет собрано: {_coinsCollected}";
        SpeedText.Text = $"Ускорение: x{_speedMultiplier:F2}";
        CaughtText.Text = $"Поймано: {_enemiesCaught} / {EnemyCount}";
        TimerText.Text = _enemiesCaught >= EnemyCount
            ? "Все противники пойманы!"
            : $"До респавна: {Math.Max(0, (int)_respawnTimer)} c";
    }

    // ================= СЛУЖЕБНОЕ =================
    private void GameCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_maze == null) return;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Application.Current.Shutdown();
        }
        else if (e.Key == Key.Space)
        {
            if (!_gameOver)
            {
                _paused = !_paused;
                StatusText.Text = _paused ? "Пауза" : "Игра идёт";
            }
        }
        else if (e.Key == Key.R)
        {
            StartNewGame();
        }
    }

    private void Restart_Click(object sender, RoutedEventArgs e) => StartNewGame();
}