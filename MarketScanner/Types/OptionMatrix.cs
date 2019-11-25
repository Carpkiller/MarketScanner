using System;

namespace MarketScanner.Types
{
    public class OptionMatrixRow
    {
        public string Underlaying { get; set; }
        public string Optionsymbol { get; set; }
        public string Optiontype { get; set; }
        public DateTime ExpirationDate { get; set; }
        public DateTime QuoteDate { get; set; }
        public double Strike { get; set; }
        public Option Call { get; set; }
        public Option Put { get; set; }
        public decimal StockPrice { get; set; }
    }
}
