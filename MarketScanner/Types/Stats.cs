using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketScanner.Types
{
    public class Stats
    {
        public string companyName { get; set; }
        public long marketcap { get; set; }
        public double beta { get; set; }
        public double week52high { get; set; }
        public double week52low { get; set; }
        public double week52change { get; set; }
        public int shortInterest { get; set; }
        public string shortDate { get; set; }
        public double dividendRate { get; set; }
        public double dividendYield { get; set; }
        public string exDividendDate { get; set; }
        public int latestEPS { get; set; }
        public string latestEPSDate { get; set; }
        public long sharesOutstanding { get; set; }
        public long @float { get; set; }
        public double returnOnEquity { get; set; }
        public double consensusEPS { get; set; }
        public int numberOfEstimates { get; set; }
        public object EPSSurpriseDollar { get; set; }
        public double EPSSurprisePercent { get; set; }
        public string symbol { get; set; }
        public long EBITDA { get; set; }
        public long revenue { get; set; }
        public long grossProfit { get; set; }
        public long cash { get; set; }
        public long debt { get; set; }
        public double ttmEPS { get; set; }
        public int revenuePerShare { get; set; }
        public int revenuePerEmployee { get; set; }
        public double peRatioHigh { get; set; }
        public int peRatioLow { get; set; }
        public double returnOnAssets { get; set; }
        public object returnOnCapital { get; set; }
        public double profitMargin { get; set; }
        public double priceToSales { get; set; }
        public double priceToBook { get; set; }
        public double day200MovingAvg { get; set; }
        public double day50MovingAvg { get; set; }
        public double institutionPercent { get; set; }
        public object insiderPercent { get; set; }
        public double shortRatio { get; set; }
        public double year5ChangePercent { get; set; }
        public double year2ChangePercent { get; set; }
        public double year1ChangePercent { get; set; }
        public double ytdChangePercent { get; set; }
        public double month6ChangePercent { get; set; }
        public double month3ChangePercent { get; set; }
        public double month1ChangePercent { get; set; }
        public double day5ChangePercent { get; set; }
        public double day30ChangePercent { get; set; }
    }
}