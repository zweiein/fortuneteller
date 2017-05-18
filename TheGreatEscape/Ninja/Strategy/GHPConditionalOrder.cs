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
    /// [NJAMC] 2012-03-30: TrailingStop feature improved
	/// 	DO NOT SELL THIS CODE OR DISTRIBUTE OUTSIDE OF BMT's Elite Forum Section
	///
	/// 						
	/// 
	/// </summary>
	/// 
	public class ConditionalOrder 
	{
		public static string 	REV_CLASSCONDORDER="02a";

		private Strategy		m_Strat;
		private int 			m_ConditionType=0;		// 0 None, 1 - Place Order, 2 - Trail Order place ATM
														// 3 - V.PT hit, 4 - V.SL Hit, 5 - Trail Entry, 6 - SL/Indi Based
														// 7 - Place Int Function, 8 - Place Double Function, 9 - PT/Indi Based
		private int 			m_PositionNumber=0;
		private int				m_BarsInProgress=0;
		private OrderAction 	m_OrderAction;
		private OrderType		m_OrderType;
		private int				m_Quantity;
		private int				m_MaxVol=0;				// 0 No Volume Limit, otherwise order held until Bid/Ask <= m_MaxVol
		private double			m_LimitPrice=0.0;
		private	double			m_StopPrice=0.0;
		private string			m_ID;
		private string			m_SignalName;
		private string			m_OCOLabel;
		private string			m_OrderToken;
		
		private OrderRules 		m_OrderRules=new OrderRules();
		
		private int				m_AwaitingPosition=-1;	// -1 for ALL
		private	int				m_AwaitingCondition=-1; // -1 None, 0 Flat, 1 Long, 
				// 2 Short, 3 LocalFlat Global Not Short, 4 LocalFlat Global Not Long, 5 Global Not Flat, 6 Local  Flat
				// 7 Local Not Flat
		
		private int				m_BreakEvenTrigger=-1;	// Number of ticks before setting break even offset
		private int				m_BreakEvenOffset=0;	// Number of ticks offset for Break Even Trigger

		private int				m_TrailTrigger=-1;		// Profit Trigger point
		private int				m_TrailType=-1;			// -1 None, 0 Linear, 1 - % save (Param1 =%)
		private int				m_TrailChaseTicks=1;	    // Ticks to Chase Bid/Ask
		private int				m_TrailChaseTicksLimit=1;	// Ticks to Chase Bid/Ask in reference to More profit
        // CKKOH
        private bool            m_permTriggered = false;
		
		private double			m_Param1=0.0;			// Param for general use (double)
		private int				m_IParam1=0;			// Param for general use (int)
		private bool			m_bParam1=false;		// Param for general use (bool)
		private double			m_Param2=0.0;			// Param for general use (double)
		private int				m_IParam2=0;			// Param for general use (int)
		private bool			m_bParam2=false;		// Param for general use (bool)
		private double			m_Param3=0.0;			// Param for general use (double)
		private double			m_Param4=0.0;			// Param for general use (double)
		private OrderFunctionType m_FunctionType=OrderFunctionType.NONE;
		
		private int 			m_Direction=0;			// 0-NA/None, 1 - Long, -1 - Short
		
		private bool			m_Canceled=false;

		private int 	m_ManagementType;	// 0 - Market, 1 - Best Market, 2- Stop Market, 3- Stop Limit
		private int		m_ActionOnReject; 	// 0 - None, 1 - Convert to Market, 2 - Back limit out by Ask/Buy + 1 tick
		private double 	m_EntryPrice;
		private double	m_TriggerPrice;
		
		public ConditionalOrder(Strategy m_strategy) {
			m_Canceled=false;
			m_Strat=m_strategy; m_PositionNumber=0; m_OCOLabel="";m_OrderRules.Disabled(true); }
		public ConditionalOrder(Strategy m_strategy,int m_Pos) { 
			m_Canceled=false;
			m_Strat=m_strategy; m_PositionNumber=m_Pos; m_OCOLabel="";m_OrderRules.Disabled(true); }
		public string ID
		{
			get { return m_ID; }  set { m_ID=value;}
		}
		public OrderRules	OrderRulesValue
		{
			get { return m_OrderRules; }  set { m_OrderRules=value;}
		}
		public int	MaxVolumeBeforeRelease
		{
			get { return m_MaxVol; }  set { m_MaxVol=value;}
		}	
		public int	ConditionType
		{
			get { return m_ConditionType; }  set { m_ConditionType=value;}
		}	
		public int	AwaitingPosition
		{
			get { return m_AwaitingPosition; }  set { m_AwaitingPosition=value;}
		}	
		public int	AwaitingCondition
		{
			get { return m_AwaitingCondition; }  set { m_AwaitingCondition=value;}
		}
		public int	BreakEvenTrigger
		{
			get { return m_BreakEvenTrigger; }  set { m_BreakEvenTrigger=value;}
		}	
		public int	BreakEvenOffset
		{
			get { return m_BreakEvenOffset; }  set { m_BreakEvenOffset=value;}
		}	
		public OrderFunctionType FunctionType
		{
			get { return m_FunctionType; }  set { m_FunctionType=value;}
		}	
		public double	Param1
		{
			get { return m_Param1; }  set { m_Param1=value;}
		}
		public int	IParam1
		{
			get { return m_IParam1; }  set { m_IParam1=value;}
		}
		public bool	bParam1
		{
			get { return m_bParam1; }  set { m_bParam1=value;}
		}
		public double	Param2
		{
			get { return m_Param2; }  set { m_Param2=value;}
		}
		public int	IParam2
		{
			get { return m_IParam2; }  set { m_IParam2=value;}
		}
		public bool	bParam2
		{
			get { return m_bParam2; }  set { m_bParam2=value;}
		}
		public double	Param3
		{
			get { return m_Param3; }  set { m_Param3=value;}
		}
		public double	Param4
		{
			get { return m_Param4; }  set { m_Param4=value;}
		}
		public int	TrailTrigger
		{
			get { return m_TrailTrigger; } set { m_TrailTrigger=value;}
		}	
		public int TrailChaseTicks
		{
			get { return m_TrailChaseTicks; }
		}	
		public int TrailChaseTicksLimit
		{
			get { return m_TrailChaseTicksLimit; }
		}	
		public int PositionNumber
		{
			get { return m_PositionNumber; }
		}			
		public int Direction
		{
			get { return m_Direction; }
		}
		public int Quantity
		{
			get { return m_Quantity; }
		}
		public int TrailType
		{
			get { return m_TrailType; }
		}	
		public bool Canceled
		{
			get { return m_Canceled; }
		}	
		public string OCOGroup
		{
			get { return m_OCOLabel; }
		}
		public string SignalName
		{
			get { return m_SignalName; }
		}		
		public void CancelOrder()
		{
				m_ConditionType=0;
				m_Canceled=true;
		}
        public bool PermTriggered //CKKOH
        {
            get { return m_permTriggered; }
            set { m_permTriggered = value;}
        }
#region GetMarketPosition()
		public MarketPosition GetMarketPosition()
		{
			MarketPosition m_MP;
			
			m_MP=MarketPosition.Flat;
			
			if (!m_Canceled)
			{
				if (m_Quantity>0)
					switch (m_OrderAction)
					{
						case (OrderAction.Buy):
							m_MP= MarketPosition.Long;
							break;
						case (OrderAction.SellShort):
							m_MP= MarketPosition.Short;
							break;
						default:
							m_MP= MarketPosition.Flat;
							break;
					}
			}
			if ((m_FunctionType!=OrderFunctionType.NONE) && (!m_Canceled))
			{
				switch (m_FunctionType)
				{
					case OrderFunctionType.GoLongLimit:
					case OrderFunctionType.GoLongMarket:
					case OrderFunctionType.GoLongStop:
					case OrderFunctionType.GoLongStopLimit:
					case OrderFunctionType.GoLongTrend:
					case OrderFunctionType.GoShortLimit:
					case OrderFunctionType.GoShortMarket:
					case OrderFunctionType.GoShortStop:
					case OrderFunctionType.GoShortStopLimit:
					case OrderFunctionType.GoShortTrend:
					
					case OrderFunctionType.GoMarketBracket:
					case OrderFunctionType.ExitMarket:
					case OrderFunctionType.ExitSLPT:
						break;
					default:
						break;
				}
			}
			return m_MP;
		}
#endregion		

		public void ConfigureTrailingEntry(int m_Pos,int m_Cond)
		{
			m_ConditionType=5;  // Trailing Entry
			m_AwaitingPosition=m_Pos;
			m_AwaitingCondition=m_Cond;
		}
		public void ConfigureConditionalOrder(int m_Pos,int m_Cond)
		{
			m_ConditionType=1;  // Place Order
			m_AwaitingPosition=m_Pos;
			m_AwaitingCondition=m_Cond;
		}
		public void ConfigureConditionalStopLossOrder(int m_Pos,int m_Cond)
		{
			m_ConditionType=2;  // Trailing order
			m_AwaitingPosition=m_Pos;
			m_AwaitingCondition=m_Cond;
		}
		
#region PendingFunctionType()	- Pending higher level entry/exit fuction	
		public bool PendingFunctionType(OrderFunctionType m_Type,int m_Quant,double m_p1,double m_p2,double m_p3,double m_p4,
			int m_PositionNumber,bool m_AbsoluteLimits,OrderRules m_GU)
		{
			bool m_ret=false;
			
			if (m_Canceled) // Order canceled but update attempted, could be a race condition
				return false;
			
			m_ConditionType=8; // Function INT version
			m_AwaitingPosition=m_PositionNumber;
			m_FunctionType=m_Type;
			m_OrderRules=m_GU;
			
			switch (m_Type)
			{
				case OrderFunctionType.ExitSLPT:
					m_OCOLabel="EXIT"+m_PositionNumber;
					m_AwaitingCondition=5;
					break;
				case OrderFunctionType.GoLimitBracket:
					m_OCOLabel="ENTER"+m_PositionNumber;
					m_AwaitingCondition=0;
					break;
				case OrderFunctionType.GoMarketBracket:
					m_OCOLabel="ENTER"+m_PositionNumber;
					m_AwaitingCondition=0;					
					break;
				case OrderFunctionType.NONE:
					m_OCOLabel="";
					m_Canceled=true;	// Don't know how to handle it yet, dump pending order
					break;
				default:
					m_FunctionType=OrderFunctionType.NONE;
					m_Canceled=true;	// Don't know how to handle it yet, dump pending order
					break;
			}

			m_Param1=m_Strat.Instrument.MasterInstrument.Round2TickSize(m_p1);
			m_Param2=m_Strat.Instrument.MasterInstrument.Round2TickSize(m_p2);
			m_bParam1=m_AbsoluteLimits;
						
			m_BarsInProgress=0;
			m_SignalName="PendingFunctionType-Double-"+m_Type.ToString();
			m_Quantity=m_Quant;
			//m_OrderAction=orderAction;
			//m_OrderType=orderType;
			
			m_Strat.BackColor = Color.FromArgb(99,Color.Yellow);
			
			return m_ret;
		}			

		public bool PendingFunctionType(OrderFunctionType m_Type,int m_Quant,int m_p1,int m_p2,
			int m_PositionNumber,double m_RefPrice,OrderRules m_GU)
		{
			bool m_ret=false;
			
			if (m_Canceled) // Order canceled but update attempted, could be a race condition
				return false;
			
			m_ConditionType=7; // Function INT version
			m_AwaitingPosition=m_PositionNumber;
			m_FunctionType=m_Type;
			m_OrderRules=m_GU;
			
			switch (m_Type)
			{
				case OrderFunctionType.ExitSLPT:
					m_OCOLabel="EXIT"+m_PositionNumber;
					m_AwaitingCondition=5;
					break;
				case OrderFunctionType.GoLimitBracket:
					m_OCOLabel="ENTER"+m_PositionNumber;
					m_AwaitingCondition=0;
					break;
				case OrderFunctionType.GoMarketBracket:
					m_OCOLabel="ENTER"+m_PositionNumber;
					m_AwaitingCondition=0;					
					break;
				case OrderFunctionType.NONE:
					m_OCOLabel="";
					m_Canceled=true;	// Don't know how to handle it yet, dump pending order
					break;
				default:
					m_FunctionType=OrderFunctionType.NONE;
					m_Canceled=true;	// Don't know how to handle it yet, dump pending order
					break;
			}

			m_Param1=m_Strat.Instrument.MasterInstrument.Round2TickSize(m_RefPrice);
			m_IParam1=m_p1;
			m_IParam2=m_p2;
			m_bParam1=false;	// No absolute feature for relative values
			
			m_BarsInProgress=0;
			m_SignalName="PendingFunctionType-Int-"+m_Type.ToString();
			m_Quantity=m_Quant;
			//m_OrderAction=orderAction;
			//m_OrderType=orderType;
			
			m_Strat.BackColor = Color.FromArgb(99,Color.Yellow);
			
			return m_ret;
		}	
#endregion
		
#region PendingOrder()
		public bool PendingOrder(int barsInProgressIndex, OrderAction orderAction, OrderType orderType,
			int quantity, double limitPrice, double stopPrice, string ocoId, string signalName)
		{
			bool m_ret=false;
			
			if (m_Canceled) // Order canceled but update attempted, could be a race condition
				return false;
						
			switch (orderAction)
			{
				case OrderAction.Buy:
					ConfigureConditionalOrder(m_PositionNumber,3);		//Going Long, local flat
					break;
				case OrderAction.SellShort:
					ConfigureConditionalOrder(m_PositionNumber,4);		//Going Short, local flat
					break;
				case OrderAction.Sell:
					break;
				case OrderAction.BuyToCover:
					break;
				default:
					break;
			}

			m_LimitPrice=m_Strat.Instrument.MasterInstrument.Round2TickSize(limitPrice);
			m_StopPrice=m_Strat.Instrument.MasterInstrument.Round2TickSize(stopPrice);
			
			m_OCOLabel=ocoId;
			
			m_BarsInProgress=barsInProgressIndex;
			m_SignalName=signalName;
			m_Quantity=quantity;
			m_OrderAction=orderAction;
			m_OrderType=orderType;
			
			m_Strat.BackColor = Color.FromArgb(99,Color.Yellow);
			
			return m_ret;
		}		
#endregion
		
#region ReleaseOrder()
		public bool ReleaseOrder(LocalOrder m_LocalOrder)
		{
			bool m_ret;
			
			if (m_Canceled) // Order canceled but update attempted, could be a race condition
				return false;
			
			switch(m_OrderRules.GoodUntilRule)
			{ // Go position dependent with m_Current.Position
				case 1: //Good Until Any
					m_LocalOrder.GoodUntilAny(m_OrderRules.BarGood,m_OrderRules.GoodTimeSpanMS,m_OrderRules.TicksAstray,m_OrderRules.ConvertExpiredToMarket);
					break;
				case 2: //Good Until ALL conditions met
					m_LocalOrder.GoodUntilAll(m_OrderRules.BarGood,m_OrderRules.GoodTimeSpanMS,m_OrderRules.TicksAstray,m_OrderRules.ConvertExpiredToMarket);
					break;
				default:
					break;
			}
			m_ret=m_LocalOrder.SubmitOrder(m_BarsInProgress,  m_OrderAction,  m_OrderType,
						 m_Quantity,  m_LimitPrice,  m_StopPrice,  m_OCOLabel,  m_SignalName);
			
			m_Canceled=CheckForNewCondition(ref m_LocalOrder);
			
			return m_ret;
		}
#endregion
		
#region CheckForNewCondition()		
		public bool CheckForNewCondition(ref LocalOrder m_LocalOrder)
		{
			bool m_ret=true; // No new conditional order, allow deletion
			
			if (m_Canceled) // Order canceled but update attempted, could be a race condition
				return true;
			// Check for conditionals that need updating (Trailing Entry, Trailing Exit, etc)
			if (false)
			{
				m_OrderToken=m_LocalOrder.Order.Token;
			}
			
			return m_ret;
		}		
#endregion
		
#region SetTrailingEntry...
		public bool SetTrailingEntryLong(int m_Quant,int TrailEntryTicks,int TrailEntryLimitTicks,int m_Pos)
		{
			return SetTrailingEntry(m_Quant,"LongEntryTrail"+m_Pos.ToString(), TrailEntryTicks, TrailEntryLimitTicks, m_Pos, true);
		}
		
		public bool SetTrailingEntryShort(int m_Quant,int TrailEntryTicks,int TrailEntryLimitTicks,int m_Pos)
		{
			return SetTrailingEntry(m_Quant,"ShortEntryTrail"+m_Pos.ToString(), TrailEntryTicks, TrailEntryLimitTicks, m_Pos, false);
		}

		private bool SetTrailingEntry(int m_Quant,string signalName, int TrailEntryTicks,int TrailEntryLimitTicks,int m_Pos, bool m_Long)
		{
			bool m_ret=true;
			
			if (m_Canceled) // Order canceled but update attempted, could be a race condition
				return false;
			
			if (m_Long)
			{
				m_Direction=1;
			}
			
			ConfigureTrailingEntry(m_Pos,-1);		//Local Flat, global not short
			m_OCOLabel="TrailingEntry";	
			
			m_SignalName=signalName;
			m_TrailChaseTicks=TrailEntryTicks;
			m_TrailChaseTicksLimit=TrailEntryLimitTicks;
			m_Quantity=m_Quant;
			m_IParam1=m_Strat.CurrentBar;
			
			m_PositionNumber=m_Pos;
			
			return m_ret;
		}
#endregion
		
#region SetBreakEven		
		public bool SetBreakEven(string signalName, int BETrigger,int BEOffset,int m_Pos)
		{
			bool m_ret=true;
			
			if (m_Canceled) // Order canceled but update attempted, could be a race condition
				return false;
			
			ConfigureConditionalStopLossOrder(m_Pos,5);		//Not Flat

			m_OCOLabel="BreakEven";
			
			m_SignalName=signalName;
			m_BreakEvenTrigger=BETrigger;
			m_BreakEvenOffset=BEOffset;
			m_PositionNumber=m_Pos;
			
			return m_ret;
		}
#endregion
		
#region SetPreserveProfit		
		public bool SetPreserveProfit(string signalName, int TrailTrigger,double Percent,int m_Pos)
		{
			bool m_ret=true;
			
			if (m_Canceled) // Order canceled but update attempted, could be a race condition
				return false;
			
			ConfigureConditionalStopLossOrder(m_Pos,5);		//Not Flat

			m_OCOLabel="TrailingStop";
			
			m_SignalName=signalName;
			m_TrailTrigger=TrailTrigger;
			m_Param1=Percent;
			m_PositionNumber=m_Pos;
			m_TrailType=1;
			
			return m_ret;
		}			
#endregion
		
#region SetTrailingStop
		public bool SetTrailingStop(string signalName, int TrailTrigger,int TrailingStop,int m_Pos)
		{
			bool m_ret=true;
			
			if (m_Canceled) // Order canceled but update attempted, could be a race condition
				return false;
			
			ConfigureConditionalStopLossOrder(m_Pos,5);		//Not Flat

			m_OCOLabel="TrailingStop";
			
			m_SignalName=signalName;
		 	m_TrailTrigger=TrailTrigger;		// Profit Trigger point
			m_IParam1=TrailingStop;				// Trailing Stop Value
		 	m_TrailType=0;						// -1 None, 0 Linear, 1 - % save (Param1 =%)
			m_PositionNumber=m_Pos;
			
			return m_ret;
		}			

		public bool SetTrailingStop(string signalName, int TrailTrigger,int m_Pos)
		{
			bool m_ret=true;
			
			if (m_Canceled) // Order canceled but update attempted, could be a race condition
				return false;
			
			ConfigureConditionalStopLossOrder(m_Pos,5);		//Not Flat

			m_OCOLabel="TrailingStop";
			
			m_SignalName=signalName;
		 	m_TrailTrigger=TrailTrigger;		// Profit Trigger point
			m_IParam1=0;				// Trailing Stop Value
		 	m_TrailType=-1;						// -1 Indicator, 0 Linear, 1 - % save (Param1 =%)
			m_PositionNumber=m_Pos;
			
			return m_ret;
		}		
#endregion

// CKKOH
#region SetProfitTaking
        public bool SetProfitTaking(string signalName, int TrailTrigger, int m_Pos)
        {
            bool m_ret = true;

            if (m_Canceled) // Order canceled but update attempted, could be a race condition
                return false;

            m_ConditionType=9;  // Trailing order
            m_AwaitingPosition=m_Pos;
            m_AwaitingCondition=5;

            m_OCOLabel = "ProfitTaking";

            m_SignalName = signalName;
            m_TrailTrigger = TrailTrigger;		// Profit Trigger point
            m_PositionNumber = m_Pos;
            return m_ret;
        }
#endregion
    }
}
