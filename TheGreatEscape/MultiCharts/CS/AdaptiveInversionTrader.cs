using System;
using System.Drawing;
using System.Linq;
using PowerLanguage.Function;
using ATCenterProxy.interop;

namespace PowerLanguage.Strategy {
	public class AdaptiveInversionTrader : SignalObject {
		
		
		
        private IOrderMarket m_OrderEntryLE;
		private IOrderMarket m_OrderEntrySE;
        private IOrderMarket m_OrderEntryLC;
		private IOrderMarket m_OrderEntrySC;	
		
		private VariableSeries<Double> m_AtrValue;
		private VariableSeries<Double> m_Brick;
		private VariableSeries<Double> m_Down;
		private VariableSeries<Double> m_Up;
		private VariableSeries<Double> m_BricksDown;
		private VariableSeries<Double> m_BricksUp;
		
		private VariableSeries<Double> m_YesterNet;
		private VariableSeries<Double> m_TodaysNet;

        private Function.ADX m_adx1;
        private VariableSeries<Double> m_adxvalue;
		
		public AdaptiveInversionTrader(object _ctx):base(_ctx){
			K=0.7;
			AdxLim=40;
			AdxLen=14;
			Smooth=10;
			StopLossTicks=40;
			BreakEvenTicks=100;
			MaxLoss=400;
		}
		
		[Input]
        public int AdxLen { get; set; }

			
		
		[Input]
        public double BreakEvenTicks { get; set; }
		
		[Input]
        public double StopLossTicks { get; set; }
		
        [Input]
        public double K { get; set; }
		
        [Input]
        public double AdxLim { get; set; }
		
		
		[Input]
        public double MaxLoss { get; set; }
		
		
        [Input]
        public int Smooth { get; set; }
		
	
		protected override void Create() {
			// create variable objects, function objects, order objects etc.
			m_OrderEntryLE= OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default, "OrderEntryLE", EOrderAction.Buy));
			m_OrderEntryLC= OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default, "OrderEntryLC", EOrderAction.Sell));
			m_OrderEntrySE= OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default, "OrderEntrySE", EOrderAction.SellShort));
			m_OrderEntrySC= OrderCreator.MarketNextBar(new SOrderParameters(Contracts.Default, "OrderEntrySC", EOrderAction.BuyToCover));
			
			m_AtrValue = new VariableSeries<Double>(this);	
			m_Brick = new VariableSeries<Double>(this);	
			m_Down = new VariableSeries<Double>(this);	
			m_Up = new VariableSeries<Double>(this);	
			m_BricksDown = new VariableSeries<Double>(this);	
			m_BricksUp = new VariableSeries<Double>(this);	
			
			m_adx1 = new Function.ADX(this);
            m_adxvalue = new VariableSeries<Double>(this);			
			m_TodaysNet = new VariableSeries<Double>(this);	
			m_YesterNet = new VariableSeries<Double>(this);	
		}

		protected override void StartCalc() {
			m_AtrValue.DefaultValue = 0;
			m_Brick.DefaultValue = 0;
			m_Down.DefaultValue = 0;
			m_Up.DefaultValue = 0;
			m_BricksDown.DefaultValue = 0;
			m_BricksUp.DefaultValue = 0;			
            m_adx1.Length = AdxLen;
            m_adxvalue.DefaultValue = 0;
		}

		protected override void CalcBar(){
			
			CurSpecOrdersMode=ESpecOrdersMode.PerPosition;
			GenerateBreakEven( (Bars.Info.MinMove/Bars.Info.PriceScale)*Bars.Info.BigPointValue*BreakEvenTicks);
			GenerateStopLoss( (Bars.Info.MinMove/Bars.Info.PriceScale)*Bars.Info.BigPointValue*StopLossTicks);			
								   
			bool MaxLossTriggered=false;
			if (Bars.Time.Value.Day>Bars.Time[1].Day){
				m_YesterNet.Value=this.NetProfit;
			}
			m_TodaysNet.Value=NetProfit-m_YesterNet.Value;
			if (m_TodaysNet.Value<(-MaxLoss))MaxLossTriggered=true;
						
			m_AtrValue.Value=Function.AvgTrueRange.AverageTrueRange(this,Smooth);
			m_adxvalue.Value = m_adx1[0];
			
			if (Bars.CurrentBar== 1){
				m_Up.Value=Bars.High.Value;
				m_Down.Value=Bars.Low.Value;
				m_Brick.Value=K*(Bars.High.Value-Bars.Low.Value);
			}else{
				if (Bars.Close.Value>(m_Up[0]+m_Brick[0])){
					if (m_Brick.Value==0)m_BricksUp.Value=0;else m_BricksUp.Value=Math.Floor((Bars.Close.Value-m_Up.Value)/m_Brick.Value)*m_Brick.Value;
					m_Up.Value=m_Up.Value+m_BricksUp.Value;
					m_Brick.Value=K*m_AtrValue.Value;
					m_Down.Value=m_Up.Value-m_Brick.Value;
					m_BricksDown.Value=0;
				}				
				if (Bars.Close.Value<(m_Down[0]-m_Brick[0])){
					if (m_Brick.Value==0)m_BricksDown.Value=0;else m_BricksDown.Value=Math.Floor((m_Down.Value-Bars.Close.Value)/m_Brick.Value)*m_Brick.Value;
					m_Down.Value=m_Down.Value-m_BricksDown.Value;
					m_Brick.Value=K*m_AtrValue.Value;
					m_Up.Value=m_Down.Value+m_Brick.Value;
					m_BricksUp.Value=0;
				}
			}
			if (MaxLossTriggered || PublicFunctions.DoubleGreater(m_adx1[0],AdxLim)){
				m_OrderEntrySC.Send();
				m_OrderEntryLC.Send();
			}else{
            if (m_Up[0]>m_Up[1])
	                m_OrderEntrySE.Send();
            if (m_Down[0]<m_Down[1])
	                m_OrderEntryLE.Send();
			}

		}
	}
}