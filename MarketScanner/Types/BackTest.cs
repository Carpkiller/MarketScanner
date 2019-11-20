using System;

namespace MarketScanner.Types
{
    public class BackTest
    {
        public string Underlaying { get; set; }
        public string Optionsymbol { get; set; }
        public string Optiontype { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime StartDate { get; set; }
        public double Price { get; set; }
        public double? Strike { get; set; }
        public double Delta { get; set; }
        public double Profit { get; set; }
        public bool Ukonceny { get; set; }
    }
}
