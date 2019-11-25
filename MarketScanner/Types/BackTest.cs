using System;

namespace MarketScanner.Types
{
    [Serializable]
    public class BackTest
    {
        public string Underlaying { get; set; }
        public string Cislo { get; set; }
        public string Optionsymbol { get; set; }
        public string Optiontype { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime StartDate { get; set; }
        public decimal Price { get; set; }
        public double? Strike { get; set; }
        public double Delta { get; set; }
        public decimal Profit { get; set; }
        public bool Ukonceny { get; set; }
    }
}
