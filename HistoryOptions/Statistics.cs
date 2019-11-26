using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MarketScanner.Types;

namespace HistoryOptions
{
    public static class Statistics
    {
        public static string ShowTrades(List<Trade> obchody)
        {
            string result = null;

            foreach (var trade in obchody.Where(x => x.CloseDate != null))
            {
                result += $"Open date - {trade.OpenDate} , Open price - {trade.OpenPrice} , Stock price - {trade.OpenStockPrice} ,Strike {trade.Strike}, expiracia  {trade.Contract}" + Environment.NewLine;
                result +=
                    $"Close date - {trade.CloseDate} , Close Price - {trade.ClosePrice} , Stock price - {trade.CloseStockPrice} , Zisk = {trade.ClosePrice * (-1) * 100 - trade.OpenPrice * 100}" + Environment.NewLine;
            }

            return result;
        }

        public static string ShowTotalStatistic(List<Trade> obchody)
        {
            string result = null;

            decimal? ZiskoveObchody = 0;
            decimal? StratoveObchody = 0;
            decimal? vysledokObchodu = 0;

            foreach (var trade in obchody.Where(x => x.CloseDate != null))
            {
                if (trade.Contract == "AKCIE")
                {
                    vysledokObchodu = (trade.OpenPrice - trade.ClosePrice) * trade.PocetKontraktov * (-1);
                }
                else
                {
                    vysledokObchodu = trade.ClosePrice * (-1) * 100 * trade.PocetKontraktov - trade.OpenPrice * 100 * trade.PocetKontraktov;
                }

                if (vysledokObchodu > 0)
                {
                    ZiskoveObchody += vysledokObchodu;
                }
                else
                {
                    StratoveObchody += vysledokObchodu;
                }
            }

            return $"Pocet obchodov - {obchody.Count(x => x.CloseDate != null)}"+ Environment.NewLine +
                $"Pocet ziskovych obchodov {obchody.Where(x=> x.CloseDate != null).Count(x => (x.ClosePrice * (-1) * 100 - x.OpenPrice * 100) > 0)}" + Environment.NewLine +
                $"Pocet stratovych obchodov {obchody.Where(x => x.CloseDate != null).Count(x => (x.ClosePrice * (-1) * 100 - x.OpenPrice * 100) < 0)}" + Environment.NewLine +
                $"Vysledok ziskovych obchodov {ZiskoveObchody}" + Environment.NewLine +
                $"Vysledok stratovych obchodov {StratoveObchody}" + Environment.NewLine +
                $"Celkovy vysledok {ZiskoveObchody + StratoveObchody}";
        }

        public static string MonthlyResults(List<Trade> obchody)
        {
            string result = null;

            for (int i = 1; i <= 12; i++)
            {
                string monthName = new DateTime(2010, i, 1).ToString("MMM", CultureInfo.InvariantCulture);
                result += $"Mesiac - {monthName}" + Environment.NewLine +
                          $"Vysledok ziskovych obchodov {obchody.Where(x => x.CloseDate != null && x.OpenDate.Month == i).Where(x => (x.ClosePrice*(-1)*100 - x.OpenPrice*100) > 0).Sum(x => (x.ClosePrice*(-1)*100 - x.OpenPrice*100))}" +
                          Environment.NewLine +
                          $"Vysledok stratovych obchodov {obchody.Where(x => x.CloseDate != null && x.OpenDate.Month == i).Where(x => (x.ClosePrice*(-1)*100 - x.OpenPrice*100) < 0).Sum(x => (x.ClosePrice*(-1)*100 - x.OpenPrice*100))}" +
                          Environment.NewLine +
                          $"Celkovy vysledok {obchody.Where(x => x.CloseDate != null && x.OpenDate.Month == i).Sum(x => (x.ClosePrice*(-1)*100 - x.OpenPrice*100))}" +
                          Environment.NewLine;

            }
            return result;
        }
    }
}
