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
    public class StrategyD9Oscillator : MultiOrderStrategy
    {
        #region Variables
            private int d9Period = 4; // Default setting for D9Period
            private int d9Phase = 0; // Default setting for D9Phase
    		bool agressiveTrailing=false;
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void InitializeIndicators()
        {
            Add(d9ParticleOscillator_V2(D9Period, D9Phase));
            Add(d9ParticleOscillator_V2(D9Period, D9Phase));
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
            // Condition set 1
            if (CrossAbove(d9ParticleOscillator_V2(D9Period, D9Phase).Prediction, 0, 1))
            {
                GoLong();
            }

            // Condition set 2
            if (CrossBelow(d9ParticleOscillator_V2(D9Period, D9Phase).Prediction, 0, 1))
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
        
        [Description("D9 Period")]
        [GridCategory("Parameters")]
        public int D9Period
        {
            get { return d9Period; }
            set { d9Period = Math.Max(1, value); }
        }

        [Description("D9 Phase")]
        [GridCategory("Parameters")]
        public int D9Phase
        {
            get { return d9Phase; }
            set { d9Phase = Math.Max(0, value); }
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
