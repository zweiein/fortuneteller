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
//	05/20/2012 - NJAMC - As Issued
//
 
	/// </summary>
	public partial class LocalOrderManager 
	{
		string REV_VIRTUAL01="01";
		
		
#region ProcessVirtualOrder(int i)
		public void ProcessVirtualOrder(int i)
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
			
	}    
}
