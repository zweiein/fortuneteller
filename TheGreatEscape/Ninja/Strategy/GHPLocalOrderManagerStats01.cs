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
	/// 	DO NOT SELL THIS CODE OR DISTRIBUTE OUTSIDE OF BMT's Elite Forum Section
	///
	/// 						
	/// 
	/// </summary>
	public partial class LocalOrderManager 
	{
		string REV_MANAGERSTATS01="01";
		
	#region UpdateStats	
		
	#endregion		
		
	#region UpdateStats		
		public void UpdateStats(MarketDataEventArgs e)
		{
			switch (e.MarketDataType)
			{
				case MarketDataType.Ask:
				{
					break;
				}
				case MarketDataType.Bid:
				{				
					break;
				}
				case MarketDataType.Last:
				{
					for (int i=0;i<m_MaxOrderPositions;++i)
					{	//Update trading band
						if (GetMarketPosition(i)!=MarketPosition.Flat)
						{							
							bool m_BTUpdated=false;
							
							if (m_BadBandHigh[i]<0)
							{
								m_BadBandHigh[i]=e.Price;
								m_BTUpdated=true;
							}
							else if (m_BadBandHigh[i]<e.Price)
							{
								m_BadBandHigh[i]=e.Price;
								m_BTUpdated=true;
							}
							if (m_BadBandLow[i]<0)
							{
								m_BadBandLow[i]=e.Price;
								m_BTUpdated=true;
							}
							else if (m_BadBandLow[i]>e.Price)
							{
								m_BadBandLow[i]=e.Price;	
								m_BTUpdated=true;
							}
						//	if (m_BTUpdated)
						//		debug(" BTrade Range Updated POS#:"+i+"  Low: "+m_BadBandLow[i]+
						//			"  High: "+m_BadBandHigh[i]);
						}
					}
					break;
				}
				default:
				{
					break;
				}
			}
		}
	#endregion
			
	#region UpdatePL		
		public void UpdatePL(int m_pos, int m_Quant, double m_Enter, double m_Exit,bool m_Long,bool m_Filled)
		{
			double m_Profit;
		//	debug("Pos: "+m_pos+"  Enter $"+m_Enter+"  Exit $"+m_Exit+"  Quant: "+m_Quant+
		//	   "  Long: "+m_Long+"  FILLED? "+m_Filled);
			if (m_Long)
			{
				m_Profit=(m_Exit-m_Enter)*m_Quant*m_Strat.Instrument.MasterInstrument.PointValue-m_ContractCommission*m_Quant;
			}
			else
			{
				m_Profit=(m_Enter-m_Exit)*m_Quant*m_Strat.Instrument.MasterInstrument.PointValue-m_ContractCommission*m_Quant;
			}
			
			if (m_Filled)
			{
				m_Profit=m_Profit;//-m_Strat.s;
			}
			
			if (m_Strat.Historical)
			{
				if (m_Profit>0)
				{
					if (m_Filled)
						m_NumberWinsHist[m_pos]+=1;
					m_PLWinsHist[m_pos]+=m_Profit;
				}
				else
				{
					if (m_Filled)
						m_NumberLossesHist[m_pos]+=1;
					m_PLLossesHist[m_pos]+=m_Profit;
				}
				
			}
			else
			{
				if (m_Profit>0)
				{
					if (m_Filled)
						m_NumberWins[m_pos]+=1;
					m_PLWins[m_pos]+=m_Profit;
				}
				else
				{
					if (m_Filled)
						m_NumberLosses[m_pos]+=1;
					m_PLLosses[m_pos]+=m_Profit;
				}
			}
			
			if (m_Filled)
			{
				if (m_Profit>0)
				{
					if (m_NumberConsecutiveWinLoss[m_pos]>=0)
						m_NumberConsecutiveWinLoss[m_pos]+= 1;
					else
						m_NumberConsecutiveWinLoss[m_pos]= +1;
					
					ResetBadBad(m_pos);
				}
				else
				{
					if (m_NumberConsecutiveWinLoss[m_pos]<=0)
						m_NumberConsecutiveWinLoss[m_pos]-= 1;
					else
						m_NumberConsecutiveWinLoss[m_pos]= -1;
				}
				
				debug("PL Pos#"+m_pos+" PL$ "+m_Profit+" TotalConsWinsLosses#"+m_NumberConsecutiveWinLoss[m_pos]
				+" TProf: $"+m_PLWins[m_pos]+" TLoss $"+m_PLLosses[m_pos]);
			}
		}
	#endregion	
		
	#region ResetBadBad				
		public void ResetBadBad(int m_pos)
		{
			m_BadBandHigh[m_pos]=-1;
			m_BadBandLow[m_pos]=-1;
		}
	#endregion	
		
	#region GetTotalPL				
		public double GetTotalPL()
		{
			return GetTotalPL(-1);
		}		
	#endregion	
		
	#region GetTotalPL				
		public double GetTotalPL(int m_Pos)
		{
			double m_PLTotal=0.0;
			
			if (m_Pos<0)
			{
				for (int i=0;i<m_MaxOrderPositions;++i)
				{
				if (m_Strat.Historical)
					m_PLTotal+=m_PLWinsHist[i]+m_PLLossesHist[i];
				else
					m_PLTotal+=m_PLWins[i]+m_PLLosses[i];	
				}
			}
			else
			{
				if (m_Strat.Historical)
					m_PLTotal+=m_PLWinsHist[m_Pos]+m_PLLossesHist[m_Pos];
				else
					m_PLTotal+=m_PLWins[m_Pos]+m_PLLosses[m_Pos];
			}
			return m_PLTotal;
		}	
	#endregion	
				
	#region GetTotalUnrealizedPL				
		public double GetTotalUnrealizedPL()
		{
			return GetTotalUnrealizedPL(-1);
		}		
	#endregion	
		
	#region GetTotalUnrealizedPL				
		public double GetTotalUnrealizedPL(int m_Pos)
		{
			double m_PLUrTotal=0.0;
			double m_CurrentValue=0.0;
			LocalOrder m_LO;
			
			if (GetMarketPosition() != MarketPosition.Flat) 
			{
				if (GetMarketPosition() == MarketPosition.Long) 
				{
					m_CurrentValue = m_Strat.GetCurrentBid(); 
					for (int i=0;i<m_MaxOrderPositions;++i)
					{
						m_LO=m_ActivePositions[i];
						if ((m_LO!=null) && ((i==m_Pos) || (m_Pos<0)))
						{	
							m_PLUrTotal+=(m_CurrentValue-m_LO.Order.AvgFillPrice)*m_LO.Order.Filled*m_Strat.Instrument.MasterInstrument.PointValue;
							
							m_PLUrTotal+=m_CurrentValue-m_ActivePositions[i].Order.AvgFillPrice;
						}
					}
				}
				else 
				{
					m_CurrentValue = m_Strat.GetCurrentAsk();
					for (int i=0;i<m_MaxOrderPositions;++i)
					{
						m_LO=m_ActivePositions[i];
						if ((m_LO!=null) && ((i==m_Pos) || (m_Pos<0)))
						{
							m_PLUrTotal+=(m_LO.Order.AvgFillPrice-m_CurrentValue)*m_LO.Order.Filled*m_Strat.Instrument.MasterInstrument.PointValue;
						}
					}
				}
			}
			
			return m_PLUrTotal;
		}	
	#endregion			
		
	#region GetTotalUnrealizedPLinTicks				
		public int GetTotalUnrealizedPLinTicks(int m_Pos)
		{
			bool m_Blocked=false;
			int m_TickBuf;
			double m_UP;
			
			m_UP=m_Strat.Instrument.MasterInstrument.Round2TickSize(
									GetTotalUnrealizedPL(m_Pos))/m_ActivePositions[m_Pos].Order.Filled/m_Strat.Instrument.MasterInstrument.PointValue;

			m_TickBuf=(int)(m_UP/m_Strat.TickSize);
			debug("ProfitPos: "+m_UP + "   Ticks= "+m_TickBuf);
			
			return m_TickBuf;
		}	
	#endregion	
		
	#region IsHighLowBlocked				
		public bool IsHighLowBlocked(double low,double high)
		{
			return IsHighLowBlocked(low,high,0); // use target 0 data
		}
	#endregion	

	#region IsHighLowBlocked				
		public bool IsHighLowBlocked(double low,double high,int m_Pos)
		{
			bool m_Blocked=false;
			int m_TickBuf=1;
			
			if ((m_BadBandHigh[m_Pos]>(Math.Max(low,high)+m_Strat.TickSize*m_TickBuf))
				&& (m_BadBandLow[m_Pos]<(Math.Min(low,high)-m_Strat.TickSize*m_TickBuf)) 
				&& (m_NumberConsecutiveWinLoss[m_Pos]<0) && (m_BadBandHigh[m_Pos]>0) 
				&& (m_BadBandLow[m_Pos]>0))
					m_Blocked=true;
			
			return m_Blocked;
		}
	#endregion	
		
	#region WhatIsHighBlockedLimit				
		public double WhatIsHighBlockedLimit(int m_Pos)
		{
			return m_BadBandHigh[m_Pos]; 
		}
	#endregion	
		
	#region WhatIsLowBlockedLimit				
		public double WhatIsLowBlockedLimit(int m_Pos)
		{
			return m_BadBandLow[m_Pos]; 
		}
	#endregion	
		
	#region WhatIsNumberConsecutiveWinLoss				
		public double WhatIsNumberConsecutiveWinLoss(int m_Pos)
		{
			return m_NumberConsecutiveWinLoss[m_Pos]; 
		}
	#endregion	
		
	#region IsHighLowBlockedBuffered				
		public bool IsHighLowBlockedBuffered(int ticksBuf,double low,double high)
		{
			bool m_Blocked=false;
			for (int i=0;i<m_MaxOrderPositions;++i)
			{
				if (IsHighLowBlockedBuffered(ticksBuf,low,high,i))
					m_Blocked=true;
			}
			return m_Blocked; 
		}
	#endregion	
		
	#region IsHighLowBlockedBuffered				
		public bool IsHighLowBlockedBuffered(int ticksBuf,double low,double high,int m_Pos)
		{
			bool m_Blocked=false;
			
			if ((m_BadBandHigh[m_Pos]>(Math.Max(low,high)+m_Strat.TickSize*ticksBuf))
				&& (m_BadBandLow[m_Pos]<(Math.Min(low,high)-m_Strat.TickSize*ticksBuf)) 
				&& (m_NumberConsecutiveWinLoss[m_Pos]<0) && (m_BadBandHigh[m_Pos]>0) 
				&& (m_BadBandLow[m_Pos]>0))
					m_Blocked=true;
			
			return m_Blocked;
		}
	#endregion			
	}    
}
