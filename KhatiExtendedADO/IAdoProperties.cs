namespace KhatiExtendedADO
{
    public interface IAdoProperties
    {
        Task<Response<string>> SqlWriteAsync(string Query);
        Task<Response<TResponse>> SqlReadAsync<TResponse>(string Query);
        Task<Response<TResponse>> SqlReadScalerModelAsync<TResponse>(string Query) where TResponse : class;
        Task<Response<TResponse>> SqlReadScalerValueAsync<TResponse>(string Query);
        Task<(bool success, string? message, string? errorMessage)> SqlBulkUploadAsync<T>(List<T> model,
             string tableName) where T : class;
    }
}
