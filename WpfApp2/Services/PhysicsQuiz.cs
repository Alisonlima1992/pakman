namespace MazePhysicsGame.Services;

public class QuizQuestion
{
    public string Text { get; init; } = "";
    public string[] Options { get; init; } = System.Array.Empty<string>();
    public int CorrectIndex { get; init; }
}

public static class PhysicsQuiz
{
    // 4 вопроса — по индексу совпадают с variant монеты
    public static readonly QuizQuestion[] Questions = new QuizQuestion[]
    {
        new QuizQuestion
        {
            Text = "Чему равен импульс тела массой 2 кг, движущегося со скоростью 3 м/с?",
            Options = new[] { "6 кг·м/с", "1.5 кг·м/с", "5 кг·м/с", "12 кг·м/с" },
            CorrectIndex = 0
        },
        new QuizQuestion
        {
            Text = "Формула периода математического маятника:",
            Options = new[] { "T = 2π√(L/g)", "T = 2π√(g/L)", "T = √(L·g)", "T = L/g" },
            CorrectIndex = 0
        },
        new QuizQuestion
        {
            Text = "Чему равен заряд электрона по модулю?",
            Options = new[] { "1.6·10⁻¹⁹ Кл", "9.1·10⁻³¹ Кл", "6.6·10⁻³⁴ Кл", "3·10⁸ Кл" },
            CorrectIndex = 0
        },
        new QuizQuestion
        {
            Text = "Что произойдёт с периодом колебаний пружинного маятника при увеличении массы в 4 раза?",
            Options = new[] { "Увеличится в 2 раза", "Уменьшится в 2 раза", "Не изменится", "Увеличится в 4 раза" },
            CorrectIndex = 0
        },
    };
}