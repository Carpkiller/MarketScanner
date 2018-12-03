using System;

namespace MarketScanner.Types
{
    public class Trade
    {
        public DateTime ExpirationDate { get; set; }
        public DateTime OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public double OpenPrice { get; set; }
        public double? ClosePrice { get; set; }
        public double OpenStockPrice { get; set; }
        public double? CloseStockPrice { get; set; }
        public double Strike { get; set; }
        public string Contract { get; set; }

        public Trade()
        {
            CloseDate = null;
            ClosePrice = null;
        }
    }
}
