//+------------------------------------------------------------------+
//|                                              BlackSwan_Gator.mq4 |
//|                                      Copyright © 2010, Blackswan |
//|                                          http://www.blackswan.no |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, Blackswan"
#property link      "http://www.blackswan.no"

      /*
      double GatorUpperPrev=iGator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_UPPER, 1);
      double GatorUpper=iGator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_UPPER, 0);
      double GatorLowerPrev=iGator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_LOWER, 1);
      double GatorLower=iGator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_LOWER, 0);
      
      double GatorJawBlue=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_GATORJAW, -3);
      double GatorTeethRed=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_GATORTEETH, -3);     
      double GatorLipsGreen=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_GATORLIPS, -3); 
      double GatorJawBluePrev=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_GATORJAW, -2);
      double GatorTeethRedPrev=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_GATORTEETH, -2);     
      double GatorLipsGreenPrev=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_GATORLIPS, -2);
      */
double GatorJawBlue=0;
double GatorTeethRed=0;
double GatorLipsGreen=0;
double GatorJawBluePrev=0;
double GatorTeethRedPrev=0;
double GatorLipsGreenPrev=0;

double UpdateAlligator(){
       GatorJawBlue=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_GATORJAW, 1);
       GatorTeethRed=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_GATORTEETH, 1);     
       GatorLipsGreen=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_GATORLIPS, 1); 
       GatorJawBluePrev=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_GATORJAW, 2);
       GatorTeethRedPrev=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_GATORTEETH, 2);     
       GatorLipsGreenPrev=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_GATORLIPS, 2); 
       
}

int PositionRelativeToMouth(){
   if (iHigh(NULL,0,1)<GatorJawBlue && iHigh(NULL,0,1)<GatorTeethRed && iHigh(NULL,0,1)<GatorLipsGreen)
      return (-1);
  if (iLow(NULL,0,1)>GatorJawBlue && iLow(NULL,0,1)>GatorTeethRed && iLow(NULL,0,1)>GatorLipsGreen)
      return (1);
      
   return (0);
}

int GetAllogatorCrossing(){
   if (GatorJawBlue>GatorLipsGreen && GatorJawBluePrev<GatorLipsGreenPrev)
      return (-1);
   if (GatorLipsGreen>GatorJawBlue && GatorLipsGreenPrev<GatorJawBluePrev)
      return (1);
      
   return (0);
}


double GetGatorValue(int timeFrame, int period){
   
   double upper=iGator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_UPPER, period);
   double lower=iGator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_LOWER, period);
   return (MathAbs(upper) + MathAbs(lower));
}

bool IsHungryAlligator(int periods){
   double curr=GetGatorValue(0, 0);
   for(int i=periods;i>1;i--){
      double test=GetGatorValue(0, i);
      if (curr>(test*1.5)){
      
      }else{
         return (false);
      }
   }
      
   return (true);
}

double CalcGator(int timeFrame, int periods, int upperlower){

   double GatorUpper=iGator(NULL, timeFrame, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_UPPER, periods);
   double GatorLower=iGator(NULL, timeFrame, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_LOWER, periods);
   if (upperlower==0)
      return (MathAbs( GatorUpper) + MathAbs(GatorLower));
   if (upperlower==1)
      return (MathAbs( GatorUpper));
    if (upperlower==-1)
      return (MathAbs( GatorLower));
}

double CalcGatorIncrease(int timeFrame, int periods){
   int count=0;

   double GatorUpperPrev=iGator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_UPPER, periods);
   double GatorUpper=iGator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_UPPER, 0);
   double GatorLowerPrev=iGator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_LOWER, periods);
   double GatorLower=iGator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_MEDIAN, MODE_LOWER, 0);
   double prev=MathAbs( GatorLowerPrev) + MathAbs(GatorUpperPrev);
   double now=MathAbs( GatorLower) + MathAbs(GatorUpper);
   if (prev==0) return (0);
   return (now/prev);
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