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
    public class Insania : Strategy
    {
        #region Variables
        // Wizard generated variables
        private double factor = 2.000; // Default setting for Factor
		private bool reverse=true;
		private bool trailer=true;
		private int sl=30;
        private int pt=0;
        private int maxloss = 500; 
		private LocalOrderManager m_OrderManager;
        // User defined variables (add any user defined variables below)
		
		
		private double priorTradesCumProfit=0;
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {

            EntriesPerDirection = 1;
            EntryHandling = EntryHandling.AllEntries;

			
            CalculateOnBarClose = true;
			Unmanaged = true;
			ExitOnClose=true;
			Enabled=true;
			
			//SetStopLoss(CalculationMode.Ticks,SL);
 			m_OrderManager=new LocalOrderManager(this,0);              
			m_OrderManager.SetDebugLevels(0,1,false,0);     // Optional      
			m_OrderManager.SetStatsBoxVisable(true) ;           // Optional     (Default is true)
			m_OrderManager.SetAutoSLPTTicks(sl,pt,0);         // Optional

        }
		protected override void OnOrderUpdate(IOrder order)
		{
				m_OrderManager.OnOrderUpdate(order);
		}
		
		protected override void OnExecution(IExecution execution)
		{
				m_OrderManager.OnExecution(execution);
		}
		
		protected override void OnMarketData(MarketDataEventArgs e)
		{    
				m_OrderManager.OnMarketData( e);
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
						
				if (MarketPosition.Flat!=m_OrderManager.GetMarketPosition(0))
					m_OrderManager.ExitMarket(0);
                return;
            }            

			bool bigbar=false;
			if (Math.Abs(Close[0]-Open[0])>Math.Abs(factor*(Close[1]-Open[1]))) bigbar=true;
		
			if (Close[0]>Open[0] && bigbar) {if (reverse)m_OrderManager.GoShortMarket((int)1,(int)0);else m_OrderManager.GoLongMarket((int)1,(int)0);}
			if (Close[0]<Open[0] && bigbar){ if (reverse)m_OrderManager.GoLongMarket((int)1,(int)0);else m_OrderManager.GoShortMarket((int)1,(int)0);}
      
		//	if (MarketPosition.Flat!=m_OrderManager.GetMarketPosition(0) && trailer){ m_OrderManager.SetTrailingStop("TrailStop", 0, sl,0);}	
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
