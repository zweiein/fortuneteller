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
    public class FortuneRenko : Strategy
    {
        #region Variables
        // Wizard generated variables
        private int trail=4;
        private int sl = 10; // Default setting for STOP LOSS TICKS

        private int startTradeTime = 143000;
        private int stopTradeTime = 190000;

        private bool noHistorical=false;
        private bool ignoreBuiltInErrorHandling=false;
        private int maxContracts = 10;
        private int cumulativeProfit=0;

        private int netPos=0;
        private int sentPos=0;   // Used to restrict entries sent in SubmitOrder.  On RenkoBars, multiple orders may be issued in the same instant (before all are received in OnExecution). This will be updated locally

        Dictionary<string, IOrder> stopOrders=new Dictionary<string, IOrder>();
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            
            IgnoreOverFill = true;            
            EntriesPerDirection=4;
            EntryHandling = EntryHandling.AllEntries;           
            IncludeCommission = true;
            Slippage = 1;
            Unmanaged = true;
            CalculateOnBarClose = true;
            

            /*
                If Playback data is used the check on Order<>GetCurrentAsk/GetCurrentBid may fail and generate errors
                In such cases, you may want to disable the standard errorhandling
            */
            if (ignoreBuiltInErrorHandling)
                RealtimeErrorHandling = RealtimeErrorHandling.TakeNoAction;        

        }
        protected override void OnOrderUpdate(IOrder order) 
        {
            if (order.OrderType==OrderType.Stop)
            {
                // Rejection handling
                if (order.OrderState == OrderState.Rejected){
                        // If running without Error handling; decide how to notify of errors. Typically Stop Orders may fail when close to BID/ASK
                        // For that reason, previous stop orders (trailing) are not removed until new stop order is accepted.
                }else  if (order.OrderState == OrderState.Filled ||order.OrderState == OrderState.Cancelled ){
                    stopOrders.Remove(order.OrderId);
                }else  if (order.OrderState == OrderState.Accepted )
                {                   
                    // Remove old stop orders. Register newly accepted one
                    RemoveAllStopOrders();
                    if (order.OrderType==OrderType.Stop){
                        stopOrders[order.OrderId]=order;
                    }   
                    
                }
            }
        }
        
        private void RemoveAllStopOrders(){
            foreach (IOrder order in stopOrders.Values){                
                CancelOrder(order);
            }
            stopOrders.Clear();
        }
        
        private bool IsFill(IOrder order){
            return (order.OrderState == OrderState.Filled || order.OrderState == OrderState.PartFilled || (order.OrderState == OrderState.Cancelled && order.Filled > 0));
        }
        
        private void AdjustNetPosFromFill(IOrder order){
            if (order.OrderAction==OrderAction.Buy|| order.OrderAction==OrderAction.BuyToCover){
                netPos=netPos+order.Filled;
            }else{
                netPos=netPos-order.Filled;
            }   
        }
        
        protected override void OnExecution(IExecution execution)
        {
            // Managed stop order
            if (execution.Order.OrderType==OrderType.Stop && IsFill(execution.Order)){
                AdjustNetPosFromFill(execution.Order);
                stopOrders.Remove(execution.Order.OrderId);
            }
            
            // Entry order fill
            if (execution.Order.OrderType==OrderType.Market && IsFill(execution.Order)) {   
                AdjustNetPosFromFill(execution.Order);
                if  (execution.Order.OrderState == OrderState.Filled ){
                    RemoveAllStopOrders();  
                    UpdateTrailing();                   
                }                
            }            
            DisplayStatusBox();
        }

        protected override void OnMarketData(MarketDataEventArgs e)
        {

        }
  
        private void GoLong(){
            int qty=1;  
            if (netPos<0){
                qty=Math.Abs(netPos)+1;
            }
            if (qty>(1+maxContracts)) qty=maxContracts+1;
            if (Math.Abs(sentPos)< maxContracts || sentPos<0){
                sentPos=sentPos+1;
                SubmitOrder(0,OrderAction.Buy, OrderType.Market, qty, 0, 0, "", "Long");
            }
        }
        private void GoShort(){
            int qty=1;  
            if (netPos>0){
                qty=Math.Abs(netPos)+1;
            }
            if (qty>(1+maxContracts) ) qty=maxContracts+1;
            if (Math.Abs(sentPos) < maxContracts || sentPos>0){
                sentPos=sentPos-1;
                SubmitOrder(0,OrderAction.SellShort, OrderType.Market, qty, 0, 0, "", "Short" );
            }
        }
        private void UpdateTrailing(){
            int qua = Math.Abs(netPos); //Quantity to trail   
            if (netPos>0)
            {
                    double sprice=Low[0] -trail * TickSize;                
                    if (sprice>GetCurrentBid() && GetCurrentBid()>0)   //RENKO BARS: During replay data, GetCurrentBid = Close, and may generate order on wrong side of market here
                        sprice=GetCurrentBid()-trail * TickSize;                                                        
                    if (Close[0]>Open[0]){
                         SubmitOrder(0,OrderAction.Sell,OrderType.Stop,qua,0,sprice,"","LX");
                    }   
            } else if (netPos<0) {
                    double sprice=High[0] + trail* TickSize;
                    if (sprice<GetCurrentAsk() && GetCurrentAsk()>0)  //RENKO BARS: During replay data, GetCurrentAsk = Close, and may generate order on wrong side of market here
                        sprice=GetCurrentAsk() + trail * TickSize;
                    if (Close[0]<Open[0]){
                        SubmitOrder(0,OrderAction.BuyToCover,OrderType.Stop,qua,0,sprice,"","SX");
                    }
                }
        }

        private void ExitAll()
        {
            int qua = Math.Abs(netPos); //Quantity to exit
            RemoveAllStopOrders();
            if (netPos>0) {
                SubmitOrder(0,OrderAction.Sell, OrderType.Market, qua, 0, 0, "", "XitL");
            } else if (netPos<0) {
                SubmitOrder(0,OrderAction.BuyToCover, OrderType.Market, qua, 0, 0, "", "XitS");
            }
    }

        private void DisplayStatusBox() 
        {
            String TextMessage = "Total Profit: " + Performance.RealtimeTrades.TradesPerformance.Currency.CumProfit.ToString("C");
            double aPrice=0;     
            if (Position.MarketPosition == MarketPosition.Long) aPrice = GetCurrentBid(); 
            else aPrice = GetCurrentAsk();          
            if (Position.MarketPosition != MarketPosition.Flat) {

                    TextMessage += "\nProfit Unrealized: " + Position.GetProfitLoss(aPrice, PerformanceUnit.Currency).ToString("C"); 
                    TextMessage += "\nProfit this Day: " + (Performance.AllTrades.TradesPerformance.Currency.CumProfit - cumulativeProfit).ToString("C");                    
            }
            TextMessage += "\nProfit Total: " + (Performance.AllTrades.TradesPerformance.Currency.CumProfit).ToString("C");
            TextMessage+="\nNetPos: " + (netPos).ToString();
            DrawTextFixed("", TextMessage, TextPosition.TopLeft,Color.Red, new Font("Arial", 8), Color.Black, Color.LightGray, 100);
        }
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
            if (Historical && noHistorical) return;

            // If Renko Bars "pause", sync local variables.
            int sec=Time[0].Subtract(Time[1]).Seconds;
            if (sec>=1){
                if (Position.MarketPosition==MarketPosition.Long){
                    netPos=Position.Quantity;
                    sentPos=Position.Quantity;
                }
                else if (Position.MarketPosition==MarketPosition.Short){
                    netPos=-Position.Quantity;
                    sentPos=-Position.Quantity;
                }else{
                    sentPos=0;
                    netPos=0;
                }
                DisplayStatusBox();
            }
            
            UpdateTrailing();

            if ((ToTime(Time[0])<= stopTradeTime && ToTime(Time[0]) >= startTradeTime))
            {
                
                if (Close[0]>Open[0] && Close[1]>Open[1] )
                    GoLong();
                if (Close[0]<Open[0] && Close[1]<Open[1]  )
                    GoShort();
            }else{
                if (Position.MarketPosition==MarketPosition.Long)
                    ExitAll();
                if (Position.MarketPosition==MarketPosition.Short)
                    ExitAll();
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

        [Description("")]
                [GridCategory("Parameters")]
        public int MaxContracts
        {
            get { return maxContracts; }
            set { maxContracts = value; }
        }
        
        
        [Description("")]
        [GridCategory("Parameters")]
        public bool NoHistorical
        {
            get { return noHistorical; }
            set { noHistorical = value; }
        }   
        
        [Description("")]
        [GridCategory("Parameters")]
        public bool IgnoreBuiltInErrorHandling
        {
            get { return ignoreBuiltInErrorHandling; }
            set { ignoreBuiltInErrorHandling = value; }
        }           
        
        [Description("")]
        [GridCategory("Parameters")]
        public int TrailingTicks
        {
            get { return trail; }
            set { trail = value; }
        }   
        #endregion
    }
}
