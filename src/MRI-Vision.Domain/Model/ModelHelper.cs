namespace MRI_Vision.Domain.Model;

/// <summary>
/// Helper class for <see cref="Model"/>
/// </summary>
public static class ModelHelper
{
    private static Task<Model> _modelTask;

    /// <summary>
    /// Get or create <see cref="Model"/> asynchronously
    /// </summary>
    /// <remarks>
    /// Caches the instance once its been created
    /// </remarks>
    public static async Task<Model> GetModelAsync()
    {
        _modelTask ??= Model.CreateAsync();
        return await _modelTask;
    }
}
