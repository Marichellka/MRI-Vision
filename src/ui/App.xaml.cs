using System.Configuration;
using System.Data;
using System.Windows;
using Python.Runtime;

namespace MRI_Vision.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Runtime.PythonDLL = @"C:\Python311\python311.dll";
        PythonEngine.Initialize();
        PythonEngine.BeginAllowThreads();
        base.OnStartup(e);
    }
}   