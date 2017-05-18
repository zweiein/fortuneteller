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
#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    /// <summary>
    /// Enter the description of your strategy here
    /// </summary>
    [Description("Enter the description of your strategy here")]
    public class Reversalia : Strategy
    {
        #region Variables
        // Wizard generated variables
        private double factor = 2.000; // Default setting for Factor
		private bool reverse=true;
		private bool trailer=true;
		private int sl=30;
        private int pt=0;
        private int maxloss = 500; 
        // User defined variables (add any user defined variables below)
		
		
		private double priorTradesCumProfit=0;
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            CalculateOnBarClose = true;
            EntriesPerDirection = 1;
            EntryHandling = EntryHandling.AllEntries;
            IncludeCommission = true;
            ExitOnClose = true;
            Enabled = true;
			if (sl>0 && trailer) this.SetTrailStop(CalculationMode.Ticks,sl);
			if (sl>0 && trailer==false) this.SetStopLoss(CalculationMode.Ticks,sl);
            if (pt>0) SetProfitTarget(CalculationMode.Ticks,pt);
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {

            if (Bars.FirstBarOfSession && FirstTickOfBar)
            {
                priorTradesCumProfit = Performance.AllTrades.TradesPerformance.Currency.CumProfit;
            }

            if (maxloss>0 && (Performance.AllTrades.TradesPerformance.Currency.CumProfit - priorTradesCumProfit) <= -maxloss)
            {           
                if (Position.MarketPosition== MarketPosition.Long) ExitLong();
				if (Position.MarketPosition== MarketPosition.Short) ExitShort();
                return;
            }            


            Trade lastTrade = Performance.AllTrades[Performance.AllTrades.Count - 1];
            double tickProfit = lastTrade.ProfitPoints / TickSize;
                    
			bool bigbar=false;
			if (Math.Abs(Close[0]-Open[0])>Math.Abs(factor*(Close[1]-Open[1]))) bigbar=true;
		
			if (Close[0]>Open[0] && bigbar) {if (reverse) EnterShort();else EnterLong();}
			if (Close[0]<Open[0] && bigbar){ if (reverse) EnterLong();else EnterShort();}
        }

        #region Properties
        [Description("")]
        [GridCategory("Parameters")]
        public int MaxLoss
        {
            get { return maxloss; }
            set { maxloss = Math.Max(0, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public double Factor
        {
            get { return factor; }
            set { factor = Math.Max(1, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public int  SL
        {
            get { return sl; }
            set { sl = Math.Max(0, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public int  ProfitTarget
        {
            get { return pt; }
            set { pt = Math.Max(0, value); }
        }
		[Description("")]
		[GridCategory("Parameters")]
		public bool UseTrailingStop
		{
			get { return trailer;}
			set { trailer=value;}
		}
		[Description("")]
		[GridCategory("Parameters")]
		public bool ReverseEntry
		{
			get { return reverse;}
			set { reverse=value;}
		}
        #endregion
    }
}
