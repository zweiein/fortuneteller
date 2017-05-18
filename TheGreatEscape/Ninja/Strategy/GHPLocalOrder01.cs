#region Using declarations
using System;
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

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    /// <summary>
    /// [NJAMC] 2011-07-07: v0.10, initial release
    /// [NJAMC] 2012-04-01: m_BarOfEntry set to currentbar on creation of LocalOrder
    /// [NJAMC] 2012-04-16: Added Good Until ____ features, Time, bars, Ticks, etc
	/// [NJAMC] 2012-04-17: Added Rejected Order rules
	/// 	DO NOT SELL THIS CODE OR DISTRIBUTE OUTSIDE OF BMT's Elite Forum Section
	///
	/// 						
	/// 
	/// </summary>
	/// 
	public class LocalOrder 
	{
		public static string 	REV_CLASSLOCALORDER="01b";

		private Strategy m_Strat;
		private IOrder	m_Order;
		private int 	m_PositionNumber=0;
		private int 	m_PositionQuantity=0;	// Added to maintain "actual" position for order due to partial entry/exit ORDER is read only
		private bool	m_Canceled=false;
		private string	m_SignalName;
		private string	m_OCOLabel;
		private int 	m_ManagementType;	// 0 - Market, 1 - Best Market, 2- Stop Market, 3- Stop Limit
		private int		m_ActionOnReject; 	// 0 - None, 1 - Convert to Market, 2 - Back limit out by Ask/Buy + 1 tick
		private double 	m_EntryPrice;
		private double	m_TriggerPrice;
		private int		m_BarOfEntry;
		// Good Until....
		private OrderRules m_OrderRules=new OrderRules();

		private DateTime m_EnterDateTime;	// Time upon Entry
		/*	
		private bool	m_ConvertExpiredToMarket;	// Good Until rules: 0-GTC, 1-Follow first violated, 2-All rules need to be violated
		private int		m_GoodUntilRule;	// Good Until rules: 0-GTC, 1-Follow first violated, 2-All rules need to be violated
		private int		m_BarGoodUntil;	// Order Should be canceled after this many bars 1=Current bar
		private int		m_TicksAstray;	// Number of ticks
		private DateTime m_EnterDateTime;	// Time upon Entry
		private TimeSpan m_GoodSpan;	// Time upon
		*/
		//
		private int		m_RejectOrderRule; // 0-Do Nothing, 1-???????
		//
		private MarketPosition m_MarketPosition;
		
		public LocalOrder(Strategy m_strategy) {
			m_MarketPosition=MarketPosition.Flat; m_RejectOrderRule=0;
			m_Canceled=false; 
			m_EnterDateTime=DateTime.MinValue; 
			m_Strat=m_strategy; m_Order=null; m_PositionNumber=0; m_OCOLabel=""; m_BarOfEntry=m_Strat.CurrentBar;}
		public LocalOrder(Strategy m_strategy,int m_Pos) { 
			m_MarketPosition=MarketPosition.Flat; m_RejectOrderRule=0;
			m_Canceled=false; 
			m_EnterDateTime=DateTime.MinValue; 
			m_Strat=m_strategy; m_Order=null; m_PositionNumber=m_Pos; m_OCOLabel=""; m_BarOfEntry=m_Strat.CurrentBar;}

		public OrderRules OrderRules
		{
			get { return m_OrderRules; }  set { m_OrderRules=value;}
		}
		public IOrder Order
		{
			get { return m_Order; }  set { m_Order=value;}
		}
		public int BarOfEntry
		{
			get { return m_BarOfEntry; }  set { m_BarOfEntry=value;}
		}
		public int PositionQuantity // Used for active order to maintain size of position on market/ work-around READONLY ORDER
		{
			get { return m_PositionQuantity; }  set { m_PositionQuantity=value;}
		}		
		public int PositionNumber
		{
			get { return m_PositionNumber; }
		}	
		public bool Canceled
		{
			get { return m_Canceled; }
		}	
		public string SignalName
		{
			get { return Order.Name; }
		}
		public string OCOGroup
		{
			get { return m_OCOLabel; }
		}
		public double GoodUntilMilliseconds
		{
			get { return m_OrderRules.GoodTimeSpanMS; }
		}
		public bool GoodUntilPending
		{
			get { return m_OrderRules.GoodUntilRule>0; }
		}
		public bool ConvertCanceledToMarket
		{
			get { return m_OrderRules.ConvertExpiredToMarket; }
		}		
		public void CancelOrder()
		{
			if (IsOrderActive())
			{
				m_Canceled=true;
				if (m_Strat.TraceOrders)
					m_Strat.Print(m_Strat.Name+" "+m_Strat.Instrument.FullName+" CANCELLING LOCALORDER: "+m_Order.ToString());
				m_Strat.CancelOrder(m_Order);
			}
			else
				m_Canceled=true;
		}
		public bool IsOrderActive()
		{
			if ((m_Order!=null) && (!m_Canceled))
			{
					switch (m_Order.OrderState)
					{
						case (OrderState.Accepted):
							return true;
						case (OrderState.PartFilled):
							return true;
						case (OrderState.PendingChange):
							return true;
						case (OrderState.PendingSubmit):
							return true;
						case (OrderState.Working):
							return true;
						default:
							return false;
					}
			}
				
			return false;	// Order not valid
		}
		public MarketPosition GetMarketPosition()
		{
			if ((m_Order!=null) && (!m_Canceled))
			{
				if (m_Order.Filled>0)
					switch (m_Order.OrderAction)
					{
						case (OrderAction.Buy):
							return MarketPosition.Long;
						case (OrderAction.SellShort):
							return MarketPosition.Short;
						default:
							return MarketPosition.Flat;
					}
			}
			return MarketPosition.Flat;
		}
		
		public void GoodUntilAll(int m_GoodBars,double m_ExpireSeconds,int m_SetTicksAstray,bool m_ConvertCanceledToMarketOrder)
		{
			m_OrderRules.SetGoodUntilAll(m_GoodBars,m_ExpireSeconds,m_SetTicksAstray,m_ConvertCanceledToMarketOrder);
		}
		
		public void GoodUntilAny(int m_GoodBars,double m_ExpireSeconds,int m_SetTicksAstray,bool m_ConvertCanceledToMarketOrder)
		{
			m_OrderRules.SetGoodUntilAny(m_GoodBars,m_ExpireSeconds,m_SetTicksAstray,m_ConvertCanceledToMarketOrder);
		}
		
		public bool Expired()
		{ // Expired TRUE time to cancel, FALSE still good
			bool m_ReturnVal=false;
			bool m_BarExpired=false;
			bool m_TimeExpired=false;
			bool m_TickExpired=false;
			
			if (m_OrderRules.GoodUntilRule>0)
			{
				m_Strat.Print("OrderCancel Check: "+m_OrderRules.GoodTimeSpanMS+" ms   "+m_EnterDateTime+"  Spanned Time: "+m_EnterDateTime.Add(m_OrderRules.GoodTimeSpan)+"  NOW: "+DateTime.Now);
				m_Strat.Print("OrderCancel Bars: "+m_Strat.CurrentBar+" Cbar   "+m_BarOfEntry+" EntryBar  Spanned Time: "+(m_BarOfEntry+m_OrderRules.BarGood-1)+"  NOW: "+DateTime.Now);
				
				if (((m_BarOfEntry+m_OrderRules.BarGood-1)<m_Strat.CurrentBar) && (m_OrderRules.BarGood>0))
					m_BarExpired=true;

				if ((m_OrderRules.GoodTimeSpanMS>0.0) && 
					(m_EnterDateTime.Add(m_OrderRules.GoodTimeSpan)<=DateTime.Now))
				{
					m_TimeExpired=true;
				m_Strat.Print("OrderCancel Time EXPIRED: "+m_EnterDateTime+"  Spanned Time: "+m_EnterDateTime.Add(m_OrderRules.GoodTimeSpan));
				}

				if (m_OrderRules.TicksAstray!=0 && false) // TODO: Insert complex Ticks Astray logic for cancel
				{
					m_TickExpired=true;
				}
				// OR all conditions
				if ((m_OrderRules.GoodUntilRule==1) && ((m_BarExpired) || (m_TimeExpired) ||(m_TickExpired)))
					m_ReturnVal=true;
				// AND all conditions
				if ((m_OrderRules.GoodUntilRule==2) && (m_BarExpired || (m_OrderRules.BarGood==0)) 
					&& (m_TimeExpired || (m_OrderRules.GoodTimeSpanMS==0)) && (m_TickExpired))
					m_ReturnVal=true;
				
				//if (m_ReturnVal)
				//	m_OrderRules.GoodUntilRule=0;
			//m_GoodUntilRules=0-GTC, 1-Any Expired, 2-All Expired
			}
			//
			
			return m_ReturnVal;
		}			
		
		public bool SubmitOrder(int barsInProgressIndex, OrderAction orderAction, OrderType orderType,
			int quantity, double limitPrice, double stopPrice, string ocoId, string signalName)
		{
			return 	SubmitOrder(barsInProgressIndex, orderAction, orderType, quantity,
				limitPrice, stopPrice, ocoId, signalName, 0);
		}
		
		public bool SubmitOrder(int barsInProgressIndex, OrderAction orderAction, OrderType orderType,
			int quantity, double limitPrice, double stopPrice, string ocoId, string signalName,int m_OnlyUpdateDirection)
		{
			bool m_ret=false;
			
			if (m_Canceled) // Order canceled but update attempted, could be a race condition
				return false;
			
			limitPrice=m_Strat.Instrument.MasterInstrument.Round2TickSize(limitPrice);
			stopPrice=m_Strat.Instrument.MasterInstrument.Round2TickSize(stopPrice);
			
			m_BarOfEntry=m_Strat.CurrentBar;  // Update CurrentBar on order, refreshed with update
			m_EnterDateTime=DateTime.Now; // Update Time on order, refreshed with update
			
			m_OCOLabel=ocoId;			
			
			if (m_Order==null)
			{
				m_Order=m_Strat.SubmitOrder(barsInProgressIndex, orderAction, orderType,
			 					quantity,  limitPrice,  stopPrice,  ocoId,  signalName);
				if (m_Strat.TraceOrders)
					m_Strat.Print(m_Strat.Name+" "+m_Strat.Instrument.FullName+" LocalOrderSubmitted: "+m_Order.ToString());
			}
			else
			{
				bool m_OkayToUpdate=false;
				//m_Strat.Print("Update Order "+m_OnlyUpdateDirection+
				//"  "+m_Order.LimitPrice+" "+);
				switch (m_OnlyUpdateDirection)
				{
					case 1:	// Only increase Prices
					{
						if (m_Order.LimitPrice<limitPrice)
							m_OkayToUpdate=true;
						else
							limitPrice=m_Order.LimitPrice;  // Don't change
						
						if (m_Order.StopPrice<stopPrice)
							m_OkayToUpdate=true;
						else
							stopPrice=m_Order.StopPrice;  // Don't change							
						break;
					}
					case -1: // Only decrease Prices
					{
						if (m_Order.LimitPrice>limitPrice)
							m_OkayToUpdate=true;
						else
							limitPrice=m_Order.LimitPrice;  // Don't change
						
						if (m_Order.StopPrice>stopPrice)
							m_OkayToUpdate=true;
						else
							stopPrice=m_Order.StopPrice;  // Don't change							
						break;					
					}
					default:
					{
						m_OkayToUpdate=true;
						break;
					}
				}
				
				if (((m_Order.Quantity!=quantity) || (m_Order.LimitPrice!=limitPrice) 
					|| (m_Order.StopPrice!=stopPrice)) && (m_OkayToUpdate))
				{
					m_Strat.ChangeOrder(m_Order, quantity,  limitPrice,  stopPrice);
				}
				else
				{
					if (m_Strat.TraceOrders)
						m_Strat.Print(m_Strat.Name+" "+m_Strat.Instrument.FullName+" Order Not updated, same Quanity, Limit & Stop price as last order: "+m_Order.ToString());
				}
			}
						
			if (m_Order!=null)
				m_ret=true;
			
			return m_ret;
		}
	}
}
