#region Using Directives
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using OpenAiRestApi.Controllers;
using OpenAiRestApi.Utils;
using OpenAiRestApi.Options;
using Azure;
using Azure.Identity;
using System.Runtime.CompilerServices;
using SharpToken;
using OpenAiRestApi.Model;
#endregion

namespace OpenAiRestApi.Services
{
    public class AzureOpenAIService : IAzureOpenAIService
    {
        #region Private Fields
        private readonly PrometheusMetrics _prometheusMetrics;
        private readonly ILogger<OpenAIController> _logger;
        private readonly PrometheusOptions _prometheusOptions;
        private readonly AzureOpenAIOptions _azureOpenAiOptions;
        private readonly Dictionary<string, string> _tenantAzureOpenAiMappings;
        private readonly Dictionary<string, OpenAIClient> _openAIClients;
        private readonly ChatCompletionsOptions _chatCompletionsOptions;
        private int _roundRobinIndex = 0;
        #endregion

        #region Public Constructors
        public AzureOpenAIService(ILogger<OpenAIController> logger,
            PrometheusOptions prometheusOptions,
            AzureOpenAIOptions azureOpenAiOptions,
            Dictionary<string, string> tenantAzureOpenAiMappings,
            ChatCompletionsOptions chatCompletionsOptions)
        {
            _prometheusMetrics = new PrometheusMetrics(prometheusOptions);
            _logger = logger;
            _prometheusOptions = prometheusOptions;
            _azureOpenAiOptions = azureOpenAiOptions;
            _tenantAzureOpenAiMappings = tenantAzureOpenAiMappings;
            _openAIClients = new Dictionary<string, OpenAIClient>();
            _chatCompletionsOptions = chatCompletionsOptions;

            foreach (var service in _azureOpenAiOptions.Services.Keys)
            {
                OpenAIClientOptions options = new()
                {
                    RetryPolicy = new RetryPolicy(maxRetries: Math.Max(0, _azureOpenAiOptions.Services[service].MaxRetries), new SequentialDelayStrategy()),
                    Diagnostics = { IsLoggingContentEnabled = true }
                };

                if (string.IsNullOrEmpty(_azureOpenAiOptions.Services[service].Endpoint))
                {
                    _logger.LogError($"Azure OpenAI Service {service} endpoint is not configured.");
                    continue;
                }

                if (string.Compare(_azureOpenAiOptions.Services[service].Type, "azuread", true) == 0)
                {
                    _openAIClients.Add(service, new OpenAIClient(new Uri(_azureOpenAiOptions.Services[service].Endpoint), new DefaultAzureCredential(), options));
                    _logger.LogInformation($"Azure OpenAI Service {service} is configured with Azure Microsoft Entra ID authentication.");
                }
                else
                {
                    if (string.IsNullOrEmpty(_azureOpenAiOptions.Services[service].ApiKey))
                    {
                        _logger.LogError($"Azure OpenAI Service {service} API key is not configured.");
                        continue;
                    }

                    _openAIClients.Add(service, new OpenAIClient(new Uri(_azureOpenAiOptions.Services[service].Endpoint), new AzureKeyCredential(_azureOpenAiOptions.Services[service].ApiKey), options));
                    _logger.LogInformation($"Azure OpenAI Service {service} is configured with an API key authentication.");
                }
            }
        }
        #endregion

        #region Public Methods
        public async Task<string> GetChatCompletionsAsync(string tenant, IEnumerable<Message> history, CancellationToken cancellationToken = default)
        {

            if (history?.Any() != true)
            {
                throw new ArgumentException("History cannot be null or empty.", nameof(history));
            }

            if (string.IsNullOrEmpty(tenant))
            {
                throw new ArgumentException("Tenant cannot be null or empty.", nameof(tenant));
            }

            var openAIName = GetOpenAIServiceName(tenant);
            var openAIClient = _openAIClients[openAIName];
            var openAIOptions = _azureOpenAiOptions.Services[openAIName];

            _logger.LogInformation($"New request: method=[GetChatCompletionsAsync] tenant=[{tenant}] openai=[{openAIName}]");

            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                MaxTokens = _chatCompletionsOptions.MaxTokens.HasValue ? _chatCompletionsOptions.MaxTokens.Value : null,
                Temperature = _chatCompletionsOptions.Temperature.HasValue ? _chatCompletionsOptions.Temperature.Value : null,
                NucleusSamplingFactor = _chatCompletionsOptions.NucleusSamplingFactor.HasValue ? _chatCompletionsOptions.NucleusSamplingFactor.Value : null,
                FrequencyPenalty = _chatCompletionsOptions.FrequencyPenalty.HasValue ? _chatCompletionsOptions.FrequencyPenalty.Value : null,
                PresencePenalty = _chatCompletionsOptions.PresencePenalty.HasValue ? _chatCompletionsOptions.PresencePenalty.Value : null,
            };

