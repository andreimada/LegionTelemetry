using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using LegionTelemetry.Services;
using Application = System.Windows.Application;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace LegionTelemetry;

internal sealed class OverlayController : IDisposable
{
    private readonly Window window;
    private readonly UIElement dragHandle;
    private readonly TextBlock cpuDisplay;
    private readonly TextBlock gpuDisplay;
    private readonly TextBlock ramDisplay;
    private readonly HardwareMonitor monitor = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Forms.ContextMenuStrip menu = new();
    private readonly Forms.NotifyIcon tray = new()
    {
        Icon = Drawing.SystemIcons.Application,
        Text = "Legion Telemetry"
    };
    private bool clickThrough;
    private bool disposed;

    public OverlayController(Window window, UIElement dragHandle,
        TextBlock cpuDisplay, TextBlock gpuDisplay, TextBlock ramDisplay)
    {
        this.window = window;
        this.dragHandle = dragHandle;
        this.cpuDisplay = cpuDisplay;
        this.gpuDisplay = gpuDisplay;
        this.ramDisplay = ramDisplay;

        menu.Items.Add("Show overlay", null, (_, _) => window.Dispatcher.Invoke(ShowOverlay));
        menu.Items.Add("Toggle click-through", null, (_, _) => window.Dispatcher.Invoke(ToggleClickThrough));
        menu.Items.Add("Opacity 60%", null, (_, _) => window.Dispatcher.Invoke(() => window.Opacity = 0.6));
        menu.Items.Add("Opacity 100%", null, (_, _) => window.Dispatcher.Invoke(() => window.Opacity = 1.0));
        menu.Items.Add("Exit", null, (_, _) => window.Dispatcher.Invoke(() => Application.Current.Shutdown()));
        tray.ContextMenuStrip = menu;
        tray.DoubleClick += ShowFromTray;
        tray.Visible = true;

        window.Loaded += PositionInCorner;
        window.Closed += OnWindowClosed;
        dragHandle.MouseLeftButtonDown += DragPanel;
        timer.Tick += RefreshSensors;

        try { monitor.Open(); }
        catch (Exception ex)
        {
            cpuDisplay.Text = "Sensor error";
            gpuDisplay.Text = ex.GetType().Name;
            return;
        }

        RefreshSensors(null, EventArgs.Empty);
        timer.Start();
    }

    private void RefreshSensors(object? sender, EventArgs e)
    {
        try
        {
            var readings = monitor.Read();
            var cpuTemperature = !readings.CpuTemperature.HasValue && !readings.IsPawnIoInstalled
                ? "Install PawnIO" : FormatTemperature(readings.CpuTemperature);
            cpuDisplay.Text = $"CPU {FormatUsage(readings.CpuUsage)} / {cpuTemperature}";
            gpuDisplay.Text = $"GPU {FormatUsage(readings.GpuUsage)} / {FormatTemperature(readings.GpuTemperature)}";
            ramDisplay.Text = readings.UsedRam.HasValue ? $"RAM {readings.UsedRam.Value:0.0} GB" : "RAM N/A";
            cpuDisplay.ToolTip = readings.CpuTemperature.HasValue ? null
                : !readings.IsPawnIoInstalled
                    ? "Install the signed PawnIO driver from https://pawnio.eu, then restart Legion Telemetry."
                    : "CPU temperature is unavailable. Check the PawnIO installation and run Legion Telemetry as administrator.";
        }
        catch { cpuDisplay.Text = "Sensor read failed"; }
    }

    private static string FormatUsage(float? value) => value.HasValue ? $"{value.Value:0}%" : "N/A";
    private static string FormatTemperature(float? value) => value.HasValue ? $"{value.Value:0}°C" : "N/A";

    private void PositionInCorner(object sender, RoutedEventArgs e)
    {
        var area = SystemParameters.WorkArea;
        window.Left = area.Right - window.ActualWidth - 12;
        window.Top = area.Top + 12;
    }

    private void DragPanel(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) window.DragMove();
    }

    private void ShowOverlay()
    {
        window.Show();
        window.Activate();
    }

    private void ShowFromTray(object? sender, EventArgs e) => window.Dispatcher.Invoke(ShowOverlay);

    private void ToggleClickThrough()
    {
        const int extendedStyle = -20;
        const long transparent = 0x20;
        const long layered = 0x80000;
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;

        clickThrough = !clickThrough;
        var style = GetWindowLongPtr(handle, extendedStyle).ToInt64();
        style = clickThrough ? style | transparent | layered : style & ~transparent;
        SetWindowLongPtr(handle, extendedStyle, new IntPtr(style));
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr newStyle);

    private void OnWindowClosed(object? sender, EventArgs e) => Dispose();

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        timer.Stop();
        timer.Tick -= RefreshSensors;
        window.Loaded -= PositionInCorner;
        window.Closed -= OnWindowClosed;
        dragHandle.MouseLeftButtonDown -= DragPanel;
        tray.DoubleClick -= ShowFromTray;
        tray.Visible = false;
        tray.Dispose();
        menu.Dispose();
        monitor.Dispose();
    }
}
