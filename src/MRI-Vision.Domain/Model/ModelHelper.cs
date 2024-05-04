namespace MRI_Vision.Domain.Model;

public static class ModelHelper
{
    private static Task<Model> _modelTask;

    public static async Task<Model> GetModelAsync()
    {
        _modelTask ??= Model.CreateAsync();
        return await _modelTask;
    }
}
