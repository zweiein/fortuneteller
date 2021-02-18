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


enum ENUM_TRENDTYPE {
   REGRESSION= 1,
   MACD= 2
};
input ENUM_TRENDTYPE TrendType=REGRESSION;


sinput string TrendSettings;    // Trend Settings
input int RegressionCount = 14;
input int RegressionStart=0;
input int  RegressionStop=5;
input bool CounterTrend=false;

input int MaxLosers=2;

sinput string MoneyManagement;    // MonMan Settings
input int BarsMaxOpenPending = 10;
input int BarsMaxOpenOrders = 10;
input double MaxOpenPos=2.0;

input int InitialStopPoints=500;
input int InitialProfPoints=200;

input int MacdFastPer=12;
input int MacdSlowtPer=28;
input int MacdSigPer=9;


sinput string TrailingStopSettings;    // Trailing Stop
extern int    BackPeriod  =1000;
extern int    ATRPeriod  =3;
extern double Factor=3;
extern bool   TypicalPrice=false;
extern bool TrailStopInRealTime=false;

double UsePoint=0;
double BuyPos=0;
double SellPos=0;





int limit;
double PrevUp, PrevDn;
double CurrUp, CurrDn;
double PriceLvl;
double LvlUp=0;
double LvlDn=1000;
int Dir=1;
int InitDir=0;
//---- check for possible errors

//---- fill in buffervalues
// InitDir=0;
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
//|                                                                  |
//+------------------------------------------------------------------+
int MacdTrend()
{
   int trend=0;
   double dMACDSig = iMA(NULL,0,MacdFastPer,0,MODE_SMMA,PRICE_MEDIAN,0);
   double dMACD =  iMA(NULL,0,MacdSlowtPer,0,MODE_SMMA,PRICE_MEDIAN,0);
   double closePrev = iClose(NULL,0,1);
   double close =  iClose(NULL,0,0);
   int divergence=0;
   if (Close[1]<dMACDSig)// && closePrev>close)
      trend= -1;
   if (Close[1]>dMACDSig )//&& closePrev<close)
      trend=1;
   return trend;
}

int MajorTrend=0;
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
//
   if(TradeOnBarOpen == true) {
      newBar = NewBar.CheckNewBar(_Symbol,_Period);
      barShift = 1;
   }
   if(newBar == true || TrailStopInRealTime==true) {
      // Calc ATR levels used by Trailing stop
      if (TypicalPrice) PriceLvl=(High[0] + Low[0] + Close[0])/3;
      else PriceLvl=Close[0];
//----
      if(InitDir==0) {
         CurrUp=Close[0] - (iATR(NULL,0,ATRPeriod,0) * Factor);
         PrevUp=Close[1] - (iATR(NULL,0,ATRPeriod,1) * Factor);
         CurrDn=Close[0] + (iATR(NULL,0,ATRPeriod,0) * Factor);
         PrevDn=Close[1] + (iATR(NULL,0,ATRPeriod,1) * Factor);
//----
         if (CurrUp > PrevUp) Dir=1;
         LvlUp=CurrUp;
         if (CurrDn < PrevDn) Dir=-1;
         LvlDn=CurrDn;
         InitDir=1;
      }
      CurrUp=PriceLvl - (iATR(NULL,0,ATRPeriod,0) * Factor);
      CurrDn=PriceLvl + (iATR(NULL,0,ATRPeriod,0) * Factor);
//----
      double temp;
      if (Dir==1) {
         MajorTrend=1;
         if (CurrUp > LvlUp) {
            temp=CurrUp;
            LvlUp=CurrUp;
         }
         else {
            temp=LvlUp;
         }
         if (Low[0] < temp) {
            Dir=-1;
            LvlDn=1000;
         }
      }
      if (Dir==-1) {
         MajorTrend=-1;
         if (CurrDn < LvlDn) {
            temp=CurrDn;
            LvlDn=CurrDn;
         }
         else {
            temp=LvlDn;
         }
         if (High[0] > temp) {
            Dir=1;
            LvlUp=0;
         }
      }
   }
   if(newBar == true) {
      BuyPos=GetOpenPos(Symbol(),OP_BUY,MagicNumber);
      SellPos=GetOpenPos(Symbol(),OP_SELL,MagicNumber);
      int trend=0;//
      if (TrendType==REGRESSION) trend=CalcRegressionTrend( RegressionStart,  RegressionStop,  RegressionCount);
      if (TrendType==MACD) trend=MacdTrend();
      if (MaxOpenPos==0 || (BuyPos<MaxOpenPos && SellPos<MaxOpenPos)) {
         int losers=0;
         for(int order = 0; order <= OrdersTotal() - 1; order++) {
            OrderSelect(order,SELECT_BY_POS);
            if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber ) {
               if (OrderProfit()<0) losers+=1;
            }
         }
         if (BuyPos>0) {
            if (losers<MaxLosers)
               Trade.OpenBuyOrder(Symbol(), LotSize);
            else  Trade.CloseAllMarketOrders();
         }
         else if (SellPos>0) {
            if (losers<MaxLosers)
               Trade.OpenSellOrder(Symbol(), LotSize);
            else  Trade.CloseAllMarketOrders();
         }//else {
         if (trend>0) Trade.OpenBuyOrder(Symbol(), LotSize);
         else  Trade.OpenSellOrder(Symbol(), LotSize);
         // }
      }
   }
//Trailing stop loss executed
   if(newBar == true || TrailStopInRealTime==true) {
      for(int order = 0; order <= OrdersTotal() - 1; order++) {
         int res=OrderSelect(order,SELECT_BY_POS);//, MODE_TRADES);
         if(!res)
            continue;
         if (Symbol()!=OrderSymbol() || MagicNumber!=OrderMagicNumber()) continue;
         if (OrderType()==OP_SELL && Dir==-1)
            ModifyStopsByPrice(OrderTicket(),CurrDn,0);
         if (OrderType()==OP_BUY && Dir==1)
            ModifyStopsByPrice(OrderTicket(),CurrUp,0);
      }
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
