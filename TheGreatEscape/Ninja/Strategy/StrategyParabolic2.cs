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
    public class StrategyParabolic2 : MultiOrderStrategy
    {
        #region Variables
		double psarVar1=0.5;
		double psarVar2=0.02;
		double psarVar3=0.5;
    	bool agressiveTrailing=false;
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void InitializeIndicators()
        {
            Add(ParabolicSAR(psarVar1, psarVar2,psarVar3));
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
          //  if (CrossAbove(Close, ParabolicSAR(psarVar1, psarVar2,psarVar3), 1))
			if (MarketPosition.Long!=Position.MarketPosition && Close[0]>ParabolicSAR(psarVar1, psarVar2,psarVar3)[0])
            {
				if (Close[0]>Close[1])
                	GoLong();
            }

            // Condition set 2
          //  if (CrossBelow(Close, ParabolicSAR(psarVar1, psarVar2,psarVar3), 1))
			if (MarketPosition.Short!=Position.MarketPosition && Close[0]<ParabolicSAR(psarVar1, psarVar2,psarVar3)[0])			
            {
				if (Close[0]<Close[1])
                	GoShort();
            }
        
        }

        #region Properties
        
         [Description("")]
        [GridCategory("Parameters")]
        public double PsarVar1 
        {
            get { return psarVar1; }
            set { psarVar1 = value; }
        }   
         [Description("")]
        [GridCategory("Parameters")]
        public double PsarVar2
        {
            get { return psarVar2; }
            set { psarVar2 = value; }
        }   
         [Description("")]
        [GridCategory("Parameters")]
        public double PsarVar3
        {
            get { return psarVar3; }
            set { psarVar3 = value; }
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
