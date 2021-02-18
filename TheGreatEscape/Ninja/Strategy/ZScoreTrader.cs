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
    /// Enter the description of your strategy here
    /// </summary>
    [Description("Enter the description of your strategy here")]
    public class ZScoreTrader : Strategy
    {
        #region Variables
        // Wizard generated variables
        private int avgLength = 10; 
		private int momLength = 5; 
		//private double myzscore=0;
		private DataSeries myscore;
		private DataSeries myscoreavg;
		private bool useLimitOrders=false;
		private int lots=1;
        // User defined variables (add any user defined variables below)
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
			myscore=new DataSeries(this);
			myscoreavg=new DataSeries(this);
            CalculateOnBarClose = true;
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			
			double i,j;
			i=SMA(Close, avgLength)[0];
			j=StdDev(Close, avgLength)[0];
			double zscore = ( (Close[0]-i) / j );
			myscore.Set(zscore);
			double avgzscore=SMA(myscore, avgLength)[0];
			myscoreavg.Set(avgzscore);
			double momentum=myscoreavg[0]-myscoreavg[momLength];
			
			if (momentum>0) EnterLongPos();
			if (momentum<0) EnterShortPos();
			
        }
		private void EnterLongPos(){
			if (useLimitOrders){
				this.EnterLongLimit(lots,Close[0]);
			}else{
				EnterLong(lots);
			}
		}
		private void EnterShortPos(){
			if (useLimitOrders){
				this.EnterShortLimit(lots,Close[0]);
			}else{
				EnterShort(lots);
			}
		}
        #region Properties
        [Description("")]
        [GridCategory("Parameters")]
        public int MomLength
        {
            get { return momLength; }
            set { momLength = Math.Max(1, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public int AvgLength
        {
            get { return avgLength; }
            set { avgLength = Math.Max(1, value); }
        }		
        #endregion
    }
}
