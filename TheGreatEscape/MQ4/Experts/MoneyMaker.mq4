//+------------------------------------------------------------------+
//|                                         Moving Average Cross.mq4 |
//|                                                  Andrew R. Young |
//|                                 http://www.expertadvisorbook.com |
//+------------------------------------------------------------------+

#property copyright     "Steinar R. Eriksen"
#property link          "http://www.blackswan.no"
#property description   "Trend trading system with two moving averages, money management and fixed trailing stop"
#property strict


//+------------------------------------------------------------------+
//| Includes and object initialization                               |
//+------------------------------------------------------------------+

#include <Mql4Book\Trade.mqh>
CTrade Trade;
CCount Count;

#include <Mql4Book\Indicators.mqh>

#include <Mql4Book\Timer.mqh>
CNewBar NewBar;
#include <BlackSwan_Awesome.mqh>
#include <BlackSwan_ADX.mqh>
#include <BlackSwan_Accelerator.mqh>
#include <Mql4Book\TrailingStop.mqh>

#include <Mql4Book\MoneyManagement.mqh>

#include <BlackSwan_ForexInclude.mqh>

enum ENUM_TRTYPE
 { 
  REGRESSION= 1 , 
  AWESOME= 2 , 
  DESSERT= 3,
  COMPLEX=4,
  EASY=5
 };

input ENUM_TRTYPE TrendType=REGRESSION;

enum ENUM_OTYPE
 { 
  MARKET= 1 , 
  LIMIT= 2
 };
 
 input ENUM_OTYPE OType=MARKET;

input double TrendLim= 0.95;
input int SlMulti = 10;
input int TpMulti =5;
input int StepSize=8;
//+------------------------------------------------------------------+
//| Input variables                                                  |
//+------------------------------------------------------------------+

sinput string TradeSettings;    // Trade Settings
input int MagicNumber = 101;
input int Slippage = 10;
input bool TradeOnBarOpen = true;
input bool AutomaticClosing=true;
input int MaxTrades=3;

input double TakeProfitFactor=2;
input double StopLossFactor=3;
input int PipRangeBarCount=5;
input double MACDOpenLevel =3;
input double MACDCloseLevel=2;
input int    MATrendPeriod =26;
input int RegressionCount = 14;
input int RegressionStart=0;
input int  RegressionStop=5;
input int AwesomeLength=3;
input int BarsMaxOpen=3;
sinput string MoneySettings;  	// Money Management
input bool UseMoneyManagement = true;
input int ToTrades = 10;
input double RiskPercent = 2;
input double FixedLotSize = 0.1;

sinput string StopSettings;					// Stop Loss & Take Profit
input int StopLoss = 0;
input int TakeProfit = 0;

sinput string TrailingStopSettings;   	// Trailing Stop
input bool UseTrailingStop = true;
input int TrailingStop = 200;
input int MinProfit = 100;
input int Step = 10;

sinput string FastMaSettings;   // Fast Moving Average
input int FastMaPeriod = 5;
input ENUM_MA_METHOD FastMaMethod = MODE_EMA;
input ENUM_APPLIED_PRICE FastMaPrice = PRICE_CLOSE;
input int MinCrossSpread = 10;

sinput string SlowMaSettings;   // Slow Moving Average
input int SlowMaPeriod = 20;
input ENUM_MA_METHOD SlowMaMethod = MODE_EMA;
input ENUM_APPLIED_PRICE SlowMaPrice = PRICE_CLOSE;
double UsePoint=0;


//+------------------------------------------------------------------+
//| Global variable and indicators                                   |
//+------------------------------------------------------------------+

CiMA FastMa(_Symbol,_Period,FastMaPeriod,0,FastMaMethod,FastMaPrice);
CiMA SlowMa(_Symbol,_Period,SlowMaPeriod,0,SlowMaMethod,SlowMaPrice);


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+

int OnInit()
{
   // Set magic number
   Trade.SetMagicNumber(MagicNumber);
   Trade.SetSlippage(Slippage);
      UsePoint=GetPipPoint(Symbol());

   return(INIT_SUCCEEDED);
}
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
double CalcPipRange(int PipBarCount){
   double DiffPips=0;
   //Print("Last bar pips= ", DiffPips);
    for(int i=1; i<=PipBarCount; i++)  {

        DiffPips=DiffPips+ ((High[i]-Low[i])/Point)/10;
     }
     double PipRange=DiffPips/PipBarCount;
  
     return PipRange;
}

