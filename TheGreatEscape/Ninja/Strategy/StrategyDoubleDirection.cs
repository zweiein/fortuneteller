#region Using declarations
using System;
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
using System.Collections.Generic;
#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{ 
    /// <summary>
    /// Enter the description of your strategy here
    /// </summary>
    [Description("Enter the description of your strategy here")]
    public class StrategyDoubleDirection : MultiOrderStrategy
    {
        #region Variables
            private bool filterStrongTrend=false;
    		bool agressiveTrailing=false;
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void InitializeIndicators()
        {
            Add(AaZiNonLagMA(2, 3, 2, 30, 0, 1));

        }

		protected override double CalcStopPrice(double initPrice){
		
			if (AgressiveTrailing && MarketPosition.Long==Position.MarketPosition && Close[0]>Close[1]){
				initPrice=Close[1];
			}
			if (AgressiveTrailing && MarketPosition.Short==Position.MarketPosition && Close[0]<Close[1]){
				initPrice=Close[1];
			}
			return initPrice;
		}

        protected override void OnBarUpdateImpl(){		
			bool strongUpTrend= AaZiNonLagMA(2, 3, 2, 30, 0, 1).UpTrend.ContainsValue(0);
			bool strongDownTrend= AaZiNonLagMA(2, 3, 2, 30, 0, 1).DownTrend.ContainsValue(0);
					
			if (Close[0]>Open[0] && Close[1]>Open[1] && (strongUpTrend || filterStrongTrend==false) )// && AaZiNonLagMA(2, 3, 2, 30, 0, 1).UpTrend.ContainsValue(0))//prev<last)//Close[1]>Open[1])//Close[0]==High[0] && Open[0]==Low[0])//bwAO().AONeg.ContainsValue(0))
				GoLong();
			if (Close[0]<Open[0] && Close[1]<Open[1]  && (strongDownTrend || filterStrongTrend==false))//&& AaZiNonLagMA(2, 3, 2, 30, 0, 1).DownTrend.ContainsValue(0))//prev>last)//&& Close[1]<Open[1])//&& Close[0]==Low[0] && Open[0]==High[0])//bwAO().AONeg.ContainsValue(0))
				GoShort();

            
            // Condition set 3
            if (Performance.AllTrades.TradesPerformance.Currency.CumProfit >= 500)
            {
                Alert("Account Alert", Priority.High, "Target Achieved", @"C:\Program Files (x86)\NinjaTrader 7\sounds\Alert1.wav", 0, Color.White, Color.Black);
            }            
        }

        #region Properties
        
         [Description("")]
        [GridCategory("Parameters")]
        public bool FilterStrongTrend
        {
            get { return filterStrongTrend; }
            set { filterStrongTrend = value; }
        }	

         [Description("")]
        [GridCategory("Parameters")]
        public bool AgressiveTrailing
        {
            get { return agressiveTrailing; }
            set { agressiveTrailing = value; }
        } 
   
        #endregion
    }
}
