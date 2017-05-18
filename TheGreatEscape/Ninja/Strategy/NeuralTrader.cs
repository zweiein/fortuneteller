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
	
	
	public enum NeuralExitStrategy{
		NO_TRAILING_STOP, TRAILING_STOP_LOW, TRAILING_STOP_ATR, TRAILING_CANDELIER, TRAILING_YOYO
	};	
	public enum NeuralEntryCurve{
		STANDARD, REGRESS, REG_INTERCEPT, REG_SLOPE, JUST_HMA
	};	
	public enum NeuralEntryStrategy{
		ZEROCROSS, TURNINGPOINT
	};	
    /// <summary>
    /// Enter the description of your strategy here
    /// </summary>
    [Description("Enter the description of your strategy here")]
    public class NeuralTrader : Strategy
    {

        #region Variables
        private int priorTradesCount = 0;
        private double priorTradesCumProfit = 0;
		private int barNumberOfOrder=0;
		private bool firstBar=true;
        #endregion
        #region Variables
        // Wizard generated variables
        private LocalOrderManager m_OrderManager=null;
		private NeuralEntryCurve entryCurve=NeuralEntryCurve.STANDARD;
		private NeuralExitStrategy trailingExit=NeuralExitStrategy.TRAILING_STOP_ATR;
		private NeuralEntryStrategy entryStrat=NeuralEntryStrategy.ZEROCROSS;
		private int m_OrderIndex=-1;
		private int sl=50;
		private int regCount=14;
		private int profitLockTicks=10;
		private int lossReverseLimit=0;
		private double profitLockAtrFactor=1.0;
		private int tp=0;
		private double predProfitLimit=0;
		private bool useLOM=false;
		private bool lockProfit=false;
		private bool enablePnlLimit=true;
		private double pnlLimitMaxLoss=-1000;
		private double pnlLimitMaxGain=4000;
		private bool stictSL=false;
		private bool production=false;
		private bool reverseOnSignal=false;
		private bool useQuickExit=false;
		private String encogHost="localhost";
		private int encogPort=5128;
		private double trailingExitFactor=3;
		private int trailingExitPeriods=22;
		private int maxHighLowPeriods=22;
		private int smoothingFactor=3;
		private int smoothingFactor2=7;
		private int lotSize=1;
		private bool tradeAggressively=false;
        // User defined variables (add any user defined variables below)
        #endregion

        /// <summary>MurreyMathSimple().N28[0] == Variable8
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            CalculateOnBarClose = true;
            m_OrderManager = null;
			if (UseLOM){ 
				m_OrderManager = new LocalOrderManager(this, 0);
				m_OrderManager.SetDebugLevels(0, 1, false, 0);
				m_OrderManager.SetStatsBoxVisable(false);
				m_OrderManager.SetAutoSLPTTicks(SL, TP, 0);
				IgnoreOverFill = true;	
			}else{
				SetStopLoss(CalculationMode.Ticks, SL);	
			}

			Add(AaLaguerreMA(70));
			HMA(EncogFrameworkIndicator(encogHost, "default", EncogPort),smoothingFactor).Panel=1;
			Add(HMA(EncogFrameworkIndicator(encogHost, "default", EncogPort),smoothingFactor));

			LinReg(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,regCount).Panel=1;			
			Add(LinReg(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,regCount));
			LinReg(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,regCount).Plots[0].Pen.Color=Color.Red;
			
			LinRegIntercept(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,regCount).Panel=1;
			Add(LinRegIntercept(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,regCount));
			LinRegIntercept(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,regCount).Plots[0].Pen.Color=Color.Navy;
		
			HMA(LinRegSlope(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,regCount),smoothingFactor).Panel=2;
			Add(HMA(LinRegSlope(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,regCount),smoothingFactor));
			HMA(LinRegSlope(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,regCount),smoothingFactor).Plots[0].Pen.Color=Color.Black;

			//Add(HeikenAshiSmoothed(0.7, HAMA.ADXVMA, HAMA.ADXVMA, 1, 2));
			
            EntriesPerDirection = 1;
            EntryHandling = EntryHandling.AllEntries;
            CalculateOnBarClose = true;
            IncludeCommission = true;
            Slippage = 1;
            ExitOnClose = true;			
			
		//	UnmanagedMode=true;
        }

		
		protected override void OnMarketData(MarketDataEventArgs e) 
        {	
			if (m_OrderManager != null) m_OrderManager.OnMarketData( e);
		}
		protected override void OnOrderUpdate(IOrder order) 
		{
            if (m_OrderManager != null) m_OrderManager.OnOrderUpdate(order);
		}
       protected override void OnExecution(IExecution execution)
        {
			if (m_OrderManager != null) m_OrderManager.OnExecution(execution);
        }	
		
		int profitLockStatus=0;
		
        private int TicksProfit(double price)
        {
            double ppoints = Position.GetProfitLoss(price, PerformanceUnit.Points);
            double tickProfit = ppoints / TickSize;
            return Convert.ToInt32(tickProfit);
        }

		private double CheckSL(double slinit){
			double adjustedSL=slinit;
			if (UseStrictSL){
				int pl=TicksProfit(slinit);
				if (Math.Abs(pl)>SL){
					if (Close[0]<slinit){
						adjustedSL=Close[0] + SL *  TickSize;
					}else{
						adjustedSL=Close[0] - SL *  TickSize;
					}	
				}
			}
			return adjustedSL;
		}

        private void DrawEntryStopLines(double stop)
        {
			int lineLength=0;
            // If the position is long or short, draw lines at the entry, target, and stop prices.
            if (Position.MarketPosition == MarketPosition.Long)
            {
                /* Calculate the line length by taking the greater of two values (3 and the difference between the current bar and the entry bar).
                The line will always be at least 3 bars long. */
                lineLength = Math.Max(CurrentBar - barNumberOfOrder, 3);
                // Draw the lines at the stop, target, and entry.
                //DrawLine("Target", false, lineLength, Position.AvgPrice + 4 * TickSize, 0, Position.AvgPrice + 4 * TickSize, Color.Green, DashStyle.Solid, 2);
                DrawLine("Stop", false, lineLength, stop, 0, stop, Color.Red, DashStyle.Solid, 2);
                DrawLine("Entry", false, lineLength, Position.AvgPrice, 0, Position.AvgPrice, Color.Brown, DashStyle.Solid, 2);
            }
            else if (Position.MarketPosition == MarketPosition.Short)
            {
                lineLength = Math.Max(CurrentBar - barNumberOfOrder, 3);
                //DrawLine("Target", false, lineLength, Position.AvgPrice - 4 * TickSize, 0, Position.AvgPrice - 4 * TickSize, Color.Green, DashStyle.Solid, 2);
                DrawLine("Stop", false, lineLength, stop, 0, stop, Color.Red, DashStyle.Solid, 2);
                DrawLine("Entry", false, lineLength, Position.AvgPrice, 0, Position.AvgPrice, Color.Brown, DashStyle.Solid, 2);
            }
        }
		private bool PnlStop(){
			if (EnablePnlLimit==false) return false;
            /* Prevents further trading if the current session's realized profit exceeds $1000 or if realized losses exceed $400.
            Also prevent trading if 10 trades have already been made in this session. */
            if (Performance.AllTrades.TradesPerformance.Currency.CumProfit - priorTradesCumProfit >= PnlLimitMaxGain
                || Performance.AllTrades.TradesPerformance.Currency.CumProfit - priorTradesCumProfit <= PnlLimitMaxLoss
                || Performance.AllTrades.Count - priorTradesCount > 10)
            {
                /* TIP FOR EXPERIENCED CODERS: This only prevents trade logic in the context of the OnBarUpdate() method. If you are utilizing
                other methods like OnOrderUpdate() or OnMarketData() you will need to insert this code segment there as well. */

                // Returns out of the OnBarUpdate() method. This prevents any further evaluation of trade logic in the OnBarUpdate() method.
               return true;
            }
			return false;
		}
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			  if (IsProduction && Historical) return;
			
            // At the start of a new session
            if (Bars.FirstBarOfSession)
            {
                // Store the strategy's prior cumulated realized profit and number of trades
                priorTradesCount = Performance.AllTrades.Count;
                priorTradesCumProfit = Performance.AllTrades.TradesPerformance.Currency.CumProfit;
                /* NOTE: Using .AllTrades will include both historical virtual trades as well as real-time trades.
                If you want to only count profits from real-time trades please use .RealtimeTrades. */
            }




			if (Position.MarketPosition==MarketPosition.Flat){
				profitLockStatus=0;
				barNumberOfOrder=0;
				if (UseLOM==false){
					CancelAllOrders(true, true);
				//	SetStopLoss("Long",CalculationMode.Ticks, SL, false);	
				//	SetStopLoss("Short",CalculationMode.Ticks, SL, false);	
				}
			}
            if (Position.MarketPosition==MarketPosition.Flat && PnlStop())
            {
               return;
            }			
			
			int ticksPL=TicksProfit(Close[0]);	
			double trailingUp=ATR(TrailingExitPeriods)[0];
			double trailingDown=ATR(TrailingExitPeriods)[0];
			if (TrailingExit==NeuralExitStrategy.TRAILING_STOP_ATR){
         		 trailingUp=Low[0] - ATR(TrailingExitPeriods)[0]*TrailingExitFactor;
				 trailingDown=High[0]+ ATR(TrailingExitPeriods)[0]*TrailingExitFactor;
			}
			else if (TrailingExit==NeuralExitStrategy.TRAILING_CANDELIER){
         		 trailingUp=MAX(High,MaxHighLowPeriods)[0] - ATR(TrailingExitPeriods)[0]*TrailingExitFactor;
				 trailingDown=MIN(Low,MaxHighLowPeriods)[0] + ATR(TrailingExitPeriods)[0]*TrailingExitFactor;
			}
			else if (TrailingExit==NeuralExitStrategy.TRAILING_YOYO){
         		 trailingUp=MAX(Close,MaxHighLowPeriods)[0] - ATR(TrailingExitPeriods)[0]*TrailingExitFactor;
				 trailingDown=MIN(Close,MaxHighLowPeriods)[0] + ATR(TrailingExitPeriods)[0]*TrailingExitFactor;
			}
			double atr=ATR(5)[0];
         	double curr=0;//HMA(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,SmoothingFactor)[0];
            double prev=0;//HMA(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,SmoothingFactor)[1];
         	if (EntryCurve==NeuralEntryCurve.STANDARD){
         		 curr=HMA(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,SmoothingFactor)[0];
            	 prev=HMA(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,SmoothingFactor)[1];
			}
			else if (EntryCurve==NeuralEntryCurve.REGRESS){
         		 curr=HMA(LinReg(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,regCount),SmoothingFactor)[0];
            	 prev=HMA(LinReg(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,regCount),SmoothingFactor)[1];
			
			}
			else if (EntryCurve==NeuralEntryCurve.REG_INTERCEPT){
         		 curr=HMA(LinRegIntercept(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,regCount),SmoothingFactor)[0];
            	 prev=HMA(LinRegIntercept(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,regCount),SmoothingFactor)[1];
			}
			else if (EntryCurve==NeuralEntryCurve.REG_SLOPE){
         		 curr=HMA(LinRegSlope(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,regCount),SmoothingFactor)[0];
            	 prev=HMA(LinRegSlope(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,regCount),SmoothingFactor)[1];
			}
			else if (EntryCurve==NeuralEntryCurve.JUST_HMA){
         		 curr=HMA(Close,SmoothingFactor)[0];
            	 prev=HMA(Close,SmoothingFactor)[1];
			}
			double currSlow=HMA(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,SmoothingFactor2)[0];
            double prevSlow=HMA(EncogFrameworkIndicator(encogHost, "default", EncogPort).Plot1,SmoothingFactor2)[1];
			
			bool bullExitSignal=false;
			bool bearExitSignal=false;
			bool bullSignal=false;
			bool bearSignal=false;
			
			if (EntryStrat==NeuralEntryStrategy.TURNINGPOINT){
				if (curr<prev){
					bearExitSignal=true;
					if (prev>PredProfitLimit)
						bearSignal=true;
				}
				if ( curr>prev){
					bullExitSignal=true;
					if (prev<(-PredProfitLimit))
						bullSignal=true;
				}
			}else if (EntryStrat==NeuralEntryStrategy.ZEROCROSS){
				if (curr<prev){
					bearExitSignal=true;
					if (curr<(-PredProfitLimit) && prev>=(-PredProfitLimit))
						bearSignal=true;
				}
				if ( curr>prev){
					bullExitSignal=true;
					if (curr>PredProfitLimit && prev<=PredProfitLimit)
						bullSignal=true;
				}			
			}

			if (TradeAggressively || Position.MarketPosition==MarketPosition.Flat){

                if ((Position.MarketPosition==MarketPosition.Flat) && bearSignal){
					if (UseLOM)
                    	m_OrderManager.GoShortMarket(lotSize,0);
					else{
                        EnterShort(lotSize, "Short");
						SetStopLoss("Short", CalculationMode.Ticks, SL,false);
					}
					barNumberOfOrder=Bars.Count;
				}
                else if ( Position.MarketPosition==MarketPosition.Short && bearSignal){  //Only triggered if trade aggressively is set
					if (UseLOM)
                   		m_OrderManager.GoShortMarket(lotSize,0);  
					else{
                        EnterShort(lotSize, "Short");
						SetStopLoss("Short", CalculationMode.Ticks, SL,false);
					}
					
					barNumberOfOrder=Bars.Count;
				}
			   else if ( Position.MarketPosition==MarketPosition.Flat && bullSignal){
					if (UseLOM)
                   		m_OrderManager.GoLongMarket(lotSize,0);
					else{
                        EnterLong(lotSize, "Long");
						SetStopLoss("Long", CalculationMode.Ticks, SL,false);
					}
					barNumberOfOrder=Bars.Count;
				}
               else if ( Position.MarketPosition==MarketPosition.Long && bullSignal){  //Only triggered if trade aggressively is set
					if (UseLOM)
                   		m_OrderManager.GoLongMarket(lotSize,0); 
					else{
                        EnterLong(lotSize, "Long");
						SetStopLoss("Long", CalculationMode.Ticks, SL,false);
					}
					
					barNumberOfOrder=Bars.Count;
				}
            } 
            else if (Position.MarketPosition==MarketPosition.Long){
				if (ReverseOnSignal && bearSignal && TicksProfit(Close[0])<LossReverseLimit){
						if (UseLOM){
							m_OrderManager.GoShortMarket(lotSize*2,0);
						}
						else{
							if (PnlStop()){
								ExitLong("Long");
								CancelAllOrders(true, true);
							}else{
								CancelAllOrders(true, true);
								EnterShort(lotSize, "Short");	
								SetStopLoss("Short", CalculationMode.Ticks, SL,false);
								barNumberOfOrder=Bars.Count;
							}
						}
				}else{
					if (LockProfit){
						if (profitLockStatus==0 && TicksProfit(Close[0])>ProfitLockTicks){
							profitLockStatus=1;
							if (UseLOM)
								m_OrderManager.ExitSLPT(Close[0]-(ProfitLockAtrFactor*atr),0.0,0);
							else
								SetStopLoss("Long",CalculationMode.Price, Close[0]-(ProfitLockAtrFactor*atr), false);
						}else if  (profitLockStatus==1 && TicksProfit(Close[0])>(ATR(TrailingExitPeriods)[0]*TrailingExitFactor)){
							profitLockStatus=2;
						}
						if (profitLockStatus==2){
							if (UseLOM)
								m_OrderManager.ExitSLPT((trailingUp),0.0,0);
                            else
                            {
                                SetStopLoss("Long", CalculationMode.Price, CheckSL(trailingUp), false);
                                DrawEntryStopLines(CheckSL(trailingUp));
                            }
						}
					}else{
						if (ticksPL<0 && UseQuickExit && bearExitSignal){
								if (UseLOM)
									m_OrderManager.ExitMarket(0);
								else
									ExitLong("Long");
						}else{
							if (TrailingExit==NeuralExitStrategy.TRAILING_STOP_LOW)
								if (UseLOM)
									m_OrderManager.ExitSLPT(Low[3]-atr,0.0,0);
								else
									SetStopLoss("Long",CalculationMode.Price, Low[3]-atr, false);
							if (TrailingExit==NeuralExitStrategy.TRAILING_STOP_ATR || TrailingExit==NeuralExitStrategy.TRAILING_CANDELIER || TrailingExit==NeuralExitStrategy.TRAILING_YOYO)
								if (UseLOM)
									m_OrderManager.ExitSLPT(CheckSL(trailingUp),0.0,0);	
								else
                                {
                                    SetStopLoss("Long", CalculationMode.Price, CheckSL(trailingUp), false);
                                    DrawEntryStopLines(CheckSL(trailingUp));
                                }
									
							if (bearSignal && TrailingExit==NeuralExitStrategy.NO_TRAILING_STOP){
								if (UseLOM)
									m_OrderManager.ExitMarket(0);
								else
									ExitLong("Long");
							}
						}
					}
				}
            }
            else if (Position.MarketPosition==MarketPosition.Short){
				if (ReverseOnSignal && bullSignal && TicksProfit(Close[0])<LossReverseLimit){
						if (UseLOM){
							m_OrderManager.GoLongMarket(lotSize*2,0);
						}
						else{
							if (PnlStop()){								
                        	    ExitShort("Short");
								CancelAllOrders(true, true);
							}
							else{
								CancelAllOrders(true, true);
								EnterLong(lotSize, "Long");	
								SetStopLoss("Long", CalculationMode.Ticks, SL,false);
								barNumberOfOrder=Bars.Count;
							}
						}
				}
				else{
						if (LockProfit){
							if (profitLockStatus==0 && TicksProfit(Close[0])>ProfitLockTicks){
								profitLockStatus=1;
								if (UseLOM)
									m_OrderManager.ExitSLPT(Close[0]+(ProfitLockAtrFactor*atr),0.0,0);
								else
									SetStopLoss(CalculationMode.Price,Close[0]+(ProfitLockAtrFactor*atr));	
							}else if  (profitLockStatus==1 && TicksProfit(Close[0])>(ATR(TrailingExitPeriods)[0]*TrailingExitFactor)){
								profitLockStatus=2;
							}
							if (profitLockStatus==2){
								if (UseLOM)
									m_OrderManager.ExitSLPT(trailingDown,0.0,0);	
								else
                                {
                                    SetStopLoss("Short", CalculationMode.Price, CheckSL(trailingDown), false);
                                    DrawEntryStopLines(CheckSL(trailingDown));
                                }
							}
						}else{				
							if (ticksPL<0 && UseQuickExit && bullExitSignal){
									if (UseLOM)
										m_OrderManager.ExitMarket(0);
									else
										ExitShort("Short");
							}else{
							if (TrailingExit==NeuralExitStrategy.TRAILING_STOP_LOW)
								if (UseLOM)
									m_OrderManager.ExitSLPT(High[3]+atr,0.0,0);
								else
									SetStopLoss("Short",CalculationMode.Price,High[3]+atr, false);
							if (TrailingExit==NeuralExitStrategy.TRAILING_STOP_ATR || TrailingExit==NeuralExitStrategy.TRAILING_CANDELIER || TrailingExit==NeuralExitStrategy.TRAILING_YOYO)
                                if (UseLOM)
                                    m_OrderManager.ExitSLPT(CheckSL(trailingDown), 0.0, 0);
                                else
                                {
                                    SetStopLoss("Short", CalculationMode.Price, CheckSL(trailingDown), false);
                                    DrawEntryStopLines(CheckSL(trailingDown));
                                }
							if (bullSignal && TrailingExit==NeuralExitStrategy.NO_TRAILING_STOP){
								if (UseLOM)
									m_OrderManager.ExitMarket(0);
								else
									ExitShort("Short");

							}
							}
						}
				
				}




                // The strategy is now flat, remove all draw objects.
                if (Position.MarketPosition == MarketPosition.Flat)
                {
                    RemoveDrawObject("Stop");
                    //RemoveDrawObject("Target");
                    RemoveDrawObject("Entry");
                }	


 
            } 			
        }

        #region Properties
        [Description("")]
        [GridCategory("Parameters")]
        public int SL
        {
            get { return sl; }
            set { sl = Math.Max(0, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public int TP
        {
            get { return tp; }
            set { tp = Math.Max(0, value); }
        }		
        [Description("")]
        [GridCategory("Parameters")]
        public int ProfitLockTicks
        {
            get { return profitLockTicks; }
            set { profitLockTicks = Math.Max(0, value); }
        }	
        [Description("")]
        [GridCategory("Parameters")]
        public int LossReverseLimit
        {
            get { return lossReverseLimit; }
            set { lossReverseLimit = value; }
        }		
		
    	[Description("")]
        [GridCategory("Parameters")]
        public int SmoothingFactor
        {
            get { return smoothingFactor; }
            set { smoothingFactor = Math.Max(0, value); }
        }
    		        [Description("")]
        [GridCategory("Parameters")]
        public int SmoothingFactor2
        {
            get { return smoothingFactor2; }
            set { smoothingFactor2 = Math.Max(0, value); }
        }
		
		[Description("")]
        [GridCategory("Parameters")]
        public String EncogHost
        {
            get { return encogHost; }
            set { encogHost =  value; }
        }
		
        [Description("")]
        [GridCategory("Parameters")]
        public int EncogPort
        {
            get { return encogPort; }
            set { encogPort = Math.Max(0, value); }
        }			
		
	        [Description("")]
        [GridCategory("Parameters")]
        public int RegCount
        {
            get { return regCount; }
            set { regCount = Math.Max(0, value); }
        }		
	        [Description("")]
        [GridCategory("Parameters")]
        public double PnlLimitMaxLoss
        {
            get { return pnlLimitMaxLoss; }
            set { pnlLimitMaxLoss = value; }
        }	
	        [Description("")]
        [GridCategory("Parameters")]
        public double PnlLimitMaxGain
        {
            get { return pnlLimitMaxGain; }
            set { pnlLimitMaxGain = value; }
        }		
        [Description("")]
        [GridCategory("Parameters")]
        public bool EnablePnlLimit
        {
            get { return enablePnlLimit; }
            set { enablePnlLimit = value; }
        }		
		
        [Description("")]
        [GridCategory("Parameters")]
        public bool ReverseOnSignal
        {
            get { return reverseOnSignal; }
            set { reverseOnSignal = value; }
        }		
        [Description("")]
        [GridCategory("Parameters")]
        public bool LockProfit
        {
            get { return lockProfit; }
            set { lockProfit = value; }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public bool UseStrictSL
        {
            get { return stictSL; }
            set { stictSL = value; }
        }
		
		
        [Description("")]
        [GridCategory("Parameters")]
        public bool IsProduction
        {
            get { return production; }
            set { production = value; }
        }		
        [Description("")]
        [GridCategory("Parameters")]
        public bool UseLOM
        {
            get { return useLOM; }
            set { useLOM = value; }
        }
        [Description("")]
        [GridCategory("Parameters")]
        public bool UseQuickExit
        {
            get { return useQuickExit; }
            set { useQuickExit = value; }
        }
		
		
        [Description("")]
        [GridCategory("Parameters")]
        public bool TradeAggressively
        {
            get { return tradeAggressively; }
            set { tradeAggressively = value; }
        }
		

        [Description("")]
        [GridCategory("Parameters")]
        public int LotSize
        {
            get { return lotSize; }
            set { lotSize = Math.Max(0, value); }
        }
        [Description("")]
        [GridCategory("Parameters")]		
        public NeuralExitStrategy TrailingExit
        {
            get { return trailingExit; }
            set { trailingExit = value; }
		}
        [Description("")]
        [GridCategory("Parameters")]		
        public NeuralEntryStrategy EntryStrat
        {
            get { return entryStrat; }
            set { entryStrat = value; }
		}
        [Description("")]
        [GridCategory("Parameters")]		
        public NeuralEntryCurve EntryCurve
        {
            get { return entryCurve; }
            set { entryCurve = value; }
		}
		
	
       [Description("")]
        [GridCategory("Parameters")]
        public double TrailingExitFactor
        {
            get { return trailingExitFactor; }
            set { trailingExitFactor = Math.Max(0, value); }
        }
	
       [Description("")]
        [GridCategory("Parameters")]
        public int TrailingExitPeriods
        {
            get { return trailingExitPeriods; }
            set { trailingExitPeriods = Math.Max(0, value); }
        }
		
       [Description("")]
        [GridCategory("Parameters")]
        public int MaxHighLowPeriods
        {
            get { return maxHighLowPeriods; }
            set { maxHighLowPeriods = Math.Max(0, value); }
        }
		
		
       [Description("")]
        [GridCategory("Parameters")]
        public double PredProfitLimit
        {
            get { return predProfitLimit; }
            set { predProfitLimit = Math.Max(0, value); }
        }
		
       [Description("")]
        [GridCategory("Parameters")]
        public double ProfitLockAtrFactor
        {
            get { return profitLockAtrFactor; }
            set { profitLockAtrFactor = Math.Max(0, value); }
        }
		
        #endregion
    }
}
