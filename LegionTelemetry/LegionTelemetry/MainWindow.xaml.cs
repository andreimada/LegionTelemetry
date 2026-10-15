using System.Windows;

namespace LegionTelemetry;

public partial class MainWindow : Window
{
    private readonly OverlayController controller;

    public MainWindow()
    {
        InitializeComponent();
        controller = new OverlayController(this, OverlayPanel, CpuDisplay, GpuDisplay, RamDisplay);
    }
}
