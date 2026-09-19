using System;
using MazePhysicsGame.Services;

namespace MazePhysicsGame.Models;

// Параметры лабораторной установки, которые игрок настраивает
public class LabSetup
{
    public double Voltage { get; set; }        // В
    public double Resistance { get; set; }     // Ом
    public double Frequency { get; set; }      // Гц
    public bool CircuitClosed { get; set; }

    // Проверка правильности для конкретной задачи
    public bool IsValid(PhysicsLabTask task)
    {
        return Math.Abs(Voltage - task.TargetVoltage) < 0.5
            && Math.Abs(Resistance - task.TargetResistance) < 0.5
            && Math.Abs(Frequency - task.TargetFrequency) < 0.5
            && CircuitClosed == task.RequireClosedCircuit;
    }
}