double CalcStopLevels(){  
 double MinStopLevel = MarketInfo(Symbol(), MODE_STOPLEVEL) + MarketInfo(Symbol(), MODE_SPREAD);



   double range = CalcPipRange(PipRangeBarCount);
   double UseStopLoss=range*StopLossFactor;
   if (UseStopLoss<MinStopLevel) UseStopLoss=MinStopLevel;
   double UseTakeProfits=range*TakeProfitFactor;
return UseStopLoss*UsePoint;

}
void ModifyStopLoss(){
   int x,trades_total=OrdersHistoryTotal();
   double MinStopLevel = MarketInfo(Symbol(), MODE_STOPLEVEL) + MarketInfo(Symbol(), MODE_SPREAD);
   double range = CalcPipRange(PipRangeBarCount);
   double UseStopLoss=range*StopLossFactor;
   if (UseStopLoss<MinStopLevel) UseStopLoss=MinStopLevel;
   double UseTakeProfits=range*TakeProfitFactor;
   for(int cnt=0;cnt<trades_total;cnt++)
     {
      int ticket=OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(!ticket)
         continue;
      if (Symbol()!=OrderSymbol() || MagicNumber!=OrderMagicNumber()) continue;
      
    Print("Orig stoploss=",StopLoss,  " and takepro=",TakeProfit);
     
     Print("New stoploss=",UseStopLoss,  " and takepro=",UseTakeProfits);
   //  ModifyStopsByPrice(ticket
      
       ModifyStopsByPoints(ticket,UseStopLoss,UseTakeProfits);
          
    }
}


int TrendSignal4(){
   //   double DSS_down=iCustom(NULL,0,"Robby DSS Bressert Colored with alert v1.1",false,8,13,2,0);
  //    double DSS_up=iCustom(NULL,0,"Robby DSS Bressert Colored with alert v1.1",false,8,13,1,0);
      int bullbear=0;   
  
 // int awesomeShift=CalcAwesomeCrossShift(0,AwesomeLength);
      if (Bid<Low[2] && Close[1]<Open[1]){//DSS_up!=0 && DSS_up<70 ){
                   bullbear=-1;
       }       
      if (Bid>High[2] && Close[1]>Open[1]){//DSS_down!=0 && DSS_down!=100 &&DSS_down>30 ){
                   bullbear=1;
       }
       
       return bullbear;  
}
int TrendSignal5(){
  //    double DSS_up=iCustom(NULL,0,"Robby DSS Bressert Colored with alert v1.1",false,8,13,1,0);
      int bullbear=0;   
  
 // int awesomeShift=CalcAwesomeCrossShift(0,AwesomeLength);
      if (Bid<Low[2]){// && Close[1]<Open[1]){//DSS_up!=0 && DSS_up<70 ){
                   bullbear=-1;
       }       
      if (Bid>High[2]){// && Close[1]>Open[1]){//DSS_down!=0 && DSS_down!=100 &&DSS_down>30 ){
                   bullbear=1;
       }
       
       return bullbear;  
}

int TrendSignal1(){
      int bullbear=0;
      if (slope(RegressionStart,RegressionCount)>slope(RegressionStop,RegressionCount)) bullbear=1; 
      if (slope(RegressionStart,RegressionCount)<slope(RegressionStop,RegressionCount)) bullbear=-1;
      return bullbear;  
}

int TrendSignal2(){
   int bearbull=0;
  double   MacdCurrent=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,0);
  double  MacdPrevious=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,1);
  double  SignalCurrent=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,0);
  double  SignalPrevious=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,1);
  double  MaCurrent=iMA(NULL,0,MATrendPeriod,0,MODE_EMA,PRICE_CLOSE,0);
 double   MaPrevious=iMA(NULL,0,MATrendPeriod,0,MODE_EMA,PRICE_CLOSE,1);
   
   if(MacdCurrent<0 && MacdCurrent>SignalCurrent  &&
         MathAbs(MacdCurrent)>(MACDOpenLevel*Point))
        bearbull=1;
        
   if(MacdCurrent>0 && MacdCurrent<SignalCurrent  &&
         MacdCurrent>(MACDOpenLevel*Point) )
        bearbull=-1;
   return (bearbull);   
}
int TrendSignal3(){
      int bullbear=0;//CalcAcceleratorColor(0, AwesomeLength);
     
     if ( Close[1]>(((High[1]-Low[1])*TrendLim)+Low[1])) bullbear=-1;
      if ( Close[1]<(High[1]-((High[1]-Low[1])*TrendLim))) bullbear=1;
  
   
   
      return bullbear;  
}


