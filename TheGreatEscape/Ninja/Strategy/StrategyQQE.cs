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
    public class StrategyQQE : MultiOrderStrategy
    {
        #region Variables
            private int rSI_Period = 14; // Default setting for RSI_Period

			private int sF=5;
    		bool agressiveTrailing=false;
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void InitializeIndicators()
        {
            Add( QQE(RSI_Period, SF));

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
			double v1 = QQE(RSI_Period, SF).Value1[0];
			double v2 = QQE(RSI_Period, SF).Value2[0];
	
            // Condition set 1
            if (v1>v2 && QQE(RSI_Period, SF).Value1[0]>QQE(RSI_Period, SF).Value1[1])
            {
                GoLong();
            }

            // Condition set 2
            if (v1<v2&& QQE(RSI_Period, SF).Value1[0]<QQE(RSI_Period, SF).Value1[1])
            {
                GoShort();
            }
            
            // Condition set 3
            if (Performance.AllTrades.TradesPerformance.Currency.CumProfit >= 500)
            {
                Alert("Account Alert", Priority.High, "Target Achieved", @"C:\Program Files (x86)\NinjaTrader 7\sounds\Alert1.wav", 0, Color.White, Color.Black);
            }            
        }

        #region Properties
        
        [Description("Period for the RSI")]
        [Category("Parameters")]
        public int RSI_Period
        {
            get { return rSI_Period; }
            set { rSI_Period = Math.Max(1, value); }
        }
		[Description("Smoothing Factor")]
        [Category("Parameters")]
        public int SF
        {
            get { return sF; }
            set { sF = Math.Max(1, value); }
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
