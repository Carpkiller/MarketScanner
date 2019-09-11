using System.Collections.Generic;

namespace MarketScanner.Types
{
    public class Stock
    {
        public Quote Quote { get; set; }
        public IList<Dividend> dividends { get; set; }
    }

    public class Quote
    {
        public string symbol { get; set; }
        public string companyName { get; set; }
        public string primaryExchange { get; set; }
        public string sector { get; set; }
        public string calculationPrice { get; set; }
        public decimal? open { get; set; }
        public long openTime { get; set; }
        public double close { get; set; }
        public long closeTime { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double latestPrice { get; set; }
        public string latestSource { get; set; }
        public string latestTime { get; set; }
        public long latestUpdate { get; set; }
        public int latestVolume { get; set; }
        public double iexRealtimePrice { get; set; }
        public int iexRealtimeSize { get; set; }
        public long iexLastUpdated { get; set; }
        public double delayedPrice { get; set; }
        public long delayedPriceTime { get; set; }
        public double extendedPrice { get; set; }
        public double extendedChange { get; set; }
        public double extendedChangePercent { get; set; }
        public long extendedPriceTime { get; set; }
        public double previousClose { get; set; }
        public double change { get; set; }
        public double? changePercent { get; set; }
        public double iexMarketPercent { get; set; }
        public int iexVolume { get; set; }
        public int avgTotalVolume { get; set; }
        public double iexBidPrice { get; set; }
        public int iexBidSize { get; set; }
        public double iexAskPrice { get; set; }
        public int iexAskSize { get; set; }
        public long marketCap { get; set; }
        public double peRatio { get; set; }
        public double week52High { get; set; }
        public double week52Low { get; set; }
        public double ytdChange { get; set; }
    }
}
