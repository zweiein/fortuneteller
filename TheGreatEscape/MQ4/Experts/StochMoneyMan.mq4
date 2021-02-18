//+------------------------------------------------------------------+
//|                                     Stochastic Money man.mq4 |
//|                                                  Steinar R. Eriksen |
//|                                 http://www.expertadvisorbook.com |
//+------------------------------------------------------------------+

#property copyright     "Steinar R. Eriksen"
#property link          "http://www.expertadvisorbook.com"
#property description   "Counter-trend system with stochastics"
#property strict


//+------------------------------------------------------------------+
//| Includes and object initialization                               |
//+------------------------------------------------------------------+

#include <Mql4Book\Trade.mqh>
CTrade Trade;
CCount Count;

#include <Mql4Book\Indicators.mqh>
#include <Mql4Book\TrailingStop.mqh>
#include <Mql4Book\MoneyManagement.mqh>


//+------------------------------------------------------------------+
//| Input variables                                                  |
//+------------------------------------------------------------------+

sinput string TS;    // Trade Settings
input int MagicNumber = 101;
input int Slippage = 10;
input bool TradeOnBarOpen = true;
input int MaxConcurrentOrders=4;
sinput string MM; 	// Money Management
input bool UseMoneyManagement = true;
input double RiskPercent = 2;
input double FixedLotSize = 0.1;
input int StopLoss = 200;
input int TakeProfit = 0;

sinput string ST;    // Stochastics Settings
input int KPeriod = 14;
input int DPeriod = 3;
input int Slowing = 5;
input ENUM_MA_METHOD MaMethod = MODE_SMA;
input int PriceField = 0;


input bool OverrideTP=true;
input bool OverrideSL=true;
input double TakeProfitFactor=2;
input double StopLossFactor=3;
input int PipRangeBarCount=5;


//+------------------------------------------------------------------+
//| Global variables and indicators                                   |
//+------------------------------------------------------------------+

CiStochastic Stoch(_Symbol,_Period,KPeriod,DPeriod,Slowing,MaMethod,PriceField);


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+

int OnInit()
{
   // Set magic number
   Trade.SetMagicNumber(MagicNumber);
   Trade.SetSlippage(Slippage);
   
   return(INIT_SUCCEEDED);
}


double CalcPipRange(int PipBarCount){
   double DiffPips=0;
   //Print("Last bar pips= ", DiffPips);
    for(int i=1; i<=PipBarCount; i++)  {

        DiffPips=DiffPips+ ((High[i]-Low[i])/Point)/10;
     }
     double PipRange=DiffPips/PipBarCount;
    // Print("PIP range=", PipRange);
     return PipRange;
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+

void OnTick()
{
   // Money management
   double lotSize = FixedLotSize;
   if(UseMoneyManagement == true)
   {
      lotSize = FixedLotSize;//MoneyManagement(_Symbol,FixedLotSize,RiskPercent,StopLoss); 
   }
  //double minSpread = MinCrossSpread * _Point;
   double range = CalcPipRange(PipRangeBarCount);
   double UseTakeProfits=0;
   if (OverrideTP)
       UseTakeProfits =range*TakeProfitFactor;
   else
       UseTakeProfits =TakeProfit;
       
    double UseStopLoss=0;
   if (OverrideSL)
       UseStopLoss =range*StopLossFactor;
   else
       UseStopLoss =StopLoss;
       
   double MinStopLevel = MarketInfo(Symbol(), MODE_STOPLEVEL) + MarketInfo(Symbol(), MODE_SPREAD);
   if (OverrideSL && UseStopLoss<MinStopLevel) 
      UseStopLoss=MinStopLevel;
      
  if (IsTradeAllowed()==false   ) {
    return;
  }
      
      
   // Close buy order
   if(Stoch.Main(1) < Stoch.Signal(1) && Count.Buy() > 0)
   {
      Trade.CloseAllBuyOrders();
   }
   
   // Close sell order
   if(Stoch.Main(1) > Stoch.Signal(1) && Count.Sell() > 0)
   {
      Trade.CloseAllSellOrders();
   }
   
   // Open buy order
   if( Stoch.Main(2) < 20 && Stoch.Main(2) < Stoch.Signal(2) 
      && Stoch.Main(1) > Stoch.Signal(1) && Count.Buy() < MaxConcurrentOrders )
   {
      int ticket = Trade.OpenBuyOrder(_Symbol,lotSize);
      ModifyStopsByPoints(ticket,UseStopLoss,UseTakeProfits);
   }
   
   // Open sell order
   else if( Stoch.Main(2) > 80 && Stoch.Main(2) > Stoch.Signal(2) 
      && Stoch.Main(1) < Stoch.Signal(1) && Count.Sell() < MaxConcurrentOrders )
   {
      int ticket = Trade.OpenSellOrder(_Symbol,lotSize);
      ModifyStopsByPoints(ticket,UseStopLoss,UseTakeProfits);
   }
}
  
//+------------------------------------------------------------------+

