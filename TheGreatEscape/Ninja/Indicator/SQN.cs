/////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Created by: Shane A. Brewer
// www.shanebrewer.com
//
////////////////////////////////////////////////////////////////////////////////////////////////////////

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
   /// Calculates the System Quality Number (SQN) for the period given as a parameter. Based on the research by Dr. Van Tharp.
   /// </summary>
   [Description("Calculates the System Quality Number (SQN) for the period given as a parameter. Based on the research by Dr. Van Tharp.")]
   public class SQN : Indicator
   {
      #region Variables
      // Wizard generated variables
      private int period = 365; // Default setting for Period

      // User defined variables (add any user defined variables below)
      private DataSeries percentBarChange;

      // SQN Market Categorization Values
      public static readonly double strongBullValue = 1.5; // Anything larger than 1.5 is considered a strong bull market
      public static readonly double bullValue = 0.8; // 0.8 to 1.5 is considered a bull market
      public static readonly double neutralValue = 0; // 0 to 0.8 is considered a neutral market
      public static readonly double bearValue = -0.7; // -0.7 to 0 is considered a bear market
      // Anything lower than -0.7 is considered a strong bear market
      #endregion

      /// <summary>
      /// This method is used to configure the indicator and is called once before any bar data is loaded.
      /// </summary>
      protected override void Initialize()
      {
         percentBarChange = new DataSeries(this);
         Add(new Plot(Color.FromKnownColor(KnownColor.Orange), PlotStyle.Line, "SQN"));
         Add(new Line(Color.Red, bearValue, "Bear Value"));
         Add(new Line(Color.Gray, neutralValue, "Neutral Value"));
         Add(new Line(Color.Green, bullValue, "Bull Value"));
         Add(new Line(Color.DarkGreen, strongBullValue, "Strong Bull Value"));

         Overlay = false;
         BarsRequired = period;
         CalculateOnBarClose = true;
      }

      /// <summary>
      /// Called on each bar update event (incoming tick)
      /// </summary>
      protected override void OnBarUpdate()
      {
         if (CurrentBar <= BarsRequired)
            //return;
            SQNValue.Set(0);

         percentBarChange.Set(calculateBarPercentChange());
         SQNValue.Set(CalculateSQNValue(period));

         //Print("CurrentBar is: " + CurrentBar);
         //Print("calculateBarPercentChange() is: " + calculateBarPercentChange());
         //Print("CalculateSQNValue(period) is: " + CalculateSQNValue(period));
      }

      /// <summary>
      /// Called to calculate the percent change of a bar
      /// </summary>
      protected double calculateBarPercentChange()
      {
         if (CurrentBar == 0)
            return 0;
         else
            return ((Close[0] - Close[1]) / Close[1]);
      }

      /// <summary>
      /// Called to calculate the SQN value for a bar
      /// </summary>
      protected double CalculateSQNValue(int period)
      {
         double SQNValue;
         double StDevValue;
         if (CurrentBar >= period)
         {
            SQNValue = SMA(percentBarChange, period)[0];
            StDevValue = StdDev(percentBarChange, period)[0];
            return (SQNValue / StDevValue * Math.Sqrt(period));
         }
         else
            return (0);
      }

      /// <summary>
      /// This public method returns the SQN market type as a string value based on the value of the parameter. 
      /// </summary>
      /// <param name="sqn">The SQN value</param>
      /// <returns>String representing the SQN market type</returns>
      public String GetSQNStringValue(double sqn)
      {
         if (sqn < bearValue)
            return ("Strong Bear");
         else if (sqn < neutralValue)
            return ("Bear");
         else if (sqn < bullValue)
            return ("Neutral");
         else if (sqn < strongBullValue)
            return ("Bull");
         else
            return ("Strong Bull");
      }

      #region Properties
      [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
      [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
      public DataSeries SQNValue
      {
         get { return Values[0]; }
      }

      [Description("The period (number of bars) used to calculate the SQN value")]
      [GridCategory("Parameters")]
      public int Period
      {
         get { return period; }
         set { period = Math.Max(1, value); }
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
        private SQN[] cacheSQN = null;

        private static SQN checkSQN = new SQN();

        /// <summary>
        /// Calculates the System Quality Number (SQN) for the period given as a parameter. Based on the research by Dr. Van Tharp.
        /// </summary>
        /// <returns></returns>
        public SQN SQN(int period)
        {
            return SQN(Input, period);
        }

        /// <summary>
        /// Calculates the System Quality Number (SQN) for the period given as a parameter. Based on the research by Dr. Van Tharp.
        /// </summary>
        /// <returns></returns>
        public SQN SQN(Data.IDataSeries input, int period)
        {
            if (cacheSQN != null)
                for (int idx = 0; idx < cacheSQN.Length; idx++)
                    if (cacheSQN[idx].Period == period && cacheSQN[idx].EqualsInput(input))
                        return cacheSQN[idx];

            lock (checkSQN)
            {
                checkSQN.Period = period;
                period = checkSQN.Period;

                if (cacheSQN != null)
                    for (int idx = 0; idx < cacheSQN.Length; idx++)
                        if (cacheSQN[idx].Period == period && cacheSQN[idx].EqualsInput(input))
                            return cacheSQN[idx];

                SQN indicator = new SQN();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.Period = period;
                Indicators.Add(indicator);
                indicator.SetUp();

                SQN[] tmp = new SQN[cacheSQN == null ? 1 : cacheSQN.Length + 1];
                if (cacheSQN != null)
                    cacheSQN.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheSQN = tmp;
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
        /// Calculates the System Quality Number (SQN) for the period given as a parameter. Based on the research by Dr. Van Tharp.
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.SQN SQN(int period)
        {
            return _indicator.SQN(Input, period);
        }

        /// <summary>
        /// Calculates the System Quality Number (SQN) for the period given as a parameter. Based on the research by Dr. Van Tharp.
        /// </summary>
        /// <returns></returns>
        public Indicator.SQN SQN(Data.IDataSeries input, int period)
        {
            return _indicator.SQN(input, period);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// Calculates the System Quality Number (SQN) for the period given as a parameter. Based on the research by Dr. Van Tharp.
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.SQN SQN(int period)
        {
            return _indicator.SQN(Input, period);
        }

        /// <summary>
        /// Calculates the System Quality Number (SQN) for the period given as a parameter. Based on the research by Dr. Van Tharp.
        /// </summary>
        /// <returns></returns>
        public Indicator.SQN SQN(Data.IDataSeries input, int period)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.SQN(input, period);
        }
    }
}
#endregion
