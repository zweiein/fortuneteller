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


public enum BollingerTypeSma
{
	SMA, EMA
};

// Simple indicator measuring the width of a bollinger chart and its SMA/EMA 
namespace NinjaTrader.Indicator
{

    /// <summary>
    /// Simple indicator measuring the width of a bollinger chart and its SMA/EMA 
	/// Developed by @ValutaTrader (steinar@currencytrader.no)
    /// </summary>
    [Description("Enter the description of your new custom indicator here")]
    public class BollingerExpansion : Indicator
    {

	
        #region Variables
        // Wizard generated variables
            private int bollingerPeriods = 20; // Default setting for BollingerPeriods
			private int bollingerStdDev = 2; // Default setting for BollingerPeriods
            private int smaPeriods = 15; // Default setting for EmaPeriods
			private BollingerTypeSma smaType=BollingerTypeSma.EMA;
        // User defined variables (add any user defined variables below)
        #endregion

        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override void Initialize()
        {
            Add(new Plot(new Pen(Color.Navy, 2), PlotStyle.Bar, "BollingerWidth"));
            Add(new Plot(new Pen(Color.Orange, 2), PlotStyle.Line, "BollingerEma"));
            Overlay				= false;
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			double bollWidth = Bollinger(2, bollingerPeriods).Upper[0]-Bollinger(2, bollingerPeriods).Lower[0];			
            BollingerWidth.Set(bollWidth);
			if (smaType==BollingerTypeSma.EMA)
				BollingerEma.Set(EMA(BollingerWidth,SmaPeriods)[0]);
			if (smaType==BollingerTypeSma.SMA)
				BollingerEma.Set(SMA(BollingerWidth,SmaPeriods)[0]);
        }

        #region Properties
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries BollingerWidth
        {
            get { return Values[0]; }
        }

        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries BollingerEma
        {
            get { return Values[1]; }
        }

        [Description("")]
        [GridCategory("Parameters")]
        public int BollingerPeriods
        {
            get { return bollingerPeriods; }
            set { bollingerPeriods = Math.Max(1, value); }
        }
		
        [Description("")]
        [GridCategory("Parameters")]
        public int BollingerStdDev
        {
            get { return bollingerStdDev; }
            set { bollingerStdDev = Math.Max(1, value); }
        }
		
        [Description("")]
        [GridCategory("Parameters")]
        public int SmaPeriods
        {
            get { return smaPeriods; }
            set { smaPeriods = Math.Max(1, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public BollingerTypeSma SmaType
        {
            get { return smaType; }
            set { smaType = value; }
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
        private BollingerExpansion[] cacheBollingerExpansion = null;

        private static BollingerExpansion checkBollingerExpansion = new BollingerExpansion();

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public BollingerExpansion BollingerExpansion(int bollingerPeriods, int bollingerStdDev, int smaPeriods, BollingerTypeSma smaType)
        {
            return BollingerExpansion(Input, bollingerPeriods, bollingerStdDev, smaPeriods, smaType);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public BollingerExpansion BollingerExpansion(Data.IDataSeries input, int bollingerPeriods, int bollingerStdDev, int smaPeriods, BollingerTypeSma smaType)
        {
            if (cacheBollingerExpansion != null)
                for (int idx = 0; idx < cacheBollingerExpansion.Length; idx++)
                    if (cacheBollingerExpansion[idx].BollingerPeriods == bollingerPeriods && cacheBollingerExpansion[idx].BollingerStdDev == bollingerStdDev && cacheBollingerExpansion[idx].SmaPeriods == smaPeriods && cacheBollingerExpansion[idx].SmaType == smaType && cacheBollingerExpansion[idx].EqualsInput(input))
                        return cacheBollingerExpansion[idx];

            lock (checkBollingerExpansion)
            {
                checkBollingerExpansion.BollingerPeriods = bollingerPeriods;
                bollingerPeriods = checkBollingerExpansion.BollingerPeriods;
                checkBollingerExpansion.BollingerStdDev = bollingerStdDev;
                bollingerStdDev = checkBollingerExpansion.BollingerStdDev;
                checkBollingerExpansion.SmaPeriods = smaPeriods;
                smaPeriods = checkBollingerExpansion.SmaPeriods;
                checkBollingerExpansion.SmaType = smaType;
                smaType = checkBollingerExpansion.SmaType;

                if (cacheBollingerExpansion != null)
                    for (int idx = 0; idx < cacheBollingerExpansion.Length; idx++)
                        if (cacheBollingerExpansion[idx].BollingerPeriods == bollingerPeriods && cacheBollingerExpansion[idx].BollingerStdDev == bollingerStdDev && cacheBollingerExpansion[idx].SmaPeriods == smaPeriods && cacheBollingerExpansion[idx].SmaType == smaType && cacheBollingerExpansion[idx].EqualsInput(input))
                            return cacheBollingerExpansion[idx];

                BollingerExpansion indicator = new BollingerExpansion();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.BollingerPeriods = bollingerPeriods;
                indicator.BollingerStdDev = bollingerStdDev;
                indicator.SmaPeriods = smaPeriods;
                indicator.SmaType = smaType;
                Indicators.Add(indicator);
                indicator.SetUp();

                BollingerExpansion[] tmp = new BollingerExpansion[cacheBollingerExpansion == null ? 1 : cacheBollingerExpansion.Length + 1];
                if (cacheBollingerExpansion != null)
                    cacheBollingerExpansion.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheBollingerExpansion = tmp;
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
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.BollingerExpansion BollingerExpansion(int bollingerPeriods, int bollingerStdDev, int smaPeriods, BollingerTypeSma smaType)
        {
            return _indicator.BollingerExpansion(Input, bollingerPeriods, bollingerStdDev, smaPeriods, smaType);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public Indicator.BollingerExpansion BollingerExpansion(Data.IDataSeries input, int bollingerPeriods, int bollingerStdDev, int smaPeriods, BollingerTypeSma smaType)
        {
            return _indicator.BollingerExpansion(input, bollingerPeriods, bollingerStdDev, smaPeriods, smaType);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.BollingerExpansion BollingerExpansion(int bollingerPeriods, int bollingerStdDev, int smaPeriods, BollingerTypeSma smaType)
        {
            return _indicator.BollingerExpansion(Input, bollingerPeriods, bollingerStdDev, smaPeriods, smaType);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public Indicator.BollingerExpansion BollingerExpansion(Data.IDataSeries input, int bollingerPeriods, int bollingerStdDev, int smaPeriods, BollingerTypeSma smaType)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.BollingerExpansion(input, bollingerPeriods, bollingerStdDev, smaPeriods, smaType);
        }
    }
}
#endregion
