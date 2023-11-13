#region Using Directives
using Grpc.Core; 
#endregion

namespace OpenAiRestApi.Services
{
    public interface IOpenAIGrpcService
    {
        #region Methods
        Task<EchoResponse> Echo(EchoRequest request, ServerCallContext context);
        Task<GetChatCompletionsResponse> GetChatCompletions(GetChatCompletionsRequest request, ServerCallContext context);
        Task GetChatCompletionsStreaming(GetChatCompletionsRequest request, IServerStreamWriter<GetChatCompletionsStreamingResponse> responseStream, ServerCallContext context); 
        #endregion
    }
}