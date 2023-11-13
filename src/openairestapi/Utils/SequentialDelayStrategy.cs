#region Using Directives
using Azure.Core;
using Azure;
#endregion

namespace OpenAiRestApi.Utils;

public class SequentialDelayStrategy : DelayStrategy
{
    #region Private Static Fields
    private static readonly TimeSpan[] s_pollingSequence =
    {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(6),
            TimeSpan.FromSeconds(6),
            TimeSpan.FromSeconds(8)
        };
    private static readonly TimeSpan s_maxDelay = s_pollingSequence[^1];
    #endregion

    #region Public Constructors
    public SequentialDelayStrategy() : base(s_maxDelay, 0)
    {
    }
    #endregion

    #region Protected Methods
    protected override TimeSpan GetNextDelayCore(Response? response, int retryNumber)
    {
        int index = Math.Max(0, retryNumber - 1);
        return index >= s_pollingSequence.Length ? s_maxDelay : s_pollingSequence[index];
    }
    #endregion
}
