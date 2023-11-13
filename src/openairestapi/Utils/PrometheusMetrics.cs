#region Using Directives
using OpenAiRestApi.Options;
using Prometheus;
using System.Drawing;
#endregion

namespace OpenAiRestApi.Utils
{
    public class PrometheusMetrics
    {
        #region Private Fields
        // Gauge metrics
        private readonly Gauge _promptTokenCount;
        private readonly Gauge _completionTokenCount;
        private readonly Gauge _totalTokenCount;

        // Counter metrics
        private readonly Counter _promptTokenTotal;
        private readonly Counter _completionTokenTotal;
        private readonly Counter _totalTokenTotal;

        // Histogram metrics
        private readonly Histogram _promptTokenHistogram;
        private readonly Histogram _completionTokenHistogram;
        private readonly Histogram _totalTokenHistogram;
        #endregion

        #region Public Constructors
        public PrometheusMetrics(PrometheusOptions prometheusOptions)
        {
            // Gauge metrics
            _promptTokenCount = Metrics.CreateGauge(
                "openai_prompt_tokens_processed",
                "Number of prompt tokens processed by the Azure OpenAI Service.",
                labelNames: new[] { "openai_name", "tenant_name", "method_name" });


            _completionTokenCount = Metrics.CreateGauge(
                "openai_completion_tokens_processed",
                "Number of completion tokens processed by the Azure OpenAI Service.",
                labelNames: new[] { "openai_name", "tenant_name", "method_name" });

            _totalTokenCount = Metrics.CreateGauge(
                "openai_total_tokens_processed",
                "Number of total tokens processed by the Azure OpenAI Service.",
                labelNames: new[] { "openai_name", "tenant_name", "method_name" });

            // Counter metrics
            _promptTokenTotal = Metrics.CreateCounter(
                "openai_prompt_tokens_total",
                "Total number of prompt tokens processed by the Azure OpenAI Service.",
                labelNames: new[] { "openai_name", "tenant_name", "method_name" });

            _completionTokenTotal = Metrics.CreateCounter(
                "openai_completion_tokens_total",
                "Total number of completion tokens processed by the Azure OpenAI Service.",
                labelNames: new[] { "openai_name", "tenant_name", "method_name" });

            _totalTokenTotal = Metrics.CreateCounter(
               "openai_total_tokens_total",
               "Total number of total tokens processed by the Azure OpenAI Service.",
               labelNames: new[] { "openai_name", "tenant_name", "method_name" });

            // Histogram metrics
            _promptTokenHistogram = Metrics.CreateHistogram(
                "openai_prompt_tokens",
                "The distribution of prompt tokens processed by the Azure OpenAI Service.",
                labelNames: new[] { "openai_name", "tenant_name", "method_name" },
                new HistogramConfiguration
                {
                    Buckets = Histogram.LinearBuckets(
                        start: prometheusOptions.Histograms["PromptTokens"].Start,
                        width: prometheusOptions.Histograms["PromptTokens"].Width,
                        count: prometheusOptions.Histograms["PromptTokens"].Count)
                });

            _completionTokenHistogram = Metrics.CreateHistogram(
                "openai_completion_tokens",
                "The distribution of completion tokens processed by the Azure OpenAI Service.",
                labelNames: new[] { "openai_name", "tenant_name", "method_name" },
                new HistogramConfiguration
                {
                    Buckets = Histogram.LinearBuckets(
                        start: prometheusOptions.Histograms["CompletionTokens"].Start,
                        width: prometheusOptions.Histograms["CompletionTokens"].Width,
                        count: prometheusOptions.Histograms["CompletionTokens"].Count)
                });

            _totalTokenHistogram = Metrics.CreateHistogram(
               "openai_total_tokens",
               "The distribution of total tokens processed by the Azure OpenAI Service.",
               labelNames: new[] { "openai_name", "tenant_name", "method_name" },
               new HistogramConfiguration
               {
                   Buckets = Histogram.LinearBuckets(
                        start: prometheusOptions.Histograms["TotalTokens"].Start,
                        width: prometheusOptions.Histograms["TotalTokens"].Width,
                        count: prometheusOptions.Histograms["TotalTokens"].Count)
               });
    }
        #endregion

        #region Public Methods
        public void SetPromptTokenCount(string tenant, string openAIName, string methodName, double value) => _promptTokenCount.WithLabels(new[] { openAIName.ToLower(), tenant.ToLower(), methodName.ToLower() }).Set(value);

        public void SetCompletionTokenCount(string tenant, string openAIName, string methodName, double value) => _completionTokenCount.WithLabels(new[] { openAIName.ToLower(), tenant.ToLower(), methodName.ToLower() }).Set(value);

        public void SetTotalTokenCount(string tenant, string openAIName, string methodName, double value) => _totalTokenCount.WithLabels(new[] { openAIName.ToLower(), tenant.ToLower(), methodName.ToLower() }).Set(value);

        public void IncPromptTokenTotal(string tenant, string openAIName, string methodName, double value) => _promptTokenTotal.WithLabels(new[] { openAIName.ToLower(), tenant.ToLower(), methodName.ToLower() }).Inc(value);

        public void IncCompletionTokenTotal(string tenant, string openAIName, string methodName, double value) => _completionTokenTotal.WithLabels(new[] { openAIName.ToLower(), tenant.ToLower(), methodName.ToLower() }).Inc(value);

        public void IncTotalTokenTotal(string tenant, string openAIName, string methodName, double value) => _totalTokenTotal.WithLabels(new[] { openAIName.ToLower(), tenant.ToLower(), methodName.ToLower() }).Inc(value);

        public void ObservePromptTokenHistogram(string tenant, string openAIName, string methodName, double value) => _promptTokenHistogram.WithLabels(new[] { openAIName.ToLower(), tenant.ToLower(), methodName.ToLower() }).Observe(value);

        public void ObserveCompletionTokenHistogram(string tenant, string openAIName, string methodName, double value) => _completionTokenHistogram.WithLabels(new[] { openAIName.ToLower(), tenant.ToLower(), methodName.ToLower() }).Observe(value);

        public void ObserveTotalTokenHistogram(string tenant, string openAIName, string methodName, double value) => _totalTokenHistogram.WithLabels(new[] { openAIName.ToLower(), tenant.ToLower(), methodName.ToLower() }).Observe(value);
        #endregion
    }
}
