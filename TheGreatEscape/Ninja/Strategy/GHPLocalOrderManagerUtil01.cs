#region Using declarations
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using System.Windows.Forms;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Indicator;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Strategy;
#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    /// <summary>
    /// [NJAMC] 2012-04-22: NJAMC initial release (Utilities)
	/// 	DO NOT SELL THIS CODE OR DISTRIBUTE OUTSIDE OF BMT's Elite Forum Section
	///
	/// 						
	/// 
	/// </summary>
	public partial class LocalOrderManager 
	{
		string REV_UTILITIES01="01a";
		
		private System.Windows.Forms.ToolStrip 			m_LOMToolStrip=null;
		private System.Windows.Forms.ToolStripButton[] 	m_LOMToolStipButton=new System.Windows.Forms.ToolStripButton[20];
		private string[] 								m_LOMToolStipButtonText;

#region SaveScreenShop
		public string SaveScreenShot(bool m_FullScreen)
		{
			Bitmap bmpScreenshot;
       		Graphics gfxScreenshot;

			Point m_Point=new Point(0,0);
			string filename = m_Strat.Instrument.FullName + m_Strat.Time[0].ToShortDateString().Replace('/',' ') + m_Strat.Time[0].ToShortTimeString().Replace(':',' ');
			string path = @"\Users\Public\Documents\" + filename;

			if (m_FullScreen)
			{
				bmpScreenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height, PixelFormat.Format32bppArgb);

				// Create a graphics object from the bitmap
				gfxScreenshot = Graphics.FromImage(bmpScreenshot);

				// Take the screenshot from the upper left corner to the right bottom corner
				gfxScreenshot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, Screen.PrimaryScreen.Bounds.Size, CopyPixelOperation.SourceCopy);

			}
			else
			{
				m_Strat.ChartControl.AlwaysOnTop=true;
				m_Strat.ChartControl.Update();
				
				// Set the bitmap object to the size of the screen
				bmpScreenshot = new Bitmap(m_Strat.ChartControl.Width,m_Strat.ChartControl.Height, PixelFormat.Format32bppArgb);
				// Create a graphics object from the bitmap
				gfxScreenshot = Graphics.FromImage(bmpScreenshot);

				// Take the screenshot from the upper left corner to the right bottom corner
				gfxScreenshot.CopyFromScreen(m_Strat.ChartControl.PointToScreen(m_Point).X, m_Strat.ChartControl.PointToScreen(m_Point).Y, 0, 0, m_Strat.ChartControl.Size, CopyPixelOperation.SourceCopy);
				m_Strat.ChartControl.AlwaysOnTop=false;
			}
				
			// Save the screenshot to the specified path that the user has chosen
			bmpScreenshot.Save(path + ".png", ImageFormat.Png);
			
			return path + ".png";
		}
#endregion
		
