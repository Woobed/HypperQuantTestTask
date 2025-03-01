using HyperQuantTestTask.Models;

namespace HyperQuantTestTask.Interfaces
{
    public interface IRestConnector
    {
        Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount);
        Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0);

    }
}
