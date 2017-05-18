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
    public class BollingerPower : Strategy
    {
        #region Variables
        // Wizard generated variables
        private int bollPeriod = 14; // Default setting for MyInput0
		private int adxPeriod=14; 
		private int sl=100;
		private int adxLim=25;
		private int bollStdDev=1;
		private bool invert=true;
		private bool exitOnAdxFilter=true;
		private double breakEven=40;

        // User defined variables (add any user defined variables below)
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            CalculateOnBarClose = true;
			Add(ADX(Close, adxPeriod));
			Add(Bollinger(bollStdDev, bollPeriod));
			this.EntriesPerDirection=2;
			this.ExitOnClose=false;
			this.Enabled=true;
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {

			if (MarketPosition.Flat==Position.MarketPosition){
				SetStopLoss(CalculationMode.Ticks, sl);
			}
			else if (Position.MarketPosition == MarketPosition.Long)
			{
				// Once the price is greater than entry price+50 ticks, set stop loss to breakeven
				if (Close[0] > Position.AvgPrice + breakEven * TickSize)
				{
					SetStopLoss(CalculationMode.Price, Position.AvgPrice);
				}
			}
			else if (Position.MarketPosition == MarketPosition.Short)
			{
				// Once the price is greater than entry price+50 ticks, set stop loss to breakeven
				if (Close[0] < Position.AvgPrice - breakEven * TickSize)
				{
					SetStopLoss(CalculationMode.Price, Position.AvgPrice);
				}
			}
			
			double adxv=ADX(Close, adxPeriod)[0];
			if (adxv>=adxLim){
				if (exitOnAdxFilter){
			  		if (MarketPosition.Long==Position.MarketPosition) ExitLong();
			  		if (MarketPosition.Short==Position.MarketPosition) ExitShort();
				}else{
				 	if (Close[0]>Bollinger(bollStdDev, bollPeriod).Upper[0]) if (invert)ExitLong();else ExitShort();	
					if (Close[0]<Bollinger(bollStdDev, bollPeriod).Lower[0])if (invert==false)ExitLong();else ExitShort();
				}
			}else{
			  if (Close[0]>Bollinger(bollStdDev, bollPeriod).Upper[0]) if (invert)EnterShort();else EnterLong();
			  if (Close[0]<Bollinger(bollStdDev, bollPeriod).Lower[0])if (invert==false)EnterShort();else EnterLong();
			}
        }

        #region Properties
        [Description("")]
        [GridCategory("Parameters")]
        public int BollPeriod
        {
            get { return bollPeriod; }
            set { bollPeriod = Math.Max(1, value); }
        }

	    [Description("")]
        [GridCategory("Parameters")]
        public int AdxLimit
		{
            get { return adxLim; }
            set { adxLim = Math.Max(1, value); }	
		}
	    [Description("")]
        [GridCategory("Parameters")]
        public int AdxPeriods
        {
            get { return adxPeriod; }
            set { adxPeriod = Math.Max(1, value); }	
		
		}
		
	    [Description("")]
        [GridCategory("Parameters")]
        public int BollStdDev
        {
            get { return bollStdDev; }
            set { bollStdDev = Math.Max(0, value); }
        }	
        [Description("")]
        [GridCategory("Parameters")]
        public int SL
        {
            get { return sl; }
            set { sl = Math.Max(0, value); }
        }		
        [Description("")]
        [GridCategory("Parameters")]
        public double BreakEvenLevel
        {
            get { return breakEven; }
            set { breakEven =value; }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public bool Invert
        {
            get { return invert; }
            set { invert = value; }
        }		
        [Description("")]
        [GridCategory("Parameters")]
        public bool ExitOnAdxFilter
        {
            get { return exitOnAdxFilter; }
            set { exitOnAdxFilter = value; }
        }	
		
		
        #endregion
    }
}
