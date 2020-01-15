using System;
using System.Data;

using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;

namespace GetYahooStockData
{
    class Program
    {
        static void Main(string[] args)
        {
            string sSystem ="";
            Console.Write("SYSTEM? ");
            sSystem = Console.ReadLine();
            if (sSystem == "Y") { RunYahoo(); }
            if (sSystem == "A") { RunAlphaVantage(); }

         }

        static void RunAlphaVantage() 
        {
            var url = "https://www.alphavantage.co/query?function=TIME_SERIES_DAILY_ADJUSTED&symbol={0}&apikey=6MOYHXRVWMHXK34I";
            string sSymbol;
            Console.Write("Symbol: ");
            sSymbol = Console.ReadLine();
            Console.Write("How many days of price average? ");
            int iDaysAverage = 5;
            try
            {
                iDaysAverage = Convert.ToInt32(Console.ReadLine());
            }
            catch { iDaysAverage = 5; }
            url = url.Replace("{0}", sSymbol);

            //Console.WriteLine(url);  
            var httpClient = new HttpClient();
            var html = httpClient.GetStringAsync(url);
            string sStart = "\"Time Series (Daily)\": ";
            int iStartPos = html.Result.IndexOf(sStart) - 1; 
            string jsonData = html.Result.Substring(iStartPos + sStart.Length );
            jsonData = jsonData.Substring(0, jsonData.Length - 1);
            jsonData = jsonData.Replace("1. open", "open");
            jsonData = jsonData.Replace("2. high", "high");
            jsonData = jsonData.Replace("3. low", "low");
            jsonData = jsonData.Replace("4. close", "close");
            jsonData = jsonData.Replace("5. adjusted close", "adjclose");
            jsonData = jsonData.Replace("6. volume", "volume");
            jsonData = jsonData.Replace(": {", ",");
            jsonData = jsonData.Insert(2, "\"prices\":[{\"day\":");
            jsonData = jsonData.Replace("},", "},{\"day\":");
            jsonData = jsonData.Insert(jsonData.Length - 2, "]");       
            // Jason Array 
            var DayPrices = JsonConvert.DeserializeObject<Dictionary<string, List<Prices>>>(jsonData);
            var ListDayPrice = DayPrices["prices"];
            
            for (var I = 0; I < iDaysAverage * 2; I++)
            {
                try
                {
                    Prices DP = ListDayPrice[I];
                    if (DP.open == null && DP.high == null && DP.low == null && DP.close == null)
                    {
                        ListDayPrice.RemoveAt(I);
                        I -= 1;
                        //Console.WriteLine("JArray *ITEM DELETED ");
                    }
                    else
                    {
                        DP.DayRange = Convert.ToDouble(DP.high) - Convert.ToDouble(DP.low);
                        DP.LowOpen = Convert.ToDouble(DP.open) - Convert.ToDouble(DP.low);
                        DP.LowClose = Convert.ToDouble(DP.close) - Convert.ToDouble(DP.low);
                        DP.HighOpen = Convert.ToDouble(DP.high) - Convert.ToDouble(DP.open);
                        DP.HighClose = Convert.ToDouble(DP.high) - Convert.ToDouble(DP.close);
                        DP.CloseOpen = Convert.ToDouble(DP.close) - Convert.ToDouble(DP.open);
                        DP.gap = 0;
                        try
                        {
                            DP.gap = Convert.ToDouble(DP.open) - Convert.ToDouble(ListDayPrice[I + 1].close);
                        }
                        catch { }
                    }
                }
                catch { }
            }

            //Calculate average from bottom up 
            for (var ii = iDaysAverage * 2; ii >= 0; ii--)
            {
                double sumGAP = ListDayPrice[ii].gap;
                double sumLowOpen = ListDayPrice[ii].LowOpen;
                double sumDayRange = ListDayPrice[ii].DayRange;
                int j = 1;
                while (j < iDaysAverage)
                {
                    sumGAP += ListDayPrice[ii + j].gap;
                    sumDayRange += ListDayPrice[ii + j].DayRange;
                    sumLowOpen += ListDayPrice[ii + j].LowOpen;
                    j++;
                }
                ListDayPrice[ii].AverageGap = sumGAP / iDaysAverage;
                ListDayPrice[ii].AverageDayRange = sumDayRange / iDaysAverage;
                ListDayPrice[ii].AverageLowOpen = sumLowOpen / iDaysAverage;
            }
            // Display Suggested High & Low
            double sLow = Convert.ToDouble(ListDayPrice[0].open) - ListDayPrice[0].AverageGap;
            double sHigh = sLow + ListDayPrice[1].AverageDayRange;
            Console.WriteLine();
            Console.WriteLine("Estimated today's trading range from {0:C} ~ {1:C}", sLow, sHigh);
            Console.WriteLine();

            // Display Array
            for (var I = 0; I < iDaysAverage; I++)
            {
                Prices DP = ListDayPrice[I];
                Console.WriteLine("{0}: {1:C}; {2:C}; {3:C};{4:C}; {5:C}; {6:C}; {7:C}; {8:C};{9:C}; {10:C}; ", UnixTimeStampToDateTime(DP.Date).ToShortDateString(), DP.open, DP.high, DP.low, DP.close, DP.gap, DP.AverageGap, DP.LowOpen, DP.AverageLowOpen, DP.DayRange, DP.AverageDayRange);
            }
        }



