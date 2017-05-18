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
// For sending screen dumps on email
using System.Windows.Forms;
using System.Net.Mail;
using System.Net.Mime;
using System.Drawing.Imaging;
using valutatrader; 

#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    /// <summary>
    /// Strategy that finds the short term direction of the market US Equities RTH time, and follows the flow
    /// </summary>
    [Description("Strategy that finds the short term direction of the market US Equities RTH time, and follows the flow")]
    public class VTStratMeanReversion_1_0  :  VTStrategyBase
    {
    	VTMoneyManagement moneyManager=null;
    	double ticksDiff=1;
    	public VTStratMeanReversion_1_0(){

    		moneyManager=new VTMoneyManagement(this, 1000);
    	}
 		protected override void OnStartUp()
        {

        }
        protected override void Initialize()
        {

        	initOrderManager();
        }
		
    	protected override void OnBarUpdate()
        {
        	if (moneyManager.checkAllowance()==false) return;

			if (CurrentBar < 20)
					return;
			double shortPrice=High[1] + ticksDiff*TickSize;
			double longPrice=Low[1] - ticksDiff*TickSize;

			double v1=Close[0];
			double v2=Open[0];

			if (MarketPosition.Flat==Position.MarketPosition){
				if (v1>v2){
					EnterLongLim(longPrice);
				}	
				if (v1<v2){
					EnterShortLim(shortPrice);
				}	
			}	
			
			if (MarketPosition.Long==Position.MarketPosition){
				EnterShortLim(shortPrice);
			}	
			if (MarketPosition.Short==Position.MarketPosition){
				EnterLongLim(longPrice);
			}		
	        

        }

    }

}