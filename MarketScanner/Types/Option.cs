using System;

namespace MarketScanner.Types
{
    public class Option
    {
        public string Underlaying { get; set; }
        public string Optionsymbol { get; set; }
        public string Optiontype { get; set; }
        public DateTime ExpirationDate { get; set; }
        public DateTime QuoteDate { get; set; }
        public double Strike { get; set; }
        public string Last { get; set; }
        public string Bid { get; set; }
        public string Ask { get; set; }
        public string Volume { get; set; }
        public string OpenInterest { get; set; }

    }
}
