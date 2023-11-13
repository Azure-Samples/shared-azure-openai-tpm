namespace OpenAiRestApi.Options;

public class AzureOpenAIOptions
{
    #region Public Properties
    /// <summary>
    /// Gets or sets the system prompt.
    /// </summary>
    public string SystemPrompt { get; set; } = "You are an AI assistant that helps people find information.";
    /// <summary>
    /// Gets or sets a dictionary of Azure OpenAI services.
    /// </summary>
    public Dictionary<string, AzureOpenAIServiceInfo> Services { get; set; } = new Dictionary<string, AzureOpenAIServiceInfo>();
    #endregion
}

public class AzureOpenAIServiceInfo
{
    #region Public Properties
    /// <summary>
    /// Gets or sets the version of URI of the Azure OpenAI Service.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the API key of the Azure OpenAI Service.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the authentication type:
    /// - AzureAD: Azure Microsoft Entra ID authentication.
    /// - Otherwise: API key authentication.
    /// </summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the version of the Azure OpenAI API.
    /// </summary>
    public string Version { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the name of the chatGPT deployment.
    /// </summary>
    public string Deployment { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the name of the chatGPT model.
    /// </summary>
    public string Model { get; set; } = string.Empty;
    /// <summary>
    ///  Gets or sets the maximum number of response tokens to generate.
    /// </summary>
    public int MaxResponseTokens { get; set; } = 500;
    /// <summary>
    ///  Gets or sets the temperature that controls the randomness of the completion.
    /// The higher the temperature, the more random the completion.
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    #endregion
}