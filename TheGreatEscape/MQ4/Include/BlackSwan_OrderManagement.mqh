//+------------------------------------------------------------------+
//|                                    BlackSwan_OrderManagement.mq4 |
//|                                      Copyright © 2010, Blackswan |
//|                                          http://www.blackswan.no |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, Blackswan"
#property link      "http://www.blackswan.no"

#include <stdLib.mqh>

void CloseByTicket(int ticket, double slippage){

   bool f=OrderSelect(ticket, SELECT_BY_TICKET);
   if (f){
   
      if (OrderCloseTime()==0){
         if (OrderType()==OP_BUY){
             double CloseLots=OrderLots();
             double ClosePrice=Bid;
             bool Closed=OrderClose(ticket, CloseLots, ClosePrice, slippage, Red);
         }else{
             CloseLots=OrderLots();
             ClosePrice=Ask;
             Closed=OrderClose(ticket, CloseLots, ClosePrice, slippage, Red);
         }
      }
   }
        
       
}

bool AreAllOrdersClosed(int MagicNumber){

   bool allClosed=true;           
   for (int c=0; c<=OrdersTotal()-1; c++){
      OrderSelect(c, SELECT_BY_POS); 
      if ((OrderMagicNumber()==MagicNumber) || (MagicNumber==0)){
         if (OrderCloseTime()==0){
            allClosed=false;  
         }
      }      
   }

}
bool MyOrderClose(int Ticket,  int BuyOrSell, double Lots, double Slippage, int Color){
 while(IsTradeContextBusy())Sleep(10);
 RefreshRates();  
 bool ok=true;  
   if (BuyOrSell==OP_BUY){
         ok= OrderClose(Ticket,Lots, Ask, Slippage, Color);     
    }
    else if (BuyOrSell==OP_SELL){
        ok=  OrderClose(Ticket, Lots, Bid, Slippage, Color);     
    }
   if (ok==false){
      string errCode=GetLastError();
      string errDesc=ErrorDescription(errCode);
      string tp="buy";
      if (BuyOrSell==OP_SELL) tp="sell";
   
      string error=StringConcatenate("Closing ", tp, " order error " , errCode, " : " , errDesc);
      Print(error);
      SendMail("Problems on order entry", error);
   }
   return (ok);
}

bool MyCloseAllOrders(int MagicNumber, double Slippage, int Color){

   bool allClosed=true;           
   for (int c=0; c<=OrdersTotal()-1; c++){
      OrderSelect(c, SELECT_BY_POS); 
      if ((OrderMagicNumber()==MagicNumber) || (MagicNumber==0)){
         if (OrderCloseTime()==0){
             MyOrderClose(OrderTicket(),OrderType(),OrderLots(), Slippage, Color);
             //OrderDelete(OrderTicket());
         }
      }      
   }

}


bool MyCloseAllPendingOrdersOfType(int MyOrderType, int MagicNumber, double Slippage,int Color){

   bool allClosed=true;           
   for (int c=0; c<=OrdersTotal()-1; c++){
      OrderSelect(c, SELECT_BY_POS); 
         Print(MyOrderType + " " + OrderType());
         if (OrderType()==MyOrderType){
             OrderDelete(OrderTicket());
         }
         
   }

}
bool MyCloseAllOrdersOfType(int MyOrderType, int MagicNumber, double Slippage,int Color){

   bool allClosed=true;           
   for (int c=0; c<=OrdersTotal()-1; c++){
      OrderSelect(c, SELECT_BY_POS); 
      if ((OrderMagicNumber()==MagicNumber) || (MagicNumber==0)){
         if (OrderCloseTime()==0 && OrderType()==MyOrderType){
             MyOrderClose(OrderTicket(),OrderType(),OrderLots(), Slippage, Color);
         }
      }      
   }

}

