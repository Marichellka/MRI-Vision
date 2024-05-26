using System.Drawing;
using System.IO;
using MRI_Vision.Domain.Picture;
using MRI_Vision.Python;
using Python.Runtime;

namespace MRI_Vision.Domain.Model;

public class Model
{
    private const string _modelModulePath = @".\utils\model_helper.py";
    private dynamic _model;

    private Model() { }

    public static async Task<Model> CreateAsync(string path = @".\Model\model.pth")
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

            _model = loader.ModelHelper.load_model(path);
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

            var tmp = loader.ModelHelper.analyze_image(path, _model);

            var picture = (float[][][])tmp[0];
            var restored = (float[][][])tmp[1];

            return (new MRIPicture(picture), new AnomalyPicture(picture, restored, Color.FromArgb(255, 0, 0)));
        }
    }
}