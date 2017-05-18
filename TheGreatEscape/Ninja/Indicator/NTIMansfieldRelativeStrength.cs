// 
// NT Indicators
// For more indicators visit www.ntindicators.com
//
#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    /// <summary>
    /// This indicator is used to compare an equity performance with its reference index
    /// </summary>
    [Description("This indicator is used to compare an equity performance with its reference index")]
    public class NTIMansfieldRelativeStrength : Indicator
    {
        #region Variables
        // Wizard generated variables
            private string compare = "^SP500"; // Default setting for Compare
			private DataSeries ser;
		int period=52;
        // User defined variables (add any user defined variables below)
        #endregion

        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override void Initialize()
        {
			Add(compare, BarsPeriod.Id, BarsPeriod.Value);
			Add(new Plot(Color.FromKnownColor(KnownColor.Black), PlotStyle.Line, "0"));
			Plots[0].Pen.Width=3;
			
			ser = new DataSeries(this);
			CalculateOnBarClose = false;
            Overlay	= false;
			DrawOnPricePanel = false;
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			if (BarsInProgress==0) {
				ser.Set(Close[0] / BarsArray[1][0]);
				
				float basePrice = (float) SMA (ser, period)[0];
								
				float rsM = (float) (ser[0]/basePrice -1)*10;
				Print(CurrentBar);
				if (rsM>0) {
					PlotColors[0][0] = Color.Green;
					DrawRegion("Fill" + CurrentBar, Math.Min(1,CurrentBar), 0 , Plot0, 0, Color.Green, Color.Green, 2);
				}
				else {
					PlotColors[0][0] = Color.Red;
					DrawRegion("Fill" + CurrentBar, Math.Min(1,CurrentBar), 0 , Plot0, 0, Color.Red, Color.Red, 2);
				}
				Plot0.Set(rsM);
				//DrawRegion("tag1", CurrentBar, -0, Plot0, 0, Color.Green, Color.Green, 2);
			}
            
        }

        #region Properties
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries Plot0
        {
            get { return Values[0]; }
        }
        
        [Description("Equity to compare")]
        [GridCategory("Parameters")]
        public string Compare
        {
            get { return compare; }
            set { compare = value; }
        }
		
        [Description("Equity to compare")]
        [GridCategory("Parameters")]
        public int Period
        {
            get { return period; }
            set { period = value; }
        }
		
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    public partial class Indicator : IndicatorBase
    {
        private NTIMansfieldRelativeStrength[] cacheNTIMansfieldRelativeStrength = null;

        private static NTIMansfieldRelativeStrength checkNTIMansfieldRelativeStrength = new NTIMansfieldRelativeStrength();

        /// <summary>
        /// This indicator is used to compare an equity performance with its reference index
        /// </summary>
        /// <returns></returns>
        public NTIMansfieldRelativeStrength NTIMansfieldRelativeStrength(string compare, int period)
        {
            return NTIMansfieldRelativeStrength(Input, compare, period);
        }

        /// <summary>
        /// This indicator is used to compare an equity performance with its reference index
        /// </summary>
        /// <returns></returns>
        public NTIMansfieldRelativeStrength NTIMansfieldRelativeStrength(Data.IDataSeries input, string compare, int period)
        {
            if (cacheNTIMansfieldRelativeStrength != null)
                for (int idx = 0; idx < cacheNTIMansfieldRelativeStrength.Length; idx++)
                    if (cacheNTIMansfieldRelativeStrength[idx].Compare == compare && cacheNTIMansfieldRelativeStrength[idx].Period == period && cacheNTIMansfieldRelativeStrength[idx].EqualsInput(input))
                        return cacheNTIMansfieldRelativeStrength[idx];

            lock (checkNTIMansfieldRelativeStrength)
            {
                checkNTIMansfieldRelativeStrength.Compare = compare;
                compare = checkNTIMansfieldRelativeStrength.Compare;
                checkNTIMansfieldRelativeStrength.Period = period;
                period = checkNTIMansfieldRelativeStrength.Period;

                if (cacheNTIMansfieldRelativeStrength != null)
                    for (int idx = 0; idx < cacheNTIMansfieldRelativeStrength.Length; idx++)
                        if (cacheNTIMansfieldRelativeStrength[idx].Compare == compare && cacheNTIMansfieldRelativeStrength[idx].Period == period && cacheNTIMansfieldRelativeStrength[idx].EqualsInput(input))
                            return cacheNTIMansfieldRelativeStrength[idx];

                NTIMansfieldRelativeStrength indicator = new NTIMansfieldRelativeStrength();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.Compare = compare;
                indicator.Period = period;
                Indicators.Add(indicator);
                indicator.SetUp();

                NTIMansfieldRelativeStrength[] tmp = new NTIMansfieldRelativeStrength[cacheNTIMansfieldRelativeStrength == null ? 1 : cacheNTIMansfieldRelativeStrength.Length + 1];
                if (cacheNTIMansfieldRelativeStrength != null)
                    cacheNTIMansfieldRelativeStrength.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheNTIMansfieldRelativeStrength = tmp;
                return indicator;
            }
        }
    }
}

// This namespace holds all market analyzer column definitions and is required. Do not change it.
namespace NinjaTrader.MarketAnalyzer
{
    public partial class Column : ColumnBase
    {
        /// <summary>
        /// This indicator is used to compare an equity performance with its reference index
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.NTIMansfieldRelativeStrength NTIMansfieldRelativeStrength(string compare, int period)
        {
            return _indicator.NTIMansfieldRelativeStrength(Input, compare, period);
        }

        /// <summary>
        /// This indicator is used to compare an equity performance with its reference index
        /// </summary>
        /// <returns></returns>
        public Indicator.NTIMansfieldRelativeStrength NTIMansfieldRelativeStrength(Data.IDataSeries input, string compare, int period)
        {
            return _indicator.NTIMansfieldRelativeStrength(input, compare, period);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// This indicator is used to compare an equity performance with its reference index
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.NTIMansfieldRelativeStrength NTIMansfieldRelativeStrength(string compare, int period)
        {
            return _indicator.NTIMansfieldRelativeStrength(Input, compare, period);
        }

        /// <summary>
        /// This indicator is used to compare an equity performance with its reference index
        /// </summary>
        /// <returns></returns>
        public Indicator.NTIMansfieldRelativeStrength NTIMansfieldRelativeStrength(Data.IDataSeries input, string compare, int period)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.NTIMansfieldRelativeStrength(input, compare, period);
        }
    }
}
#endregion
