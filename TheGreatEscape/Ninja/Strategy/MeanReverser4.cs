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
    public class MeanReverser4 : Strategy
    {
        #region Variables
		
		
		private int		target1			= 5;
		private LocalOrderManager m_OrderManager;
		private int lots=1;
        // Wizard generated variables
        private int maLen = 75; // Default setting for RSI_Period
		private double multiplier=1;
		private int entryTrendSignal=2;
		private int SL = 22; // Default setting for RSI_Period
		//private int PT = 25; // Default setting for RSI_Period
		//private ArrayList highArray = new ArrayList();
		//private ArrayList lowArray = new ArrayList();
		private DataSeries  highArray;
		private DataSeries  lowArray;
        // User defined variables (add any user defined variables below)
        #endregion
		
		protected override void OnStartUp()
        {
            highArray =  new DataSeries(this,  MaximumBarsLookBack.Infinite);
            lowArray =  new DataSeries(this, MaximumBarsLookBack.Infinite);


		}
		//These override’s connect the Strategy to the Local Order Manager
		
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
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            CalculateOnBarClose = false;
			Unmanaged = true;
			ExitOnClose=false;
			Enabled=true;
			
			//SetStopLoss(CalculationMode.Ticks,SL);
 			m_OrderManager=new LocalOrderManager(this,0);              
			m_OrderManager.SetDebugLevels(0,1,false,0);     // Optional      
			m_OrderManager.SetStatsBoxVisable(true) ;           // Optional     (Default is true)
			m_OrderManager.SetAutoSLPTTicks(SL,target1,0);         // Optional
			

			
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
			if (CurrentBar < 20)
				return;

			double v1=SMA(Close, maLen)[0];
			double v2=SMA(Close, maLen)[1];
			if (EntryTrendSignal==2){
				v1=Close[0];
				v2=Open[0];
			}
			if (EntryTrendSignal==3){
				v2=Close[0];
				v1=Open[0];
			}
			highArray.Set(High[0]-Open[0]);
			lowArray.Set(Open[0]-Low[0]);
		 	stdDevHigh =ATR(1)[0]*multiplier;//StdDev(highArray,5)[0]*multiplier;
			stdDevLow = ATR(1)[0]*multiplier;//StdDev(lowArray,5)[0]*multiplier;

			
			if (MarketPosition.Flat==m_OrderManager.GetMarketPosition(0)){
				if (v1>v2){
					EnterLongLim();
				}	
				if (v1<v2){
					EnterShortLim();
				}	
			}	
			
			if (MarketPosition.Long==m_OrderManager.GetMarketPosition(0)){
				EnterShortLim();
	
			}	
			if (MarketPosition.Short==m_OrderManager.GetMarketPosition(0)){
				
			EnterLongLim();
				}		
        }
		private void EnterShortLim(){
			double t=this.GetCurrentBid();
			if (t==0) t=Close[0];
					double price=t+ stdDevLow;
				 m_OrderManager.GoShortLimit((int)lots,(double)price,(int)0) ; 
			
		}
		private void EnterLongLim(){
			double t=this.GetCurrentAsk();
			if (t==0) t=Close[0];
					double price=t - stdDevLow;
				      m_OrderManager.GoLongLimit((int)lots,(double)price,(int)0) ; // Place Limit Order at "m_Limit"

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
        public int MaLength
        {
            get { return maLen; }
            set { maLen = Math.Max(1, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public int EntryTrendSignal
        {
            get { return entryTrendSignal; }
            set { entryTrendSignal = Math.Max(1, value); }
        }
		
        [Description("")]
        [GridCategory("Parameters")]
        public double Multiplier
        {
            get { return multiplier; }
            set { multiplier = value; }
        }		
		
		
		[Description("target1")]
        [Category("Parameters")]
        public int PT
        {
            get { return target1; }
            set { target1 = Math.Max(1, value); }
        }
		
        #endregion
    }
}
