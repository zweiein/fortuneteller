//+------------------------------------------------------------------+
//|                                                   GridTrader.mq4 |
//|                        Copyright 2020, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright     "Steinar R. Eriksen"
#property link          "http://www.blackswan.no"
#property version   "1.00"
#property strict

#include <BlackSwan/TradeHelper.mqh>
#include <BlackSwan/TrendSignals.mqh>

sinput string TradeSettings;    // Trade Settings
input int MagicNumber = 666;
input int Slippage = 10;
input int OrderSteps=5;
input double LotSize=0.1;
input bool TradeOnBarOpen = true;
input bool AutomaticClosing=false;


enum ENUM_GRIDTYPE
 { 
  DEFAULT= 1 , 
  TIGHT= 2 
 };
input ENUM_GRIDTYPE GridType=DEFAULT;


sinput string TrendSettings;    // Trend Settings
input int RegressionCount = 14;
input int RegressionStart=0;
input int  RegressionStop=5;
input bool CounterTrend=false;

sinput string MoneyManagement;    // MonMan Settings
input int BarsMaxOpenPending = 10;
input int BarsMaxOpenOrders = 10;
input double MaxOpenPos=2.0;

input int InitialStopPoints=500;
input int InitialProfPoints=200;

sinput string TrailingStopSettings;   	// Trailing Stop
input bool UseTrailingStop = true;
input int TrailingStop = 200;
input int MinProfit = 100;
input int Step = 10;
 
double UsePoint=0;
double BuyPos=0;
double SellPos=0;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   Trade.SetMagicNumber(MagicNumber);
   Trade.SetSlippage(Slippage);
   UsePoint=GetPipPoint(Symbol());
//--- create timer
   EventSetTimer(60);
   
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- destroy timer
   EventKillTimer();
   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   bool newBar = true;
   int barShift = 0;
   
   if(TradeOnBarOpen == true)
   {
      newBar = NewBar.CheckNewBar(_Symbol,_Period);
      barShift = 1;
   }
   if(newBar == true)
   {

     // Print("BuyPos=", BuyPos, " SellPos=", SellPos);
      Trade.CloseExpiredOrders(BarsMaxOpenOrders);
      Trade.CloseExpiredPendingOrders(BarsMaxOpenPending);     
      BuyPos=GetOpenPos(Symbol(),OP_BUY,MagicNumber);
      SellPos=GetOpenPos(Symbol(),OP_SELL,MagicNumber); 
      
      
      double MinStopLevel = MarketInfo(Symbol(), MODE_STOPLEVEL) + MarketInfo(Symbol(), MODE_SPREAD);
      double h=High[1];
      double l=Low[1];
      double m=(h+l)/2;
      double st=(h-l)/OrderSteps;
      if (GridType==TIGHT) st=(m-l)/OrderSteps;
      
      int trend= CalcRegressionTrend( RegressionStart,  RegressionStop,  RegressionCount);
      
      if (MaxOpenPos==0 || (BuyPos<MaxOpenPos && SellPos<MaxOpenPos)){
         if((CounterTrend==false && trend==1) || (CounterTrend && trend==-1) )
         {
            for (int v=1;v<=OrderSteps;v++){ 
               double inc=NormalizeDouble(st*v,Digits);
               double lim=Bid-inc;
               int ticket=Trade.OpenBuyLimitOrder(Symbol(),LotSize,NormalizeDouble(lim,Digits),0,0);
            }
         } else if((CounterTrend==false && trend==-1) || (CounterTrend && trend==1) )
         {
            for (int v=1;v<=OrderSteps;v++){
               double inc=NormalizeDouble(st*v,Digits);
               double lim=Ask+inc;            
               int ticket=Trade.OpenSellLimitOrder(Symbol(),LotSize,NormalizeDouble(lim,Digits),0,0);
            }
         }
      }
      
   }else{
      BuyPos=GetOpenPos(Symbol(),OP_BUY,MagicNumber);
      SellPos=GetOpenPos(Symbol(),OP_SELL,MagicNumber);
      if (MaxOpenPos>0 && (BuyPos>=MaxOpenPos || SellPos>=MaxOpenPos)){
         Trade.DeleteAllPendingOrders();
       }
      if (InitialStopPoints>0 || InitialProfPoints>0)
         CheckInitialProfLoss(Symbol(), MagicNumber, InitialProfPoints, InitialStopPoints);
   }
   
   
   // Trailing stop
   if(UseTrailingStop == true)
   {
      TrailingStopAll(TrailingStop, MinProfit,Step);
   } 
  }
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
//---
   
  }
//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
//---
   
  }
//+------------------------------------------------------------------+
