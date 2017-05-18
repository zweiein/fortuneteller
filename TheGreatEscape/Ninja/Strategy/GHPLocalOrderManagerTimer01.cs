#region Using declarations
using System;
using System.Timers;
using System.ComponentModel;
using System.Collections.Generic;
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
//------------------------------------------------------------------------------
//  LOM Update Record (Timer Section)
//
//	02/25/2012 - NJAMC - As Issued
//	04/01/2012 - NJAMC - Timer Feature partially functional, not attached to LOM features, more testing required
//	04/09/2012 - NJAMC - Timer Feature partially functional, not attached to LOM features correct approach
//	
//


// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{

	public partial class LocalOrderManager 
	{
		string REV_TIMER01="01";

		//private System.Windows.Forms.Timer m_LOMtimer = new System.Windows.Forms.Timer();
		private System.Timers.Timer m_LOMtimer=null; // From System.Timers
		private Object	m_TimerLockObject	= new Object();
		
		public void SetupTimer(int m_milliseconds)
		{
			if ((m_Strat.Historical) ||(!m_EnableInternalTimer))
				return;	// Ignore timer requests in Historical Mode
			
			if (m_LOMtimer==null)
			{
				m_LOMtimer = new System.Timers.Timer();
				m_LOMtimer.Elapsed+=new ElapsedEventHandler(TimerEventProcessor);
				//m_LOMtimer.Elapsed += (sender, args) => TimerEventProcessor("myCallHere", args); 
			}
			if (!m_LOMtimer.Enabled)
			{
			m_LOMtimer.Interval=m_milliseconds;
			//m_LOMtimer.Elapsed+=new ElapsedEventHandler(TimerEventProcessor);
			//m_LOMtimer.Elapsed+=delegate { TimerEventProcessor(m_milliseconds); }; 
			m_LOMtimer.AutoReset=true; 	// Must Start() for each interval;
			m_LOMtimer.Start();
			}
		}	
		
		
		// Your timer object's tick event handler
		void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
		{
			try
			{
				//int m_count;
				//m_count=CheckForExpiredOrders();
				//m_Strat.Print("testing  Count="+GetGoodUntilCount());
				m_Strat.TriggerCustomEvent(MyCustomHandler, m_BarsInProgress, myObject);
			}
			catch (Exception e)
			{
				m_Strat.Print("Exception CustomHandler "+e.ToString());
			}
		}
		
		private void MyCustomHandler(object state)
		{
			int m_count;
			
			if (!m_Strat.Running)
			{
            	m_Strat.Print(m_UniqueID+" MyCustomHandler(Stopping Timer): "+((string) state));
            	m_Strat.Log(m_UniqueID+" MyCustomHandler(Stopping Timer): "+((string) state),NinjaTrader.Cbi.LogLevel.Information);
				m_LOMtimer.Stop();
				m_LOMtimer.Dispose();
				m_LOMtimer=null;
			}
			try
			{
				if ( GetGoodUntilCount()>0)
					m_count=CheckForExpiredOrders();
				else
					m_LOMtimer.Stop();
			}
			catch (Exception e)
			{
            	m_Strat.Log("Exception CustomHandler "+e.ToString(),NinjaTrader.Cbi.LogLevel.Error);
				m_Strat.Print("Exception CustomHandler "+e.ToString());
			}
				
			//Trace.TraceError("Custom Handle Called" + state+"  m_Strat "+m_Strat.Name);			
			//if (GetGoodUntilCount()==0)
			//	m_LOMtimer.Stop();
			//Trace.Assert(false,(string)state.ToString(),"value of m_Strat = "+m_Strat.ToString());
            //m_Strat.Print(m_UniqueID+" MyCustomHandler(TRIGGERED & PRINTING): "+((string) state));
			//ExitMarket(0);
/*			if (GetMarketPosition()==MarketPosition.Flat)
			{
            	m_Strat.Print(m_UniqueID+" MyCustomHandler(Moving Since Flat): "+((string) state));
				GoMarketBracket(1,3,0);
				GoMarketBracket(1,5,1);
				GoMarketBracket(1,6,2);
			}*/
			//ExitSLPT(m_Strat.GetCurrentBid()-5*m_Strat.TickSize,m_Strat.GetCurrentAsk()+5*m_Strat.TickSize,0,true);
		}
	}  	
}