#region ToolBar Tools		
		public bool ToolBarStartUp()
		{
			bool m_Return=true;
			int i;

			m_LOMToolStipButtonText=new string[]  {"&Enter","E&xit","&ABORT"};
			
            System.Windows.Forms.Control[] coll = m_Strat.ChartControl.Controls.Find("tsrTool",false);
			
			if(coll.Length > 0) 
			{
				m_LOMToolStrip = (System.Windows.Forms.ToolStrip)coll[0];

				for (i=0;i<m_LOMToolStipButtonText.Length;i++)
				{
					string m_S=i.ToString()+"/"+m_LOMToolStipButtonText[i];
					m_LOMToolStipButton[i] = new System.Windows.Forms.ToolStripButton(m_LOMToolStipButtonText[i]);
					//m_LOMToolStipButton[i].Click += ToolBarEventProcessor; 
					m_LOMToolStipButton[i].Name = m_LOMToolStipButtonText[i]; 
					m_LOMToolStipButton[i].Text = m_LOMToolStipButtonText[i]; 
					m_LOMToolStipButton[i].Click += (sender, args) => ToolBarEventProcessor(m_S, args); 
					m_LOMToolStrip.Items.Add(m_LOMToolStipButton[i]);
				}
			}	
			return m_Return;
		}
		
		public bool ToolBarTerminate()
		{
			bool m_Return=true;
			int i;
			m_LOMToolStipButtonText=new string[]  {"Enter","Exit","ABORT"};
			
            System.Windows.Forms.Control[] coll = m_Strat.ChartControl.Controls.Find("tsrTool",false);
			
			if(coll.Length > 0) 
			{
				m_LOMToolStrip = (System.Windows.Forms.ToolStrip)coll[0];

				for (i=m_LOMToolStipButtonText.Length-1;i>=0;i--)
				{
					m_LOMToolStrip.Items.Remove(m_LOMToolStipButton[i]);
					m_LOMToolStipButton[i].Dispose();
					m_LOMToolStipButton[i]=null;
				}
			}	
			
			return m_Return;
		}		
		
		void ToolBarEventProcessor(Object myObject, EventArgs myEventArgs)
		{
			m_Strat.Print("ToolBarEventProcessor: ");
			try
			{
				//int m_count;
				//m_count=CheckForExpiredOrders();
				m_Strat.Print("ToolBarEventProcessor:  Count="+(string)myObject.ToString()+"  EVENT "+myEventArgs.ToString());
				m_LOMToolStipButton[1].Enabled=!m_LOMToolStipButton[1].Enabled;
				m_LOMToolStipButton[0].Visible=!m_LOMToolStipButton[0].Visible;
				
				m_LOMToolStipButton[2].BackColor=Color.LightBlue;
				m_Strat.ChartControl.Invalidate();
				m_MarketDepthManager.PrintLadder();
				//m_Strat.TriggerCustomEvent(MyCustomHandler, m_BarsInProgress, myObject);
			}
			catch (Exception e)
			{
				m_Strat.Print("ToolBarEventProcessor: Exception CustomHandler "+e.ToString());
			}
		}
#endregion
	}

