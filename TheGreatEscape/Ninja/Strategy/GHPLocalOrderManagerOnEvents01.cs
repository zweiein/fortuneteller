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
//------------------------------------------------------------------------------
// 	DO NOT SELL THIS CODE OR DISTRIBUTE OUTSIDE OF BMT's Elite Forum Section
//  LOM Update Record
//
//	12/01/2011 - NJAMC - As Issued
//	01/11/2012 - NJAMC - Updated GoXxxxLimit to actually place a limit order, previous only placed STOP and STOPLIMIT
//	02/05/2012 - NJAMC - OnExecute partial Fill
//	02/17/2012 - NJAMC - OnMarketData Added test before displaying Stats Box
//	02/25/2012 - NJAMC - OnTermination required to clean up Timer (Class not destroyed until exit NT)
//	04/01/2012 - NJAMC - Partial Fill Rhobustness added, added some Trace.TraceError() commands for debugging in Trace file
//	04/02/2012 - NJAMC - Fixed Stats for "Non-partial fill" - broken went partial fill case handled
//	04/02/2012 - NJAMC - Improved Memory Management with better SL/PT on Partial Fills
//
 
	/// </summary>
	public partial class LocalOrderManager 
	{
		string REV_ONEVENTS01="01b";
		
		DateTime	m_OMDLastRealTime=DateTime.MinValue;
		DateTime	m_OMDLastSimTime=DateTime.MinValue;

		#region OnMarketData		
		public void OnMarketData(MarketDataEventArgs e)
		{
			
			#region Detect Simulation speed >1X disable timer
			if ((Simulation) && (m_LOMtimer!=null))
			{// This is a hack to disable the internal Timer while >1X in Replay Mode, known to crash script
				// Only check timing if a simulation is running.
				TimeSpan	m_TS,m_TSReal;
				
				if (m_OMDLastRealTime==DateTime.MinValue)
				{
					m_OMDLastRealTime=DateTime.Now+TimeSpan.FromSeconds(1.0);
					m_OMDLastSimTime=e.Time;
				}
				
				if (m_OMDLastRealTime<DateTime.Now)
				{
					m_TSReal=DateTime.Now-m_OMDLastRealTime;
					m_OMDLastRealTime=DateTime.Now+TimeSpan.FromSeconds(1.0);
					m_TS=e.Time-m_OMDLastSimTime;
					m_OMDLastSimTime=e.Time;
					if (m_TS.TotalSeconds>(m_TSReal.TotalMilliseconds/1000+1.5))
					{
						if (m_NormalPlaybackSpeed && m_EnableInternalTimer)
						{
							debug(e.Time+" SIMULATION DETECTED: Fast Speed: "+(m_TSReal.TotalMilliseconds/1000+1.0)+"s="+m_TS.TotalSeconds,1);
							m_Strat.Log(m_Strat.Name+": SIMULATION DETECTED: Fast Speed, timers disabled",NinjaTrader.Cbi.LogLevel.Information);
							m_NormalPlaybackSpeed=false;
							m_LOMtimer.Stop(); // Disable timer
						}
					}
					else
					{
						if (!m_NormalPlaybackSpeed && m_EnableInternalTimer)
						{
							m_Strat.Log(m_Strat.Name+": SIMULATION DETECTED: Normal Speed, timers enabled",NinjaTrader.Cbi.LogLevel.Information);
							debug(e.Time+" SIMULATION DETECTED: Normal Speed: "+(m_TSReal.TotalMilliseconds/1000+1.0)+"s="+m_TS.TotalSeconds,1);
							m_NormalPlaybackSpeed=true;	
						}
					}
				}
				
			}
			#endregion
			
			if (m_ProcessMarketData)
			{
				m_MarketDataManager.ProcessEvent(e);
				if (e.MarketDataType==MarketDataType.Last)
				{
					//if (m_ProcessMarketDepthData)
					//	m_MarketDepthManager.CurrentAskTotolVolume;
					
					m_MarketDataManager.PrintData();
					if (m_ProcessMarketDepthData)
						m_MarketDepthManager.PrintLadder();
					
					//if (m_ProcessMarketDepthData)
					//	m_MarketDepthManager.CurrentBidTotolVolume;
				}
			}
			
			// Perform any other tasks (like entry, exit, dynamic trailing, etc)
			if (!m_Strat.Historical && m_ClearWhenNotHistoric)
			{
				ClearHistoricalData();
				m_ClearWhenNotHistoric=false;
			}
			
			try 
			{
				lock(m_TimerLockObject)
				{

					CheckForExpiredOrders();
					CheckForConditionalTasks();
					UpdateStats(e);
				}
				if (m_DisplayStatusBox)
					UpdateStatusBox();
			}
			catch (Exception ex)
			{
				m_Strat.Print("Exception Caught: "+ex.ToString());
			}
		}
		#endregion	
		
		#region OnExecution		
		public void OnExecution(IExecution execution)
		{
			debug("OnExecute: "+execution.Order.OrderState,10);
			
			if (execution.Order.OrderState == OrderState.Filled) 
			{
				LocalOrder m_TmpOrder;
				
				m_TmpOrder=PopOrder(execution.Order.Token);
				
				debug("OnExecute: STATE=Filled m_TmpOrder="+m_TmpOrder,10);
				if (m_TmpOrder!=null)
				{
					m_TmpOrder.PositionQuantity=execution.Order.Filled;
					debug("OnExec: "+m_TmpOrder.PositionNumber+" Returned LocalOrder "+m_TmpOrder.Order.ToString(),10);
					
					if (m_TmpOrder.GetMarketPosition() != MarketPosition.Flat)
					{
						int m_PosNum;
						
						TraceError("OnExecution: Long/Short  "+m_TmpOrder.OCOGroup+" " +execution.Name+ "  Pos: "+m_TmpOrder.PositionNumber+" Returned LocalOrder "+m_TmpOrder.Order.ToString());
						
						m_PosNum=m_TmpOrder.PositionNumber;
						if (m_ActivePositionPartialFilled[m_PosNum].Length>0)
						{
							PopOrder(m_ActivePositionPartialFilled[m_PosNum]);  // Erase any older pending order during a partial fill
							m_ActivePositionPartialFilled[m_PosNum]=string.Empty;
						}
						m_TmpOrder.BarOfEntry=m_Strat.CurrentBar;

						m_PreviousPositions[m_PosNum]=m_ActivePositions[m_PosNum];
						m_ActivePositions[m_PosNum]=m_TmpOrder;
						if (((m_AutoSLNumberTicks[m_PosNum]+m_AutoPTNumberTicks[m_PosNum])>0))
						{  // Only if SL and/or PL above 0
							debug("OnExecution: ExitSLPT Called for PosNum: "+m_PosNum+" SL: " +m_AutoSLNumberTicks[m_PosNum]+
								"  PT: "+m_AutoPTNumberTicks[m_PosNum]+" AveFill: "+m_TmpOrder.Order.AvgFillPrice,10);
							ExitSLPT(m_AutoSLNumberTicks[m_PosNum],m_AutoPTNumberTicks[m_PosNum],
							m_PosNum,m_TmpOrder.Order.AvgFillPrice);
							debug(" ExitSLPT Returned for PosNum: "+m_PosNum,10);
						}
						CancelAllEnter(m_PosNum);// Clean up any pending Entry Orders
					}
					else 
					{
						int m_PosNum;
					
						m_PosNum=m_TmpOrder.PositionNumber;
						
						if (((m_ActivePositions[m_TmpOrder.PositionNumber]!=null) && 
							(m_PreviousPositions[m_TmpOrder.PositionNumber]==null))  ||
							(m_ActivePositions[m_TmpOrder.PositionNumber].Order.Token!=execution.Order.Token)) // Hopefully Takes care of non-PartialFill path
								m_PreviousPositions[m_TmpOrder.PositionNumber]=m_ActivePositions[m_TmpOrder.PositionNumber];
						
						if ((m_ActivePositions[m_TmpOrder.PositionNumber]!=null) && 
							(m_PreviousPositions[m_TmpOrder.PositionNumber]!=null))
						{
							debug(execution.Name+ " EXECUTE: UpdatePL "+execution.Order.Filled+
							"  "+m_PreviousPositions[m_TmpOrder.PositionNumber].Order.AvgFillPrice+"   "+
							execution.Order.AvgFillPrice,10);
							
							UpdatePL(m_TmpOrder.PositionNumber,execution.Order.Filled,
								m_PreviousPositions[m_TmpOrder.PositionNumber].Order.AvgFillPrice,
								execution.Order.AvgFillPrice,
								m_PreviousPositions[m_TmpOrder.PositionNumber].GetMarketPosition()==MarketPosition.Long,
								true);
						}
						
						// Determine if multiple Orders are cutting back current active position, may need to go with Autofill fix
						if (m_ActivePositions[m_TmpOrder.PositionNumber]!=null) 
						{// Establish FLAT position with multiple EXITS
							m_ActivePositions[m_TmpOrder.PositionNumber].PositionQuantity-=m_TmpOrder.PositionQuantity;
						}
					//	if (m_ActivePositions[m_TmpOrder.PositionNumber].PositionQuantity==0)	// Must be FLAT
						{
							debug(execution.Name+ " EXECUTE: Clearing ActivePosition, Going FLAT",10);
							m_PreviousPositions[m_PosNum]=m_ActivePositions[m_TmpOrder.PositionNumber];
							m_ActivePositions[m_PosNum]=null;
							CancelAllExit(m_PosNum);// Clean up any pending Exit Orders
							debug(execution.Name+ " EXECUTE: CancelAllExit, Going FLAT",10);
						}

					}
				}
				else
				{
					TraceError("OnExecute NULL: "+execution.Order.Token+" NAME: " +execution.Name);
					debug(execution.Name+ " EXECUTE: PopOrder Returned NULL   ORDER="+execution.Order.ToString(),10);
					if ("Exit on close"==execution.Name)
					{		// Close of Session, automatically fired by NinjaTrader
						for (int i=0;i<m_MaxOrderPositions;++i)
						{
							CancelAllOrders(i);
							if (m_PreviousPositions[i]!=null)
							{
								if (m_PreviousPositions[i].GetMarketPosition()!= MarketPosition.Flat)
								{
									UpdatePL(i,m_PreviousPositions[i].Order.Filled,
										m_PreviousPositions[i].Order.AvgFillPrice,
										execution.Order.AvgFillPrice,
										m_PreviousPositions[i].GetMarketPosition()==MarketPosition.Long,
										true);							
								}
							}
							m_PreviousPositions[i]=null;
							m_ActivePositions[i]=null;
						}
											}
				}

			}
			
			if (execution.Order.OrderState == OrderState.PartFilled)  
			{
				LocalOrder m_TmpOrder;

				debug("EXECUTE: PartFilled",10);
				TraceError("EXECUTE: ENTRY PartFilled "+execution.Order.ToString());

				
				m_TmpOrder=GetOrder(execution.Order.Token);

				if (m_TmpOrder!=null)
				{
						int m_PosNum;
						
						m_TmpOrder.PositionQuantity=execution.Order.Filled;
					//m_TmpOrder.Order.OrderState
						m_PosNum=m_TmpOrder.PositionNumber;
						m_ActivePositionPartialFilled[m_PosNum]=execution.Order.Token;
						m_TmpOrder.BarOfEntry=m_Strat.CurrentBar;
					
						if ((m_ActivePositions[m_PosNum]!=null) && (m_ActivePositions[m_PosNum].Order!=null))
							if (m_ActivePositions[m_PosNum].Order.OrderState!=OrderState.PartFilled)
							{
								m_PreviousPositions[m_PosNum]=m_ActivePositions[m_PosNum];  // Don't do this yet, let FILL take care of it
							}
						m_ActivePositions[m_PosNum]=m_TmpOrder;  // Don't do this yet, let FILL take care of it
						m_ActivePositions[m_PosNum].Order=execution.Order;
							
						debug("EXECUTE: PartFilled, final order status="+m_ActivePositions[m_PosNum].Order.OrderState+
							"  Order Action="+m_ActivePositions[m_PosNum].Order.OrderAction,10);

						if ((m_TmpOrder.GetMarketPosition() != MarketPosition.Flat))
						{
							if ((m_AutoSLNumberTicks[m_PosNum]+m_AutoPTNumberTicks[m_PosNum])>0)
							{  // Only if SL and/or PL above 0
								debug(" ExitSLPT Called for PosNum: "+m_PosNum,10);
								TraceError("ExitSLPT Called for PosNum: "+m_PosNum);
								
								ExitSLPT(m_AutoSLNumberTicks[m_PosNum],m_AutoPTNumberTicks[m_PosNum],
								m_PosNum,m_TmpOrder.Order.AvgFillPrice);
							}							
						}
							/*
						UpdatePL(m_TmpOrder.PositionNumber,m_TmpOrder.Order.Filled,
							m_ActivePositions[m_TmpOrder.PositionNumber].Order.AvgFillPrice,
							m_TmpOrder.Order.AvgFillPrice,
							m_ActivePositions[m_TmpOrder.PositionNumber].GetMarketPosition()==MarketPosition.Long,
							false);*/
				}
				else  // Partial Fill, no order?
				{
					if ("Exit on close"==execution.Name)
					{		// Close of Session, automatically fired by NinjaTrader
						TraceError("EXECUTE: PartFilled [Exit on close] "+execution.Order.ToString());
					}
					else
					{
						TraceError("EXECUTE: PartFilled NO ORDER MATCHED Returned LocalOrder "+execution.Order.ToString());
					}
				}
			}
			
			if (execution.Order.OrderState == OrderState.Unknown) 
				debug("EXECUTE: Unknown",1);// Should never be here
		}
		#endregion	
		
		#region OnOrderUpdate		
		public void OnOrderUpdate(IOrder order)
		{
			if (order.OrderState == OrderState.Accepted) debug("UPDATE: Accepted",10);
			if (order.OrderState == OrderState.Cancelled) 
			{
				LocalOrder m_order;
				
				m_order=GetOrder(order.Token);
				if (m_order!=null)
				{				
					if (m_order.Order.OrderType==OrderType.Market) // Clear any pending Market Order
						m_PendingOrders[m_order.PositionNumber]=null;
				}
				
				TraceError("OnOrderUpdate: Canceled "+order.ToString());
				
//WARNING ->  Disabled CleanUpCancelledPartialOrder bug likely messing up order when PT or SL filled, OCO cancels compliment which strikes all orders
				//CleanUpCancelledPartialOrder(order.Token);

				debug("OnOrderUpdate: Canceled OrderPopped "+order.ToString(),10);// Discard this order from OrderStack
			}
			if (order.OrderState == OrderState.Filled)
			{
				LocalOrder m_order;
				
				m_order=GetOrder(order.Token);
				if (m_order!=null)
				{				
					if (m_order.Order.OrderType==OrderType.Market) // Clear any pending Market Order
						m_PendingOrders[m_order.PositionNumber]=null;
				}
				
				debug("UPDATE: Filled",10);
			}

			if (order.OverFill) {
				TraceError("OnExecution: ERROR: OVERFILLED " +order.Name+" Returned LocalOrder "+order.ToString(),0);
				ThrowError(1,"OnExecution: ERROR: OVERFILLED "+order.Name+" Returned LocalOrder "+order.ToString());
				debug("ERROR: OVERFILLED "+order.Name+" Returned LocalOrder "+order.ToString(),0);
			}

			if (order.OrderState == OrderState.PartFilled) debug("UPDATE: PartFilled"+order.ToString(),10);
			if (order.OrderState == OrderState.PendingCancel) 
			{
				LocalOrder m_order;
				
				m_order=GetOrder(order.Token);

				debug("UPDATE: PendingCancel: "+order.ToString(),10);
			}
			if (order.OrderState == OrderState.PendingChange) debug("UPDATE: PendingChange: "+order.ToString(),10);
			if (order.OrderState == OrderState.PendingSubmit) debug("UPDATE: PendingSubmit: "+order.ToString(),10);
			if (order.OrderState == OrderState.Rejected) 
			{
				LocalOrder m_order;
				
				debug("UPDATE: Rejected: "+order.ToString(),10);
				m_order=PopOrder(order.Token);
				if (m_order!=null)
				{
					if (m_order.Order.OrderType==OrderType.Market) // Clear any pending Market Order
						m_PendingOrders[m_order.PositionNumber]=null;
					// Do something about the rejection
					ThrowError(99,"OrderUpdate: Order Rejected!"+order.ToString());
					debug("OnOrderUpdate: Canceled OrderPopped "+m_order.Order.ToString(),10);// Discard this order from OrderStack
					TraceError("OnOrderUpdate: Canceled OrderPopped "+m_order.Order.ToString(),0);// Discard this order from OrderStack
				}
			}
			if (order.OrderState == OrderState.Working) debug("UPDATE: Working: "+order.ToString(),10);
			if (order.OrderState == OrderState.Unknown) debug("UPDATE: Unknown: "+order.ToString(),10);
		}
		#endregion

		#region OnMarketDepth		
		public void OnMarketDepth(MarketDepthEventArgs e)
		{
			// Print some data to the Output window
			//if (e.MarketDataType == MarketDataType.Ask && e.Operation == Operation.Update)
			//	Print("The most recent ask change is " + e.Price + " " + e.Volume);
			//m_Strat.Print("Depth: "+e.ToString());
			if (m_ProcessMarketDepthData)
				m_MarketDepthManager.ProcessEvent(e);
		}
		#endregion		
		
		#region OnTermination					
		public void OnTermination()
		{
			// Clean up your resources here
			int i;
			if (m_LOMtimer!=null)
			{
				m_LOMtimer.Stop();
				m_LOMtimer=null;
			}
			for (i=0;i<m_MaxOrderPositions;++i)
			{
				//m_SLShort[i].Dispose();
				//m_SLLong[i].Dispose();
			}
		}
		#endregion				
		
		#region Status Box
		private void UpdateStatusBox()
		{
			// Print our current P&L to the upper right hand corner of the chart
				double actprice;
				string m_Display="",m_hist="";
			
				if (m_Strat.Historical)
					m_hist="HIST";
				
				m_Display = m_hist+"P&L: " + GetTotalPL().ToString("C")+" [0] "+
										GetTotalPL(0).ToString("C")+" [1] "+GetTotalPL(1).ToString("C");
				
				if (!m_NormalPlaybackSpeed)
					m_Display="SIM >1X IT disabled\n"+m_Display;
				
				if (GetMarketPosition() != MarketPosition.Flat) 
				{
					m_Display = m_Display + "\n"+m_hist+GetMarketPosition() + GetTotalUnrealizedPL().ToString("C")+" [0] "+
										GetTotalUnrealizedPL(0).ToString("C")+" [1] "+GetTotalUnrealizedPL(1).ToString("C");; 
				}
	//			m_Display = m_Display + "\nP&L this Day: " + (Performance.AllTrades.TradesPerformance.Currency.CumProfit - cumprofit).ToString("C");				
				m_Display = m_Display + "\n#Cons[0]: " +m_NumberConsecutiveWinLoss[0] + " #Cons[1]: "+m_NumberConsecutiveWinLoss[1];
				m_Display = m_Display + "\nTick Counter: " + m_Strat.Bars.TickCount.ToString(); 

				m_Strat.DrawTextFixed("PnL", m_Display, TextPosition.TopLeft,Color.Red, new Font("Arial", 8), Color.Black, Color.LightGray, 100);

				if ((m_NumberConsecutiveWinLoss[0]<0) )
				{
					//DrawSquare("BlockHigh"+m_ConsecutiveLosses.ToString(),true, 0, m_HighSinceLastWinEnter, Color.Red);
					//DrawSquare("BlockLow"+m_ConsecutiveLosses.ToString(),true, 0,m_LowSinceLastWinEnter, Color.Red);
					m_Strat.DrawHorizontalLine("BlockHigh", m_BadBandHigh[0], Color.Red);
					m_Strat.DrawHorizontalLine("BlockLow", m_BadBandLow[0], Color.Red);
				}
				else
				{
					m_Strat.RemoveDrawObject("BlockHigh");
					m_Strat.RemoveDrawObject("BlockLow");
				}				
		
		}
		#endregion		
		
		
	}    
}
