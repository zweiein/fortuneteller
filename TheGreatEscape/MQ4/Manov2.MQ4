//+------------------------------------------------------------------+
//|                                                        Manov.mq4 |
//|           Ëåâåðèäæ            Copyright © Evgeniy Trofimov, 2010 |
//|                                  http://forum.mql4.com/ru/16793/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © Evgeniy Trofimov, 2010"
#property link      "http://forum.mql4.com/ru/16793/"
extern int MagicNumber = 363111459; // Èäåíòèôèêàòîð ñîâåòíèêà (Ìîé ICQ)
extern int BeginStep = 1000; //Ðàññòîÿíèå îò ïåðâîé ïîçèöèè äî âòîðîé
extern double StepMultiplier = 1.5; //Äåëèòåëü øàãà ïîñëåäóþùèõ ïîçèöèé
extern double NeedProfit = 600.0; //Îæèäàåìàÿ ïðèáûëü â âàëþòå äåïîçèòà
extern double MaxLoss = 14000.0; //Ìàêñèìàëüíûé óáûòîê â âàëþòå äåïîçèòà (åñëè áîëüøå 100000, òî ÑòîïËîññ íå ñòàâèòñÿ)
extern int MaxCount = 20; //Ìàêñèìàëüíî äîïóñòèìîå êîëè÷åñòâî ñäåëîê
extern double Lot = 0.2; //Ëîò îòêðûâàþùåéñÿ ïîçèöèè
extern bool   OnlyOneSide = true; //Îòêðûâàòü ïîçèöèè òîëüêî â ñòîðîíó óáûòêà
int CurrentDist=0, CurrentType=-1;
int Slippage = 30;
string Name = "Manov";
double LimitOpen = 0.0;
//+------------------------------------------------------------------+
void start() {
   for(int i=0; i<2; i++){
      if(SignalClose(Symbol(), i)){
         CloseAllPozitions(Symbol(), MagicNumber, i);
         return;
      }
   }//Next i
   int Sygnal = SignalOpen(Symbol());
   if(Sygnal==OP_BUY){
      if(OrderSend(Symbol(), OP_BUY, Lot, MarketInfo(Symbol(),MODE_ASK), Slippage, 0, 0, Name, MagicNumber, 0, Blue)==-1){
         Print("Ñòàâêà íå îòêðûâàåòñÿ ïî ïðè÷èíå îøèáêè ¹ ",GetLastError());
      }
   } else if(Sygnal==OP_SELL){
      if(OrderSend(Symbol(), OP_SELL, Lot, MarketInfo(Symbol(),MODE_BID), Slippage, 0, 0, Name, MagicNumber, 0, Red)==-1){
         Print("Ñòàâêà íå îòêðûâàåòñÿ ïî ïðè÷èíå îøèáêè ¹ ",GetLastError());
      }
   }
   //Ìîäèôèêàöèÿ îðäåðîâ
   double TP = ProfitPrice(Symbol(), OP_BUY, MagicNumber, NeedProfit);
   double SL = ProfitPrice(Symbol(), OP_BUY, MagicNumber, -MaxLoss);
   if(MaxLoss>100000) SL=0;
   CorrectOrders(Symbol(), MagicNumber, OP_BUY, SL, TP);
   TP = ProfitPrice(Symbol(), OP_SELL, MagicNumber, NeedProfit);
   SL = ProfitPrice(Symbol(), OP_SELL, MagicNumber, -MaxLoss);
   if(MaxLoss>100000) SL=0;
   CorrectOrders(Symbol(), MagicNumber, OP_SELL, SL, TP);
   return;
}//start()
//+------------------------------------------------------------------+
void CorrectOrders(string fSymbol, int fMagic, int fType, double SL, double TP){
   int total = OrdersTotal();
   for (int i = 0; i < total; i++) {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if (OrderSymbol() == fSymbol || fSymbol=="") {
         if(OrderMagicNumber() == fMagic || fMagic == 0) {
            if(OrderType() == fType || fType == -1){
               
               Modify(OrderTicket(), SL, TP);
            }
         }
      }
   } //Next i
}//CorrectOrders()
//+------------------------------------------------------------------+
int SignalOpen(string fSymbol, int fType=-1) {
   //Ôóíêöèÿ âîçâðàùàåò ñèãíàë íà îòêðûòèå ïîçèöèè:
   // 0 - ïîêóïàòü
   // 1 - ïðîäàâàòü
   //-1 - íå îòêðûâàòü ïîçèöèþ
   int Cnt=CountTrades(MagicNumber, fSymbol, fType);
   if(Cnt==0){
      int LT=LastTrade(fSymbol);
      if(LT==OP_BUY){
         return(OP_SELL);
      }else if(LT==OP_SELL){
         return(OP_BUY);
      }
      double CurrentOpen=iOpen(fSymbol,PERIOD_D1,0);
      double LastOpen=iOpen(fSymbol,PERIOD_D1,1);
      if(CurrentOpen>=LastOpen) {
         return(OP_BUY);
      } else {
         return(OP_SELL);
      }
   } else {
      if(Cnt>=MaxCount) return(-1);
      double CurrentPrice=(MarketInfo(fSymbol, MODE_ASK)+MarketInfo(fSymbol, MODE_BID))/2;
      int MinDist = FoundMinDistance(MagicNumber, fSymbol, fType);
      int NeedDist = MathRound(MinDist/StepMultiplier);
      if(NeedDist<MarketInfo(fSymbol, MODE_SPREAD)) NeedDist=MarketInfo(fSymbol, MODE_SPREAD);
      if(CurrentDist>NeedDist){
         if(OnlyOneSide){
            if(CurrentType==OP_BUY){
               if(LimitOpen>CurrentPrice){
                  Print("Type: BUY, LimitOpen = "+DoubleToStr(LimitOpen,MarketInfo(fSymbol, MODE_DIGITS))+", CurrentDist = ", CurrentDist, ", NeedDist = ", NeedDist);
                  return(OP_BUY);
               }
            }else if(CurrentType==OP_SELL){
               if(LimitOpen<CurrentPrice){
                  Print("Type: SELL, LimitOpen = "+DoubleToStr(LimitOpen,MarketInfo(fSymbol, MODE_DIGITS))+", CurrentDist = ", CurrentDist, ", NeedDist = ", NeedDist);
                  return(OP_SELL);
               }
            }
         }else{
            return(CurrentType);
         }
      }
   }
   return(-1);
}//SignalOpen()
//+------------------------------------------------------------------+
bool SignalClose(string fSymbol, int fType=-1) {
   if(ProfitFilter(MagicNumber, fSymbol, fType)>NeedProfit) {
      Print("Äîñòèãíóòà ïðèáûëü "+DoubleToStr(ProfitFilter(MagicNumber, fSymbol, fType),2));
      return(true);
   }
   return(false);
}//SignalClose()
//+------------------------------------------------------------------+
double ProfitFilter(int fMagic=0, string fSymbol="", int fType=-1){
   //Ôóíêöèÿ âîçâðàùàåò ïðèáûëü â âàëþòå äåïîçèòà ñî ñäåëîê, îòêðûòûõ â äàííûé ìîìåíò, ñîãëàñòíî ôèëüòðó
   double Profit=0.0;
   int total = OrdersTotal();
   for (int i = 0; i < total; i++) {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if (OrderSymbol() == fSymbol || fSymbol=="") {
         if(OrderMagicNumber() == fMagic || fMagic == 0) {
            if(OrderType() == fType || fType == -1){
               Profit=Profit+OrderProfit();
            }
         }
      }
   }
   return(Profit);
}//ProfitFilter()
//+------------------------------------------------------------------+
int CountTrades(int fMagic=0, string fSymbol="", int fType=-1){
   //Ôóíêöèÿ âîçâðàùàåò êîëè÷åñòâî ñäåëîê, îòêðûòûõ â äàííûé ìîìåíò, ñîãëàñòíî ôèëüòðó
   int Count=0;
   int total = OrdersTotal();
   for (int i = 0; i < total; i++) {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if (OrderSymbol() == fSymbol || fSymbol=="") {
         if(OrderMagicNumber() == fMagic || fMagic == 0) {
            if(OrderType() == fType || fType == -1){
               Count++;
            }
         }
      }
   }
   return(Count);
}//CountTrades()
//+------------------------------------------------------------------+
int FoundMinDistance(int fMagic=0, string fSymbol="", int fType=-1) {
   //Ïîèñê ìèíèìàëüíîé äèñòàíöèè ìåæäó îòêðûòûìè ïîçèöèÿìè â ïóíêòàõ
   double CurrentPrice=(MarketInfo(fSymbol, MODE_ASK)+MarketInfo(fSymbol, MODE_BID))/2;
   LimitOpen = 0.0;
   int MinDist = 100000;
   int Count = 0;
   CurrentDist = 100000;
   CurrentType = -1;
   if(fSymbol=="") fSymbol="EURUSD";
   double ArrOpen[];
   int total = OrdersTotal();
   for (int i = 0; i < total; i++) {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if (OrderSymbol() == fSymbol || fSymbol=="") {
         if(OrderMagicNumber() == fMagic || fMagic == 0) {
            if(OrderType() == fType || fType == -1){
               if(CurrentDist>MathRound(MathAbs(CurrentPrice-OrderOpenPrice())/MarketInfo(fSymbol,MODE_POINT))){
                  CurrentDist=MathRound(MathAbs(CurrentPrice-OrderOpenPrice())/MarketInfo(fSymbol,MODE_POINT));
                  CurrentType=OrderType();
               }
               Count++;
               ArrayResize(ArrOpen,Count);
               ArrOpen[Count-1]=OrderOpenPrice();
               LimitOpen = OrderOpenPrice();
            }
         }
      }
   }//Next i
   if(Count==1) return(MathRound(BeginStep*StepMultiplier));
   for (i = 0; i < Count-1; i++){
      if(CurrentType==OP_BUY){
         if(LimitOpen>ArrOpen[i]){
            LimitOpen=ArrOpen[i]; //Öåíà îòêðûòèÿ ñàìîé íèæíåé ñäåëêè
         }
      }else if(CurrentType==OP_SELL){
         if(LimitOpen<ArrOpen[i]){
            LimitOpen=ArrOpen[i]; //Öåíà îòêðûòèÿ ñàìîé âåðõíåé ñäåëêè
         }
      }
      for(int j = i+1; j<Count; j++){
         if(MinDist>MathRound(MathAbs(ArrOpen[i]-ArrOpen[j])/MarketInfo(fSymbol,MODE_POINT))){
            MinDist=MathRound(MathAbs(ArrOpen[i]-ArrOpen[j])/MarketInfo(fSymbol,MODE_POINT));
         }
      }//Next j
   }//Next i
   return(MinDist);
}//FoundMinDistance()
//+------------------------------------------------------------------+
int CloseAllPozitions(string fSymbol="", int fMagic=0, int fType=-1, int attempts=10) {
   int LE=135, j=0;
   double PriceClose;
   if(IsTradeAllowed()) {
      int  total = OrdersTotal();
      for (int i = total-1; i >= 0; i--) {
         OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
         if(OrderSymbol()==fSymbol || fSymbol=="") {
            if(OrderMagicNumber()==fMagic || fMagic==0) {
               if(OrderType()==fType||fType==-1) {
                  LE=135;
                  j=0;
                  while(LE>134 && LE<139) {
                     if(OrderType()==OP_BUY) {
                        PriceClose=MarketInfo(OrderSymbol(),MODE_BID);
                     } else if(OrderType()==OP_SELL) {
                        PriceClose=MarketInfo(OrderSymbol(),MODE_ASK);
                     }
                     OrderClose(OrderTicket(),OrderLots(),PriceClose,Slippage);
                     LE = GetLastError();
                     if(LE>0) Print("ÑòàÔÔêà íå çàêðûâàåòñÿ ïî ïðè÷èíå îøèáêè ¹ ",LE);
                     j++;
                     if(j>=attempts) break;
                  }
               }
            }
         } // Åñëè ñâîé
      } // Next i
   } // Ñîâåòíèêó ìîæíî òîðãîâàòü
   return(0);
}//CloseAllPozitions()
//+------------------------------------------------------------------+
double ProfitPrice(string fSymbol, int fType, int fMagic=0, double MyProfit=0.0){
   //Ôóíêöèÿ âîçâðàùàåò öåíó, íà êîòîðóþ íåîáõîäèìî óñòàíîâèòü óðîâåíü TakeProfit, ÷òîáû ïîëó÷èòü ïðèáûëü MyProfit
   double SummPrice=0.0, SummLots=0.0, Formula=0.0;
   int k;
   int total = OrdersTotal();
   for (int i = total-1; i >= 0; i--) {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol()==fSymbol) {
         if(OrderMagicNumber()==fMagic || fMagic==0) {
            if(OrderType()==fType) {
               k++;
               SummLots=SummLots+OrderLots();
               SummPrice=SummPrice+OrderOpenPrice()*OrderLots();
            }
         }
      }
   }//Next i  
   
   if(k>0){
      if(fType==OP_BUY){
         Formula = SummPrice/SummLots + 
         MyProfit * MarketInfo(fSymbol, MODE_POINT) / 
         (MarketInfo(fSymbol, MODE_TICKVALUE) * SummLots) +
         MarketInfo(fSymbol, MODE_SPREAD) * MarketInfo(fSymbol, MODE_POINT);
         //LotsBuy = SummLots;
      } else {
         Formula = SummPrice/SummLots - 
         MyProfit * MarketInfo(fSymbol, MODE_POINT) / 
         (MarketInfo(fSymbol, MODE_TICKVALUE) * SummLots) -
         MarketInfo(fSymbol, MODE_SPREAD) * MarketInfo(fSymbol, MODE_POINT);
         //LotsSell = SummLots;         
      }
   }
   return(Formula);
}//ProfitPrice()
//+------------------------------------------------------------------+
int Modify(int ticket, double sl = 0.0, double tp = 0.0) {
   double stoplimit = MarketInfo(Symbol(),MODE_STOPLEVEL)*Point;
   if(OrderSelect(ticket, SELECT_BY_TICKET)) {
      sl=NormalizeDouble(sl,Digits);
      tp=NormalizeDouble(tp,Digits);
      if(sl==NormalizeDouble(OrderStopLoss(),Digits) && tp==NormalizeDouble(OrderTakeProfit(),Digits)) return(0);
      if(OrderType()==OP_BUY) {
         if(sl>0) {
            if(OrderStopLoss()>0)
               if(OrderStopLoss()+Slippage*Point>=sl) return(0);
            if(Bid-sl<stoplimit) {
               Print("Modify: Ñëèøêîì áëèçêèé ñòîï (",(Bid-sl)/Point," ïóíêòîâ). Òðåáóåòñÿ íå ìåíåå: ", stoplimit/Point);
               return(0);
            }
         }
         if(tp>0) if(tp-Ask<stoplimit) {
            Print("Modify: Ñëèøêîì áëèçêèé ïðîôèò (",(tp-Ask)/Point," ïóíêòîâ). Òðåáóåòñÿ íå ìåíåå: ", stoplimit/Point);
            return(0);
         }
      } else if(OrderType()==OP_SELL) {
         if(sl>0) {
            if(OrderStopLoss()>0)
               if(OrderStopLoss()-Slippage*Point<=sl) return(0);
            if(sl-Ask<stoplimit) {
               Print("Modify: Ñëèøêîì áëèçêèé ñòîï (",(sl-Ask)/Point," ïóíêòîâ). Òðåáóåòñÿ íå ìåíåå: ", stoplimit/Point);
               return(0);
            }
         }
         if(tp>0) if(Bid-tp<stoplimit) {
            Print("Modify: Ñëèøêîì áëèçêèé ïðîôèò (",(Bid-tp)/Point," ïóíêòîâ). Òðåáóåòñÿ íå ìåíåå: ", stoplimit/Point);
            return(0);
         }
      }
      if(sl>0 && tp>0) {
         if(OrderModify(ticket,OrderOpenPrice(), sl, tp, 0)==FALSE) Print("Îøèáêà OrderModify(): ",GetLastError());
      } else if(sl>0) {
         if(OrderModify(ticket,OrderOpenPrice(), sl, OrderTakeProfit(), 0)==FALSE) Print("Îøèáêà OrderModify(): ",GetLastError());
      } else if(tp>0) {
         if(OrderModify(ticket,OrderOpenPrice(), OrderStopLoss(), tp, 0)==FALSE) Print("Îøèáêà OrderModify(): ",GetLastError());
      }
   }
   return(0);
}//Modify()
//+------------------------------------------------------------------+
int LastTrade(string fSymbol){
   int total=OrdersHistoryTotal()-1;
   for(int i=total; i>-1; i--){
      OrderSelect(i, SELECT_BY_POS, MODE_HISTORY);
      if(OrderSymbol()==fSymbol){
         if(OrderType()<2){
            return(OrderType());
         }
      }
   }
   return(-1);
}//LastTrade()
//+------------------------------------------------------------------+


