using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using valutatrader;

namespace NinjaTrader.Strategy
{	
    public abstract class VTStrategyBase :  Strategy
    {
        #region Variables
        // Wizard generated variables
            private int sl = 30; // Default setting for TicksSL
            private int pt = 5;
            private LocalOrderManager m_OrderManager;
        #endregion


        virtual protected void initOrderManager()
        {
            //allow a maximum of 1 entries while a position is open
            EntriesPerDirection = 1;
            EntryHandling = EntryHandling.AllEntries;
            IncludeCommission = true;
            Slippage = 1;
            CalculateOnBarClose = false;
            Unmanaged = true;
            ExitOnClose=false;
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
        
        public LocalOrderManager getOrderManager(){
            return m_OrderManager;
        }

        protected void EnterShortLim(double price){
         
            if (this.GetCurrentBid()>0 && price<this.GetCurrentBid())
                price=this.GetCurrentAsk();
              m_OrderManager.GoShortLimit((int)1,(double)price,(int)0) ; 
            
        }
        protected void EnterLongLim(double price){
          
            if (this.GetCurrentAsk()>0 && price>this.GetCurrentAsk())
                price=this.GetCurrentBid();
             m_OrderManager.GoLongLimit((int)1,(double)price,(int)0) ; 

        }   

        [Description("target")]
        [Category("Parameters")]
        public int ProfitTarget
        {
            get { return pt; }
            set { pt = Math.Max(0, value); }
        }
        [Description("sl")]
        [Category("Parameters")]
        public int StopLoss
        {
            get { return sl; }
            set { sl = Math.Max(0, value); }
        }

    }
}