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
    /// PSAR Reverals
    /// </summary>
    [Description("PSAR Reverals")]
    public class ParabolicSARRev : Strategy
    {
        #region Variables
        // Wizard generated variables
        // User defined variables (add any user defined variables below)
		double parA=0.2;
		double parB=0.2;
		double parC=0.02;
		private int slTicks=45;
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            Add(ParabolicSAR(parA, parB, parC));
			this.SetStopLoss(CalculationMode.Ticks, slTicks);
			this.Enabled=true;
			this.ExitOnClose=false;
            CalculateOnBarClose = true;
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
            // Condition set 1
            if (CrossAbove(Close, ParabolicSAR(parA, parB, parC), 1))
            {
                EnterShort(DefaultQuantity, "");
            }

            // Condition set 2
            if (CrossBelow(Close, ParabolicSAR(parA, parB, parC), 1))
            {
                EnterLong(DefaultQuantity, "");
            }
        }

        #region Properties
		
        [Description("")]
        [GridCategory("Parameters")]
        public double ParA
        {
            get { return parA; }
            set { parA = value; }
        }
		
        [Description("")]
        [GridCategory("Parameters")]
        public double ParB
        {
            get { return parB; }
            set { parB = value; }
        }
		
        [Description("")]
        [GridCategory("Parameters")]
        public double ParC
        {
            get { return parC; }
            set { parC = value; }
        }	
		
        [Description("")]
        [GridCategory("Parameters")]
        public int StopLossTicks
        {
            get { return slTicks; }
            set { slTicks = Math.Max(1, value); }
        }	
        #endregion
    }
}