            if (chatCompletionsOptions.StopSequences is { Count: > 0 })
            {
                foreach (var s in chatCompletionsOptions.StopSequences) { chatCompletionsOptions.StopSequences.Add(s); }
            }

            if (history.Count() == 1 || history.FirstOrDefault()?.Role != ChatRole.System)
            {
                chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System, _azureOpenAiOptions.SystemPrompt));
            }

            foreach (var message in history)
            {
                chatCompletionsOptions.Messages.Add(new ChatMessage(message.Role, message.Content));
            }

            var tokenNumber = TruncateHistory(openAIOptions.Model,
                _chatCompletionsOptions.MaxTokens.HasValue ? _chatCompletionsOptions.MaxTokens.Value : 4096,
                openAIOptions.MaxResponseTokens,
                chatCompletionsOptions.Messages);

            if (_prometheusOptions.Enabled)
            {
                
            }

            var response = await openAIClient.GetChatCompletionsAsync(openAIOptions.Model, chatCompletionsOptions, cancellationToken).ConfigureAwait(false);
            var result = response?.Value?.Choices?.FirstOrDefault()?.Message.Content ?? string.Empty;
                        
            if (_prometheusOptions.Enabled)
            {
                
                var promptTokens = response?.Value?.Usage?.PromptTokens != null ? (double)response?.Value?.Usage?.PromptTokens! : tokenNumber;
                _prometheusMetrics.SetPromptTokenCount(tenant, openAIName, "chat", promptTokens);
                _prometheusMetrics.IncPromptTokenTotal(tenant, openAIName, "chat", promptTokens);
                _prometheusMetrics.ObservePromptTokenHistogram(tenant, openAIName, "chat", promptTokens);

                var completionTokens = response?.Value?.Usage?.CompletionTokens != null ? (double)response?.Value?.Usage?.CompletionTokens! : GetTokenNumberFromString(openAIOptions.Model, result);
                _prometheusMetrics.SetCompletionTokenCount(tenant, openAIName, "chat", completionTokens);
                _prometheusMetrics.IncCompletionTokenTotal(tenant, openAIName, "chat", completionTokens);
                _prometheusMetrics.ObserveCompletionTokenHistogram(tenant, openAIName, "chat", completionTokens);

                var totalTokens = response?.Value?.Usage?.TotalTokens != null ? (double)response?.Value?.Usage?.TotalTokens! : promptTokens + completionTokens;
                _prometheusMetrics.SetTotalTokenCount(tenant, openAIName, "chat", totalTokens);
                _prometheusMetrics.IncTotalTokenTotal(tenant, openAIName, "chat", totalTokens);
                _prometheusMetrics.ObserveTotalTokenHistogram(tenant, openAIName, "chat", totalTokens);
            }
            return result;
        }

        public async IAsyncEnumerable<string> GetChatCompletionsStreamingAsync(string tenant, IEnumerable<Message> history, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {

            if (history?.Any() != true)
            {
                throw new ArgumentException("History cannot be null or empty.", nameof(history));
            }

            if (string.IsNullOrEmpty(tenant))
            {
                throw new ArgumentException("Tenant cannot be null or empty.", nameof(tenant));
            }

            var openAIName = GetOpenAIServiceName(tenant);
            var openAIClient = _openAIClients[openAIName];
            var openAIOptions = _azureOpenAiOptions.Services[openAIName];

            _logger.LogInformation($"New request: method=[GetChatCompletionsStreamingAsync] tenant=[{tenant}] openai=[{openAIName}]");

            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                MaxTokens = _chatCompletionsOptions.MaxTokens.HasValue ? _chatCompletionsOptions.MaxTokens.Value : null,
                Temperature = _chatCompletionsOptions.Temperature.HasValue ? _chatCompletionsOptions.Temperature.Value : null,
                NucleusSamplingFactor = _chatCompletionsOptions.NucleusSamplingFactor.HasValue ? _chatCompletionsOptions.NucleusSamplingFactor.Value : null,
                FrequencyPenalty = _chatCompletionsOptions.FrequencyPenalty.HasValue ? _chatCompletionsOptions.FrequencyPenalty.Value : null,
                PresencePenalty = _chatCompletionsOptions.PresencePenalty.HasValue ? _chatCompletionsOptions.PresencePenalty.Value : null,
            };

            if (chatCompletionsOptions.StopSequences is { Count: > 0 })
            {
                foreach (var s in chatCompletionsOptions.StopSequences) { chatCompletionsOptions.StopSequences.Add(s); }
            }

            if (history.Count() == 1 || history.FirstOrDefault()?.Role != ChatRole.System)
            {
                chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System, _azureOpenAiOptions.SystemPrompt));
            }

            foreach (var message in history)
            {
                chatCompletionsOptions.Messages.Add(new ChatMessage(message.Role, message.Content));
            }

            var promptTokens = TruncateHistory(openAIOptions.Model,
                _chatCompletionsOptions.MaxTokens.HasValue ? _chatCompletionsOptions.MaxTokens.Value : 4096,
                openAIOptions.MaxResponseTokens,
                chatCompletionsOptions.Messages);

            if (_prometheusOptions.Enabled)
            {
                _prometheusMetrics.SetPromptTokenCount(tenant, openAIName, "stream", promptTokens);
                _prometheusMetrics.IncPromptTokenTotal(tenant, openAIName, "stream", promptTokens);
                _prometheusMetrics.ObservePromptTokenHistogram(tenant, openAIName, "stream", promptTokens);
            }

            var response = await openAIClient.GetChatCompletionsStreamingAsync(openAIOptions.Model, chatCompletionsOptions, cancellationToken).ConfigureAwait(false);
            var completionTokens = 0;
            await foreach (var choice in response.Value.GetChoicesStreaming(cancellationToken))
            {
                await foreach (var message in choice.GetMessageStreaming(cancellationToken))
                {
                    var result = message.Content ?? string.Empty;

                    if (_prometheusOptions.Enabled)
                    {
                        completionTokens += GetTokenNumberFromString(openAIOptions.Model, result);
                    }

                    yield return result;
                }
            }
            if (_prometheusOptions.Enabled)
            {
                _prometheusMetrics.SetCompletionTokenCount(tenant, openAIName, "stream", completionTokens);
                _prometheusMetrics.IncCompletionTokenTotal(tenant, openAIName, "stream", completionTokens);
                _prometheusMetrics.ObserveCompletionTokenHistogram(tenant, openAIName, "stream", completionTokens);

                var totalTokens = promptTokens + completionTokens;
                _prometheusMetrics.SetTotalTokenCount(tenant, openAIName, "stream", totalTokens);
                _prometheusMetrics.IncTotalTokenTotal(tenant, openAIName, "stream", totalTokens);
                _prometheusMetrics.ObserveTotalTokenHistogram(tenant, openAIName, "stream", totalTokens);
            }
        }
        #endregion

        #region Private Methods
        private string GetOpenAIServiceName(string tenant)
        {
            string openAIServiceName;
            if (_tenantAzureOpenAiMappings.ContainsKey(tenant) &&
                _openAIClients.ContainsKey(_tenantAzureOpenAiMappings[tenant]))
            {
                openAIServiceName = _tenantAzureOpenAiMappings[tenant];
                _logger.LogInformation($"Tenant {tenant} is mapped to {openAIServiceName} Azure OpenAI Service .");
                return openAIServiceName;
            }
            else
            {
                openAIServiceName = _openAIClients.Keys.ElementAt(_roundRobinIndex);
                _logger.LogInformation($"{openAIServiceName} Azure OpenAI Service was assigned to tenant {tenant} by round robin policy.");
                _roundRobinIndex = (_roundRobinIndex + 1) % _openAIClients.Count;
                return openAIServiceName;
            }
        }
        #endregion

        #region Private Methods
        private int GetTokenNumberFromString(string model, string message)
        {
            return GptEncoding.GetEncodingForModel(model).Encode(message).Count;
        }

        private int GetTokenNumberFromMessages(string model, IList<ChatMessage> messages)
        {
            var encoding = GptEncoding.GetEncodingForModel(model);
            int numTokens = 0;

            foreach (var message in messages)
            {
                numTokens += 4; // Every message follows <im_start>{role/name}\n{content}<im_end>\n
                numTokens += encoding.Encode(message.Role.ToString()).Count;
                numTokens += encoding.Encode(message.Content).Count;
            }

            numTokens += 2; // Every reply is primed with <im_start>assistant
            return numTokens;
        }

        private int TruncateHistory(string model, int maxTokens, int maxResponseTokens, IList<ChatMessage> messages)
        {
            int historyTokenNumber = GetTokenNumberFromMessages(model, messages);

            while (historyTokenNumber + maxResponseTokens >= maxTokens)
            {
                messages.RemoveAt(1);
                historyTokenNumber = GetTokenNumberFromMessages(model, messages);
            }
            return historyTokenNumber;
        }
        #endregion
    }
}
