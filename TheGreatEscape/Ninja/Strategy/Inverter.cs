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
using System.Collections;
#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    /// <summary>
    /// Enter the description of your strategy here
    /// </summary>
    [Description("Enter the description of your strategy here")]
    public class Inverter2 : Strategy
    {
        #region Variables
		
		
		private int		target1			= 1500;

		private int lots=1;
        // Wizard generated variables
        private int maLen = 10; // Default setting for RSI_Period
		private int ticksDiff=1;
		private bool useAtrEntry=true;
		private bool useHighLowEntry=false;
		private int entryTrendSignal=2;
		private int SL = 100; // Default setting for RSI_Period
		private int maxprofit = 5000; // Max profit before halt strategy
		private int maxloss = 5000; // Max loss before halt strategy
		private double priorTradesCumProfit=0;
		//private int PT = 25; // Default setting for RSI_Period

        // User defined variables (add any user defined variables below)
        #endregion
		
		protected override void OnStartUp()
        {



		}

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            CalculateOnBarClose = true;
			this.EntriesPerDirection=3;
			//Unmanaged = true;
			this.Slippage=1;
			ExitOnClose=false;
			Enabled=true;
			
			//SetStopLoss(CalculationMode.Ticks,SL);
 		/*	m_OrderManager=new LocalOrderManager(this,0);              
			m_OrderManager.SetDebugLevels(0,1,false,0);     // Optional      
			m_OrderManager.SetStatsBoxVisable(true) ;           // Optional     (Default is true)
			m_OrderManager.SetAutoSLPTTicks(SL,target1,0);         // Optional
			m_OrderManager.SetAutoSLPTTicks(SL,target1,1); 
		*/	
			this.SetStopLoss(CalculationMode.Ticks, SL);
			this.SetProfitTarget(CalculationMode.Ticks,target1);
			
        }
        public   double TicksProfit(double price) {
            double ppoints = Position.GetProfitLoss(price, PerformanceUnit.Points);
            double tickProfit = ppoints / TickSize;
			return tickProfit;
        }
		
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
		/// 
		/// 

		/// '
		///
		double stdDevHigh=0;
		double stdDevLow=0;
        protected override void OnBarUpdate()
        {
			
				// At the start of a new session
			if (Bars.FirstBarOfSession && FirstTickOfBar)
			{
				priorTradesCumProfit = Performance.AllTrades.TradesPerformance.Currency.CumProfit;
			}

			if ((Performance.AllTrades.TradesPerformance.Currency.CumProfit - priorTradesCumProfit) <= -maxloss)
			{			
				if (MarketPosition.Long==this.Position.MarketPosition)
					this.ExitLong();
				if (MarketPosition.Short==this.Position.MarketPosition)
					this.ExitShort();				
				return;
			}
			if (CurrentBar < 20)
				return;
			
			int x=this.HighestBar(Close, maLen);
			int y=this.LowestBar(Close, maLen);
			
			int position=0;
	
			if (x==ticksDiff) EnterShort();
			if (y==ticksDiff) EnterLong();
			

	
        }
		
		private void ManageOrders(int position, int direction){
						
			if (direction!=0 && MarketPosition.Flat==Position.MarketPosition){
				if (direction==1){
					EnterLongLim(position);
				}	
				if (direction==-1){
					EnterShortLim(position);
				}	
			}	
			
			if (MarketPosition.Long==Position.MarketPosition){
				EnterShortLim(position);
	
			}	
			if (MarketPosition.Short==Position.MarketPosition){				
				EnterLongLim(position);
			}	
		}
		
		
		//double trigglim=(Close[0]+LimitOrderOffset*TickSize);
		private void EnterShortLim(int position){
	
			
		}
		private void EnterLongLim(int position){
			
		}		
		
        #region Properties
        [Description("")]
        [GridCategory("Parameters")]
        public int StopLoss
        {
            get { return SL; }
            set { SL = Math.Max(1, value); }
        }	
	
		
		 [Description("lots")]
        [GridCategory("Parameters")]
        public int Lots
        {
            get { return lots; }
            set { lots = Math.Max(1, value); }
        }		
        [Description("")]
        [GridCategory("Parameters")]
        public int MaxLen
        {
            get { return maLen; }
            set { maLen = Math.Max(1, value); }
        }
 
		
        [Description("")]
        [GridCategory("Parameters")]
        public int Index
        {
            get { return ticksDiff; }
            set { ticksDiff = value; }
        }		
		
		
		[Description("target1")]
        [Category("Parameters")]
        public int PT
        {
            get { return target1; }
            set { target1 = Math.Max(1, value); }
        }

		[Description("Maximum profit.")]
		[Category("Parameters")]
		[Gui.Design.DisplayName("\t\t\tMax Profit")]
		public int MaxProfit
		{
		get { return maxprofit; }
		set { maxprofit = Math.Max(1, value); }
		}

		[Description("Maximum loss (enter positive value!).")]
		[Category("Parameters")]
		[Gui.Design.DisplayName("\t\t\tMax Loss")]
		public int MaxLoss
		{
		get { return maxloss; }
		set { maxloss = Math.Max(1, value); }
		}		
		
		
        #endregion
    }
}
