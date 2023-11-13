#region Using Directives
using System.Text.Json.Serialization;
#endregion

namespace OpenAiRestApi.Model
{
    public class Message
    {
        #region Public Properties
        [JsonPropertyName("role")]
        public string? Role { get; set; }
        [JsonPropertyName("content")]
        public string? Content { get; set; }
        #endregion
    }
}
