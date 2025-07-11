namespace AISummarizerAPI.Infrastructure.Interfaces;

public interface IRetryPolicyService
{
    Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> operation,
        CancellationToken cancellationToken);
}