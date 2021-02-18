//+------------------------------------------------------------------+
//|                                            BlackSwan_Awesome.mq4 |
//|                      Copyright © 2010, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"


double ADX=0;
double ADX_PLUS=0;
double ADX_MINUS=0;

double ADX_PREV=0;
double ADX_PLUS_PREV=0;
double ADX_MINUS_PREV=0;

void UpdateADX(){
   ADX=iADX(NULL,0,14,PRICE_HIGH,MODE_MAIN,0);
   ADX_PLUS=iADX(NULL,0,14,PRICE_HIGH,MODE_PLUSDI,0);
   ADX_MINUS=iADX(NULL,0,14,PRICE_HIGH,MODE_MINUSDI,0);   
   
   ADX_PREV=iADX(NULL,0,14,PRICE_HIGH,MODE_MAIN,2);
   ADX_PLUS_PREV=iADX(NULL,0,14,PRICE_HIGH,MODE_PLUSDI,2);
   ADX_MINUS_PREV=iADX(NULL,0,14,PRICE_HIGH,MODE_MINUSDI,2);     
}


int DetectAdxTrend(){
   
   if (ADX_PREV<ADX_PLUS_PREV && ADX_PREV<ADX_MINUS_PREV && (ADX>ADX_PLUS || ADX>ADX_MINUS)){
   
      if (ADX_PLUS>ADX_MINUS)
         return (1);
      if (ADX_PLUS<ADX_MINUS)
         return (-1);
   }
   return (0);
}