#region OrderRules Class	
		public class OrderRules // Order Rules; Good Until, Pending Volume
		{
			// Order Handling
			private bool			m_VitualOrder		=false;		// Order managed locally, should be conditional order
			private int				m_HoldBAVolLower	=0;			// Hold Order until Ask/Bid Contracts below
			private int				m_HoldBAVolHigher	=0;			// Hold Order until Ask/Bid Contracts Above
			private int				m_TrailAskTicks		=0;			// Trail Ask tick moves, then cancel
			private int				m_TrailBidTicks		=0;			// Trail Bid tick moves, then cancel
			private double			m_VOlTargetPrice	=0.0;		// Action Price 
			
			private bool			m_RelativePosition=false;		// Order is relative to current position, don't block duplicates
			// Order Life Duration
			private bool			m_DisableRules=false;			// Set to True to temp disable
			private bool 			m_ConvertExpiredToMarket=false;	// True will convert Expire to Market Order
			private int 			m_GoodUntilRule=0;				// 0 - None, 1- Any, 2 - All
			private int 			m_BarGood=0;					// 0 - Disabled, 1 - Current Bar Close, 2 - next Bar, etc
			private int 			m_TicksAstray=0;				// 0 - Disabled, Ticks astray (far away, cancel)
			private double 			m_GoodTimeSpan=0.0;				// Seconds 0.0 disable, down to 1 ms handled 0.001
		
			private TimeSpan m_GoodSpan;	// Time upon

			public OrderRules()
			{
				m_DisableRules=true;
				m_RelativePosition=false;
			}				

			public bool VitualOrder
			{
				get {return m_VitualOrder;} set {m_VitualOrder=value;}
			}
			
			public TimeSpan GoodTimeSpan
			{
				get {return m_GoodSpan;} set {m_GoodSpan=value;}
			}	
				
			public double GoodTimeSpanSec
			{
				get {return m_GoodTimeSpan;} set {m_GoodTimeSpan=value;}
			}	
			
			public bool RelativePosition
			{
				get { return m_RelativePosition; } set { m_RelativePosition=value; }
			}				
			
			public int GoodUntilRule
			{
				get { 
					if (m_DisableRules)
						return 0; // Fake no rule
					return m_GoodUntilRule; 
				}
				set { m_GoodUntilRule=value; }
			}				

			public int BarGood
			{
				get { 
					if (m_DisableRules)
						return 0; // Fake no rule
					return m_BarGood; 
				}
			}
			
			public int TicksAstray
			{
				get { 
					if (m_DisableRules)
						return 0; // Fake no rule
					return m_TicksAstray; 
				}
			}
			
			public double GoodTimeSpanMS
			{
				get { 
					if (m_DisableRules)
						return 0.0; // Fake no rule
					return m_GoodSpan.TotalMilliseconds; 
				}
			}	
			
			public bool ConvertExpiredToMarket
			{
				get { 
					return m_ConvertExpiredToMarket; 
				}
			}	
			
			public void Disabled(bool m_Disable)
			{
				m_DisableRules=m_Disable;
			}				
			
			public void SetConvertExpiredToMarket(bool m_Convert)
			{
				m_ConvertExpiredToMarket=m_Convert;
			}		
			
			public void SetGoodUntilAll(int m_GoodBars,double m_ExpireSeconds,int m_SetTicksAstray,bool m_ConvertCanceledToMarketOrder)
			{
				m_DisableRules=false;
				m_GoodUntilRule=2;
				m_ConvertExpiredToMarket=m_ConvertCanceledToMarketOrder;
				m_BarGood=m_GoodBars; // 1 bar means current bar
				m_GoodTimeSpan=m_ExpireSeconds; // Fractions of seconds okay
				m_GoodSpan=TimeSpan.FromMilliseconds(m_ExpireSeconds*1000.0); // convert to Milliseconds
				m_TicksAstray=m_SetTicksAstray;
			}
			
			public void SetGoodUntilAny(int m_GoodBars,double m_ExpireSeconds,int m_SetTicksAstray,bool m_ConvertCanceledToMarketOrder)
			{
				m_DisableRules=false;
				m_GoodUntilRule=1;
				m_ConvertExpiredToMarket=m_ConvertCanceledToMarketOrder;
				m_BarGood=m_GoodBars; // 1 bar means current bar
				m_GoodTimeSpan=m_ExpireSeconds; // Fractions of seconds okay
				m_GoodSpan=TimeSpan.FromMilliseconds(m_ExpireSeconds*1000.0); // convert to Milliseconds
				m_TicksAstray=m_SetTicksAstray;
			}				
		}
#endregion
	
