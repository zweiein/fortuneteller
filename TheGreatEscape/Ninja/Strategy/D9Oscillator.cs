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
    /// Strategy based on d9 particle oscillator
    /// </summary>
    [Description("Strategy based on d9 particle oscillator")]
    public class D9OscillatorLocalOrder : Strategy
    {
        #region Variables
        // Wizard generated variables
        private int d9Period = 10; // Default setting for D9Period
        private int d9Phase = 0; // Default setting for D9Phase
        private int startTradeTime = 070000;
        private int stopTradeTime = 220000;
		
		private int		target1			= 17;
		private int		target2			= 30;
		private int		target3			= 100;
		
		
		private int		stop			= 25;
		
		private bool	be2				= true;
		private bool	be3				= true;
		
		private int     lots           = 1;
		
		private int     dailyProfit   = 500;
		
		private LocalOrderManager m_OrderManager=null;
		
       
      
		
        // User defined variables (add any user defined variables below)
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            Add(d9ParticleOscillator_V2(D9Period, D9Phase));
            Add(d9ParticleOscillator_V2(D9Period, D9Phase));
			
            m_OrderManager = new LocalOrderManager(this, 0);
            m_OrderManager.SetDebugLevels(0, 0, false, 0);
            m_OrderManager.SetStatsBoxVisable(true);
            m_OrderManager.SetAutoSLPTTicks(Stop, Target1, 0);
			m_OrderManager.SetAutoSLPTTicks(Stop, (Target1+Target2), 1);
			m_OrderManager.SetAutoSLPTTicks(Stop, (Target1+Target2+Target3), 2);
            IgnoreOverFill = true;
		
			EntryHandling = EntryHandling.UniqueEntries; 
		    CalculateOnBarClose = true;
             Slippage = 1;
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
		
		private void GoLong()
		{
            m_OrderManager.SetAutoSLPTTicks(Stop, Target1, 0);
			m_OrderManager.SetAutoSLPTTicks(Stop, (Target1+Target2), 1);
			m_OrderManager.SetAutoSLPTTicks(Stop, (Target1+Target2+Target3), 2);
		
			m_OrderManager.GoLongMarket(lots, 0);
			m_OrderManager.GoLongMarket(lots, 1);
			m_OrderManager.GoLongMarket(lots, 2);			
		}
		private void GoShort()
		{
			
            m_OrderManager.SetAutoSLPTTicks(Stop, Target1, 0);
			m_OrderManager.SetAutoSLPTTicks(Stop, (Target1+Target2), 1);
			m_OrderManager.SetAutoSLPTTicks(Stop, (Target1+Target2+Target3), 2);			
			
			m_OrderManager.GoShortMarket(lots, 0);
			m_OrderManager.GoShortMarket(lots, 1);
			m_OrderManager.GoShortMarket(lots, 2);						
		}
		private void debug (String message)
		{
			this.Log(message,NinjaTrader.Cbi.LogLevel.Information);
		}
		private void ManageOrders()
		{
			int slTicks=12;
			if (Position.MarketPosition == MarketPosition.Long)
			{
			
				slTicks=Convert.ToInt32((Close[0]-Position.AvgPrice)/TickSize);
			//	debug("Updating SL to " + slTicks);
				if (slTicks>0){
					if (BE2 && High[0] > Position.AvgPrice + (Target1*TickSize))
						//m_OrderManager.UpdateSLTicks(slTicks,1);
					m_OrderManager.GoShortStop(lots,slTicks,1);
					
					if (BE3 && High[0] > Position.AvgPrice + ((Target1+Target2)*TickSize))
						//m_OrderManager.UpdateSLTicks(slTicks,2);
					m_OrderManager.GoShortStop(lots,slTicks,2);
				}
					
			}
			if (Position.MarketPosition == MarketPosition.Short)
			{
				slTicks=Convert.ToInt32((Position.AvgPrice-Close[0])/TickSize);
				//debug("Updating SL to " + slTicks);Print("Updating SL to " + slTicks);
				if (slTicks>0){
					if (BE2 && Low[0] < Position.AvgPrice - (Target1*TickSize))
					//	m_OrderManager.UpdateSLTicks(slTicks,1);
						m_OrderManager.GoLongStop(lots,slTicks,1);
						
				
					
					if (BE3 && Low[0] < Position.AvgPrice - ((Target1+Target2)*TickSize))
					//	m_OrderManager.UpdateSLTicks(slTicks,2);
						m_OrderManager.GoLongStop(lots,slTicks,2);
				}
				
			
			}
			
		}

            
        

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
		

            if ((ToTime(Time[0])<= stopTradeTime && ToTime(Time[0]) >= startTradeTime))
            {
			
				ManageOrders();
				
				// Condition set 1
				if (CrossAbove(d9ParticleOscillator_V2(D9Period, D9Phase).Prediction, 0, 1))
				{
					GoLong();
				}

				// Condition set 2
				if (CrossBelow(d9ParticleOscillator_V2(D9Period, D9Phase).Prediction, 0, 1))
				{
					GoShort();
				}
				
				// Condition set 3
				if (Performance.AllTrades.TradesPerformance.Currency.CumProfit >= 500)
				{
					Alert("Account Alert", Priority.High, "Target Achieved", @"C:\Program Files (x86)\NinjaTrader 7\sounds\Alert1.wav", 0, Color.White, Color.Black);
				}
			}else{
				m_OrderManager.ExitMarket(0);
				m_OrderManager.ExitMarket(1);
				m_OrderManager.ExitMarket(2);
			}
			   
		 }
		
		

        #region Properties
        [Description("D9 Period")]
        [GridCategory("Parameters")]
        public int D9Period
        {
            get { return d9Period; }
            set { d9Period = Math.Max(1, value); }
        }

        [Description("D9 Phase")]
        [GridCategory("Parameters")]
        public int D9Phase
        {
            get { return d9Phase; }
            set { d9Phase = Math.Max(0, value); }
        }
		
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
		
		 [Description("lots")]
        [GridCategory("Parameters")]
        public int Lots
        {
            get { return lots; }
            set { lots = Math.Max(1, value); }
        }
		
		 [Description("Daily Profit Target")]
        [GridCategory("Parameters")]
        public int DailyProfit
        {
            get { return dailyProfit; }
            set { dailyProfit = Math.Max(1, value); }
        }
		
		
        [Description("Time to start trading")]
        [GridCategory("Parameters")]
        public int StartTradeTime
        {
            get { return startTradeTime; }
            set { startTradeTime = value; }
        }
        
        
        [Description("Time to stop trading")]
        [GridCategory("Parameters")]
        public int StopTradeTime
        {
            get { return stopTradeTime; }
            set { stopTradeTime = value; }
        }


        
        #endregion
    }
}
