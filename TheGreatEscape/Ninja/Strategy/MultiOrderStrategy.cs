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
	public enum MultiOrderExitStrategy{
		NO_TRAILING_STOP,  TRAILING_STOP_ATR, TRAILING_CANDELIER, TRAILING_YOYO, TRAILING_CUSTOM
	};

    /// <summary>
    /// Strategy 
    /// </summary>
    [Description("Strategy to inherit other strategies from. Generic. Multiorders")]
    public class MultiOrderStrategy : Strategy
    {
        #region Variables
        // Wizard generated variables
		private MultiOrderExitStrategy trailingExit=MultiOrderExitStrategy.TRAILING_STOP_ATR;


		
        private int sessionOneStart = 3500;
        private int sessionOneEnd = 110000;
        private int sessionTwoStart = 143500;
        private int sessionTwoEnd = 204500;
		
        private int     target1         = 20;
        private int     target2         = 65;
        private int     target3         = 95;               
        private int     stop            = 18;       
        private bool    be2             = true;
        private bool    be3             = true;     
		private bool    ignoreTimeFilter      = false; 
		private bool 	useLimitOrders = false;
		private int     limitOrderAdjustTicks           = 2;
        private int     lots           = 1;
        private int     orderCount      = 3;     
        private int     sessionMaxLoss   = 1000;       
		private double lastLongTrail=0;
		private double lastShortTrail=0;
        private double yestPnl=0;
		private double sessionPriorPnl=0;
        private double dayPnl=0;
		private double sessionPnl=0;
		private double trailingPnl=0;
		private bool maxLossTriggered=false;
		private bool trailSessionMaxLoss=false;
        private LocalOrderManager m_OrderManager=null;
                
					
		private double trailingExitFactor=3;
		private int trailingExitPeriods=22;
		private int maxHighLowPeriods=22;
        
        // User defined variables (add any user defined variables below)
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
                    
            m_OrderManager = new LocalOrderManager(this, 0);
            m_OrderManager.SetDebugLevels(0, 0, false, 0);
            m_OrderManager.SetStatsBoxVisable(false);
            m_OrderManager.SetAutoSLPTTicks(Stop, Target1, 0);
            if (orderCount>1)
                m_OrderManager.SetAutoSLPTTicks(Stop, (Target1+Target2), 1);
            if (orderCount>2)
                m_OrderManager.SetAutoSLPTTicks(Stop, (Target1+Target2+Target3), 2);
            IgnoreOverFill = true;      
            EntryHandling = EntryHandling.UniqueEntries; 
            CalculateOnBarClose = true;
			trailingPnl=-sessionMaxLoss;
            Slippage = 1;
            InitializeIndicators();
        }  
		protected bool IsNewDay(){
			bool flag=false;
			//if (Bars.FirstBarOfSession) flag=true;
			
			if(Time[0].DayOfWeek!=Time[1].DayOfWeek)
				flag=true;
			return flag;
		}
        protected virtual void InitializeIndicators(){
        }

        protected override void OnOrderUpdate(IOrder order) 
        {
            if (m_OrderManager != null) m_OrderManager.OnOrderUpdate(order);
        }

        protected override void OnExecution(IExecution execution)
        {
            if (m_OrderManager != null) m_OrderManager.OnExecution(execution);

        }
        protected override void OnMarketData(MarketDataEventArgs e)
        {
            if (m_OrderManager != null) m_OrderManager.OnMarketData(e);
        }
        
        protected void GoLong()
        {
			if (UseLimitOrders){
				m_OrderManager.GoLongLimit(lots,Close[0]+ (limitOrderAdjustTicks*TickSize), 0);
				if (orderCount>1)
					m_OrderManager.GoLongLimit(lots,Close[0]+ (limitOrderAdjustTicks*TickSize), 1);
				if (orderCount>2)
					m_OrderManager.GoLongLimit(lots,Close[0]+ (limitOrderAdjustTicks*TickSize), 2);  
			}else{
				m_OrderManager.GoLongMarket(lots, 0);
				if (orderCount>1)
					m_OrderManager.GoLongMarket(lots, 1);
				if (orderCount>2)
					m_OrderManager.GoLongMarket(lots, 2);  
			}
        }

        protected void GoShort()
        {      
			if (UseLimitOrders){
				m_OrderManager.GoShortLimit(lots,Close[0]- (limitOrderAdjustTicks*TickSize), 0);
				if (orderCount>1)
					m_OrderManager.GoShortLimit(lots,Close[0]- (limitOrderAdjustTicks*TickSize), 1);
				if (orderCount>2)
					m_OrderManager.GoShortLimit(lots,Close[0]- (limitOrderAdjustTicks*TickSize), 2);  
			}else{
				m_OrderManager.GoShortMarket(lots, 0);
				if (orderCount>1)
					m_OrderManager.GoShortMarket(lots, 1);
				if (orderCount>2)
					m_OrderManager.GoShortMarket(lots, 2);  
			}
        }

        private void debug (String message)
        {
            this.Log(message,NinjaTrader.Cbi.LogLevel.Information);
        }
		protected virtual double CalcStopPrice(double initPrice){
			return initPrice;
		}
        protected virtual void ManageOrders()
        {
			double stopPrice=Position.AvgPrice;//CalcStopPrice(Position.AvgPrice);
			
			double trailingUp=0;//ATR(TrailingExitPeriods)[0];
			double trailingDown=0;//ATR(TrailingExitPeriods)[0];
			if (TrailingExitStrat==MultiOrderExitStrategy.TRAILING_STOP_ATR){
         		 trailingUp=Low[0] - ATR(TrailingExitPeriods)[0]*TrailingExitFactor;
				 trailingDown=High[0]+ ATR(TrailingExitPeriods)[0]*TrailingExitFactor;
			}
			else if (TrailingExitStrat==MultiOrderExitStrategy.TRAILING_CANDELIER){
         		 trailingUp=MAX(High,TrailingMaxHighLowPeriods)[0] - ATR(TrailingExitPeriods)[0]*TrailingExitFactor;
				 trailingDown=MIN(Low,TrailingMaxHighLowPeriods)[0] + ATR(TrailingExitPeriods)[0]*TrailingExitFactor;
			}
			else if (TrailingExitStrat==MultiOrderExitStrategy.TRAILING_YOYO){
         		 trailingUp=MAX(Close,TrailingMaxHighLowPeriods)[0] - ATR(TrailingExitPeriods)[0]*TrailingExitFactor;
				 trailingDown=MIN(Close,TrailingMaxHighLowPeriods)[0] + ATR(TrailingExitPeriods)[0]*TrailingExitFactor;
			}else if (TrailingExitStrat==MultiOrderExitStrategy.TRAILING_CUSTOM){
				stopPrice=CalcStopPrice(Position.AvgPrice);
			}
			
            if (stopPrice!= 0 && Position.MarketPosition == MarketPosition.Long)
            {  
				if (trailingUp>0) stopPrice=trailingUp;
				lastShortTrail=0;
				if (stopPrice<lastLongTrail)
					stopPrice=lastLongTrail;
				if (stopPrice<Position.AvgPrice) stopPrice=Position.AvgPrice;
				lastLongTrail=stopPrice;	
				
                if (stopPrice>GetCurrentBid() && GetCurrentBid()>0)   
                        stopPrice=GetCurrentBid()-3 * TickSize;	
				
				if (BE2 && m_OrderManager.GetMarketPosition(0)==MarketPosition.Flat && 
					m_OrderManager.GetMarketPosition(1)==MarketPosition.Long)
					m_OrderManager.ExitSLPT(stopPrice, 0, 1);
				if (BE3 && m_OrderManager.GetMarketPosition(0)==MarketPosition.Flat && 
					m_OrderManager.GetMarketPosition(2)==MarketPosition.Long)
					m_OrderManager.ExitSLPT(stopPrice, 0, 2);
					
				
               /* if (slTicks>target1){
			    int slTicks=Convert.ToInt32((Close[0]-Position.AvgPrice)/TickSize);     
                    if (BE2 && orderCount>1)
                        m_OrderManager.ExitSLPT(stopPrice, 0, 1);

                    if (BE3 && orderCount>2)
                        m_OrderManager.ExitSLPT(stopPrice, 0, 2);

                }*/
                    
            }
            else if (stopPrice!=null && Position.MarketPosition == MarketPosition.Short)
            {
				if (trailingDown>0) stopPrice=trailingDown;
				lastLongTrail=0;
				if (lastShortTrail!=0 && stopPrice>lastShortTrail)
					stopPrice=lastShortTrail;
				if (stopPrice>Position.AvgPrice) stopPrice=Position.AvgPrice;
				
				lastShortTrail=stopPrice;
								
                if (stopPrice<GetCurrentAsk() && GetCurrentAsk()>0)  //RENKO BARS: During replay data, GetCurrentAsk = Close, and may generate order on wrong side of market here
                        stopPrice=GetCurrentAsk() + 3 * TickSize;
				
				if (BE2 && m_OrderManager.GetMarketPosition(0)==MarketPosition.Flat && 
					m_OrderManager.GetMarketPosition(1)==MarketPosition.Short)
					m_OrderManager.ExitSLPT(stopPrice, 0, 1);
				if (BE3 && m_OrderManager.GetMarketPosition(0)==MarketPosition.Flat && 
					m_OrderManager.GetMarketPosition(2)==MarketPosition.Short)
					m_OrderManager.ExitSLPT(stopPrice, 0, 2);
					/*
                int slTicks=Convert.ToInt32((Position.AvgPrice-Close[0])/TickSize);
                if (slTicks>target1){
                    if (BE2 && orderCount>1)
                        m_OrderManager.ExitSLPT(stopPrice, 0, 1);

                    if (BE3 && orderCount>2)
                        m_OrderManager.ExitSLPT(stopPrice, 0, 2);
                }*/
                
            
            }else{
				lastLongTrail=0;
				lastShortTrail=0;
			}
            
        }
        
        private void DisplayStatusBox() 
        {
            String TextMessage = "Total Profit: " + Performance.AllTrades.TradesPerformance.Currency.CumProfit.ToString("C");       
            if (Position.MarketPosition != MarketPosition.Flat) {
                    TextMessage += "\nProfit Unrealized: " + Position.GetProfitLoss(Close[0], PerformanceUnit.Currency).ToString("C");                            
            }
            TextMessage += "\nProfit this Day: " + (dayPnl).ToString("C");            
			TextMessage += "\nProfit this Sess: " + (sessionPnl).ToString("C");            			
            TextMessage+="\nNetPos: " +(Position.Quantity).ToString();
			if (maxLossTriggered) TextMessage+="\nMax loss of day triggered";
            DrawTextFixed("", TextMessage, TextPosition.TopLeft,Color.Red, new Font("Arial", 8), Color.Black, Color.LightGray, 100);
        }

            
        protected virtual void OnBarUpdateImpl(){

        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
        	
            if (IsNewDay()) 
			{
                yestPnl = Performance.AllTrades.TradesPerformance.Currency.CumProfit;
				sessionPriorPnl= Performance.AllTrades.TradesPerformance.Currency.CumProfit;
                dayPnl=0;
				sessionPnl=0;
				trailingPnl=-sessionMaxLoss;
				maxLossTriggered=false;
            }else{
                dayPnl=Performance.AllTrades.TradesPerformance.Currency.CumProfit-yestPnl;
				sessionPnl=Performance.AllTrades.TradesPerformance.Currency.CumProfit-sessionPriorPnl;
				if (trailSessionMaxLoss && ((sessionPnl-sessionMaxLoss)>trailingPnl))trailingPnl=sessionPnl-sessionMaxLoss;
            }
			DisplayStatusBox();
			
			if (sessionPnl<trailingPnl){
				maxLossTriggered=true;
				if (Position.MarketPosition != MarketPosition.Flat) ExitAll();
				return;
			}			            

            if (IgnoreTimeFilter || (ToTime(Time[0])<= SessionOneEnd && ToTime(Time[0]) >= SessionOneStart)||
				(ToTime(Time[0])<= SessionTwoEnd && ToTime(Time[0]) >= SessionTwoStart))
            {            
                ManageOrders();
				if (CalculateOnBarClose || FirstTickOfBar)
               		 OnBarUpdateImpl();
        
            }else{
                ExitAll();
				sessionPriorPnl= Performance.AllTrades.TradesPerformance.Currency.CumProfit;
				sessionPnl=0;
				trailingPnl=-sessionMaxLoss;
            }
               
         }

         protected void ExitAll(){
            m_OrderManager.ExitMarket(0);
            if (orderCount>1)
                m_OrderManager.ExitMarket(1);
            if (orderCount>2)
                m_OrderManager.ExitMarket(2);
         }
        

        #region Properties

        
        [Description("target1")]
        [Category("Parameters")]
        public int Target1
        {
            get { return target1; }
            set { target1 = Math.Max(1, value); }
        }
        [Description("target2")]
        [Category("Parameters")]
        public int Target2
        {
            get { return target2; }
            set { target2 = Math.Max(1, value); }
        }
        
        
        [Description("target3")]
        [Category("Parameters")]
        public int Target3
        {
            get { return target3; }
            set { target3 = Math.Max(1, value); }
        }   
        [Description("stop")]
        [Category("Parameters")]
        public int Stop
        {
            get { return stop; }
            set { stop = Math.Max(1, value); }
        }
        [Description("breakeven2")]
        [Category("Parameters")]
        public bool BE2
        {
            get { return be2; }
            set { be2 = value; }
        }
        
        [Description("breakeven3")]
        [Category("Parameters")]
        public bool BE3
        {
            get { return be3; }
            set { be3 = value; }
        }
        [Description("Ignore Time Filter")]
        [Category("Parameters")]
        public bool IgnoreTimeFilter
        {
            get { return ignoreTimeFilter; }
            set { ignoreTimeFilter = value; }
        }		
        [Description("trailSessionMaxLoss")]
        [Category("Parameters")]
        public bool TrailSessionMaxLoss
        {
            get { return trailSessionMaxLoss; }
            set { trailSessionMaxLoss = value; }
        }
        [Description("useLimitOrders")]
        [Category("Parameters")]
        public bool UseLimitOrders
        {
            get { return useLimitOrders; }
            set { useLimitOrders = value; }
        }
        
         [Description("lots")]
        [GridCategory("Parameters")]
        public int Lots
        {
            get { return lots; }
            set { lots = Math.Max(1, value); }
        }
         [Description("orderCount")]
        [GridCategory("Parameters")]
         public int OrderCount
        {
            get { return orderCount; }
            set { if (value > 3) orderCount = 3; else  orderCount = Math.Max(1, value); }
        }
        
         [Description("Daily Profit Target")]
        [GridCategory("Parameters")]
        public int SessionMaxLoss
        {
            get { return sessionMaxLoss; }
            set { sessionMaxLoss = Math.Max(0, value); }
        }
        
        
        [Description("Time to start trading")]
        [GridCategory("Parameters")]
        public int SessionOneStart
        {
            get { return sessionOneStart; }
            set { sessionOneStart = value; }
        }
        
        
        [Description("Time to stop trading")]
        [GridCategory("Parameters")]
        public int SessionOneEnd
        {
            get { return sessionOneEnd; }
            set { sessionOneEnd = value; }
        }
		
		
        
        
        [Description("Time to start trading")]
        [GridCategory("Parameters")]
        public int SessionTwoStart
        {
            get { return sessionTwoStart; }
            set { sessionTwoStart = value; }
        }
        
        
        [Description("Time to stop trading")]
        [GridCategory("Parameters")]
        public int SessionTwoEnd
        {
            get { return sessionTwoEnd; }
            set { sessionTwoEnd = value; }
		}
		
        [Description("limitOrderAdjustTicks")]
        [GridCategory("Parameters")]
        public int LimitOrderAdjustTicks
        {
            get { return limitOrderAdjustTicks; }
            set { limitOrderAdjustTicks = value; }
		}
        [Description("")]
        [GridCategory("Parameters")]		
        public MultiOrderExitStrategy TrailingExitStrat
        {
            get { return trailingExit; }
            set { trailingExit = value; }
		}
       [Description("")]
        [GridCategory("Parameters")]
        public double TrailingExitFactor
        {
            get { return trailingExitFactor; }
            set { trailingExitFactor = Math.Max(0, value); }
        }
	
       [Description("")]
        [GridCategory("Parameters")]
        public int TrailingExitPeriods
        {
            get { return trailingExitPeriods; }
            set { trailingExitPeriods = Math.Max(0, value); }
        }
		
       [Description("")]
        [GridCategory("Parameters")]
        public int TrailingMaxHighLowPeriods
        {
            get { return maxHighLowPeriods; }
            set { maxHighLowPeriods = Math.Max(0, value); }
        }
        #endregion
    }
}
