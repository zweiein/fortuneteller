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
    public class StrategyPivotTrailer: MultiOrderStrategy
    {
        #region Variables
            private int leftPivotStrength=2;
            private int rightPivotStrength=2;
            private bool strictPivot=true;
        	bool highLowTrailing=false;
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void InitializeIndicators()
        {

        }

		protected override double CalcStopPrice(double initPrice){
		
			if (HighLowTrailing && MarketPosition.Long==Position.MarketPosition && Close[0]<Low[1]){
				ExitAll();
			}
			else if (HighLowTrailing && MarketPosition.Short==Position.MarketPosition && High[1]<Close[0]){
				ExitAll();
			}
			return 0;
		}

        protected override void OnBarUpdateImpl(){				
            if (PivotLowDetected(PivotStrengthLeft,PivotStrengthRight)){
                GoLong();
            }
            if ( PivotHighDetected(PivotStrengthLeft,PivotStrengthRight)){
                GoShort();
            }            
        }

        #region Functions
        public bool PivotLowDetected(int leftStrength, int rightStrength){      
            bool flag=true;
            for (int i=(rightStrength-1);i>=0;i--){
                if ((StrictPivot && Low[i]<=Low[rightStrength]) ||
                    (StrictPivot==false && Low[i]<Low[rightStrength]))
                    flag=false;
            }
            for (int i=(rightStrength+1);i<=(leftStrength+rightStrength);i++){
                if ((StrictPivot && Low[i]<=Low[rightStrength])||
                    (StrictPivot==false && Low[i]<Low[rightStrength]))
                    flag=false;
            }   
            return flag;
        }

        public bool PivotHighDetected(int leftStrength, int rightStrength){
            bool flag=true;

            for (int i=(rightStrength-1);i>=0;i--){
                if ((StrictPivot && High[i]>=High[rightStrength]) ||
                    (StrictPivot=false && High[i]>High[rightStrength]))
                    flag=false;
            }
            for (int i=(rightStrength+1);i<=(leftStrength+rightStrength);i++){
                if ((StrictPivot && High[i]>=High[rightStrength]) ||
                    (StrictPivot==false && High[i]>High[rightStrength]))
                    flag=false;
            }
            return flag;
        }
#endregion

        #region Properties
        
        [Description("")]
        [GridCategory("Parameters")]
        public int PivotStrengthRight
        {
            get { return rightPivotStrength; }
            set { rightPivotStrength = Math.Max(1, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public int PivotStrengthLeft
        {
            get { return leftPivotStrength; }
            set { leftPivotStrength = Math.Max(1, value); }
        }   

        
        [Description("")]
        [GridCategory("Parameters")]
        public bool StrictPivot
        {
            get { return strictPivot; }
            set { strictPivot =value; }
        }
   
         [Description("")]
        [GridCategory("Parameters")]
        public bool HighLowTrailing
        {
            get { return highLowTrailing; }
            set { highLowTrailing = value; }
        } 
				
		
        #endregion
    }
}
