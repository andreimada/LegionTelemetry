using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.PawnIo;

namespace LegionTelemetry.Services;

internal sealed record SensorReadings(
    float? CpuUsage, float? CpuTemperature,
    float? GpuUsage, float? GpuTemperature,
    float? UsedRam, bool IsPawnIoInstalled);

internal sealed class HardwareMonitor : IDisposable
{
    private readonly Computer computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true
    };

    public void Open() => computer.Open();

    public SensorReadings Read()
    {
        foreach (var hardware in computer.Hardware) UpdateRecursive(hardware);

        var cpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        // Prefer the dedicated NVIDIA GPU over integrated graphics.
        var gpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia)
            ?? computer.Hardware.FirstOrDefault(h => h.HardwareType is HardwareType.GpuAmd or HardwareType.GpuIntel);
        var memory = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);

        return new SensorReadings(
            cpu?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Total", StringComparison.OrdinalIgnoreCase))?.Value,
            cpu == null ? null : ChooseTemperature(cpu, true)?.Value,
            gpu?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && (s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) || s.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase)))?.Value,
            gpu == null ? null : ChooseTemperature(gpu, false)?.Value,
            memory?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name.Equals("Memory Used", StringComparison.OrdinalIgnoreCase))?.Value,
            PawnIo.IsInstalled);
    }

    private static void UpdateRecursive(IHardware hardware)
    {
        hardware.Update();
        foreach (var sub in hardware.SubHardware) UpdateRecursive(sub);
    }

    private static ISensor? ChooseTemperature(IHardware hardware, bool cpu)
    {
        var temperatures = hardware.Sensors
            .Where(s => s.SensorType == SensorType.Temperature && s.Value.HasValue).ToList();
        string[] preferred = cpu
            ? ["CPU Package", "Core (Tctl/Tdie)", "CPU Core", "Core Average"]
            : ["GPU Core", "GPU Temperature", "GPU Hot Spot"];

        foreach (var name in preferred)
        {
            var match = temperatures.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }

        return temperatures.FirstOrDefault(s => !s.Name.Contains("Hot Spot", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose() => computer.Close();
}
