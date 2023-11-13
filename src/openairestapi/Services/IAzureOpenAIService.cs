#region Using Directives
using OpenAiRestApi.Model;
using System.Runtime.CompilerServices; 
#endregion

namespace OpenAiRestApi.Services
{
    public interface IAzureOpenAIService
    {
        Task<string> GetChatCompletionsAsync(string tenant, IEnumerable<Message> history, CancellationToken cancellationToken = default);
        IAsyncEnumerable<string> GetChatCompletionsStreamingAsync(string tenant, IEnumerable<Message> history, CancellationToken cancellationToken = default);
    }
}