int TrendSignal(){
    int bullbear=0;
   switch ( TrendType )                           // Operator header 
      {                                          // Opening brace
      case 1: bullbear=TrendSignal1();break;                   // One of the 'case' variations 
      case 2: bullbear=TrendSignal2();break;                  // One of the 'case' variations 
      case 3: bullbear=TrendSignal3();break;
      case 4:bullbear=TrendSignal4();break;
       case 5:bullbear=TrendSignal5();break;
     
      } 
   return bullbear;
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+

void OnTick()
{
   // Check for bar open
   bool newBar = true;
   int barShift = 0;
   
   if(TradeOnBarOpen == true)
   {
      newBar = NewBar.CheckNewBar(_Symbol,_Period);
      barShift = 1;
   }
   if(newBar == true)
   {
      double minSpread = MinCrossSpread * _Point;
    double range = CalcPipRange(PipRangeBarCount);
   double UseTakeProfits=range*TakeProfitFactor;
   double UseStopLoss=range*StopLossFactor;
 //  if (UseStopLoss<MinStopLevel) 
   UseStopLoss=0;//MinStopLevel;
      Trade.CloseExpiredOrders(BarsMaxOpen);
      // Close orders
      if(AutomaticClosing && (FastMa.Main(barShift) > SlowMa.Main(barShift) + minSpread)) 
      {
         Trade.CloseAllSellOrders();
      }
      else if(AutomaticClosing && (FastMa.Main(barShift) < SlowMa.Main(barShift) - minSpread))
      {
         Trade.CloseAllBuyOrders();
      } 
       
      // Money management
      double lotSize = FixedLotSize;
      if(UseMoneyManagement == true)
      {
         lotSize = MoneyManagement(_Symbol,FixedLotSize,RiskPercent,StopLoss); 
      }
       int ticket = 0;
      // Trade.OpenBuyStopOrder(Symbol(), lotSize, (Close[1]),0,0);
      // Trade.OpenSellStopOrder(Symbol(), lotSize, (Close[1]),0,0);
      // Trade.OpenSellLimitOrder(Symbol(), lotSize, (High[1]+Close[1])/2,0,0);
      // Trade.OpenBuyLimitOrder(Symbol(), lotSize, (Low[1]+Close[1])/2,0,0);
     double MinStopLevel = MarketInfo(Symbol(), MODE_STOPLEVEL) + MarketInfo(Symbol(), MODE_SPREAD);
         double h=High[1];
         double l=Low[1];
         double m=(h+l)/2;
         Print("Low, High and Mid Price ", h, ",", l, " => ", m);
         double st=(h-l)/StepSize;
         Print("Step ", st); 
         double prange=NormalizeDouble(st*TpMulti,4);
          double srange=NormalizeDouble(st*SlMulti,4);
        
         Print("Stop range ", srange); 
         
      // Open buy order
      if( TrendSignal()==1 // slope(RegressionStart,RegressionCount)>slope(RegressionStop,RegressionCount)
         && Count.Buy() < MaxTrades )
      {
for (int v=1;v<=ToTrades;v++){
         ticket=Trade.OpenBuyLimitOrder(Symbol(),lotSize,NormalizeDouble(m-st*v,4),NormalizeDouble(m-st*v,4) - srange,NormalizeDouble(m-st*v,4)+prange);
         ModifyStopsByPrice(ticket,NormalizeDouble(m+st*v,4)-srange,NormalizeDouble(m+st*v,4)+prange);//, "Buy limit oconds()
        
         }
         
         
         //, "Buy limit order", PeriodSeconds());
      //   ModifyStopsByPoints(ticket,UseStopLoss,UseTakeProfits);
       //  ticket=Trade.OpenBuyLimitOrder(Symbol(),lotSize,NormalizeDouble(m-2*st,4),NormalizeDouble(m-2*st,4)-srange,NormalizeDouble(m-2*st,4)+prange);//, "Buy limit order", PeriodSeconds());
      //   ModifyStopsByPoints(ticket,UseStopLoss,UseTakeProfits);
        // ticket=Trade.OpenBuyLimitOrder(Symbol(),lotSize,NormalizeDouble(m-3*st,4),NormalizeDouble(m-3*st,4)-srange,NormalizeDouble(m-3*st,4)+prange);//, "Buy limit order", PeriodSeconds());
       //  ModifyStopsByPoints(ticket,UseStopLoss,UseTakeProfits);
      }
      
      // Open sell order
      else if( TrendSignal()==-1 // slope(RegressionStart,RegressionCount)<slope(RegressionStop,RegressionCount)
         && Count.Sell() < MaxTrades )
      {
for (int w=1;w<=ToTrades;w++){

         ticket=Trade.OpenSellLimitOrder(Symbol(),lotSize,NormalizeDouble(m+st*w,4),NormalizeDouble(m+st*w,4)+srange,NormalizeDouble(m+st*w,4)-prange);//, "Buy limit order", PeriodSeconds());
        ModifyStopsByPrice(ticket,NormalizeDouble(m+st*w,4)+srange,NormalizeDouble(m+st*w,4)-prange);//, "Buy limit oconds()

     
     }
     
      //   ModifyStopsByPoints(ticket,UseStopLoss,UseTakeProfits);
      //   ticket=Trade.OpenSellLimitOrder(Symbol(),lotSize,NormalizeDouble(m+2*st,4),NormalizeDouble(m+2*st,4)+srange,NormalizeDouble(m+2*st,4)-prange);//, "Buy limit order", PeriodSeconds());
      //   ModifyStopsByPoints(ticket,UseStopLoss,UseTakeProfits);
      //   ticket=Trade.OpenSellLimitOrder(Symbol(),lotSize,NormalizeDouble(m+3*st,4),NormalizeDouble(m+3*st,4)+srange,NormalizeDouble(m+3*st,4)-prange);//, "Buy limit order", PeriodSeconds());
      //   ModifyStopsByPoints(ticket,UseStopLoss,UseTakeProfits);
      }
   }  
      //   ModifyStopLoss();

   // Trailing stop
   if(UseTrailingStop == true)
   {
      TrailingStopAll(TrailingStop,MinProfit,Step);
   } 
    
}
  
//+------------------------------------------------------------------+
