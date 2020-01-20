using IBApi;

namespace IB
{
    public class SpecContract : Contract
    {
        public double Bid { get; set; }
        public double Ask { get; set; }

        public int TickerId { get; set; }
        public int TickerOptionId { get; set; }
    }
}
