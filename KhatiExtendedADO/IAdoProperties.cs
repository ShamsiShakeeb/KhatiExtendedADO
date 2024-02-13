namespace KhatiExtendedADO
{
    public interface IAdoProperties
    {
        Response<string> SqlWrite(string Query);
        Response<TResponse> SqlRead<TResponse>(string Query);
        Response<TResponse> SqlReadScalerModel<TResponse>(string Query) where TResponse : class;
        Response<TResponse> SqlReadScalerValue<TResponse>(string Query);
        (bool success, string? message, string? errorMessage) SqlBulkUpload<T>(List<T> model,
             string tableName) where T : class;
    }
}
