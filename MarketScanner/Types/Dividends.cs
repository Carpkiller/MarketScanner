using System.Collections.Generic;

namespace MarketScanner.Types
{
    public class Dividend
    {
        public string exDate { get; set; }
        public string paymentDate { get; set; }
        public string recordDate { get; set; }
        public string declaredDate { get; set; }
        public double? amount { get; set; }
        public string flag { get; set; }
        public string type { get; set; }
        public string qualified { get; set; }
        public string indicated { get; set; }
    }

    public class DividendovyReport
    {
        public Quote Quote { get; set; }
        public Stats Stats { get; set; }
        public IList<Dividend> Dividends { get; set; }
    }
}
