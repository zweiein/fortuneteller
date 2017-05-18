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
    public class StrategyHeikenAshiSimple : MultiOrderStrategy
    {
        #region Variables
            private double haAmp = 0.7;
            private int haPeriod1 = 1; 
            private int haPeriod2= 2; 
			private bool quickExit=true;			
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void InitializeIndicators()
        {            
			Add(HeikenAshiSmoothed(haAmp, HAMA.SMA, HAMA.SMMA, HAPeriod1, HAPeriod2));//.HAClose[0];
        }

        protected override void OnBarUpdateImpl(){	

            double haL=HeikenAshiSmoothed(haAmp, HAMA.SMA, HAMA.SMMA, HAPeriod1, HAPeriod2).HALow[0];
            double haC=HeikenAshiSmoothed(haAmp, HAMA.SMA, HAMA.SMMA, HAPeriod1, HAPeriod2).HAClose[0];
            double haH=HeikenAshiSmoothed(haAmp, HAMA.SMA, HAMA.SMMA, HAPeriod1, HAPeriod2).HAHigh[0];
            double haO=HeikenAshiSmoothed(haAmp, HAMA.SMA, HAMA.SMMA, HAPeriod1, HAPeriod2).HAOpen[0];     

            double haL2=HeikenAshiSmoothed(haAmp, HAMA.SMA, HAMA.SMMA, HAPeriod1, HAPeriod2).HALow[1];
            double haC2=HeikenAshiSmoothed(haAmp, HAMA.SMA, HAMA.SMMA, HAPeriod1, HAPeriod2).HAClose[1];
            double haH2=HeikenAshiSmoothed(haAmp, HAMA.SMA, HAMA.SMMA, HAPeriod1, HAPeriod2).HAHigh[1];
            double haO2=HeikenAshiSmoothed(haAmp, HAMA.SMA, HAMA.SMMA, HAPeriod1, HAPeriod2).HAOpen[1]; 
			
			if (QuickExit && MarketPosition.Long==Position.MarketPosition && Close[0]<haC){
				ExitAll();
			}
			if (QuickExit && MarketPosition.Short==Position.MarketPosition && Close[0]>haC){
				ExitAll();
			}				
			if (haC>haO && haC2>haO2){
					GoLong();
			}
			if (haC<haO &&  haC2<haO2){
					GoShort();	
			}	            
        }

        #region Properties

            [Description("HA Amp")]
            [GridCategory("Parameters")]
            public double HAAmp
            {
                get { return haAmp; }
                set { haAmp = value; }
            }

            [Description("HA Period1")]
            [GridCategory("Parameters")]
            public int HAPeriod1
            {
                get { return haPeriod1; }
                set { haPeriod1 = Math.Max(1, value); }
            }

            [Description("HA Period2")]
            [GridCategory("Parameters")]
            public int HAPeriod2
            {
                get { return haPeriod2; }
                set { haPeriod2 = Math.Max(0, value); }
            }
			[Description("")]
			[GridCategory("Parameters")]
			public bool QuickExit
			{
				get { return quickExit; }
				set { quickExit = value; }
			} 
  
        #endregion
    }
}
