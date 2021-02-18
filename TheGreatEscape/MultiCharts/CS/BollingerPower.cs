using System;

namespace PowerLanguage.Strategy
{
    public class BollingerPower_1_1 : SignalObject
    {
        private IOrderMarket m_BBandLE;
		private IOrderMarket m_BBandSE;
        private IOrderMarket m_BBandLC;
		private IOrderMarket m_BBandSC;		
        private Function.ADX m_adx1;
        private VariableSeries<Double> m_adxvalue;
		
        public BollingerPower_1_1(object ctx) :
            base(ctx)
        {
            Length = 10;
			AdxLen=14;
			AdxLim=40;
            NumDevsDn = 1;
			BreakEvenTriggerTicks = 250;
			StopLossTicks=60;
        }
        [Input]
        public int AdxLen { get; set; }
        [Input]
        public int AdxLim { get; set; }		
		
        [Input]
        public double StopLossTicks { get; set; }
		

		
        [Input]
        public double BreakEvenTriggerTicks { get; set; }
		
        [Input]
        public int Length { get; set; }

        [Input]
        public double NumDevsDn { get; set; }

        private VariableSeries<double> m_LowerBand;
		private VariableSeries<double> m_UpperBand;

        protected override void Create()
        {
            m_LowerBand = new VariableSeries<Double>(this);
			m_UpperBand = new VariableSeries<Double>(this);
			m_BBandLE= OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default, "BBandLE", EOrderAction.Buy));
			m_BBandLC= OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default, "BBandLC", EOrderAction.Sell));
			m_BBandSE= OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default, "BBandSE", EOrderAction.SellShort));
			m_BBandSC= OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default, "BBandSC", EOrderAction.BuyToCover));
            m_adx1 = new Function.ADX(this);
            m_adxvalue = new VariableSeries<Double>(this);	
			
        }
        protected override void StartCalc(){
            m_adx1.Length = AdxLen;
            m_adxvalue.DefaultValue = 0;
		}
        protected override void CalcBar()
        {

			
			m_adxvalue.Value = m_adx1[0];
			CurSpecOrdersMode=ESpecOrdersMode.PerPosition;
			GenerateBreakEven( (Bars.Info.BigPointValue/Bars.Info.PriceScale)*BreakEvenTriggerTicks);
			GenerateStopLoss( (Bars.Info.BigPointValue/Bars.Info.PriceScale)*StopLossTicks);
			
            m_LowerBand.Value = Bars.Close.BollingerBandCustom(Length, -NumDevsDn);
			m_UpperBand.Value = Bars.Close.BollingerBandCustom(Length, NumDevsDn);

			if (PublicFunctions.DoubleLess(m_adx1[0],AdxLim)){
	            if (Bars.CurrentBar > 1 && Bars.Close.Value>m_UpperBand.Value)
	                m_BBandSE.Send();
	            if (Bars.CurrentBar > 1 && Bars.Close.Value<m_LowerBand.Value )
	                m_BBandLE.Send();		
			}else{
				if (CurrentPosition.Value<0)
					m_BBandSC.Send();
				if (CurrentPosition.Value>0)
					m_BBandLC.Send();				
			}
        }
    }
}