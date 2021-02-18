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
    public class NeuralTurningpoint : Strategy
    {
        #region Variables
        // Wizard generated variables
        private LocalOrderManager m_OrderManager=null;
		private NeuralExitStrategy trailingExit=NeuralExitStrategy.NO_TRAILING_STOP;
		private int m_OrderIndex=-1;
		private int sl=50;
		private int tp=0;
		private int smoothingFactor=7;
		private bool reverse=false;
		private bool exitOnCounterSignal=false;
		private int encogPort=5128;
		private String encogHost="localhost";
		private double shortTriggerLevel=0;
		private double longTriggerLevel=0;
		private double atrFactor=1;
		private int lotSize=1;
		private bool trailOnProfit=false;
        // User defined variables (add any user defined variables below)
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            CalculateOnBarClose = true;
            m_OrderManager = new LocalOrderManager(this, 0);
            m_OrderManager.SetDebugLevels(0, 1, false, 0);
            m_OrderManager.SetStatsBoxVisable(false);
            m_OrderManager.SetAutoSLPTTicks(SL,TP, 0);
			Add(AaLaguerreMA(70));
			HMA(EncogFrameworkIndicator(encogHost, "default", EncogPort),smoothingFactor).Panel=1;
			Add(HMA(EncogFrameworkIndicator(encogHost, "default", EncogPort),smoothingFactor));
			EncogFrameworkIndicator(encogHost, "default", EncogPort).Panel=1;
			Add(EncogFrameworkIndicator(encogHost, "default", EncogPort));
            IgnoreOverFill = true;	
      
            EntriesPerDirection = 1;
            EntryHandling = EntryHandling.AllEntries;
            CalculateOnBarClose = true;
            IncludeCommission = true;
            Slippage = 1;
            ExitOnClose = true;
        }

		
		protected override void OnMarketData(MarketDataEventArgs e) 
        {	
			if (m_OrderManager != null) m_OrderManager.OnMarketData( e);
		}
		protected override void OnOrderUpdate(IOrder order) 
		{
            if (m_OrderManager != null) m_OrderManager.OnOrderUpdate(order);
		}
       protected override void OnExecution(IExecution execution)
        {
			if (m_OrderManager != null) m_OrderManager.OnExecution(execution);
			

        }		
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			double atr=ATR(5)[0];
         	double curr=HMA(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,SmoothingFactor)[0];
            double prev=HMA(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,SmoothingFactor)[1];
			if (smoothingFactor==0){
         	 	curr=EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1[0];
             	prev=EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1[1];
			}
			
            if (Position.MarketPosition==MarketPosition.Flat){

                if ( curr<prev && prev>=ShortTriggerLevel){
                    if (reverse==false)m_OrderManager.GoShortMarket(lotSize,0); else  m_OrderManager.GoLongMarket(lotSize,0);
				}
                else if ( curr>prev && prev<=LongTriggerLevel)
                    if (reverse==false ) m_OrderManager.GoLongMarket(lotSize,0); else  m_OrderManager.GoShortMarket(lotSize,0);
            } 
            else if (Position.MarketPosition==MarketPosition.Long){
				if (TrailingExit==NeuralExitStrategy.TRAILING_STOP_LOW)
					m_OrderManager.ExitSLPT(Low[3],0.0,0);
				if (m_OrderManager.GetAvePrice(0)<Close[0] && TrailingExit==NeuralExitStrategy.TRAILING_STOP_ATR)
					m_OrderManager.ExitSLPT(Low[0]-(atr*TrailingAtrFactor),0.0,0);				
                if (exitOnCounterSignal && prev>=0 && curr<0 && TrailingExit==NeuralExitStrategy.NO_TRAILING_STOP)
             		 m_OrderManager.ExitMarket(0);


            }
            else if (Position.MarketPosition==MarketPosition.Short){
				if (TrailingExit==NeuralExitStrategy.TRAILING_STOP_LOW)
					m_OrderManager.ExitSLPT(High[3],0.0,0);
				if (m_OrderManager.GetAvePrice(0)>Close[0] && TrailingExit==NeuralExitStrategy.TRAILING_STOP_ATR)
					m_OrderManager.ExitSLPT(High[0]+(atr*TrailingAtrFactor),0.0,0);				
               if (exitOnCounterSignal && curr>0 && prev>=0 && TrailingExit==NeuralExitStrategy.NO_TRAILING_STOP)
              	m_OrderManager.ExitMarket(0);


            } 			
        }

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
        public int TP
        {
            get { return tp; }
            set { tp = Math.Max(0, value); }
        }		
		        [Description("")]
        [GridCategory("Parameters")]
        public int SmoothingFactor
        {
            get { return smoothingFactor; }
            set { smoothingFactor = Math.Max(0, value); }
        }
		
        [Description("")]
        [GridCategory("Parameters")]
        public bool Reverse
        {
            get { return reverse; }
            set { reverse = value; }
        }
		
        [Description("")]
        [GridCategory("Parameters")]
        public bool ExitOnCounterSignal
        {
            get { return exitOnCounterSignal; }
            set { exitOnCounterSignal = value; }
        }
	
		[Description("")]
        [GridCategory("Parameters")]
        public String EncogHost
        {
            get { return encogHost; }
            set { encogHost =  value; }
        }			
		
        [Description("")]
        [GridCategory("Parameters")]
        public int EncogPort
        {
            get { return encogPort; }
            set { encogPort = Math.Max(0, value); }
        }		
		
        [Description("")]
        [GridCategory("Parameters")]	
        public double LongTriggerLevel
        {
            get { return longTriggerLevel; }
            set { longTriggerLevel = value; }
        }
        [Description("")]
        [GridCategory("Parameters")]	
        public double ShortTriggerLevel
        {
            get { return shortTriggerLevel; }
            set { shortTriggerLevel = value; }
        }
        [Description("")]
        [GridCategory("Parameters")]		
        public NeuralExitStrategy TrailingExit
        {
            get { return trailingExit; }
            set { trailingExit = value; }
		}
       [Description("")]
        [GridCategory("Parameters")]
        public double TrailingAtrFactor
        {
            get { return atrFactor; }
            set { atrFactor = Math.Max(0, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public int LotSize
        {
            get { return lotSize; }
            set { lotSize = Math.Max(0, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public bool TrailOnProfit
        {
            get { return trailOnProfit; }
            set { trailOnProfit =  value; }
        }		
		
		

        #endregion
    }
}
