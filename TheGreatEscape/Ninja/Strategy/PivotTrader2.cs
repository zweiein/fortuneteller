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
	
	public enum PivotTraderExitStrategy{
		NO_TRAILING_STOP, BREAKEVEN_STOP, TRAILING_STOP_ATR, TRAILING_CHANDELIER, TRAILING_STOP_PREV_HIGHLOW,TRAILING_MANUAL_HIGHLOW1,TRAILING_MANUAL_HIGHLOW2
	};
    /// <summary>
    /// This strategy puts on reversal trades on pivot points (with N bars to the left and right of the pivot bar
    /// </summary>
    [Description("Enter the description of your strategy here")]
    public class PivotTrader2 : Strategy
    {
		
		
        #region Variables
        // Wizard generated variables
        private int sl = 45; // Default setting for STOP LOSS TICKS
		private int pt = 0; // Profit Target (tics) 0 means not activated
		private int trailingStopMinProfitTicks=10;
		private int leftPivotStrength=2;
		private int rightPivotStrength=2;
		private PivotTraderExitStrategy trailingExitStrategy=PivotTraderExitStrategy.NO_TRAILING_STOP;
		
        private int sessionStart = 080000;
        private int sessionEnd = 100000;
		
		
		// USed for Candelier and ATR trailing stops.
		private double TrailingExitFactor=3;
		private int TrailingExitPeriods=22;
		private int TrailingMaxHighLowPeriods=22;
		
        // User defined variables (add any user defined variables below)
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        { 
            EntriesPerDirection = 1;
            EntryHandling = EntryHandling.AllEntries;
            CalculateOnBarClose = true;
            IncludeCommission = true;
            Slippage = 1;
            ExitOnClose = false;				
			SetStopLoss(CalculationMode.Ticks, sl);	
			if (pt>0)
				SetProfitTarget(CalculationMode.Ticks,pt);
        }
        protected virtual void ManageOrders()
        {
			double stopPrice=0;
			if (Position.MarketPosition == MarketPosition.Long){
				
				//Only start trailing stop when Close is better than average price + minimum profit
				if (Close[0]>(Position.AvgPrice +(TrailingStopMinProfitTicks*TickSize))){
					
					if (TrailingExitStrategy==PivotTraderExitStrategy.BREAKEVEN_STOP){
						stopPrice=Position.AvgPrice; // Break even
					}
					if (TrailingExitStrategy==PivotTraderExitStrategy.TRAILING_STOP_ATR){
						stopPrice=Low[0] - ATR(TrailingExitPeriods)[0]*TrailingExitFactor;
					}
					if (TrailingExitStrategy==PivotTraderExitStrategy.TRAILING_CHANDELIER){
						stopPrice=MAX(High,TrailingMaxHighLowPeriods)[0] - ATR(TrailingExitPeriods)[0]*TrailingExitFactor;
					}
					if (TrailingExitStrategy==PivotTraderExitStrategy.TRAILING_STOP_PREV_HIGHLOW){
						stopPrice=Low[1]; 
					}
					if (TrailingExitStrategy==PivotTraderExitStrategy.TRAILING_MANUAL_HIGHLOW1){
						if (Close[0]<High[1])
							ExitLong("LongPos");
					}
					if (TrailingExitStrategy==PivotTraderExitStrategy.TRAILING_MANUAL_HIGHLOW2){
						if (Close[0]<Low[1])
							ExitLong("LongPos");
					}
					//If trailing stop is turned off, leave stop loss to it original level
					if (stopPrice!=0)
						SetStopLoss("LongPos",CalculationMode.Price, stopPrice,false);	
				}
				
			}
			if (Position.MarketPosition == MarketPosition.Short){
				
				//Only start trailing stop when Close is better (less when short) than average price + minimum profit
				if (Close[0]<(Position.AvgPrice -(TrailingStopMinProfitTicks*TickSize))){
					
					if (TrailingExitStrategy==PivotTraderExitStrategy.BREAKEVEN_STOP){
						stopPrice=Position.AvgPrice; // Break even
					}
					if (TrailingExitStrategy==PivotTraderExitStrategy.TRAILING_STOP_ATR){
						stopPrice=High[0]+ ATR(TrailingExitPeriods)[0]*TrailingExitFactor;
					}
					if (TrailingExitStrategy==PivotTraderExitStrategy.TRAILING_CHANDELIER){
						stopPrice=MIN(Low,TrailingMaxHighLowPeriods)[0] + ATR(TrailingExitPeriods)[0]*TrailingExitFactor;
					}
					if (TrailingExitStrategy==PivotTraderExitStrategy.TRAILING_STOP_PREV_HIGHLOW){
						stopPrice=High[1]; 
					}
					if (TrailingExitStrategy==PivotTraderExitStrategy.TRAILING_MANUAL_HIGHLOW1){
						if (Close[0]>Low[1])
							ExitShort("ShortPos");
					}
					if (TrailingExitStrategy==PivotTraderExitStrategy.TRAILING_MANUAL_HIGHLOW2){
						if (Close[0]>High[1])
							ExitShort("ShortPos");
					}
					//If trailing stop is turned off, leave stop loss to it original level
					if (stopPrice!=0)
						SetStopLoss("ShortPos", CalculationMode.Price, stopPrice,false);	
				}
			}

			if (Position.MarketPosition == MarketPosition.Flat){
				SetStopLoss(CalculationMode.Ticks, sl);	  //Reset SL default value	
			}
		}

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			ManageOrders();
			
			if (ToTime(Time[0])<= SessionEnd && ToTime(Time[0]) >= SessionStart){
				
				if (PivotLowDetected(PivotStrengthLeft,PivotStrengthRight)){
						EnterLong("LongPos");
				}
				if ( PivotHighDetected(PivotStrengthLeft,PivotStrengthRight)){
						EnterShort("ShortPos");				
				}	
				
			}else{
				// Outside session. Close positions?
				if (Position.MarketPosition == MarketPosition.Long){
					ExitLong("LongPos");
				}
				if (Position.MarketPosition == MarketPosition.Short){
					ExitShort("ShortPos");
				}
			}
        }
		
		#region Functions

		public bool PivotLowDetected(int leftStrength, int rightStrength){		
			bool flag=true;
			for (int i=(rightStrength-1);i>=0;i--){
				if (Low[i]<Low[rightStrength])
					flag=false;
			}
			for (int i=(rightStrength+1);i<=(leftStrength+rightStrength);i++){
				if (Low[i]<Low[rightStrength])
					flag=false;
			}	
			return flag;
		}

		public bool PivotHighDetected(int leftStrength, int rightStrength){
			bool flag=true;

			for (int i=(rightStrength-1);i>=0;i--){
				if (High[i]>High[rightStrength])
					flag=false;
			}
			for (int i=(rightStrength+1);i<=(leftStrength+rightStrength);i++){
				if (High[i]>High[rightStrength])
					flag=false;
			}
			return flag;
		}
#endregion
		
        #region Properties
        [Description("")]
        [GridCategory("Parameters")]
        public int SL
        {
            get { return sl; }
            set { sl = Math.Max(0, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public int PT
        {
            get { return pt; }
            set { pt = Math.Max(0, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public int TrailingStopMinProfitTicks
        {
            get { return trailingStopMinProfitTicks; }
            set { trailingStopMinProfitTicks = Math.Max(0, value); }
        }
		
		
        [Description("")]
        [GridCategory("Parameters")]
        public int PivotStrengthRight
        {
            get { return rightPivotStrength; }
            set { rightPivotStrength = Math.Max(1, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public int PivotStrengthLeft
        {
            get { return leftPivotStrength; }
            set { leftPivotStrength = Math.Max(1, value); }
        }		
        [Description("")]
        [GridCategory("Parameters")]		
        public PivotTraderExitStrategy TrailingExitStrategy
        {
            get { return trailingExitStrategy; }
            set { trailingExitStrategy = value; }
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
