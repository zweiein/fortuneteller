#region Using declarations
using System;
using System.ComponentModel;
using System.Threading;
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
// 	01/28/2012 - NJAMC - Added Commission extraction from NinjaTrader as default value.
// 	02/17/2012 - NJAMC - Added Display Box & Associated Function
// 	02/25/2012 - NJAMC - Integrating Timer function and Destructor, commission default moved to set
// 	04/01/2012 - NJAMC - Added features to assist with Partial Fills
// 	04/15/2012 - NJAMC - Added features to assist with ExitSLPT working with partials, better memeory management
// 	04/18/2012 - NJAMC - Added default Order Good Until settings
//
// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
	public partial class LocalOrderManager 
	{
		string REV_STACKFUNCTIONS01="01b";
		string REV_MINOR="2a";
        const int MAXORDERPOSITIONSDEFAULT = 10;
		
#region LocalOrderManager Variables
		private Strategy 					m_Strat;
		
		private bool						m_TradingEnabled=true;	// When false, prevent order processing

		private string						m_UniqueID;
		private bool						m_ClearWhenNotHistoric=true;
		private int 						m_MaxOrderPositions=10;
		private int 						m_MaxTickData=255;
		private int							m_MarketEventPendingTasks=0; 
		private double[]					m_MarketLastArray;
		private List<LocalOrder> 			m_OrderStack=new List<LocalOrder>();
		private List<ConditionalOrder>		m_ConditionalStack=new List<ConditionalOrder>();
		private LocalOrder[]				m_ActivePositions;
		private LocalOrder[]				m_PreviousPositions;
		private LocalOrder[]				m_PendingOrders;
		private string[]					m_ActivePositionPartialFilled;
		private int 						logLevel=0;	//0=All, 1 - Important, 10 - Trade transactions
		private int 						chartLogLevel=0;
		private int							fileStreamLevel=0;
		private int[] 						m_AutoSLNumberTicks;	// zero disables feature
		private int[] 						m_AutoPTNumberTicks;	// zero disables feature
		private int[] 						m_NumberConsecutiveWinLoss;	// + wins, - lossese
		private int[] 						m_NumberWins;	// + wins, - lossese
		private int[] 						m_NumberLosses;	// + wins, - lossese
		private int[] 						m_NumberWinsHist;	// + wins, - lossese
		private int[] 						m_NumberLossesHist;	// + wins, - lossese
		private double[] 					m_PLWins;	// Total Profit trades value
		private double[] 					m_PLLosses;	// Total Lossing Trades value
		private double[] 					m_PLWinsHist;	// Total Profit trades value
		private double[] 					m_PLLossesHist;	// Total Lossing Trades value
		private double[] 					m_BadBandHigh;	// + wins, - lossese
		private double[] 					m_BadBandLow;	// + wins, - lossese
		private bool						m_OnMarketDataConnected=false;
		private bool						m_OnExecutionConnected=false;
		private bool						m_OnOrderUpdateConnected=false;
		private double						m_ContractCommission=0.00;

		private bool						m_DisplayStatusBox=true;
		
		private bool						m_EnableInternalTimer=true; // Allow for internal timer function
		private bool						m_NormalPlaybackSpeed=true;	// In Simulation Mode if >1X, set to false to disable timer function
		private bool						m_ProcessMarketDepthData=false;
		private bool						m_ProcessMarketData=false;
		
		private int 						m_BarsInProgress=0;
		private RejectedOrderAction			m_RejectedOrderHandling=RejectedOrderAction.ExitMarket;
		// Order Life Duration
		private OrderRules					m_DefaultOrderRules;
		//
		private MarketDataManager			m_MarketDataManager;	// Holds level II data
		private MarketDepthManager			m_MarketDepthManager;	// Holds level II data
		
		private DataSeries[] 				m_SLShort;	// 
		private DataSeries[] 				m_SLLong;	// 
		private int[] 						m_SLShortBarOffset;	// 
		private int[] 						m_SLLongBarOffset;	// 
		private int[] 						m_SLShortTicksOffset;	// 
		private int[] 						m_SLLongTicksOffset;	// 

        // CKKOH
        private DataSeries[]                m_PTShort;	// 
        private DataSeries[]                m_PTLong;	// 
        private int[]                       m_PTShortBarOffset;	// 
        private int[]                       m_PTLongBarOffset;	// 
        private int[]                       m_PTShortTicksOffset;	// 
        private int[]                       m_PTLongTicksOffset;	// 
#endregion
		
#region LOM Destructor		
		~ LocalOrderManager()
		{
			OnTermination();
		}
#endregion
		
#region LocalOrderManager(Strategy m_strategy,int m_BarsInProgressValue) Initializer
		public LocalOrderManager(Strategy m_strategy,int m_BarsInProgressValue)
			:this(m_strategy,m_BarsInProgressValue,MAXORDERPOSITIONSDEFAULT)
		{ 
			return;
		}
		public LocalOrderManager(Strategy m_strategy,int m_BarsInProgressValue,int m_MaxPos) 
		{ 
			int i;
			
			m_MaxOrderPositions=m_MaxPos;
			m_ClearWhenNotHistoric=true;
			m_BarsInProgress=m_BarsInProgressValue;
			m_Strat=m_strategy; 
			m_Strat.Unmanaged=true;
			m_Strat.RealtimeErrorHandling =RealtimeErrorHandling.TakeNoAction;
			m_MarketLastArray=new double[m_MaxTickData];

			//m_OrderStack=new List<LocalOrder>();
			//m_ConditionalStack=new List<ConditionalOrder>();
			m_ActivePositions=new LocalOrder[m_MaxOrderPositions];
			m_ActivePositionPartialFilled=new string[m_MaxOrderPositions];
			m_PreviousPositions=new LocalOrder[m_MaxOrderPositions];
			m_PendingOrders=new LocalOrder[m_MaxOrderPositions];
			m_AutoSLNumberTicks=new int[m_MaxOrderPositions];
			m_AutoPTNumberTicks=new int[m_MaxOrderPositions];
			m_SLShort=new DataSeries[m_MaxOrderPositions];
			m_SLLong=new DataSeries[m_MaxOrderPositions];
			m_SLShortBarOffset=new int[m_MaxOrderPositions];
			m_SLLongBarOffset=new int[m_MaxOrderPositions];
			m_SLShortTicksOffset=new int[m_MaxOrderPositions];
			m_SLLongTicksOffset=new int[m_MaxOrderPositions];
            //CKKOH
            m_PTShort = new DataSeries[m_MaxOrderPositions];
            m_PTLong = new DataSeries[m_MaxOrderPositions];
            m_PTShortBarOffset = new int[m_MaxOrderPositions];
            m_PTLongBarOffset = new int[m_MaxOrderPositions];
            m_PTShortTicksOffset = new int[m_MaxOrderPositions];
            m_PTLongTicksOffset = new int[m_MaxOrderPositions];
			
			m_NumberConsecutiveWinLoss=new int[m_MaxOrderPositions];	
			m_NumberWins=new int[m_MaxOrderPositions];
			m_NumberLosses=new int[m_MaxOrderPositions];
			m_NumberWinsHist=new int[m_MaxOrderPositions];
			m_NumberLossesHist=new int[m_MaxOrderPositions];
			
			m_PLWins=new double[m_MaxTickData];
			m_PLLosses=new double[m_MaxTickData];
			m_BadBandHigh=new double[m_MaxTickData];
			m_BadBandLow=new double[m_MaxTickData];
			m_PLWinsHist=new double[m_MaxTickData];
			m_PLLossesHist=new double[m_MaxTickData];
			
			for (i=0;i<m_MaxOrderPositions;++i)
			{
				m_ActivePositions[i]=null;
				m_PreviousPositions[i]=null;
				m_PendingOrders[i]=null;
				m_ActivePositionPartialFilled[i]=string.Empty;
				m_AutoSLNumberTicks[i]=0;
				m_AutoPTNumberTicks[i]=0;
				m_NumberConsecutiveWinLoss[i]=0;
				m_PLWins[i]=0.0;
				m_PLLosses[i]=0.0;
				m_PLWinsHist[i]=0.0;
				m_PLLossesHist[i]=0.0;
				m_BadBandHigh[i]=-1;
				m_BadBandLow[i]=-1;
				m_NumberWins[i]=0;
				m_NumberLosses[i]=0;
				m_NumberWinsHist[i]=0;
				m_NumberLossesHist[i]=0;
				m_SLShortBarOffset[i]=-1;
				m_SLLongBarOffset[i]=-1;
			}
			
			for (i=0;i<m_MaxTickData;++i)
			{
				m_MarketLastArray[i]=0.0;
			}
			
			m_DefaultOrderRules=new OrderRules();
			m_MarketDepthManager=new MarketDepthManager(m_Strat);
			m_MarketDataManager=new MarketDataManager(m_Strat);
			
			m_DefaultOrderRules.Disabled(true);
			m_UniqueID="_"+m_Strat.Id;

			Trace.TraceError("LOM INIT: "+m_Strat.Name+" "+SoftwareID+m_UniqueID);
			m_Strat.Log("LOM INIT: "+m_Strat.Name+" "+SoftwareID+m_UniqueID,NinjaTrader.Cbi.LogLevel.Information);
		}
#endregion
		
#region GlobalLocalOrderVariables
		public string SoftwareID
		{
			get { return "LOM:"+REV_LOM01+"."+REV_STACKFUNCTIONS01+"."+REV_ONEVENTS01
					+"."+REV_MANAGERSTATS01+"."+LocalOrder.REV_CLASSLOCALORDER+"."
					+ConditionalOrder.REV_CLASSCONDORDER+"."+REV_TIMER01+"."
					+REV_UTILITIES01+"."+REV_VIRTUAL01+"."+0+"."+0+"."+REV_MINOR; } 
		}
// m_ProcessMarketData
		public bool TradingEnabled
		{
			get { return m_TradingEnabled; } set { m_TradingEnabled=value;}
		}
		public RejectedOrderAction RejectedOrderHandling
		{
			get { return m_RejectedOrderHandling; } set { m_RejectedOrderHandling=value;}
		}		
		public bool Simulation
		{	// Strategy/Account set to Simulation Mode?
			get { return m_Strat.Account.Mode==Mode.Simulation; } 
		}
		
		public bool EnableInternalTimer
		{
			get { return m_EnableInternalTimer; }  set { m_EnableInternalTimer=value;}
		}	
		
		public bool ProcessMarketData
		{
			get { return m_ProcessMarketData; }  set { m_ProcessMarketData=value;}
		}	
		
		public bool ProcessMarketDepthData
		{
			get { return m_ProcessMarketDepthData; }  set { m_ProcessMarketDepthData=value;}
		}	

		public DateTime GetTimeNow()
		{
			return DateTime.Now;
		}
		
		public string UniqueID
		{
			get { return m_UniqueID; }
		}	
		#endregion
		
#region SubmitOrder()		
		public bool SubmitOrder(LocalOrder m_Current,int barsInProgressIndex, OrderAction orderAction, OrderType orderType,
			int quantity, double limitPrice, double stopPrice, string ocoId, string signalName,OrderRules m_GU)
		{
			return SubmitOrder(m_Current,barsInProgressIndex,orderAction,orderType,
				quantity, limitPrice, stopPrice, ocoId, signalName,0,m_GU);
		}
		
		public bool SubmitOrder(LocalOrder m_Current,int barsInProgressIndex, OrderAction orderAction, OrderType orderType,
			int quantity, double limitPrice, double stopPrice, string ocoId, string signalName,int m_OnlyUpdateDirection,
			OrderRules m_GU)
		{
		debug("Submit Order SetupTimer: "+m_GU.GoodUntilRule+"  "+m_GU.GoodTimeSpan+" secs   TimeEnabled?: "+m_EnableInternalTimer,1);

			m_Current.OrderRules=m_GU;
			
			switch(m_GU.GoodUntilRule)
			{ // Go position dependent with m_Current.Position
				case 1: //Good Until Any
					m_Current.GoodUntilAny(m_GU.BarGood,m_GU.GoodTimeSpanSec,m_GU.TicksAstray,m_GU.ConvertExpiredToMarket);
					if (m_GU.GoodTimeSpanMS>0 && m_EnableInternalTimer && m_NormalPlaybackSpeed)
						SetupTimer(100);//100 ms for now
					break;
				case 2: //Good Until ALL conditions met
					m_Current.GoodUntilAll(m_GU.BarGood,m_GU.GoodTimeSpanSec,m_GU.TicksAstray,m_GU.ConvertExpiredToMarket);
					if (m_GU.GoodTimeSpanMS>0 && m_EnableInternalTimer && m_NormalPlaybackSpeed)
						SetupTimer(100);//100 ms for now
					break;
				default:
					break;
			}
			if (!m_TradingEnabled)
				return false;	// trading disabled,don't pass order to broker, pass to internal Order Filling system (TBD)
			
			// ------- Try to block duplicate orders
			if ((!m_GU.RelativePosition) && (orderType==OrderType.Market))
				// not a relative position, check for duplicate orders being processed on MarketOrders (ExitMarket has been biggest problem)
			{
				if (m_PendingOrders[m_Current.PositionNumber]==null)
				{
					m_PendingOrders[m_Current.PositionNumber]=m_Current; // Okay to process order store pending order until cleared
				}
				else
				{	
					if (m_PendingOrders[m_Current.PositionNumber].Order!=null) // Check if update of current pending order
					{
						if (m_PendingOrders[m_Current.PositionNumber].Order!=m_Current.Order) // Not an update of the current order, cancel
							m_Current.CancelOrder();
					}
					else
						m_Current.CancelOrder(); // Pending Order not updated yet, this order must be received too fast, cancel
				}
			}
			
			try 
			{
			return m_Current.SubmitOrder(barsInProgressIndex, orderAction, orderType,
				quantity, limitPrice, stopPrice, ocoId, signalName,m_OnlyUpdateDirection);
			}
			catch (Exception e)
			{
				m_Strat.Print("Exception Caught: "+e.ToString());
			}
			return false;
		}
#endregion	
		
#region SetCommission()		
		public void SetCommission()
		{
		 	SetCommission(-1.0);
		}
		public void SetCommission(double m_Com)
		{// Must be called from OnStartUp or during strategy running.  During INIT, Instrument isn't value
			if (m_Com<0)
				m_ContractCommission=2*m_Strat.Instrument.MasterInstrument.GetCommission(1,m_Strat.Instrument.MasterInstrument.Commission.ProviderCommissions[0].Provider);
			else
				m_ContractCommission=m_Com;
		}	
#endregion
		
#region SimpleFunctions
		private void ClearHistoricalData()
		{
			m_ConditionalStack.Clear();
		}
		
		public void SetStatsBoxVisable(bool m_Stats)
		{
			m_DisplayStatusBox=m_Stats;
		}	
		
		public void SetDebugLevels(int m_LogOutput, int m_ChartOutput, bool m_TraceOrders,
			int m_StreamOutput)
		{
			logLevel=m_LogOutput;
			chartLogLevel=m_ChartOutput;
			m_Strat.TraceOrders			= m_TraceOrders;
			fileStreamLevel=m_StreamOutput;  //Nothing implemented as of yet
		}	
		
		public IOrder GetOrderPosition(int m_PositionNumber)
		{
			 return m_ActivePositions[m_PositionNumber].Order; 
		}			
		public int GetBarOfEntry(int m_PositionNumber)
		{
			 return m_ActivePositions[m_PositionNumber].BarOfEntry; 
		}			
		public void SetAutoSLPTTicks(int m_StopLoss, int m_ProfitTarget,int m_PositionNumber)
		{	// Only impacts initial SL/PT at order Filled
			if (m_StopLoss>0)
				m_AutoSLNumberTicks[m_PositionNumber]=m_StopLoss;
			else
				m_AutoSLNumberTicks[m_PositionNumber]=0;
			
			if (m_ProfitTarget>0)
				m_AutoPTNumberTicks[m_PositionNumber]=m_ProfitTarget;
			else
				m_AutoPTNumberTicks[m_PositionNumber]=0;				
				
		}
		#endregion
		
		public LocalOrder PopOrder(string m_Token)
		{
			int i=0;
			LocalOrder m_TempOrder=null;

			for (i=0;i<m_OrderStack.Count;++i)
			{
				if (m_OrderStack[i].Order!=null)
					if (m_OrderStack[i].Order.Token==m_Token)
					{
						m_TempOrder=m_OrderStack[i];
						m_OrderStack.RemoveAt(i);
						break;
					}
			}
			return m_TempOrder;
		}	
		
#region GetOrderFunctions		
		public int GetOrderEntry(string m_Entry)
		{
			int i=0;
			int index=-99; 
			
			for (i=0;i<m_OrderStack.Count;++i)
			{
				if (m_OrderStack[i].Order!=null)
					if ((m_OrderStack[i].Order.Name==m_Entry) &&
						(m_OrderStack[i].Canceled==false))
					{
						index=i;
						break;
					}
			}
			return index;
		}	
		
		public int GetOrderEntryTotalQuantity(string m_Entry)
		{
			int i=0;
			int quant=0; 
			
			for (i=0;i<m_OrderStack.Count;++i)
			{
				debug(i+" ENTRY GET TOTAL: "+m_Entry+" Name: "+m_OrderStack[i].Order.Name+" TotalCount: "+m_OrderStack.Count,0);

				if (m_OrderStack[i].Order!=null)
				{// Within Order Creation, Order could be NULL SL vs PT for example
					if (m_OrderStack[i].Order.Name.StartsWith(m_Entry,StringComparison.CurrentCultureIgnoreCase) &&
						(m_OrderStack[i].Canceled==false))
					{
						quant+=m_OrderStack[i].Order.Quantity;
					}
				}
			}
			if (quant==0)
					debug(" ENTRY GET TOTAL: "+m_Entry+" EMPTY LIST! TotalCount: "+m_OrderStack.Count,0);

			return quant;
		}	
		
		public int GetOrderOCOTotalQuantity(string m_Entry)
		{
			int i=0;
			int quant=0; 
			
			for (i=0;i<m_OrderStack.Count;++i)
			{
				debug(i+" ENTRY OCO GET TOTAL: "+m_Entry+" OCO: "+m_OrderStack[i].Order.Oco+" TotalCount: "+m_OrderStack.Count,0);

				if (m_OrderStack[i].Order!=null)
				{// Within Order Creation, Order could be NULL SL vs PT for example
					if (m_OrderStack[i].Order.Oco.StartsWith(m_Entry,StringComparison.CurrentCultureIgnoreCase) &&
						(m_OrderStack[i].Canceled==false))
					{
						quant+=m_OrderStack[i].Order.Quantity;
					}
				}
			}
			if (quant==0)
					debug(" ENTRY OCO GET TOTAL: "+m_Entry+" EMPTY LIST! TotalCount: "+m_OrderStack.Count,0);

			return quant;
		}			
		private LocalOrder GetOrder(string m_Token)
		{
			int i=0;
			LocalOrder m_TempOrder=null;			
			
			for (i=0;i<m_OrderStack.Count;++i)
			{
				if (m_OrderStack[i].Order!=null)
					if (m_OrderStack[i].Order.Token==m_Token)
					{
						m_TempOrder=m_OrderStack[i];
					}
			}
			return m_TempOrder;
		}	
		#endregion
		
#region CancelFunctions
		public int CancelAllEnter(int m_PositionNumber)
		{	
			return CancelOCOHold("ENTER"+m_PositionNumber+m_UniqueID);
		}

		public int CancelAllExit(int m_PositionNumber)
		{	
			return CancelOCOHold("EXIT"+m_PositionNumber+m_UniqueID);
		}
		
		public int CancelSignalName(string m_Name)
		{
			int i=0;
			int count=0;
			
			for (i=m_OrderStack.Count-1;i>=0;--i)
			{
				if (m_OrderStack[i].SignalName==m_Name)
				{
					m_OrderStack[i].CancelOrder();
					m_OrderStack.RemoveAt(i);
					count++;
				}
			}
			return count;
		}	
		#endregion
		
#region CountFunction()
		public int CountAllEnter(int m_PositionNumber)
		{	
			return CountOCOHold("ENTER"+m_PositionNumber+m_UniqueID,-1);
		}

		public int CountAllExit(int m_PositionNumber)
		{	
			return CountOCOHold("EXIT"+m_PositionNumber+m_UniqueID,-1);
		}
		public int CountOCOHold(string m_OCO,int m_Pos)
		{
			int i=0;
			int count=0;
			
			for (i=m_OrderStack.Count-1;i>=0;--i)
			{
				if ((m_OrderStack[i].OCOGroup==m_OCO) &&
					((m_Pos<0) || (m_OrderStack[i].PositionNumber==m_Pos)))
				{
					count++;
				}
			}
			return count;
		}			
		public int CountSignalName(string m_Name)
		{
			int i=0;
			int count=0;
			
			for (i=m_OrderStack.Count-1;i>=0;--i)
			{
				if (m_OrderStack[i].SignalName==m_Name)
				{
					m_OrderStack[i].CancelOrder();
					m_OrderStack.RemoveAt(i);
					count++;
				}
			}
			return count;
		}
#endregion
		
#region CancelOCOHold()
		public int CancelOCOHold(string m_OCO)
		{
			return CancelOCOHold(m_OCO,-1);
		}	
		
		public int CancelOCOHold(string m_OCO,int m_Pos)
		{
			int i=0;
			int count=0;
			
			for (i=m_OrderStack.Count-1;i>=0;--i)
			{
				if ((m_OrderStack[i].OCOGroup==m_OCO) &&
					((m_Pos<0) || (m_OrderStack[i].PositionNumber==m_Pos)))
				{
					m_OrderStack[i].CancelOrder();
					m_OrderStack.RemoveAt(i);
					count++;
				}
			}
			return count;
		}	
#endregion
		
#region CancelAllOrders()
		public int CancelAllOrders(int m_PositionNumber)
		{
			int i=0;
			int count=0;
			
			for (i=m_OrderStack.Count-1;i>=0;--i)
			{
				if (m_OrderStack[i].PositionNumber==m_PositionNumber)
				{
					if (m_OrderStack[i].Order!=null)
					{
						debug(" CancelAllOrders: "+m_OrderStack[i].Order.ToString()+" Count = "+m_OrderStack.Count,10);
						m_OrderStack[i].CancelOrder();
					}
					m_OrderStack.RemoveAt(i);
					count++;
				}
			}		
			return count;
		}	
		
		public int CancelAllOrders()
		{
			int i=0;
			int count=0;
			
			for (i=m_OrderStack.Count-1;i>=0;--i)
			{
				if (m_OrderStack[i].Order!=null)
				{
					debug(" CancelAllOrders: "+m_OrderStack[i].Order.ToString()+" Count = "+m_OrderStack.Count,10);
					m_OrderStack[i].CancelOrder();
				}
				m_OrderStack.RemoveAt(i);
				count++;
			}		
			return count;
		}	
#endregion
		
#region CleanUpCancelledPartialOrder(string m_Token)
		public int CleanUpCancelledPartialOrder(string m_Token)
		{
			int i=0;
			int count=0;
			
			for (i=0;i<m_MaxOrderPositions;++i)
			{
				if (m_ActivePositionPartialFilled[i]==m_Token)
				{
					PopOrder(m_Token);
					
					if (m_ActivePositions[i].GetMarketPosition()!= MarketPosition.Flat)
					{
						UpdatePL(i,m_ActivePositions[i].Order.Filled,
						m_PreviousPositions[i].Order.AvgFillPrice,
						m_ActivePositions[i].Order.AvgFillPrice,
						m_ActivePositions[i].GetMarketPosition()==MarketPosition.Long,
						true);
					}
					
					count++;
					m_ActivePositionPartialFilled[i]=string.Empty;
				}
			}
			return count;
		}
#endregion
		
#region CheckForExpiredOrders()		
		public int CheckForExpiredOrders()
		{
			int i=0;
			int count=0;
			lock(m_TimerLockObject)
			{
							debug(" ExpiredOrder(check) OrderStackCount: "+m_OrderStack.Count,10);
				for (i=m_OrderStack.Count-1;i>=0;--i)
				{
							debug(" ExpiredOrder(check) Loop: "+i+"  Order: "+m_OrderStack[i].Order.ToString(),10);
					if (m_OrderStack[i].GoodUntilPending)
					{
							debug(" ExpiredOrder(check) GUP: "+i+"  Order: "+m_OrderStack[i].Order.ToString(),10);
						if (m_OrderStack[i].Expired())
						{
							debug(" ExpiredOrder(check): "+m_OrderStack[i].Order.ToString(),10);
							if (m_OrderStack[i].ConvertCanceledToMarket)
							{
								int m_Shares=m_OrderStack[i].Order.Quantity;
								int m_Position=m_OrderStack[i].PositionNumber;
								OrderAction m_Action=m_OrderStack[i].Order.OrderAction;
								
								debug(" ExpiredOrder {ConvertToMarket}: "+m_OrderStack[i].Order.ToString(),10);
								m_OrderStack[i].CancelOrder();
								m_OrderStack.RemoveAt(i);
								
								switch(m_Action)
								{
									case OrderAction.Buy:
										SubmitPendingOrder(m_BarsInProgress, OrderAction.Buy, OrderType.Market, m_Shares, 0,
											0,"ENTER"+m_Position+m_UniqueID, "GoLongMarket"+m_Position,m_Position,m_DefaultOrderRules);
										break;
									case OrderAction.SellShort:
										SubmitPendingOrder(m_BarsInProgress, OrderAction.SellShort, OrderType.Market, m_Shares, 0, 
											0,"ENTER"+m_Position+m_UniqueID, "GoShortMarket"+m_Position,m_Position,m_DefaultOrderRules);
										break;
									case OrderAction.BuyToCover:
										SubmitPendingOrder(m_BarsInProgress, OrderAction.BuyToCover, OrderType.Market, m_Shares, 0,
											0,"EXIT"+m_Position+m_UniqueID, "ExitMarketShort"+m_Position,m_Position,m_DefaultOrderRules);
										break;
									case OrderAction.Sell:
										SubmitPendingOrder(m_BarsInProgress, OrderAction.Sell, OrderType.Market, m_Shares, 0, 
											0,"EXIT"+m_Position+m_UniqueID, "ExitMarketLong"+m_Position,m_Position,m_DefaultOrderRules);
										break;
									default:
										TraceError("Cancel Order OrderAction Invalid",0);
										break;
								}
								count++;
							}
							else
							{
								debug(" ExpiredOrder {Order Canceled}: "+m_OrderStack[i].Order.ToString(),10);
								//Trace.TraceError(" ExpiredOrder {Order Canceled} ");
									
								m_OrderStack[i].CancelOrder();
								m_OrderStack.RemoveAt(i);
								count++;
							}
						}
					}
				}		
			}
			return count;
		}	
#endregion

#region GetGoodUntilCount()
		public int GetGoodUntilCount()
		{
			int i=0;
			int count=0;
						//debug(" Pending ExpiredOrder {m_TimerLockObject}: "+m_OrderStack[i].Order.ToString(),0);
			//lock(m_TimerLockObject)
			{
						//debug(" Pending ExpiredOrder {for}: "+m_OrderStack[i].Order.ToString(),0);
				for (i=0;i<m_OrderStack.Count-1;i++)
				{
						//debug(" Pending ExpiredOrder {OrderRulesPending}: "+m_OrderStack[i].Order.ToString(),0);
					if (m_OrderStack[i].GoodUntilPending)
					{
						//debug(" Pending ExpiredOrder {count}: "+m_OrderStack[i].Order.ToString(),0);
						++count;
					}
				}
			}
			return count;
		}	
#endregion
		
		public int PrintOrderStack(int m_DebugLevel)
		{
			int i=0;
			int count=0;
			
			for (i=m_OrderStack.Count-1;i>=0;--i)
			{
				{
					debug(" PrintOrderStack: "+m_OrderStack[i].SignalName+" Order: "+m_OrderStack[i].Order.ToString()+" Count = "+m_OrderStack.Count,m_DebugLevel);
					count++;
				}
			}
			return count;
		}			
	}  
}
