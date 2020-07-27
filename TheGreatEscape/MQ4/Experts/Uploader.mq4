//+------------------------------------------------------------------+
//|                                                     Uploader.mq4 |
//|                        Copyright 2020, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict

#include <Mql4Book\Trade.mqh>
CTrade Trade;
CCount Count;

#include <Mql4Book\Indicators.mqh>

#include <Mql4Book\Timer.mqh>
CNewBar NewBar;

#include <Mql4Book\TrailingStop.mqh>

#include <Mql4Book\MoneyManagement.mqh>

extern string url="http://127.0.0.1/api/mt4trades/";



double CalcPipProf(){

int point_compat = 1;
if(Digits == 3 || Digits == 5) point_compat = 10;

double DiffPips = MathAbs((NormalizeDouble(((OrderClosePrice() - OrderOpenPrice())/MarketInfo(Symbol(),MODE_POINT)),MarketInfo(Symbol(),MODE_DIGITS)))/point_compat);
 if ((OrderType()==OP_SELL && OrderClosePrice()>OrderOpenPrice()) ||
      (OrderType()==OP_BUY && OrderClosePrice()<OrderOpenPrice())) return -DiffPips;
 return DiffPips;
}

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- create timer
   EventSetTimer(60);
   
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- destroy timer
   EventKillTimer();
   
  }
  





string SendResquest( string strJsonText = "", string headers = "", int timeout=0)
{
    //uchar resultDataCharArray[];
   // uchar resultHeader[];
   // string str;
    
    
     uchar jsonData[];
   StringToCharArray(strJsonText, jsonData, 0, StringLen(strJsonText));
  //  uchar bodyDataCharArray[];
  //  ArrayResize(bodyDataCharArray, StringToCharArray(strJsonText, bodyDataCharArray)-1);

 
       char serverResult[];
   string serverHeaders;
   // Print(url);
   // Print(headers);
  //  int  response=WebRequest("POST",url,"","",5000,jsonData, ArraySize(jsonData),serverResult,serverHeaders);
    int response = WebRequest("POST", url, headers, 5000, jsonData, serverResult, serverHeaders);
   //  Print("Web request result: ", response, ", error: #", (response == -1 ? GetLastError() : 0));
    string result = CharArrayToString(serverResult); 
    Print(result);
    if(response == 200 || response == 201)
        return result;
   // Print("Error when trying to call API : ", response);
    return "";
}

string GetTrades(){

   string headers="Content-Type: application/json\r\nDataServiceVersion: 3.0\r\n";
   string trades="[";
   
   string order_symbol="";
   double point_value;
   double balance=0;
   int x,trades_total=OrdersHistoryTotal();

   for(int i=0; i<trades_total; i++)
     {
         if(!OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)) continue;
         int type=OrderType();
      //---- market orders only
         if(type!=OP_BUY && type!=OP_SELL) continue;
            //---- calculate profit in points
         if(order_symbol!=OrderSymbol())
         {
            order_symbol=OrderSymbol();
            point_value=MarketInfo(order_symbol,MODE_POINT);
         }
         
         if(point_value==0) continue;
         double open_price=OrderOpenPrice();
         double close_price=OrderClosePrice();
         double profit=(close_price-open_price)/point_value;
         if(type==OP_SELL) profit=-profit;
         balance+=profit;
         
         double  trProf=  OrderProfit()+OrderCommission()+OrderSwap();
       
         
         
         //---- output trade line
         string command="sell";
         if(type==OP_BUY) command="buy";
   
         string optime= TimeToStr(OrderOpenTime(),TIME_DATE|TIME_SECONDS );
         string cltime= TimeToStr(OrderCloseTime(),TIME_DATE|TIME_SECONDS );
         StringReplace(optime, ".", "-");
         StringReplace(cltime, ".", "-");
         string comment=OrderComment();
         if (comment==NULL || comment=="") comment="-";
        string JSON_string = StringConcatenate( "{",                                                    // **** MQL4 can concat max 63 items
                                              "\"ticket\":", OrderTicket(), ",",
                                              "\"symbol\":\"", OrderSymbol(), "\",",
                                              "\"magic_number\":", OrderMagicNumber(), ",",
                                              "\"open_time\":\"", optime , "\",",
                                              "\"buy_sell\":\"",  command , "\",",
                                              "\"quantity\":",OrderLots() , ",",
                                              "\"open_price\":",  OrderOpenPrice() , ",",
                                              "\"order_sl\":",  OrderStopLoss() , ",",
                                              "\"order_tp\":",OrderTakeProfit() , ",",
                                              "\"close_time\":\"", cltime , "\",",
                                              "\"close_price\":", OrderClosePrice() , ",",
                                              "\"profit\":", NormalizeDouble(profit,3) , ",",
                                              "\"balance\":", NormalizeDouble(CalcPipProf(),3) , ",",
                                              "\"comment\":\"",comment, "\"",
                                              "}"
                                              );
                                             
          trades+=JSON_string;   
          SendResquest(JSON_string,headers);
         
         if ((i+1)<trades_total) trades=trades+",";            
     //    break;                   
   }                                          
  return trades + "]";
}  
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   
   // Check for bar open
   bool newBar = true;
   int barShift = 0;
   

    newBar = NewBar.CheckNewBar(_Symbol,_Period);
    barShift = 1;
   string headers="Content-Type: application/json\r\nDataServiceVersion: 3.0\r\n";
    
   if(newBar == true)
   {
    //  string terminal_data_path=TerminalInfoString(TERMINAL_DATA_PATH);
    //  string filename=terminal_data_path+"\\MQL4\\Files\\"+"tradehist.txt";
    //  Print("tradehist.txt");
     // int  handle=FileOpen("tradehist.txt",FILE_WRITE|FILE_TXT);
      string tradeRecord=GetTrades();
     // Print(tradeRecord);
     
     // FileWrite(handle,tradeRecord);
     // FileClose(handle);
   
   }
    
   //string response = SendResquest("POST", "GetPrediction", "[4, 7]", "Content-Type: application/json", 5000);
   
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
