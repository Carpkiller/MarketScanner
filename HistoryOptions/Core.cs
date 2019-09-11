using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
                //var najblizsiaExpiracia = optionData.Where(x => x.ExpirationDate >= obchDen.AddDays(10)).Min(x => x.ExpirationDate);
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[1]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data);
                var atmRow = optionMatrix[deltaStrike];

                if (!obchody.Any())
                {
                    var hodnota = MarketStrategies.GetHodnotaStraddleBuy(optionMatrix, atmRow.Strike);
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
                    var hodnota = MarketStrategies.GetHodnotaStraddleSell(obchodOptionMatrix, obchod.Strike);

                    if (Math.Abs((obchod.OpenPrice * 100) + (hodnota*100)) > obchod.OpenPrice * 100*0.1)
                    {
                        obchod.CloseDate = obchDen;
                        obchod.ClosePrice = hodnota;
                        obchod.CloseStockPrice = atmRow.StockPrice;

                        hodnota = MarketStrategies.GetHodnotaStraddleBuy(optionMatrix, atmRow.Strike);
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

        private int GetDeltaStrike(List<Option> optionMatrix, double hodota = 0.5)
        {
            double previousDelta = 1;
            int index = 0;
            int i = 0;

            foreach (var option in optionMatrix.Where(x => x.Optiontype.ToUpper() == "CALL"))
            {
                //Console.WriteLine(Math.Abs(option.Delta));
                if (Math.Abs(Math.Abs(option.Delta) - hodota) < previousDelta)
                {
                    index = i;
                    previousDelta = Math.Abs(Math.Abs(option.Delta) - hodota);
                }

                i++;
            }

            return index;
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

        // vo stvrtok predat ATM straddle a v piatok obchod ukoncit
        public string PocitajStrategiu2(List<Option> optionData)
        {
            string result = "";
            var vixHodnoty = LoadVixHistoryStockPrice();

            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();

            for (int i = 0; i < obchodneDni.Count; i++)
            {
                var obchDen = obchodneDni[i];
                var expirations = optionData.Where(x => x.QuoteDate >= obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[0]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data);
                var atmRow = optionMatrix[deltaStrike];

                if (obchDen.DayOfWeek == DayOfWeek.Thursday && obchodneDni[i+1].DayOfWeek == DayOfWeek.Friday 
                 //   && vixHodnoty.Single(x => x.date == obchDen.ToString("yyyy-MM-dd")).open < 25
                    )
                {
                    var hodnota = MarketStrategies.GetHodnotaStraddleSell(optionMatrix, atmRow.Strike);
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
                else if (obchody.Any() && obchDen.DayOfWeek == DayOfWeek.Friday && obchody.Last().CloseDate == null)
                {
                    var obchod = obchody.Last();
                    // var obchodOptionMatrix = GetOptionMatrix(optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchod.ExpirationDate).ToList());
                    var hodnota = MarketStrategies.GetHodnotaStraddleBuy(optionMatrix, obchod.Strike);

                    obchod.CloseDate = obchDen;
                    obchod.ClosePrice = hodnota;
                    obchod.CloseStockPrice = atmRow.StockPrice;
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

        public string PocitajStrategiu3(List<Option> optionData)
        {
            string result = "";
            var vixHodnoty = LoadVixHistoryStockPrice();

            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();

            for (int i = 0; i < obchodneDni.Count; i++)
            {
                var obchDen = obchodneDni[i];
                var expirations = optionData.Where(x => x.QuoteDate >= obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[1]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data);
                var atmRow = optionMatrix[deltaStrike];

                if (obchDen.DayOfWeek == DayOfWeek.Friday 
                    //&& vixHodnoty.Single(x => x.date == obchDen.ToString("yyyy-MM-dd")).open < 13
                    )
                {
                    var hodnota = MarketStrategies.GetHodnotaStraddleSell(optionMatrix, atmRow.Strike);
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
                else if (obchody.Any() && obchody.Last().CloseDate == null)
                {
                    var obchod = obchody.Last();
                    var obchodOptionMatrix = GetOptionMatrix(optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchod.ExpirationDate).ToList());
                    var hodnota = MarketStrategies.GetHodnotaStraddleBuy(obchodOptionMatrix, obchod.Strike);

                    obchod.CloseDate = obchDen;
                    obchod.ClosePrice = hodnota;
                    obchod.CloseStockPrice = atmRow.StockPrice;
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

        public List<HistoryStockPrice> LoadVixHistoryStockPrice()
        {
            var result = new List<HistoryStockPrice>();

            var data = File.ReadAllLines("VIX.txt");
            foreach (var row in data.Skip(1))
            {
                var record = row.Split(',');
                var day = new HistoryStockPrice()
                {

                    date = record[0],
                    open = Convert.ToDouble(record[1], new CultureInfo("en-US")),
                    high = Convert.ToDouble(record[2], new CultureInfo("en-US")),
                    low = Convert.ToDouble(record[3], new CultureInfo("en-US")),
                    close = Convert.ToDouble(record[4], new CultureInfo("en-US"))
                };

                result.Add(day);
            }

            return result;
        }

        public string PocitajStrategiu4(List<Option> optionData)
        {
            string result = "";
            var vixHodnoty = LoadVixHistoryStockPrice();
            int VixMax = 14;

            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();

            foreach (var obchDen in obchodneDni)
            {
                var expirations = optionData.Where(x => x.QuoteDate >= obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[0]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data);
                var atmRow = optionMatrix[deltaStrike];

                if (!obchody.Any() || obchody.Last().CloseDate != null
                    && vixHodnoty.Single(x => x.date == obchDen.ToString("yyyy-MM-dd")).open < VixMax
                    )
                {
                    var hodnota = MarketStrategies.GetHodnotaStraddleSell(optionMatrix, atmRow.Strike);
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
                    if (obchDen.Date.ToShortDateString() == new DateTime(2018, 08, 20).ToShortDateString())
                    {
                        
                    }
                    var obchod = obchody.Last();
                    var obchodOptionMatrix = GetOptionMatrix(optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchod.ExpirationDate).ToList());
                    var hodnota = MarketStrategies.GetHodnotaStraddleBuy(obchodOptionMatrix, obchod.Strike);

                    if (Math.Abs((obchod.OpenPrice * 100) + (hodnota * 100)) > obchod.OpenPrice * 100 * 0.1 
                        || IsLastTradingDayOfContract(optionData, obchod.ExpirationDate, obchDen))
                    {
                        obchod.CloseDate = obchDen;
                        obchod.ClosePrice = hodnota;
                        obchod.CloseStockPrice = atmRow.StockPrice;

                        if (obchDen.DayOfWeek != DayOfWeek.Friday
                            && !IsLastTradingDayOfContract(optionData, obchod.ExpirationDate, obchDen)
                          //  && vixHodnoty.Single(x => x.date == obchDen.ToString("yyyy-MM-dd")).open < VixMax
                            )
                        {
                            hodnota = MarketStrategies.GetHodnotaStraddleSell(optionMatrix, atmRow.Strike);
                            var obchodNew = new Trade
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
                        else
                        {
                            
                        }
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

        private bool IsLastTradingDayOfContract(List<Option> optionData, DateTime expirationDate, DateTime obchDen)
        {
            if (obchDen.Date.ToShortDateString() == new DateTime(2018, 08, 17,0,0,0).ToShortDateString())
            {

            }
            var lastTradingDate = optionData.Where(x => x.ExpirationDate == expirationDate).Select(x => x.QuoteDate).Max();

            return lastTradingDate == obchDen;
        }

        public string PocitajStrategiu5(List<Option> optionData)
        {
            string result = "";
            var vixHodnoty = LoadVixHistoryStockPrice();
            int VixMax = 25;

            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();
            DateTime? predDen = null;

            foreach (var obchDen in obchodneDni)
            {
                var expirations = optionData.Where(x => x.QuoteDate == obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[6]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data.Where(x =>  x.Optiontype.ToUpper() == "PUT").ToList(), 0.25);
                var atmRow = optionMatrix[deltaStrike];

                if (!obchody.Any() || obchody.Last().CloseDate != null
                 //   && RozdielCeny(obchDen, predDen, optionData)
                    && vixHodnoty.Single(x => x.date == obchDen.ToString("yyyy-MM-dd")).open < VixMax
                    )
                {
                    var hodnota = MarketStrategies.GetHodnotaOptionPutSell(optionMatrix, atmRow.Strike);
                    var obchod = new Trade
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
                    var hodnota = MarketStrategies.GetHodnotaOptionPutBuy(obchodOptionMatrix, obchod.Strike);

                    if (Math.Abs((obchod.OpenPrice * 100) + (hodnota * 100)) > obchod.OpenPrice * 100 * 0.5*(-1))
                    {
                        obchod.CloseDate = obchDen;
                        obchod.ClosePrice = hodnota;
                        obchod.CloseStockPrice = atmRow.StockPrice;

                        if (
                            //RozdielCeny(obchDen, predDen, optionData)
                            //&& 
                            vixHodnoty.Single(x => x.date == obchDen.ToString("yyyy-MM-dd")).open < VixMax)
                        {
                            hodnota = MarketStrategies.GetHodnotaOptionPutSell(optionMatrix, atmRow.Strike);
                            var obchodNew = new Trade
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

                predDen = obchDen;
            }

            result = Statistics.ShowTrades(obchody);
            result += Environment.NewLine;
            result += Statistics.ShowTotalStatistic(obchody);
            result += Environment.NewLine;
            result += Environment.NewLine;
            result += Statistics.MonthlyResults(obchody);

            return result;
        }

        private bool RozdielCeny(DateTime obchDen, DateTime? predchDen, List<Option> optionData)
        {
            if (predchDen == null)
            {
                return false;
            }

            var predDen = optionData.Where(x => x.QuoteDate == predchDen).Select(x => x.StockPrice).Distinct().Single();

            var sucDen = optionData.Where(x => x.QuoteDate == obchDen).Select(x => x.StockPrice).Distinct().Single();

            if (Math.Abs((predDen - sucDen)/ sucDen) > 0.015)
            {
                return true;
            }

            return false;
        }

        public string PocitajStrategiuBuyCallAtm(List<Option> optionData)
        {
            string result = "";
            int VixMax = 25;

            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();
            DateTime? predDen = null;

            foreach (var obchDen in obchodneDni)
            {
                var expirations = optionData.Where(x => x.QuoteDate == obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[2]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data.Where(x => x.Optiontype.ToUpper() == "CALL").ToList());
                var atmRow = optionMatrix[deltaStrike];

                if (!obchody.Any() || obchody.Last().CloseDate != null)
                {
                    var hodnota = MarketStrategies.GetHodnotaOptionCallBuy(optionMatrix, atmRow.Strike);
                    var obchod = new Trade
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
                    var obchodOptionMatrix =
                        GetOptionMatrix(
                            optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchod.ExpirationDate)
                                .ToList());
                    var hodnota = MarketStrategies.GetHodnotaOptionCallSell(obchodOptionMatrix, obchod.Strike);

                    if (Math.Abs((hodnota*100)) < obchod.OpenPrice*100*0.5 ||
                        Math.Abs((hodnota * 100)) > obchod.OpenPrice * 100 * 1.5)
                    {
                        obchod.CloseDate = obchDen;
                        obchod.ClosePrice = hodnota;
                        obchod.CloseStockPrice = atmRow.StockPrice;

                        hodnota = MarketStrategies.GetHodnotaOptionCallBuy(optionMatrix, atmRow.Strike);
                        var obchodNew = new Trade
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

        public string PocitajStrategiuBuyCallOtm3(List<Option> optionData)
        {
            string result = "";
            int VixMax = 25;

            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();
            DateTime? predDen = null;

            foreach (var obchDen in obchodneDni)
            {
                var expirations = optionData.Where(x => x.QuoteDate == obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[2]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data.Where(x => x.Optiontype.ToUpper() == "CALL").ToList());
                var atmRow = optionMatrix[deltaStrike + 2];

                if (!obchody.Any() || obchody.Last().CloseDate != null)
                {
                    var hodnota = MarketStrategies.GetHodnotaOptionCallBuy(optionMatrix, atmRow.Strike);
                    var obchod = new Trade
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
                    var obchodOptionMatrix =
                        GetOptionMatrix(
                            optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchod.ExpirationDate)
                                .ToList());
                    var hodnota = MarketStrategies.GetHodnotaOptionCallSell(obchodOptionMatrix, obchod.Strike);

                    if (Math.Abs((hodnota * 100)) < obchod.OpenPrice * 100 * 0.3 ||
                        Math.Abs((hodnota * 100)) > obchod.OpenPrice * 100 * 1.5)
                    {
                        obchod.CloseDate = obchDen;
                        obchod.ClosePrice = hodnota;
                        obchod.CloseStockPrice = atmRow.StockPrice;

                        hodnota = MarketStrategies.GetHodnotaOptionCallBuy(optionMatrix, atmRow.Strike);
                        var obchodNew = new Trade
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

        public string PocitajStrategiuBuyCallAtm3(List<Option> optionData)
        {
            string result = "";
            int VixMax = 25;

            var obchodneDni = optionData.Select(x => x.QuoteDate).Distinct().ToList();
            var obchody = new List<Trade>();
            DateTime? predDen = null;

            foreach (var obchDen in obchodneDni)
            {
                var expirations = optionData.Where(x => x.QuoteDate == obchDen).Select(x => x.ExpirationDate).Distinct().OrderBy(x => x.Date).ToList();
                var data = optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == expirations[2]).ToList();
                var optionMatrix = GetOptionMatrix(data);

                var deltaStrike = GetDeltaStrike(data.Where(x => x.Optiontype.ToUpper() == "CALL").ToList());
                var atmRow = optionMatrix[deltaStrike - 2];

                if (!obchody.Any() || obchody.Last().CloseDate != null)
                {
                    var hodnota = MarketStrategies.GetHodnotaOptionCallBuy(optionMatrix, atmRow.Strike);
                    var obchod = new Trade
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
                else if (obchody.Last().CloseDate == null || obchDen.AddDays(3) >= obchody.Last().ExpirationDate)
                {
                    var obchod = obchody.Last();
                    var obchodOptionMatrix =
                        GetOptionMatrix(
                            optionData.Where(x => x.QuoteDate == obchDen && x.ExpirationDate == obchod.ExpirationDate)
                                .ToList());
                    var hodnota = MarketStrategies.GetHodnotaOptionCallSell(obchodOptionMatrix, obchod.Strike);

                    if (Math.Abs((hodnota * 100)) < obchod.OpenPrice * 100 * 0.3 ||
                        Math.Abs((hodnota * 100)) > obchod.OpenPrice * 100 * 1.5 || 
                        obchDen.AddDays(3) >= obchody.Last().ExpirationDate
                        )
                    {
                        obchod.CloseDate = obchDen;
                        obchod.ClosePrice = hodnota;
                        obchod.CloseStockPrice = atmRow.StockPrice;

                        hodnota = MarketStrategies.GetHodnotaOptionCallBuy(optionMatrix, atmRow.Strike);
                        var obchodNew = new Trade
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

                Console.WriteLine(obchDen.AddDays(4) >= obchody.Last().ExpirationDate);
            }

            result = Statistics.ShowTrades(obchody);
            result += Environment.NewLine;
            result += Statistics.ShowTotalStatistic(obchody);
            result += Environment.NewLine;
            result += Environment.NewLine;
            result += Statistics.MonthlyResults(obchody);

            return result;
        }
    }
}
