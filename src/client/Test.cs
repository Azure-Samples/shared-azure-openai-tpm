#region Using Directives
using System.ComponentModel.DataAnnotations; 
#endregion

namespace OpenAiRestApi.Client;
public class Test
{
    #region Public Properties
    [Required, NotEmptyOrWhitespace]
    public string Name { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public string Description { get; set; } = string.Empty;

    [Required, NotEmptyOrWhitespace]
    public Func<Task> ActionAsync { get; set; } = () => Task.CompletedTask; 
    #endregion
}