#region MarketDataManager Class
		public class MarketDataManager // Order Rules; Good Until, Pending Volume
		{
			static int MaxMarketDataEvents=500;

			private Strategy					m_Strat;
			
			private List<MarketDataEventArgs> 	m_MarketData	=new List<MarketDataEventArgs>();
			private List<MarketDataEventArgs> 	m_MarketDataAsk	=new List<MarketDataEventArgs>();
			private List<MarketDataEventArgs> 	m_MarketDataBid	=new List<MarketDataEventArgs>();
			private List<MarketDataEventArgs> 	m_MarketDataLast=new List<MarketDataEventArgs>(); 
			
			private double						m_CurrentAsk;
			private double						m_CurrentBid;
			private double						m_CurrentAskVol;
			private double						m_CurrentBidVol;
			
			private long						m_CurrentAskTotalVol;
			private long						m_CurrentBidTotalVol;
			
			private long						m_TicksAsk;
			private long						m_VolAsk;
			private long						m_TicksBid;
			private long						m_VolBid;
			
			public MarketDataManager(Strategy strat)
			{
				int i;

				m_TicksAsk=0;
				m_VolAsk=0;
				m_TicksBid=0;
				m_VolBid=0;
	//			MarketDepthEventArgs	m_Event=new MarketDepthEventArgs(null,ErrorCode.NoError,"none",0,"",Operation.Insert,MarketDataType.Unknown,0.0,0,DateTime.Now);
	//			m_MarketDepthAsk=new MarketDepthEventArgs[MaxMarketDepthEvents];
	//			m_MarketDepthBid=new MarketDepthEventArgs[MaxMarketDepthEvents];
				
				m_Strat=strat;
				for (i=0;i<MaxMarketDataEvents;++i)
				{
		//			m_MarketDepthAsk[i]=m_Event;
		//			m_MarketDepthBid[i]=m_Event;
				}
			}
			
			public void ResetStats()
			{
				m_TicksAsk=0;
				m_VolAsk=0;
				m_TicksBid=0;
				m_VolBid=0;
			}
			
			public void PrintData()
			{
				int i;
				
				m_Strat.Print("--------------------Data-----------------");
				m_Strat.Print("m_TicksAsk	="+m_TicksAsk);
				m_Strat.Print("m_VolAsk		="+m_VolAsk);
				m_Strat.Print("m_TicksBid	="+m_TicksBid);
				m_Strat.Print("m_VolBid		="+m_VolBid);
				m_Strat.Print("DeltaTicks	="+(m_TicksAsk-m_TicksBid));
				m_Strat.Print("DeltaVol		="+(m_VolAsk-m_VolBid));
				//for (i=MaxMarketDataEvents-1;i>=0;i--)
				//{
				//	m_Strat.Print("ASK: "+i+" P:"+m_MarketDepthAsk[i].Price+" V:"+m_MarketDepthAsk[i].Volume+"/"+m_AskQuantity[i]);
				//}
				//m_Strat.Print("CurrentAsk/Vol="+m_CurrentAsk+"/"+m_CurrentAskVol);
				
				//m_Strat.Print("CurrentBid/Vol="+m_CurrentBid+"/"+m_CurrentBidVol);
				//for (i=0;i<=MaxMarketDataEvents-1;i++)
				//{
				//	m_Strat.Print("BID: "+i+" P:"+m_MarketDepthBid[i].Price+" V:"+m_MarketDepthBid[i].Volume+"/"+m_BidQuantity[i]);
				//}
				m_Strat.Print("----------------Data (End)---------------");
			}
			
			public void ProcessEvent(MarketDataEventArgs e)
			{
				try
				{
					switch (e.MarketDataType)
					{
						case MarketDataType.Ask:
						{
							m_CurrentAsk=e.Price;
							m_CurrentAskVol=e.Volume;
							m_MarketData.Add(e);
							m_MarketDataAsk.Add(e);
							if (m_MarketDataAsk.Count>MaxMarketDataEvents)
								m_MarketDataAsk.RemoveRange(0,m_MarketDataAsk.Count-MaxMarketDataEvents); // Remove excess
							//m_Strat.Print("ASK MarketEvent: "+e.ToString());
							/*if (m_MarketDataAsk.Count>2)
								if (m_MarketDataAsk[m_MarketDataAsk.Count-2].Price!=m_MarketDataAsk[m_MarketDataAsk.Count-1].Price)
								{
									if (GetMarketPosition(1)==MarketPosition.Flat)
										GoMarketBracket(1,1,1);							
									if (GetMarketPosition(2)==MarketPosition.Flat)
										GoMarketBracket(1,2,2);
									if (GetMarketPosition(3)==MarketPosition.Flat)
										GoMarketBracket(1,3,3);						
								}*/
							break;
						}
						case MarketDataType.Bid:
						{
							m_CurrentBid=e.Price;
							m_CurrentBidVol=e.Volume;
							m_MarketData.Add(e);
							m_MarketDataBid.Add(e);
							if (m_MarketDataBid.Count>MaxMarketDataEvents)
								m_MarketDataBid.RemoveRange(0,m_MarketDataBid.Count-MaxMarketDataEvents); // Remove excess
							//m_Strat.Print("BID MarketEvent: "+e.ToString());
							/*if (m_MarketDataBid.Count>2)
								if (m_MarketDataBid[m_MarketDataBid.Count-2].Price!=m_MarketDataBid[m_MarketDataBid.Count-1].Price)
								{
									if (GetMarketPosition(1)==MarketPosition.Flat)
										GoMarketBracket(1,1,1);							
									if (GetMarketPosition(2)==MarketPosition.Flat)
										GoMarketBracket(1,2,2);
									else
										ExitSLPT(8,0,2);
									if (GetMarketPosition(3)==MarketPosition.Flat)
										GoMarketBracket(1,3,3);						
									else
										ExitSLPT(8,0,3);
								}	*/					
							break;
						}
						case MarketDataType.Last:
						{
							m_MarketData.Add(e);
							m_MarketDataLast.Add(e);
							if (m_MarketDataLast.Count>MaxMarketDataEvents)
								m_MarketDataLast.RemoveRange(0,m_MarketDataLast.Count-MaxMarketDataEvents); // Remove excess
							//m_Strat.Print("Last MarketEvent: "+e.ToString());
							
							if (e.Price>=m_CurrentAsk)
							{
								m_TicksAsk++;
								m_VolAsk+=e.Volume;
							}
							if (e.Price<=m_CurrentBid)
							{
								m_TicksBid++;
								m_VolBid+=e.Volume;
							}
							//PrintData();
							break;
						}
						default:
						{
							break;
						}
					}
					if (m_MarketData.Count>MaxMarketDataEvents)
						m_MarketData.RemoveRange(0,m_MarketData.Count-MaxMarketDataEvents); // Remove excess
					
					// Determaine Trends by Tick
					/*if (m_MarketEventPendingTasks>0 && false)
					{
						string m_array="";
						
						for (int i=0;i<m_MarketDataLast.Count;++i)
						{
							m_MarketLastArray[i]=m_MarketDataLast[i].Price;
							m_array=m_array+" "+m_MarketDataLast[i].Price.ToString();
						}
						
						debug("LAST ARRY: "+m_MarketDataLast.Count+" Values: "+m_array);
					}*/

			
				}
				catch (Exception exc)
				{
					m_Strat.Print("MarketDepth Exception: "+exc.ToString());
				}
			}
			private void ProcessEventAsk(MarketDataEventArgs e)
			{
				//switch (e.Operation)
				{
				//	default:					
				//		break;
				}
			}
			private void ProcessEventBid(MarketDataEventArgs e)
			{

				//switch (e.Operation)
				{
				//	default:					
				//		break;
				}	
			}	
			
			private void InsertEvent(MarketDataEventArgs e,ref MarketDataEventArgs[] m_events,ref long[] m_vol)
			{
								
			}
			
			private void RemoveEvent(MarketDataEventArgs e,ref MarketDataEventArgs[] m_events,ref long[] m_vol)
			{
				int i;
				
				//for (i=e.Position;i<(MaxMarketDataEvents-1);++i)
				{
				//	m_events[i]=m_events[i+1];
				//	m_vol[i]=m_vol[i+1];					
				}
				//m_events[MaxMarketDepthEvents-1]=e;
				//m_vol[MaxMarketDepthEvents-1]=0;										
			}
			
		}		

