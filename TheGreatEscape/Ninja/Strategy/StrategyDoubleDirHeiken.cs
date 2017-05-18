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
    public class StrategyDoubleDirHeiken : MultiOrderStrategy
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
            Add(anaHeikinAshi(anaHeikinAshiType.Dan_Valcu, 10, anaHAMAType.Median));

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
			double o1=anaHeikinAshi(anaHeikinAshiType.Dan_Valcu, 10, anaHAMAType.Median).HAOpen[0];
			double o2=anaHeikinAshi(anaHeikinAshiType.Dan_Valcu, 10, anaHAMAType.Median).HAOpen[1];
			double c1=anaHeikinAshi(anaHeikinAshiType.Dan_Valcu, 10, anaHAMAType.Median).HAClose[0];
			double c2=anaHeikinAshi(anaHeikinAshiType.Dan_Valcu, 10, anaHAMAType.Median).HAClose[1];
					
			if (c1>o1 && c2>o2 && MarketPosition.Long!=Position.MarketPosition)
				GoLong();
			if (c1<o1 && c2<o2 && MarketPosition.Short!=Position.MarketPosition)
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
