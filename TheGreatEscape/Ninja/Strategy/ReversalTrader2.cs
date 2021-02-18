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
    /// This strategy puts on reversal trades on up bars that are løopwer than previous up bars, and vice versa for shorts
    /// </summary>
    [Description("Reversal trading strategy that uses a Kagi chart and a methodology that tries to detect the reversal of an instrument and go long or short.")]
    public class ReversalTrader2 : Strategy
    {	
        #region Variables
		
		private bool ignoreTimeFilter = true;
        private int sessionStart = 080000;
        private int sessionEnd = 140000;
		
		// Exiting declarations
        private int sl = 35; 								// Default setting for stop loss (ticks)
		private int pt = 50; 								// Profit Target 
		public enum ExitType { Trailing, StopProfit }
		private ExitType exitType = ExitType.StopProfit;
		private int TrailingExitPeriods = 10;
        private double dATRStop 		= 1.25; 			// Default setting for ATR stop multiple (for trailing stops)

        // User defined variables (add any user defined variables below)
        #endregion

		#region Initialize
        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            EntriesPerDirection = 2;
            EntryHandling = EntryHandling.AllEntries;
            CalculateOnBarClose = true;
            IncludeCommission = true;
//            Slippage = 2;
            ExitOnClose = true;
			Enabled = true;
        }
		#endregion
		
		#region Exit type
		private void exitMethodSetup( string label, double trail_stop_ticks )
		{
			if( exitType == ExitType.StopProfit )
			{
				SetStopLoss( label, CalculationMode.Ticks, SL, false );
				SetProfitTarget( label, CalculationMode.Ticks, PT );
			}
			else if( exitType == ExitType.Trailing )
			{
				SetTrailStop( label, CalculationMode.Ticks, trail_stop_ticks, false);
			}
		}
		#endregion		

		#region OnBarUpdate
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			double trail_stop = ATRStop * ATR(TrailingExitPeriods)[0];
			double trail_stop_ticks = Bars.Instrument.MasterInstrument.Round2TickSize(trail_stop)/TickSize ;
			
			if( IgnoreTimeFilter 
				|| (ToTime(Time[0]) <= SessionEnd && ToTime(Time[0]) >= SessionStart) )
			{
				string label;
				if( BullReversalDetected() ) 
				{
					if( Position.MarketPosition == MarketPosition.Short
						|| Position.MarketPosition == MarketPosition.Flat ) {
						label = "Long 0";
					}
					else {
						label = "Long 1";
					}
					exitMethodSetup( label, trail_stop_ticks );
					EnterLong( label );
				}
				else if( BearReversalDetected() )
				{
					if( Position.MarketPosition == MarketPosition.Flat
						|| Position.MarketPosition == MarketPosition.Long ) {
						label = "Short 0";
					}
					else {
						label = "Short 1";
					}
					exitMethodSetup( label, trail_stop_ticks );
					EnterShort( label );				
				}	
				
			}
			else
			{
				// Outside session. Close positions?
				// Don't really need this if ExitOnClose is true.  Besides, since I changed the labels, it won't
				// work anyway.
				if (Position.MarketPosition == MarketPosition.Long){
					ExitLong("LX","LE");
				}
				if (Position.MarketPosition == MarketPosition.Short){
					ExitShort("SX","SE");
				}
			}
        }
		#endregion
		
		#region Functions

		public bool BullReversalDetected()
		{
			if( Close[0] < Open[0] 
				&& Low[0] > Low[2] )  {
				return true;
			}

			return false;
		}

		public bool BearReversalDetected()
		{
			if( Close[0] > Open[0] 
				&& High[0] < High[2] ) {
				return true;
			}

			return false;
		}
		#endregion
		
        #region Properties

		#region Exit strategy
		// Exit strategy
        [Gui.Design.DisplayNameAttribute("01. Exit strategy type")]
        [Description("Choose how to protect your trade")]
        [GridCategory("ExitStrategy")]
		public ExitType ExitStrategyType
        {
            get { return exitType; }
            set { exitType = value; }
        }		
        [Gui.Design.DisplayNameAttribute("02. Stop loss")]
        [Description("Actual stop loss to set")]
        [GridCategory("ExitStrategy")]
		public int SL
        {
            get { return sl; }
            set { sl = Math.Max(0, value); }
        }
        [Gui.Design.DisplayNameAttribute("03. Profit target")]
        [Description("Actual profit target to set.")]
        [GridCategory("ExitStrategy")]
        public int PT
        {
            get { return pt; }
            set { pt = Math.Max(0, value); }
        }
		#endregion		
		
        [Description("ATR Stop Multiple")]
        [Category("Parameters")]
        public double ATRStop
        {
            get { return dATRStop; }
            set { dATRStop = Math.Max(1, value); }
        }
        [Description("Ignore session/time filter")]
        [GridCategory("Parameters")]
        public bool IgnoreTimeFilter
        {
            get { return ignoreTimeFilter; }
            set { ignoreTimeFilter = value; }
        }
		
        [Description("Time to start trading")]
        [GridCategory("Parameters")]
        public int SessionStart
        {
            get { return sessionStart; }
            set { sessionStart = value; }
        }
       
        [Description("Time to stop trading")]
        [GridCategory("Parameters")]
        public int SessionEnd
        {
            get { return sessionEnd; }
            set { sessionEnd = value; }
        }
		
        #endregion
    }
}