#endregion		
		
#region MarketDepthManager Class
		public class MarketDepthManager // Order Rules; Good Until, Pending Volume
		{
			static int MaxMarketDepthEvents=11;

			private Strategy					m_Strat;
			private MarketDepthEventArgs[]		m_MarketDepthAsk;
			private MarketDepthEventArgs[]		m_MarketDepthBid;
			private long[]						m_AskQuantity	=new long[MaxMarketDepthEvents];					
			private long[]						m_BidQuantity	=new long[MaxMarketDepthEvents];	
			private double						m_CurrentAsk;
			private double						m_CurrentBid;
			private double						m_CurrentAskVol;
			private double						m_CurrentBidVol;
			
			private int							m_VolDepthToTotal=MaxMarketDepthEvents;
			private long						m_CurrentAskTotalVol;
			private long						m_CurrentBidTotalVol;
			
			public MarketDepthManager(Strategy strat)
			{
				int i;
				MarketDepthEventArgs	m_Event=new MarketDepthEventArgs(null,ErrorCode.NoError,"none",0,"",Operation.Insert,MarketDataType.Unknown,0.0,0,DateTime.Now);
				m_MarketDepthAsk=new MarketDepthEventArgs[MaxMarketDepthEvents];
				m_MarketDepthBid=new MarketDepthEventArgs[MaxMarketDepthEvents];
				
				m_Strat=strat;
				for (i=0;i<MaxMarketDepthEvents;++i)
				{
					m_MarketDepthAsk[i]=m_Event;
					m_MarketDepthBid[i]=m_Event;
				}
			}
			public long CurrentAskTotolVolume
			{ get {return m_CurrentAskTotalVol;} }	
			public long CurrentBidTotolVolume
			{ get {return m_CurrentBidTotalVol;} }	
				
				
			public void PrintLadder()
			{
				int i;
				
				m_Strat.Print("--------------------Level II Ladder-----------------");
				m_Strat.Print("AskTotalVol="+m_CurrentAskTotalVol);
				for (i=MaxMarketDepthEvents-1;i>=0;i--)
				{
					m_Strat.Print("ASK: "+i+" P:"+m_MarketDepthAsk[i].Price+" V:"+m_MarketDepthAsk[i].Volume+"/"+m_AskQuantity[i]);
				}
				m_Strat.Print("CurrentAsk/Vol="+m_CurrentAsk+"/"+m_CurrentAskVol);
				
				m_Strat.Print("CurrentBid/Vol="+m_CurrentBid+"/"+m_CurrentBidVol);
				for (i=0;i<=MaxMarketDepthEvents-1;i++)
				{
					m_Strat.Print("BID: "+i+" P:"+m_MarketDepthBid[i].Price+" V:"+m_MarketDepthBid[i].Volume+"/"+m_BidQuantity[i]);
				}
				m_Strat.Print("BidTotalVol="+m_CurrentBidTotalVol);
				m_Strat.Print("----------------Level II Ladder (End)---------------");
			}
			
			public void ProcessEvent(MarketDepthEventArgs e)
			{
				try
				{
					if (e.Position>=MaxMarketDepthEvents)
						return; // out of range
					
					switch (e.MarketDataType)
					{
						case MarketDataType.Ask:
							ProcessEventAsk(e);
							m_CurrentAskTotalVol=TotalVolume(m_AskQuantity);
							break;
						case MarketDataType.Bid:
							ProcessEventBid(e);
							m_CurrentBidTotalVol=TotalVolume(m_BidQuantity);
							break;
						default:					
							break;
					}
				}
				catch (Exception exc)
				{
					m_Strat.Print("MarketDepth Exception: "+exc.ToString());
				}
			}
			private void ProcessEventAsk(MarketDepthEventArgs e)
			{
				switch (e.Operation)
				{
					case Operation.Update:
						m_MarketDepthAsk[e.Position]=e;
						m_AskQuantity[e.Position]=e.Volume;
						if (e.Position==0)
						{
							m_CurrentAsk=e.Price;
							m_CurrentAskVol=e.Volume;
						}
						break;
					case Operation.Insert:
						InsertEvent(e,ref m_MarketDepthAsk,ref m_AskQuantity);
						m_CurrentAsk=m_MarketDepthAsk[0].Price;
						m_CurrentAskVol=m_AskQuantity[0];
						break;
					case Operation.Remove:
						RemoveEvent(e,ref m_MarketDepthAsk,ref m_AskQuantity);
						m_CurrentAsk=m_MarketDepthAsk[0].Price;
						m_CurrentAskVol=m_AskQuantity[0];
						break;
					default:					
						break;
				}
			}
			private void ProcessEventBid(MarketDepthEventArgs e)
			{

				switch (e.Operation)
				{
					case Operation.Update:
						m_MarketDepthBid[e.Position]=e;
						m_BidQuantity[e.Position]=e.Volume;
						if (e.Position==0)
						{
							m_CurrentBid=e.Price;
							m_CurrentBidVol=e.Volume;
						}
						break;
					case Operation.Insert:
						InsertEvent(e,ref m_MarketDepthBid,ref m_BidQuantity);
						m_CurrentBidVol=m_BidQuantity[0];
						m_CurrentBid=m_MarketDepthBid[0].Price;
						break;
					case Operation.Remove:
						RemoveEvent(e,ref m_MarketDepthBid,ref m_BidQuantity);
						m_CurrentBidVol=m_BidQuantity[0];
						m_CurrentBid=m_MarketDepthBid[0].Price;
						break;
					default:					
						break;
				}	
			}	
			
			private void InsertEvent(MarketDepthEventArgs e,ref MarketDepthEventArgs[] m_events,ref long[] m_vol)
			{
				int i;
				
				for (i=e.Position;i<(MaxMarketDepthEvents-1);++i)
				{
					m_events[i+1]=m_events[i];
					m_vol[i+1]=m_vol[i];					
				}
				m_events[e.Position]=e;
				m_vol[e.Position]=e.Volume;										
			}
			
			private void RemoveEvent(MarketDepthEventArgs e,ref MarketDepthEventArgs[] m_events,ref long[] m_vol)
			{
				int i;
				
				for (i=e.Position;i<(MaxMarketDepthEvents-1);++i)
				{
					m_events[i]=m_events[i+1];
					m_vol[i]=m_vol[i+1];					
				}
				m_events[MaxMarketDepthEvents-1]=e;
				m_vol[MaxMarketDepthEvents-1]=0;										
			}
			
			private long TotalVolume(long[] m_vol)
			{
				int i;
				long m_total=0;
				
				for (i=0;i<m_VolDepthToTotal;++i)
				{
					m_total+=m_vol[i];					
				}
				return m_total;
			}
		}		

