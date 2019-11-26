using System;

namespace MarketScanner.Types
{
    public class Trade
    {
        public DateTime ExpirationDate { get; set; }
        public DateTime OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal? ClosePrice { get; set; }
        public decimal OpenStockPrice { get; set; }
        public decimal? CloseStockPrice { get; set; }
        public double Strike { get; set; }
        public string Contract { get; set; }
        public decimal PocetKontraktov { get; set; }

        public Trade()
        {
            CloseDate = null;
            ClosePrice = null;
        }
    }
}
