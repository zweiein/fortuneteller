//+------------------------------------------------------------------+
//|                                                  TradeHelper.mqh |
//|                        Copyright 2020, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property strict
//+------------------------------------------------------------------+
//| defines                                                          |
//+------------------------------------------------------------------+
// #define MacrosHello   "Hello, world!"
// #define MacrosYear    2010
//+------------------------------------------------------------------+
//| DLL imports                                                      |
//+------------------------------------------------------------------+
// #import "user32.dll"
//   int      SendMessageA(int hWnd,int Msg,int wParam,int lParam);
// #import "my_expert.dll"
//   int      ExpertRecalculate(int wParam,int lParam);
// #import
//+------------------------------------------------------------------+
//| EX5 imports                                                      |
//+------------------------------------------------------------------+
// #import "stdlib.ex5"
//   string ErrorDescription(int error_code);
// #import
//+------------------------------------------------------------------+
#include <Mql4Book\Trade.mqh>
#include <Mql4Book\Indicators.mqh>
#include <Mql4Book\Timer.mqh>
#include <Mql4Book\TrailingStop.mqh>
#include <Mql4Book\MoneyManagement.mqh>
CNewBar NewBar;
CTrade Trade;
CCount Count;



double GetPipPoint(string Currency){
   double CalcPoint=0;
   int CalcDigits = MarketInfo(Currency, MODE_DIGITS);
   if (CalcDigits==2 || CalcDigits == 3)  CalcPoint = 0.01;
   else if (CalcDigits==4 || CalcDigits == 5)  CalcPoint = 0.0001;
   
   return(CalcPoint);
}


int GetSlippage(string Currency, int SlippagePips){
   double CalcSlippage=0;
   int CalcDigits = MarketInfo(Currency, MODE_DIGITS);
   if (CalcDigits==2 || CalcDigits == 3)  CalcSlippage = SlippagePips;
   else if (CalcDigits==4 || CalcDigits == 5)  CalcSlippage = SlippagePips*10;
   return(CalcSlippage);
}

int GetOpenPos(string Symb, int OType, int MagicNumber){
   double opPos=0;
   // Loop through open order pool from oldest to newest
   for(int order = 0; order <= OrdersTotal() - 1; order++)
   {
      // Select order
      bool result = OrderSelect(order,SELECT_BY_POS);
      if(OrderSymbol() == Symb && OrderMagicNumber() == MagicNumber && OrderType()==OType){
         opPos+=OrderLots();
      }
   }
   return opPos;
}