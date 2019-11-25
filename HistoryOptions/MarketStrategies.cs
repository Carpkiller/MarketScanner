using System.Collections.Generic;
using System.Linq;
using MarketScanner.Types;

namespace HistoryOptions
{
    public static class MarketStrategies
    {
        public static decimal GetHodnotaOptionPutBuy(List<OptionMatrixRow> optionData, double strike)
        {
            decimal result = 0;
            // ask
            result = ((optionData.Single(x => x.Strike == strike).Put.Ask + optionData.Single(x => x.Strike == strike).Put.Bid) / 2);

            return result;
        }

        public static decimal GetHodnotaOptionPutSell(List<OptionMatrixRow> optionData, double strike)
        {
            decimal result = 0;
            // ask
            result = ((optionData.Single(x => x.Strike == strike).Put.Ask + optionData.Single(x => x.Strike == strike).Put.Bid) / 2);

            return result * (-1);
        }

        public static decimal GetHodnotaOptionCallBuy(List<OptionMatrixRow> optionData, double strike)
        {
            decimal result = 0;
            // ask
            result = ((optionData.Single(x => x.Strike == strike).Call.Ask + optionData.Single(x => x.Strike == strike).Call.Bid) / 2);

            return result;
        }

        public static decimal GetHodnotaOptionCallSell(List<OptionMatrixRow> optionData, double strike)
        {
            decimal result = 0;
            // ask
            result = ((optionData.Single(x => x.Strike == strike).Call.Ask + optionData.Single(x => x.Strike == strike).Call.Bid) / 2);

            return result * (-1);
        }

        public static decimal GetHodnotaStraddleBuy(List<OptionMatrixRow> optionData, double strike)
        {
            decimal result = 0;
            // ask
            result = ((optionData.Single(x => x.Strike == strike).Call.Ask + optionData.Single(x => x.Strike == strike).Call.Bid) / 2) +
                     ((optionData.Single(x => x.Strike == strike).Put.Ask + optionData.Single(x => x.Strike == strike).Put.Bid) / 2);

            return result;
        }

        public static decimal GetHodnotaStraddleSell(List<OptionMatrixRow> optionData, double strike)
        {
            decimal result = 0;
            // bid
            result = ((optionData.Single(x => x.Strike == strike).Call.Ask + optionData.Single(x => x.Strike == strike).Call.Bid) / 2) +
                     ((optionData.Single(x => x.Strike == strike).Put.Ask + optionData.Single(x => x.Strike == strike).Put.Bid) / 2);

            return result * (-1);
        }
    }
}
