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
    public class RandomEntryProd : Strategy
    {
        #region Variables
        // Wizard generated variables
        private int myInput0 = 1; // Default setting for MyInput0
		private int position = 0;
		private int quantity = 5;
		private int ticsToChange = 6;
		private int maxContracts = 10;
		private double currentAskInScope;
		private int bias = 3; //-1 is short, 1 is long, 0 is neither
		private int startTradeTime = 073000;
		private int stopTradeTime = 160000;
		private double cumulativeProfit = 0;
		private bool tradeToday = false;
		private bool noHistorical = true;
		private int maxloss = 2000;
		private IOrder initialEntryOrder;
		private IOrder longOrder;
		private IOrder shortOrder;
		private bool intialTradeFilled = false;
		int i = 0; //for debugging
		
        // User defined variables (add any user defined variables below)
        #endregion

		#region Initialize
        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
			Add(PeriodType.Day, 1);
			Unmanaged = true;
            CalculateOnBarClose = false;
        }
		#endregion

		#region OnBarUpdate
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			if (Historical && noHistorical) return;
			
			#region BiasAndColor
			try {
				//Set up bias
				if (BarsInProgress == 1)
				{
					if (FirstTickOfBar)
					{
						cumulativeProfit = Performance.AllTrades.TradesPerformance.Currency.CumProfit;
						bias = determineBias(BMTCollectiveMAQuikStart(true).Slow[0], BMTCollectiveMAQuikStart(true).Medium[0], BMTCollectiveMAQuikStart(true).Fast[0]);
						if (bias == 1) 
						{
							tradeToday = true;
							BackColorAll = Color.Green;
						}
						else if (bias == -1)
						{
							tradeToday = true;
							BackColorAll = Color.Red;
						}
						else 
						{
							tradeToday = false;
							BackColorAll = Color.Gray;
						}
					}
					return;
				}
				
				
				
				#endregion
				
			#region ManageTrades
				
				if (tradeToday) //Only trade if there is a bias and we have not reached max loss
				{
					//Max Loss has been reached, flatten everything and turn it pink!
					if ((cumulativeProfit - Performance.AllTrades.TradesPerformance.Currency.CumProfit) > maxloss)
					{
						flattenAll();
						position = 0;
						bias = 3;
						tradeToday = false;
						BackColorAll = Color.HotPink;
						return;
					}	
					if (ToTime(Time[0]) == startTradeTime && FirstTickOfBar)
					{
						enterBiasLongOrShort();
					}
					
					//Time to trade
					if ((ToTime(Time[0])<= stopTradeTime && ToTime(Time[0]) >= startTradeTime) && intialTradeFilled) //Added check to make sure intial trade has been filled.
					{
						//Print status box and check on status of current contracts held and remedy to position
						if (i++ > 3)
						{
							i = 0;
							statusBox();
							getInSync();
						}
						
						
						//Price is going up...
						if (ticsToChange <= ((GetCurrentAsk() - currentAskInScope) / TickSize))
						{
							if (bias == 1)
							{
								//Add a contract, we're winning!
								if (position < maxContracts)
								{
									position++;
									longOrder = SubmitOrder(0,OrderAction.Buy, OrderType.Market, 1, 0, 0, "", "Long" + (position - 1));
								}
								currentAskInScope = GetCurrentAsk(); //Always update scope, reguardless if max contracts have been met
							}
							if (bias == -1)
							{
								//Subtract a contract, we're losing!
								if (position > 0)
								{
									position--;
									shortOrder = SubmitOrder(0,OrderAction.BuyToCover, OrderType.Market, 1, 0, 0, "", "BuyToCover" + (position + 1));
								}
								currentAskInScope = GetCurrentAsk();
							}
						}
						
						//Price is going down...
						if (ticsToChange <= ((currentAskInScope - GetCurrentAsk()) / TickSize))
						{
							if (bias == 1)
							{
								//Subtract a contract, we're losing!
								if (position > 0)
								{
									position--;
									shortOrder = SubmitOrder(0,OrderAction.Sell, OrderType.Market, 1, 0, 0, "", "ShortLosing" + (position + 1));
								}
								currentAskInScope = GetCurrentAsk();
							}
							if (bias == -1)
							{
								//Add a contract, we're winning!
								if (position < maxContracts)
								{
									position++;
									shortOrder = SubmitOrder(0,OrderAction.SellShort, OrderType.Market, 1, 0, 0, "", "Short" + position);
								}
								currentAskInScope = GetCurrentAsk(); //Always update scope, reguardless if max contracts have been met
							}
						}
					}
				}
				
				if (ToTime(Time[0]) == stopTradeTime)
				{
					flattenAll();
					position = 0;
					bias = 3;
					tradeToday = false;
				}
			} catch (Exception e) {
				flattenAll();
				Print("Something went wrong\n");
				Print(e.StackTrace);
				Print("\n" + e.GetBaseException().StackTrace);
			}
			#endregion
        }
		#endregion
		
		#region Helper Methods
		
		private void enterBiasLongOrShort()
		{
			switch(bias)
			{
				case 1: //long
					position = quantity;
					currentAskInScope = GetCurrentAsk();
					initialEntryOrder = SubmitOrder(0,OrderAction.Buy, OrderType.Market, quantity, 0, 0, "", "LongBias");
					break;
				case -1: //short
					position = quantity;
					currentAskInScope = GetCurrentBid();
					initialEntryOrder = SubmitOrder(0,OrderAction.Sell, OrderType.Market, quantity, 0, 0, "", "ShortBias");
					break;
			}
		}
		
		private void getInSync()
		{	
			int qua = Position.Quantity; //Quantity we are at
			
			if (qua != position) //Check if we're in sync
			{
				//If we're long with a short bias, something went wrong...throw exception
				if (Position.MarketPosition == MarketPosition.Long && bias == -1)
				{
					throw new System.Exception("We are long with a short bias, something went wrong!");	
				}
				
				//If wear are short with a long bias, something went wrong...throw exception
				if (Position.MarketPosition == MarketPosition.Short && bias == 1)
				{
					throw new System.Exception("We are long with a short bias, something went wrong!");
				}
				
				if (Math.Abs(qua - position) == 1) //if only 1 off, could have open or stuck order
				{
					//Check to see if there is an open order and if it has been open to long
					if (longOrder != null && longOrder.OrderState != OrderState.Filled)
					{
							if (DateTime.Now > longOrder.Time.AddMinutes(3))
							{
								CancelOrder(longOrder);
								BackColorAll = Color.Brown;
								PrintWithTimeStamp("Out of sync with hung long order. Position: " + quantity + "Actual Position: " + qua);
								position = qua;
							}
						return;
					}
					//Check to see if there is an open order and if it has been open to long
					if (shortOrder != null && shortOrder.OrderState != OrderState.Filled)
					{
							if (DateTime.Now > shortOrder.Time.AddMinutes(3))
							{
								CancelOrder(shortOrder);
								BackColorAll = Color.Brown;
								PrintWithTimeStamp("Out of sync with hung short order. Position: " + quantity + "Actual Position: " + qua);
								position = qua;
							}
						return;
					}
				}
				
				PrintWithTimeStamp("Out of sync. Position: " + quantity + "Actual Position: " + qua);
				position = qua;	
			}
			
		}
		
		private void flattenAll()
		{
			int qua = Position.Quantity; //Quantity to exit
			
			if (Position.MarketPosition == MarketPosition.Long)
			{
				SubmitOrder(0,OrderAction.Sell, OrderType.Market, qua, 0, 0, "", "LongFlatten");
			} else if (Position.MarketPosition == MarketPosition.Short) {
				SubmitOrder(0,OrderAction.BuyToCover, OrderType.Market, qua, 0, 0, "", "ShortFlatten");
			}
		}
		
		private int determineBias(double slow, double med, double fast)
		{
			int ret = 0;
			
			if (fast > slow)
			{
				ret = 1;
			}
			else if (fast < slow)
			{
				ret = -1;	
			}
			return ret;
		}
		
		private void statusBox() 
		{
			String dispayInBox = "Total Profit: " + Performance.RealtimeTrades.TradesPerformance.Currency.CumProfit.ToString("C");
			double aPrice;
			
			if (Position.MarketPosition != MarketPosition.Flat) 
				{
					if (Position.MarketPosition == MarketPosition.Long) aPrice = GetCurrentBid(); 
					else aPrice = GetCurrentAsk();
					dispayInBox += "\nProfit Unrealized: " + Position.GetProfitLoss(aPrice, PerformanceUnit.Currency).ToString("C"); 
					dispayInBox += "\nProfit this Day: " + (Performance.AllTrades.TradesPerformance.Currency.CumProfit - cumulativeProfit).ToString("C");
				}
			dispayInBox += "\nProfit Total: " + (Performance.AllTrades.TradesPerformance.Currency.CumProfit).ToString("C");
			if (bias == 1) dispayInBox += "\nBias Long";
			else if (bias == -1) dispayInBox += "\nBias Short";
			dispayInBox += "\nInternal position: " + position;
			dispayInBox += "\nCurrent Ask in Scope: " + currentAskInScope;
			DrawTextFixed("Loot", dispayInBox, TextPosition.TopLeft,Color.Red, new Font("Arial", 8), Color.Black, Color.LightGray, 100);
		}
		#endregion
		
		#region OnOrderUpdate
		protected override void OnOrderUpdate(IOrder order)
		{
			if (order == initialEntryOrder) //Check to see if this is first order of day
			{	
				if (order.OrderState == OrderState.Filled)
				{
					intialTradeFilled = true;	// We're filled				
				}	
			}	
			
		}
		#endregion
		
        #region Properties
		
		[Description("Maximum loss (enter positive value!).")]
        [Category("Parameters")]
        public int MaxLoss
        {
        	get { return maxloss; }
           	set { maxloss = Math.Max(1, value); }
        }
		
        [Description("Time to start trading")]
        [GridCategory("Parameters")]
        public int StartTradeTime
        {
            get { return startTradeTime; }
            set { startTradeTime = value; }
        }
		
		[Description("Time to stop trading")]
        [GridCategory("Parameters")]
        public int StopTradeTime
        {
            get { return stopTradeTime; }
            set { stopTradeTime = value; }
        }
		
		[Description("Initial quantity to enter at start time")]
        [GridCategory("Parameters")]
        public int Quantity
        {
            get { return quantity; }
            set { quantity = value; }
        }
		
		[Description("Amount of tics that go by before you increase/decrease order")]
        [GridCategory("Parameters")]
        public int TicsToChange
        {
            get { return ticsToChange; }
            set { ticsToChange = value; }
        }
		
		[Description("Max contracts to enter into")]
        [GridCategory("Parameters")]
        public int MaxContracts
        {
            get { return maxContracts; }
            set { maxContracts = value; }
        }
		
		[Description("Override bias for first day. 1=Long -1=Short")]
        [GridCategory("Parameters")]
        public int Bias
        {
            get { return bias; }
            set { tradeToday = true; bias = value; }
        }
		
		public bool NoHistorical
        {
            get { return noHistorical; }
            set { noHistorical = value; }
        }
        #endregion
    }
}
