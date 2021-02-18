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
    public class BigBarReverse : Strategy
    {
        #region Variables
        // Wizard generated variables
        private int atrLongPeriods = 14; // Default setting for MyInput0
		private int atrShortPeriods = 1;
		private double atrFactor=1.6;
		int exitOnBar=-1;
		private int sl=100;
		private bool invert=true;
        // User defined variables (add any user defined variables below)
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            CalculateOnBarClose = true;
			this.ExitOnClose=false;
			this.EntriesPerDirection=3;
			this.Enabled=true;
			if (sl>0)
				SetStopLoss(CalculationMode.Ticks, sl);
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			if (ATR(Close, AtrShortPeriods)[0]>AtrFactor*(double)ATR(Close, AtrShortPeriods)[1]){
				
				if (Close[0]>Open[0]) EnterShort();
				if (Close[0]<Open[0]) EnterLong();
			}
		//	if (Close[0]>Bollinger(2, bollPeriod).Upper[0]) if (invert)EnterShort();else EnterLong();
		//	if (Close[0]<Bollinger(2, bollPeriod).Lower[0])if (invert==false)EnterShort();else EnterLong();
						if (this.BarsSinceEntry()>=exitOnBar && exitOnBar>=0 && Position.MarketPosition==MarketPosition.Long) ExitLong();
			if (this.BarsSinceEntry()>=exitOnBar && exitOnBar>=0 && Position.MarketPosition==MarketPosition.Short) ExitShort();

						
        }

        #region Properties
	        [Description("")]
        [GridCategory("Parameters")]
	        public int ExitOnBar
		{
	            get { return exitOnBar;}
	            set { exitOnBar = value; }
	}

		
        [Description("")]
        [GridCategory("Parameters")]
        public int AtrShortPeriods
        {
            get { return atrShortPeriods; }
            set { atrShortPeriods = Math.Max(1, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]		
        public int AtrLongPeriods
        {
            get { return atrLongPeriods; }
            set { atrLongPeriods = Math.Max(1, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]		
        public double AtrFactor
        {
            get { return atrFactor; }
            set { atrFactor =  value; }
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
        public bool Invert
        {
            get { return invert; }
            set { invert = value; }
        }			
        #endregion
    }
}