#endregion
}
namespace NinjaTrader
{	
#region WriteLineToFile Class
		public class WriteLineToFile // Order Rules; Good Until, Pending Volume
		{
			private string					m_FileName;
			private StreamWriter 			m_OutFile=null;
        	//string mydocpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			
			public WriteLineToFile()
			{
				string filename = "temp.cvs";
				string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)+@"\";
				m_FileName=path + filename;
				if (m_OutFile==null)
					m_OutFile = new StreamWriter(m_FileName);
			}
			public WriteLineToFile(string m_FN)
			{
				string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)+@"\";
				m_FileName=path + m_FN;
				if (m_OutFile==null)
					m_OutFile = new StreamWriter(m_FileName);
			}			
			public WriteLineToFile(string m_Path,string m_FN)
			{
				m_FileName=m_Path + m_FN;
				if (m_OutFile==null)
					m_OutFile = new StreamWriter(m_FileName);
			}

			public void WriteLine(string m_Line)
			{
				m_OutFile.WriteLine(m_Line);
			}
			
			public void Close()
			{
				if (m_OutFile!=null)
				{
					m_OutFile.Dispose();
					m_OutFile=null;
				}
			}
					
			~WriteLineToFile()
			{
				if (m_OutFile!=null)
				{
					m_OutFile.Dispose();
					m_OutFile=null;
				}
			}
			
		}		
#endregion
					
#region OrderFunctionType ENUM		
		public enum OrderFunctionType : int 
		{
			NONE=0,
			GoLongMarket,
			GoShortMarket,
			GoLongTrend,
			GoShortTrend,
			GoMarketBracket,
			GoLimitBracket,
			GoLongLimit,
			GoLongStop,
			GoLongStopLimit,
			GoShortLimit,
			GoShortStop,
			GoShortStopLimit,
			ExitSLPT,
			ExitMarket
		}
#endregion
		
#region RejectedOrderAction ENUM		
		public enum RejectedOrderAction : int 
		{
			DEFAULT=0,
			Ignore,
			ExitMarket,
			ExitEnterMarket,
			EnterMarket,
			SimExitOrder,
			SimExitEnterOrder,
			SimEnterOrder
		}
#endregion			
}
