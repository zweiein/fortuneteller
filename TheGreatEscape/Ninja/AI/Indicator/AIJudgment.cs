// #############################################################
// #														   	#
// #                   AIJudgment                   			#
// #														   	#
// #     10/05/2012 by NJAMC Base On PriceActionSwing     		#	
// #														   	#
// #############################################################
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
using PriceActionSwing.Utility;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    /// <summary>
    /// Enter the description of your new custom indicator here
    /// </summary>
    [Description("AIJudgment Generates Classificatoin data for AI learning series.  Major repainting of bars.  This is only useful for processing historical data.")]
    public class AIJudgment : Indicator
    {
        #region Variables
        //#####################################################################
        private int swingSize = 7;
        private SwingTypes swingType = SwingTypes.Standard;
        private int dtbStrength = 15;
        private PriceActionSwing m_PAS=null;
        private int TrendStepDirection = 0;
		
		private int m_MaxSwingsBack=3;		
		private int m_ChopLimitTicks=9;		
        //#####################################################################
        #endregion

        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override void Initialize()
        {
            Add(new Plot(new Pen(Color.Black, 1), PlotStyle.Square, "Classification"));
            Add(new Plot(new Pen(Color.Red, 2), PlotStyle.Square, "SwingPoint"));
            Add(new Plot(new Pen(Color.Gold, 3), PlotStyle.Square, "BarsToSwing"));
            Add(new Plot(new Pen(Color.Green, 3), PlotStyle.Square, "TicksProfitAtSwing"));

			TrendStepDirection=0;
			
			
            CalculateOnBarClose = true;
            Overlay				= false;
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			bool m_NewStatus=false;
			int m_SwingNum=0;
			int m_RefBar=0;
			
			if (CurrentBar < 22)
            {
                if (m_PAS == null )
                    m_PAS = PriceActionSwing(Input, dtbStrength, swingSize, swingType);
            }
			
				Classification.Set(0.0);
				BarsToSwing.Set(0.0);
				SwingPoint.Set(0.0);
				TicksProfitAtSwing.Set(0.0);
				m_RefBar=0;
			
				if(m_PAS.DoubleTop.ContainsValue(0)||
					m_PAS.HigherHigh.ContainsValue(0)||
					m_PAS.LowerHigh.ContainsValue(0))
				{
					SwingPoint.Set(1.0);
					TrendStepDirection=-1;
					BarsToSwing.Set(0.0);
					m_NewStatus=true;
				}
				if(m_PAS.DoubleBottom.ContainsValue(0)||
					m_PAS.HigherLow.ContainsValue(0)||
					m_PAS.LowerLow.ContainsValue(0))
				{
					SwingPoint.Set(-1.0);
					TrendStepDirection=1;
					BarsToSwing.Set(0.0);
					m_NewStatus=true;
				}
				
				for (int i=1;i<=(CurrentBar-1);++i) // Find Next Pivot
				{
					SwingPoint.Set(i,0.0);
					m_NewStatus=false;

					if((m_PAS.DoubleTop.ContainsValue(i)||
						m_PAS.HigherHigh.ContainsValue(i)||
						m_PAS.LowerHigh.ContainsValue(i)) && (TrendStepDirection==1))
					{
						SwingPoint.Set(i,1.0);
						TrendStepDirection=-1;
						m_RefBar=i;
						BarsToSwing.Set(i,0.0);
						TicksProfitAtSwing.Set(i,0.0);
						m_NewStatus=true;
					}
					if((m_PAS.DoubleBottom.ContainsValue(i)||
						m_PAS.HigherLow.ContainsValue(i)||
						m_PAS.LowerLow.ContainsValue(i)) && (TrendStepDirection==-1))
					{
						SwingPoint.Set(i,-1.0);
						TrendStepDirection=1;
						m_RefBar=i;
						BarsToSwing.Set(i,0.0);
						TicksProfitAtSwing.Set(i,0.0);
						m_NewStatus=true;
					}
					if (!m_NewStatus)
					{
						BarsToSwing.Set(i,-(i-m_RefBar)*TrendStepDirection);
						SwingPoint.Set(i,0.0);
						TicksProfitAtSwing.Set(i,(Close[m_RefBar]-Close[i])/TickSize);
						if(m_ChopLimitTicks<TicksProfitAtSwing[i])
							Classification.Set(i,1.0);
						else if((-m_ChopLimitTicks)>TicksProfitAtSwing[i])
							Classification.Set(i,-1.0);
						else
							Classification.Set(0.0);
					}
					else
					{
						Classification.Set(0.0);
						++m_SwingNum;
						if (m_SwingNum>=m_MaxSwingsBack)
							break;
					}
				}
            }

        #region Properties
        //#####################################################################
        #region Plots
        //=====================================================================
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries Classification
        {
            get { return Values[0]; }
        }
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries SwingPoint
        {
            get { return Values[1]; }
        }
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries BarsToSwing
        {
            get { return Values[2]; }
        }
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries TicksProfitAtSwing
        {
            get { return Values[3]; }
        }

        //===================================================================
        #endregion

        #region Parameters
        //=====================================================================
        /// <summary>
        /// Represents the 
        /// </summary>
        [GridCategory("Parameters")]
        [Description("Represents the swing size. e.g. 1 = small swings and 5 = bigger swings.")]
        [Gui.Design.DisplayName("Swing size")]
        public int SwingSize
        {
            get { return swingSize; }
            set { swingSize = Math.Max(1, value); }
        }
        /// <summary>
        /// Represents the swing type. Standard | Gann
        /// </summary>
        [GridCategory("Parameters")]
        [Description("Represents the swing type. Standard | Gann")]
        [Gui.Design.DisplayName("Swing type")]
        public SwingTypes SwingType
        {
            get { return swingType; }
            set { swingType = value; }
        }
        /// <summary>
        /// Represents the double top/-bottom strength.
        /// </summary>
        [GridCategory("Parameters")]
        [Description("Represents the double top/-bottom strength. Increase the value to get more DB/DT.")]
        [Gui.Design.DisplayName("Double top/-bottom strength")]
        public int DtbStrength
        {
            get { return dtbStrength; }
            set { dtbStrength = Math.Max(1, value); }
        }
        [GridCategory("Parameters")]
        [Description("Retrace this many swings to fix data, default is 3.")]
        [Gui.Design.DisplayName("Fix Swings Back")]
        public int MaxSwingsBack
        {
            get { return m_MaxSwingsBack; }
            set { m_MaxSwingsBack = Math.Max(1, value); }
        }
		        [GridCategory("Parameters")]
        [Description("Under or equal to this level of ticks, the price movement will be considered Chop.  Above Long, below Short, to be used for classification")]
        [Gui.Design.DisplayName("Chop Limit Ticks")]
        public int ChopLimitTicks
        {
            get { return m_ChopLimitTicks; }
            set { m_ChopLimitTicks = Math.Max(1, value); }
        }

        //=====================================================================
        #endregion
        //#####################################################################
        #endregion
    }
}
#region NinjaScript generated code. Neither change nor remove.
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    public partial class Indicator : IndicatorBase
    {
        private AIJudgment[] cacheAIJudgment = null;

        private static AIJudgment checkAIJudgment = new AIJudgment();

        /// <summary>
        /// AIJudgment Generates Classificatoin data for AI learning series.  Major repainting of bars.  This is only useful for processing historical data.
        /// </summary>
        /// <returns></returns>
        public AIJudgment AIJudgment(int chopLimitTicks, int dtbStrength, int maxSwingsBack, int swingSize, SwingTypes swingType)
        {
            return AIJudgment(Input, chopLimitTicks, dtbStrength, maxSwingsBack, swingSize, swingType);
        }

        /// <summary>
        /// AIJudgment Generates Classificatoin data for AI learning series.  Major repainting of bars.  This is only useful for processing historical data.
        /// </summary>
        /// <returns></returns>
        public AIJudgment AIJudgment(Data.IDataSeries input, int chopLimitTicks, int dtbStrength, int maxSwingsBack, int swingSize, SwingTypes swingType)
        {
            if (cacheAIJudgment != null)
                for (int idx = 0; idx < cacheAIJudgment.Length; idx++)
                    if (cacheAIJudgment[idx].ChopLimitTicks == chopLimitTicks && cacheAIJudgment[idx].DtbStrength == dtbStrength && cacheAIJudgment[idx].MaxSwingsBack == maxSwingsBack && cacheAIJudgment[idx].SwingSize == swingSize && cacheAIJudgment[idx].SwingType == swingType && cacheAIJudgment[idx].EqualsInput(input))
                        return cacheAIJudgment[idx];

            lock (checkAIJudgment)
            {
                checkAIJudgment.ChopLimitTicks = chopLimitTicks;
                chopLimitTicks = checkAIJudgment.ChopLimitTicks;
                checkAIJudgment.DtbStrength = dtbStrength;
                dtbStrength = checkAIJudgment.DtbStrength;
                checkAIJudgment.MaxSwingsBack = maxSwingsBack;
                maxSwingsBack = checkAIJudgment.MaxSwingsBack;
                checkAIJudgment.SwingSize = swingSize;
                swingSize = checkAIJudgment.SwingSize;
                checkAIJudgment.SwingType = swingType;
                swingType = checkAIJudgment.SwingType;

                if (cacheAIJudgment != null)
                    for (int idx = 0; idx < cacheAIJudgment.Length; idx++)
                        if (cacheAIJudgment[idx].ChopLimitTicks == chopLimitTicks && cacheAIJudgment[idx].DtbStrength == dtbStrength && cacheAIJudgment[idx].MaxSwingsBack == maxSwingsBack && cacheAIJudgment[idx].SwingSize == swingSize && cacheAIJudgment[idx].SwingType == swingType && cacheAIJudgment[idx].EqualsInput(input))
                            return cacheAIJudgment[idx];

                AIJudgment indicator = new AIJudgment();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.ChopLimitTicks = chopLimitTicks;
                indicator.DtbStrength = dtbStrength;
                indicator.MaxSwingsBack = maxSwingsBack;
                indicator.SwingSize = swingSize;
                indicator.SwingType = swingType;
                Indicators.Add(indicator);
                indicator.SetUp();

                AIJudgment[] tmp = new AIJudgment[cacheAIJudgment == null ? 1 : cacheAIJudgment.Length + 1];
                if (cacheAIJudgment != null)
                    cacheAIJudgment.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheAIJudgment = tmp;
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
        /// AIJudgment Generates Classificatoin data for AI learning series.  Major repainting of bars.  This is only useful for processing historical data.
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.AIJudgment AIJudgment(int chopLimitTicks, int dtbStrength, int maxSwingsBack, int swingSize, SwingTypes swingType)
        {
            return _indicator.AIJudgment(Input, chopLimitTicks, dtbStrength, maxSwingsBack, swingSize, swingType);
        }

        /// <summary>
        /// AIJudgment Generates Classificatoin data for AI learning series.  Major repainting of bars.  This is only useful for processing historical data.
        /// </summary>
        /// <returns></returns>
        public Indicator.AIJudgment AIJudgment(Data.IDataSeries input, int chopLimitTicks, int dtbStrength, int maxSwingsBack, int swingSize, SwingTypes swingType)
        {
            return _indicator.AIJudgment(input, chopLimitTicks, dtbStrength, maxSwingsBack, swingSize, swingType);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// AIJudgment Generates Classificatoin data for AI learning series.  Major repainting of bars.  This is only useful for processing historical data.
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.AIJudgment AIJudgment(int chopLimitTicks, int dtbStrength, int maxSwingsBack, int swingSize, SwingTypes swingType)
        {
            return _indicator.AIJudgment(Input, chopLimitTicks, dtbStrength, maxSwingsBack, swingSize, swingType);
        }

        /// <summary>
        /// AIJudgment Generates Classificatoin data for AI learning series.  Major repainting of bars.  This is only useful for processing historical data.
        /// </summary>
        /// <returns></returns>
        public Indicator.AIJudgment AIJudgment(Data.IDataSeries input, int chopLimitTicks, int dtbStrength, int maxSwingsBack, int swingSize, SwingTypes swingType)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.AIJudgment(input, chopLimitTicks, dtbStrength, maxSwingsBack, swingSize, swingType);
        }
    }
}
#endregion
