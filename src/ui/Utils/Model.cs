using System.Drawing;
using System.IO;
using Python.Runtime;

namespace MRI_Vision.UI.Utils;

public class Model
{
    private const string _modelModulePath = @"C:\Users\maric\Studying\Diploma\Project\MRI-Vision\src\network\model\utils\loader.py";
    private dynamic _model;

    public Model(string path= "C:\\Users\\maric\\Studying\\Diploma\\Project\\MRI-Vision\\content\\saved_model\\best_model.pth")
    {
        LoadModel(path);
    }
    
    private void LoadModel(string path)
    {
        using (var _ = Py.GIL())
        {
            dynamic os = Py.Import("os");
            dynamic sys = Py.Import("sys");
            sys.path.append(os.path.dirname(_modelModulePath));

            dynamic loader = Py.Import(Path.GetFileNameWithoutExtension(_modelModulePath));

            _model = loader.load_model(path);
        }
    }

    public (MRIPicture, AnomalyPicture) AnalyzeImage(string path)
    {
        using (var _ = Py.GIL())
        {
            dynamic os = Py.Import("os");
            dynamic sys = Py.Import("sys");
            sys.path.append(os.path.dirname(_modelModulePath));

            dynamic loader = Py.Import(Path.GetFileNameWithoutExtension(_modelModulePath));

            var tmp = loader.process_image(path, _model);

            var picture = (float[][][])tmp[0];
            var anomaly = (float[][][])tmp[1];
         
            return (new MRIPicture(picture), new AnomalyPicture(anomaly, Color.FromArgb(255, 0, 0)));
        }
    }
}