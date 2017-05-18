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
    /// Strategi translated from http://systemtradersuccess.com/rsi-and-how-to-profit-from-it/
    /// </summary>
    [Description("RSI system that trades on dips against the trend. ")]
    public class Rsi2 : Strategy
    {
		
		public enum RsiOrderTypes{
			ORDER_BUYSELL, ORDER_BUYONLY, ORDER_SELLONLY
		};	
		public enum ExitTypes{
			EXIT_MA, EXIT_EMA
		};	
        #region Variables
		private String emailFrom="user@domain.com";
		private String emailTo="user@domain.com";
        // Wizard generated variables 
        private int smaExitPeriods = 8; //
		private int smaEntryPeriods= 140;
		private int exitOnBarNumber= 0;
		private int rsiPeriods= 2;
		private bool useTrailingStop= false;
		private bool useLimitOrders= false;
		private int lots = 2;
		private int momentumCheckBarCount = 10;
		private bool enableMomentumCheck = false;
		private int sl = 150;
		private int rsiUpperThreshold= 95;
		private int rsiLowerThreshold= 5;
		private RsiOrderTypes rsiOrderType=RsiOrderTypes.ORDER_BUYSELL;
		private ExitTypes exitType=ExitTypes.EXIT_MA;
        private bool ignoreTimeFilters = true;
		private int sessionNoTradingFrom=070000;
		private int sessionNoTradingUntil=080000;
		private bool skipThursdays=false;		
		
        // User defined variables (add any user defined variables below)
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {			
            EntriesPerDirection = 2;
            EntryHandling = EntryHandling.AllEntries;
            CalculateOnBarClose = true;
            IncludeCommission = true;
            //Slippage = 1;
			Add(bwAO());
            ExitOnClose = true;
			Enabled = true;
			if (useTrailingStop==false)
				SetStopLoss(CalculationMode.Ticks, sl);
			else
				SetTrailStop(CalculationMode.Ticks, sl);
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			RSI rsi = RSI(rsiPeriods, 0);
			SMA sma = SMA(smaEntryPeriods);
			double exitLevel=0;
		//	SMA smaExit = SMA(smaExitPeriods);
			
			int trend=0;
			if (Close[0]>sma[0]) trend=1;
			if (Close[0]<sma[0]) trend=-1;
			
			if (exitType==ExitTypes.EXIT_MA){
				exitLevel=SMA(smaExitPeriods)[0];
			}
			if (exitType==ExitTypes.EXIT_EMA){
				exitLevel=EMA(smaExitPeriods)[0];
			}
			
			if (InsideTradingSessions() && rsi[0]<rsiLowerThreshold  && trend==1 && rsiOrderType!=RsiOrderTypes.ORDER_SELLONLY) EnterLongPos();			
			if (MarketPosition.Long==Position.MarketPosition && (KeepLongPos()==false || Close[0]>exitLevel)) ExitLong();
			if (InsideTradingSessions() && rsi[0]>rsiUpperThreshold  && trend==-1 && rsiOrderType!=RsiOrderTypes.ORDER_BUYONLY) EnterShortPos();
			if (MarketPosition.Short==Position.MarketPosition && (KeepShortPos()==false || Close[0]<exitLevel)) ExitShort();
        }
		#region Filters
		private bool KeepLongPos(){
			if (ExitOnBarNumber>0 && ExitOnBarNumber<=BarsSinceEntry()) return false;
			if (BarsSinceEntry()<momentumCheckBarCount || enableMomentumCheck==false)
				return true;
			for (int j=0;j<momentumCheckBarCount;j++){
				if (bwAO().AOValue[j]>bwAO().AOValue[j+1])
					return true;
			}
			return false;
		}
		private bool KeepShortPos(){
			if (ExitOnBarNumber>0 && ExitOnBarNumber<=BarsSinceEntry()) return false;
			if (BarsSinceEntry()<momentumCheckBarCount || enableMomentumCheck==false)
				return true;
			for (int j=0;j<momentumCheckBarCount;j++){
				if (bwAO().AOValue[j]<bwAO().AOValue[j+1])
					return true;
			}
			return false;
		}		

		protected bool InsideTradingSessions(){           
    		if (SkipThursdays && Time[0].DayOfWeek == DayOfWeek.Thursday)
        		return false;	
 			if (IgnoreTimeFilters) return true;			
			int current=ToTime(Time[0]);
			if (SessionNoTradingFrom!=0 || SessionNoTradingUntil!=0){
				if (current>=SessionNoTradingFrom && current<SessionNoTradingUntil)
					return false;
			}
			if (SessionNoTradingFrom!=0 || SessionNoTradingUntil!=0){
				if (current>=SessionNoTradingFrom && current<SessionNoTradingUntil)
					return false;
			}
			return true;
		}				
		#endregion		
		#region OrderManagement
		private void ExitLongPos(){
			if (useLimitOrders){
				this.EnterShortLimit(Math.Abs(Position.Quantity),Close[0]);
			}else{
				ExitLong();
			}
		}
		private void ExitShortPos(){
			if (useLimitOrders){
				this.EnterLongLimit(Math.Abs(Position.Quantity),Close[0]);
			}else{
				ExitShort();
			}
		}
		
		private void EnterLongPos(){
			if (useLimitOrders){
				this.EnterLongLimit(lots,Close[0]);
			}else{
				EnterLong(lots);
			}
		}
		private void EnterShortPos(){
			if (useLimitOrders){
				this.EnterShortLimit(lots,Close[0]);
			}else{
				EnterShort(lots);
			}
		}
		#endregion
		#region Email
		public void signalOrderFilled( NinjaTrader.Cbi.IOrder order)
        {	
            string subject = this.Account.Name + "/" + order.Instrument.FullName + "/" + order.OrderAction.ToString();
            string message = "";
			string profitloss = "";			
          	  if (this.Performance.AllTrades.Count > 1) {
                Trade lastTrade = this.Performance.AllTrades[this.Performance.AllTrades.Count - 1];
                double tickProfit = lastTrade.ProfitPoints / this.TickSize;
                if ((order.OrderAction == OrderAction.BuyToCover || order.OrderAction == OrderAction.Sell)){
					// The position was closed. Calc profit or loss
					if (Convert.ToInt32(tickProfit)>=0)                    	
						profitloss="profit";
					else
						profitloss="loss";					
					subject = subject + " #ticks " + profitloss + ":" + Convert.ToInt32(tickProfit);					
					message = order.Instrument.FullName + "/" + order.OrderAction.ToString() + ": closed at #ticks " + profitloss + ": " + Convert.ToInt32(tickProfit);
				 }else{
					subject = subject + " at price " + order.AvgFillPrice;
					message = order.Instrument.FullName + "/" + order.OrderAction.ToString() + " at price " + order.AvgFillPrice;
				}
            }
            if (this.Account.Name.StartsWith("Replay") == false && !Historical){
				this.SendMail(emailFrom, emailTo, subject, message);
			}
			
		}		
		protected override void OnOrderUpdate(IOrder order) 
		{ 
			if (order.OrderState == OrderState.Filled )
			{
				signalOrderFilled( order);
			}
		}		
#endregion
        #region Properties

		
        [Description("")]
        [GridCategory("Parameters")]
        public string EmailFrom
        {
            get { return emailFrom; }
            set { emailFrom = value; }
        }	
		
        [Description("")]
        [GridCategory("Parameters")]
        public string EmailTo
        {
            get { return emailTo; }
            set { emailTo = value; }
        }		
		
		
		
        [Description("")]
        [GridCategory("Parameters")]
        public int ExitOnBarNumber
        {
            get { return exitOnBarNumber; }
            set { exitOnBarNumber = Math.Max(0, value); }
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
        public bool UseTrailingStop
        {
            get { return useTrailingStop; }
            set { useTrailingStop = value; }
        }	
		
        [Description("")]
        [GridCategory("Parameters")]
        public bool UseLimitOrders
        {
            get { return useLimitOrders; }
            set { useLimitOrders = value; }
        }		
			
		
        [Description("")]
        [GridCategory("Parameters")]
        public int SmaExitPeriods
        {
            get { return smaExitPeriods; }
            set { smaExitPeriods = Math.Max(1, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public int SmaEntryPeriods
        {
            get { return smaEntryPeriods; }
            set { smaEntryPeriods = Math.Max(1, value); }
        }

        [Description("")]
        [GridCategory("Parameters")]
        public bool EnableMomentumCheck
        {
            get { return enableMomentumCheck; }
            set { enableMomentumCheck = value; }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public int MomentumCheckBarCount
        {
            get { return momentumCheckBarCount; }
            set { momentumCheckBarCount = Math.Max(1, value); }
        }
		
        [Description("")]
        [GridCategory("Parameters")]
        public int RsiLowerThreshold
        {
            get { return rsiLowerThreshold; }
            set { rsiLowerThreshold = Math.Max(1, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public int RsiUpperThreshold
        {
            get { return rsiUpperThreshold; }
            set { rsiUpperThreshold = Math.Max(1, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public int RsiPeriods
        {
            get { return rsiPeriods; }
            set { rsiPeriods = Math.Max(1, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public int Lots
        {
            get { return lots; }
            set { lots = Math.Max(1, value); }
		}	

		        [Description("")]
        [GridCategory("Parameters")]
        public RsiOrderTypes OrderType
        {
            get { return rsiOrderType; }
            set { rsiOrderType = value; }
		}	
		
		        [Description("")]
        [GridCategory("Parameters")]
        public ExitTypes ExitType
        {
            get { return exitType; }
            set { exitType = value; }
		}	
		
        [Description("")]
        [GridCategory("Parameters")]
        public int SessionNoTradingFrom
        {
            get { return sessionNoTradingFrom; }
            set { sessionNoTradingFrom = value; }
        }

        [Description("")]
        [GridCategory("Parameters")]
        public int SessionNoTradingUntil
        {
            get { return sessionNoTradingUntil; }
            set { sessionNoTradingUntil = value; }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public bool IgnoreTimeFilters
        {
            get { return ignoreTimeFilters; }
            set { ignoreTimeFilters = value; }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public  bool SkipThursdays
        {
            get { return skipThursdays; }
            set { skipThursdays = value; }
        }
        #endregion
    }
}
