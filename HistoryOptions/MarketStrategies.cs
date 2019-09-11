using System.Collections.Generic;
using System.Linq;
using MarketScanner.Types;

namespace HistoryOptions
{
    public static class MarketStrategies
    {
        public static double GetHodnotaOptionPutBuy(List<OptionMatrixRow> optionData, double strike)
        {
            double result = 0;
            // ask
            result = ((optionData.Single(x => x.Strike == strike).Put.Ask + optionData.Single(x => x.Strike == strike).Put.Bid) / 2);

            return result;
        }

        public static double GetHodnotaOptionPutSell(List<OptionMatrixRow> optionData, double strike)
        {
            double result = 0;
            // ask
            result = ((optionData.Single(x => x.Strike == strike).Put.Ask + optionData.Single(x => x.Strike == strike).Put.Bid) / 2);

            return result * (-1);
        }

        public static double GetHodnotaOptionCallBuy(List<OptionMatrixRow> optionData, double strike)
        {
            double result = 0;
            // ask
            result = ((optionData.Single(x => x.Strike == strike).Call.Ask + optionData.Single(x => x.Strike == strike).Call.Bid) / 2);

            return result;
        }

        public static double GetHodnotaOptionCallSell(List<OptionMatrixRow> optionData, double strike)
        {
            double result = 0;
            // ask
            result = ((optionData.Single(x => x.Strike == strike).Call.Ask + optionData.Single(x => x.Strike == strike).Call.Bid) / 2);

            return result * (-1);
        }

        public static double GetHodnotaStraddleBuy(List<OptionMatrixRow> optionData, double strike)
        {
            double result = 0;
            // ask
            result = ((optionData.Single(x => x.Strike == strike).Call.Ask + optionData.Single(x => x.Strike == strike).Call.Bid) / 2) +
                     ((optionData.Single(x => x.Strike == strike).Put.Ask + optionData.Single(x => x.Strike == strike).Put.Bid) / 2);

            return result;
        }

        public static double GetHodnotaStraddleSell(List<OptionMatrixRow> optionData, double strike)
        {
            double result = 0;
            // bid
            result = ((optionData.Single(x => x.Strike == strike).Call.Ask + optionData.Single(x => x.Strike == strike).Call.Bid) / 2) +
                     ((optionData.Single(x => x.Strike == strike).Put.Ask + optionData.Single(x => x.Strike == strike).Put.Bid) / 2);

            return result * (-1);
        }
    }
}
