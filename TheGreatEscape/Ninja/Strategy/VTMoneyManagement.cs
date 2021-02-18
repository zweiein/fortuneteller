#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Indicator;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Strategy;

// For sending screen dumps on email
using System.Windows.Forms;
using System.Net.Mail;
using System.Net.Mime;
using System.Drawing.Imaging;
#endregion

namespace valutatrader
{
	class VTMoneyManagement{
		VTStrategyBase strategy = null;
		double dailyMaxLos=0;
		double priorTradesCumProfit=0;

        public VTMoneyManagement(VTStrategyBase _strategy, double _maxLoss)
        {
            strategy = _strategy;
            dailyMaxLos = _maxLoss;
        }

        public bool checkAllowance(){

				// At the start of a new session
			if (strategy.Bars.FirstBarOfSession && strategy.FirstTickOfBar)
			{
				priorTradesCumProfit = strategy.Performance.AllTrades.TradesPerformance.Currency.CumProfit;
			}

			if ((strategy.Performance.AllTrades.TradesPerformance.Currency.CumProfit - priorTradesCumProfit) <= -dailyMaxLos)
			{	
				
				if (MarketPosition.Flat!=strategy.getOrderManager().GetMarketPosition(0))
					strategy.getOrderManager().ExitMarket(0);
				return false;
			}
			return true;
        }

	}
}