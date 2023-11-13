#region Using Directives
namespace OpenAiRestApi.Options; 
#endregion

public class PrometheusOptions
{
    #region Public Properties
    /// <summary>
    /// Gets or sets a boolean value that specifies whether Prometheus metrics are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    public string SystemPrompt { get; set; } = "You are an AI assistant that helps people find information.";
    /// <summary>
    /// Gets or sets a dictionary of Prometheus metrics options.
    /// </summary>
    public Dictionary<string, PrometheusMetricInfo> Histograms { get; set; } = new Dictionary<string, PrometheusMetricInfo>();
    #endregion
}

public class PrometheusMetricInfo
{
    #region Public Properties
    /// <summary>
    /// Gets or sets the upper bound of the lowest bucket.
    /// </summary>
    public int Start { get; set; } = 100;
    /// <summary>
    /// Gets or sets the width of each bucket (the distance between the lower and upper bound of a bucket).
    /// </summary>
    public int Width { get; set; } = 10;
    /// <summary>
    ///  Gets or sets the number of buckets to create. It must be greater than zero.
    /// </summary>
    public int Count { get; set; } = 10;
    #endregion
}
