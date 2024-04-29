using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRI_Vision.UI.Utils;

internal static class ModelHelper
{
    private static Task<Model> _modelTask;

    public static async Task<Model> GetModelAsync()
    {
        _modelTask ??= Model.CreateAsync();
        return await _modelTask;
    }
}
