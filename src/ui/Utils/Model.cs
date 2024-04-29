using System.Drawing;
using System.IO;
using Python.Runtime;

namespace MRI_Vision.UI.Utils;

public class Model
{
    private const string _modelModulePath = @"C:\Users\maric\Studying\Diploma\Project\MRI-Vision\src\network\model\utils\loader.py";
    private dynamic _model;

    private Model() { }
    
    public static async Task<Model> CreateAsync(string path = "C:\\Users\\maric\\Studying\\Diploma\\Project\\MRI-Vision\\content\\saved_model\\best_model.pth")
    {
        Model model = new();
        await model.LoadModelAsync(path);
        return model;
    }

    private async Task LoadModelAsync(string path)
    {
        await PythonHelper.MoveTo();

        using (var _ = Py.GIL())
        {
            dynamic os = Py.Import("os");
            dynamic sys = Py.Import("sys");
            sys.path.append(os.path.dirname(_modelModulePath));

            dynamic loader = Py.Import(Path.GetFileNameWithoutExtension(_modelModulePath));

            _model = loader.load_model(path);
        }
    }

    public async Task<(MRIPicture, AnomalyPicture)> AnalyzeImageAsync(string path)
    {
        await PythonHelper.MoveTo();

        using (var _ = Py.GIL())
        {
            dynamic os = Py.Import("os");
            dynamic sys = Py.Import("sys");
            sys.path.append(os.path.dirname(_modelModulePath));

            dynamic loader = Py.Import(Path.GetFileNameWithoutExtension(_modelModulePath));

            var tmp = loader.process_image(path, _model);

            var picture = (float[][][])tmp[0];
            var restored = (float[][][])tmp[1];
         
            return (new MRIPicture(picture), new AnomalyPicture(picture, restored, Color.FromArgb(255, 0, 0)));
        }
    }
}