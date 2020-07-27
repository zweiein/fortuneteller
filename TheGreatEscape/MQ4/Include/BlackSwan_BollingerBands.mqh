//+------------------------------------------------------------------+
//|                                     BlackSwan_BollingerBands.mq4 |
//|                                      Copyright © 2010, Blackswan |
//|                                          http://www.blackswan.no |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, Blackswan"
#property link      "http://www.blackswan.no"


double bandsOneStdUpper = 0;
double bandsOneStdLower = 0;
double bandsTwoStdUpper = 0;
double bandsTwoStdLower = 0;
double bandsMain =0;
      
      
void UpdateBands(){
   bandsOneStdUpper = iBands(NULL,0,20,1,0,PRICE_CLOSE,MODE_UPPER,0);
   bandsOneStdLower = iBands(NULL,0,20,1,0,PRICE_CLOSE,MODE_LOWER,0);
   bandsTwoStdUpper = iBands(NULL,0,20,2,0,PRICE_CLOSE,MODE_UPPER,0);
   bandsTwoStdLower = iBands(NULL,0,20,2,0,PRICE_CLOSE,MODE_LOWER,0);
   bandsMain =iBands(NULL,0,20,2,0,PRICE_CLOSE,MODE_MAIN,0);
}


int DetectBollingerTrend(int periods){
   double last=iClose(NULL, 0, 0);
   double test=0;
   if (last>bandsOneStdUpper){
       for (int i=1;i<periods;i++){
         last=iLow(NULL, 0, i);
         test=iBands(NULL,0,20,1,0,PRICE_CLOSE,MODE_UPPER,i);
         if (last<test)
            return (0);
      }   
      return (1);
   }else if (last<bandsOneStdLower){
       for (i=1;i<periods;i++){
         last=iHigh(NULL, 0, i);
         test=iBands(NULL,0,20,1,0,PRICE_CLOSE,MODE_LOWER,i);
         if (last>test)
            return (0);
      }   
      return (-1);
   }
   return (0);
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