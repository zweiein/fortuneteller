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
    /// [NJAMC] 2012-04-01: Improving Trailing Stop Function (Fixed)
    /// [CKKOH] 2013-09-20: (i) Added Profit Taking by Dataseries Function.
    ///                     (ii) Fixed a bug in Trailing Stop by Dataseries.
    ///                     (iii) Changed the behaviour of Trailing Stop by Dataseries.
	/// 	DO NOT SELL THIS CODE OR DISTRIBUTE OUTSIDE OF BMT's Elite Forum Section
	///
	/// 						
	/// 
	/// </summary>
	public partial class LocalOrderManager 
	{
		string REV_CONDITIONALFUNCTIONS01="01b";

#region CheckForConditionalTasks()
		public void CheckForConditionalTasks()
		{
			for (int i=m_ConditionalStack.Count-1;i>=0;--i)
			{
//Trace.TraceError("CheckForConditionalTasks: ");
				if (m_ConditionalStack[i].Canceled)
				{
					m_ConditionalStack.RemoveAt(i);
					break;
				}
				else
				{
					if ((m_ConditionalStack.Count>i)&&
						((m_ConditionalStack[i].OrderRulesValue.VitualOrder)))
					{
						ProcessVirtualOrder(i);
					}
					if ((m_ConditionalStack.Count>i)&&
						((m_ConditionalStack[i].ConditionType==1) ||   	// Regular Pending Order
						(m_ConditionalStack[i].ConditionType==7) ||		// INT Pending Order Function
						(m_ConditionalStack[i].ConditionType==8)  ))	// DOUBLE Pending Order Function
					{// Place Order
						//m_Strat.Print("CheckForConditionalTasks: Found type: "+m_ConditionalStack[i].ConditionType);
						CheckForConditionalPlaceOrder(i);
					}
					if ((m_ConditionalStack.Count>i)&&(m_ConditionalStack[i].ConditionType==2))
					{// Conditional Trailing Order
						CheckForConditionalTrailingOrder(i);
					}
					if ((m_ConditionalStack.Count>i)&&(m_ConditionalStack[i].ConditionType==5))
					{// Trailing Entry
						UpdateTrailingEntries(i);
					}
                    // CKKOH
                    if ((m_ConditionalStack.Count > i) && (m_ConditionalStack[i].ConditionType == 9))
                    {// Conditional Profit Taking Order
                        CheckForConditionalProfitTakingOrder(i);
                    }
                }
			}			
		}	
#endregion
		
#region SubmitPendingOrder()		
		public void SubmitPendingOrder(int barsInProgressIndex, OrderAction orderAction, OrderType orderType,
			int quantity, double limitPrice, double stopPrice, string ocoId, string signalName,int m_Pos,OrderRules m_GU)
		{
			int m_Index;
			
			ConditionalOrder m_CO=new ConditionalOrder(m_Strat,m_Pos);
			
			m_CO.PendingOrder(barsInProgressIndex, orderAction, orderType, quantity, limitPrice,
				stopPrice,	ocoId, signalName);
			
			m_CO.OrderRulesValue=m_GU;
			
			m_Index=GetConditionalOrderEntry(m_CO.SignalName);
			if (m_Index>=0)
				m_ConditionalStack.RemoveAt(m_Index);
			m_ConditionalStack.Add(m_CO);
//Trace.TraceError("SubmitPendingOrder Exit:  ");
		}	
#endregion
		
#region SubmitPendingBracket()
		public void SubmitPendingBracket(OrderFunctionType m_Type,int m_Quant,int m_p1,int m_p2,
			int m_PositionNumber,double m_RefPrice,OrderRules m_GU)
		{
			int m_Index;
			//m_Strat.Print("SubmitPendingBracket(INT): "+m_Type);
			
			ConditionalOrder m_CO=new ConditionalOrder(m_Strat,m_PositionNumber);
			
			m_CO.PendingFunctionType(m_Type,m_Quant,m_p1,m_p2,m_PositionNumber,m_RefPrice,m_GU);
			
			m_Index=GetConditionalOrderEntry(m_CO.SignalName);
			if (m_Index>=0)
				m_ConditionalStack.RemoveAt(m_Index);
			m_ConditionalStack.Add(m_CO);
		}	
		public void SubmitPendingBracket(OrderFunctionType m_Type,int m_Quant,double m_p1,double m_p2,int m_PositionNumber,bool m_AbsoluteLimits,OrderRules m_GU)
		{
			SubmitPendingBracket(m_Type,m_Quant,m_p1,m_p2,0.0,0.0,m_PositionNumber,m_AbsoluteLimits,m_GU);
		}
		public void SubmitPendingBracket(OrderFunctionType m_Type,int m_Quant,double m_p1,double m_p2,double m_p3,double m_p4,int m_PositionNumber,bool m_AbsoluteLimits,OrderRules m_GU)
		{
			int m_Index;
			//m_Strat.Print("SubmitPendingBracket(DOUBLE): "+m_Type);
			
			ConditionalOrder m_CO=new ConditionalOrder(m_Strat,m_PositionNumber);
			
			m_CO.PendingFunctionType(m_Type,m_Quant,m_p1,m_p2,m_p3,m_p4,m_PositionNumber,m_AbsoluteLimits,m_GU);
			
			m_CO.OrderRulesValue=m_GU;
			
			m_Index=GetConditionalOrderEntry(m_CO.SignalName);
			if (m_Index>=0)
				m_ConditionalStack.RemoveAt(m_Index);
			m_ConditionalStack.Add(m_CO);
		}		
#endregion
		
#region ReleasePendingOrderFunction()
		public void ReleasePendingOrderFunction(ConditionalOrder m_CO)
		{
			//m_Strat.Print("ReleasePendingOrderFunction: "+m_CO.ConditionType);
			switch (m_CO.FunctionType)
			{
				case OrderFunctionType.ExitSLPT:
					if (m_CO.ConditionType==7)	// INT Version
						ExitSLPT(m_CO.IParam1,m_CO.IParam2,m_CO.PositionNumber,m_CO.Param1,m_CO.OrderRulesValue);
					else if (m_CO.ConditionType==8)	// DOUBLE Version
						ExitSLPT(m_CO.Param1,m_CO.Param2,m_CO.PositionNumber,m_CO.bParam1,m_CO.OrderRulesValue);
					break;
				case OrderFunctionType.GoLimitBracket:
					if (m_CO.ConditionType==7)	// INT Version
						ThrowError(99,"ReleasePendingOrderFunction: Type GoLimitBracket was of TYPE INT, but not possible");
					else if (m_CO.ConditionType==8)	// DOUBLE Version
						GoLimitBracket(m_CO.Quantity,m_CO.Param1,m_CO.Param2,m_CO.Param3,m_CO.Param4,m_CO.PositionNumber,m_CO.OrderRulesValue);
					break;
				case OrderFunctionType.GoMarketBracket:
					if (m_CO.ConditionType==7)	// INT Version
						GoMarketBracket(m_CO.Quantity,m_CO.IParam1,m_CO.PositionNumber,m_CO.OrderRulesValue);
					else if (m_CO.ConditionType==8)	// DOUBLE Version
						GoMarketBracket(m_CO.Quantity,m_CO.Param1,m_CO.Param2,m_CO.PositionNumber,m_CO.OrderRulesValue);
					break;
				case OrderFunctionType.NONE:
					m_Strat.Log("ReleasePendingOrderFunction: Unknown Conditional Order Type, failed to release.",LogLevel.Error);
					break;
				default:
					m_Strat.Log("ReleasePendingOrderFunction: Unknown Conditional Order Type, failed to release.",LogLevel.Error);
					break;
			}
			m_CO.CancelOrder(); // Done with order, sent off for processing
		}
#endregion
		
#region SetTrailingEntryLong()	
		public void SetTrailingEntryLong(int m_Quant,int m_ChaseTicks,int m_ChaseTicksLimit,int m_Pos)
		{
			SetTrailingEntry(m_Quant,m_ChaseTicks,m_ChaseTicksLimit,m_Pos,true);
		}
		
		public void SetTrailingEntryLong(int m_Quant,int m_ChaseTicks,int m_Pos)
		{
			SetTrailingEntry(m_Quant,m_ChaseTicks,-99999,m_Pos,true);
		}
		
		public void SetTrailingEntryShort(int m_Quant,int m_ChaseTicks,int m_ChaseTicksLimit, int m_Pos)
		{
			SetTrailingEntry(m_Quant,m_ChaseTicks,m_ChaseTicksLimit,m_Pos,false);
		}
		
		public void SetTrailingEntryShort(int m_Quant,int m_ChaseTicks,int m_Pos)
		{
			SetTrailingEntry(m_Quant,m_ChaseTicks,-99999,m_Pos,false);
		}
		
		private void SetTrailingEntry(int m_Quant,int m_ChaseTicks, int m_ChaseTicksLimit,int m_Pos,bool m_long)
		{
			int m_Index;
			string m_OCO="TrailingEntry";
			
			ConditionalOrder m_CO=new ConditionalOrder(m_Strat,m_Pos);
			
			m_Index=GetConditionalOrderEntryOCO(m_OCO,m_Pos);
			if (m_long)
			{
				if (m_Index<0)
					GoLongLimit(m_Quant,m_ChaseTicks,m_ChaseTicksLimit,m_Pos);
				m_CO.SetTrailingEntryLong(m_Quant,m_ChaseTicks, m_ChaseTicksLimit, m_Pos);
			}
			else
			{
				if (m_Index<0)
					GoShortLimit(m_Quant,m_ChaseTicks,m_ChaseTicksLimit,m_Pos);
				m_CO.SetTrailingEntryShort(m_Quant,m_ChaseTicks, m_ChaseTicksLimit, m_Pos);
			}
			
			debug("Signal Name: "+m_CO.SignalName,0);
						
			if (m_Index>=0)
				m_ConditionalStack.RemoveAt(m_Index);
			m_ConditionalStack.Add(m_CO);
		}
#endregion
		
#region CancelTrailingEntries()
		public int CancelTrailingEntries()
		{
			return CancelTrailingEntries(-1);
		}
		public int CancelTrailingEntries(int m_Pos)
		{
			string m_OCO="TrailingEntry";

			CancelOCOHold(m_OCO, m_Pos); // Cancel Orders on Market
			return CancelConditionalOrderOCO(m_OCO, m_Pos);	 // Cancel task to update Market order		
		}
#endregion
		
#region CancelBreakEven()
		public int CancelBreakEven()
		{
			return CancelBreakEven(-1);
		}
		public int CancelBreakEven(int m_Pos)
		{
			string m_OCO="BreakEven";

			CancelOCOHold(m_OCO, m_Pos); // Cancel Orders on Market
			return CancelConditionalOrderOCO(m_OCO, m_Pos);	 // Cancel task to update Market order		
		}	
#endregion
		
#region CancelTrailingStop()
		public int CancelTrailingStop()
		{
			return CancelTrailingStop(-1);
		}
		public int CancelTrailingStop(int m_Pos)
		{
			string m_OCO="TrailingStop";

			CancelOCOHold(m_OCO, m_Pos); // Cancel Orders on Market
			return CancelConditionalOrderOCO(m_OCO, m_Pos);	 // Cancel task to update Market order		
		}	
#endregion

#region PrintConditionalStack()		
		public int PrintConditionalStack(int m_DebugLevel)
		{
			int i=0;
			int count=0;
			
			for (i=m_ConditionalStack.Count-1;i>=0;--i)
			{
				{
					debug(" PrintConditionalStack: "+m_ConditionalStack[i].SignalName+" ID: "+m_ConditionalStack[i].ID+" OCO: "+m_ConditionalStack[i].OCOGroup+" Count = "+m_ConditionalStack.Count,m_DebugLevel);
					count++;
				}
			}
			return count;
		}	
#endregion

#region GetConditionalOrders....		
		public int GetConditionalOrderEntry(string m_Entry)
		{
			int i=0;
			int index=-99; 
			
			for (i=0;i<m_ConditionalStack.Count;++i)
			{
				debug("Conditional Stack COUNT: "+i+"  NAME/FIND: "+m_ConditionalStack[i].SignalName+" / "+m_Entry,10);
				if (m_ConditionalStack[i].SignalName==m_Entry)
				{
					index=i;
					break;
				}
			}
			return index;
		}
		
		public int GetConditionalOrderEntryOCO(string m_OCO)
		{
			return GetConditionalOrderEntryOCO(m_OCO, -1);
		}
		
		public int GetConditionalOrderEntryOCO(string m_OCO, int m_Pos)
		{
			int i=0;
			int index=-99; 
			
			for (i=0;i<m_ConditionalStack.Count;++i)
			{
				if ((m_ConditionalStack[i].OCOGroup==m_OCO) &&
					((m_Pos<0) || (m_ConditionalStack[i].PositionNumber==m_Pos)))
				{
					index=i;
					break;
				}
			}
			return index;
		}
#endregion
		
#region CancelConditionals....
		public int CancelConditionalOrder(string m_Name)
		{
			int i=0;
			int count=0;
			
			for (i=m_ConditionalStack.Count-1;i>=0;--i)
			{
				if (m_ConditionalStack[i].SignalName==m_Name)
				{
					m_ConditionalStack[i].CancelOrder();
					count++;
				}
			}
			return count;
		}	
		
		public int CancelConditionalOrderOCO(string m_OCO)
		{		
			return CancelConditionalOrderOCO(m_OCO,-1);
		}
		
		public int CancelConditionalOrderOCO(string m_OCO, int m_Pos)
		{
			int i=0;
			int count=0;
			
			for (i=m_ConditionalStack.Count-1;i>=0;--i)
			{
				if ((m_ConditionalStack[i].OCOGroup==m_OCO) &&
					((m_Pos<0) || (m_ConditionalStack[i].PositionNumber==m_Pos)))
				{
					m_ConditionalStack[i].CancelOrder();
					count++;
				}
			}
			return count;
		}			
#endregion
		
#region SetBreakEven()
		public void SetBreakEven(string signalName, int m_BreakEvenTrigger,int m_BreakEvenOffset,int m_Pos)
		{	// m_BreakEvenTrigger is number of ticks profit, then SL is moved m_BreakEvenOffset from Entry Price
			int m_Index;
			
			ConditionalOrder m_CO=new ConditionalOrder(m_Strat,m_Pos);
			
			m_CO.SetBreakEven(signalName+m_Pos.ToString(),m_BreakEvenTrigger,m_BreakEvenOffset,m_Pos);
			
			m_Index=GetConditionalOrderEntry(m_CO.SignalName);
			if (m_Index>=0)
				m_ConditionalStack.RemoveAt(m_Index);
			m_ConditionalStack.Add(m_CO);
		}
#endregion
		
#region SetTrailingStop(Trigger/TrailStopTicks)
		public void SetTrailingStop(string signalName, int m_TrailStopTrigger,int m_TrailStopLoss,int m_Pos)
		{	// After m_TrailStopTrigger profit, use m_TrailStopLoss Trailing StopLoss
			int m_Index;
			
			ConditionalOrder m_CO=new ConditionalOrder(m_Strat,m_Pos);
			
			m_CO.SetTrailingStop(signalName+m_Pos.ToString(),m_TrailStopTrigger,m_TrailStopLoss,m_Pos);
			
			m_Index=GetConditionalOrderEntry(m_CO.SignalName);
			if (m_Index>=0)
				m_ConditionalStack.RemoveAt(m_Index);
			m_ConditionalStack.Add(m_CO);
		}
#endregion

#region SetTrailingStop(External DataSeries/Indicator)
		public void SetTrailingStop(string signalName, int m_TrailStopTrigger,
			DataSeries m_ShortSL,DataSeries m_LongSL,int m_DSBarsOffset,int m_DSValueTicksOffset,int m_Pos)
		{	// After m_TrailStopTrigger profit, use m_TrailStopLoss Trailing StopLoss
			int m_Index;
			
			ConditionalOrder m_CO=new ConditionalOrder(m_Strat,m_Pos);
			
			m_SLShortTicksOffset[m_Pos]=m_DSValueTicksOffset;
			m_SLLongTicksOffset[m_Pos]=m_DSValueTicksOffset;
			m_SLShortBarOffset[m_Pos]=m_DSBarsOffset;
			m_SLLongBarOffset[m_Pos]=m_DSBarsOffset;
			m_SLShort[m_Pos]=(DataSeries)m_ShortSL;
			m_SLLong[m_Pos]=(DataSeries)m_LongSL;
			
			m_CO.SetTrailingStop(signalName+m_Pos.ToString(),m_TrailStopTrigger,m_Pos);
			
			m_Index=GetConditionalOrderEntry(m_CO.SignalName);
			if (m_Index>=0)
				m_ConditionalStack.RemoveAt(m_Index);

			m_ConditionalStack.Add(m_CO);
		}
#endregion
		
#region SetPreserveProfit()
		public void SetPreserveProfit(string signalName, int m_BreakTrigger,double m_Precent,int m_Pos)
		{
			int m_Index;
			
			m_Index=GetConditionalOrderEntry(signalName+m_Pos.ToString());
			
			if (m_Index>=0)
			{
				m_ConditionalStack[m_Index].SetPreserveProfit(signalName+m_Pos.ToString(),
					m_BreakTrigger,m_Precent,m_Pos);
			}
			else
			{
				ConditionalOrder m_CO=new ConditionalOrder(m_Strat,m_Pos);
			
				m_CO.SetPreserveProfit(signalName+m_Pos.ToString(),m_BreakTrigger,m_Precent,m_Pos);
			
				m_ConditionalStack.Add(m_CO);
			}
		}
#endregion

// CKKOH
#region SetProfitTaking(External DataSeries/Indicator)
        public void SetProfitTaking(string signalName, int m_PTTrigger,
            DataSeries m_ShortPT, DataSeries m_LongPT, int m_DSBarsOffset, int m_DSValueTicksOffset, int m_Pos)
        {
            int m_Index;

            ConditionalOrder m_CO = new ConditionalOrder(m_Strat, m_Pos);

            m_PTShortTicksOffset[m_Pos] = m_DSValueTicksOffset;
            m_PTLongTicksOffset[m_Pos] = m_DSValueTicksOffset;
            m_PTShortBarOffset[m_Pos] = m_DSBarsOffset;
            m_PTLongBarOffset[m_Pos] = m_DSBarsOffset;
            m_PTShort[m_Pos] = (DataSeries)m_ShortPT;
            m_PTLong[m_Pos] = (DataSeries)m_LongPT;

            m_CO.SetProfitTaking(signalName+m_Pos.ToString(), m_PTTrigger, m_Pos);

            m_Index = GetConditionalOrderEntry(m_CO.SignalName);
            if (m_Index>=0)
                m_ConditionalStack.RemoveAt(m_Index);

            m_ConditionalStack.Add(m_CO);
        }
#endregion

#region CheckForConditionalPlaceOrder(int i)
        public void CheckForConditionalPlaceOrder(int i)
		{
			bool m_ReleaseOrder=false;
			MarketPosition m_MP;
			
			m_MP=GetMarketPosition();
			
			switch(m_ConditionalStack[i].AwaitingCondition)
			{
				case 0: // Global Flat
					if (m_MP==MarketPosition.Flat)
						m_ReleaseOrder=true;
					break;
				case 1:	// Global Long
					if (m_MP==MarketPosition.Long)
						m_ReleaseOrder=true;
					break;
				case 2: // Global Short
					if (m_MP==MarketPosition.Short)
						m_ReleaseOrder=true;
					break;							
				case 3:		// Going Long, Global Not Short, Local FLat
					if ((m_MP!=MarketPosition.Short) &&
						(GetMarketPosition(m_ConditionalStack[i].AwaitingPosition)==MarketPosition.Flat))
							m_ReleaseOrder=true;
					break;
				case 4:	// Going Short, Global not Long, Local Flat
					if ((m_MP!=MarketPosition.Long)&&
						(GetMarketPosition(m_ConditionalStack[i].AwaitingPosition)==MarketPosition.Flat))
						m_ReleaseOrder=true;
					break;
				case 5: // Global Not Flat
					if (m_MP!=MarketPosition.Flat)
						m_ReleaseOrder=true;
					break;
				case 6: // Local Flat
					if (GetMarketPosition(m_ConditionalStack[i].AwaitingPosition)==MarketPosition.Flat)
						m_ReleaseOrder=true;
					break;
				case 7: // Local Not Flat
					if (GetMarketPosition(m_ConditionalStack[i].AwaitingPosition)!=MarketPosition.Flat)
						m_ReleaseOrder=true;
					break;
				default:
					break;
			}
			
			if (m_ConditionalStack[i].MaxVolumeBeforeRelease>0)
			{
				//if m_ConditionalStack[i].Order==MarketPosition.)
			}
			
			
			if (m_ReleaseOrder)
			{
				switch (m_ConditionalStack[i].ConditionType)
				{
					case 1:	// Standard Order waiting to release
						LocalOrder m_LO=new LocalOrder(m_Strat,m_ConditionalStack[i].PositionNumber);
						
						m_OrderStack.Add(m_LO);
						m_ConditionalStack[i].ReleaseOrder(m_LO);
						if (m_ConditionalStack[i].Canceled)
							m_ConditionalStack.RemoveAt(i);
						break;
					case 7:	// INT Based Order Function waiting to release
						ReleasePendingOrderFunction(m_ConditionalStack[i]);

						if (m_ConditionalStack[i].Canceled)
							m_ConditionalStack.RemoveAt(i);
						break;
					case 8:	// DOUBLE Based Order Function waiting to release
						ReleasePendingOrderFunction(m_ConditionalStack[i]);

						if (m_ConditionalStack[i].Canceled)
							m_ConditionalStack.RemoveAt(i);
						break;
					default:
						break;
				}		
			}
		}
#endregion
		
#region CheckForConditionalTrailingOrder()	
		public void CheckForConditionalTrailingOrder(int i)
		{
			bool m_ConditionMet=false;
			MarketPosition m_MP;
			
			m_MP=GetMarketPosition();
			
#region m_ConditionMet?
			switch(m_ConditionalStack[i].AwaitingCondition)
			{
				case -1: // Any Position Okay
					m_ConditionMet=true;
					break;
				case 0: // Global Flat
					if (m_MP==MarketPosition.Flat)
						m_ConditionMet=true;
					break;
				case 1:	// Global Long
					if (m_MP==MarketPosition.Long)
						m_ConditionMet=true;
					break;
				case 2: // Global Short
					if (m_MP==MarketPosition.Short)
						m_ConditionMet=true;
					break;							
				case 3:		// Going Long, Global Not Short, Local FLat
					if ((m_MP!=MarketPosition.Short) &&
						(GetMarketPosition(m_ConditionalStack[i].AwaitingPosition)==MarketPosition.Flat))
						m_ConditionMet=true;
					break;
				case 4:	// Going Short, Global not Long, Local Flat
					if ((m_MP!=MarketPosition.Long)&&
						(GetMarketPosition(m_ConditionalStack[i].AwaitingPosition)==MarketPosition.Flat))
						m_ConditionMet=true;
					break;
				case 5:	// Global Not Flat
					if (m_MP!=MarketPosition.Flat)
					{
						m_ConditionalStack[i].ID=m_ActivePositions[m_ConditionalStack[i].PositionNumber].Order.OrderId;
						m_ConditionMet=true;
					}
					break;
				default:
					break;
			}
 #endregion
			
			if (m_ConditionMet)
			{
				m_ConditionalStack[i].AwaitingCondition=-1;
				
				if (m_ActivePositions[m_ConditionalStack[i].PositionNumber]!=null)
				{
					if(m_ConditionalStack[i].ID==
						m_ActivePositions[m_ConditionalStack[i].PositionNumber].Order.OrderId)
					{
						if ((m_MP!=MarketPosition.Flat) &&
							(m_ActivePositions[m_ConditionalStack[i].PositionNumber]!=null))
						{
							bool m_BE,m_PP,m_TS;
							
							m_BE=CheckForConditionalBreakEven(i);
							if (m_ConditionalStack[i].TrailType==0)
								m_TS=CheckForConditionalTrailingStop(i);
							else if (m_ConditionalStack[i].TrailType==-1)
								m_TS=CheckForConditionalTrailingStopIndiBased(i);
								
							m_PP=CheckForConditionalPreserveProfit(i);
						}
					}
					else
						m_ConditionalStack[i].CancelOrder();
				}
				else
					m_ConditionalStack[i].CancelOrder();
			}		
		}	
#endregion


#region CheckForConditionalProfitTakingOrder()
        public void CheckForConditionalProfitTakingOrder(int i)
        {
            bool m_ConditionMet = false;
            MarketPosition m_MP;

            m_MP = GetMarketPosition();

            #region m_ConditionMet?
            switch (m_ConditionalStack[i].AwaitingCondition)
            {
                case -1: // Any Position Okay
                    m_ConditionMet = true;
                    break;
                case 0: // Global Flat
                    if (m_MP == MarketPosition.Flat)
                        m_ConditionMet = true;
                    break;
                case 1:	// Global Long
                    if (m_MP == MarketPosition.Long)
                        m_ConditionMet = true;
                    break;
                case 2: // Global Short
                    if (m_MP == MarketPosition.Short)
                        m_ConditionMet = true;
                    break;
                case 3:		// Going Long, Global Not Short, Local FLat
                    if ((m_MP != MarketPosition.Short) &&
                        (GetMarketPosition(m_ConditionalStack[i].AwaitingPosition) == MarketPosition.Flat))
                        m_ConditionMet = true;
                    break;
                case 4:	// Going Short, Global not Long, Local Flat
                    if ((m_MP != MarketPosition.Long) &&
                        (GetMarketPosition(m_ConditionalStack[i].AwaitingPosition) == MarketPosition.Flat))
                        m_ConditionMet = true;
                    break;
                case 5:	// Global Not Flat
                    if (m_MP != MarketPosition.Flat)
                    {
                        m_ConditionalStack[i].ID = m_ActivePositions[m_ConditionalStack[i].PositionNumber].Order.OrderId;
                        m_ConditionMet = true;
                    }
                    break;
                default:
                    break;
            }
            #endregion

            if (m_ConditionMet)
            {
                m_ConditionalStack[i].AwaitingCondition = -1;

                if (m_ActivePositions[m_ConditionalStack[i].PositionNumber] != null)
                {
                    if (m_ConditionalStack[i].ID ==
                        m_ActivePositions[m_ConditionalStack[i].PositionNumber].Order.OrderId)
                    {
                        if ((m_MP != MarketPosition.Flat) &&
                            (m_ActivePositions[m_ConditionalStack[i].PositionNumber] != null))
                        {
                            bool m_TS;

                            m_TS = CheckForConditionalProfitTakingIndiBased(i);
                        }
                    }
                    else
                        m_ConditionalStack[i].CancelOrder();
                }
                else
                    m_ConditionalStack[i].CancelOrder();
            }
        }
        #endregion
		
#region CheckForConditionalBreakEven()
		public bool CheckForConditionalBreakEven(int i)
		{
			int m_ticks;
								
			m_Strat.BackColor = Color.FromArgb(50,Color.AliceBlue);		
			if (m_ConditionalStack[i].BreakEvenTrigger>0)
			{
				m_ticks=GetTotalUnrealizedPLinTicks(m_ConditionalStack[i].PositionNumber);
				debug("BreakEven Ticks Profit: "+m_ticks+ " Position: "+m_ConditionalStack[i].PositionNumber+
				"   Fill Price: "+m_ActivePositions[m_ConditionalStack[i].PositionNumber].Order.AvgFillPrice+
				" Ticksfor BreakEvenTriger: "+m_ConditionalStack[i].BreakEvenTrigger+"  TicksFor BEOffset: "+
				m_ConditionalStack[i].BreakEvenOffset);
				
				m_Strat.BackColor = Color.FromArgb(50,Color.Black);
				if (m_ticks>=m_ConditionalStack[i].BreakEvenTrigger)
				{
					if (GetMarketPosition(m_ConditionalStack[i].PositionNumber)==MarketPosition.Long)
					{
						ExitSLPT(m_ActivePositions[m_ConditionalStack[i].PositionNumber].Order.AvgFillPrice+
						m_ConditionalStack[i].BreakEvenOffset*m_Strat.TickSize,
						0,m_ConditionalStack[i].PositionNumber);

					}
					else// Must be short
					{
						ExitSLPT(m_ActivePositions[m_ConditionalStack[i].PositionNumber].Order.AvgFillPrice-
						m_ConditionalStack[i].BreakEvenOffset*m_Strat.TickSize,
						0,m_ConditionalStack[i].PositionNumber);
					}
					
					m_ConditionalStack[i].BreakEvenTrigger=-2;
					m_Strat.BackColor = Color.FromArgb(20,Color.Blue);
				}
				return true;
			}
			else
			{
				return false;
			}
		}		
#endregion
		
#region CheckForConditionalPreserveProfit()
		public bool CheckForConditionalPreserveProfit(int i)
		{
			int m_ticks;
							
			if (m_ConditionalStack[i].TrailTrigger >= 0)
			{
				m_ticks=GetTotalUnrealizedPLinTicks(m_ConditionalStack[i].PositionNumber);
				debug("Preserver Profit Ticks Profit: "+m_ticks);
				m_Strat.BackColor = Color.FromArgb(50,Color.Blue);
				if (m_ticks>=m_ConditionalStack[i].TrailTrigger)
				{
					ExitSLPT((int)(m_ticks*m_ConditionalStack[i].Param1/100.0),
						0,m_ConditionalStack[i].PositionNumber);
					debug("Preserver profit Set Ticks Profit limit: "+(m_ticks*m_ConditionalStack[i].Param1/100.0));
					
					m_ConditionalStack[i].TrailTrigger=0;
					m_Strat.BackColor = Color.FromArgb(20,Color.Red);
				}
				return true;
			}
			else
			{
				return false;
			}
		}
#endregion
		
#region CheckForConditionalTrailingStop()
		public bool CheckForConditionalTrailingStop(int i)
		{
			int m_ticks;
							
			if (m_ConditionalStack[i].TrailTrigger >= 0)
			{
				m_ticks=GetTotalUnrealizedPLinTicks(m_ConditionalStack[i].PositionNumber);
				debug("CheckForConditionalTrailingStop( Pos="+i.ToString()+") Name: "+m_ConditionalStack[i].SignalName+
				"  TriggerTicks: "+m_ConditionalStack[i].TrailTrigger+"  TrailingValue: "+m_ConditionalStack[i].IParam1+
				"    Current Ticks Profit: "+m_ticks);

				m_Strat.BackColor = Color.FromArgb(50,Color.Blue);
				if (m_ticks>=m_ConditionalStack[i].TrailTrigger)
				{
					ExitSLPT((int)(m_ConditionalStack[i].IParam1),
						0,m_ConditionalStack[i].PositionNumber);
					debug("TrailingStop Set Ticks Profit limit: "+(m_ConditionalStack[i].IParam1));
					
					//m_ConditionalStack[i].TrailTrigger=0;
					m_Strat.BackColor = Color.FromArgb(20,Color.Green);
				}
				return true;
			}
			else
			{
				return false;
			}
		}
#endregion

#region	CheckForConditionalTrailingStopIndiBased()	
	    public bool CheckForConditionalTrailingStopIndiBased(int i)
	    {
		    int m_ticks;
							
			if (m_ConditionalStack[i].TrailTrigger >= 0)
			{
                // CKKOH 20Sep13: To allow the trailing stop to continue to work after the price has breached the offset line once
                // and not just when the price remains above/below the offset line.
                if (m_ConditionalStack[i].PermTriggered == false)
                {
				    m_ticks=GetTotalUnrealizedPLinTicks(m_ConditionalStack[i].PositionNumber);
                    if (m_ticks >= m_ConditionalStack[i].TrailTrigger)
                        m_ConditionalStack[i].PermTriggered = true;
                }
				m_Strat.BackColor = Color.FromArgb(50,Color.Blue);

                if (m_ConditionalStack[i].PermTriggered == true)
				{					
					int m_pos;
					
					m_pos=m_ConditionalStack[i].PositionNumber;
					
					if (GetMarketPosition(m_pos)==MarketPosition.Long)
					{
						if (m_SLLong[m_pos].ContainsValue(m_SLLongBarOffset[m_pos]))
						{
							double m_val;
							
							m_val=m_SLLong[m_pos][m_SLLongBarOffset[m_pos]]-m_SLLongTicksOffset[m_pos]*m_Strat.TickSize;
							
							ExitSLPT(m_val,0,m_pos);
							//debug("TrailingStop ("+m_pos+") Set LONG (from Indicator): "+(m_val),0);
						}
						else
						{
                            // CKKOH 12Sep13: Not considered an error. It is possible to have an indicator that provide a stop value as and when appropriate.
							//m_Strat.Log("TrailingStop ("+m_pos+") CANNOT Set LONG (from Indicator) No value set: ",NinjaTrader.Cbi.LogLevel.Error);
							//debug("TrailingStop ("+m_pos+") CANNOT Set LONG (from Indicator) No value set: ",0);
						}
						
						//m_ConditionalStack[i].TrailTrigger=0;
						m_Strat.BackColor = Color.FromArgb(20,Color.Green);
					}
					else
					{
						if (m_SLShort[m_pos].ContainsValue(m_SLShortBarOffset[m_pos]))
						{
							double m_val;
							
                            // CKKOH 25Sep13: Fixed bug.
							//m_val=m_SLShort[m_pos][m_SLShortBarOffset[m_pos]]+m_SLShortBarOffset[m_pos]*m_Strat.TickSize;
                            m_val = m_SLShort[m_pos][m_SLShortBarOffset[m_pos]] + m_SLShortTicksOffset[m_pos] * m_Strat.TickSize;
							
							ExitSLPT(m_val,0,m_pos);
						}
						else
						{
                            // CKKOH 12Sep13: Not considered an error. It is possible to have an indicator that provide a stop value as and when appropriate.
                            //m_Strat.Log("TrailingStop ("+m_pos+") CANNOT Set STORT (from Indicator) No value set: ",NinjaTrader.Cbi.LogLevel.Error);
							//debug("TrailingStop ("+m_pos+") CANNOT Set SHORT (from Indicator) No value set: ",0);
						}						
						
						//m_ConditionalStack[i].TrailTrigger=0;
						m_Strat.BackColor = Color.FromArgb(20,Color.Green);
					}
				}
				return true;
			}
			else
			{
				return false;
			}
		}		
#endregion

// CKKOH
#region CheckForConditionalProfitTakingIndiBased()
    public bool CheckForConditionalProfitTakingIndiBased(int i)
    {
        int m_ticks;

        if (m_ConditionalStack[i].TrailTrigger >= 0)
        {
            bool m_conditionMet = false;

            if (m_ConditionalStack[i].PermTriggered == false)
            {
                m_ticks = GetTotalUnrealizedPLinTicks(m_ConditionalStack[i].PositionNumber);
                if( m_ticks >= m_ConditionalStack[i].TrailTrigger )
                    m_ConditionalStack[i].PermTriggered = true;
            }
            m_Strat.BackColor = Color.FromArgb(50, Color.Blue);

            if (m_ConditionalStack[i].PermTriggered == true)
            {
                int     m_pos;
                double  m_val;

                m_pos = m_ConditionalStack[i].PositionNumber;

                if (GetMarketPosition(m_pos) == MarketPosition.Long)
                {
                    if (m_PTLong[m_pos].ContainsValue(m_PTLongBarOffset[m_pos]))
                    {
                        // If there is tick offset specified, change the Profit target limit. Otherwise, exit the market immediately.
                        if (m_PTLongTicksOffset[m_pos] > 0)
                        {
                            m_val = m_PTLong[m_pos][m_PTLongBarOffset[m_pos]] + m_PTLongTicksOffset[m_pos]*m_Strat.TickSize;
                            ExitSLPT(0, m_val, m_pos);
                        }
                        else
                        {
                            ExitMarket(m_pos);
                        }
                    }
                    m_Strat.BackColor = Color.FromArgb(20, Color.Green);
                }
                else
                {
                    if (m_PTShort[m_pos].ContainsValue(m_PTShortBarOffset[m_pos]))
                    {
                        // If there is tick offset specified, change the Profit target limit. Otherwise, exit the market immediately.
                        if (m_PTShortTicksOffset[m_pos] > 0)
                        {
                            m_val = m_PTShort[m_pos][m_PTShortBarOffset[m_pos]] - m_PTShortTicksOffset[m_pos] * m_Strat.TickSize;
                            ExitSLPT(0, m_val, m_pos);
                        }
                        else
                        {
                            ExitMarket(m_pos);
                        }
                    }
                    m_Strat.BackColor = Color.FromArgb(20, Color.Green);
                }
            }
            return true;
        }
        else
        {
            return false;
        }
    }
#endregion

#region UpdateTrailingEntries()
        public void UpdateTrailingEntries(int i)
		{
			int m_Index;
			string m_OCO="TrailingEntry";
			debug("UpdateTrailingEntries "+i.ToString());
			if (m_ConditionalStack[i].Direction>0)
			{//"LongEntryTrail"+m_Pos.ToString()
				m_Index=GetOrderEntry("GoLongLimit"+m_ConditionalStack[i].PositionNumber.ToString());

				if (m_Index>=0)
				{
					GoLongLimit(m_ConditionalStack[i].Quantity,m_ConditionalStack[i].TrailChaseTicks,
					m_ConditionalStack[i].TrailChaseTicksLimit,m_ConditionalStack[i].PositionNumber);
				}
			}
			else
			{
				m_Index=GetOrderEntry("GoShortLimit"+m_ConditionalStack[i].PositionNumber.ToString());
				
				if (m_Index>=0)
				{
					GoShortLimit(m_ConditionalStack[i].Quantity,m_ConditionalStack[i].TrailChaseTicks,
					m_ConditionalStack[i].TrailChaseTicksLimit,m_ConditionalStack[i].PositionNumber);
				}
			}
			
			if (m_Index<0)
				m_ConditionalStack[i].CancelOrder();
			//ToDo, check for Expiration of TrailingEntry
		}
#endregion
		
	}    
}
