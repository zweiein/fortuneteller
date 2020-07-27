//+------------------------------------------------------------------+
//|                                                 TrendSignals.mqh |
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
double slope(int iBar, int nBars)
{
   double sumy=0,
          sumx=0,
          sumxy=0,
          sumx2=0;
          
   int iLimit = iBar + nBars;
   for(; iBar < iLimit; iBar++)
      {
      sumy+=Close[iBar];
      sumxy+=Close[iBar]*iBar;
      sumx+=iBar;
      sumx2+=iBar*iBar;
      }      
   double teller=(nBars*sumxy - sumx*sumy);
   double nevner=(nBars*sumx2 - sumx*sumx);
   if (nevner==0) return 0;
   return(teller  / nevner );
}


int CalcRegressionTrend(int RegressionStart, int RegressionStop, int RegressionCount){
      int bullbear=0;
      if (slope(RegressionStart,RegressionCount)>slope(RegressionStop,RegressionCount)) bullbear=1; 
      if (slope(RegressionStart,RegressionCount)<slope(RegressionStop,RegressionCount)) bullbear=-1;
      return bullbear;  
}