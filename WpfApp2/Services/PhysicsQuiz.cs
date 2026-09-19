using System;
using System.Collections.Generic;

namespace MazePhysicsGame.Services;

public class QuizQuestion
{
    public string Text { get; init; } = "";
    public string[] Options { get; init; } = Array.Empty<string>();
    public int CorrectIndex { get; init; }
}

public static class PhysicsQuiz
{
    private static readonly Random _rng = new();

    // Каждый шаблон умеет сгенерировать вопрос со случайными числами.
    // Возвращает текст, варианты ответов и правильный индекс.
    private static readonly Func<QuizQuestion>[] Generators = new Func<QuizQuestion>[]
    {
        // 1. Импульс тела: p = m·v
        () =>
        {
            int m = _rng.Next(2, 15);         // 2..14 кг
            int v = _rng.Next(2, 20);         // 2..19 м/с
            int correct = m * v;

            return BuildQuestion(
                $"Чему равен импульс тела массой {m} кг, движущегося со скоростью {v} м/с?",
                correct, "кг·м/с");
        },

        // 2. Кинетическая энергия: E = m·v²/2
        () =>
        {
            int m = _rng.Next(1, 10) * 2;     // чётная масса 2..18
            int v = _rng.Next(2, 10);         // 2..9 м/с
            int correct = m * v * v / 2;

            return BuildQuestion(
                $"Чему равна кинетическая энергия тела массой {m} кг, движущегося со скоростью {v} м/с?",
                correct, "Дж");
        },

        // 3. Закон Ома: I = U/R
        () =>
        {
            int u = _rng.Next(2, 30);         // 2..29 В
            int r = _rng.Next(1, 10);         // 1..9 Ом
            int correct = u / r;              // целое, т.к. подбираем u кратно r ниже
            // Подбираем u кратно r, чтобы результат был целым
            u = correct * r;

            return BuildQuestion(
                $"Чему равна сила тока в цепи с напряжением {u} В и сопротивлением {r} Ом?",
                correct, "А");
        },

        // 4. Мощность: P = U·I
        () =>
        {
            int u = _rng.Next(5, 30);         // 5..29 В
            int i = _rng.Next(1, 10);         // 1..9 А
            int correct = u * i;

            return BuildQuestion(
                $"Чему равна мощность тока при напряжении {u} В и силе тока {i} А?",
                correct, "Вт");
        },

        // 5. Работа: A = F·s
        () =>
        {
            int f = _rng.Next(5, 50);         // 5..49 Н
            int s = _rng.Next(2, 15);         // 2..14 м
            int correct = f * s;

            return BuildQuestion(
                $"Какую работу совершает сила {f} Н при перемещении тела на {s} м?",
                correct, "Дж");
        },

        // 6. Механическая мощность: P = A/t
        () =>
        {
            int t = _rng.Next(2, 15);         // 2..14 с
            int p = _rng.Next(5, 50);         // 5..49 Вт
            int a = p * t;

            return BuildQuestion(
                $"За {t} с совершена работа {a} Дж. Чему равна средняя мощность?",
                p, "Вт");
        },

        // 7. Ускорение: a = Δv / t
        () =>
        {
            int dv = _rng.Next(2, 30);        // 2..29 м/с
            int t = _rng.Next(2, 10);         // 2..9 с
            int correct = dv / t;
            // Подбираем dv кратно t, чтобы результат целый
            dv = correct * t;

            return BuildQuestion(
                $"Скорость тела изменилась на {dv} м/с за {t} с. Чему равно ускорение?",
                correct, "м/с²");
        },

        // 8. Период: T = 1/ν (частота целая, T = 1000/ν в мс)
        () =>
        {
            int v = _rng.Next(2, 20);         // 2..19 Гц
            int correct = 1000 / v;           // период в мс, целое если v делит 1000
            // подберём v из делителей 1000: 2,4,5,8,10,20 — оставим v из этого списка
            int[] divisors = { 2, 4, 5, 8, 10, 20 };
            v = divisors[_rng.Next(divisors.Length)];
            correct = 1000 / v;

            return BuildQuestion(
                $"Чему равен период колебаний, если частота равна {v} Гц? (ответ в мс)",
                correct, "мс");
        },

        // 9. Плотность: ρ = m / V
        () =>
        {
            int vol = _rng.Next(1, 10);       // 1..9 м³
            int rho = _rng.Next(500, 3000);   // 500..2999 кг/м³
            int mass = rho * vol;

            return BuildQuestion(
                $"Тело массой {mass} кг занимает объём {vol} м³. Чему равна плотность?",
                rho, "кг/м³");
        },

        // 10. Давление: p = F / S
        () =>
        {
            int s = _rng.Next(1, 10);         // 1..9 м²
            int p = _rng.Next(50, 500);       // 50..499 Па
            int f = p * s;

            return BuildQuestion(
                $"Сила {f} Н действует на площадь {s} м². Чему равно давление?",
                p, "Па");
        },

        // 11. Сила тяжести: F = m·g, g≈10
        () =>
        {
            int m = _rng.Next(1, 30);         // 1..29 кг
            int correct = m * 10;

            return BuildQuestion(
                $"Чему равна сила тяжести, действующая на тело массой {m} кг? (g = 10 м/с²)",
                correct, "Н");
        },

        // 12. Путь: s = v·t
        () =>
        {
            int v = _rng.Next(2, 30);         // 2..29 м/с
            int t = _rng.Next(2, 20);         // 2..19 с
            int correct = v * t;

            return BuildQuestion(
                $"Тело движется со скоростью {v} м/с в течение {t} с. Какой путь оно пройдёт?",
                correct, "м");
        },
    };

    // Вспомогательный метод: строит вопрос и 4 варианта ответа (1 правильный + 3 случайных неправильных)
    private static QuizQuestion BuildQuestion(string text, int correctValue, string unit)
    {
        var options = new List<int> { correctValue };
        int attempts = 0;

        // Генерируем 3 неправильных ответа, не совпадающих с правильным и между собой
        while (options.Count < 4 && attempts++ < 100)
        {
            // Неправильный ответ — на 5..50% больше или меньше правильного
            int delta = _rng.Next(1, Math.Max(2, correctValue / 2 + 1));
            int wrong = _rng.Next(2) == 0
                ? correctValue - delta
                : correctValue + delta;

            if (wrong <= 0) wrong = correctValue + delta;
            if (wrong == correctValue) continue;
            if (options.Contains(wrong)) continue;

            options.Add(wrong);
        }

        // Перемешиваем варианты
        for (int i = options.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (options[i], options[j]) = (options[j], options[i]);
        }

        int correctIndex = options.IndexOf(correctValue);

        return new QuizQuestion
        {
            Text = text,
            Options = new[]
            {
                $"{options[0]} {unit}",
                $"{options[1]} {unit}",
                $"{options[2]} {unit}",
                $"{options[3]} {unit}",
            },
            CorrectIndex = correctIndex
        };
    }

    // Публичный метод: возвращает случайный вопрос
    public static QuizQuestion GetRandom()
    {
        int idx = _rng.Next(Generators.Length);
        return Generators[idx]();
    }
}