        static void RunYahoo() 
        {
            var url = "https://finance.yahoo.com/quote/{0}/history";
            string sSymbol;
            Console.Write("Symbol: "); 
            sSymbol = Console.ReadLine();
            Console.Write("How many days of price average? ");
            int iDaysAverage = 5;  
            try { iDaysAverage = Convert.ToInt32(Console.ReadLine()); 
            }  catch { iDaysAverage = 5; }
            url = url.Replace("{0}", sSymbol);

            //Console.WriteLine(url);  
            var httpClient = new HttpClient();
            var html = httpClient.GetStringAsync(url);

            //string strBegin = "PriceStore\":{\"prices\":";
            string strBegin = "PriceStore\":";
            string strEnd = ",\"isPending\"";
   
            var iBegin = html.Result.IndexOf(strBegin);
            var iEnd = html.Result.IndexOf(strEnd);
                     
            var jsonData  = html.Result.Substring(iBegin+strBegin.Length, iEnd-iBegin-strBegin.Length) + "}";
        
            // Jason Array 
            var DayPrices = JsonConvert.DeserializeObject<Dictionary<string, List<Prices>>>(jsonData);
            var ListDayPrice = DayPrices["prices"];
            
            for (var I =0; I < iDaysAverage * 2 ; I++)
            {   try
                {
                    Prices DP = ListDayPrice[I];
                    if (DP.open == null && DP.high == null && DP.low == null && DP.close == null)
                    {
                        ListDayPrice.RemoveAt(I);
                        I -= 1;
                        //Console.WriteLine("JArray *ITEM DELETED ");
                    }
                    else
                    {                               
                        DP.DayRange = Convert.ToDouble(DP.high) - Convert.ToDouble(DP.low);
                        DP.LowOpen = Convert.ToDouble(DP.open) - Convert.ToDouble(DP.low);
                        DP.LowClose = Convert.ToDouble(DP.close) - Convert.ToDouble(DP.low); 
                        DP.HighOpen = Convert.ToDouble(DP.high) - Convert.ToDouble(DP.open);
                        DP.HighClose = Convert.ToDouble(DP.high) - Convert.ToDouble(DP.close); 
                        DP.CloseOpen = Convert.ToDouble(DP.close) - Convert.ToDouble(DP.open);
                        DP.gap = 0;
                        try {
                            DP.gap = Convert.ToDouble(DP.open) - Convert.ToDouble(ListDayPrice[I + 1].close);
                        }
                        catch {}
                    }
                }
                catch {}
            }

            //Calculate average from bottom up 
            for (var ii = iDaysAverage * 2 ; ii >= 0 ; ii--)
            {   double sumGAP = ListDayPrice[ii].gap;
                double sumLowOpen = ListDayPrice[ii].LowOpen;
                double sumDayRange = ListDayPrice[ii].DayRange;
                int j = 1;
                while (j < iDaysAverage)
                {                   
                    sumGAP += ListDayPrice[ii + j].gap;
                    sumDayRange += ListDayPrice[ii + j].DayRange;
                    sumLowOpen +=  ListDayPrice[ii + j].LowOpen;
                    j++;
                }                
                ListDayPrice[ii].AverageGap = sumGAP / iDaysAverage;
                ListDayPrice[ii].AverageDayRange = sumDayRange / iDaysAverage;
                ListDayPrice[ii].AverageLowOpen = sumLowOpen / iDaysAverage;
            }
            // Display Suggested High & Low
            double sLow = Convert.ToDouble(ListDayPrice[0].open) - ListDayPrice[0].AverageGap; 
            double sHigh = sLow + ListDayPrice[1].AverageDayRange;
            Console.WriteLine();
            Console.WriteLine("Estimated today's trading range from {0:C} ~ {1:C}", sLow, sHigh);
            Console.WriteLine();
                                 
            // Display Array
            for (var I = 0; I < iDaysAverage; I++)
            {   Prices DP = ListDayPrice[I];
                Console.WriteLine("{0}: {1:C}; {2:C}; {3:C};{4:C}; {5:C}; {6:C}; {7:C}; {8:C};{9:C}; {10:C}; ", UnixTimeStampToDateTime(DP.Date).ToShortDateString(), DP.open , DP.high, DP.low, DP.close , DP.gap, DP.AverageGap, DP.LowOpen, DP.AverageLowOpen, DP.DayRange, DP.AverageDayRange  );
            }
        }

        class Prices 
        {   public double Date { get ; set; }
            public string open { get; set; }
            public string high { get; set; }
            public string low { get; set; }
            public string close { get; set; }
            public double Average { get; set; }
            public string volume { get; set; }
            public string adjclose { get; set; }
            public double gap { get; set; }
            public double AverageGap { get; set; }
            public double LowOpen { get; set; }
            public double AverageLowOpen { get; set; }
            public double LowClose { get; set; }
            public double HighOpen { get; set; }
            public double HighClose { get; set; }         
            public double CloseOpen { get; set; }
            public double DayRange { get; set; }
            public double AverageDayRange { get; set; }
            public double EstHigh { get; set; }
            public double EstLow { get; set; }
                  
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }          
    }
}

