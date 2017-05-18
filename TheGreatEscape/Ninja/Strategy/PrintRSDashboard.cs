/////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// Created by: Shane A. Brewer
// www.shanebrewer.com
// Version 1.0
// Last Modified: January 5, 2012
//
////////////////////////////////////////////////////////////////////////////////////////////////////////

#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Forms.Design;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Indicator;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Strategy;
using Excel = Microsoft.Office.Interop.Excel;
#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
   /// <summary>
   /// This strategy is run to print out an Excel spreadsheet Dashboard for longer term investments.
   /// 
   /// I had some errors when the end date of the data series is too close to the present. SQN values were 0 so if
   /// this happens again, push the end date back a couple of days.
   /// 
   /// </summary>
   [Description("This strategy is run to print out an Excel spreadsheet Dashboard of various ETFs for longer term investments.")]
   public class PrintRSDashboard : Strategy
   {
      #region Variables
      private string excelFilename = @"Market RS Dashboard ";
	  private string benchMark="^SP500";
      private string excelFileAndPath;
      protected string outputFolder = @"C:\LogDir"; // Default setting for outputFolder
      private Excel.Application excelApp;
      private Excel.Workbook excelWorkbook;
      private Excel.Worksheet stockMarketWorksheet;
      private const string stockMarketWorksheetName = "Stock Market";
      private readonly String[] names = { "", "Austrailia", "China", "SPDR S&P China", "China/India", "Hong Kong", "India", "Japan",
         "Malaysia", "Singapore", "South Korea", "Taiwan", "Thailand", "Asia Less Japan", 
         "Currency Harvest", "Australian Dollar", "Brazil Real", "British Pound", "GBP/USD", "Canadian Dollar", "Euro Trust",
         "Euro", "EUR/USD", "Japanese Yen", "JPY/USD", "Rupee", "Swedish Krona", "Swiss Franc", "Yuan", "$US Bullish", "$US Bearish",
         "Commodities", "Gold", "Silver", "Base Metals", "Oil", "Natural Gas", "Coal", "Steel", "Global Water", "Timber", "Agriculture", "Livestock", "Gbl Agribusiness",
         "Real Estate", "RE Less US", "RE Less US", "China RE",
         "20 yr+ Bonds", "10-20 yr US Bonds", "7-10 yr US Bonds", "3-7 yr US Bonds", "1-3 yr US Bonds", "ST Bonds", "TIPS Bonds", "Corp Bonds", "High Yield Bonds",
         "Dow Jones", "S&P 500", "NASDAQ", "S&P MidCap 400 Value", "S&P MidCap 400", "S&P MidCap 400 Growth", "S&P Small Cap 600 Value", "Russell 2000", "S&P SmallCap 600 Growth", "Zacks Micro Cap",
         "Canada", "Mexico", "Brazil", "Chile", "Latin America", "Emerging Mkts", "S&P World ex-US",
         "Biotech", "Building Materials", "Consumer Staples", "Consumer Discretionary", "Energy", "Financial", "Health Care", "Homebuilders", "Industrial", "Metals and Mining", "Oil & Gas Equipment", 
         "Oil & Gas Exploration", "Phamaceuticals", "Retail", "REIT", "Semiconductor", "Technology", "Utilities", "Aerospace and Defense",
         "Biotech and Genome", "Broker Dealers", "Food and Beverage", "Gaming", "Insurance", "Media", "Networking", "Regional Banks",
         "Software", "Telecom", "Transportation Dow", "Volatility Index",
         "World Building Materials", "World Consumer Staples", "World Consumer Discretionary", "World Energy", "World Financial", "World Health Care",
         "World Industrial", "World Technology", "World Utilities",
         "Austria", "Belgium", "France", "Germany", "Netherlands", "Russia", "Spain", "Sweden", "Switzerland", "UK", "Emerging Europe", "Mid-East/Africa", "South Africa", "EAFE Value", "EAFE", "EAFE Growth"
         };

      Excel.Style headingStyle;
      Excel.Style subheadingStyle;
      Excel.Style nameStyle;
      Excel.Style tickerStyle;
      Excel.Style sqnStyle;

      private const int asiaColumn = 20;
      private const int asiaRow = 2;
      private const int europeAfricaColumn = 20;
      private const int europeAfricaRow = 18;
      private const int foreignCurrencyColumn = 2;
      private const int foreignCurrencyRow = 2;
      private const int commoditiesColumn = 2;
      private const int commoditiesRow = 22;
      private const int realEstateColumn = 2;
      private const int realEstateRow = 38;
      private const int interestRateColumn = 2;
      private const int interestRateRow = 45;

      #endregion

      /// <summary>
      /// This method is used to configure the strategy and is called once before any strategy method is called.
      /// </summary>
      protected override void Initialize()
      {
		
		
         // Asia
         Add("EWA", PeriodType.Day, 1);
         Add("PGJ", PeriodType.Day, 1);
         Add("GXC", PeriodType.Day, 1);
         Add("FNI", PeriodType.Day, 1);
         Add("EWH", PeriodType.Day, 1);
         Add("IFN", PeriodType.Day, 1);
         Add("EWJ", PeriodType.Day, 1);
         Add("EWM", PeriodType.Day, 1);
         Add("EWS", PeriodType.Day, 1);
         Add("EWY", PeriodType.Day, 1);
         Add("EWT", PeriodType.Day, 1);
         Add("THD", PeriodType.Day, 1);
         Add("EPP", PeriodType.Day, 1);

         // Foreign Currencies
         Add("DBV", PeriodType.Day, 1);
         Add("FXA", PeriodType.Day, 1);
         Add("BZF", PeriodType.Day, 1);
         Add("FXB", PeriodType.Day, 1);
         Add("GBB", PeriodType.Day, 1);
         Add("FXC", PeriodType.Day, 1);
         Add("FXE", PeriodType.Day, 1);
         Add("EU", PeriodType.Day, 1);
         Add("ERO", PeriodType.Day, 1);
         Add("FXY", PeriodType.Day, 1);
         Add("JYN", PeriodType.Day, 1);
         Add("ICN", PeriodType.Day, 1);
         Add("FXS", PeriodType.Day, 1);
         Add("FXF", PeriodType.Day, 1);
         Add("CYB", PeriodType.Day, 1);
         Add("UUP", PeriodType.Day, 1);
         Add("UDN", PeriodType.Day, 1);

         // Commodities
         Add("DBC", PeriodType.Day, 1);
         Add("GLD", PeriodType.Day, 1);
         Add("SLV", PeriodType.Day, 1);
         Add("DBB", PeriodType.Day, 1);
         Add("USO", PeriodType.Day, 1);
         Add("UNG", PeriodType.Day, 1);
         Add("KOL", PeriodType.Day, 1);
         Add("SLX", PeriodType.Day, 1);
         Add("CGW", PeriodType.Day, 1);
         Add("CUT", PeriodType.Day, 1);
         Add("DBA", PeriodType.Day, 1);
         Add("COW", PeriodType.Day, 1);
         Add("MOO", PeriodType.Day, 1);

         // Real Estate
         Add("RWR", PeriodType.Day, 1);
         Add("WPS", PeriodType.Day, 1);
         Add("IFGL", PeriodType.Day, 1);
         Add("TAO", PeriodType.Day, 1);

         // Interest Rate Products
         Add("TLT", PeriodType.Day, 1);
         Add("TLH", PeriodType.Day, 1);
         Add("IEF", PeriodType.Day, 1);
         Add("IEI", PeriodType.Day, 1);
         Add("SHY", PeriodType.Day, 1);
         Add("BSV", PeriodType.Day, 1);
         Add("TIP", PeriodType.Day, 1);
         Add("LQD", PeriodType.Day, 1);
         Add("JNK", PeriodType.Day, 1);

         // Market Segments
         Add("DIA", PeriodType.Day, 1);
         Add("SPY", PeriodType.Day, 1);
         Add("QQQ", PeriodType.Day, 1);
         Add("IJJ", PeriodType.Day, 1);
         Add("MDY", PeriodType.Day, 1);
         Add("IJK", PeriodType.Day, 1);
         Add("IJS", PeriodType.Day, 1);
         Add("IWM", PeriodType.Day, 1);
         Add("IJT", PeriodType.Day, 1);
         Add("PZI", PeriodType.Day, 1);
         Add("EWC", PeriodType.Day, 1);
         Add("EWW", PeriodType.Day, 1);
         Add("EWZ", PeriodType.Day, 1);
         Add("ECH", PeriodType.Day, 1);
         Add("ILF", PeriodType.Day, 1);
         Add("EEM", PeriodType.Day, 1);
         Add("GWL", PeriodType.Day, 1);

         // U.S. Sectors
         Add("XBI", PeriodType.Day, 1);
         Add("XLB", PeriodType.Day, 1);
         Add("XLP", PeriodType.Day, 1);
         Add("XLY", PeriodType.Day, 1);
         Add("XLE", PeriodType.Day, 1);
         Add("XLF", PeriodType.Day, 1);
         Add("XLV", PeriodType.Day, 1);
         Add("XHB", PeriodType.Day, 1);
         Add("XLI", PeriodType.Day, 1);
         Add("XME", PeriodType.Day, 1);
         Add("XES", PeriodType.Day, 1);
         Add("XOP", PeriodType.Day, 1);
         Add("XPH", PeriodType.Day, 1);
         Add("XRT", PeriodType.Day, 1);
         Add("FRI", PeriodType.Day, 1);
         Add("XSD", PeriodType.Day, 1);
         Add("XLK", PeriodType.Day, 1);
         Add("XLU", PeriodType.Day, 1);
         Add("ITA", PeriodType.Day, 1);
         Add("PBE", PeriodType.Day, 1);
         Add("IAI", PeriodType.Day, 1);
         Add("PBJ", PeriodType.Day, 1);
         Add("BJK", PeriodType.Day, 1);
         Add("IAK", PeriodType.Day, 1);
         Add("PBS", PeriodType.Day, 1);
         Add("IGN", PeriodType.Day, 1);
         Add("KRE", PeriodType.Day, 1);
         Add("IGV", PeriodType.Day, 1);
         Add("IYZ", PeriodType.Day, 1);
         Add("IYT", PeriodType.Day, 1);
         Add("VXX", PeriodType.Day, 1);
         
         // World Sectors
         Add("MXI", PeriodType.Day, 1);
         Add("KXI", PeriodType.Day, 1);
         Add("RXI", PeriodType.Day, 1);
         Add("IXC", PeriodType.Day, 1);
         Add("IXG", PeriodType.Day, 1);
         Add("IXJ", PeriodType.Day, 1);
         Add("EXI", PeriodType.Day, 1);
         Add("IXN", PeriodType.Day, 1);
         Add("JXI", PeriodType.Day, 1);

         // Europe And Africa
         Add("EWO", PeriodType.Day, 1);
         Add("EWK", PeriodType.Day, 1);
         Add("EWQ", PeriodType.Day, 1);
         Add("EWG", PeriodType.Day, 1);
         Add("EWN", PeriodType.Day, 1);
         Add("RSX", PeriodType.Day, 1);
         Add("EWP", PeriodType.Day, 1);
         Add("EWD", PeriodType.Day, 1);
         Add("EWL", PeriodType.Day, 1);
         Add("EWU", PeriodType.Day, 1);
         Add("GUR", PeriodType.Day, 1);
         Add("GAF", PeriodType.Day, 1);
         Add("EZA", PeriodType.Day, 1);
         Add("EFV", PeriodType.Day, 1);
         Add("EFA", PeriodType.Day, 1);
         Add("EFG", PeriodType.Day, 1);
         Add(benchMark, PeriodType.Day, 1);
         excelFileAndPath = outputFolder + @"\" + excelFilename + DateTime.Now.ToLongDateString() + ".xlsx";

         CalculateOnBarClose = true;
      }

      protected override void OnTermination()
      {
		
System.Globalization.CultureInfo oldCI = System.Threading.Thread.CurrentThread.CurrentCulture;
System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
 
         excelApp = new Microsoft.Office.Interop.Excel.Application();
         excelApp.DisplayAlerts = false;
         excelWorkbook = excelApp.Application.Workbooks.Add(Type.Missing);

         SetExcelWorkbookStyles();
         PrintStockMarketSummary();

         excelWorkbook.Close(true, excelFileAndPath, false);
         excelApp.Quit();
      }

      private void SetExcelWorkbookStyles()
      {
         headingStyle = excelWorkbook.Styles.Add("HeadingStyle", Type.Missing);
         headingStyle.Font.Bold = true;
         headingStyle.Font.Size = 13;
         headingStyle.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Orange);
         headingStyle.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
         //AddBordersToStyle(headingStyle);

         subheadingStyle = excelWorkbook.Styles.Add("SubHeadingStyle", Type.Missing);
         subheadingStyle.Font.Bold = true;
         subheadingStyle.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Orange);
         subheadingStyle.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;

         nameStyle = excelWorkbook.Styles.Add("NameStyle", Type.Missing);
         nameStyle.Font.Bold = true;

         tickerStyle = excelWorkbook.Styles.Add("TickerStyle", Type.Missing);
         tickerStyle.Font.Bold = true;
         tickerStyle.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Wheat);
         tickerStyle.HorizontalAlignment = Excel.XlHAlign.xlHAlignRight;
         AddBordersToStyle(tickerStyle);

         sqnStyle = excelWorkbook.Styles.Add("SQNStyle", Type.Missing);
         sqnStyle.Font.Bold = true;
         AddBordersToStyle(sqnStyle);
      }

      private void AddBordersToStyle(Excel.Style style)
      {
         style.Borders.Weight = 2d;
         style.Borders[Excel.XlBordersIndex.xlEdgeBottom].Color = Color.Black.ToArgb();
         style.Borders[Excel.XlBordersIndex.xlEdgeBottom].LineStyle = Excel.XlLineStyle.xlContinuous;
         style.Borders[Excel.XlBordersIndex.xlEdgeTop].Color = Color.Black.ToArgb();
         style.Borders[Excel.XlBordersIndex.xlEdgeTop].LineStyle = Excel.XlLineStyle.xlContinuous;
         style.Borders[Excel.XlBordersIndex.xlEdgeLeft].Color = Color.Black.ToArgb();
         style.Borders[Excel.XlBordersIndex.xlEdgeLeft].LineStyle = Excel.XlLineStyle.xlContinuous;
         style.Borders[Excel.XlBordersIndex.xlEdgeRight].Color = Color.Black.ToArgb();
         style.Borders[Excel.XlBordersIndex.xlEdgeRight].LineStyle = Excel.XlLineStyle.xlContinuous;
         style.Borders[Excel.XlBordersIndex.xlDiagonalDown].LineStyle = Excel.XlLineStyle.xlLineStyleNone;
         style.Borders[Excel.XlBordersIndex.xlDiagonalUp].LineStyle = Excel.XlLineStyle.xlLineStyleNone;
      }

      private void PrintStockMarketSummary()
      {
         stockMarketWorksheet = (Excel.Worksheet)excelApp.ActiveWorkbook.Sheets[1];
         stockMarketWorksheet.Name = stockMarketWorksheetName;

         // Outline Color
         Excel.Range outlineRange = stockMarketWorksheet.get_Range("A1", "Z67");
         outlineRange.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(147, 210, 79));

         // Foreign Currencies Section
         Excel.Range foriegnCurrenciesHeadingRange = stockMarketWorksheet.get_Range("B2", "G2");
         foriegnCurrenciesHeadingRange.MergeCells = true;
         foriegnCurrenciesHeadingRange.Style = "HeadingStyle";

         stockMarketWorksheet.Cells[foreignCurrencyRow, foreignCurrencyColumn] = "Foreign Currencies";

         Excel.Range foriegnCurrenciesSubheadingRange = stockMarketWorksheet.get_Range("B3", "G3");
         foriegnCurrenciesSubheadingRange.Style = "SubHeadingStyle";

         stockMarketWorksheet.Cells[foreignCurrencyRow + 1, foreignCurrencyColumn + 1] = "Ticker";
         stockMarketWorksheet.Cells[foreignCurrencyRow + 1, foreignCurrencyColumn + 2] = @"RS(104)";
         stockMarketWorksheet.Cells[foreignCurrencyRow + 1, foreignCurrencyColumn + 3] = @"RS(52)";
         stockMarketWorksheet.Cells[foreignCurrencyRow + 1, foreignCurrencyColumn + 4] = @"RS(26)";
         stockMarketWorksheet.Cells[foreignCurrencyRow + 1, foreignCurrencyColumn + 5] = @"RS(13)";

         Excel.Range foriegnCurrenciesNameRange = stockMarketWorksheet.get_Range("B4", "B20");
         foriegnCurrenciesNameRange.Style = "NameStyle";

         Excel.Range foriegnCurrenciesTickerStyle = stockMarketWorksheet.get_Range("C4", "C20");
         foriegnCurrenciesTickerStyle.Style = "TickerStyle";
         foriegnCurrenciesTickerStyle.Columns.AutoFit();

         Excel.Range foriegnCurrenciesSQNRange = stockMarketWorksheet.get_Range("D4", "G20");
         foriegnCurrenciesSQNRange.Style = "SQNStyle";

         PrintSQNValues(stockMarketWorksheet, 14, 17, foreignCurrencyRow + 2, foreignCurrencyColumn);

         // Commodities Section

         Excel.Range commoditiesHeadingRange = stockMarketWorksheet.get_Range("B22", "G22");
         commoditiesHeadingRange.MergeCells = true;
         commoditiesHeadingRange.Style = "HeadingStyle";

         stockMarketWorksheet.Cells[commoditiesRow, commoditiesColumn] = "Commodities";

         Excel.Range commoditiesSubheadingRange = stockMarketWorksheet.get_Range("B23", "G23");
         commoditiesSubheadingRange.Style = "SubHeadingStyle";

         stockMarketWorksheet.Cells[commoditiesRow + 1, commoditiesColumn + 1] = "Ticker";
         stockMarketWorksheet.Cells[commoditiesRow + 1, commoditiesColumn + 2] = @"RS(104)";
         stockMarketWorksheet.Cells[commoditiesRow + 1, commoditiesColumn + 3] = @"RS(52)";
         stockMarketWorksheet.Cells[commoditiesRow + 1, commoditiesColumn + 4] = @"RS(26)";
         stockMarketWorksheet.Cells[commoditiesRow + 1, commoditiesColumn + 5] = @"RS(13)";

         Excel.Range nameRange = stockMarketWorksheet.get_Range("B24", "B36");
         nameRange.Style = "NameStyle";

         Excel.Range tickerRange = stockMarketWorksheet.get_Range("C24", "C36");
         tickerRange.Style = "TickerStyle";

         Excel.Range sqnRange = stockMarketWorksheet.get_Range("D24", "G36");
         sqnRange.Style = "SQNStyle";

         PrintSQNValues(stockMarketWorksheet, 31, 13, commoditiesRow + 2, commoditiesColumn);

         nameRange.Columns.AutoFit();

         // Real Estate Section

         Excel.Range realEstateHeadingRange = stockMarketWorksheet.get_Range("B38", "G38");
         realEstateHeadingRange.MergeCells = true;
         realEstateHeadingRange.Style = "HeadingStyle";

         stockMarketWorksheet.Cells[realEstateRow, realEstateColumn] = "Real Estate";

         Excel.Range realEstateSubheadingRange = stockMarketWorksheet.get_Range("B39", "G39");
         realEstateSubheadingRange.Style = "SubHeadingStyle";

         stockMarketWorksheet.Cells[realEstateRow + 1, realEstateColumn + 1] = "Ticker";
         stockMarketWorksheet.Cells[realEstateRow + 1, realEstateColumn + 2] = @"RS(104)";
         stockMarketWorksheet.Cells[realEstateRow + 1, realEstateColumn + 3] = @"RS(52)";
         stockMarketWorksheet.Cells[realEstateRow + 1, realEstateColumn + 4] = @"RS(26)";
         stockMarketWorksheet.Cells[realEstateRow + 1, realEstateColumn + 5] = @"RS(13)";

         nameRange = excelApp.get_Range("B40", "B43");
         nameRange.Style = "NameStyle";

         tickerRange = excelApp.get_Range("C40", "C43");
         tickerRange.Style = "TickerStyle";

         sqnRange = excelApp.get_Range("D40", "G43");
         sqnRange.Style = "SQNStyle";

         PrintSQNValues(stockMarketWorksheet, 44, 4, realEstateRow + 2, realEstateColumn);

         // Interest Rate Products Section

         Excel.Range interestRateProductsHeadingRange = stockMarketWorksheet.get_Range("B45", "G45");
         interestRateProductsHeadingRange.MergeCells = true;
         interestRateProductsHeadingRange.Style = "HeadingStyle";

         stockMarketWorksheet.Cells[interestRateRow, interestRateColumn] = "Interest Rate Products";

         Excel.Range interestRateProductsSubheadingRange = excelApp.get_Range("B46", "G46");
         interestRateProductsSubheadingRange.Style = "SubHeadingStyle";

         stockMarketWorksheet.Cells[interestRateRow + 1, interestRateColumn + 1] = "Ticker";
         stockMarketWorksheet.Cells[interestRateRow + 1, interestRateColumn + 2] = @"RS(104)";
         stockMarketWorksheet.Cells[interestRateRow + 1, interestRateColumn + 3] = @"RS(52)";
         stockMarketWorksheet.Cells[interestRateRow + 1, interestRateColumn + 4] = @"RS(26)";
         stockMarketWorksheet.Cells[interestRateRow + 1, interestRateColumn + 5] = @"RS(13)";

         nameRange = excelApp.get_Range("B47", "B55");
         nameRange.Style = "NameStyle";

         tickerRange = excelApp.get_Range("C47", "C55");
         tickerRange.Style = "TickerStyle";

         sqnRange = excelApp.get_Range("D47", "G55");
         sqnRange.Style = "SQNStyle";

         PrintSQNValues(stockMarketWorksheet, 48, 9, interestRateRow + 2, interestRateColumn);

         // Buffer Zone
         Excel.Range bufferRange1 = stockMarketWorksheet.get_Range("H1", "I1");
         bufferRange1.ColumnWidth = 2;

         // US Market Segments
         Excel.Range titleRange = stockMarketWorksheet.get_Range("J1", "Q1");
         titleRange.MergeCells = true;
         titleRange.Style = "HeadingStyle";

         stockMarketWorksheet.Cells[1, 10] = "SQN Scores by Market Segment, Region, and Industrial Sector";

         Excel.Range usMarketSegmentsRange = stockMarketWorksheet.get_Range("K2", "P2");
         usMarketSegmentsRange.MergeCells = true;
         usMarketSegmentsRange.Style = "HeadingStyle";

         stockMarketWorksheet.Cells[2, 11] = "Market Segments";

         Excel.Range marketSegmentsSubheadingRange = excelApp.get_Range("K3", "P3");
         marketSegmentsSubheadingRange.Style = "SubHeadingStyle";

         stockMarketWorksheet.Cells[3, 12] = "Ticker";
         stockMarketWorksheet.Cells[3, 13] = @"RS(104)";
         stockMarketWorksheet.Cells[3, 14] = @"RS(52)";
         stockMarketWorksheet.Cells[3, 15] = @"RS(26)";
         stockMarketWorksheet.Cells[3, 16] = @"RS(13)";

         nameRange = excelApp.get_Range("K4", "K20");
         nameRange.Style = "NameStyle";

         tickerRange = excelApp.get_Range("L4", "L20");
         tickerRange.Style = "TickerStyle";
         tickerRange.Columns.AutoFit();

         sqnRange = excelApp.get_Range("M4", "P20");
         sqnRange.Style = "SQNStyle";

         PrintSQNValues(stockMarketWorksheet, 57, 17, 4, 11);

         nameRange.Columns.AutoFit();

         // US Sectors

         Excel.Range usSectorsTitleRange = stockMarketWorksheet.get_Range("K22", "P22");
         usSectorsTitleRange.MergeCells = true;
         usSectorsTitleRange.Style = "HeadingStyle";

         stockMarketWorksheet.Cells[22, 11] = "U.S. Sectors";

         Excel.Range usSectorsSubheadingRange = excelApp.get_Range("K23", "P23");
         usSectorsSubheadingRange.Style = "SubHeadingStyle";

         stockMarketWorksheet.Cells[23, 12] = "Ticker";
         stockMarketWorksheet.Cells[23, 13] = @"RS(104)";
         stockMarketWorksheet.Cells[23, 14] = @"RS(52)";
         stockMarketWorksheet.Cells[23, 15] = @"RS(26)";
         stockMarketWorksheet.Cells[23, 16] = @"RS(13)";

         nameRange = excelApp.get_Range("K24", "K54");
         nameRange.Style = "NameStyle";

         tickerRange = excelApp.get_Range("L24", "L54");
         tickerRange.Style = "TickerStyle";

         sqnRange = excelApp.get_Range("M24", "P54");
         sqnRange.Style = "SQNStyle";

         PrintSQNValues(stockMarketWorksheet, 74, 31, 24, 11);

         // World Sectors

         Excel.Range worldSectorsTitleRange = stockMarketWorksheet.get_Range("K56", "P56");
         worldSectorsTitleRange.MergeCells = true;
         worldSectorsTitleRange.Style = "HeadingStyle";

         stockMarketWorksheet.Cells[56, 11] = "World Sectors";

         Excel.Range worldSectorsSubheadingRange = excelApp.get_Range("K57", "P57");
         worldSectorsSubheadingRange.Style = "SubHeadingStyle";

         stockMarketWorksheet.Cells[57, 12] = "Ticker";
         stockMarketWorksheet.Cells[57, 13] = @"RS(104)";
         stockMarketWorksheet.Cells[57, 14] = @"RS(52)";
         stockMarketWorksheet.Cells[57, 15] = @"RS(26)";
         stockMarketWorksheet.Cells[57, 16] = @"RS(13)";

         nameRange = excelApp.get_Range("K58", "K66");
         nameRange.Style = "NameStyle";

         tickerRange = excelApp.get_Range("L58", "L66");
         tickerRange.Style = "TickerStyle";

         sqnRange = excelApp.get_Range("M58", "P66");
         sqnRange.Style = "SQNStyle";

         PrintSQNValues(stockMarketWorksheet, 104, 9, 58, 11);

         nameRange.Columns.AutoFit();

         // Buffer Zone
         Excel.Range bufferRange2 = stockMarketWorksheet.get_Range("R1", "S1");
         bufferRange2.ColumnWidth = 2;

         // Asia Section
         Excel.Range asiaHeadingRange = stockMarketWorksheet.get_Range("T2", "Y2");
         asiaHeadingRange.MergeCells = true;
         asiaHeadingRange.Style = "HeadingStyle";

         stockMarketWorksheet.Cells[asiaRow, asiaColumn] = "Asia";

         Excel.Range asiaSubheadingRange = stockMarketWorksheet.get_Range("T3", "Y3");
         asiaSubheadingRange.Style = "SubHeadingStyle";

         stockMarketWorksheet.Cells[asiaRow + 1, asiaColumn + 1] = "Ticker";
         stockMarketWorksheet.Cells[asiaRow + 1, asiaColumn + 2] = @"RS(104)";
         stockMarketWorksheet.Cells[asiaRow + 1, asiaColumn + 3] = @"RS(52)";
         stockMarketWorksheet.Cells[asiaRow + 1, asiaColumn + 4] = @"RS(26)";
         stockMarketWorksheet.Cells[asiaRow + 1, asiaColumn + 5] = @"RS(13)";

         Excel.Range asiaNameRange = stockMarketWorksheet.get_Range("T4", "T16");
         asiaNameRange.Style = "NameStyle";

         Excel.Range asiaTickerRange = stockMarketWorksheet.get_Range("U4", "U16");
         asiaTickerRange.Style = "TickerStyle";
         asiaTickerRange.Columns.AutoFit();

         Excel.Range asiaSQNRange = stockMarketWorksheet.get_Range("V4", "Y16");
         asiaSQNRange.Style = "SQNStyle";

         PrintSQNValues(stockMarketWorksheet, 1, 13, asiaRow + 2, asiaColumn);

         asiaNameRange.Columns.AutoFit();

         // Europe and Africa
         Excel.Range europeAfricaTitleRange = stockMarketWorksheet.get_Range("T18", "Y18");
         europeAfricaTitleRange.MergeCells = true;
         europeAfricaTitleRange.Style = "HeadingStyle";

         stockMarketWorksheet.Cells[18, 20] = "Europe and Africa";

         Excel.Range europeAfricaSubheadingRange = excelApp.get_Range("T19", "Y19");
         europeAfricaSubheadingRange.Style = "SubHeadingStyle";

         stockMarketWorksheet.Cells[19, 21] = "Ticker";
         stockMarketWorksheet.Cells[19, 22] = @"RS(104)";
         stockMarketWorksheet.Cells[19, 23] = @"RS(52)";
         stockMarketWorksheet.Cells[19, 24] = @"RS(26)";
         stockMarketWorksheet.Cells[19, 25] = @"RS(13)";
		 Print( "SO FAR");
         nameRange = excelApp.get_Range("T20", "T35");
         nameRange.Style = "NameStyle";

         tickerRange = excelApp.get_Range("U20", "U35");
         tickerRange.Style = "TickerStyle";

         sqnRange = excelApp.get_Range("V20", "Y35");
         sqnRange.Style = "SQNStyle";

         PrintSQNValues(stockMarketWorksheet, 112, 16, 20, 20);

         nameRange.Columns.AutoFit();
      }

      private void PrintSQNValues(Excel.Worksheet worksheet, int startingBarsArrayIndex, int numToPrint, int startingExcelRow, int startingExcelColumn)
      {
         int row = startingExcelRow;
         double sqn;

         for (int i = startingBarsArrayIndex; i < startingBarsArrayIndex + numToPrint; i++)
         {
            worksheet.Cells[row, startingExcelColumn] = names[i];
            worksheet.Cells[row, startingExcelColumn + 1] = BarsArray[i].Instrument.MasterInstrument.Name;
            sqn = Math.Round(NTIMansfieldRelativeStrength(BarsArray[i],BenchMark,104)[0]*100, 2);
            worksheet.Cells[row, startingExcelColumn + 2] = sqn;
            SetSQNBackgroundColor((Excel.Range) worksheet.Cells[row, startingExcelColumn + 2], sqn);
            sqn = Math.Round(NTIMansfieldRelativeStrength(BarsArray[i],BenchMark,52)[0]*100, 2);
            worksheet.Cells[row, startingExcelColumn + 3] = sqn;
            SetSQNBackgroundColor((Excel.Range)worksheet.Cells[row, startingExcelColumn + 3], sqn);
            sqn =Math.Round(NTIMansfieldRelativeStrength(BarsArray[i],BenchMark,26)[0]*100, 2);
            worksheet.Cells[row, startingExcelColumn + 4] = sqn;
            SetSQNBackgroundColor((Excel.Range)worksheet.Cells[row, startingExcelColumn + 4], sqn);
            sqn = Math.Round(NTIMansfieldRelativeStrength(BarsArray[i],BenchMark,13)[0]*100, 2);
            worksheet.Cells[row, startingExcelColumn + 5] = sqn;
            SetSQNBackgroundColor((Excel.Range)worksheet.Cells[row, startingExcelColumn + 5], sqn);

            row++;
         }
      }
      private string GetSQNStringValue( double sqnValue){
		
		if (sqnValue<-50) return "Strong Bear";
		if (sqnValue<-15) return "Bear";
		if (sqnValue>50) return "Strong Bull";
		if (sqnValue>15) return "Bull";
		return "Neutral";
	}
      private void SetSQNBackgroundColor(Excel.Range cell, double sqnValue)
      {
         string sqnStringValue = GetSQNStringValue(sqnValue);
         if (sqnStringValue == "Strong Bear")
         {
            cell.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(180, 0, 0));
            //cell.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Red);
            cell.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Yellow);
         }
         else if (sqnStringValue == "Bear")
         {
            cell.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(140, 51, 57));
            //cell.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Firebrick);
            cell.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Yellow);
         }
         else if (sqnStringValue == "Neutral")
         {
            cell.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Yellow);
            cell.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Black);
         }
         else if (sqnStringValue == "Bull")
         {
            cell.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(113, 142, 50));
            //cell.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Olive);
            cell.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Yellow);
         }
         else if (sqnStringValue == "Strong Bull")
         {
            cell.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.FromArgb(0, 132, 2));
            //cell.Interior.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Green);
            cell.Font.Color = System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Yellow);
         }
      }

      #region Properties
      [Description("The directory that the output data is written to")]
      [GridCategory("Parameters")]
      
      public string OutputFolder
      {
         get { return outputFolder; }
         set { outputFolder = value; }
      }
      [Description("Benchmark Index")]
      [GridCategory("Parameters")]
      
      public string BenchMark
      {
         get { return benchMark; }
         set { benchMark = value; }
      }
	
      #endregion
   }
}
