//+------------------------------------------------------------------+
//|                                             BlackSwan_Pivots.mq4 |
//|                                      Copyright © 2010, Blackswan |
//|                                          http://www.blackswan.no |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, Blackswan"
#property link      "http://www.blackswan.no"

double PP=0;   // Pivot Point
double R1=0;
double R2=0;
double R3=0;
double S1=0;
double S2=0;
double S3=0;
double PrevHigh=0;
double PrevLow=0;
double PrevClose=0;
double TodaysHigh=0;
double TodaysLow=0;


int CurrentPivotBar=0;
int PrevPivotBar=0;

void ResetPivots(int PivotPeriod){
   Print("Resetting pivot points");
   PrevHigh= iHigh(NULL, PivotPeriod, 1);
   PrevLow = iLow(NULL, PivotPeriod, 1);
   PrevClose = iClose(NULL, PivotPeriod, 1);
   TodaysHigh=High[0];
   TodaysLow=Low[0];
   PP=(PrevHigh+PrevLow+PrevClose)/3;
   R1=PP*2-PrevLow;
   R2=PP + PrevHigh - PrevLow;
   R3=R2 + PrevHigh - PrevLow;
   S1=PP*2-PrevHigh;
   S2=PP - PrevHigh + PrevLow;
   S3=S2 - PrevHigh + PrevLow;   
}

bool IsWithinRangeOfPivot(double value, double target, double range){
  // Print("Check IsWithinRange value, target, range "+  value + " " + target + " " + range);
   if (value>(target-range) && value<(target+range)){
      return (true);
   }
   return (false);
}

bool PivotSupportResistanceCheck(double suppres, bool support, int PointRange){
   
   if (support){
      if (IsWithinRangeOfPivot(Bid, suppres, PointRange) && Low[0]<suppres && Bid>suppres) return (true);
   }else{
      if (IsWithinRangeOfPivot(Ask, suppres, PointRange) && High[0]>suppres && Ask<suppres) return (true);
   }
   return (false);
}

bool IsAtPivotSupport(int PointRange){

   if (PivotSupportResistanceCheck(S1, true, PointRange) || PivotSupportResistanceCheck(S2, true, PointRange) || PivotSupportResistanceCheck(S3, true, PointRange)){
      return (true);
   }
   return (false);
}

bool IsAtPivotResistance(int PointRange){

   if (PivotSupportResistanceCheck(R1, false, PointRange) || PivotSupportResistanceCheck(R2, false, PointRange) || PivotSupportResistanceCheck(R3, false, PointRange)){
      return (true);
   }
   return (false);
}

//+------------------------------------------------------------------+
//| defines                                                          |
//+------------------------------------------------------------------+
// #define MacrosHello   "Hello, world!"
// #define MacrosYear    2005

//+------------------------------------------------------------------+
//| DLL imports                                                      |
//+------------------------------------------------------------------+
// #import "user32.dll"
//   int      SendMessageA(int hWnd,int Msg,int wParam,int lParam);

// #import "my_expert.dll"
//   int      ExpertRecalculate(int wParam,int lParam);
// #import

//+------------------------------------------------------------------+
//| EX4 imports                                                      |
//+------------------------------------------------------------------+
// #import "stdlib.ex4"
//   string ErrorDescription(int error_code);
// #import
//+------------------------------------------------------------------+