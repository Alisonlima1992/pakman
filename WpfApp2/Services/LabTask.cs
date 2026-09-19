namespace MazePhysicsGame.Services;

public class PhysicsLabTask
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public double TargetVoltage { get; init; }
    public double TargetResistance { get; init; }
    public double TargetFrequency { get; init; }
    public bool RequireClosedCircuit { get; init; }

    // 4 лабораторные — по индексу совпадают с variant монеты
    public static readonly PhysicsLabTask[] Tasks = new PhysicsLabTask[]
    {
        new PhysicsLabTask
        {
            Title = "Резонанс в LC-контуре",
            Description = "Настройте: U = 12 В, R = 10 Ом, f = 50 Гц, цепь замкнута.",
            TargetVoltage = 12, TargetResistance = 10, TargetFrequency = 50, RequireClosedCircuit = true
        },
        new PhysicsLabTask
        {
            Title = "Закон Ома для участка цепи",
            Description = "Настройте: U = 6 В, R = 3 Ом, f = 60 Гц, цепь замкнута.",
            TargetVoltage = 6, TargetResistance = 3, TargetFrequency = 60, RequireClosedCircuit = true
        },
        new PhysicsLabTask
        {
            Title = "Колебательный контур",
            Description = "Настройте: U = 24 В, R = 20 Ом, f = 100 Гц, цепь замкнута.",
            TargetVoltage = 24, TargetResistance = 20, TargetFrequency = 100, RequireClosedCircuit = true
        },
        new PhysicsLabTask
        {
            Title = "RC-цепочка",
            Description = "Настройте: U = 9 В, R = 15 Ом, f = 30 Гц, цепь разомкнута.",
            TargetVoltage = 9, TargetResistance = 15, TargetFrequency = 30, RequireClosedCircuit = false
        },
    };
}

// Задача-ловушка: игрок должен сам посчитать напряжение
public class TrapTask
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public double TargetVoltage { get; init; }

    // 2 ловушки — по индексу совпадают с variant монеты
    public static readonly TrapTask[] Tasks = new TrapTask[]
    {
        new TrapTask
        {
            Title = "ЛОВУШКА: Закон Ома",
            Description = "R = 8 Ом, I = 3 А. Вычислите напряжение по формуле U = I·R\n" +
                          "и выставьте его на слайдере.\n\n" +
                          "ВНИМАНИЕ: правильный ответ активирует ловушку!",
            TargetVoltage = 24
        },
        new TrapTask
        {
            Title = "ЛОВУШКА: Мощность",
            Description = "P = 60 Вт, I = 5 А. Вычислите напряжение по формуле U = P / I\n" +
                          "и выставьте его на слайдере.\n\n" +
                          "ВНИМАНИЕ: правильный ответ активирует ловушку!",
            TargetVoltage = 12
        },
    };
}