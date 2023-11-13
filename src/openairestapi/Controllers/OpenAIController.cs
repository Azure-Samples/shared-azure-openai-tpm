#region Using Directives
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using OpenAiRestApi.Model;
using OpenAiRestApi.Services;
#endregion

namespace OpenAiRestApi.Controllers;

[ApiController]
[Route("openai")]
public class OpenAIController : ControllerBase
{
    #region Private Fields
    private readonly ILogger<OpenAIController> _logger;
    private readonly AzureOpenAIService _azureOpenAIService;
    #endregion

    #region Public Constructors
    public OpenAIController(ILogger<OpenAIController> logger, AzureOpenAIService azureOpenAIService)
    {
        _logger = logger;
        _azureOpenAIService = azureOpenAIService;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Returns a string that includes both the tenant and value provided as parameters.
    /// </summary>
    /// <param name="tenant">Specifies the tenant name</param>
    /// <param name="value">Specifies a value</param>
    /// <returns>A a string that includes both the tenant and value provided as parameters</returns>
    /// <response code="200">Success</response>
    /// <response code="400">Bad Request</response>
    [HttpGet]
    [Route("echo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Echo(string? tenant, int value)
    {
        // Validate the tenant parameter
        if (string.IsNullOrEmpty(tenant))
        {
            return BadRequest("Tenant cannot be null or empty.");
        }

        // Normalize the tenant name
        tenant = tenant.ToLower();

        // Log the request
        _logger.LogInformation($"Echo called with tenant = {tenant} and value = {value}");

        // Set the content type
        Response.Headers.ContentType = "text/plain";

        // Completion metrics
        return Ok($"tenant: {tenant} value: {value}");
    }

    /// <summary>
    /// Returns a completion prompt.
    /// </summary>
    /// <param name="tenant">Specifies the tenant name</param>
    /// <param name="conversation">Specifies a collection of messages representing the history</param>
    /// <returns>A completion prompt</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /openai/chat
    ///     [
    ///         {
    ///             "role": "user",
    ///             "content": "Tell me about Milan"
    ///         }
    ///     ]
    ///
    /// </remarks>
    /// <response code="200">Success</response>
    /// <response code="400">Bad Request</response>
    [HttpPost]
    [Route("chat")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(Task<IActionResult>))]
    public async Task<IActionResult> GetChatCompletionsAsync(string tenant,[FromBody] IEnumerable<Message> conversation)
    {
        try
        {
            // Validate the tenant parameter
            if (string.IsNullOrEmpty(tenant))
            {
                return BadRequest("Tenant cannot be null or empty.");
            }

            // Log the response
            _logger.LogInformation($"GetChatCompletionsAsync call by {tenant.ToLower()} tenant processing...");

            var result = await _azureOpenAIService.GetChatCompletionsAsync(tenant, conversation);

            // Log the request
            _logger.LogInformation($"GetChatCompletionsAsync call by {tenant.ToLower()} tenant successfully completed.");

            // Set the content type
            Response.Headers.ContentType = "text/plain";

            return Ok(result);
        }
        catch (Exception ex)
        {
            // Create the error message
            var errorMessage = $"GetChatCompletionsAsync call by {tenant.ToLower()} tenant failed: {ex.Message}.";
            
            // Log the error
            _logger.LogError(errorMessage);

            // Return the error
            return BadRequest(errorMessage);
        }
        
    }

    /// <summary>
    /// Begins a chat completions request and get an object that can stream response data as it becomes available.
    /// </summary>
    /// <param name="tenant">Specifies the tenant name</param>
    /// <param name="conversation">Specifies a collection of messages representing the history</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A streaming completion prompt</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /openai/chat
    ///     [
    ///         {
    ///             "role": "user",
    ///             "content": "Tell me about Milan"
    ///         }
    ///     ]
    ///
    /// </remarks>
    /// <response code="200">Success</response>
    /// <response code="400">Bad Request</response>
   [HttpPost]
    [Route("stream")]
    public async IAsyncEnumerable<string> GetChatCompletionsStreamingAsync(string tenant, [FromBody] IEnumerable<Message> conversation, [EnumeratorCancellation]CancellationToken cancellationToken = default)
    {
        // Validate the tenant parameter
        if (string.IsNullOrEmpty(tenant))
        {
            throw new ArgumentNullException(nameof(tenant), "Tenant cannot be null or empty.");
        }

        // Log the response
        _logger.LogInformation($"GetChatCompletionsStreamingAsync call by {tenant.ToLower()} tenant processing...");

        // Return the result
        await foreach (var completion in _azureOpenAIService.GetChatCompletionsStreamingAsync(tenant, conversation, cancellationToken))
        {
            yield return completion;
            await Task.Delay(1);
        }
    }
    #endregion
}