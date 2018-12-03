using System;
using System.Collections.Generic;
using System.Linq;
using MarketScanner.Types;

namespace HistoryOptions
{
    public class Core
    {
        public Core()
        {
        }

        public string PocitajStrategiu1(List<Option> optionData)
        {
            string result = "";
            
            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();

            foreach (var obchDen in obchodneDni)
            {   
                var expirations = optionData.Where(x => x.QuoteDate >= obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                var najblizsiaExpiracia = optionData.Where(x => x.ExpirationDate >= obchDen.AddDays(10)).Min(x => x.ExpirationDate);
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[2]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data);
                var atmRow = optionMatrix[deltaStrike];

                if (!obchody.Any())
                {
                    var hodnota = GetHodnotaStraddleBuy(optionMatrix, atmRow.Strike);
                    var obchod = new Trade()
                    {
                        OpenDate = obchDen,
                        Strike = atmRow.Strike,
                        OpenPrice = hodnota,
                        Contract = atmRow.ExpirationDate.ToShortDateString(),
                        OpenStockPrice = atmRow.StockPrice,
                        ExpirationDate = atmRow.ExpirationDate
                    };

                    obchody.Add(obchod);
                }
                else if (obchody.Last().CloseDate == null)
                {
                    var obchod = obchody.Last();
                    var obchodOptionMatrix = GetOptionMatrix(optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchod.ExpirationDate).ToList());
                    var hodnota = GetHodnotaStraddleSell(obchodOptionMatrix, obchod.Strike);

                    if (Math.Abs((obchod.OpenPrice * 100) + (hodnota*100)) > obchod.OpenPrice * 100*0.1)
                    {
                        obchod.CloseDate = obchDen;
                        obchod.ClosePrice = hodnota;
                        obchod.CloseStockPrice = atmRow.StockPrice;

                        hodnota = GetHodnotaStraddleBuy(optionMatrix, atmRow.Strike);
                        var obchodNew = new Trade()
                        {
                            OpenDate = obchDen,
                            Strike = atmRow.Strike,
                            OpenPrice = hodnota,
                            Contract = atmRow.ExpirationDate.ToShortDateString(),
                            OpenStockPrice = atmRow.StockPrice,
                            ExpirationDate = atmRow.ExpirationDate
                        };

                        obchody.Add(obchodNew);
                    }
                }
            }

            result = Statistics.ShowTrades(obchody);
            result += Environment.NewLine;
            result += Statistics.ShowTotalStatistic(obchody);
            result += Environment.NewLine;
            result += Environment.NewLine;
            result += Statistics.MonthlyResults(obchody);

            return result;
        }

        private int GetDeltaStrike(List<Option> optionMatrix)
        {
            double previousDelta = 1;
            int index = 0;
            int i = 0;

            foreach (var option in optionMatrix)
            {
                if (Math.Abs(option.Delta - 0.5) < previousDelta)
                {
                    index = i;
                    previousDelta = Math.Abs(option.Delta - 0.5);
                }

                i++;
            }

            return index;
        }

        private double GetHodnotaStraddleBuy(List<OptionMatrixRow> optionData, double strike)
        {
            double result = 0;
            // ask
            result = ((optionData.Single(x => x.Strike == strike).Call.Ask + optionData.Single(x => x.Strike == strike).Call.Bid) / 2) +
                     ((optionData.Single(x => x.Strike == strike).Put.Ask + optionData.Single(x => x.Strike == strike).Put.Bid) / 2);

            return result;
        }

        private double GetHodnotaStraddleSell(List<OptionMatrixRow> optionData, double strike)
        {
            double result = 0;
            // bid
            result = ((optionData.Single(x => x.Strike == strike).Call.Ask + optionData.Single(x => x.Strike == strike).Call.Bid) / 2) +
                     ((optionData.Single(x => x.Strike == strike).Put.Ask + optionData.Single(x => x.Strike == strike).Put.Bid) / 2);

            return result*(-1);
        }

        private List<OptionMatrixRow> GetOptionMatrix(List<Option> data)
        {
            var res = new List<OptionMatrixRow>();
            foreach (var option in data.Where(x => x.Optiontype.ToUpper() == "CALL"))
            {
                OptionMatrixRow row = new OptionMatrixRow()
                {
                    QuoteDate = option.QuoteDate,
                    ExpirationDate = option.ExpirationDate,
                    Optionsymbol = option.Optionsymbol,
                    StockPrice = option.StockPrice,
                    Call = new Option
                    {
                        OpenInterest = option.OpenInterest,
                        Volume = option.Volume,
                        Delta = option.Delta,
                        Bid = option.Bid,
                        Ask = option.Ask,
                    },
                    Strike = option.Strike,
                    Put = new Option
                    {
                        Bid =
                            data.Single(
                                x =>
                                    x.QuoteDate == option.QuoteDate && x.Strike == option.Strike &&
                                    x.Optiontype.ToUpper() == "PUT").Bid,
                        Ask =
                            data.Single(
                                x =>
                                    x.QuoteDate == option.QuoteDate && x.Strike == option.Strike &&
                                    x.Optiontype.ToUpper() == "PUT").Ask,
                        Delta =
                            data.Single(
                                x =>
                                    x.QuoteDate == option.QuoteDate && x.Strike == option.Strike &&
                                    x.Optiontype.ToUpper() == "PUT").Delta,
                        Volume =
                            data.Single(
                                x =>
                                    x.QuoteDate == option.QuoteDate && x.Strike == option.Strike &&
                                    x.Optiontype.ToUpper() == "PUT").Volume,
                        OpenInterest =
                            data.Single(
                                x =>
                                    x.QuoteDate == option.QuoteDate && x.Strike == option.Strike &&
                                    x.Optiontype.ToUpper() == "PUT").OpenInterest
                    }
                };

                res.Add(row);
            }

            return res;
        }
    }
}
