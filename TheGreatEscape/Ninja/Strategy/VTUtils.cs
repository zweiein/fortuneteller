#region Using declarations
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Indicator;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Strategy;

#endregion

namespace valutatrader
{
    public class VTUtils
    {

        #region UtilityFunctions
        
        public static bool RisingByValue(DataSeries series, double value)
        {
            if (series[0] > (series[1] + value))
                return true;
            return false;
        }

        public static bool FallingByValue(DataSeries series, double value)
        {
            if (series[0] < (series[1] - value))
                return true;
            return false;
        }
       public static  bool BarsInDirection(StrategyBase strategy, bool positive, int count)
        {
            bool flag = true;
            for (int i = 0; i < count; i++)
            {
                int add = 0;
                if (strategy.CalculateOnBarClose == false)
                    add = 1;
                if (positive)
                {
                    if (strategy.Close[i+add] < strategy.Open[i+add])
                        return false;
                }
                else
                {
                    if (strategy.Close[i+add] > strategy.Open[i+add])
                        return false;
                }
            }
            return true;
        }
        public static bool StrictlyRising(DataSeries series, int count)
        {

            bool flag = true;
            for (int i = 1; i < count; i++)
            {
                if (series[i] <= series[i - 1])
                    flag = false;
            }
            return flag;
        }
        public static bool StrictlyFalling(DataSeries series, int count)
        {

            bool flag = true;
            for (int i = 1; i < count; i++)
            {
                if (series[i] >= series[i - 1])
                    flag = false;
            }
            return flag;
        }
        public static bool ContainsValues(DataSeries series, int count)
        {
            bool flag = true;
            for (int i = 0; i < count; i++)
            {
                if (series.ContainsValue(i) == false)
                    flag = false;
            }
            return flag;
        }
        public static int CheckZeroCross(DataSeries series, int count)
        {
            if (series.Count <= count) return 0;
            if (series[0] > 0 && series[(count - 1)] < 0)
                return 1;
            if (series[0] < 0 && series[(count - 1)] > 0)
                return -1;
            return 0;
        }
        public static  int TicksProfit(StrategyBase strategy, double price) {
            double ppoints = strategy.Position.GetProfitLoss(price, PerformanceUnit.Points);
            double tickProfit = ppoints / strategy.TickSize;
            return Convert.ToInt32(tickProfit);
        }
		public static bool IsTradingAllowed(StrategyBase strategy,int sessionType){
			bool tradingAllowed=false;
			int hour=strategy.Time[0].Hour;
			switch (sessionType)   {
				case 0:
					tradingAllowed=true;
					break;
				case 1:
					if (hour>15 && hour<23)
						tradingAllowed=true;
					break;	
				case 2:
					if (hour>14 && hour<23)
						tradingAllowed=true;
					break;
				case 3:
					if (hour>13 && hour<23)
						tradingAllowed=true;
					break;	
				case 4:
					if (hour>13 && hour<17)
						tradingAllowed=true;
					break;	
			}
			return tradingAllowed;
		}
        #endregion
    }
}
