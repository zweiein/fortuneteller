//+------------------------------------------------------------------+
//|                                             BlackSwan_Trends.mq4 |
//|                      Copyright © 2010, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"


/*
   Returns -1 on bear trend, 0 on range and 1 on bull trend
*/
int ResolveTrend(string Symb, int TimeFrame, string TimeFramDesc){
  
   int AdxPlusDi=iADX(Symb,TimeFrame,20, PRICE_CLOSE, MODE_PLUSDI, 0); 
   int AdxMinusDi=iADX(Symb,TimeFrame,20, PRICE_CLOSE, MODE_MINUSDI, 0); 
   int AdxValuePrev=iADX(Symb,TimeFrame,20, PRICE_CLOSE, MODE_MAIN, 1); 
   int AdxValue=iADX(Symb,TimeFrame,20, PRICE_CLOSE, MODE_MAIN, 0);
   Print("Adx(20) for " + Symb + " at " + TimeFramDesc + " was " + AdxValue + "," + AdxValuePrev + "," + AdxPlusDi + ","  + AdxMinusDi);
   if (AdxValue<20 && AdxValue<AdxValuePrev)
      return(0);
   if (AdxValue<AdxMinusDi && AdxValue<AdxPlusDi)
      return (0);
   if  (AdxPlusDi>AdxMinusDi)
      return(1);
   if  (AdxPlusDi<AdxMinusDi)
      return(-1);
   return (0);
}

int CloseTrend(int periods){
   
   double price1=iClose(NULL,0,periods-1);
   double pricetest=iClose(NULL,0,periods-2);
   if (price1<pricetest){
      for (int close=(periods-3);close>=0;close--){
         Print ("close=" + close);
         double lower=iClose(NULL,0,close);
         Print (price1 + " " + pricetest + " " + lower);
         if (lower<pricetest)
            return (0);
         pricetest=lower;
      }
      return (1);
   }else{
      for (close=(periods-3);close>=0;close--){
      Print ("close=" + close);
           lower=iClose(NULL,0,close);
           Print ("Short " + price1 + " " + pricetest + " " + lower);
         if (lower>pricetest)
            return (0);
         pricetest=lower;
      }
      return (-1);
   }
   return (0);   
}

int GetVortexOscillatorTrend(int TimeFrame, int periods){
   
   int flag=0;
   int bearbull=0;
   double firstvalue=iCustom(NULL,TimeFrame,"Vortex Oscillator",14,0,periods);
   double value=firstvalue;
   for(int counter = (periods-1); counter> 0; counter--){
      double newvalue=iCustom(NULL,TimeFrame,"Vortex Oscillator",14,0,counter);
      if (flag==0){
         if (newvalue>value)
            bearbull=1;
         else
            bearbull=-1;
            
            flag=1;
      }
      else if (bearbull==1){
      
         if (newvalue<value)
            return(0);
           
      }else  if (bearbull==-1){
         if (newvalue>value)
            return(0);
      }
      value=newvalue;  
 
   }

   return (bearbull);      
}


int GetVortexTrendSetter(int TimeFrame, int periods){
   
   int flag=0;
   int bearbull=0;
   double firstvalue=iCustom(NULL,TimeFrame,"Vortex Oscillator",14,0,periods);
   double value=firstvalue;
   for(int counter = (periods-1); counter> 0; counter--){
      double newvalue=iCustom(NULL,TimeFrame,"Vortex Oscillator",14,0,counter);
      if (flag==0){
         if (newvalue>value)
            bearbull=1;
         else
            bearbull=-1;
            
            flag=1;
      }
      else if (bearbull==1){
      
         if (newvalue<value)
            return(0);
           
      }else  if (bearbull==-1){
         if (newvalue>value)
            return(0);
      }
      value=newvalue;  
 
   }
   if ((firstvalue<0 && value>0) || (firstvalue>0 && value<0))
      return (bearbull);      
   return (0);
}
int GetMacdTrend(int TimeFrame, int periods){
   
   int flag=0;
   int bearbull=0;
   double dMACD = iMACD(NULL, TimeFrame, 12, 26, 9, PRICE_CLOSE, MODE_MAIN,  periods);
   double dOldMACD=dMACD;                     
   for(int counter =(periods-1); counter> 0; counter--){
      dMACD=iMACD(NULL, TimeFrame, 12, 26, 9, PRICE_CLOSE,MODE_MAIN,counter);
      if (flag==0){
         if (dMACD>dOldMACD)
            bearbull=1;
         else
            bearbull=-1;
            
            flag=1;
      }
      else if (bearbull==1){
      
         if (dMACD<dOldMACD)
            return(0);
           
      }else  if (bearbull==-1){
         if (dMACD>dOldMACD)
            return(0);
      }
      dOldMACD=dMACD;  
   }
   return (bearbull);      
}

int GetSmaTrend(int TimeFrame, int maPeriods, int trendPeriods){
   
   int flag=0;
   int bearbull=0;
   double ma=iMA(NULL,TimeFrame,maPeriods,0,MODE_SMMA,PRICE_CLOSE,trendPeriods);
   double oldMa=ma;                     
   for(int counter =(trendPeriods-1); counter> 0; counter--){
      ma=iMA(NULL,TimeFrame,maPeriods,0,MODE_SMMA,PRICE_CLOSE,counter);
      if (flag==0){
         if (ma>oldMa)
            bearbull=1;
         else
            bearbull=-1;
            
            flag=1;
      }
      else if (bearbull==1){
      
         if (ma<oldMa)
            return(0);
           
      }else  if (bearbull==-1){
         if (ma>oldMa)
            return(0);
      }
      oldMa=ma;  
   }
   return (bearbull);      
}

int GetEmaTrend(int TimeFrame, int maPeriods, int trendPeriods){
   
   int flag=0;
   int bearbull=0;
   double ma=iMA(NULL,TimeFrame,maPeriods,0,MODE_EMA,PRICE_CLOSE,trendPeriods);
   double oldMa=ma;                     
   for(int counter =(trendPeriods-1); counter> 0; counter--){
      ma=iMA(NULL,TimeFrame,maPeriods,0,MODE_EMA,PRICE_CLOSE,counter);
      if (flag==0){
         if (ma>oldMa)
            bearbull=1;
         else
            bearbull=-1;
            
            flag=1;
      }
      else if (bearbull==1){
      
         if (ma<oldMa)
            return(0);
           
      }else  if (bearbull==-1){
         if (ma>oldMa)
            return(0);
      }
      oldMa=ma;  
   }
   return (bearbull);      
}