#region Using Directives
using Grpc.Core;
using OpenAiRestApi.Model; 
#endregion

namespace OpenAiRestApi.Services;

public class OpenAIGrpcService : OpenAIServiceGrpc.OpenAIServiceGrpcBase, IOpenAIGrpcService
{
    #region Private Fields
    private readonly ILogger<OpenAIGrpcService> _logger;
    private readonly AzureOpenAIService _azureOpenAIService;
    #endregion

    #region Public Constructors
    public OpenAIGrpcService(ILogger<OpenAIGrpcService> logger, AzureOpenAIService azureOpenAIService)
    {
        _logger = logger;
        _azureOpenAIService = azureOpenAIService;
    }
    #endregion

    #region Public Properties
    public override Task<EchoResponse> Echo(EchoRequest request, ServerCallContext context)
    {
        // Validate the tenant parameter
        if (string.IsNullOrEmpty(request.Tenant))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Tenant cannot be null or empty."));
        }

        // Normalize the tenant name
        var tenant = request.Tenant.ToLower();

        // Log the request
        _logger.LogInformation($"Echo called with tenant = {tenant} and value = {request.Value}");

        // Set the content type
        context.ResponseTrailers.Add("Content-Type", "text/plain");

        // Completion metrics
        var response = new EchoResponse
        {
            Message = $"tenant: {tenant} value: {request.Value}"
        };

        return Task.FromResult(response);
    }

    public override async Task<GetChatCompletionsResponse> GetChatCompletions(GetChatCompletionsRequest request, ServerCallContext context)
    {
        // Validate the tenant parameter
        if (string.IsNullOrEmpty(request.Tenant))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Tenant cannot be null or empty."));
        }

        try
        {
            // Log the response
            _logger.LogInformation($"GetChatCompletions call by {request.Tenant.ToLower()} tenant processing...");

            // Convert the request to a list of messages
            var messages = request.Conversation.Select(message => new Message
            {
                Role = message.Role,
                Content = message.Content
            }).ToList();

            var result = await _azureOpenAIService.GetChatCompletionsAsync(request.Tenant, messages);

            // Log the request
            _logger.LogInformation($"GetChatCompletions call by {request.Tenant.ToLower()} tenant successfully completed.");

            var response = new GetChatCompletionsResponse
            {
                Result = result
            };

            return response;
        }
        catch (Exception ex)
        {
            // Create the error message
            var errorMessage = $"GetChatCompletions call by {request.Tenant.ToLower()} tenant failed: {ex.Message}.";

            // Log the error
            _logger.LogError(errorMessage);

            throw new RpcException(new Status(StatusCode.Internal, errorMessage));
        }
    }

    public override async Task GetChatCompletionsStreaming(GetChatCompletionsRequest request, IServerStreamWriter<GetChatCompletionsStreamingResponse> responseStream, ServerCallContext context)
    {
        // Validate the tenant parameter
        if (string.IsNullOrEmpty(request.Tenant))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Tenant cannot be null or empty."));
        }

        try
        {
            // Log the response
            _logger.LogInformation($"GetChatCompletionsStreaming call by {request.Tenant.ToLower()} tenant processing...");

            // Convert the request to a list of messages
            var messages = request.Conversation.Select(message => new Message
            {
                Role = message.Role,
                Content = message.Content
            }).ToList();

            await foreach (var item in _azureOpenAIService.GetChatCompletionsStreamingAsync(request.Tenant, messages, context.CancellationToken))
            {
                var response = new GetChatCompletionsStreamingResponse
                {
                    Message = item
                };

                await responseStream.WriteAsync(response);
            }
        }
        catch (Exception ex)
        {
            // Create the error message
            var errorMessage = $"GetChatCompletionsStreaming call by {request.Tenant.ToLower()} tenant failed: {ex.Message}.";

            // Log the error
            _logger.LogError(errorMessage);

            throw new RpcException(new Status(StatusCode.Internal, errorMessage));
        }
    }
    #endregion
}