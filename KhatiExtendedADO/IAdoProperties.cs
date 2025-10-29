namespace KhatiExtendedADO
{
    public interface IAdoProperties
    {
        Task<Response<string>> SqlWriteAsync(string query, Dictionary<string, object> parameters);
        Task<Response<TResponse>> SqlReadAsync<TResponse>(string query, Dictionary<string, object>? parameters = null);
        Task<Response<TResponse>> SqlReadScalerModelAsync<TResponse>(string query, Dictionary<string, object>? parameters = null);
        Task<Response<TResponse>> SqlReadScalerValueAsync<TResponse>(string query, Dictionary<string, object>? parameters = null);
        Task<(bool success, string? message, string? errorMessage)> SqlBulkUploadAsync<T>(List<T> model,
             string tableName) where T : class;
    }
}
