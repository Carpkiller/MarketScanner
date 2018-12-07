﻿using System;

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
        public double Bid { get; set; }
        public double Ask { get; set; }
        public string Volume { get; set; }
        public string OpenInterest { get; set; }
        public double Delta { get; set; }
        public double StockPrice { get; set; }
    }
}