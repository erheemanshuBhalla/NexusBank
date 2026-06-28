namespace NexusBank.Api.Interface
{
    public interface IStatementStorageService
    {
        Task<string> UploadStatementAsync(string accountId, string statementPeriod, string textContent);
    }
}