int MyOrderSend(string CurrSymbol, int BuyOrSell, double Lots, double Slippage, double StopLossPoints, double TakeProfitPoints,string CommentStr, int MagicNumber, int Expiration,int Color){

 while(IsTradeContextBusy())Sleep(10);
 RefreshRates();
 double bidask=Ask;
 if (BuyOrSell==OP_SELL)
   bidask=Bid;

 int ticket= OrderSend(CurrSymbol, BuyOrSell, Lots, bidask, Slippage, 0, 0,CommentStr, MagicNumber, Expiration,Color); 
 if (ticket==-1){
   
   string errCode=GetLastError();
   string errDesc=ErrorDescription(errCode);
   string tp="buy";
   if (BuyOrSell==OP_SELL) tp="sell";
   
   string error=StringConcatenate("Open ", tp, " order error " , errCode, " : " , errDesc, ". Lots=", Lots);
   Print(error);
   SendMail("Problems on order entry", error);
 }else{
   
   OrderSelect(ticket,SELECT_BY_TICKET);
   RefreshRates();
   double stoploss=0;
   double takeprofit=0;
   if (BuyOrSell==OP_SELL)
   {
      stoploss=Bid + StopLossPoints;
      takeprofit=Bid - TakeProfitPoints;
   }else{
      stoploss=Ask - StopLossPoints;
      takeprofit=Ask + TakeProfitPoints;
   }   
   bool status = OrderModify(ticket,OrderOpenPrice(),stoploss, takeprofit,0,Blue);
   error=StringConcatenate("Open ", tp, " order. Lots=", Lots);
   Print(error);
   SendMail("Order entered", error);   
 }
 return (ticket);
}


int MyOrderSendLimit(string CurrSymbol, int BuyOrSell, double LimitPrice, double Lots, double Slippage, double StopLossPoints, double TakeProfitPoints,string CommentStr, int MagicNumber, int Expiration,int Color){

 while(IsTradeContextBusy())Sleep(10);
 RefreshRates();
 double bidask=Ask;
 if (BuyOrSell==OP_SELL)
   bidask=Bid;

 Print("LimitPrice=" + LimitPrice);
 LimitPrice=NormalizeDouble(LimitPrice,Digits);
 Print("LimitPrice=" + LimitPrice);
 int ticket= OrderSend(CurrSymbol, BuyOrSell, Lots, LimitPrice, Slippage, 0, 0,CommentStr, MagicNumber, Expiration,Color); 
 if (ticket==-1){
   
   string errCode=GetLastError();
   string errDesc=ErrorDescription(errCode);
   
   string tp="buy";
   if (BuyOrSell==OP_SELL) tp="sell";
   
   string error=StringConcatenate("Open ", tp, " order error " , errCode, " : " , errDesc, ". Lots=", Lots);
   Print(error);
   SendMail("Problems on order entry", error);
 }else{
   
   OrderSelect(ticket,SELECT_BY_TICKET);
   RefreshRates();
   double stoploss=0;
   double takeprofit=0;
   if (BuyOrSell==OP_SELL)
   {
      stoploss=LimitPrice + StopLossPoints;
      takeprofit=LimitPrice - TakeProfitPoints;
   }else{
      stoploss=LimitPrice - StopLossPoints;
      takeprofit=LimitPrice + TakeProfitPoints;
   }   
   bool status = OrderModify(ticket,OrderOpenPrice(),stoploss, takeprofit,0,Blue);
   
 }
 return (ticket);
}
void AdjTrailingStops(string argSymbol,double argPipPoint, int argTrailingStop, int argMinProfit, int argMagicNumber){
   while(IsTradeContextBusy())Sleep(10);
   
   for (int trailCounter=0; trailCounter<=OrdersTotal()-1; trailCounter++){
      RefreshRates();
      OrderSelect(trailCounter, SELECT_BY_POS);
      double MinProfit=argMinProfit*argPipPoint;
      double MaxStopLoss=0;
      double PipsProfit=0;
      int mode=MODE_BID;
      if (OrderType()==OP_BUY){
         MaxStopLoss=MarketInfo(argSymbol, MODE_BID) - (argTrailingStop*argPipPoint);
         PipsProfit=MarketInfo(argSymbol, MODE_BID) - OrderOpenPrice();
      }else{
         //Print("MarketInfo(argSymbol, MODE_ASK) = " + MarketInfo(argSymbol, MODE_ASK));
         //Print("(argTrailingStop*argPipPoint) = " + (argTrailingStop*argPipPoint));
         MaxStopLoss=MarketInfo(argSymbol, MODE_ASK) + (argTrailingStop*argPipPoint);
         PipsProfit=OrderOpenPrice()-MarketInfo(argSymbol, MODE_ASK);
      }
      
      Print("MaxStopLoss=" + MaxStopLoss + " OrderStopLoss()=" + OrderStopLoss() + "  PipsProfit=" + PipsProfit + " MinProfit=" + MinProfit);
     // if (OrderMagicNumber()==argMagicNumber && OrderSymbol()==argSymbol && OrderStopLoss()<MaxStopLoss && PipsProfit>=MinProfit){
       if (PipsProfit>=MinProfit){
         OrderModify(OrderTicket(), OrderOpenPrice(), MaxStopLoss, OrderTakeProfit(), 0);
      }
      
   }
}


