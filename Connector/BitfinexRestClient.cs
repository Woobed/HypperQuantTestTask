namespace HyperQuantTestTask.Connector
{
    public class BitfinexRestClient
    {

        private readonly HttpClient _httpClient;
        private const string Url = "https://api-pub.bitfinex.com/v2";

        public BitfinexRestClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
    }
}
