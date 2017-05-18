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
//------------------------------------------------------------------------------
//  LOM Update Record
//
//	12/01/2011 - NJAMC - As Issued
//	01/11/2012 - NJAMC - Updated GoXxxxLimit to actually place a limit order, previous only placed STOP and STOPLIMIT
//	02/20/2012 - NJAMC - Added better interface for STOPLIMITs as GoXxxxStopLimit
//	04/09/2012 - NJAMC - ThrowError writes TraceError()
//	04/15/2012 - NJAMC - Update ExitSLPT() to handle updates with partial files (update SL/PT if quantity different)
//	04/19/2012 - NJAMC - Update Submit Order to use same class version to allow for Order Good Til Function implementation
//	04/29/2012 - NJAMC - Added Good Until Parameter Option to Entry/Exit functions
//


// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{

	public partial class LocalOrderManager 
	{
		string REV_LOM01="02b";

#region GetAvePrice
		public double GetAvePrice(int m_PositionNumber)
		{
			LocalOrder m_Current;
			
			m_Current=m_ActivePositions[m_PositionNumber];
			
			if (m_Current!=null)
			{
				return m_Strat.Instrument.MasterInstrument.Round2TickSize(m_Current.Order.AvgFillPrice);
			}
				
			return 0.0;
		}	
#endregion
		
#region GetFillQuantity
		public int GetFillQuantity()
		{
			int total_fill=0;
			
			for (int i=0;i<m_MaxOrderPositions;++i)
				total_fill+=GetFillQuantity(i);
				
			return total_fill;
		}
		
		public int GetFillQuantity(int m_PositionNumber)
		{
			LocalOrder m_Current;
			
			m_Current=m_ActivePositions[m_PositionNumber];
			
			if (m_Current!=null)
			{
				if (m_Current.GetMarketPosition()!=MarketPosition.Flat)
					return m_Current.PositionQuantity;
			}
				
			return 0;
		}	
#endregion
		
#region Debug Function		
		private void debug (String message)
		{
			debug(message,1);
		}

		private void debug (String message,int m_Priority)
		{
			if (logLevel >= m_Priority)
			{
				m_Strat.Print (m_Strat.Name+" "+m_Strat.CurrentBar + " INST("+m_Strat.Instrument.MasterInstrument.Name +") "+m_Strat.Instrument.Expiry+" Time: "+m_Strat.Time[0]+" OrderSS: " +m_OrderStack.Count+" CondSS: " +m_ConditionalStack.Count+ " MESSAGE: "+ message +
					" [A=" + m_Strat.GetCurrentAsk().ToString("F4") + " B=" + m_Strat.GetCurrentBid().ToString("F4") + 
					" C=" + m_Strat.Close[0].ToString("F4")+"]" );
				m_Strat.Log(m_Strat.Name+" "+m_Strat.CurrentBar + " INST("+m_Strat.Instrument.MasterInstrument.Name +") "+m_Strat.Instrument.Expiry+" Time: "+m_Strat.Time[0]+" OrderSS: " +m_OrderStack.Count+" CondSS: " +m_ConditionalStack.Count+ " MESSAGE: "+ message +
					" [A=" + m_Strat.GetCurrentAsk().ToString("F4") + " B=" + m_Strat.GetCurrentBid().ToString("F4") + 
					" C=" + m_Strat.Close[0].ToString("F4")+"]",NinjaTrader.Cbi.LogLevel.Information);
			}
		}
#endregion
				
#region ThrowError
		public void ThrowError(int m_errorCode,string m_comment)
		{
			string m_HelpfulInfo;
			
			m_HelpfulInfo=" Strat:"+m_Strat.Name+" Bar:"+m_Strat.CurrentBar + " INST("+m_Strat.Instrument.MasterInstrument.Name +") "+m_Strat.Instrument.Expiry+" Time: "+m_Strat.Time[0]+" [A=" + m_Strat.GetCurrentAsk().ToString("F4") + " B=" + m_Strat.GetCurrentBid().ToString("F4") + 
					" C=" + m_Strat.Close[0].ToString("F4")+"]";
			
			m_Strat.BackColor = Color.FromArgb(95,Color.Black);
			TraceError("ThrowError "+m_comment+" Error Code: ["+m_errorCode+"]"+m_HelpfulInfo,0);
			m_Strat.Log("ThrowError "+m_comment+" Error Code: ["+m_errorCode+"]"+m_HelpfulInfo,NinjaTrader.Cbi.LogLevel.Error);
		}			
		
		public void ThrowError(int m_errorCode)
		{
			ThrowError(m_errorCode,"No Error Description Supplied");
		}	
#endregion
		
#region ThrowError
		public void TraceError(string m_comment,int m_errorLevel)
		{
			if (m_errorLevel>=logLevel)
			{
				string m_HelpfulInfo;
			
				m_HelpfulInfo=" Strat:"+m_Strat.Name+" Bar:"+m_Strat.CurrentBar + " INST("+m_Strat.Instrument.MasterInstrument.Name +") "+m_Strat.Instrument.Expiry+" Time: "+m_Strat.Time[0]+" [A=" + m_Strat.GetCurrentAsk().ToString("F4") + " B=" + m_Strat.GetCurrentBid().ToString("F4") + 
					" C=" + m_Strat.Close[0].ToString("F4")+"]";
			
				Trace.TraceError("TraceError "+m_comment+"  "+m_HelpfulInfo);
			}
		}			
		
		public void TraceError(string m_comment)
		{
			TraceError(m_comment,1);
		}	
#endregion		
		
		
#region GetMarketPosition
		public MarketPosition GetMarketPosition(int m_PositionNumber,int test)
		{
			LocalOrder m_Current,m_Previous;
			
			m_Current=m_ActivePositions[m_PositionNumber];
			m_Previous=m_PreviousPositions[m_PositionNumber];
			
			if (m_Current!=null)
			{
				return m_Current.GetMarketPosition();
			}
				
			return MarketPosition.Flat;
		}	
		
		public MarketPosition GetMarketPosition(int m_PositionNumber)
		{
			LocalOrder m_Current,m_Previous;
			MarketPosition m_mp,m_mp2;
			bool m_Short=false,m_Long=false;
			
			m_Current=m_ActivePositions[m_PositionNumber];
			m_Previous=m_PreviousPositions[m_PositionNumber];
		
			if ((m_Previous!=null) && (m_Current!=null))
				if (m_Current.Order.OrderState==OrderState.PartFilled)
				{
			//debug(" GetMarketPosition(): Current Is PARTIAL, Both Long and Short position detected",0);
					m_mp= m_Previous.GetMarketPosition();
					m_mp2= m_Current.GetMarketPosition();
					if (m_mp==MarketPosition.Long)// Set incase Active (Current) order is/going flat
						m_Long=true;
					if (m_mp==MarketPosition.Short)// Set incase Active (Current) order is/going flat
						m_Short=true;
					if (m_mp2==MarketPosition.Long)
					{	// If new position is going Long, set that as priority
						m_Long=true;
						m_Short=false;				
					}
					if (m_mp2==MarketPosition.Short)
					{   // If new position is going Short, set that as priority
						m_Long=false;
						m_Short=true;				
					}
			//debug(" GetMarketPosition(): Current Is PARTIAL, Short:",0);
				}				
			if (m_Current!=null)
				if (m_Current.Order.OrderState!=OrderState.PartFilled)
				{
					m_mp= m_Current.GetMarketPosition();
					if (m_mp==MarketPosition.Long)
						m_Long=true;
					if (m_mp==MarketPosition.Short)
						m_Short=true;
				}


			if (m_Long && m_Short)
			{
				m_mp=MarketPosition.Long;
				debug(" GetMarketPosition(): WARNING, Both Long and Short position detected, Reporting LONG",0);
				TraceError("GetMarketPosition(): WARNING, Both Long and Short position detected, Reporting LONG 99 ",0);
				ThrowError(99," GetMarketPosition(): WARNING, Both Long and Short position detected, Reporting LONG");
			} else if (m_Long)
				m_mp=MarketPosition.Long;
			else if (m_Short)
				m_mp=MarketPosition.Short;
			else 
				m_mp=MarketPosition.Flat;
				
			return m_mp;
		}
		
		public MarketPosition GetMarketPosition()
		{
			LocalOrder m_Current,m_Previous;
			MarketPosition m_mp,m_mp2;
			bool m_Short=false,m_Long=false;
			
			for (int i=0;i<m_MaxOrderPositions;++i)
			{
				m_Current=m_ActivePositions[i];
				m_Previous=m_PreviousPositions[i];
				
				if ((m_Previous!=null) && (m_Current!=null))
					if (m_Current.Order.OrderState==OrderState.PartFilled)
				{
		//	debug(i+" GetMarketPosition(): Current Is PARTIAL, Both Long and Short position detected, Reporting LONG",0);
					m_mp= m_Previous.GetMarketPosition();
					m_mp2= m_Current.GetMarketPosition();
					if (m_mp==MarketPosition.Long)// Set incase Active (Current) order is/going flat
						m_Long=true;
					if (m_mp==MarketPosition.Short)// Set incase Active (Current) order is/going flat
						m_Short=true;
					if (m_mp2==MarketPosition.Long)
					{	// If new position is going Long, set that as priority
						m_Long=true;
						m_Short=false;				
					}
					if (m_mp2==MarketPosition.Short)
					{   // If new position is going Short, set that as priority
						m_Long=false;
						m_Short=true;				
					}
				}	
			if (m_Current!=null)
				if (m_Current.Order.OrderState!=OrderState.PartFilled)
				{
					m_mp= m_Current.GetMarketPosition();
					if (m_mp==MarketPosition.Long)
						m_Long=true;
					if (m_mp==MarketPosition.Short)
						m_Short=true;
				}
			//debug(i+" GetMarketPosition(): Results LONG: "+m_Long+"  SHORT: "+m_Short,0);
			}
			if (m_Long && m_Short)
			{
				m_mp=MarketPosition.Long;
				debug(" GetMarketPosition(): WARNING, Both Long and Short position detected, Reporting LONG",0);
				TraceError("GetMarketPosition(): WARNING, Both Long and Short position detected, Reporting LONG 99 ",0);
				ThrowError(99," GetMarketPosition(): WARNING, Both Long and Short position detected, Reporting LONG");
			} else if (m_Long)
				m_mp=MarketPosition.Long;
			else if (m_Short)
				m_mp=MarketPosition.Short;
			else 
				m_mp=MarketPosition.Flat;
				
			return m_mp;
		}	
#endregion
		
#region GoLongMarket
		public bool GoLongMarket(int m_SharesTraded,int m_PositionNumber)
		{
			return GoLongMarket(m_SharesTraded,m_PositionNumber,m_DefaultOrderRules);
		}
		
		public bool GoLongMarket(int m_SharesTraded,int m_PositionNumber,OrderRules m_OrderRules)
		{
			LocalOrder m_Current;
			bool m_Conditional=false;
			int m_Index;

			if  (m_SharesTraded==0)
			{
				debug("GoLongMarket:Zero Quantity Caught, nothing to do");
				return false;
			}	

			m_Current=m_ActivePositions[m_PositionNumber];
			if  (m_Current!=null)
			{
				if (m_Current.GetMarketPosition() == MarketPosition.Short)  // Reverse the position, exit the short position
				{
					debug("Go Long: Position "+m_PositionNumber+" is SHORT with "+
						m_Current.Order.Filled+" Shares/Contracts REVERSING ORDER");

					CancelAllOrders(m_PositionNumber);	// Clean up before the reversal
					ExitMarket(m_PositionNumber);
					m_Conditional=true;  // Wait until flat to enter position
				}
				
				if (m_Current.GetMarketPosition() == MarketPosition.Long)  // Same direction !?!? maybe reject order for now, update quanitity in the future
				{
					debug("ERROR Go Long: Position "+m_PositionNumber+" is already LONG with "+
						m_Current.Order.Filled+" Shares/Contracts REJECTED ORDER");
					return false;
				}

				if (m_Current.GetMarketPosition() == MarketPosition.Flat)  // How did we get here !?!? maybe Clear orders for now, and go Long
				{
					debug("ERROR Go Long: Position "+m_PositionNumber+" is already FLAT with "+
						m_Current.Order.Filled+" Shares/Contracts CLEANING UP and then processing",0);
					ThrowError(99,"ERROR Go Long: Position "+m_PositionNumber+" is already FLAT with "+
						m_Current.Order.Filled+" Shares/Contracts CLEANING UP and then processing");
					CancelAllOrders(m_PositionNumber);
					m_ActivePositions[m_PositionNumber]=null;
				}
			}
				
			debug("GoLongMarket: ");

			if (!m_Conditional)
			{
				m_Index=GetOrderEntry("GoLongMarket"+m_PositionNumber);
				if (m_Index<0)
				{		
					m_OrderStack.Add(new LocalOrder(m_Strat,m_PositionNumber));
					m_Index=m_OrderStack.Count-1;
					
					SubmitOrder(m_OrderStack[m_Index],m_BarsInProgress, OrderAction.Buy, OrderType.Market, m_SharesTraded, 0,
						0, "ENTER"+m_PositionNumber+m_UniqueID, "GoLongMarket"+m_PositionNumber,m_OrderRules);

					if (chartLogLevel>0)
						m_Strat.DrawDiamond("GoLongMarket"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, m_Strat.GetCurrentAsk()+m_Strat.TickSize*5, Color.Green);
				}
				else
					debug("GoLongMarket IGNORED, Market Order Already Submitted: ");
			}
			else
			{
				SubmitPendingOrder(m_BarsInProgress, OrderAction.Buy, OrderType.Market, m_SharesTraded, 0,
					0, "ENTER"+m_PositionNumber+m_UniqueID, "GoLongMarket"+m_PositionNumber,m_PositionNumber,m_OrderRules);
				
				debug("GoLongMarket PENDING ORDER");
			}
			return true;
		}
#endregion

#region GoLongTrend
		public bool GoLongTrend(int m_SharesTraded,double m_RefPrice,int m_TicksInTrend,int m_PositionNumber)
		{	// THis function will provide a EnterSTOPMarket at m_RefPrice+m_TicksInTrend, RefPrice==0 uses GetCurrentAsk()
			return GoLongTrend(m_SharesTraded,m_RefPrice,m_TicksInTrend,m_PositionNumber,m_DefaultOrderRules);
		}
		public bool GoLongTrend(int m_SharesTraded,double m_RefPrice,int m_TicksInTrend,int m_PositionNumber,OrderRules m_OrderRules)
		{	// THis function will provide a EnterSTOPMarket at m_RefPrice+m_TicksInTrend, RefPrice==0 uses GetCurrentAsk()
			//OrderAction.Buy, OrderType.Stop
			bool m_Conditional=false;
			LocalOrder m_Current;
			int m_Index;

			if  (m_SharesTraded==0)
			{
				debug("GoLongMarket:Zero Quantity Caught, nothing to do");
				return false;
			}	
			
			if (m_RefPrice<=0.0)
				m_RefPrice=m_Strat.GetCurrentAsk();
			
			m_Current=m_ActivePositions[m_PositionNumber];
			if  (m_Current!=null)
			{
				if (m_Current.GetMarketPosition() == MarketPosition.Flat)  // How did we get here !?!? maybe Clear orders for now, and go Long
				{
					debug("ERROR GoLongTrend: Position "+m_PositionNumber+" is already FLAT with "+
						m_Current.Order.Filled+" Shares/Contracts CLEANING UP and then processing",0);
					CancelAllOrders(m_PositionNumber);
					ThrowError(99,"ERROR GoLongTrend: Position "+m_PositionNumber+" is already FLAT with "+
						m_Current.Order.Filled+" Shares/Contracts CLEANING UP and then processing");
					m_ActivePositions[m_PositionNumber]=null;
				}
				if (m_Current.GetMarketPosition() == MarketPosition.Short)  // Reverse the position, exit the short position
				{
					debug("GoLongTrend: Position "+m_PositionNumber+" is SHORT with "+
						m_Current.Order.Filled+" Shares/Contracts REVERSING ORDER");

					CancelAllOrders(m_PositionNumber);	// Clean up before the reversal
					ExitMarket(m_PositionNumber);
					m_Conditional=true;
				}
			}
				
			debug("GoLongTrend: ");
			if (!m_Conditional)
			{
				m_Index=GetOrderEntry("GoLongTrend"+m_PositionNumber);
				if (m_Index<0)
				{
					m_OrderStack.Add(new LocalOrder(m_Strat,m_PositionNumber));
					m_Index=m_OrderStack.Count-1;
				}

				
				SubmitOrder(m_OrderStack[m_Index],m_BarsInProgress, OrderAction.Buy, OrderType.Stop, m_SharesTraded, 0,
					Math.Max(m_RefPrice+m_TicksInTrend*m_Strat.TickSize,m_Strat.GetCurrentAsk()+m_Strat.TickSize),
					"ENTER"+m_PositionNumber+m_UniqueID, "GoLongTrend"+m_PositionNumber,m_OrderRules);

				if (chartLogLevel>0)
					m_Strat.DrawDiamond("GoLongTrend"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0,m_RefPrice+m_Strat.TickSize*m_TicksInTrend, Color.Green);

			}
			else
			{
				SubmitPendingOrder(m_BarsInProgress, OrderAction.Buy, OrderType.Stop, m_SharesTraded, 0,
					Math.Max(m_RefPrice+m_TicksInTrend*m_Strat.TickSize,m_Strat.GetCurrentAsk()+m_Strat.TickSize),
				"ENTER"+m_PositionNumber+m_UniqueID, "GoLongTrend"+m_PositionNumber,m_PositionNumber,m_OrderRules);
				
				debug("GoLongMarket PENDING ORDER");
			}			
			
			return true;
		}
#endregion

#region GoShortMarket
		public bool GoShortMarket(int m_SharesTraded,int m_PositionNumber)
		{
			return GoShortMarket(m_SharesTraded,m_PositionNumber,m_DefaultOrderRules);
		}		
		public bool GoShortMarket(int m_SharesTraded,int m_PositionNumber,OrderRules m_OrderRules)
		{//OrderAction.SellShort
			bool m_Conditional=false;
			LocalOrder m_Current;
			int m_Index;
			
			if  (m_SharesTraded==0)
			{
				debug("GoShortMarket:Zero Quantity Caught, nothing to do");
				return false;
			}	
			
			m_Current=m_ActivePositions[m_PositionNumber];
			if  (m_Current!=null)
			{
				if (m_Current.GetMarketPosition() == MarketPosition.Long)  // Reverse the position, exit the long position
				{
					debug("GoShortMarket: Position "+m_PositionNumber+" is LONG with "+
						m_Current.Order.Filled+" Shares/Contracts REVERSING ORDER");
					CancelAllOrders(m_PositionNumber);	// Clean up before the reversal
					ExitMarket(m_PositionNumber);
					m_Conditional=true;
				}
				
				if (m_Current.GetMarketPosition() == MarketPosition.Short)  // Same direction !?!? maybe reject order for now, update quanitity in the future
				{
					debug("ERROR GoShortMarket: Position "+m_PositionNumber+" is already SHORT with "+
						m_Current.Order.Filled+" Shares/Contracts REJECTED ORDER");
					return false;
				}

				if (m_Current.GetMarketPosition() == MarketPosition.Flat)  // How did we get here !?!? maybe Clear orders for now, and go Long
				{
					debug("ERROR GoShortMarket: Position "+m_PositionNumber+" is already FLAT with "+
						m_Current.Order.Filled+" Shares/Contracts CLEANING UP and then processing");
					CancelAllOrders(m_PositionNumber);
					m_ActivePositions[m_PositionNumber]=null;
				}
			}
			
			debug("GoShortMarket: ");
			
			if (!m_Conditional)
			{
				m_Index=GetOrderEntry("GoShortMarket"+m_PositionNumber);
				if (m_Index<0)
				{		
					m_OrderStack.Add(new LocalOrder(m_Strat,m_PositionNumber));
					m_Index=m_OrderStack.Count-1;

					SubmitOrder(m_OrderStack[m_Index],m_BarsInProgress, OrderAction.SellShort, OrderType.Market, m_SharesTraded, 0, 
						0,"ENTER"+m_PositionNumber+m_UniqueID, "GoShortMarket"+m_PositionNumber,m_OrderRules);

					if (chartLogLevel>0)
						m_Strat.DrawDiamond("GoShortMarket"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, m_Strat.GetCurrentBid()-m_Strat.TickSize*5, Color.Red);
				}
				else
				{
					TraceError("GoShortMarket IGNORED, Market order already in progress: ");
					debug("GoShortMarket IGNORED, Market order already in progress: ");
				}
			}
			else
			{
				SubmitPendingOrder(m_BarsInProgress, OrderAction.SellShort, OrderType.Market, m_SharesTraded, 0, 
						0,"ENTER"+m_PositionNumber+m_UniqueID, "GoShortMarket"+m_PositionNumber,m_PositionNumber,m_OrderRules);
				
				debug("GoShortMarket PENDING ORDER");
			}			
			
			return true;
		}
#endregion

#region GoShortTrend
		public bool GoShortTrend(int m_SharesTraded,double m_RefPrice,int m_TicksInTrend,int m_PositionNumber)
		{
			return GoShortTrend(m_SharesTraded,m_RefPrice,m_TicksInTrend,m_PositionNumber,m_DefaultOrderRules);
		}	
		public bool GoShortTrend(int m_SharesTraded,double m_RefPrice,int m_TicksInTrend,int m_PositionNumber,OrderRules m_OrderRules)
		{
			bool m_Conditional=false;
			LocalOrder m_Current;
			int m_Index;
			
			if  (m_SharesTraded==0)
			{
				debug("GoShortTrend:Zero Quantity Caught, nothing to do");
				return false;
			}	
			
			if (m_RefPrice<=0.0)
				m_RefPrice=m_Strat.GetCurrentBid();	
			
			m_Current=m_ActivePositions[m_PositionNumber];
			if  (m_Current!=null)
			{
				if (m_Current.GetMarketPosition() == MarketPosition.Long)  // Reverse the position, exit the long position
				{
					debug("GoShortTrend: Position "+m_PositionNumber+" is LONG with "+
						m_Current.Order.Filled+" Shares/Contracts REVERSING ORDER");
					CancelAllOrders(m_PositionNumber);	// Clean up before the reversal
					ExitMarket(m_PositionNumber);
					m_Conditional=true;
				}
				
				if (m_Current.GetMarketPosition() == MarketPosition.Flat)  // How did we get here !?!? maybe Clear orders for now, and go Long
				{
					debug("ERROR GoShortTrend: Position "+m_PositionNumber+" is already FLAT with "+
						m_Current.Order.Filled+" Shares/Contracts CLEANING UP and then processing",0);
					CancelAllOrders(m_PositionNumber);
					ThrowError(99,"ERROR GoShortTrend: Position "+m_PositionNumber+" is already FLAT with "+
						m_Current.Order.Filled+" Shares/Contracts CLEANING UP and then processing");
					m_ActivePositions[m_PositionNumber]=null;
				}
			}
			
			debug("GoShortTrend: ");
			if (!m_Conditional)
			{
				m_Index=GetOrderEntry("GoShortTrend"+m_PositionNumber);
				if (m_Index<0)
				{
					m_OrderStack.Add(new LocalOrder(m_Strat,m_PositionNumber));
					m_Index=m_OrderStack.Count-1;
				}

				SubmitOrder(m_OrderStack[m_Index],m_BarsInProgress, OrderAction.SellShort, OrderType.Stop, m_SharesTraded, 0,
					Math.Min(m_RefPrice-m_TicksInTrend*m_Strat.TickSize,m_Strat.GetCurrentBid()-m_Strat.TickSize),
					"ENTER"+m_PositionNumber+m_UniqueID, "GoShortTrend"+m_PositionNumber,m_OrderRules);

				if (chartLogLevel>0)
					m_Strat.DrawDiamond("GoShortTrend"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, m_RefPrice-m_TicksInTrend*m_Strat.TickSize, Color.Red);
			}
			else
			{
				SubmitPendingOrder(m_BarsInProgress, OrderAction.SellShort, OrderType.Stop, m_SharesTraded, 0,
					Math.Min(m_RefPrice-m_TicksInTrend*m_Strat.TickSize,m_Strat.GetCurrentBid()-m_Strat.TickSize),
					"ENTER"+m_PositionNumber+m_UniqueID, "GoShortTrend"+m_PositionNumber,m_PositionNumber,m_OrderRules);
				
				debug("GoShortTrend PENDING ORDER");
			}					
			
			
			return true;
		}
#endregion

#region GoMarketBracket
		public bool GoMarketBracket(int m_SharesTraded,int m_TickBuffer,int m_PositionNumber)
		{
			return GoMarketBracket(m_SharesTraded,m_TickBuffer,m_PositionNumber,m_DefaultOrderRules);
		}
		public bool GoMarketBracket(int m_SharesTraded,int m_TickBuffer,int m_PositionNumber,OrderRules m_OrderRules)
		{
			double m_TickOffset;
			LocalOrder m_Current;

			m_Current=m_ActivePositions[m_PositionNumber];
			if  ((m_Current!=null) || (GetMarketPosition()!=MarketPosition.Flat))
			{
				if (CountAllExit(m_PositionNumber)==0)
				{
					debug("GoMarketBracket ORDER REJECTED: Position already existes",0);
					ThrowError(99,"GoMarketBracket ORDER REJECTED: Position already existes");
					return false;
				}
				else
				{
					SubmitPendingBracket(OrderFunctionType.GoMarketBracket,m_SharesTraded,m_TickBuffer,0,m_PositionNumber,0.0,m_OrderRules);
					return true;
				}
			}
			
			m_TickOffset=m_TickBuffer*m_Strat.TickSize;

			if (chartLogLevel>0)
			{
					m_Strat.DrawTriangleDown("Ask"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, m_Strat.GetCurrentAsk(), Color.Red);

					m_Strat.DrawTriangleUp("Bid"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, m_Strat.GetCurrentBid(), Color.Green);
			}
			return GoMarketBracket(m_SharesTraded,m_Strat.GetCurrentBid()-m_TickOffset,m_Strat.GetCurrentAsk()+m_TickOffset, m_PositionNumber,m_OrderRules);
		}
		
		public bool GoMarketBracket(int m_SharesTraded,double low,double high,int m_PositionNumber)
		{
			return GoMarketBracket(m_SharesTraded,low,high,m_PositionNumber,m_DefaultOrderRules);
		}
		public bool GoMarketBracket(int m_SharesTraded,double low,double high,int m_PositionNumber,OrderRules m_OrderRules)
		{
			int m_LowIndex,m_HighIndex;
			bool m_NewLowPos=false;
			bool m_NewHighPos=false;
			LocalOrder m_Current;
			
			if  (m_SharesTraded==0)
			{
				debug("GoMarketBracket:Zero Quantity Caught, nothing to do");
				return false;
			}			
			m_Current=m_ActivePositions[m_PositionNumber];
			if  ((m_Current!=null) || (GetMarketPosition()!=MarketPosition.Flat))
			{
				if (CountAllExit(m_PositionNumber)==0)
				{
					debug("GoMarketBracket ORDER REJECTED: Position already existes",0);
					ThrowError(99,"GoMarketBracket ORDER REJECTED: Position already existes");
					return false;
				}
				else
				{
					SubmitPendingBracket(OrderFunctionType.GoMarketBracket,m_SharesTraded,low,high,m_PositionNumber,false,m_OrderRules);
					return true;
				}
			}
			
			m_LowIndex=GetOrderEntry("GoMarketBracketLow"+m_PositionNumber);
			m_HighIndex=GetOrderEntry("GoMarketBracketHigh"+m_PositionNumber);
			
			if ((m_LowIndex<0) && (low>0)) 
			{
				m_NewLowPos=true;
				m_OrderStack.Add(new LocalOrder(m_Strat,m_PositionNumber));
				m_LowIndex=m_OrderStack.Count-1;
			}
			if ((m_HighIndex<0) && (high>0))
			{
				m_NewHighPos=true;
				m_OrderStack.Add(new LocalOrder(m_Strat,m_PositionNumber));
				m_HighIndex=m_OrderStack.Count-1;
			}
			
			debug("GoMarketBracket: Pos: "+m_PositionNumber+" L: "+low+" H: "+high);
			
			if ((low>m_Strat.GetCurrentBid()) || ((high<m_Strat.GetCurrentAsk()) && (high>0)))
			{
				debug("GoMarketBracket: REJECTED Due to Market Bid/ASK price Pos: "+m_PositionNumber+" L: "+low+" H: "+high,0);
				ThrowError(99,"GoMarketBracket: REJECTED Due to Market Bid/ASK price Pos: "+m_PositionNumber+" L: "+low+" H: "+high);
				return false;
			}

			if (low>0)
			{
				SubmitOrder(m_OrderStack[m_LowIndex],m_BarsInProgress, OrderAction.SellShort, OrderType.Stop, m_SharesTraded, 0, low,
					"ENTER"+m_PositionNumber+m_UniqueID, "GoMarketBracketLow"+m_PositionNumber,m_OrderRules);
			}
			
			if (high>0)
			{
				SubmitOrder(m_OrderStack[m_HighIndex],m_BarsInProgress, OrderAction.Buy, OrderType.Stop, m_SharesTraded, 0, high,
						"ENTER"+m_PositionNumber+m_UniqueID, "GoMarketBracketHigh"+m_PositionNumber,m_OrderRules);
			}
			
			if (chartLogLevel>0)
			{
				if (low>0)
					m_Strat.DrawTriangleDown("GoMarketBracketLow"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, low, Color.Red);
				if (high>0)
					m_Strat.DrawTriangleUp("GoMarketBracketHigh"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, high, Color.Green);
			}
			
			return true;
		}	
#endregion
		
#region GoLimitBracket
		public bool GoLimitBracket(int m_SharesTraded,double low,double lowLimit,double high,double highLimit,int m_PositionNumber)
		{
			return GoLimitBracket(m_SharesTraded,low,lowLimit,high,highLimit,m_PositionNumber,m_DefaultOrderRules);
		}
		public bool GoLimitBracket(int m_SharesTraded,double low,double lowLimit,double high,double highLimit,int m_PositionNumber,OrderRules m_OrderRules)
		{
			int m_LowIndex,m_HighIndex;
			bool m_NewLowPos=false;
			bool m_NewHighPos=false;
			LocalOrder m_Current;
			
			if  (m_SharesTraded==0)
			{
				debug("GoLimitBracket:Zero Quantity Caught, nothing to do");
				return false;
			}			
			m_Current=m_ActivePositions[m_PositionNumber];
			if  ((m_Current!=null) || (GetMarketPosition()!=MarketPosition.Flat))
			{
				if (CountAllExit(m_PositionNumber)==0)
				{
					debug("GoLimitBracket ORDER REJECTED: Position already existes",0);
					ThrowError(99,"GoLimitBracket ORDER REJECTED: Position already existes");
					return false;
				}
				else
				{
					SubmitPendingBracket(OrderFunctionType.GoMarketBracket,m_SharesTraded,low,lowLimit,high,highLimit,m_PositionNumber,false,m_OrderRules);
					return true;
				}
			}
			
			m_LowIndex=GetOrderEntry("GoLimitBracketLow"+m_PositionNumber);
			m_HighIndex=GetOrderEntry("GoLimitBracketHigh"+m_PositionNumber);
			
			if ((m_LowIndex<0) && (low>0)) 
			{
				m_NewLowPos=true;
				m_OrderStack.Add(new LocalOrder(m_Strat,m_PositionNumber));
				m_LowIndex=m_OrderStack.Count-1;
			}
			if ((m_HighIndex<0) && (high>0))
			{
				m_NewHighPos=true;
				m_OrderStack.Add(new LocalOrder(m_Strat,m_PositionNumber));
				m_HighIndex=m_OrderStack.Count-1;
			}
			
			debug("GoLimitBracket: Pos: "+m_PositionNumber+" L: "+low+" H: "+high);
			
			if ((low>m_Strat.GetCurrentBid()) || ((high<m_Strat.GetCurrentAsk()) && (high>0)))
			{
				debug("GoLimitBracket: REJECTED Due to Market Bid/ASK price Pos: "+m_PositionNumber+" L: "+low+" H: "+high,0);
				ThrowError(99);
			}

			if (low>0)
			{
				SubmitOrder(m_OrderStack[m_LowIndex],m_BarsInProgress, OrderAction.SellShort, OrderType.StopLimit, m_SharesTraded, lowLimit, low,
					"ENTER"+m_PositionNumber+m_UniqueID, "GoLimitBracketLow"+m_PositionNumber,m_OrderRules);
			}
			
			if (high>0)
			{
				SubmitOrder(m_OrderStack[m_HighIndex],m_BarsInProgress, OrderAction.Buy, OrderType.StopLimit, m_SharesTraded, highLimit, high,
						"ENTER"+m_PositionNumber+m_UniqueID, "GoLimitBracketHigh"+m_PositionNumber,m_OrderRules);
			}
			
			if (chartLogLevel>0)
			{
				if (low>0)
					m_Strat.DrawTriangleDown("GoLimitBracketLow"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, low, Color.Red);
				if (high>0)
					m_Strat.DrawTriangleUp("GoLimitBracketHigh"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, high, Color.Green);
			}
			
			return true;
		}	
#endregion

#region GoLongLimit
		public bool GoLongLimit(int m_SharesTraded,int m_LimitTicks,double m_RefPrice,int m_PositionNumber)
		{	// Enter a "Limit Order" at Reference - Ticks, if Ref=0 uses CurrentBid
			return GoLongLimit(m_SharesTraded,m_LimitTicks,m_RefPrice,m_PositionNumber,m_DefaultOrderRules);
		}		
		
		public bool GoLongLimit(int m_SharesTraded,int m_LimitTicks,double m_RefPrice,int m_PositionNumber,OrderRules m_OrderRules)
		{	// Enter a "Limit Order" at Reference - Ticks, if Ref=0 uses CurrentBid
			double m_Limit;
			
			if (m_RefPrice<=0.0)
				m_RefPrice=m_Strat.GetCurrentBid();
			
			m_Limit=m_RefPrice-m_LimitTicks*m_Strat.TickSize;		
			
			return GoLongLimit(m_SharesTraded,-1.0,m_Limit,m_PositionNumber,m_OrderRules);
		}
		
		public bool GoLongLimit(int m_SharesTraded,double m_Limit,int m_PositionNumber)
		{	// True Limit Order Placed, no trigger
			return GoLongLimit(m_SharesTraded,-1.0,m_Limit, m_PositionNumber,m_DefaultOrderRules);
		}
		
		public bool GoLongLimit(int m_SharesTraded,double m_Limit,int m_PositionNumber,OrderRules m_OrderRules)
		{	// True Limit Order Placed, no trigger
			return GoLongLimit(m_SharesTraded,-1.0,m_Limit, m_PositionNumber,m_OrderRules);
		}
#endregion

#region GoLongStop
		public bool GoLongStop(int m_SharesTraded,int m_StopTicks,int m_PositionNumber)
		{	// Use this to place STOPLIMIT SHORT order
			return GoLongLimit(m_SharesTraded,m_StopTicks,-99999, m_PositionNumber,m_DefaultOrderRules);
		}
						
		public bool GoLongStop(int m_SharesTraded,int m_StopTicks,int m_PositionNumber,OrderRules m_OrderRules)
		{	// Use this to place STOPLIMIT SHORT order
			return GoLongLimit(m_SharesTraded,m_StopTicks,-99999, m_PositionNumber,m_OrderRules);
		}
		
		public bool GoLongStop(int m_SharesTraded,double m_Stop,int m_PositionNumber)
		{	// Use this to place STOPLIMIT SHORT order
			return GoLongLimit(m_SharesTraded,m_Stop,-99999, m_PositionNumber,m_DefaultOrderRules);
		}
		
		public bool GoLongStop(int m_SharesTraded,double m_Stop,int m_PositionNumber,OrderRules m_OrderRules)
		{	// Use this to place STOPLIMIT SHORT order
			return GoLongLimit(m_SharesTraded,m_Stop,-99999, m_PositionNumber,m_OrderRules);
		}
#endregion
		
#region GoLongStopLimit
		public bool GoLongStopLimit(int m_SharesTraded,int m_TrailAskTicks,int m_LimitProfitTicks,int m_PositionNumber)
		{	// Use this to place STOPLIMIT SHORT order
			return GoLongLimit(m_SharesTraded,m_TrailAskTicks,m_LimitProfitTicks, m_PositionNumber,m_DefaultOrderRules);
		}
						
		public bool GoLongStopLimit(int m_SharesTraded,int m_TrailAskTicks,int m_LimitProfitTicks,int m_PositionNumber,OrderRules m_OrderRules)
		{	// Use this to place STOPLIMIT SHORT order
			return GoLongLimit(m_SharesTraded,m_TrailAskTicks,m_LimitProfitTicks, m_PositionNumber,m_OrderRules);
		}
		
		public bool GoLongStopLimit(int m_SharesTraded,double m_Trigger,double m_Limit,int m_PositionNumber)
		{	// Use this to place STOPLIMIT SHORT order
			return GoLongLimit(m_SharesTraded,m_Trigger,m_Limit, m_PositionNumber,m_DefaultOrderRules);
		}
		
		public bool GoLongStopLimit(int m_SharesTraded,double m_Trigger,double m_Limit,int m_PositionNumber,OrderRules m_OrderRules)
		{	// Use this to place STOPLIMIT SHORT order
			return GoLongLimit(m_SharesTraded,m_Trigger,m_Limit, m_PositionNumber,m_OrderRules);
		}
#endregion
		
#region GoLongLimit depriciated		
		public bool GoLongLimit(int m_SharesTraded,int m_TrailAskTicks,int m_LimitTicks,int m_PositionNumber)
		{	// Places a STOPLIMIT
			// Do not directly call this function, confusing and called from Other function, depriciated 
			return GoLongLimit(m_SharesTraded,m_TrailAskTicks,m_LimitTicks,m_PositionNumber,m_DefaultOrderRules);
		}
		public bool GoLongLimit(int m_SharesTraded,int m_TrailAskTicks,int m_LimitTicks,int m_PositionNumber,OrderRules m_OrderRules)
		{	// Places a STOPLIMIT
			// Do not directly call this function, confusing and called from Other function, depriciated 
			double m_Stop,m_Limit;
			
			m_Stop=m_Strat.GetCurrentAsk()+m_TrailAskTicks*m_Strat.TickSize;
			m_Stop=Math.Max(m_Strat.Input[0]+m_Strat.TickSize,m_Stop);		// For a long STOP, make sure 1 tick above Last value
			m_Limit=m_Stop+m_LimitTicks*m_Strat.TickSize;		
			if (m_LimitTicks==-99999)
				m_Limit=-99999;// Flag for just a STOP rather than STOPLIMIT
			
			return GoLongLimit(m_SharesTraded,m_Stop,m_Limit, m_PositionNumber,m_OrderRules);
		}

		public bool GoLongLimit(int m_SharesTraded,double m_Trigger,double m_Limit,int m_PositionNumber,OrderRules m_OrderRules)
		{	// if Trigger>=0, assumed to be a STOP or STOPLIMIT order, otherwise it is a LIMIT order	
			// Do not directly call this function, confusing and called from Other function, depriciated 
			int m_Index;
			bool m_NewPos=false;
			LocalOrder m_Current;
			
			if  (m_SharesTraded==0)
			{
				debug("GoLongLimit: Zero Quantity Caught, nothing to do");
				return false;
			}	
			
			if  ((m_Trigger<0.0) && (m_Limit<0.0))
			{
				debug("GoLongLimit: Limit Order placed with no limit set");
				return false;
			}	
			
			m_Current=m_ActivePositions[m_PositionNumber];
			if  ((m_Current!=null) || (GetMarketPosition(m_PositionNumber)!=MarketPosition.Flat))
			{
				debug("GoLongLimit ORDER REJECTED: Position already exisits",0);
				ThrowError(99,"GoLongLimit ORDER REJECTED: Position already exisits");
				return false;
			}

			if (m_Trigger<0.0)
				m_Index=GetOrderEntry("GoLongLimit"+m_PositionNumber);
			else
			{
				// A STOP/STOPLIMIT Order Passed
				m_Index=GetOrderEntry("GoLongStop"+m_PositionNumber);
				if (m_Index<0) // Didn't find a STOP, try a STOPLIMIT
					m_Index=GetOrderEntry("GoLongStopLimit"+m_PositionNumber);
			}
			
			if (m_Index<0)
			{
				m_NewPos=true;
				m_OrderStack.Add(new LocalOrder(m_Strat,m_PositionNumber));
				m_Index=m_OrderStack.Count-1;
			}
			
			debug("GoLongLimit: Pos: "+m_PositionNumber+" Stop: "+m_Trigger+" Limit: "+m_Limit);
			
			if (m_Limit== -99999)
			{		// Must be a STOP order to be here
				SubmitOrder(m_OrderStack[m_Index],m_BarsInProgress, OrderAction.Buy, OrderType.Stop,
					m_SharesTraded, 0, m_Trigger,
					"ENTER"+m_PositionNumber+m_UniqueID, "GoLongStop"+m_PositionNumber,-1,m_OrderRules);
				if (chartLogLevel>0)
				{
					m_Strat.DrawTriangleUp("GoLongStop"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0,m_Trigger, Color.Blue);
				}
			}
			else
			{
				if (m_Trigger<0.0)
				{	// Send a "Limit Order"
					SubmitOrder(m_OrderStack[m_Index],m_BarsInProgress, OrderAction.Buy, OrderType.Limit,
						m_SharesTraded, m_Limit, 0.0,
						"ENTER"+m_PositionNumber+m_UniqueID, "GoLongLimit"+m_PositionNumber,-1,m_OrderRules);
					if (chartLogLevel>0)
					{
						m_Strat.DrawTriangleUp("GoLongLimit"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0,m_Limit, Color.Blue);
					}
				}
				else
				{	// Send a "Stop Limit Order"			
					SubmitOrder(m_OrderStack[m_Index],m_BarsInProgress, OrderAction.Buy, OrderType.StopLimit,
						m_SharesTraded, m_Limit, m_Trigger,
						"ENTER"+m_PositionNumber+m_UniqueID, "GoLongStopLimit"+m_PositionNumber,-1,m_OrderRules);
					if (chartLogLevel>0)
					{
						m_Strat.DrawTriangleUp("GoLongStopLimit"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0,m_Trigger, Color.Blue);
					}
				}
			}
						
			return true;
		}			
#endregion

#region GoShortLimit
		public bool GoShortLimit(int m_SharesTraded,int m_LimitTicks,double m_RefPrice,int m_PositionNumber)
		{    // Enter a "Limit Order" at Reference + Ticks, if Ref=0 uses CurrentAsk
			return GoShortLimit(m_SharesTraded,m_LimitTicks,m_RefPrice,m_PositionNumber,m_DefaultOrderRules);
		}
		public bool GoShortLimit(int m_SharesTraded,int m_LimitTicks,double m_RefPrice,int m_PositionNumber,OrderRules m_OrderRules)
		{    // Enter a "Limit Order" at Reference + Ticks, if Ref=0 uses CurrentAsk
			// Call this to place a SHORT LIMIT order
			double m_Limit;
			
			if (m_RefPrice<=0.0)
				m_RefPrice=m_Strat.GetCurrentAsk();
			
			m_Limit=m_RefPrice+m_LimitTicks*m_Strat.TickSize;		
			
			return GoShortLimit(m_SharesTraded,-1.0,m_Limit,m_PositionNumber,m_OrderRules);
		}
		
		public bool GoShortLimit(int m_SharesTraded,double m_Limit,int m_PositionNumber)
		{	// Call this to place a SHORT LIMIT order
			return GoShortLimit(m_SharesTraded,-1.0,m_Limit,m_PositionNumber,m_DefaultOrderRules);
		}
		
		public bool GoShortLimit(int m_SharesTraded,double m_Limit,int m_PositionNumber,OrderRules m_OrderRules)
		{	// True Limit Order Placed, no trigger
			return GoShortLimit(m_SharesTraded,-1.0,m_Limit, m_PositionNumber,m_OrderRules);
		}		
#endregion

#region GoShortStop
		public bool GoShortStop(int m_SharesTraded,int m_StopTicks,int m_PositionNumber)
		{	// Use this to place STOPLIMIT SHORT order
			return GoShortLimit(m_SharesTraded,m_StopTicks,-99999, m_PositionNumber,m_DefaultOrderRules);
		}
						
		public bool GoShortStop(int m_SharesTraded,int m_StopTicks,int m_PositionNumber,OrderRules m_OrderRules)
		{	// Use this to place STOPLIMIT SHORT order
			return GoShortLimit(m_SharesTraded,m_StopTicks,-99999, m_PositionNumber,m_OrderRules);
		}
		
		public bool GoShortStop(int m_SharesTraded,double m_Stop,int m_PositionNumber)
		{	// Use this to place STOPLIMIT SHORT order
			return GoShortLimit(m_SharesTraded,m_Stop,-99999, m_PositionNumber,m_DefaultOrderRules);
		}
		
		public bool GoShortStop(int m_SharesTraded,double m_Stop,int m_PositionNumber,OrderRules m_OrderRules)
		{	// Use this to place STOPLIMIT SHORT order
			return GoShortLimit(m_SharesTraded,m_Stop,-99999, m_PositionNumber,m_OrderRules);
		}
#endregion		
		
#region GoShortStopLimit
		public bool GoShortStopLimit(int m_SharesTraded,int m_TrailBidTicks,int m_LimitTicks,int m_PositionNumber)
		{	// Use this to place STOPLIMIT SHORT order
			return GoShortLimit(m_SharesTraded,m_TrailBidTicks,m_LimitTicks,m_PositionNumber,m_DefaultOrderRules);
		}
		
		public bool GoShortStopLimit(int m_SharesTraded,int m_TrailBidTicks,int m_LimitTicks,int m_PositionNumber,OrderRules m_OrderRules)
		{	// Use this to place STOPLIMIT SHORT order
			return GoShortLimit(m_SharesTraded,m_TrailBidTicks,m_LimitTicks,m_PositionNumber,m_OrderRules);
		}			
		
		public bool GoShortStopLimit(int m_SharesTraded,double m_Trigger,double m_Limit,int m_PositionNumber)
		{	// Use this to place STOPLIMIT SHORT orders
			return GoShortLimit(m_SharesTraded,m_Trigger,m_Limit, m_PositionNumber,m_DefaultOrderRules);
		}

		public bool GoShortStopLimit(int m_SharesTraded,double m_Trigger,double m_Limit,int m_PositionNumber,OrderRules m_OrderRules)
		{	// Use this to place STOPLIMIT SHORT orders
			return GoShortLimit(m_SharesTraded,m_Trigger,m_Limit, m_PositionNumber,m_OrderRules);
		}
#endregion
		
#region GoShortLimit depriciated		
		
		public bool GoShortLimit(int m_SharesTraded,int m_TrailBidTicks,int m_LimitTicks,int m_PositionNumber)
		{	// Places a SHORT STOPLIMIT, but confusing, depriciated
			return GoShortLimit(m_SharesTraded,m_TrailBidTicks,m_LimitTicks,m_PositionNumber,m_DefaultOrderRules);
		}	
		
		public bool GoShortLimit(int m_SharesTraded,int m_TrailBidTicks,int m_LimitTicks,int m_PositionNumber,OrderRules m_OrderRules)
		{	// Places a SHORT STOPLIMIT, but confusing, depriciated
			double m_Stop,m_Limit;
			
			m_Stop=m_Strat.GetCurrentBid()-m_TrailBidTicks*m_Strat.TickSize;
			m_Stop=Math.Min(m_Strat.Input[0]-m_Strat.TickSize,m_Stop);		// For a long STOP, make sure 1 tick above Last value
			m_Limit=m_Stop-m_LimitTicks*m_Strat.TickSize;					
			if (m_LimitTicks==-99999)
				m_Limit=-99999;// Flag for just a STOP rather than STOPLIMIT
			
			return GoShortLimit(m_SharesTraded,m_Stop,m_Limit, m_PositionNumber,m_OrderRules);
		}			
			
		public bool GoShortLimit(int m_SharesTraded,double m_Trigger,double m_Limit,int m_PositionNumber,OrderRules m_OrderRules)
		{	// Do not directly call this function, confusing and called from Other function, depriciated 
			int m_Index;
			bool m_NewPos=false;
			LocalOrder m_Current;
			
			if  (m_SharesTraded==0)
			{
				debug("GoShortLimit: Zero Quantity Caught, nothing to do");
				return false;
			}	
			if  ((m_Trigger<0.0) && (m_Limit<0.0))
			{
				debug("GoShortLimit: Limit Order placed with no limit set");
				return false;
			}	
			
			m_Current=m_ActivePositions[m_PositionNumber];
			if  ((m_Current!=null) || (GetMarketPosition(m_PositionNumber)!=MarketPosition.Flat))
			{
				debug("GoShortLimit ORDER REJECTED: Position already exisits",0);
				ThrowError(99,"GoShortLimit ORDER REJECTED: Position already exisits");
				return false;
			}
			
			if (m_Trigger<0.0) // A LIMIT Order Passed
				m_Index=GetOrderEntry("GoShortLimit"+m_PositionNumber);
			else 
			{
				// A STOP/STOPLIMIT Order Passed
				m_Index=GetOrderEntry("GoShortStop"+m_PositionNumber);
				if (m_Index<0) // Didn't find a STOP, try a STOPLIMIT
					m_Index=GetOrderEntry("GoShortStopLimit"+m_PositionNumber);
			}
				
			if (m_Index<0)
			{
				m_NewPos=true;
				m_OrderStack.Add(new LocalOrder(m_Strat,m_PositionNumber));
				m_Index=m_OrderStack.Count-1;
			}
			
			debug("GoShortLimit: Pos: "+m_PositionNumber+" Stop: "+m_Trigger+" Limit: "+m_Limit);
			
			if (m_Limit== -99999)
			{
				SubmitOrder(m_OrderStack[m_Index],m_BarsInProgress, OrderAction.SellShort, OrderType.Stop,
					m_SharesTraded, 0, m_Trigger,
					"ENTER"+m_PositionNumber+m_UniqueID, "GoShortStop"+m_PositionNumber,1,m_OrderRules);				
				if (chartLogLevel>0)
				{
					m_Strat.DrawTriangleDown("GoShortStop"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, m_Trigger, Color.Red);
				}
			}
			else
			{
				if (m_Trigger<0.0)
				{
					SubmitOrder(m_OrderStack[m_Index],m_BarsInProgress, OrderAction.SellShort, OrderType.Limit,
						m_SharesTraded, m_Limit, 0.0,"ENTER"+m_PositionNumber+m_UniqueID, "GoShortLimit"+m_PositionNumber,1,m_OrderRules);
					if (chartLogLevel>0)
					{
						m_Strat.DrawTriangleDown("GoShortLimit"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, m_Limit, Color.Red);
					}
				}
				else
				{
					SubmitOrder(m_OrderStack[m_Index],m_BarsInProgress, OrderAction.SellShort, OrderType.StopLimit,
						m_SharesTraded, m_Limit, m_Trigger,
						"ENTER"+m_PositionNumber+m_UniqueID, "GoShortStopLimit"+m_PositionNumber,1,m_OrderRules);
					if (chartLogLevel>0)
					{
						m_Strat.DrawTriangleDown("GoShortStopLimit"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, m_Trigger, Color.Red);
					}
				}
			}
		
			return true;
		}			
#endregion

#region ExitSLPT
		public bool ExitSLPT(int m_StopTick,int m_ProfitTick,int m_PositionNumber)
		{
			return ExitSLPT(m_StopTick,m_ProfitTick, m_PositionNumber,0.0,m_DefaultOrderRules);
		}
		public bool ExitSLPT(int m_StopTick,int m_ProfitTick,int m_PositionNumber,OrderRules m_OrderRules)
		{
			return ExitSLPT(m_StopTick,m_ProfitTick, m_PositionNumber,0.0,m_OrderRules);
		}
		public bool ExitSLPT(int m_StopTick,int m_ProfitTick,int m_PositionNumber,double m_RefPrice)
		{	// 0 RefPrice will use Ask/Bid price as references
			// 0 for SL or PT will not trigger that order type
			return 	ExitSLPT(m_StopTick,m_ProfitTick,m_PositionNumber,m_RefPrice,m_DefaultOrderRules);
		}
		public bool ExitSLPT(int m_StopTick,int m_ProfitTick,int m_PositionNumber,double m_RefPrice,OrderRules m_OrderRules)
		{	// 0 RefPrice will use Ask/Bid price as references
			// 0 for SL or PT will not trigger that order type
			double m_SLTickOffset,m_PTTickOffset;
			double m_HighRefPrice,m_LowRefPrice;
			double m_LowPrice,m_HighPrice;
			int m_PTZero=1,m_STZero=1;
			LocalOrder m_Current;
			
			m_Current=m_ActivePositions[m_PositionNumber];
			if  ((m_Current==null) && (CountAllEnter(m_PositionNumber)==0))
			{
				debug("ExitSLPT ORDER REJECTED, No pending Entry: Position already FLAT");
				return false;
			}
			
			m_SLTickOffset=m_StopTick*m_Strat.TickSize;			
			m_PTTickOffset=m_ProfitTick*m_Strat.TickSize;
			
			if (m_RefPrice<=0.0)
			{
				m_HighRefPrice=m_Strat.GetCurrentAsk();
				m_LowRefPrice=m_Strat.GetCurrentBid();
			}
			else
			{
				m_HighRefPrice=m_RefPrice;
				m_LowRefPrice=m_RefPrice;
			}
			
			if (m_StopTick<=0) m_STZero=0;
			if (m_ProfitTick<=0) m_PTZero=0;
			
			if (m_Current.GetMarketPosition() == MarketPosition.Long)
			{
				return ExitSLPT((m_LowRefPrice-m_SLTickOffset)*m_STZero,(m_HighRefPrice+m_PTTickOffset)*m_PTZero, m_PositionNumber,m_OrderRules);
			}
			else
			{
				return ExitSLPT((m_HighRefPrice+m_SLTickOffset)*m_STZero,(m_LowRefPrice-m_PTTickOffset)*m_PTZero, m_PositionNumber,m_OrderRules);
			}
		}		
		
		public bool ExitSLPT(double m_Stop,double m_Profit,int m_PositionNumber)
		{
			return ExitSLPT(m_Stop,m_Profit,m_PositionNumber,false,m_DefaultOrderRules);
		}
		
		public bool ExitSLPT(double m_Stop,double m_Profit,int m_PositionNumber,OrderRules m_OrderRules)
		{
			return ExitSLPT(m_Stop,m_Profit,m_PositionNumber,false,m_OrderRules);
		}
		public bool ExitSLPT(double m_Stop,double m_Profit,int m_PositionNumber,bool m_AbsoluteLimits)
		{
			return ExitSLPT(m_Stop,m_Profit,m_PositionNumber, m_AbsoluteLimits,m_DefaultOrderRules);
		}
		public bool ExitSLPT(double m_Stop,double m_Profit,int m_PositionNumber,bool m_AbsoluteLimits,OrderRules m_OrderRules)
		{// AboluteLimits True = Reset SLPT, False = Only improve limits
			LocalOrder m_Current;
			int m_SL,m_PT;
			bool m_UpdateSLTarget=true;
			bool m_UpdatePTTarget=true;
			int m_CurrentQuantity=0;
			int m_SLQuantity=0;
			int m_PTQuantity=0;
			
			double m_Limit=0.0;  // Set this to send a StopLimit (No way to change this yet)
			
			m_Current=m_ActivePositions[m_PositionNumber];
			if  (m_Current==null)
			{
				if (CountAllEnter(m_PositionNumber)==0)
				{
					TraceError("ExitSLPT ORDER REJECTED:  "+m_Stop+" " +m_Profit+ "  Pos: "+m_PositionNumber);
					debug("ExitSLPT ORDER REJECTED: Position already FLAT");
					m_Strat.Log("ExitSLPT ORDER REJECTED: Position already FLAT",NinjaTrader.Cbi.LogLevel.Information);
				//					SubmitPendingOrder(m_BarsInProgress, OrderAction.Buy, OrderType.Market, m_SharesTraded, 0,
				//		0, "ENTER"+m_PositionNumber+m_UniqueID, "GoLongMarket"+m_PositionNumber,m_PositionNumber,m_OrderRules);
					return false;
				}
				else
				{
					TraceError("ExitSLPT ORDER SetPending:  "+m_Stop+" " +m_Profit+ "  Pos: "+m_PositionNumber);
					SubmitPendingBracket(OrderFunctionType.ExitSLPT,-1,m_Stop,m_Profit,m_PositionNumber,m_AbsoluteLimits,m_OrderRules);
					return true;
				}
			}
			else
			{
				m_CurrentQuantity=m_Current.Order.Filled;
				m_SLQuantity=m_CurrentQuantity;
				m_PTQuantity=m_CurrentQuantity;
				debug("ExitSLPT Quantity Filled: "+m_CurrentQuantity,10);		
				TraceError("ExitSLPT Quantity Filled:  "+m_CurrentQuantity+"  SL: "+m_Stop+" PT: " +m_Profit+ "  Pos: "+m_PositionNumber);
			}
			
			m_SL=GetOrderEntry("ExitSL"+m_PositionNumber);
			m_PT=GetOrderEntry("ExitPT"+m_PositionNumber);
			// Generated multiple SL/PTs if Quantity changed (mainly due to Partial Fills)	
			if (((m_SL<0) && (m_Stop>0)))// ||
			//	((GetOrderEntryTotalQuantity("ExitSL"+m_PositionNumber)!=m_Current.Order.Filled)&&(m_SL>=0)))
			{
			//	if (m_SL>=0) // If SL order exists but didn't match quantity, add another ExitSL with delta (Creates multiple ExitSL#'s CAREFULL)
			//		m_SLQuantity=m_CurrentQuantity-GetOrderEntryTotalQuantity("ExitSL"+m_PositionNumber);
				{
					m_UpdateSLTarget=false;
					m_OrderStack.Add(new LocalOrder(m_Strat,m_PositionNumber));
					m_SL=m_OrderStack.Count-1;
				}
			}
			
			if (((m_PT<0) && (m_Profit>0)))// || 
			//	((GetOrderEntryTotalQuantity("ExitPT"+m_PositionNumber)!=m_Current.Order.Filled)&&(m_PT>=0)))
			{
				//if (m_PT>=0) // If PT order exists but didn't match quantity, add another ExitPT with delta (Creates multiple ExitPT#'s CAREFULL)
				//	m_PTQuantity=m_CurrentQuantity-GetOrderEntryTotalQuantity("ExitPT"+m_PositionNumber);
				{
					m_UpdatePTTarget=false;
					m_OrderStack.Add(new LocalOrder(m_Strat,m_PositionNumber));
					m_PT=m_OrderStack.Count-1;
				}
			}
						
			if (m_Current!=null)
			{				
				if (m_Current.GetMarketPosition() != MarketPosition.Flat)
				{

					if (m_Current.GetMarketPosition() == MarketPosition.Long)
					{
						if (m_SL>=0)
						{
							if (((m_UpdateSLTarget) &&((m_Stop>m_OrderStack[m_SL].Order.StopPrice)||(m_AbsoluteLimits))) || 
								( (!m_UpdateSLTarget) && (m_Stop>0)))
							{
								OrderType m_OrderType=OrderType.Stop;
								
								if (m_Limit>0.0)
									m_OrderType=OrderType.StopLimit;
								
								SubmitOrder(m_OrderStack[m_SL],m_BarsInProgress, OrderAction.Sell, m_OrderType,  m_SLQuantity, m_Limit, m_Stop,
									"EXIT"+m_PositionNumber+m_UniqueID, "ExitSL"+m_PositionNumber,m_OrderRules);
								if (chartLogLevel>0)
								{
									m_Strat.DrawTriangleUp("ExitSL"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, m_Stop, Color.Red);
								}
							}
						}
						
						if (m_PT>=0)
						{
							if (((m_UpdatePTTarget) &&((m_Profit>m_OrderStack[m_PT].Order.LimitPrice)||(m_AbsoluteLimits)) && (m_Profit>0)) || 
								( (!m_UpdatePTTarget) && (m_Profit>0)))
							{
								SubmitOrder(m_OrderStack[m_PT],m_BarsInProgress, OrderAction.Sell, OrderType.Limit,  m_PTQuantity, m_Profit, 0,
										"EXIT"+m_PositionNumber+m_UniqueID, "ExitPT"+m_PositionNumber,m_OrderRules);
								if (chartLogLevel>0)
								{
									m_Strat.DrawTriangleDown("ExitPT"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, m_Profit, Color.Green);
								}
							}
						}
					}
					else
					{
						if (m_PT>=0)
						{
							if (((m_UpdatePTTarget) &&((m_Profit<m_OrderStack[m_PT].Order.LimitPrice)||(m_AbsoluteLimits)) && (m_Profit>0)) || 
								( (!m_UpdatePTTarget) && (m_Profit>0)))
							{
								SubmitOrder(m_OrderStack[m_PT],m_BarsInProgress, OrderAction.BuyToCover, OrderType.Limit, m_PTQuantity, m_Profit, 0,
										"EXIT"+m_PositionNumber+m_UniqueID, "ExitPT"+m_PositionNumber,m_OrderRules);
								if (chartLogLevel>0)
								{
									m_Strat.DrawTriangleUp("ExitPT"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, m_Profit, Color.Red);
								}
							}
						}
						
						if (m_SL>=0)
						{
							if (((m_UpdateSLTarget) &&((m_Stop<m_OrderStack[m_SL].Order.StopPrice)||(m_AbsoluteLimits)) && (m_Stop>0)) || 
								( (!m_UpdateSLTarget) && (m_Stop>0)))
							{
								SubmitOrder(m_OrderStack[m_SL],m_BarsInProgress, OrderAction.BuyToCover, OrderType.Stop, m_SLQuantity, 0, m_Stop,
										"EXIT"+m_PositionNumber+m_UniqueID, "ExitSL"+m_PositionNumber,m_OrderRules);
							
								if (chartLogLevel>0)
								{
									m_Strat.DrawTriangleDown("ExitSL"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, m_Stop, Color.Green);
								}
							}
						}
					}
				}
			}
			
			return true;
		}	
#endregion

#region ExitMarket
		public bool ExitMarket(int m_PositionNumber)
		{
			return ExitMarket(m_PositionNumber,m_DefaultOrderRules);
		}		
		public bool ExitMarket(int m_PositionNumber,OrderRules m_OrderRules)
		{
			int	m_Index;
			LocalOrder m_Current;
			
			m_Current=m_ActivePositions[m_PositionNumber];

			if  (m_Current!=null)
			{
				if (m_Current.GetMarketPosition() != MarketPosition.Flat)
				{
					debug("ExitMarket: Before CancelAll "+m_PositionNumber);
					CancelAllOrders(m_PositionNumber);
					debug("ExitMarket: After CancelAll "+m_PositionNumber);
					//m_ActivePositions[m_PositionNumber]=null;
					
					LocalOrder m_local=new LocalOrder(m_Strat,m_PositionNumber);

					if (m_Current.GetMarketPosition()  == MarketPosition.Long)
					{
						m_Index=GetOrderEntry("ExitMarketLong"+m_PositionNumber);
						if (m_Index<0)
						{
							SubmitOrder(m_local,m_BarsInProgress, OrderAction.Sell, OrderType.Market, m_Current.Order.Quantity, 0, 0,
								"EXIT"+m_PositionNumber+m_UniqueID,"ExitMarketLong"+m_PositionNumber,m_OrderRules);
							if (chartLogLevel>0)
								m_Strat.DrawSquare("ExitMarketLong"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, m_Strat.GetCurrentAsk()+m_Strat.TickSize*5, Color.Green);
						} else 
							m_local=null;
					}
					else
					{
						m_Index=GetOrderEntry("ExitMarketShort"+m_PositionNumber);
						if (m_Index<0)
						{
							SubmitOrder(m_local,m_BarsInProgress, OrderAction.BuyToCover, OrderType.Market, m_Current.Order.Quantity, 0, 0,
								"EXIT"+m_PositionNumber+m_UniqueID,"ExitMarketShort"+m_PositionNumber,m_OrderRules);
							if (chartLogLevel>0)
								m_Strat.DrawSquare("ExitMarketShort"+m_PositionNumber+"-"+m_Strat.CurrentBar, true, 0, m_Strat.GetCurrentBid()-m_Strat.TickSize*5,  Color.Red);
						} else 
							m_local=null;
					}
					if (m_local!=null)
						m_OrderStack.Add(m_local);
				}
				else
				{
					debug("ExitMarket: FLAT Nothing to do  "+m_PositionNumber);
					ThrowError(99,"ExitMarket: FLAT Nothing to do  "+m_PositionNumber);
				}
			}
			else
			{
				debug("ExitMarket: Current Position NULL Nothing to do  #"+m_PositionNumber);
				ThrowError(99,"ExitMarket: Current Position NULL Nothing to do  #"+m_PositionNumber);
			}

			return true;
		}
#endregion
	}  
}
