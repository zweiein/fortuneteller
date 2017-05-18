// 
// Copyright (C) 2006, NinjaTrader LLC <ninjatrader@ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//

#region Using declarations
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Xml.Serialization;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
	/// <summary>
	/// Original Pivot Points enhanced file by cvax.
	/// Modified to Murrey Math Indicator by Prtester
	/// Ported From
	/// MetaTrader4 conversion created by CrazyChart
	/// "mailto:newcomer2003@yandex.ru"
	/// Minor visual changes added by Big Mike (www.bigmiketrading.com) 06/22/2009
	/// v1.5 Minor left justify option added by Big Mike 07/01/2009
	/// </summary>
	[Description("Murrey Math Indicator (version 1.5.1) NT7 compatible")]
	[Gui.Design.DisplayName("Murrey Math Indicator")]
	public class MurreyMath : Indicator
	{
		#region Variables
		private	SolidBrush[]	brushes			= { new SolidBrush(Color.Black), new SolidBrush(Color.Black), new SolidBrush(Color.Black), new SolidBrush(Color.Black), new SolidBrush(Color.Black), 
													new SolidBrush(Color.Black), new SolidBrush(Color.Black), new SolidBrush(Color.Black), new SolidBrush(Color.Black), new SolidBrush(Color.Black),
													new SolidBrush(Color.Black), new SolidBrush(Color.Black), new SolidBrush(Color.Black),};
		private	double			currentClose	= 0;
		private DateTime		currentDate		= Cbi.Globals.MinDate;
		private DateTime		currentMonth	= Cbi.Globals.MinDate;
		private DateTime		currentWeek		= Cbi.Globals.MinDate;
		private	double			currentHigh		= double.MinValue;
		private	double			currentLow		= double.MaxValue;
		private Data.PivotRange pivotRangeType	= PivotRange.Daily;
		private bool			labels			= true;
		private bool			textcolorispencolor = false;
		private Color			textcolor		= Color.DimGray;
		private bool			labelsonleft	= false;

		private double sum, v1, v2, fractal;
		private double v45, mml00, mml0, mml1, mml2, mml3, mml4, mml5, mml6, mml7, mml8, mml9, mml98, mml99;
		private double range, octave, mn, mx, price;
		private double finalH, finalL;
		private double x1, x2, x3, x4, x5, x6, y1, y2, y3, y4, y5, y6;
		private int periodtotake = 200;
		
		
		private	StringFormat	stringFormat	= new StringFormat();
		private int				width			= 370;
		private int				rtmargin		= 0;
		private Bars hourlyBars;
		private bool isLoaded = false;
		private bool isInit = false;
		private bool existsHistHourlyData = false;
		private bool CalcPivotHourly = false;
		#endregion

		/// <summary>
		/// This method is used to configure the indicator and is called once before any bar data is loaded.
		/// </summary>
		protected override void Initialize()
		{
				Add(new Plot(Color.Blue,		"Extreme OS (-2/8)"));
				Add(new Plot(Color.Blue,		"Extreme OS (-1/8)"));
				Add(new Plot(Color.Blue,		"Ultimate Support/Resistance (0/8)"));
				Add(new Plot(Color.Blue,		"Weak, Stop and Reverse (1/8)"));
				Add(new Plot(Color.Blue,		"Pivot, Reverse (2/8)"));
				Add(new Plot(Color.Blue,		"Bottom of Trading Range (3/8)"));
				Add(new Plot(Color.Blue,		"Major Support/Resistance Line (4/8)"));
				Add(new Plot(Color.Blue,		"Top of Trading Range (5/8)"));
				Add(new Plot(Color.Blue,		"Pivot, Reverse (6/8)"));
				Add(new Plot(Color.Blue,		"Weak, Stop and Reverse (7/8)"));
				Add(new Plot(Color.Blue,		"Ultimate Support/Resistance (8/8)"));
				Add(new Plot(Color.Blue,		"Extreme OB (+1/8)"));
				Add(new Plot(Color.Blue,		"Extreme OB (+2/8)"));
			
			Plots[0].Pen = new Pen(Color.Blue, 1);
			Plots[1].Pen = new Pen(Color.Blue, 1);
			Plots[2].Pen = new Pen(Color.Blue, 1);
			Plots[3].Pen = new Pen(Color.Blue, 1);
			Plots[4].Pen = new Pen(Color.Blue, 1);
			Plots[5].Pen = new Pen(Color.Blue, 1);
			Plots[6].Pen = new Pen(Color.Blue, 1);
			Plots[7].Pen = new Pen(Color.Blue, 1);
			Plots[8].Pen = new Pen(Color.Blue, 1);
			Plots[9].Pen = new Pen(Color.Blue, 1);
			Plots[10].Pen = new Pen(Color.Blue, 1);
			Plots[11].Pen = new Pen(Color.Blue, 1);
			Plots[12].Pen = new Pen(Color.Blue, 1);
			
			
			AutoScale				= false;
			CalculateOnBarClose		= false;
			Overlay					= true;
			stringFormat.Alignment	= StringAlignment.Far;
			
			ZOrder = -1;
		}

		/// <summary>
		/// Called on each bar update event (incoming tick)
		/// </summary>
		protected override void OnBarUpdate()
		{
			if (Bars == null)
				return; 
			if (!Data.BarsType.GetInstance(Bars.Period.Id).IsIntraday )
				return;
			if (Bars.Period.Id == PeriodType.Day && Bars.Period.Value > 1)
				return;

			if(!isLoaded && !isInit)
			{
			isInit = true;
#if NT7
			hourlyBars= Data.Bars.GetBars(Bars.Instrument, new Period(PeriodType.Minute, 60,MarketDataType.Last), Bars.From, Bars.To, (Session) Bars.Session.Clone(), Data.Bars.SplitAdjust, Data.Bars.DividendAdjust);
#else
			hourlyBars= Data.Bars.GetBars(Bars.Instrument, new Period(PeriodType.Minute, 60), Bars.From, Bars.To, (Session) Bars.Session.Clone(), Data.Bars.SplitAdjust, Data.Bars.DividendAdjust);
#endif
			existsHistHourlyData	= (hourlyBars.Count <= 1) ? false : true;
			isInit = false;
			isLoaded = true;
			}
			IBar hourlyBar;
			
			if (existsHistHourlyData && CalcPivotHourly)
			{
				DateTime intradayBarTime = Time[0].AddHours(-1);
				hourlyBar = hourlyBars.Get(hourlyBars.GetBar(intradayBarTime));
				v1 = hourlyBar.Low;
        		v2 = hourlyBar.High;
				FractalCalc();	
			}
			
			if ((currentDate != Cbi.Globals.MinDate && pivotRangeType == PivotRange.Daily && Time[0].Date != currentDate)
				|| (currentWeek != Cbi.Globals.MinDate && pivotRangeType == PivotRange.Weekly && RoundUpTimeToPeriodTime(Time[0].Date, PivotRange.Weekly) != currentWeek) 
				|| (currentMonth != Cbi.Globals.MinDate && pivotRangeType == PivotRange.Monthly && RoundUpTimeToPeriodTime(Time[0].Date, PivotRange.Monthly) != currentMonth)
				!= CalcPivotHourly) 
			{
				v1 = MIN(Low, periodtotake)[0];
				v2 = MAX(High, periodtotake)[0];
				FractalCalc();	
			}

			if (pivotRangeType == PivotRange.Daily)
				currentDate = Time[0].Date;
			if (pivotRangeType == PivotRange.Weekly)
				currentWeek = RoundUpTimeToPeriodTime(Time[0].Date, PivotRange.Weekly);
			if (pivotRangeType == PivotRange.Monthly)
				currentMonth = RoundUpTimeToPeriodTime(Time[0].Date, PivotRange.Monthly);

			if (finalL != 0)
			{
				N28.Set(mml00);
				N18.Set(mml0);
				N08.Set(mml1);
				P18.Set(mml2);
				P28.Set(mml3);
				P38.Set(mml4);
				P48.Set(mml5);
				P58.Set(mml6);
				P68.Set(mml7);
				P78.Set( mml8 );
				P88.Set(mml9);
				PP18.Set(mml99);
				PP28.Set(mml98);
			}
		}

		#region Properties
		/// <summary>
		/// </summary>
		[Description("Type of period for pivot points.")]
		[Category("Parameters")]
		public Data.PivotRange PivotRangeType 
		{
			get { return pivotRangeType; }
			set { pivotRangeType = value; }
		}

		[Description("Calculate the Pivot Hourly?")]
		[Category("Parameters")]
		[Gui.Design.DisplayName("Calculate the Pivot Hourly?")]
		public bool CALCPIVOTHOURLY 
		{
			get { return CalcPivotHourly; }
			set { CalcPivotHourly = value; }
		}
		
		/// <summary>
		/// </summary>
		[Description("Turn Pivot Labels On/Off.")]
		[Category("Pivot Line Settings")]
		[Gui.Design.DisplayName("01. Display labels on chart?")]
		public bool Labels 
		{
			get { return labels; }
			set { labels = value; }
		}
		[Description("Left justify labels?")]
		[Category("Pivot Line Settings")]
		[Gui.Design.DisplayName("02. Display labels on left?")]
		public bool Labelsonleft
		{
			get { return labelsonleft; }
			set { labelsonleft = value; }
		}
		[Description("Right margin width")]
		[Category("Pivot Line Settings")]
		[Gui.Design.DisplayName("03. Right margin width if lables on right")]
		public int RtMargin
		{
			get { return rtmargin; }
			set { rtmargin = Math.Max(0, value); }
		}
		[Description("Label Color is Pen Color?")]
		[Category("Pivot Line Settings")]
		[Gui.Design.DisplayNameAttribute("04. Label Color is Pen Color?")]
		public bool Textcolorispencolor 
		{
			get { return textcolorispencolor; }
			set { textcolorispencolor = value; }
		}
		/// <summary>
        /// </summary>
        [XmlIgnore()]
        [Description("Color of text if Label Color is false")]
        [Category("Pivot Line Settings")]
        [Gui.Design.DisplayNameAttribute("05. Label Color if False?")]
        public Color TextColor
        {
            get { return textcolor; }
            set { textcolor = value; }
        }

        /// <summary>
        /// </summary>
        [Browsable(false)]
        public string TextColorSerialize
        {
            get { return NinjaTrader.Gui.Design.SerializableColor.ToString(textcolor); }
            set { textcolor = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
        }
		/// <summary>
		/// </summary>
		[Description("# of bars back the Pivot Line will be drawn.")]
		[Category("Pivot Line Settings")]
		[Gui.Design.DisplayName("06. How many bars back to draw lines?")]
		public int Width
		{
			get { return width; }
			set { width = Math.Max(1, value); }
		}
		
		/// <summary>
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public DataSeries N28
		{
			get { return Values[0]; }
		}

		/// <summary>
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public DataSeries N18
		{
			get { return Values[1]; }
		}

		/// <summary>
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public DataSeries N08
		{
			get { return Values[2]; }
		}
	
		/// <summary>
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public DataSeries P18
		{
			get { return Values[3]; }
		}

		/// <summary>
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public DataSeries P28
		{
			get { return Values[4]; }
		}
		
		/// <summary>
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public DataSeries P38
		{
			get { return Values[5]; }
		}
		
		/// <summary>
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public DataSeries P48
		{
			get { return Values[6]; }
		}
		
		/// <summary>
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public DataSeries P58
		{
			get { return Values[7]; }
		}
		
		/// <summary>
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public DataSeries P68
		{
			get { return Values[8]; }
		}
		
		/// <summary>
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public DataSeries P78
		{
			get { return Values[9]; }
		}
		
		/// <summary>
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public DataSeries P88
		{
			get { return Values[10]; }
		}
		
		/// <summary>
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public DataSeries PP18
		{
			get { return Values[11]; }
		}
		
		/// <summary>
		/// </summary>
		[Browsable(false)]
		[XmlIgnore]
		public DataSeries PP28
		{
			get { return Values[12]; }
		}

		#endregion

		#region Miscellaneous
		private void FractalCalc()
		{
			
			 //determine fractal.....
        if(v2 <= 250000 && v2 > 25000) 
          {
            fractal = 100000;
          }
        else 
            if(v2 <= 25000 && v2 > 2500)
              {
                fractal = 10000;
              }
            else 
                if(v2 <= 2500 && v2 > 250)
                  {
                    fractal = 1000;
                  }
                else 
                    if(v2 <= 250 && v2 > 25) 
                      {
                        fractal = 100;
                      }
                    else 
                        if(v2 <= 25 && v2 > 12.5) 
                          {
                            fractal = 12.5;
                          }
                        else 
                            if(v2 <= 12.5 && v2 > 6.25) 
                              {
                                fractal = 12.5;
                              }
                            else 
                                if(v2 <= 6.25 && v2 > 3.125) 
                                  {
                                    fractal = 6.25;
                                  }
                                else 
                                    if(v2 <= 3.125 && v2 > 1.5625) 
                                      {
                                        fractal = 3.125;
                                      }
                                    else 
                                        if(v2 <= 1.5625 && v2 > 0.390625) 
                                          {
                                            fractal = 1.5625;
                                          }
                                        else 
                                            if(v2 <= 0.390625 && v2 > 0) 
                                              {
                                                fractal = 0.1953125;
                                              }
        
					range = (v2 - v1);
        			sum = Math.Floor(Math.Log(fractal / range) / Math.Log(2));
       				octave = fractal*(Math.Pow(0.5, sum));
        			mn = Math.Floor(v1 / octave)*octave;
        //----
        				if((mn + octave) > v2) 
          					{
            					mx = mn + octave; 
          					}
        				else
          					{
            					mx = mn + (2*octave);
          					}
        // calculating xx
        //x2
        				if((v1 >= 3 / 16*(mx - mn) + mn) && (v2 <= 9 / 16*(mx - mn) + mn)) 
          					{
            					x2 = mn + (mx - mn) / 2; 
          					}
        				else
          					{
            					x2 = 0;
          					}
        //x1
        				if((v1 >= mn - (mx - mn) / 8) && (v2 <= 5 / 8*(mx - mn) + mn) && x2 == 0) 
          					{
            					x1 = mn + (mx - mn) / 2; 
          					}
        				else
          					{
            					x1 = 0;
          					}
        //x4
        				if((v1 >= mn + 7*(mx - mn) / 16) && (v2 <= 13 / 16*(mx - mn) + mn)) 
          					{
            					x4 = mn + 3*(mx - mn) / 4; 
          					}
        				else
          					{
            					x4 = 0;
          					}
        //x5
        				if((v1 >= mn + 3*(mx - mn) / 8) && (v2 <= 9 / 8*(mx - mn) + mn) && x4 == 0) 
          					{
            					x5 = mx; 
          					}
        				else
          					{
            					x5 = 0;
          					}
        //x3
        				if((v1 >= mn + (mx - mn) / 8)&& (v2 <= 7 / 8*(mx - mn) + mn) && x1 == 0 && x2 == 0 && 
           						x4 == 0 && x5 == 0) 
          					{
            					x3 = mn + 3*(mx - mn) / 4; 
          					}
        				else
          					{
            					x3 = 0;
          					}
        //x6
        				if((x1 + x2 + x3 + x4 + x5) == 0) 
          					{
            					x6 = mx; 
          					}
        				else
          					{
            					x6 = 0;
          					}
        			finalH = x1 + x2 + x3 + x4 + x5 + x6;
        // calculating yy
        //y1
        				if(x1 > 0) 
          					{
            					y1 = mn; 
          					}
        				else
          					{
            					y1 = 0;
          					}
        //y2
        				if(x2 > 0) 
          					{
           					 	y2 = mn + (mx - mn) / 4; 
          					}
        				else
          					{
            					y2 = 0;
          					}
        //y3
        				if(x3 > 0) 
          					{
            					y3 = mn + (mx - mn) / 4; 
          					}
        				else
          					{
            					y3 = 0;
          					}
        //y4
        				if(x4 > 0) 
          					{
            					y4 = mn + (mx - mn) / 2;
          					}
        				else
          					{
            					y4 = 0;
          					}
        //y5
        				if(x5 > 0) 
          					{
            					y5 = mn + (mx - mn) / 2; 
          					}
        				else
          					{
            					y5 = 0;
          					}
        //y6
        				if((finalH > 0) && (y1 + y2 + y3 + y4 + y5 == 0)) 
          					{
            					y6 = mn; 
          					}
        				else
          					{
            					y6 = 0;
          					}
					
					finalL = y1 + y2 + y3 + y4 + y5 + y6;
        			v45 = (finalH - finalL) / 8;
							
        			mml00 = Bars.Instrument.MasterInstrument.Round2TickSize(finalL - v45*2);  //-2/8
       				mml0 = Bars.Instrument.MasterInstrument.Round2TickSize(finalL - v45);  //-1/8
        			mml1 = Bars.Instrument.MasterInstrument.Round2TickSize(finalL);// 0/8
        			mml2 = Bars.Instrument.MasterInstrument.Round2TickSize(finalL + v45);// 1/8
        			mml3 = Bars.Instrument.MasterInstrument.Round2TickSize(finalL + 2*v45); // 2/8
        			mml4 = Bars.Instrument.MasterInstrument.Round2TickSize(finalL + 3*v45); //  3/8
        			mml5 = Bars.Instrument.MasterInstrument.Round2TickSize(finalL + 4*v45); //  4/8
        			mml6 = Bars.Instrument.MasterInstrument.Round2TickSize(finalL + 5*v45); //  5/8
        			mml7 = Bars.Instrument.MasterInstrument.Round2TickSize(finalL + 6*v45); //  6/8 
        			mml8 = Bars.Instrument.MasterInstrument.Round2TickSize(finalL + 7*v45);// 7/8
        			mml9 = Bars.Instrument.MasterInstrument.Round2TickSize(finalL + 8*v45);// 8/8
        			mml99 = Bars.Instrument.MasterInstrument.Round2TickSize(finalL + 9*v45);// +1/8
        			mml98 = Bars.Instrument.MasterInstrument.Round2TickSize(finalL + 10*v45);// +2/8
			
			
			
		}
			
			
		
		
		
		
		private int GetDayOfWeek(DateTime date)
		{
			DateTime saturday = new DateTime(1776, 7, 4).AddDays(2);
			TimeSpan diff = date.Subtract(saturday);
			return (diff.Days % 7);
		}

		private DateTime RoundUpTimeToPeriodTime(DateTime time, PivotRange pivotRange)
		{
			if (pivotRange == PivotRange.Weekly)
			{
				DateTime periodStart = time.AddDays((6-GetDayOfWeek(time)));
				return periodStart.Date.AddDays(System.Math.Ceiling(System.Math.Ceiling(time.Date.Subtract(periodStart.Date).TotalDays) / 7) * 7).Date;
			}
			else if (pivotRange == PivotRange.Monthly)
			{
				DateTime result = new DateTime(time.Year, time.Month, 1); 
				return result.AddMonths(1).AddDays(-1);
			}
			else
				return time;
		}

		/// <summary>
		/// </summary>
		/// <param name="graphics"></param>
		/// <param name="bounds"></param>
		/// <param name="min"></param>
		/// <param name="max"></param>
		public override void Plot(Graphics graphics, Rectangle bounds, double min, double max)
		{
			if (Bars == null || ChartControl == null)
				return;

			int	barWidth = ChartControl.ChartStyle.GetBarPaintWidth(ChartControl.BarWidth);
			for (int seriesCount = 0; seriesCount < Values.Length; seriesCount++)
			{
				SolidBrush		brush				= brushes[seriesCount];
				DateTime		lastDate			= Cbi.Globals.MinDate;
				DateTime		lastWeek			= Cbi.Globals.MinDate;
				DateTime		lastMonth			= Cbi.Globals.MinDate;
				int				firstX				= -1;
				int				lastX				= -1;
				int				lastY				= -1;
				int				xcounter			= 0;
				SmoothingMode	oldSmoothingMode	= graphics.SmoothingMode;
				GraphicsPath	path				= new GraphicsPath();
				Gui.Chart.Plot	plot				= Plots[seriesCount];
				DataSeries	series				= (DataSeries) Values[seriesCount];
				
				if (brush.Color != plot.Pen.Color && Textcolorispencolor)	
					brush = new SolidBrush(plot.Pen.Color);
				else
					brush = new SolidBrush(TextColor);	

				for (int count = ChartControl.BarsPainted - 1; count >= ChartControl.BarsPainted - Math.Min(ChartControl.BarsPainted, Width); count--)
				{
					
					int idx = ChartControl.LastBarPainted - ChartControl.BarsPainted + 1 + count;
					if (idx - Displacement < 0 || idx - Displacement >= Bars.Count || (!ChartControl.ShowBarsRequired && idx - Displacement < BarsRequired))
						continue;
					else if (!series.IsValidPlot(idx))
						continue;

					if (pivotRangeType == PivotRange.Daily && lastDate == Cbi.Globals.MinDate) 
						lastDate = Bars.Get(idx).Time.Date;
					else if (pivotRangeType == PivotRange.Weekly && lastWeek == Cbi.Globals.MinDate) 
						lastWeek = RoundUpTimeToPeriodTime(Bars.Get(idx).Time.Date, PivotRange.Weekly);
					else if (pivotRangeType == PivotRange.Monthly && lastMonth == Cbi.Globals.MinDate) 
						lastMonth = RoundUpTimeToPeriodTime(Bars.Get(idx).Time.Date, PivotRange.Monthly);
					else if (pivotRangeType == PivotRange.Daily && lastDate != Bars.Get(idx).Time.Date 
							|| pivotRangeType == PivotRange.Weekly && lastWeek != RoundUpTimeToPeriodTime(Bars.Get(idx).Time.Date, PivotRange.Weekly)
							|| pivotRangeType == PivotRange.Monthly && lastMonth != RoundUpTimeToPeriodTime(Bars.Get(idx).Time.Date, PivotRange.Monthly))
						continue;

					double		val = series.Get(idx);
					int			x	= (int) (ChartControl.CanvasRight - ChartControl.BarMarginRight - barWidth / 2
										- (ChartControl.BarsPainted - 1) * ChartControl.BarSpace + count * ChartControl.BarSpace);
					int			y	= (int) ((bounds.Y + bounds.Height) - ((val - min ) / Gui.Chart.ChartControl.MaxMinusMin(max, min)) * bounds.Height);

					if (xcounter == 0)
					{
						firstX	= x;
					}
					if (lastX >= 0)
					{
						if (y != lastY) // Problem here is, that last bar of old day has date of new day
							y = lastY;
						path.AddLine(lastX - plot.Pen.Width / 2, lastY, x - plot.Pen.Width / 2, y);
					}
					lastX	= x + RtMargin;
					lastY	= y;
					
					xcounter++;
				}

				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				graphics.DrawPath(plot.Pen, path);
				graphics.SmoothingMode = oldSmoothingMode;
				if ( labels == true )
				{
					if (!Textcolorispencolor)
						brush = new SolidBrush(TextColor);
					
					if (pivotRangeType == PivotRange.Daily)
					{
						if (!Labelsonleft)
							graphics.DrawString(plot.Name, ChartControl.Font, brush, RtMargin + firstX, lastY - ChartControl.Font.GetHeight() - 2, stringFormat);			//align label right
						else
						{
							stringFormat.Alignment	= StringAlignment.Near;	
							graphics.DrawString(plot.Name, ChartControl.Font, brush, lastX, lastY - ChartControl.Font.GetHeight() - 2, stringFormat);		//align label left
						}
					}
					else if (pivotRangeType == PivotRange.Weekly)
					{
						if (!Labelsonleft)
							graphics.DrawString("Weekly " + plot.Name, ChartControl.Font, brush, RtMargin + firstX, lastY - ChartControl.Font.GetHeight() - 2, stringFormat);			//align label right
						else
						{
							stringFormat.Alignment	= StringAlignment.Near;			
							graphics.DrawString("Weekly " + plot.Name, ChartControl.Font, brush, lastX, lastY - ChartControl.Font.GetHeight() - 2, stringFormat);		//align label left
						}
					}
					else
					{
						if (!Labelsonleft)
							graphics.DrawString("Monthly " + plot.Name, ChartControl.Font, brush, RtMargin + firstX, lastY - ChartControl.Font.GetHeight() - 2, stringFormat);			//align label right
						else
						{
							stringFormat.Alignment	= StringAlignment.Near;			
							graphics.DrawString("Monthly " + plot.Name, ChartControl.Font, brush, lastX, lastY - ChartControl.Font.GetHeight() - 2, stringFormat);		//align label left
						}
					}
				}
				else
				{
					if (!Labelsonleft)
						graphics.DrawString("", ChartControl.Font, brush, RtMargin + firstX, lastY - ChartControl.Font.GetHeight(), stringFormat);			//align label right
					else
					{
						stringFormat.Alignment	= StringAlignment.Near;			
						graphics.DrawString("", ChartControl.Font, brush, lastX, lastY - ChartControl.Font.GetHeight() - 2, stringFormat);		//align label left
					}

				}
			}
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    public partial class Indicator : IndicatorBase
    {
        private MurreyMath[] cacheMurreyMath = null;

        private static MurreyMath checkMurreyMath = new MurreyMath();

        /// <summary>
        /// Murrey Math Indicator (version 1.5.1) NT7 compatible
        /// </summary>
        /// <returns></returns>
        public MurreyMath MurreyMath(bool cALCPIVOTHOURLY, Data.PivotRange pivotRangeType)
        {
            return MurreyMath(Input, cALCPIVOTHOURLY, pivotRangeType);
        }

        /// <summary>
        /// Murrey Math Indicator (version 1.5.1) NT7 compatible
        /// </summary>
        /// <returns></returns>
        public MurreyMath MurreyMath(Data.IDataSeries input, bool cALCPIVOTHOURLY, Data.PivotRange pivotRangeType)
        {
            if (cacheMurreyMath != null)
                for (int idx = 0; idx < cacheMurreyMath.Length; idx++)
                    if (cacheMurreyMath[idx].CALCPIVOTHOURLY == cALCPIVOTHOURLY && cacheMurreyMath[idx].PivotRangeType == pivotRangeType && cacheMurreyMath[idx].EqualsInput(input))
                        return cacheMurreyMath[idx];

            lock (checkMurreyMath)
            {
                checkMurreyMath.CALCPIVOTHOURLY = cALCPIVOTHOURLY;
                cALCPIVOTHOURLY = checkMurreyMath.CALCPIVOTHOURLY;
                checkMurreyMath.PivotRangeType = pivotRangeType;
                pivotRangeType = checkMurreyMath.PivotRangeType;

                if (cacheMurreyMath != null)
                    for (int idx = 0; idx < cacheMurreyMath.Length; idx++)
                        if (cacheMurreyMath[idx].CALCPIVOTHOURLY == cALCPIVOTHOURLY && cacheMurreyMath[idx].PivotRangeType == pivotRangeType && cacheMurreyMath[idx].EqualsInput(input))
                            return cacheMurreyMath[idx];

                MurreyMath indicator = new MurreyMath();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.CALCPIVOTHOURLY = cALCPIVOTHOURLY;
                indicator.PivotRangeType = pivotRangeType;
                Indicators.Add(indicator);
                indicator.SetUp();

                MurreyMath[] tmp = new MurreyMath[cacheMurreyMath == null ? 1 : cacheMurreyMath.Length + 1];
                if (cacheMurreyMath != null)
                    cacheMurreyMath.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheMurreyMath = tmp;
                return indicator;
            }
        }
    }
}

// This namespace holds all market analyzer column definitions and is required. Do not change it.
namespace NinjaTrader.MarketAnalyzer
{
    public partial class Column : ColumnBase
    {
        /// <summary>
        /// Murrey Math Indicator (version 1.5.1) NT7 compatible
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.MurreyMath MurreyMath(bool cALCPIVOTHOURLY, Data.PivotRange pivotRangeType)
        {
            return _indicator.MurreyMath(Input, cALCPIVOTHOURLY, pivotRangeType);
        }

        /// <summary>
        /// Murrey Math Indicator (version 1.5.1) NT7 compatible
        /// </summary>
        /// <returns></returns>
        public Indicator.MurreyMath MurreyMath(Data.IDataSeries input, bool cALCPIVOTHOURLY, Data.PivotRange pivotRangeType)
        {
            return _indicator.MurreyMath(input, cALCPIVOTHOURLY, pivotRangeType);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// Murrey Math Indicator (version 1.5.1) NT7 compatible
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.MurreyMath MurreyMath(bool cALCPIVOTHOURLY, Data.PivotRange pivotRangeType)
        {
            return _indicator.MurreyMath(Input, cALCPIVOTHOURLY, pivotRangeType);
        }

        /// <summary>
        /// Murrey Math Indicator (version 1.5.1) NT7 compatible
        /// </summary>
        /// <returns></returns>
        public Indicator.MurreyMath MurreyMath(Data.IDataSeries input, bool cALCPIVOTHOURLY, Data.PivotRange pivotRangeType)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.MurreyMath(input, cALCPIVOTHOURLY, pivotRangeType);
        }
    }
}
#endregion
