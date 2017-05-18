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
#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    /// <summary>
    /// Thanks to The Wizard for his endorsement so that I can start developing a strategy that will capture one of his set-ups.
    /// </summary>
    [Description("The Wizard's set-up + d9 and Sharkfin")]
    public class StrategyWizard : MultiOrderStrategy
    {
       #region Variables
            private int d9Period = 7; // Default setting for D9Period
            private int d9Phase = 0; // Default setting for D9Phase    
       #endregion
	   
       protected override void InitializeIndicators()
        {
            Add(d9ParticleOscillatorWVertLineR(D9Period, D9Phase));
            Add(EMA_Colors_Paint_v01(30, 60, 14));
        }

        protected override void OnBarUpdateImpl(){              


            
            // Condition set 1 Go Long
            if (d9ParticleOscillatorWVertLineR(D9Period, D9Phase).RawTrend[0]>0 && (DonMA(14).MARising[0] > DonMA(14).MARising[1])
                && (EMA_Colors_Paint_v01(30, 60, 14).EMAup[0] > EMA_Colors_Paint_v01(30, 60, 14).EMAup[1]))
            {
                GoLong();
            }

            // Condition set 2 Go Short
            if (d9ParticleOscillatorWVertLineR(D9Period, D9Phase).RawTrend[0]<0 && (DonMA(14).MAFalling[0] < DonMA(14).MAFalling[1]) 
                && (EMA_Colors_Paint_v01(30, 60, 14).EMAdown[0] < EMA_Colors_Paint_v01(30, 60, 14).EMAdown[1]))
            {
                GoShort();
            
            }
            
            // Exit with warning signals of trend reversal
            if (CrossBelow(d9ParticleOscillatorWVertLineR(D9Period, D9Phase).RawTrend, _Sharkfin(6).Rising, 1))                
                ExitAll();
            
            if (CrossAbove(d9ParticleOscillatorWVertLineR(D9Period, D9Phase).RawTrend, _Sharkfin(6).Falling, 1))                
                ExitAll();    

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
        #endregion  
    }
}
