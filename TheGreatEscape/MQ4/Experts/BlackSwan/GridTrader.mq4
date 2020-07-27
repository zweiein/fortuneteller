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

sinput string TrendSettings;    // Trend Settings
input int RegressionCount = 14;
input int RegressionStart=0;
input int  RegressionStop=5;

sinput string MoneyManagement;    // MonMan Settings
input int BarsMaxOpenPending = 10;
input int BarsMaxOpenOrders = 10;
input double MaxOpenPos=2.0;
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
      Print("BuyPos=", BuyPos, " SellPos=", SellPos);
      Trade.CloseExpiredOrders(BarsMaxOpenOrders);
      Trade.CloseExpiredPendingOrders(BarsMaxOpenPending);      
      double MinStopLevel = MarketInfo(Symbol(), MODE_STOPLEVEL) + MarketInfo(Symbol(), MODE_SPREAD);
      double h=High[1];
      double l=Low[1];
      double m=(h+l)/2;
      Print("Low, High and Mid Price ", h, ",", l, " => ", m);
      double st=(h-l)/OrderSteps;
      
      int trend= CalcRegressionTrend( RegressionStart,  RegressionStop,  RegressionCount);
      if(trend==1  )
      {
         for (int v=1;v<=OrderSteps;v++){
            int ticket=Trade.OpenBuyLimitOrder(Symbol(),LotSize,NormalizeDouble(m-st*v,4),0,0);
         }
      } else if(trend==-1  )
      {
         for (int v=1;v<=OrderSteps;v++){
            int ticket=Trade.OpenSellLimitOrder(Symbol(),LotSize,NormalizeDouble(m-st*v,4),0,0);
         }
      }
   }else{
      BuyPos=GetOpenPos(_Symbol,OP_BUY,MagicNumber);
      SellPos=GetOpenPos(_Symbol,OP_SELL,MagicNumber);
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
