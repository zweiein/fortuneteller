// #############################################################
// #														   #
// #                     PriceActionSwing                      #
// #														   #
// #     13.07.2011 by dorschden, die.unendlichkeit@gmx.de     #
// #														   #
// #############################################################
// 

#region Updates
// Version 1.5.0 - 21.03.2011
// - Add Gann swing calculation
// - Remove PSAR and ZigZagUTC swing calculaton
// - Add Gann style for the visualization
// - Add public swingTrend series - could use in other indicators or strategies
// - Unique entry arrow ID for each entry
// Version 12 - 12.07.2011
// - Add risk management (auto risk calculation for ABC pattern)
// - Optimize for market analyzer
// - Alerts for ABC pattern
// Version 13 - 13.07.2011
// - Bug fix - check if indicator is applied to a chart
#endregion

#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using PriceActionSwing.Utility;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    /// <summary>
    /// PriceActionSwing calculate swings, visualize them in different ways
    /// and display several information about them. Features: 
    /// ABC pattern recognition | Risk management | Fibonacci retracements/-extensions | Statistic
    /// </summary>
    [Description("PriceActionSwing calculate swings, visualize them in different ways and display several information about them. Features: ABC pattern recognition | Risk management | Fibonacci retracements/-extensions | Statistic")]
    public class PriceActionSwing : Indicator
    {
        #region Variables
        //#####################################################################
        #region ABC pattern
        //=====================================================================
        private int abcEntryTag = 0;
        private bool abcLongChanceInProgress = false;
        private bool abcShortChanceInProgress = false;
        private double retracementEntryValue = 38.0;
        private double entryLevel = 0.0;
        private int entryLineStartBar = 0;
        private int aBar = 0;
        private int bBar = 0;
        private int cBar = 0;
        private int entryBar = 0;
        private int patternCounter = 0;
        private int tmpCounter = 0;
        private int drawTag = 0;
        //=====================================================================
        #endregion

        #region Alerts
        //=====================================================================
        private int alertTag = 0;
        private bool useAbcAlerts = false;
        private bool useAbcEntryAlerts = true;
        private NinjaTrader.Cbi.Priority priorityAbc = NinjaTrader.Cbi.Priority.Medium;
        private NinjaTrader.Cbi.Priority priorityAbcEntry = NinjaTrader.Cbi.Priority.High;
        private string fileNameAbcLong = "ABC_Long.wav";
        private string fileNameAbcLongEntry = "ABC_Long_Entry.wav";
        private string fileNameAbcShort = "ABC_Short.wav";
        private string fileNameAbcShortEntry = "ABC_Short_Entry.wav";
        //=====================================================================
        #endregion

        #region Parameters
        //=====================================================================
        private int swingSize = 7;
        private SwingTypes swingType = SwingTypes.Standard;
        private int dtbStrength = 15;
        //=====================================================================
        #endregion

        #region DataSeries
        // Public =============================================================
        private DataSeries swingTrend;
        private DataSeries swingRelation;
        private BoolSeries upFlip;
        private BoolSeries dnFlip;
        // Private ============================================================
        private DataSeries abcSignals;
        private DataSeries entryLong;
        private DataSeries entryShort;
        private DataSeries entryLevelLine;
        private List<Swings> swingHighs = new List<Swings>();
        private List<Swings> swingLows = new List<Swings>();
        //=====================================================================
        #endregion

        #region Display
        //=====================================================================
        private bool showSwingDuration = false;
        private bool showSwingLabel = true;
        private bool showSwingPercent = false;
        private bool showSwingTime = false;
        private SwingVolumeTypes swingVolumeType = SwingVolumeTypes.False;
        private VisualizationTypes visualizationType = VisualizationTypes.DotsAndLines;
        private SwingLengthTypes swingLengthType = SwingLengthTypes.Price_And_Points;
        //=====================================================================
        #endregion

        #region Enums
        //=====================================================================
        public enum AbcPatternMode
        {
            False,
            Long_And_Short,
            Long,
            Short,
        }
        public enum BarType
        {
            UpBar,
            DnBar,
            InsideBar,
            OutsideBar,
        }
        public enum Relation
        {
            Double,
            Higher,
            Lower,
        }
        public enum StatisticPositions
        {
#if NT7
            Bottom,
            False,
            Left,
            Right,
            Top,
#else
            BottomLeft,
            BottomRight,
            False,
            TopLeft,
            TopRight,
#endif
        }

        /// <summary>
        /// Represents the possible swing length outputs.
        /// </summary>
        public enum SwingLengthTypes
        {
            /// <summary>
            /// Swing length is disabled.
            /// </summary>
            False,
            /// <summary>
            /// Swing length in points.
            /// </summary>
            Points,
            /// <summary>
            /// Swing price.
            /// </summary>
            Price,
            /// <summary>
            /// Swing length in percent.
            /// </summary>
            /// <returns></returns>
            Percent,
            /// <summary>
            /// Swing price and swing length in points.
            /// </summary>
            Price_And_Points,
            /// <summary>
            /// Swing price and swing length in ticks.
            /// </summary>
            Price_And_Ticks,
            /// <summary>
            /// Swing length in ticks.
            /// </summary>
            Ticks,
            /// <summary>
            /// Swing length in ticks and swing price.
            /// </summary>
            /// <returns></returns>
            Ticks_And_Price,
        }

        /// <summary>
        /// Represents the possible swing visualizations. 
        /// Dots | Zig-Zag lines | Dots and lines | Gann style
        /// </summary>
        public enum VisualizationTypes
        {
            /// <summary>
            /// Visualize swings with dots.
            /// </summary>
            Dots,
            /// <summary>
            /// Visualize swings with zigzag lines.
            /// </summary>
            ZigZagLines,
            /// <summary>
            /// Visualize swings with dots and zigzag lines.
            /// </summary>
            DotsAndLines,
            /// <summary>
            /// Visualize swings with zigzag lines.
            /// </summary>
            GannStyle,
        }

        /// <summary>
        /// Represents the possible swing volume outputs. 
        /// Absolute | Relativ | False
        /// </summary>
        public enum SwingVolumeTypes
        {
            /// <summary>
            /// Absolue swing volume.
            /// </summary>
            Absolute,
            /// <summary>
            /// Relative swing volume.
            /// </summary>
            Relativ,
            /// <summary>
            /// No swing volume.
            /// </summary>
            False,
        }
        //=====================================================================
        #endregion

        #region Forms
        // Statistic forms ========================================================================
        private System.Windows.Forms.Panel panel = null;
        private System.Windows.Forms.Label label = null;
        private System.Windows.Forms.TabControl mainTabControl = null;
        private System.Windows.Forms.TabPage tabABC = null;
        private System.Windows.Forms.TabPage tabSwingLength = null;
        private System.Windows.Forms.TabPage tabSwingRelation = null;
        private System.Windows.Forms.DataGridView lengthList = null;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLengthDirection = null;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLengthSwingCount = null;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLengthLength = null;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLengthLastLength = null;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLengthDuration = null;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLengthLastDuration = null;
        private System.Windows.Forms.DataGridView relationList = null;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRelationSwing = null;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRelationSwingCount = null;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRelationHigherHigh = null;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRelationLowerHigh = null;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRelationHigherLow = null;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRelationLowerLow = null;
        private System.Windows.Forms.Splitter splitter = null;
        // Necessary toolstrip items forms ========================================================
        System.Windows.Forms.ToolStrip toolStrip = null;
        System.Windows.Forms.ToolStripButton toolStripButton = null;
        System.Windows.Forms.ToolStripSeparator toolStripSeparator = null;
        // Risk calculation forms =================================================================
        System.Windows.Forms.ToolStripSeparator toolStripSeparator1 = null;
        System.Windows.Forms.ToolStripSeparator toolStripSeparator2 = null;
        System.Windows.Forms.ToolStripSeparator toolStripSeparator3 = null;
        System.Windows.Forms.ToolStripLabel toolStripLabelQty = null;
        System.Windows.Forms.ToolStripLabel toolStripLabelRiskReward = null;
        System.Windows.Forms.ToolStripLabel toolStripLabelLoss = null;
        System.Windows.Forms.ToolStripLabel toolStripLabelProfit = null;
        System.Windows.Forms.ToolStripLabel toolStripLabelEntry = null;
        System.Windows.Forms.ToolStripLabel toolStripLabelStop = null;
        System.Windows.Forms.ToolStripLabel toolStripLabelTarget = null;
        System.Windows.Forms.NumericUpDown numericUpDownEntry;
        System.Windows.Forms.NumericUpDown numericUpDownStop;
        System.Windows.Forms.NumericUpDown numericUpDownTarget;
        System.Windows.Forms.ToolStripControlHost toolStripControlHostEntry = null;
        System.Windows.Forms.ToolStripControlHost toolStripControlHostStop = null;
        System.Windows.Forms.ToolStripControlHost toolStripControlHostTarget = null;
        //=========================================================================================
        #endregion

        #region Features
        //=====================================================================
        private AbcPatternMode abcPattern = AbcPatternMode.Long_And_Short;
        private bool addFibExt = false;
        private bool addFastFibRet = false;
        private bool addSlowFibRet = false;
        private bool ignoreSwings = true;
        // Statistic should set to "False" if other indicators/strategies use 
        // the PriceActionSwing indicator
        private StatisticPositions statisticPosition = StatisticPositions.False;
        private int statisticLength = 5;
        private bool showRiskManagement = true;
        private double accountSize = 100000.00;
        private double accountRisk = 1.0;
        private double abcTarget = 100.0;
        //=====================================================================
        #endregion

        #region Gann Swings
        // Bars ===============================================================
        private BarType barType = BarType.InsideBar;
        private int consecutiveBars = 0;
        private int consecutiveBarNumber = 0;
        private double consecutiveBarValue = 0;
        private bool paintBars = false;
        private Color dnBarColour = Color.Red;
        private Color upBarColour = Color.Lime;
        private Color insideBarColour = Color.Black;
        private Color outsideBarColour = Color.Blue;
        // Swings =============================================================
        private bool useBreakouts = true;
        private bool ignoreInsideBars = true;
        //=====================================================================
        private bool stopOutsideBarCalc = false;
        //=====================================================================
        #endregion

        #region Statistic
        //===================================================================
        private double overallAvgDnLength = 0;
        private double overallAvgUpLength = 0;
        private double overallUpLength = 0;
        private double overallDnLength = 0;
        private double overallAvgDnDuration = 0;
        private double overallAvgUpDuration = 0;
        private double overallUpDuration = 0;
        private double overallDnDuration = 0;

        private double avgUpLength = 0;
        private double avgDnLength = 0;
        private double upLength = 0;
        private double dnLength = 0;
        private double avgUpDuration = 0;
        private double avgDnDuration = 0;
        private double upDuration = 0;
        private double dnDuration = 0;

        // Variables for the swing to swing relation statistic ================
        private int hhCount = 0;
        private int hhCountHH = 0;
        private double hhCountHHPercent = 0;
        private int hhCountLH = 0;
        private double hhCountLHPercent = 0;
        private int hhCountHL = 0;
        private double hhCountHLPercent = 0;
        private int hhCountLL = 0;
        private double hhCountLLPercent = 0;

        private int lhCount = 0;
        private int lhCountHH = 0;
        private double lhCountHHPercent = 0;
        private int lhCountLH = 0;
        private double lhCountLHPercent = 0;
        private int lhCountHL = 0;
        private double lhCountHLPercent = 0;
        private int lhCountLL = 0;
        private double lhCountLLPercent = 0;

        private int dtCount = 0;
        private int dtCountHH = 0;
        private double dtCountHHPercent = 0;
        private int dtCountLH = 0;
        private double dtCountLHPercent = 0;
        private int dtCountHL = 0;
        private double dtCountHLPercent = 0;
        private int dtCountLL = 0;
        private double dtCountLLPercent = 0;

        private int llCount = 0;
        private int llCountHH = 0;
        private double llCountHHPercent = 0;
        private int llCountLH = 0;
        private double llCountLHPercent = 0;
        private int llCountHL = 0;
        private double llCountHLPercent = 0;
        private int llCountLL = 0;
        private double llCountLLPercent = 0;

        private int hlCount = 0;
        private int hlCountHH = 0;
        private double hlCountHHPercent = 0;
        private int hlCountLH = 0;
        private double hlCountLHPercent = 0;
        private int hlCountHL = 0;
        private double hlCountHLPercent = 0;
        private int hlCountLL = 0;
        private double hlCountLLPercent = 0;

        private int dbCount = 0;
        private int dbCountHH = 0;
        private double dbCountHHPercent = 0;
        private int dbCountLH = 0;
        private double dbCountLHPercent = 0;
        private int dbCountHL = 0;
        private double dbCountHLPercent = 0;
        private int dbCountLL = 0;
        private double dbCountLLPercent = 0;
        //===================================================================
        #endregion

        #region Swing values
        // Swing calculation ======================================================================
        private int decimalPlaces = 0;
        private int bar = 0;
        private double price = 0.0;
        private int newSwing = 0;
        private bool newHigh = false;
        private bool newLow = false;
        private bool updateHigh = false;
        private bool updateLow = false;
        private double signalBarVolumeDn = 0;
        private double signalBarVolumeUp = 0;
        private int swingCounterDn = 0;
        private int swingCounterUp = 0;
        private int upCount = 0;
        private int dnCount = 0;
        private int trendChangeBar = 0;
        private int swingSlope = 0;
        // Swing values ===========================================================================
        private double curHigh = 0.0;
        private int curHighBar = 0;
        private int curHighDuration = 0;
        private int curHighLength = 0;
        private double curHighPercent = 0.0;
        private int curHighTime = 0;
        private Relation curHighRelation = Relation.Higher;
        private double curLow = 0.0;
        private int curLowBar = 0;
        private int curLowDuration = 0;
        private int curLowLength = 0;
        private double curLowPercent = 0.0;
        private int curLowTime = 0;
        private Relation curLowRelation = Relation.Higher;
        private double lastHigh = 0.0;
        private int lastHighBar = 0;
        private int lastHighDuration = 0;
        private int lastHighLength = 0;
        private double lastHighPercent = 0.0;
        private int lastHighTime = 0;
        private Relation lastHighRelation = Relation.Higher;
        private double lastLow = 0.0;
        private int lastLowBar = 0;
        private int lastLowDuration = 0;
        private int lastLowLength = 0;
        private double lastLowPercent = 0.0;
        private int lastLowTime = 0;
        private Relation lastLowRelation = Relation.Higher;
        //=========================================================================================
        #endregion

        #region Visualize patterns
        //===================================================================
        private bool abcLabel = true;
        private DashStyle abcLineStyle = DashStyle.Solid;
        private DashStyle abcLineStyleRatio = DashStyle.Dash;
        private int abcLineWidth = 4;
        private int abcLineWidthRatio = 2;
        private Font abcTextFont = new Font("Courier", 11, FontStyle.Bold);
        private int abcTextOffsetLabel = 50;
        private Color abcTextColourDn = Color.Red;
        private Color abcTextColourUp = Color.Green;
        private Color abcZigZagColourDn = Color.Red;
        private Color abcZigZagColourUp = Color.Green;
        private double abcMaxRetracement = 92.0;
        private double abcMinRetracement = 61.0;
        private DashStyle entryLineStyle = DashStyle.Solid;
        private int entryLineWidth = 4;
        private Color entryLineColourDn = Color.Red;
        private Color entryLineColourUp = Color.Green;
        private bool showEntryArrows = true;
        private bool showEntryLine = true;
        private bool showHistoricalEntryLine = true;
        private int yTickOffset = 5;
        //===================================================================
        #endregion

        #region Visualize swings
        //=====================================================================
        private Color textColourHigher = Color.Green;
        private Color textColorLower = Color.Red;
        private Color textColourDtb = Color.Gold;
        private Font textFont = new Font("Courier", 9, FontStyle.Regular);
        private int textOffsetLength = 15;
        private int textOffsetPercent = 45;
        private int textOffsetLabel = 30;
        private int textOffsetVolume = 60;
        private int textOffsetTime = 75;
        private Color zigZagColourUp = Color.Green;
        private Color zigZagColourDn = Color.Red;
        private DashStyle zigZagStyle = DashStyle.Dash;
        private int zigZagWidth = 1;
        //=====================================================================
        #endregion

        #region Swing struct
        //===================================================================
        private struct Swings
        {
            public int barNumber;
            public double price;
            public int length;
            public int duration;
            public Relation relation;

            public Swings(int swingBarNumber, double swingPrice,
                    int swingLength, int swingDuration, Relation swingRelation)
            {
                barNumber = swingBarNumber;
                price = swingPrice;
                length = swingLength;
                duration = swingDuration;
                relation = swingRelation;
            }
        }
        //===================================================================
        #endregion
        //#####################################################################
        #endregion

        #region Initialize()
        //###################################################################
        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override void Initialize()
        {
            Add(new Plot(new Pen(Color.Gold, 3), PlotStyle.Dot, "DoubleBottom"));
            Add(new Plot(new Pen(Color.Red, 3), PlotStyle.Dot, "LowerLow"));
            Add(new Plot(new Pen(Color.Green, 3), PlotStyle.Dot, "HigherLow"));

            Add(new Plot(new Pen(Color.Gold, 3), PlotStyle.Dot, "DoubleTop"));
            Add(new Plot(new Pen(Color.Red, 3), PlotStyle.Dot, "LowerHigh"));
            Add(new Plot(new Pen(Color.Green, 3), PlotStyle.Dot, "HigherHigh"));

            Add(new Plot(new Pen(Color.Blue, 1), PlotStyle.Square, "GannSwing"));

            AutoScale = true;
            BarsRequired = 2;
            CalculateOnBarClose = false;
            Overlay = true;
            PriceTypeSupported = false;

            swingTrend = new DataSeries(this);
            swingRelation = new DataSeries(this);
            dnFlip = new BoolSeries(this);
            upFlip = new BoolSeries(this);
            abcSignals = new DataSeries(this);
            entryLong = new DataSeries(this);
            entryShort = new DataSeries(this);
            entryLevelLine = new DataSeries(this);
        }
        //###################################################################
        #endregion

        #region Create forms
#if NT7
        //#####################################################################
        /// <summary>
        /// This method is used to initialize any variables or resources. 
        /// This method is called only once immediately prior to the start of the script, 
        /// but after the Initialize() method.
        /// </summary>
        protected override void OnStartUp()
        {
            base.OnStartUp();

            // Calculate increment value and decimal places for the numericUpDown =============
            decimal increment = Convert.ToDecimal(Instrument.MasterInstrument.TickSize);
            int incrementLength = increment.ToString().Length;
            decimalPlaces = 0;
            if (incrementLength == 1)
                decimalPlaces = 0;
            else if (incrementLength > 2)
                decimalPlaces = incrementLength - 2;

            if (ChartControl == null)
            {
                statisticPosition = StatisticPositions.False;
                showRiskManagement = false;
            }

            if (statisticPosition != StatisticPositions.False || showRiskManagement)
                toolStrip = (ToolStrip)ChartControl.Controls["tsrTool"];

                #region Statistic panel
                // Statistic panel ====================================================================
                if (statisticPosition != StatisticPositions.False)
                {
                    DockStyle dockStyle = DockStyle.Bottom;
                    switch (statisticPosition)
                    {
                        case StatisticPositions.Bottom:
                            dockStyle = DockStyle.Bottom;
                            break;
                        case StatisticPositions.Left:
                            dockStyle = DockStyle.Left;
                            break;
                        case StatisticPositions.Right:
                            dockStyle = DockStyle.Right;
                            break;
                        case StatisticPositions.Top:
                            dockStyle = DockStyle.Top;
                            break;
                        default:
                            dockStyle = DockStyle.Bottom;
                            break;
                    }

                    #region Create new control objects
                    // Create new control objects =====================================================
                    System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
                    this.panel = new System.Windows.Forms.Panel();
                    this.label = new System.Windows.Forms.Label();
                    this.mainTabControl = new System.Windows.Forms.TabControl();
                    this.tabABC = new System.Windows.Forms.TabPage();
                    this.tabSwingLength = new System.Windows.Forms.TabPage();
                    this.tabSwingRelation = new System.Windows.Forms.TabPage();
                    this.lengthList = new System.Windows.Forms.DataGridView();
                    this.colLengthDirection = new System.Windows.Forms.DataGridViewTextBoxColumn();
                    this.colLengthSwingCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
                    this.colLengthLength = new System.Windows.Forms.DataGridViewTextBoxColumn();
                    this.colLengthLastLength = new System.Windows.Forms.DataGridViewTextBoxColumn();
                    this.colLengthDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
                    this.colLengthLastDuration = new System.Windows.Forms.DataGridViewTextBoxColumn();
                    this.relationList = new System.Windows.Forms.DataGridView();
                    this.colRelationSwing = new System.Windows.Forms.DataGridViewTextBoxColumn();
                    this.colRelationSwingCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
                    this.colRelationHigherHigh = new System.Windows.Forms.DataGridViewTextBoxColumn();
                    this.colRelationLowerHigh = new System.Windows.Forms.DataGridViewTextBoxColumn();
                    this.colRelationHigherLow = new System.Windows.Forms.DataGridViewTextBoxColumn();
                    this.colRelationLowerLow = new System.Windows.Forms.DataGridViewTextBoxColumn();
                    //=================================================================================
                    #endregion

                    #region Tab Swing Length
                    //===== Tab Swing Length ==========================================================
                    // 
                    // colLengthDirection
                    // 
                    this.colLengthDirection.HeaderText = "Direction";
                    this.colLengthDirection.Name = "colLengthDirection";
                    this.colLengthDirection.ReadOnly = true;
                    this.colLengthDirection.Width = 120;
                    // 
                    // colLengthSwingCount
                    // 
                    this.colLengthSwingCount.HeaderText = "Swing Count";
                    this.colLengthSwingCount.Name = "colLengthSwingCount";
                    this.colLengthSwingCount.ReadOnly = true;
                    this.colLengthSwingCount.Width = 120;
                    // 
                    // colLengthLength
                    // 
                    this.colLengthLength.HeaderText = "Length";
                    this.colLengthLength.Name = "colLengthLength";
                    this.colLengthLength.ReadOnly = true;
                    this.colLengthLength.Width = 120;
                    // 
                    // colLengthLastLength
                    // 
                    this.colLengthLastLength.HeaderText = "Last Length";
                    this.colLengthLastLength.Name = "colLengthLastLength";
                    this.colLengthLastLength.ReadOnly = true;
                    this.colLengthLastLength.Width = 120;
                    // 
                    // colLengthDuration
                    // 
                    this.colLengthDuration.HeaderText = "Duration";
                    this.colLengthDuration.Name = "colLengthDuration";
                    this.colLengthDuration.ReadOnly = true;
                    this.colLengthDuration.Width = 120;
                    // 
                    // colLengthLastDuration
                    // 
                    this.colLengthLastDuration.HeaderText = "Last Duration";
                    this.colLengthLastDuration.Name = "colLengthLastDuration";
                    this.colLengthLastDuration.ReadOnly = true;
                    this.colLengthLastDuration.Width = 120;
                    // 
                    // lengthList
                    // 
                    this.lengthList.AllowUserToAddRows = false;
                    this.lengthList.AllowUserToDeleteRows = false;
                    this.lengthList.AllowUserToResizeColumns = false;
                    this.lengthList.AllowUserToResizeRows = false;
                    this.lengthList.BackgroundColor = System.Drawing.SystemColors.Control;
                    this.lengthList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
                    this.lengthList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
				this.colLengthDirection,
				this.colLengthSwingCount,
				this.colLengthLength,
				this.colLengthLastLength,
				this.colLengthDuration,
				this.colLengthLastDuration});
                    dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
                    dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
                    dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                    dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
                    dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
                    dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
                    dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
                    this.lengthList.DefaultCellStyle = dataGridViewCellStyle1;
                    this.lengthList.Dock = System.Windows.Forms.DockStyle.Fill;
                    this.lengthList.Location = new System.Drawing.Point(3, 3);
                    this.lengthList.Name = "lengthList";
                    this.lengthList.ReadOnly = true;
                    this.lengthList.Rows.Add("Up");
                    this.lengthList.Rows.Add("Down");
                    this.lengthList.RowHeadersVisible = false;
                    this.lengthList.RowTemplate.Height = 24;
                    this.lengthList.Size = new System.Drawing.Size(920, 305);
                    this.lengthList.TabIndex = 0;
                    // 
                    // tabSwingLength
                    // 
                    this.tabSwingLength.Controls.Add(this.lengthList);
                    this.tabSwingLength.Location = new System.Drawing.Point(4, 25);
                    this.tabSwingLength.Name = "tabSwingLength";
                    this.tabSwingLength.Padding = new System.Windows.Forms.Padding(3);
                    this.tabSwingLength.TabIndex = 1;
                    this.tabSwingLength.Text = "Swing Length";
                    this.tabSwingLength.UseVisualStyleBackColor = true;
                    //=================================================================================
                    #endregion

                    #region Tab Swing Relation
                    //===== Tab Swing Relation ========================================================
                    // 
                    // colRelationSwing
                    // 
                    this.colRelationSwing.HeaderText = "Swing";
                    this.colRelationSwing.Name = "colRelationSwing";
                    this.colRelationSwing.ReadOnly = true;
                    this.colRelationSwing.Width = 120;
                    // 
                    // colRelationSwingCount
                    // 
                    this.colRelationSwingCount.HeaderText = "Swing Count";
                    this.colRelationSwingCount.Name = "colRelationSwingCount";
                    this.colRelationSwingCount.ReadOnly = true;
                    this.colRelationSwingCount.Width = 120;
                    // 
                    // colRelationHigherHigh
                    // 
                    this.colRelationHigherHigh.HeaderText = "Higher High";
                    this.colRelationHigherHigh.Name = "colRelationHigherHigh";
                    this.colRelationHigherHigh.ReadOnly = true;
                    this.colRelationHigherHigh.Width = 120;
                    // 
                    // colRelationLowerHigh
                    // 
                    this.colRelationLowerHigh.HeaderText = "Lower High";
                    this.colRelationLowerHigh.Name = "colRelationLowerHigh";
                    this.colRelationLowerHigh.ReadOnly = true;
                    this.colRelationLowerHigh.Width = 120;
                    // 
                    // colRelationHigherLow
                    // 
                    this.colRelationHigherLow.HeaderText = "Higher Low";
                    this.colRelationHigherLow.Name = "colRelationHigherLow";
                    this.colRelationHigherLow.ReadOnly = true;
                    this.colRelationHigherLow.Width = 120;
                    // 
                    // colRelationLowerLow
                    // 
                    this.colRelationLowerLow.HeaderText = "Lower Low";
                    this.colRelationLowerLow.Name = "colRelationLowerLow";
                    this.colRelationLowerLow.ReadOnly = true;
                    this.colRelationLowerLow.Width = 120;
                    // 
                    // relationList
                    // 
                    this.relationList.AllowUserToAddRows = false;
                    this.relationList.AllowUserToDeleteRows = false;
                    this.relationList.AllowUserToResizeColumns = false;
                    this.relationList.AllowUserToResizeRows = false;
                    this.relationList.BackgroundColor = System.Drawing.SystemColors.Control;
                    this.relationList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
                    this.relationList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
				this.colRelationSwing,
				this.colRelationSwingCount,
				this.colRelationHigherHigh,
				this.colRelationLowerHigh,
				this.colRelationHigherLow,
				this.colRelationLowerLow});
                    this.relationList.DefaultCellStyle = dataGridViewCellStyle1;
                    this.relationList.Dock = System.Windows.Forms.DockStyle.Fill;
                    this.relationList.Location = new System.Drawing.Point(3, 3);
                    this.relationList.Name = "relationList";
                    this.relationList.ReadOnly = true;
                    this.relationList.Rows.Add("Higher High");
                    this.relationList.Rows.Add("Lower High");
                    this.relationList.Rows.Add("Double Top");
                    this.relationList.Rows.Add("Lower Low");
                    this.relationList.Rows.Add("Higher Low");
                    this.relationList.Rows.Add("Double Bottom");
                    this.relationList.RowHeadersVisible = false;
                    this.relationList.RowTemplate.Height = 24;
                    this.relationList.Size = new System.Drawing.Size(986, 240);
                    this.relationList.TabIndex = 1;
                    // 
                    // tabSwingRelation
                    // 
                    this.tabSwingRelation.Controls.Add(this.relationList);
                    this.tabSwingRelation.Location = new System.Drawing.Point(4, 25);
                    this.tabSwingRelation.Name = "tabSwingRelation";
                    this.tabSwingRelation.Padding = new System.Windows.Forms.Padding(3);
                    this.tabSwingRelation.TabIndex = 2;
                    this.tabSwingRelation.Text = "Swing Relation";
                    this.tabSwingRelation.UseVisualStyleBackColor = true;
                    //=================================================================================
                    #endregion

                    #region Add button to the tool strip and create main controls
                    //===== Add button to the tool strip ==============================================
                    // 
                    // toolStripSeparator
                    // 
                    toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
                    toolStripSeparator.Name = "separator";
                    // 
                    // Add controls to the toolstrip
                    // 
                    toolStrip.Items.Add(toolStripSeparator);

                    toolStripButton = new System.Windows.Forms.ToolStripButton("Hide Extras");
                    toolStripButton.Name = "button";
                    toolStripButton.Text = "Show Statistic";
                    toolStripButton.Click += buttonClick;
                    toolStripButton.Enabled = true;
                    toolStripButton.ForeColor = Color.Black;
                    toolStrip.Items.Add(toolStripButton);

                    this.splitter = new System.Windows.Forms.Splitter();
                    this.splitter.Name = "splitter";
                    this.splitter.BackColor = Color.LightGray;
                    this.splitter.Dock = dockStyle;
                    this.splitter.Hide();
                    this.splitter.Width = 3;
                    ChartControl.Controls.Add(this.splitter);
                    //===== Create panel and main tab =================================================
                    this.panel = new System.Windows.Forms.Panel();
                    this.mainTabControl = new System.Windows.Forms.TabControl();
                    // 
                    // mainTabControl
                    // 
                    this.mainTabControl.Controls.Add(this.tabSwingLength);
                    this.mainTabControl.Controls.Add(this.tabSwingRelation);
                    this.mainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
                    this.mainTabControl.Location = new System.Drawing.Point(0, 0);
                    this.mainTabControl.Multiline = true;
                    this.mainTabControl.Name = "mainTabControl";
                    this.mainTabControl.Padding = new System.Drawing.Point(3, 3);
                    this.mainTabControl.SelectedIndex = 3;
                    this.mainTabControl.TabIndex = 0;
                    // 
                    // panel
                    // 
                    this.panel.BackColor = System.Drawing.Color.White;
                    this.panel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
                    this.panel.Controls.Add(this.label);
                    this.panel.Controls.Add(this.mainTabControl);
                    this.panel.Dock = dockStyle;
                    this.panel.Location = new System.Drawing.Point(0, 0);
                    this.panel.MinimumSize = new System.Drawing.Size(150, 0);
                    this.panel.Name = "panel";
                    this.panel.Hide();
                    if (dockStyle == DockStyle.Bottom || dockStyle == DockStyle.Top)
                        this.panel.Size = new System.Drawing.Size(ChartControl.Width, 210);
                    else
                        this.panel.Size = new System.Drawing.Size(250, ChartControl.Height);
                    this.panel.TabIndex = 0;
                    ChartControl.Controls.Add(this.panel);
                    //=================================================================================
                    #endregion
                }
                // End statistic panel ================================================================
                #endregion

                #region Riskmanagement items
                // Riskmanagement items ===============================================================
                if (showRiskManagement)
                {
                    #region Create new control objects
                    // Create new control objects =====================================================
                    toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
                    toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
                    toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
                    toolStripLabelQty = new System.Windows.Forms.ToolStripLabel("toolStripLabelQty");
                    toolStripLabelRiskReward = new System.Windows.Forms.ToolStripLabel("toolStripLabelRiskReward");
                    toolStripLabelLoss = new System.Windows.Forms.ToolStripLabel("toolStripLabelLoss");
                    toolStripLabelProfit = new System.Windows.Forms.ToolStripLabel("toolStripLabelProfit");
                    toolStripLabelEntry = new System.Windows.Forms.ToolStripLabel("toolStripLabelEntry");
                    toolStripLabelStop = new System.Windows.Forms.ToolStripLabel("toolStripLabelStop");
                    toolStripLabelTarget = new System.Windows.Forms.ToolStripLabel("toolStripLabelTarget");
                    numericUpDownEntry = new System.Windows.Forms.NumericUpDown();
                    numericUpDownStop = new System.Windows.Forms.NumericUpDown();
                    numericUpDownTarget = new System.Windows.Forms.NumericUpDown();
                    //=================================================================================
                    #endregion

                    #region Add button to the tool strip and create main controls
                    // Properties of ToolStrip items ==================================================
                    // 
                    // toolStripSeparator
                    // 
                    toolStripSeparator1.Name = "toolStripSeparator1";
                    toolStripSeparator2.Name = "toolStripSeparator2";
                    toolStripSeparator3.Name = "toolStripSeparator3";
                    // 
                    // toolStripLabelQty
                    // 
                    toolStripLabelQty.AutoSize = false;
                    toolStripLabelQty.ForeColor = System.Drawing.Color.Black;
                    toolStripLabelQty.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
                    toolStripLabelQty.Name = "toolStripLabelQty";
                    toolStripLabelQty.Size = new System.Drawing.Size(126, 22);
                    toolStripLabelQty.Text = "Qty: ";
                    toolStripLabelQty.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                    // 
                    // toolStripLabelRiskReward
                    // 
                    toolStripLabelRiskReward.AutoSize = false;
                    toolStripLabelRiskReward.ForeColor = System.Drawing.Color.Black;
                    toolStripLabelRiskReward.Name = "toolStripLabelRiskReward";
                    toolStripLabelRiskReward.Size = new System.Drawing.Size(78, 22);
                    toolStripLabelRiskReward.Text = "R/R: ";
                    toolStripLabelRiskReward.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                    // 
                    // toolStripLabelLoss
                    // 
                    toolStripLabelLoss.AutoSize = false;
                    toolStripLabelLoss.ForeColor = System.Drawing.Color.Black;
                    toolStripLabelLoss.Name = "toolStripLabelLoss";
                    toolStripLabelLoss.Size = new System.Drawing.Size(96, 22);
                    toolStripLabelLoss.Text = "Loss: ";
                    toolStripLabelLoss.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                    // 
                    // toolStripLabelProfit
                    // 
                    toolStripLabelProfit.AutoSize = false;
                    toolStripLabelProfit.ForeColor = System.Drawing.Color.Black;
                    toolStripLabelProfit.Name = "toolStripLabelProfit";
                    toolStripLabelProfit.Size = new System.Drawing.Size(96, 22);
                    toolStripLabelProfit.Text = "Win: ";
                    toolStripLabelProfit.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                    // 
                    // toolStripLabelEntry
                    // 
                    toolStripLabelEntry.AutoSize = true;
                    toolStripLabelEntry.ForeColor = System.Drawing.Color.Black;
                    toolStripLabelEntry.Name = "toolStripLabelEntry";
                    toolStripLabelEntry.Text = "Entry: ";
                    // 
                    // toolStripLabelStop
                    // 
                    toolStripLabelStop.AutoSize = true;
                    toolStripLabelStop.ForeColor = System.Drawing.Color.Red;
                    toolStripLabelStop.Name = "toolStripLabelStop";
                    toolStripLabelStop.Text = " SL: ";
                    // 
                    // toolStripLabelTarget
                    // 
                    toolStripLabelTarget.AutoSize = true;
                    toolStripLabelTarget.ForeColor = System.Drawing.Color.Green;
                    toolStripLabelTarget.Name = "toolStripLabelTarget";
                    toolStripLabelTarget.Text = " PT: ";
                    // 
                    // numericUpDownEntry
                    // 
                    numericUpDownEntry.DecimalPlaces = decimalPlaces;
                    numericUpDownEntry.Increment = increment;
                    numericUpDownEntry.Maximum = new decimal(new int[] {
            999999,
            0,
            0,
            0});
                    numericUpDownEntry.Name = "numericUpDownEntry";
                    numericUpDownEntry.Size = new System.Drawing.Size(90, 22);
                    numericUpDownEntry.TabIndex = 0;
                    numericUpDownEntry.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
                    numericUpDownEntry.ThousandsSeparator = true;
                    numericUpDownEntry.UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left;
                    numericUpDownEntry.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
                    numericUpDownEntry.ValueChanged += new System.EventHandler(numericUpDown_ValueChanged);
                    // 
                    // numericUpDownStop
                    // 
                    numericUpDownStop.DecimalPlaces = decimalPlaces;
                    numericUpDownStop.Increment = increment;
                    numericUpDownStop.Maximum = new decimal(new int[] {
            999999,
            0,
            0,
            0});
                    numericUpDownStop.Name = "numericUpDownStop";
                    numericUpDownStop.Size = new System.Drawing.Size(90, 22);
                    numericUpDownStop.TabIndex = 10;
                    numericUpDownStop.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
                    numericUpDownStop.ThousandsSeparator = true;
                    numericUpDownStop.UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left;
                    numericUpDownStop.Value = new decimal(new int[] {
            995,
            0,
            0,
            0});
                    numericUpDownStop.ValueChanged += new System.EventHandler(numericUpDown_ValueChanged);
                    // 
                    // numericUpDownTarget
                    // 
                    numericUpDownTarget.DecimalPlaces = decimalPlaces;
                    numericUpDownTarget.Increment = increment;
                    numericUpDownTarget.Maximum = new decimal(new int[] {
            999999,
            0,
            0,
            0});
                    numericUpDownTarget.Name = "numericUpDownTarget";
                    numericUpDownTarget.Size = new System.Drawing.Size(90, 22);
                    numericUpDownTarget.TabIndex = 9;
                    numericUpDownTarget.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
                    numericUpDownTarget.ThousandsSeparator = true;
                    numericUpDownTarget.UpDownAlign = System.Windows.Forms.LeftRightAlignment.Left;
                    numericUpDownTarget.Value = new decimal(new int[] {
            1010,
            0,
            0,
            0});
                    numericUpDownTarget.ValueChanged += new System.EventHandler(numericUpDown_ValueChanged);
                    // 
                    // Add controls to the toolstrip
                    // 
                    toolStripControlHostEntry = new ToolStripControlHost(numericUpDownEntry, "toolStripControlHostEntry");
                    toolStripControlHostEntry.AutoSize = false;
                    toolStripControlHostStop = new ToolStripControlHost(numericUpDownStop, "toolStripControlHostStop");
                    toolStripControlHostStop.AutoSize = false;
                    toolStripControlHostTarget = new ToolStripControlHost(numericUpDownTarget, "toolStripControlHostTarget");
                    toolStripControlHostTarget.AutoSize = false;
                    toolStrip.Items.Add(toolStripSeparator1);
                    toolStrip.Items.Add(toolStripLabelQty);
                    toolStrip.Items.Add(toolStripSeparator2);
                    toolStrip.Items.Add(toolStripLabelRiskReward);
                    toolStrip.Items.Add(toolStripLabelLoss);
                    toolStrip.Items.Add(toolStripLabelProfit);
                    toolStrip.Items.Add(toolStripSeparator3);
                    toolStrip.Items.Add(toolStripLabelEntry);
                    toolStrip.Items.Add(toolStripControlHostEntry);
                    toolStrip.Items.Add(toolStripLabelStop);
                    toolStrip.Items.Add(toolStripControlHostStop);
                    toolStrip.Items.Add(toolStripLabelTarget);
                    toolStrip.Items.Add(toolStripControlHostTarget);
                    //=================================================================================
                    #endregion
                }
                // End riskmanagement items ===========================================================
                #endregion
        }

        #region buttonClick(object s, EventArgs e)
        //#####################################################################
        /// <summary>
        /// Show or hide the statistic.
        /// </summary>
        private void buttonClick(object s, EventArgs e)
        {
            if (panel.Visible)
            {
                panel.Hide();
                splitter.Hide();
            }
            else
            {
                panel.Show();
                splitter.Show();
            }
        }
        //#####################################################################
        #endregion

        #region numericUpDown_ValueChanged(object sender, EventArgs e)
        //#####################################################################
        /// <summary>
        /// Calculates the risk, if a the numericUpDown value changed .
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            double entryRM = (double)numericUpDownEntry.Value;
            double stopRM = (double)numericUpDownStop.Value;
            double target100RM = (double)numericUpDownTarget.Value;

            double lossRM = 0;
            int quantity = 0;
            double win100RM = 0;
            double rr100RM = 0;
            string cur = "";

            if (stopRM < entryRM && entryRM < target100RM)
            {
                lossRM = Math.Round(((entryRM - stopRM) * Instrument.MasterInstrument.PointValue), 2, MidpointRounding.AwayFromZero);
                quantity = (int)Math.Truncate(accountSize / 100.0 * accountRisk / lossRM);
                win100RM = Math.Round(((target100RM - entryRM) * Instrument.MasterInstrument.PointValue), 2, MidpointRounding.AwayFromZero);
                rr100RM = Math.Round(win100RM / lossRM, 2, MidpointRounding.AwayFromZero);
                cur = Instrument.MasterInstrument.Currency.ToString() + " ";

                toolStripLabelQty.Text = "Qty: " + quantity;
                toolStripLabelQty.ForeColor = Color.Green;
            }
            else if (stopRM > entryRM && entryRM > target100RM)
            {
                lossRM = Math.Round(((stopRM - entryRM) * Instrument.MasterInstrument.PointValue), 2, MidpointRounding.AwayFromZero);
                quantity = (int)Math.Truncate(accountSize / 100.0 * accountRisk / lossRM);
                win100RM = Math.Round(((entryRM - target100RM) * Instrument.MasterInstrument.PointValue), 2, MidpointRounding.AwayFromZero);
                rr100RM = Math.Round(win100RM / lossRM, 2, MidpointRounding.AwayFromZero);
                cur = Instrument.MasterInstrument.Currency.ToString() + " ";

                toolStripLabelQty.Text = "Qty: " + quantity;
                toolStripLabelQty.ForeColor = Color.Red;
            }
            else
                return;

            toolStripLabelRiskReward.Text = "R/R: " + rr100RM;
            toolStripLabelLoss.Text = "Loss: " + lossRM;
            toolStripLabelProfit.Text = "Win: " + win100RM;
            if (rr100RM < 1.0)
            {
                toolStripLabelRiskReward.ForeColor = Color.Red;
                toolStripLabelLoss.ForeColor = Color.Red;
                toolStripLabelProfit.ForeColor = Color.Red;
            }
            else if (rr100RM < 1.6)
            {
                toolStripLabelRiskReward.ForeColor = Color.Black;
                toolStripLabelLoss.ForeColor = Color.Black;
                toolStripLabelProfit.ForeColor = Color.Black;
            }
            else
            {
                toolStripLabelRiskReward.ForeColor = Color.Green;
                toolStripLabelLoss.ForeColor = Color.Green;
                toolStripLabelProfit.ForeColor = Color.Green;
            }
        }
        //#####################################################################
        #endregion

        #region OnTermination()
        //#####################################################################
        /// <summary>
        /// Remove the controls.
        /// </summary>
        protected override void OnTermination()
        {
            #region Remove statistic forms
            // Remove statistic forms =============================================================
            if (statisticPosition != StatisticPositions.False)
            {
                // Remove control elements
                ChartControl.Controls.RemoveByKey("colRelationLowerLow");
                ChartControl.Controls.RemoveByKey("colRelationHigherLow");
                ChartControl.Controls.RemoveByKey("colRelationLowerHigh");
                ChartControl.Controls.RemoveByKey("colRelationHigherHigh");
                ChartControl.Controls.RemoveByKey("colRelationSwingCount");
                ChartControl.Controls.RemoveByKey("colRelationSwing");
                ChartControl.Controls.RemoveByKey("relationList");
                ChartControl.Controls.RemoveByKey("colLengthLastDuration");
                ChartControl.Controls.RemoveByKey("colLengthDuration");
                ChartControl.Controls.RemoveByKey("colLengthLastLength");
                ChartControl.Controls.RemoveByKey("colLengthLength");
                ChartControl.Controls.RemoveByKey("colLengthSwingCount");
                ChartControl.Controls.RemoveByKey("colLengthDirection");
                ChartControl.Controls.RemoveByKey("lengthList");
                ChartControl.Controls.RemoveByKey("tabSwingRelation");
                ChartControl.Controls.RemoveByKey("tabSwingLength");
                ChartControl.Controls.RemoveByKey("mainTabControl");
                ChartControl.Controls.RemoveByKey("label");
                ChartControl.Controls.RemoveByKey("panel");
                ChartControl.Controls.RemoveByKey("splitter");

                // Remove button and separator from the NT menu
                if (toolStrip != null)
                {
                    if (toolStripButton != null)
                        toolStrip.Items.RemoveByKey("button");
                    if (toolStripSeparator != null)
                        toolStrip.Items.RemoveByKey("separator");
                }

                // Set control elements to null
                this.panel = null;
                this.mainTabControl = null;
                this.tabSwingLength = null;
                this.lengthList = null;
                this.tabSwingRelation = null;
                this.colLengthDirection = null;
                this.colLengthSwingCount = null;
                this.colLengthLength = null;
                this.colLengthLastLength = null;
                this.colLengthDuration = null;
                this.colLengthLastDuration = null;
                this.relationList = null;
                this.label = null;
                this.colRelationSwing = null;
                this.colRelationSwingCount = null;
                this.colRelationHigherHigh = null;
                this.colRelationLowerHigh = null;
                this.colRelationHigherLow = null;
                this.colRelationLowerLow = null;

                this.splitter = null;
                if (!showRiskManagement)
                    toolStrip = null;
                toolStripButton = null;
                toolStripSeparator = null;
            }
            // End remove statistic forms =========================================================
            #endregion

            #region Remove riskmanagement items
            // Remove riskmanagement items ========================================================
            if (showRiskManagement)
            {
                // Remove control elements from the NT menu
                if (toolStrip != null)
                {
                    toolStrip.Items.Remove(toolStripSeparator1);
                    toolStrip.Items.Remove(toolStripSeparator2);
                    toolStrip.Items.Remove(toolStripSeparator3);
                    toolStrip.Items.Remove(toolStripLabelQty);
                    toolStrip.Items.Remove(toolStripLabelRiskReward);
                    toolStrip.Items.Remove(toolStripLabelLoss);
                    toolStrip.Items.Remove(toolStripLabelProfit);
                    toolStrip.Items.Remove(toolStripLabelEntry);
                    toolStrip.Items.Remove(toolStripLabelStop);
                    toolStrip.Items.Remove(toolStripLabelTarget);
                    toolStrip.Items.Remove(toolStripControlHostEntry);
                    toolStrip.Items.Remove(toolStripControlHostStop);
                    toolStrip.Items.Remove(toolStripControlHostTarget);
                }

                // Set control elements to null
                toolStripSeparator1 = null;
                toolStripSeparator2 = null;
                toolStripSeparator3 = null;
                toolStripLabelQty = null;
                toolStripLabelRiskReward = null;
                toolStripLabelLoss = null;
                toolStripLabelProfit = null;
                toolStripLabelEntry = null;
                toolStripLabelStop = null;
                toolStripLabelTarget = null;
                numericUpDownEntry = null;
                numericUpDownStop = null;
                numericUpDownTarget = null;
                toolStripControlHostEntry = null;
                toolStripControlHostStop = null;
                toolStripControlHostTarget = null;
                if (statisticPosition == StatisticPositions.False)
                    toolStrip = null;
            }
            // End remove riskmanagement items ====================================================
            #endregion

            if (statisticPosition != StatisticPositions.False || showRiskManagement)
                toolStrip = null;

            base.OnTermination();
        }
        //#####################################################################
        #endregion
        //#####################################################################
#endif
        #endregion

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
            #region Initialize varibles
            //=================================================================
            if (FirstTickOfBar)
            {
                stopOutsideBarCalc = false;
                entryLevelLine.Set(0);
                abcSignals.Set(0);

                if (curLow == 0.0 || curHigh == 0.0)
                {
                    entryLong.Set(0);
                    entryShort.Set(0);
                }
                else
                {
                    entryLong.Set(entryLong[1]);
                    entryShort.Set(entryShort[1]);
                }

                if (CurrentBar == 1)
                {
                    curHighBar = curLowBar = CurrentBar;
                    curHigh = High[1];
                    curLow = Low[1];
                    Swings up = new Swings(curHighBar, curHigh, 0, 0, curHighRelation);
                    Swings dn = new Swings(curLowBar, curLow, 0, 0, curLowRelation);
                    swingHighs.Add(up);
                    swingLows.Add(dn);
                    upCount = swingHighs.Count;
                    dnCount = swingLows.Count;
                }
            }
            // Set new/update high/low back to false, to avoid function 
            // calls which depends on them
            dnFlip.Set(false);
            upFlip.Set(false);
            newHigh = newLow = updateHigh = updateLow = false;

            // Checks to make sure we have enough bars to calculate the indicator   
            if (CurrentBar < swingSize)
                return;
            //=================================================================
            #endregion

            #region Swing calculation
            // Swing calculation ==============================================
            switch (swingType)
            {
                case SwingTypes.Standard:
                    #region Standard Swing
                    if (swingSlope == 1 && High[0] <= curHigh)
                        newHigh = false;
                    else
                        newHigh = true;

                    if (swingSlope == -1 && Low[0] >= curLow)
                        newLow = false;
                    else
                        newLow = true;

                    if (FirstTickOfBar && VisualizationTypes.GannStyle == visualizationType)
                    {
                        if (swingSlope == 1)
                            GannSwing.Set(curHigh);
                        else
                            GannSwing.Set(curLow);
                    }

                    // CalculatOnBarClose == true =================================================
                    if (CalculateOnBarClose)
                    {
                        if (newHigh)
                        {
                            for (int i = 1; i < swingSize + 1; i++)
                            {
                                if (High[0] <= High[i])
                                {
                                    newHigh = false;
                                    break;
                                }
                            }
                        }
                        if (newLow)
                        {
                            for (int i = 1; i < swingSize + 1; i++)
                            {
                                if (Low[0] >= Low[i])
                                {
                                    newLow = false;
                                    break;
                                }
                            }
                        }

                        if (newHigh && newLow)
                        {
                            if (swingSlope == -1)
                                newHigh = false;
                            else
                                newLow = false;
                        }
                    }
                    // CalculatOnBarClose == false ================================================
                    else
                    {
                        if (FirstTickOfBar)
                            newSwing = 0;

                        if (newSwing != -1)
                        {
                            if (newHigh)
                            {
                                for (int i = 1; i < swingSize + 1; i++)
                                {
                                    if (High[0] <= High[i])
                                    {
                                        newHigh = false;
                                        break;
                                    }
                                }
                                if (newHigh)
                                    newSwing = 1;
                            }
                        }

                        if (newSwing != 1)
                        {
                            if (newLow)
                            {
                                for (int i = 1; i < swingSize + 1; i++)
                                {
                                    if (Low[0] >= Low[i])
                                    {
                                        newLow = false;
                                        break;
                                    }
                                }
                                if (newLow)
                                    newSwing = -1;
                            }
                        }

                        if (newSwing == 1)
                            newLow = false;
                        if (newSwing == -1)
                            newHigh = false;
                    }

                    // Set new high or low ========================================================
                    if (newHigh)
                    {
                        if (swingSlope != 1)
                        {
                            bar = CurrentBar - HighestBar(High, CurrentBar - curLowBar);
                            price = High[HighestBar(High, CurrentBar - curLowBar)];
                            updateHigh = false;
                        }
                        else
                        {
                            bar = CurrentBar;
                            price = High[0];
                            updateHigh = true;
                        }
                        CalcUpSwing(bar, price, updateHigh);
                    }
                    else if (newLow)
                    {
                        if (swingSlope != -1)
                        {
                            bar = CurrentBar - LowestBar(Low, CurrentBar - curHighBar);
                            price = Low[LowestBar(Low, CurrentBar - curHighBar)];
                            updateLow = false;
                        }
                        else
                        {
                            bar = CurrentBar;
                            price = Low[0];
                            updateLow = true;
                        }
                        CalcDnSwing(bar, price, updateLow);
                    }
                    // ============================================================================
                    #endregion
                    break;
                case SwingTypes.Gann:
                    #region Set bar property
                    //=================================================================
                    if (paintBars)
                    {
                        if (High[0] > High[1])
                        {
                            if (Low[0] < Low[1])
                            {
                                barType = BarType.OutsideBar;
                                BarColor = outsideBarColour;
                            }
                            else
                            {
                                barType = BarType.UpBar;
                                BarColor = upBarColour;
                            }
                        }
                        else
                        {
                            if (Low[0] < Low[1])
                            {
                                barType = BarType.DnBar;
                                BarColor = dnBarColour;
                            }
                            else
                            {
                                barType = BarType.InsideBar;
                                BarColor = insideBarColour;
                            }
                        }
                    }
                    else
                    {
                        if (High[0] > High[1])
                        {
                            if (Low[0] < Low[1])
                                barType = BarType.OutsideBar;
                            else
                                barType = BarType.UpBar;
                        }
                        else
                        {
                            if (Low[0] < Low[1])
                                barType = BarType.DnBar;
                            else
                                barType = BarType.InsideBar;
                        }
                    }
                    //===================================================================
                    #endregion

                    #region Gann Swing
                    // Gann swing =================================================================
                    #region Up swing
                    // Up Swing ===================================================================
                    if (swingSlope == 1)
                    {
                        if (FirstTickOfBar && VisualizationTypes.GannStyle == visualizationType)
                            GannSwing.Set(curHigh);

                        switch (barType)
                        {
                            // Up bar =============================================================
                            case BarType.UpBar:
                                consecutiveBars = 0;
                                consecutiveBarValue = 0.0;
                                if (High[0] > curHigh)
                                {
                                    updateHigh = true;
                                    CalcUpSwing(CurrentBar, High[0], true);
                                    if ((consecutiveBars + 1) == swingSize)
                                        stopOutsideBarCalc = true;
                                }
                                break;
                            // Down bar ===========================================================
                            case BarType.DnBar:
                                if (consecutiveBarNumber != CurrentBar)
                                {
                                    if (consecutiveBarValue == 0.0)
                                    {
                                        consecutiveBars++;
                                        consecutiveBarNumber = CurrentBar;
                                        consecutiveBarValue = Low[0];
                                    }
                                    else if (Low[0] < consecutiveBarValue)
                                    {
                                        consecutiveBars++;
                                        consecutiveBarNumber = CurrentBar;
                                        consecutiveBarValue = Low[0];
                                    }
                                }
                                else if (Low[0] < consecutiveBarValue)
                                    consecutiveBarValue = Low[0];
                                if (consecutiveBars == swingSize || (useBreakouts && Low[0] < curLow))
                                {
                                    consecutiveBars = 0;
                                    consecutiveBarValue = 0.0;
                                    newLow = true;
                                    bar = CurrentBar - LowestBar(Low, CurrentBar - curHighBar);
                                    price = Low[LowestBar(Low, CurrentBar - curHighBar)];
                                    CalcDnSwing(bar, price, false);
                                }
                                break;
                            // Inside bar =========================================================
                            case BarType.InsideBar:
                                if (!ignoreInsideBars)
                                {
                                    consecutiveBars = 0;
                                    consecutiveBarValue = 0.0;
                                }
                                break;
                            // Outside bar ========================================================
                            case BarType.OutsideBar:
                                if (High[0] > curHigh)
                                {
                                    updateHigh = true;
                                    CalcUpSwing(CurrentBar, High[0], true);
                                }
                                else if (!stopOutsideBarCalc)
                                {
                                    if (consecutiveBarNumber != CurrentBar)
                                    {
                                        if (consecutiveBarValue == 0.0)
                                        {
                                            consecutiveBars++;
                                            consecutiveBarNumber = CurrentBar;
                                            consecutiveBarValue = Low[0];
                                        }
                                        else if (Low[0] < consecutiveBarValue)
                                        {
                                            consecutiveBars++;
                                            consecutiveBarNumber = CurrentBar;
                                            consecutiveBarValue = Low[0];
                                        }
                                    }
                                    else if (Low[0] < consecutiveBarValue)
                                        consecutiveBarValue = Low[0];
                                    if (consecutiveBars == swingSize || (useBreakouts && Low[0] < curLow))
                                    {
                                        consecutiveBars = 0;
                                        consecutiveBarValue = 0.0;
                                        newLow = true;
                                        bar = CurrentBar - LowestBar(Low, CurrentBar - curHighBar);
                                        price = Low[LowestBar(Low, CurrentBar - curHighBar)];
                                        CalcDnSwing(bar, price, false);
                                    }
                                }
                                break;
                        }
                    }
                    // End up swing ===============================================================
                    #endregion

                    #region Down swing
                    // Down swing =================================================================
                    else
                    {
                        if (FirstTickOfBar && VisualizationTypes.GannStyle == visualizationType)
                            GannSwing.Set(curLow);

                        switch (barType)
                        {
                            // Dwon bar ===========================================================
                            case BarType.DnBar:
                                consecutiveBars = 0;
                                consecutiveBarValue = 0.0;
                                if (Low[0] < curLow)
                                {
                                    updateLow = true;
                                    CalcDnSwing(CurrentBar, Low[0], true);
                                    if ((consecutiveBars + 1) == swingSize)
                                        stopOutsideBarCalc = true;
                                }
                                break;
                            // Up bar =============================================================
                            case BarType.UpBar:
                                if (consecutiveBarNumber != CurrentBar)
                                {
                                    if (consecutiveBarValue == 0.0)
                                    {
                                        consecutiveBars++;
                                        consecutiveBarNumber = CurrentBar;
                                        consecutiveBarValue = High[0];
                                    }
                                    else if (High[0] > consecutiveBarValue)
                                    {
                                        consecutiveBars++;
                                        consecutiveBarNumber = CurrentBar;
                                        consecutiveBarValue = High[0];
                                    }
                                }
                                else if (High[0] > consecutiveBarValue)
                                    consecutiveBarValue = High[0];
                                if (consecutiveBars == swingSize || (useBreakouts && High[0] > curHigh))
                                {
                                    consecutiveBars = 0;
                                    consecutiveBarValue = 0.0;
                                    newHigh = true;
                                    bar = CurrentBar - HighestBar(High, CurrentBar - curLowBar);
                                    price = High[HighestBar(High, CurrentBar - curLowBar)];
                                    CalcUpSwing(bar, price, false);
                                }
                                break;
                            // Inside bar =========================================================
                            case BarType.InsideBar:
                                if (!ignoreInsideBars)
                                {
                                    consecutiveBars = 0;
                                    consecutiveBarValue = 0.0;
                                }
                                break;
                            // Outside bar ========================================================
                            case BarType.OutsideBar:
                                if (Low[0] < curLow)
                                {
                                    updateLow = true;
                                    CalcDnSwing(CurrentBar, Low[0], true);
                                }
                                else if (!stopOutsideBarCalc)
                                {
                                    if (consecutiveBarNumber != CurrentBar)
                                    {
                                        if (consecutiveBarValue == 0.0)
                                        {
                                            consecutiveBars++;
                                            consecutiveBarNumber = CurrentBar;
                                            consecutiveBarValue = High[0];
                                        }
                                        else if (High[0] > consecutiveBarValue)
                                        {
                                            consecutiveBars++;
                                            consecutiveBarNumber = CurrentBar;
                                            consecutiveBarValue = High[0];
                                        }
                                    }
                                    else if (High[0] > consecutiveBarValue)
                                        consecutiveBarValue = High[0];
                                    if (consecutiveBars == swingSize || (useBreakouts && High[0] > curHigh))
                                    {
                                        consecutiveBars = 0;
                                        consecutiveBarValue = 0.0;
                                        newHigh = true;
                                        bar = CurrentBar - HighestBar(High, CurrentBar - curLowBar);
                                        price = High[HighestBar(High, CurrentBar - curLowBar)];
                                        CalcUpSwing(bar, price, false);
                                    }
                                }
                                break;
                        }
                    }
                    // End down swing =============================================================
                    #endregion
                    // End Gann swing =============================================================
                    #endregion
                    break;
            }
            // End swing calculation ==========================================
            #endregion

            #region Swing trend/-relation
            // Swing trend ========================================================================
            if ((curHighRelation == Relation.Higher && curLowRelation == Relation.Higher)
                    || (swingTrend[1] == 1 && swingSlope == 1)
                    || (ignoreSwings && swingTrend[1] == 1 && curLowRelation == Relation.Higher)
                    || (((swingTrend[1] == 1) || (swingSlope == 1 && curHighRelation == Relation.Higher))
                        && curLowRelation == Relation.Double))
                swingTrend.Set(1);
            else if ((curHighRelation == Relation.Lower && curLowRelation == Relation.Lower)
                    || (swingTrend[1] == -1 && swingSlope == -1)
                    || (ignoreSwings && swingTrend[1] == -1 && curHighRelation == Relation.Lower)
                    || (((swingTrend[1] == -1) || (swingSlope == -1 && curLowRelation == Relation.Lower))
                        && curHighRelation == Relation.Double))
                swingTrend.Set(-1);
            else
                swingTrend.Set(0);
            // Swing relation =====================================================================
            if (curLowRelation == Relation.Double)
                swingRelation.Set(2);
            else if (curHighRelation == Relation.Double)
                swingRelation.Set(-2);
            else if (curHighRelation == Relation.Higher && curLowRelation == Relation.Higher)
                swingRelation.Set(1);
            else if (curHighRelation == Relation.Lower && curLowRelation == Relation.Lower)
                swingRelation.Set(-1);
            else
                swingRelation.Set(0);
            // ====================================================================================
            #endregion

            #region Fibonacci tools
            //=================================================================
            if (newHigh || newLow || updateHigh || updateLow)
            {
                #region Fibonacci extensions
                // Fibonacci extensions ===========================================================
                if (addFibExt)
                {
                    if (lastHigh == 0.0 || lastLow == 0.0)
                        return;

                    if (swingLows[swingLows.Count - 1].relation == Relation.Higher && swingSlope == -1)
                    {
                        int anchor1BarsAgo = CurrentBar - lastLowBar;
                        int anchor2BarsAgo = CurrentBar - curHighBar;
                        int anchor3BarsAgo = CurrentBar - curLowBar;
                        double anchor1Y = lastLow;
                        double anchor2Y = curHigh;
                        double anchor3Y = curLow;
                        DrawFibonacciExtensions("FibExtUp", false, anchor1BarsAgo, anchor1Y,
                            anchor2BarsAgo, anchor2Y, anchor3BarsAgo, anchor3Y);
                    }
                    else if (swingLows[swingLows.Count - 1].relation == Relation.Higher && swingSlope == 1)
                    {
                        int anchor1BarsAgo = CurrentBar - lastLowBar;
                        int anchor2BarsAgo = CurrentBar - lastHighBar;
                        int anchor3BarsAgo = CurrentBar - curLowBar;
                        double anchor1Y = lastLow;
                        double anchor2Y = lastHigh;
                        double anchor3Y = curLow;
                        DrawFibonacciExtensions("FibExtUp", false, anchor1BarsAgo, anchor1Y,
                            anchor2BarsAgo, anchor2Y, anchor3BarsAgo, anchor3Y);
                    }
                    else
                        RemoveDrawObject("FibExtUp");

                    if (swingHighs[swingHighs.Count - 1].relation == Relation.Lower && swingSlope == 1)
                    {
                        int anchor1BarsAgo = CurrentBar - lastHighBar;
                        int anchor2BarsAgo = CurrentBar - curLowBar;
                        int anchor3BarsAgo = CurrentBar - curHighBar;
                        double anchor1Y = lastHigh;
                        double anchor2Y = curLow;
                        double anchor3Y = curHigh;
                        DrawFibonacciExtensions("FibExtDn", false, anchor1BarsAgo, anchor1Y,
                            anchor2BarsAgo, anchor2Y, anchor3BarsAgo, anchor3Y);
                    }
                    else if (swingHighs[swingHighs.Count - 1].relation == Relation.Lower && swingSlope == -1)
                    {
                        int anchor1BarsAgo = CurrentBar - lastHighBar;
                        int anchor2BarsAgo = CurrentBar - lastLowBar;
                        int anchor3BarsAgo = CurrentBar - curHighBar;
                        double anchor1Y = lastHigh;
                        double anchor2Y = lastLow;
                        double anchor3Y = curHigh;
                        DrawFibonacciExtensions("FibExtDn", false, anchor1BarsAgo, anchor1Y,
                            anchor2BarsAgo, anchor2Y, anchor3BarsAgo, anchor3Y);
                    }
                    else
                        RemoveDrawObject("FibExtDn");
                }
                // ================================================================================
                #endregion

                #region Fibonacci retracements
                // Fibonacci retracements =========================================================
                if (addFastFibRet)
                {
                    int anchor1BarsAgo = 0;
                    int anchor2BarsAgo = 0;
                    double anchor1Y = 0.0;
                    double anchor2Y = 0.0;

                    if (swingSlope == 1)
                    {
                        anchor1BarsAgo = CurrentBar - curLowBar;
                        anchor1Y = curLow;
                        anchor2BarsAgo = CurrentBar - curHighBar;
                        anchor2Y = curHigh;
                    }
                    else
                    {
                        anchor1BarsAgo = CurrentBar - curHighBar;
                        anchor1Y = curHigh;
                        anchor2BarsAgo = CurrentBar - curLowBar;
                        anchor2Y = curLow;
                    }
                    DrawFibonacciRetracements("FastFibRet", AutoScale,
                        anchor1BarsAgo, anchor1Y, anchor2BarsAgo, anchor2Y);
                }

                if (addSlowFibRet)
                {
                    if (lastHigh == 0.0 || lastLow == 0.0) return;

                    int anchor1BarsAgo = 0;
                    int anchor2BarsAgo = 0;
                    double anchor1Y = 0.0;
                    double anchor2Y = 0.0;

                    if (swingSlope == 1)
                    {
                        anchor1BarsAgo = CurrentBar - lastHighBar;
                        anchor1Y = lastHigh;
                        anchor2BarsAgo = CurrentBar - curLowBar;
                        anchor2Y = curLow;
                    }
                    else
                    {
                        anchor1BarsAgo = CurrentBar - lastLowBar;
                        anchor1Y = lastLow;
                        anchor2BarsAgo = CurrentBar - curHighBar;
                        anchor2Y = curHigh;
                    }

                    if ((swingSlope == 1 && curHigh < lastHigh) || (swingSlope == -1 && curLow > lastLow))
                        DrawFibonacciRetracements("SlowFibRet", AutoScale, anchor1BarsAgo, anchor1Y,
                            anchor2BarsAgo, anchor2Y);
                    else
                        RemoveDrawObject("SlowFibRet");
                }
                #endregion
            }
            //=================================================================
            #endregion

            #region ABC pattern
            // ABC pattern ====================================================
            #region ABC long pattern
            // ABC long pattern ===================================================================
            if (abcPattern == AbcPatternMode.Long_And_Short || abcPattern == AbcPatternMode.Long)
            {
                if ((updateLow || newLow) && !abcLongChanceInProgress
                        && lastLowRelation == Relation.Lower && curLowRelation == Relation.Higher
                        && curLowPercent > abcMinRetracement && curLowPercent < abcMaxRetracement)
                {
                    drawTag = swingCounterUp;
                    abcLongChanceInProgress = true;
                    entryLineStartBar = CurrentBar;
                    patternCounter++;
                    tmpCounter = swingCounterDn;
                    abcSignals.Set(1);
                    aBar = CurrentBar - lastLowBar;
                    bBar = CurrentBar - curHighBar;
                    cBar = CurrentBar - curLowBar;

                    DrawLine("ABLineUp" + drawTag, AutoScale, aBar, lastLow,
                        bBar, curHigh, abcZigZagColourUp, abcLineStyle, abcLineWidth);
                    DrawLine("BCLineUp" + drawTag, AutoScale, bBar, curHigh,
                        cBar, curLow, abcZigZagColourUp, abcLineStyle, abcLineWidth);
                    DrawLine("ACLineUp" + drawTag, AutoScale, aBar, lastLow,
                        cBar, curLow, abcZigZagColourUp, abcLineStyleRatio, abcLineWidthRatio);
                    if (abcLabel)
                    {
                        DrawText("AUp" + drawTag, AutoScale, "A", aBar, lastLow,
                            -abcTextOffsetLabel, abcTextColourUp, abcTextFont,
                            StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
                        DrawText("BUp" + drawTag, AutoScale, "B", bBar, curHigh,
                            abcTextOffsetLabel, abcTextColourUp, abcTextFont,
                            StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
                        DrawText("CUp" + drawTag, AutoScale, "C", cBar, curLow,
                            -abcTextOffsetLabel, abcTextColourUp, abcTextFont,
                            StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
                    }

                    entryLevel = curLow + Instrument.MasterInstrument.Round2TickSize((Math.Abs(curLowLength) * TickSize)
                        / 100 * retracementEntryValue);
                    entryLevelLine.Set(entryLevel);
                    if (showEntryLine)
                        DrawLine("EntryLine" + CurrentBar.ToString(), AutoScale, 1, entryLevelLine[0], 0, entryLevelLine[0],
                            entryLineColourUp, entryLineStyle, entryLineWidth);
                    if (useAbcAlerts)
                        Alert("Alert_Abc_Long" + alertTag++.ToString(), priorityAbc, "ABC Long" + " ("
                            + Bars.Period.ToString() + ")", fileNameAbcLong, 0, Color.White, Color.Blue);
                    entryLong.Set(entryLevel);
                    if (showRiskManagement)
                    {
                        double entryRM = entryLong[0];
                        double stopRM = curLow - 1 * TickSize;
                        double target100RM = 0.0;
                        if (swingSlope == 1)
                            target100RM = curLow + lastHighLength * TickSize * abcTarget / 100;
                        else
                            target100RM = curLow + curHighLength * TickSize * abcTarget / 100;
                        CalculateLongRisk(entryRM, stopRM, target100RM);
                    }
                }

                if (abcLongChanceInProgress)
                {
                    if (curLowPercent > abcMaxRetracement && tmpCounter == swingCounterDn)
                    {
                        abcLongChanceInProgress = false;
                        RemoveDrawObject("ABLineUp" + drawTag.ToString());
                        RemoveDrawObject("BCLineUp" + drawTag.ToString());
                        RemoveDrawObject("ACLineUp" + drawTag.ToString());
                        RemoveDrawObject("AUp" + drawTag.ToString());
                        RemoveDrawObject("BUp" + drawTag.ToString());
                        RemoveDrawObject("CUp" + drawTag.ToString());
                        // Remove entryLevelLine (maybe remove more objects as drawn (COBC))
                        if (!showHistoricalEntryLine)
                        {
                            for (int index = 0; index < CurrentBar - entryLineStartBar + 1; index++)
                            {
                                RemoveDrawObject("EntryLine" + (CurrentBar - index).ToString());
                            }
                            entryLineStartBar = 0;
                        }
                    }
                    else if (dnFlip[0] && tmpCounter != swingCounterDn)
                    {
                        abcLongChanceInProgress = false;
                        entryLineStartBar = 0;
                    }
                    else
                    {
                        if (updateLow && tmpCounter == swingCounterDn)
                        {
                            aBar = CurrentBar - lastLowBar;
                            bBar = CurrentBar - curHighBar;
                            cBar = CurrentBar - curLowBar;

                            DrawLine("BCLineUp" + drawTag, AutoScale, bBar, curHigh,
                                cBar, curLow, abcZigZagColourUp, abcLineStyle, abcLineWidth);
                            DrawLine("ACLineUp" + drawTag, AutoScale, aBar, lastLow,
                                cBar, curLow, abcZigZagColourUp, abcLineStyleRatio, abcLineWidthRatio);
                            if (abcLabel)
                            {
                                DrawText("CUp" + drawTag, AutoScale, "C", cBar, curLow,
                                    -abcTextOffsetLabel, abcTextColourUp, abcTextFont,
                                    StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
                            }

                            entryLevel = curLow + Instrument.MasterInstrument.Round2TickSize((Math.Abs(curLowLength) * TickSize)
                                / 100 * retracementEntryValue);
                            entryLevelLine.Set(entryLevel);

                            if (showEntryLine)
                            {
                                if (entryLevelLine[1] == 0)
                                    DrawLine("EntryLine" + CurrentBar.ToString(), AutoScale, 1, entryLevelLine[0], 0, entryLevelLine[0],
                                        entryLineColourUp, entryLineStyle, entryLineWidth);
                                else
                                    DrawLine("EntryLine" + CurrentBar.ToString(), AutoScale, 1, entryLevelLine[1], 0, entryLevelLine[0],
                                        entryLineColourUp, entryLineStyle, entryLineWidth);
                            }
                            entryLong.Set(entryLevel);
                            if (showRiskManagement)
                            {
                                double entryRM = entryLong[0];
                                double stopRM = curLow - 1 * TickSize;
                                double target100RM = 0.0;
                                if (swingSlope == 1)
                                    target100RM = curLow + lastHighLength * TickSize * abcTarget / 100;
                                else
                                    target100RM = curLow + curHighLength * TickSize * abcTarget / 100;
                                CalculateLongRisk(entryRM, stopRM, target100RM);
                            }
                        }
                        else if (FirstTickOfBar)
                        {
                            entryLevelLine.Set(entryLevel);

                            if (showEntryLine)
                            {
                                if (entryLevelLine[1] == 0)
                                    DrawLine("EntryLine" + CurrentBar.ToString(), AutoScale, 1, entryLevelLine[0], 0, entryLevelLine[0],
                                        entryLineColourUp, entryLineStyle, entryLineWidth);
                                else
                                    DrawLine("EntryLine" + CurrentBar.ToString(), AutoScale, 1, entryLevelLine[1], 0, entryLevelLine[0],
                                        entryLineColourUp, entryLineStyle, entryLineWidth);
                            }
                        }
                        abcSignals.Set(1);

                        bool abcLong = false;
                        if (CalculateOnBarClose || Historical)
                        {
                            if (Close[0] > entryLevel)
                            {
                                entryLong.Set(Close[0]);
                                abcLong = true;
                            }
                        }
                        else
                        {
                            if (FirstTickOfBar && Open[0] > entryLevel)
                            {
                                entryLong.Set(Open[0]);
                                abcLong = true;
                            }
                        }

                        if (abcLong)
                        {
                            if (showEntryArrows)
                                DrawArrowUp("AbcUp" + abcEntryTag++.ToString(), AutoScale, 0,
                                    Low[0] - yTickOffset * TickSize, abcTextColourUp);
                            abcLongChanceInProgress = false;
                            entryLineStartBar = 0;
                            entryBar = CurrentBar;
                            abcSignals.Set(2);
                            if (showRiskManagement)
                            {
                                double entryRM = entryLong[0];
                                double stopRM = curLow - 1 * TickSize;
                                double target100RM = 0.0;
                                if (swingSlope == 1)
                                    target100RM = curLow + lastHighLength * TickSize * abcTarget / 100;
                                else
                                    target100RM = curLow + curHighLength * TickSize * abcTarget / 100;
                                CalculateLongRisk(entryRM, stopRM, target100RM);
                            }
                            if (useAbcEntryAlerts)
                                Alert("Alert_Abc_Long_Entry" + alertTag++.ToString(), priorityAbcEntry, "ABC Long Entry" + " ("
                                    + Bars.Period.ToString() + ")", fileNameAbcLongEntry, 0, Color.Blue, Color.White);
                        }
                    }
                }
            }
            // End ABC long pattern ===============================================================
            #endregion

            #region ABC short pattern
            // ABC short pattern ==================================================================
            if (abcPattern == AbcPatternMode.Long_And_Short || abcPattern == AbcPatternMode.Short)
            {
                if ((updateHigh || newHigh) && !abcShortChanceInProgress
                        && lastHighRelation == Relation.Higher && curHighRelation == Relation.Lower
                        && curHighPercent > abcMinRetracement && curHighPercent < abcMaxRetracement)
                {
                    drawTag = swingCounterDn;
                    abcShortChanceInProgress = true;
                    entryLineStartBar = CurrentBar;
                    patternCounter++;
                    tmpCounter = swingCounterUp;
                    abcSignals.Set(-1);

                    aBar = CurrentBar - lastHighBar;
                    bBar = CurrentBar - curLowBar;
                    cBar = CurrentBar - curHighBar;

                    DrawLine("ABLineDn" + drawTag, AutoScale, aBar, lastHigh,
                        bBar, curLow, abcZigZagColourDn, abcLineStyle, abcLineWidth);
                    DrawLine("BCLineDn" + drawTag, AutoScale, bBar, curLow,
                        cBar, curHigh, abcZigZagColourDn, abcLineStyle, abcLineWidth);
                    DrawLine("ACLineDn" + drawTag, AutoScale, aBar, lastHigh,
                        cBar, curHigh, abcZigZagColourDn, abcLineStyleRatio, abcLineWidthRatio);
                    if (abcLabel)
                    {
                        DrawText("ADn" + drawTag, AutoScale, "A", aBar, lastHigh,
                            abcTextOffsetLabel, abcTextColourDn, abcTextFont,
                            StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
                        DrawText("BDn" + drawTag, AutoScale, "B", bBar, curLow,
                            -abcTextOffsetLabel, abcTextColourDn, abcTextFont,
                            StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
                        DrawText("CDn" + drawTag, AutoScale, "C", cBar, curHigh,
                            abcTextOffsetLabel, abcTextColourDn, abcTextFont,
                            StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
                    }

                    entryLevel = curHigh - Instrument.MasterInstrument.Round2TickSize((curHighLength * TickSize)
                        / 100 * retracementEntryValue);
                    entryLevelLine.Set(entryLevel);
                    if (showEntryLine)
                        DrawLine("EntryLine" + CurrentBar.ToString(), AutoScale, 1, entryLevelLine[0], 0, entryLevelLine[0],
                            entryLineColourDn, entryLineStyle, entryLineWidth);
                    if (useAbcAlerts)
                        Alert("Alert_Abc_Short" + alertTag++.ToString(), priorityAbc, "ABC Short" + " ("
                            + Bars.Period.ToString() + ")", fileNameAbcShort, 0, Color.White, Color.Red);
                    entryShort.Set(entryLevel);
                    if (showRiskManagement)
                    {
                        double entryRM = entryShort[0];
                        double stopRM = curHigh + 1 * TickSize;
                        double target100RM = 0.0;
                        if (swingSlope == -1)
                            target100RM = curHigh + lastLowLength * TickSize * abcTarget / 100;
                        else
                            target100RM = curHigh + curLowLength * TickSize * abcTarget / 100;
                        CalculateShortRisk(entryRM, stopRM, target100RM);
                    }
                }

                if (abcShortChanceInProgress)
                {
                    if (curHighPercent > abcMaxRetracement && tmpCounter == swingCounterUp)
                    {
                        abcShortChanceInProgress = false;
                        RemoveDrawObject("ABLineDn" + drawTag.ToString());
                        RemoveDrawObject("BCLineDn" + drawTag.ToString());
                        RemoveDrawObject("ACLineDn" + drawTag.ToString());
                        RemoveDrawObject("ADn" + drawTag.ToString());
                        RemoveDrawObject("BDn" + drawTag.ToString());
                        RemoveDrawObject("CDn" + drawTag.ToString());
                        // Remove entryLevelLine (maybe remove more objects as drawn (COBC))
                        if (!showHistoricalEntryLine)
                        {
                            for (int index = 0; index < CurrentBar - entryLineStartBar + 1; index++)
                            {
                                RemoveDrawObject("EntryLine" + (CurrentBar - index).ToString());
                            }
                            entryLineStartBar = 0;
                        }
                    }
                    else if (upFlip[0] && tmpCounter != swingCounterUp)
                    {
                        abcShortChanceInProgress = false;
                        entryLineStartBar = 0;
                    }
                    else
                    {
                        if (updateHigh && tmpCounter == swingCounterUp)
                        {
                            aBar = CurrentBar - lastHighBar;
                            bBar = CurrentBar - curLowBar;
                            cBar = CurrentBar - curHighBar;

                            DrawLine("BCLineDn" + drawTag, AutoScale, bBar, curLow,
                                cBar, curHigh, abcZigZagColourDn, abcLineStyle, abcLineWidth);
                            DrawLine("ACLineDn" + drawTag, AutoScale, aBar, lastHigh,
                                cBar, curHigh, abcZigZagColourDn, abcLineStyleRatio, abcLineWidthRatio);
                            if (abcLabel)
                            {
                                DrawText("CDn" + drawTag, AutoScale, "C", cBar, curHigh,
                                    abcTextOffsetLabel, abcTextColourDn, abcTextFont,
                                    StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
                            }

                            entryLevel = curHigh - Instrument.MasterInstrument.Round2TickSize((curHighLength * TickSize)
                                / 100 * retracementEntryValue);
                            entryLevelLine.Set(entryLevel);

                            if (showEntryLine)
                            {
                                if (entryLevelLine[1] == 0)
                                    DrawLine("EntryLine" + CurrentBar.ToString(), AutoScale, 1, entryLevelLine[0], 0, entryLevelLine[0],
                                        entryLineColourDn, entryLineStyle, entryLineWidth);
                                else
                                    DrawLine("EntryLine" + CurrentBar.ToString(), AutoScale, 1, entryLevelLine[1], 0, entryLevelLine[0],
                                        entryLineColourDn, entryLineStyle, entryLineWidth);
                            }
                            entryShort.Set(entryLevel);
                            if (showRiskManagement)
                            {
                                double entryRM = entryShort[0];
                                double stopRM = curHigh + 1 * TickSize;
                                double target100RM = 0.0;
                                if (swingSlope == -1)
                                    target100RM = curHigh + lastLowLength * TickSize * abcTarget / 100;
                                else
                                    target100RM = curHigh + curLowLength * TickSize * abcTarget / 100;
                                CalculateShortRisk(entryRM, stopRM, target100RM);
                            }
                        }
                        else if (FirstTickOfBar)
                        {
                            entryLevelLine.Set(entryLevel);

                            if (showEntryLine)
                            {
                                if (entryLevelLine[1] == 0)
                                    DrawLine("EntryLine" + CurrentBar.ToString(), AutoScale, 1, entryLevelLine[0], 0, entryLevelLine[0],
                                        entryLineColourDn, entryLineStyle, entryLineWidth);
                                else
                                    DrawLine("EntryLine" + CurrentBar.ToString(), AutoScale, 1, entryLevelLine[1], 0, entryLevelLine[0],
                                        entryLineColourDn, entryLineStyle, entryLineWidth);
                            }
                        }
                        abcSignals.Set(-1);

                        bool abcShort = false;
                        if (CalculateOnBarClose || Historical)
                        {
                            if (Close[0] < entryLevel)
                            {
                                entryShort.Set(Close[0]);
                                abcShort = true;
                            }
                        }
                        else
                        {
                            if (FirstTickOfBar && Open[0] < entryLevel)
                            {
                                entryShort.Set(Open[0]);
                                abcShort = true;
                            }
                        }

                        if (abcShort)
                        {
                            if (showEntryArrows)
                                DrawArrowDown("AbcDn" + abcEntryTag++.ToString(), AutoScale, 0,
                                    High[0] + yTickOffset * TickSize, abcTextColourDn);
                            abcShortChanceInProgress = false;
                            entryLineStartBar = 0;
                            entryBar = CurrentBar;
                            abcSignals.Set(-2);
                            if (showRiskManagement)
                            {
                                double entryRM = entryShort[0];
                                double stopRM = curHigh + 1 * TickSize;
                                double target100RM = 0.0;
                                if (swingSlope == -1)
                                    target100RM = curHigh + lastLowLength * TickSize * abcTarget / 100;
                                else
                                    target100RM = curHigh + curLowLength * TickSize * abcTarget / 100;
                                CalculateShortRisk(entryRM, stopRM, target100RM);
                            }
                            if (useAbcEntryAlerts)
                                Alert("Alert_Abc_Short_Entry" + alertTag++.ToString(), priorityAbcEntry, "ABC Short Entry" + " ("
                                    + Bars.Period.ToString() + ")", fileNameAbcShortEntry, 0, Color.Red, Color.Black);
                        }
                    }
                }
            }
            // End ABC short pattern ==============================================================
            #endregion
            // End ABC pattern ================================================
            #endregion
        }

        #region CalcDnSwing(int bar, double low, bool updateLow)
        //#####################################################################
        private void CalcDnSwing(int bar, double low, bool updateLow)
        {
            #region New and update Swing values
            if (!updateLow)
            {
                if (VisualizationTypes.GannStyle == visualizationType)
                {
                    for (int i = CurrentBar - trendChangeBar; i >= 0; i--)
                        GannSwing.Set(i, curHigh);
                    GannSwing.Set(low);
                }
                lastLow = curLow;
                lastLowBar = curLowBar;
                lastLowDuration = curLowDuration;
                lastLowLength = curLowLength;
                lastLowTime = curLowTime;
                lastLowPercent = curLowPercent;
                lastLowRelation = curLowRelation;
                swingCounterDn++;
                swingSlope = -1;
                trendChangeBar = bar;
                dnFlip.Set(true);
#if NT7
                if (statisticPosition != StatisticPositions.False) upStatistic();
#endif
            }
            else
            {
                if (VisualizationTypes.Dots == visualizationType
                    || VisualizationTypes.DotsAndLines == visualizationType)
                {
                    DoubleBottom.Reset(CurrentBar - curLowBar);
                    LowerLow.Reset(CurrentBar - curLowBar);
                    HigherLow.Reset(CurrentBar - curLowBar);
                }
                swingLows.RemoveAt(swingLows.Count - 1);
            }
            curLowBar = bar;
            curLow = Math.Round(low, decimalPlaces, MidpointRounding.AwayFromZero);
            curLowTime = ToTime(Time[CurrentBar - curLowBar]);
            #endregion

            #region Calculate essential swing values
            curLowLength = Convert.ToInt32(Math.Round((curLow - curHigh) / TickSize,
                0, MidpointRounding.AwayFromZero));
            if (curHighLength != 0)
                curLowPercent = Math.Round(100.0 / curHighLength * Math.Abs(curLowLength), 1);
            curLowDuration = curLowBar - curHighBar;
            double dtbOffset = ATR(14)[CurrentBar - curLowBar] * dtbStrength / 100;
            if (curLow > lastLow - dtbOffset && curLow < lastLow + dtbOffset)
                curLowRelation = Relation.Double;
            else if (curLow < lastLow)
                curLowRelation = Relation.Lower;
            else
                curLowRelation = Relation.Higher;
            #endregion

            #region Visualize swing
            switch (visualizationType)
            {
                case VisualizationTypes.Dots:
                    switch (curLowRelation)
                    {
                        case Relation.Higher:
                            HigherLow.Set(CurrentBar - curLowBar, curLow);
                            break;
                        case Relation.Lower:
                            LowerLow.Set(CurrentBar - curLowBar, curLow);
                            break;
                        case Relation.Double:
                            DoubleBottom.Set(CurrentBar - curLowBar, curLow);
                            break;
                    }
                    break;
                case VisualizationTypes.ZigZagLines:
                    DrawLine("ZigZagDown" + swingCounterDn, AutoScale, CurrentBar - curHighBar,
                        curHigh, CurrentBar - curLowBar, curLow, zigZagColourDn, zigZagStyle, zigZagWidth);
                    break;
                case VisualizationTypes.DotsAndLines:
                    switch (curLowRelation)
                    {
                        case Relation.Higher:
                            HigherLow.Set(CurrentBar - curLowBar, curLow);
                            break;
                        case Relation.Lower:
                            LowerLow.Set(CurrentBar - curLowBar, curLow);
                            break;
                        case Relation.Double:
                            DoubleBottom.Set(CurrentBar - curLowBar, curLow);
                            break;
                    }
                    DrawLine("ZigZagDown" + swingCounterDn, AutoScale, CurrentBar - curHighBar,
                        curHigh, CurrentBar - curLowBar, curLow, zigZagColourDn, zigZagStyle, zigZagWidth);
                    break;
                case VisualizationTypes.GannStyle:
                    for (int i = CurrentBar - trendChangeBar; i >= 0; i--)
                        GannSwing.Set(i, curLow);
                    break;
            }
            #endregion

            #region Swing value output
            string output = null;
            string swingLabel = null;
            Color textColor = Color.Transparent;
            if (SwingLengthTypes.False != swingLengthType)
            {
                switch (swingLengthType)
                {
                    case SwingLengthTypes.Ticks:
                        output = curLowLength.ToString();
                        break;
                    case SwingLengthTypes.Ticks_And_Price:
                        output = curLowLength.ToString() + " / " + curLow.ToString();
                        break;
                    case SwingLengthTypes.Points:
                        output = (curLowLength * TickSize).ToString();
                        break;
                    case SwingLengthTypes.Price:
                        output = curLow.ToString();
                        break;
                    case SwingLengthTypes.Price_And_Points:
                        output = curLow.ToString() + " / " + (curLowLength * TickSize).ToString();
                        break;
                    case SwingLengthTypes.Price_And_Ticks:
                        output = curLow.ToString() + " / " + curLowLength.ToString();
                        break;
                    case SwingLengthTypes.Percent:
                        output = (Math.Round((100.0 / curHigh * (curLowLength * TickSize)),
                            2, MidpointRounding.AwayFromZero)).ToString();
                        break;
                }
            }
            if (showSwingDuration)
            {
                if (SwingLengthTypes.False != swingLengthType)
                    output = output + " / " + curLowDuration.ToString();
                else
                    output = curLowDuration.ToString();
            }
            switch (curLowRelation)
            {
                case Relation.Higher:
                    swingLabel = "HL";
                    textColor = textColourHigher;
                    break;
                case Relation.Lower:
                    swingLabel = "LL";
                    textColor = textColorLower;
                    break;
                case Relation.Double:
                    swingLabel = "DB";
                    textColor = textColourDtb;
                    break;
            }
            if (output != null)
                DrawText("DnLength" + swingCounterDn, AutoScale, output.ToString(),
                    CurrentBar - curLowBar, curLow, -textOffsetLength, textColor, textFont,
                    StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
            if (showSwingLabel)
                DrawText("DnLabel" + swingCounterDn, AutoScale, swingLabel,
                    CurrentBar - curLowBar, curLow, -textOffsetLabel, textColor, textFont,
                    StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
            if (showSwingPercent && curLowPercent != 0)
            {
                DrawText("DnPerc" + swingCounterDn, AutoScale, curLowPercent.ToString() + "%",
                    CurrentBar - curLowBar, curLow, -textOffsetPercent, textColor, textFont,
                    StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
            }
            if (showSwingTime)
            {
                DrawText("DnPerc" + swingCounterDn, AutoScale, curLowTime.ToString(),
                    CurrentBar - curLowBar, curLow, -textOffsetTime, textColor, textFont,
                    StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
            }
            if (SwingVolumeTypes.False != swingVolumeType)
            {
                if (!CalculateOnBarClose)
                    signalBarVolumeUp = Volume[0];

                double swingVolume = 0.0;
                string volumeOutput = "";

                for (int i = 0; i < curLowDuration; i++)
                {
                    swingVolume = swingVolume + Volume[i];
                }

                if (!CalculateOnBarClose)
                    swingVolume = swingVolume + (Volume[CurrentBar - curHighBar] - signalBarVolumeDn);

                if (SwingVolumeTypes.Relativ == swingVolumeType)
                {
                    swingVolume = Math.Round(swingVolume / curLowDuration, 0, MidpointRounding.AwayFromZero);
                    // Output - Diameter sign isn't represented correct - Latin capital letter O with stroke instead
                    volumeOutput = "\u00D8 ";
                }

                if (swingVolume > 10000000000)
                {
                    swingVolume = Math.Round(swingVolume / 1000000000, 0, MidpointRounding.AwayFromZero);
                    volumeOutput = volumeOutput + swingVolume.ToString() + "B";
                }
                else if (swingVolume > 10000000)
                {
                    swingVolume = Math.Round(swingVolume / 1000000, 0, MidpointRounding.AwayFromZero);
                    volumeOutput = volumeOutput + swingVolume.ToString() + "M";
                }
                else if (swingVolume > 10000)
                {
                    swingVolume = Math.Round(swingVolume / 1000, 0, MidpointRounding.AwayFromZero);
                    volumeOutput = volumeOutput + swingVolume.ToString() + "K";
                }
                else
                    volumeOutput = volumeOutput + swingVolume.ToString();

                DrawText("DnVolume" + swingCounterDn, AutoScale, volumeOutput,
                    CurrentBar - curLowBar, curLow, -textOffsetVolume, textColor, textFont,
                    StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
            }
            #endregion

            Swings dn = new Swings(curLowBar, curLow, curLowLength, curLowDuration, curLowRelation);
            swingLows.Add(dn);
            dnCount = swingLows.Count - 1;
        }
        //#####################################################################
        #endregion

        #region CalcUpSwing(int bar, double high, bool updateHigh)
        //#####################################################################
        private void CalcUpSwing(int bar, double high, bool updateHigh)
        {
            #region New and update swing values
            if (!updateHigh)
            {
                if (VisualizationTypes.GannStyle == visualizationType)
                {
                    for (int i = CurrentBar - trendChangeBar; i >= 0; i--)
                        GannSwing.Set(i, curLow);
                    GannSwing.Set(high);
                }
                lastHigh = curHigh;
                lastHighBar = curHighBar;
                lastHighDuration = curHighDuration;
                lastHighLength = curHighLength;
                lastHighTime = curHighTime;
                lastHighPercent = curHighPercent;
                lastHighRelation = curHighRelation;
                swingCounterUp++;
                swingSlope = 1;
                trendChangeBar = bar;
                upFlip.Set(true);
#if NT7
                if (statisticPosition != StatisticPositions.False) dnStatistic();
#endif
            }
            else
            {
                if (VisualizationTypes.Dots == visualizationType
                    || VisualizationTypes.DotsAndLines == visualizationType)
                {
                    DoubleTop.Reset(CurrentBar - curHighBar);
                    HigherHigh.Reset(CurrentBar - curHighBar);
                    LowerHigh.Reset(CurrentBar - curHighBar);
                }
                swingHighs.RemoveAt(swingHighs.Count - 1);
            }
            curHighBar = bar;
            curHigh = Math.Round(high, decimalPlaces, MidpointRounding.AwayFromZero);
            curHighTime = ToTime(Time[CurrentBar - curHighBar]);
            #endregion

            #region Calculate essential swing values
            curHighLength = Convert.ToInt32(Math.Round((curHigh - curLow) / TickSize,
                0, MidpointRounding.AwayFromZero));
            if (curLowLength != 0)
                curHighPercent = Math.Round(100.0 / Math.Abs(curLowLength) * curHighLength, 1);
            curHighDuration = curHighBar - curLowBar;
            double dtbOffset = ATR(14)[CurrentBar - curHighBar] * dtbStrength / 100;
            if (curHigh > lastHigh - dtbOffset && curHigh < lastHigh + dtbOffset)
                curHighRelation = Relation.Double;
            else if (curHigh < lastHigh)
                curHighRelation = Relation.Lower;
            else
                curHighRelation = Relation.Higher;
            #endregion

            #region Visualize swing
            switch (visualizationType)
            {
                case VisualizationTypes.Dots:
                    switch (curHighRelation)
                    {
                        case Relation.Higher:
                            HigherHigh.Set(CurrentBar - curHighBar, curHigh);
                            break;
                        case Relation.Lower:
                            LowerHigh.Set(CurrentBar - curHighBar, curHigh);
                            break;
                        case Relation.Double:
                            DoubleTop.Set(CurrentBar - curHighBar, curHigh);
                            break;
                    }
                    break;
                case VisualizationTypes.ZigZagLines:
                    DrawLine("ZigZagUp" + swingCounterUp, AutoScale, CurrentBar - curLowBar,
                        curLow, CurrentBar - curHighBar, curHigh, zigZagColourUp, zigZagStyle, zigZagWidth);
                    break;
                case VisualizationTypes.DotsAndLines:
                    switch (curHighRelation)
                    {
                        case Relation.Higher:
                            HigherHigh.Set(CurrentBar - curHighBar, curHigh);
                            break;
                        case Relation.Lower:
                            LowerHigh.Set(CurrentBar - curHighBar, curHigh);
                            break;
                        case Relation.Double:
                            DoubleTop.Set(CurrentBar - curHighBar, curHigh);
                            break;
                    }
                    DrawLine("ZigZagUp" + swingCounterUp, AutoScale, CurrentBar - curLowBar,
                        curLow, CurrentBar - curHighBar, curHigh, zigZagColourUp, zigZagStyle, zigZagWidth);
                    break;
                case VisualizationTypes.GannStyle:
                    for (int i = CurrentBar - trendChangeBar; i >= 0; i--)
                        GannSwing.Set(i, high);
                    break;
            }
            #endregion

            #region Swing value output
            string output = null;
            string swingLabel = null;
            Color textColor = Color.Transparent;
            if (SwingLengthTypes.False != swingLengthType)
            {
                switch (swingLengthType)
                {
                    case SwingLengthTypes.Ticks:
                        output = curHighLength.ToString();
                        break;
                    case SwingLengthTypes.Ticks_And_Price:
                        output = curHighLength.ToString() + " / " + curHigh.ToString();
                        break;
                    case SwingLengthTypes.Points:
                        output = (curHighLength * TickSize).ToString();
                        break;
                    case SwingLengthTypes.Price:
                        output = curHigh.ToString();
                        break;
                    case SwingLengthTypes.Price_And_Points:
                        output = curHigh.ToString() + " / " + (curHighLength * TickSize).ToString();
                        break;
                    case SwingLengthTypes.Price_And_Ticks:
                        output = curHigh.ToString() + " / " + curHighLength.ToString();
                        break;
                    case SwingLengthTypes.Percent:
                        output = (Math.Round((100.0 / curLow * (curHighLength * TickSize)),
                            2, MidpointRounding.AwayFromZero)).ToString();
                        break;
                }
            }
            if (showSwingDuration)
            {
                if (SwingLengthTypes.False != swingLengthType)
                    output = output + " / " + curHighDuration.ToString();
                else
                    output = curHighDuration.ToString();
            }
            switch (curHighRelation)
            {
                case Relation.Higher:
                    swingLabel = "HH";
                    textColor = textColourHigher;
                    break;
                case Relation.Lower:
                    swingLabel = "LH";
                    textColor = textColorLower;
                    break;
                case Relation.Double:
                    swingLabel = "DT";
                    textColor = textColourDtb;
                    break;
            }
            if (output != null)
                DrawText("High" + swingCounterUp, AutoScale, output.ToString(),
                    CurrentBar - curHighBar, curHigh, textOffsetLength, textColor, textFont,
                    StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
            if (showSwingLabel)
                DrawText("UpLabel" + swingCounterUp, AutoScale, swingLabel,
                    CurrentBar - curHighBar, curHigh, textOffsetLabel, textColor, textFont,
                    StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
            if (showSwingPercent && curHighPercent != 0)
            {
                DrawText("UpPerc" + swingCounterUp, AutoScale, curHighPercent.ToString() + "%",
                    CurrentBar - curHighBar, curHigh, textOffsetPercent, textColor, textFont,
                    StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
            }
            if (showSwingTime)
            {
                DrawText("UpTime" + swingCounterUp, AutoScale, curHighTime.ToString(),
                    CurrentBar - curHighBar, curHigh, textOffsetTime, textColor, textFont,
                    StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
            }
            if (SwingVolumeTypes.False != swingVolumeType)
            {
                if (!CalculateOnBarClose)
                    signalBarVolumeDn = Volume[0];

                double swingVolume = 0.0;
                string volumeOutput = "";

                for (int i = 0; i < curHighDuration; i++)
                {
                    swingVolume = swingVolume + Volume[i];
                }

                if (!CalculateOnBarClose)
                    swingVolume = swingVolume + (Volume[CurrentBar - curLowBar] - signalBarVolumeUp);

                if (SwingVolumeTypes.Relativ == swingVolumeType)
                {
                    swingVolume = Math.Round(swingVolume / curHighDuration, 0, MidpointRounding.AwayFromZero);
                    // Output - Diameter sign isn't represented correct - use Latin capital letter O with stroke instead
                    volumeOutput = "\u00D8 ";
                }

                if (swingVolume > 10000000000)
                {
                    swingVolume = Math.Round(swingVolume / 1000000000, 0, MidpointRounding.AwayFromZero);
                    volumeOutput = volumeOutput + swingVolume.ToString() + "B";
                }
                else if (swingVolume > 10000000)
                {
                    swingVolume = Math.Round(swingVolume / 1000000, 0, MidpointRounding.AwayFromZero);
                    volumeOutput = volumeOutput + swingVolume.ToString() + "M";
                }
                else if (swingVolume > 10000)
                {
                    swingVolume = Math.Round(swingVolume / 1000, 0, MidpointRounding.AwayFromZero);
                    volumeOutput = volumeOutput + swingVolume.ToString() + "K";
                }
                else
                    volumeOutput = volumeOutput + swingVolume.ToString();

                DrawText("UpVolume" + swingCounterUp, AutoScale, volumeOutput,
                    CurrentBar - curHighBar, curHigh, textOffsetVolume, textColor, textFont,
                    StringAlignment.Center, Color.Transparent, Color.Transparent, 0);
            }
            // ====================================================================================
            #endregion

            Swings up = new Swings(curHighBar, curHigh, curHighLength, curHighDuration, curHighRelation);
            swingHighs.Add(up);
            upCount = swingHighs.Count - 1;
        }
        //#####################################################################
        #endregion

        #region Statistic
        //#########################################################################################
        private void upStatistic()
        {
            upCount = swingHighs.Count - 1;
            if (upCount == 0)
                return;

            overallUpLength = overallUpLength + swingHighs[upCount].length;
            overallAvgUpLength = Math.Round(overallUpLength / upCount, 0, MidpointRounding.AwayFromZero);

            overallUpDuration = overallUpDuration + swingHighs[upCount].duration;
            overallAvgUpDuration = Math.Round(overallUpDuration / upCount, 0, MidpointRounding.AwayFromZero);

            if (upCount >= statisticLength)
            {
                upLength = 0;
                upDuration = 0;
                for (int i = 0; i < statisticLength; i++)
                {
                    upLength = upLength + swingHighs[upCount - i].length;
                    upDuration = upDuration + swingHighs[upCount - i].duration;
                }
                avgUpLength = Math.Round(upLength / statisticLength, 0, MidpointRounding.AwayFromZero);
                avgUpDuration = Math.Round(upDuration / statisticLength, 0, MidpointRounding.AwayFromZero);
            }

            lengthList[1, 0].Value = upCount;
            lengthList[2, 0].Value = overallAvgUpLength;
            lengthList[3, 0].Value = avgUpLength;
            lengthList[4, 0].Value = overallAvgUpDuration;
            lengthList[5, 0].Value = avgUpDuration;

            if (lastHighRelation == Relation.Higher)
            {
                hhCount++;

                if (curHighRelation == Relation.Higher) hhCountHH++;
                if (curHighRelation == Relation.Lower) hhCountLH++;
                if (lastLowRelation == Relation.Higher) hhCountHL++;
                if (lastLowRelation == Relation.Lower) hhCountLL++;

                hhCountLHPercent = Math.Round(100.0 / hhCount * hhCountLH, 1, MidpointRounding.AwayFromZero);
                hhCountHHPercent = Math.Round(100.0 / hhCount * hhCountHH, 1, MidpointRounding.AwayFromZero);
                hhCountHLPercent = Math.Round(100.0 / hhCount * hhCountHL, 1, MidpointRounding.AwayFromZero);
                hhCountLLPercent = Math.Round(100.0 / hhCount * hhCountLL, 1, MidpointRounding.AwayFromZero);
            }

            if (lastHighRelation == Relation.Lower)
            {
                lhCount++;

                if (curHighRelation == Relation.Higher) lhCountHH++;
                if (curHighRelation == Relation.Lower) lhCountLH++;
                if (lastLowRelation == Relation.Higher) lhCountHL++;
                if (lastLowRelation == Relation.Lower) lhCountLL++;

                lhCountLHPercent = Math.Round(100.0 / lhCount * lhCountLH, 1, MidpointRounding.AwayFromZero);
                lhCountHHPercent = Math.Round(100.0 / lhCount * lhCountHH, 1, MidpointRounding.AwayFromZero);
                lhCountHLPercent = Math.Round(100.0 / lhCount * lhCountHL, 1, MidpointRounding.AwayFromZero);
                lhCountLLPercent = Math.Round(100.0 / lhCount * lhCountLL, 1, MidpointRounding.AwayFromZero);
            }

            if (lastHighRelation == Relation.Double)
            {
                dtCount++;

                if (curHighRelation == Relation.Higher) dtCountHH++;
                if (curHighRelation == Relation.Lower) dtCountLH++;
                if (lastLowRelation == Relation.Higher) dtCountHL++;
                if (lastLowRelation == Relation.Lower) dtCountLL++;

                dtCountLHPercent = Math.Round(100.0 / dtCount * dtCountLH, 1, MidpointRounding.AwayFromZero);
                dtCountHHPercent = Math.Round(100.0 / dtCount * dtCountHH, 1, MidpointRounding.AwayFromZero);
                dtCountHLPercent = Math.Round(100.0 / dtCount * dtCountHL, 1, MidpointRounding.AwayFromZero);
                dtCountLLPercent = Math.Round(100.0 / dtCount * dtCountLL, 1, MidpointRounding.AwayFromZero);
            }
            relationList[1, 0].Value = hhCount;
            relationList[2, 0].Value = hhCountHHPercent + "%";
            relationList[3, 0].Value = hhCountLHPercent + "%";
            relationList[4, 0].Value = hhCountHLPercent + "%";
            relationList[5, 0].Value = hhCountLLPercent + "%";

            relationList[1, 1].Value = lhCount;
            relationList[2, 1].Value = lhCountHHPercent + "%";
            relationList[3, 1].Value = lhCountLHPercent + "%";
            relationList[4, 1].Value = lhCountHLPercent + "%";
            relationList[5, 1].Value = lhCountLLPercent + "%";

            relationList[1, 2].Value = dtCount;
            relationList[2, 2].Value = dtCountHHPercent + "%";
            relationList[3, 2].Value = dtCountLHPercent + "%";
            relationList[4, 2].Value = dtCountHLPercent + "%";
            relationList[5, 2].Value = dtCountLLPercent + "%";
        }

        //#########################################################################################
        private void dnStatistic()
        {
            dnCount = swingLows.Count - 1;
            if (dnCount == 0)
                return;

            overallDnLength = overallDnLength + swingLows[dnCount].length;
            overallAvgDnLength = Math.Round(overallDnLength / dnCount, 0, MidpointRounding.AwayFromZero);

            overallDnDuration = overallDnDuration + swingLows[dnCount].duration;
            overallAvgDnDuration = Math.Round(overallDnDuration / dnCount, 0, MidpointRounding.AwayFromZero);

            if (dnCount >= statisticLength)
            {
                dnLength = 0;
                dnDuration = 0;
                for (int i = 0; i < statisticLength; i++)
                {
                    dnLength = dnLength + swingLows[dnCount - i].length;
                    dnDuration = dnDuration + swingLows[dnCount - i].duration;
                }
                avgDnLength = Math.Round(dnLength / statisticLength, 0, MidpointRounding.AwayFromZero);
                avgDnDuration = Math.Round(dnDuration / statisticLength, 0, MidpointRounding.AwayFromZero);
            }

            lengthList[1, 1].Value = dnCount;
            lengthList[2, 1].Value = overallAvgDnLength;
            lengthList[3, 1].Value = avgDnLength;
            lengthList[4, 1].Value = overallAvgDnDuration;
            lengthList[5, 1].Value = avgDnDuration;

            if (lastLowRelation == Relation.Lower)
            {
                llCount++;

                if (lastHighRelation == Relation.Higher) llCountHH++;
                if (lastHighRelation == Relation.Lower) llCountLH++;
                if (curLowRelation == Relation.Higher) llCountHL++;
                if (curLowRelation == Relation.Lower) llCountLL++;

                llCountLHPercent = Math.Round(100.0 / llCount * llCountLH, 1, MidpointRounding.AwayFromZero);
                llCountHHPercent = Math.Round(100.0 / llCount * llCountHH, 1, MidpointRounding.AwayFromZero);
                llCountHLPercent = Math.Round(100.0 / llCount * llCountHL, 1, MidpointRounding.AwayFromZero);
                llCountLLPercent = Math.Round(100.0 / llCount * llCountLL, 1, MidpointRounding.AwayFromZero);
            }

            if (lastLowRelation == Relation.Higher)
            {
                hlCount++;

                if (lastHighRelation == Relation.Higher) hlCountHH++;
                if (lastHighRelation == Relation.Lower) hlCountLH++;
                if (curLowRelation == Relation.Higher) hlCountHL++;
                if (curLowRelation == Relation.Lower) hlCountLL++;

                hlCountLHPercent = Math.Round(100.0 / hlCount * hlCountLH, 1, MidpointRounding.AwayFromZero);
                hlCountHHPercent = Math.Round(100.0 / hlCount * hlCountHH, 1, MidpointRounding.AwayFromZero);
                hlCountHLPercent = Math.Round(100.0 / hlCount * hlCountHL, 1, MidpointRounding.AwayFromZero);
                hlCountLLPercent = Math.Round(100.0 / hlCount * hlCountLL, 1, MidpointRounding.AwayFromZero);
            }

            if (lastLowRelation == Relation.Double)
            {
                dbCount++;

                if (lastHighRelation == Relation.Higher) dbCountHH++;
                if (lastHighRelation == Relation.Lower) dbCountLH++;
                if (curLowRelation == Relation.Higher) dbCountHL++;
                if (curLowRelation == Relation.Lower) dbCountLL++;

                dbCountLHPercent = Math.Round(100.0 / dbCount * dbCountLH, 1, MidpointRounding.AwayFromZero);
                dbCountHHPercent = Math.Round(100.0 / dbCount * dbCountHH, 1, MidpointRounding.AwayFromZero);
                dbCountHLPercent = Math.Round(100.0 / dbCount * dbCountHL, 1, MidpointRounding.AwayFromZero);
                dbCountLLPercent = Math.Round(100.0 / dbCount * dbCountLL, 1, MidpointRounding.AwayFromZero);
            }
            relationList[1, 3].Value = llCount;
            relationList[2, 3].Value = llCountHHPercent + "%";
            relationList[3, 3].Value = llCountLHPercent + "%";
            relationList[4, 3].Value = llCountHLPercent + "%";
            relationList[5, 3].Value = llCountLLPercent + "%";

            relationList[1, 4].Value = hlCount;
            relationList[2, 4].Value = hlCountHHPercent + "%";
            relationList[3, 4].Value = hlCountLHPercent + "%";
            relationList[4, 4].Value = hlCountHLPercent + "%";
            relationList[5, 4].Value = hlCountLLPercent + "%";

            relationList[1, 5].Value = dbCount;
            relationList[2, 5].Value = dbCountHHPercent + "%";
            relationList[3, 5].Value = dbCountLHPercent + "%";
            relationList[4, 5].Value = dbCountHLPercent + "%";
            relationList[5, 5].Value = dbCountLLPercent + "%";
        }
        //#########################################################################################
        #endregion

        #region CalculateLongRisk(double entryRM, double stopRM, double target100RM)
        //#####################################################################	
        /// <summary>
        /// Calculate risk for a long trade.
        /// </summary>
        private void CalculateLongRisk(double entryRM, double stopRM, double target100RM)
        {
            numericUpDownEntry.Value = (decimal)entryRM;
            numericUpDownStop.Value = (decimal)stopRM;
            numericUpDownTarget.Value = (decimal)target100RM;
            double lossRM = Math.Round(((entryRM - stopRM) * Instrument.MasterInstrument.PointValue), 2, MidpointRounding.AwayFromZero);
            int quantity = (int)Math.Truncate(accountSize / 100.0 * accountRisk / lossRM);
            double win100RM = Math.Round(((target100RM - entryRM) * Instrument.MasterInstrument.PointValue), 2, MidpointRounding.AwayFromZero);
            double rr100RM = Math.Round(win100RM / lossRM, 2, MidpointRounding.AwayFromZero);
            string cur = Instrument.MasterInstrument.Currency.ToString() + " ";

            toolStripLabelQty.Text = "Qty: " + quantity;
            toolStripLabelQty.ForeColor = Color.Green;
            toolStripLabelRiskReward.Text = "R/R: " + rr100RM;
            toolStripLabelLoss.Text = "Loss: " + lossRM;
            toolStripLabelProfit.Text = "Win: " + win100RM;
            if (rr100RM < 1.0)
            {
                toolStripLabelRiskReward.ForeColor = Color.Red;
                toolStripLabelLoss.ForeColor = Color.Red;
                toolStripLabelProfit.ForeColor = Color.Red;
            }
            else if (rr100RM < 1.6)
            {
                toolStripLabelRiskReward.ForeColor = Color.Black;
                toolStripLabelLoss.ForeColor = Color.Black;
                toolStripLabelProfit.ForeColor = Color.Black;
            }
            else
            {
                toolStripLabelRiskReward.ForeColor = Color.Green;
                toolStripLabelLoss.ForeColor = Color.Green;
                toolStripLabelProfit.ForeColor = Color.Green;
            }
        }
        //#####################################################################	
        #endregion

        #region CalculateShortRisk(double entryRM, double stopRM, double target100RM)
        //#####################################################################	
        /// <summary>
        /// Calculate risk for a short trade.
        /// </summary>
        private void CalculateShortRisk(double entryRM, double stopRM, double target100RM)
        {
            numericUpDownEntry.Value = (decimal)entryRM;
            numericUpDownStop.Value = (decimal)stopRM;
            numericUpDownTarget.Value = (decimal)target100RM;
            double lossRM = Math.Round(((stopRM - entryRM) * Instrument.MasterInstrument.PointValue), 2, MidpointRounding.AwayFromZero);
            int quantity = (int)Math.Truncate(accountSize / 100.0 * accountRisk / lossRM);
            double win100RM = Math.Round(((entryRM - target100RM) * Instrument.MasterInstrument.PointValue), 2, MidpointRounding.AwayFromZero);
            double rr100RM = Math.Round(win100RM / lossRM, 2, MidpointRounding.AwayFromZero);
            string cur = Instrument.MasterInstrument.Currency.ToString() + " ";

            toolStripLabelQty.Text = "Qty: " + quantity;
            toolStripLabelQty.ForeColor = Color.Red;
            toolStripLabelRiskReward.Text = "R/R: " + rr100RM;
            toolStripLabelLoss.Text = "Loss: " + lossRM;
            toolStripLabelProfit.Text = "Win: " + win100RM;
            if (rr100RM < 1.0)
            {
                toolStripLabelRiskReward.ForeColor = Color.Red;
                toolStripLabelLoss.ForeColor = Color.Red;
                toolStripLabelProfit.ForeColor = Color.Red;
            }
            else if (rr100RM < 1.6)
            {
                toolStripLabelRiskReward.ForeColor = Color.Black;
                toolStripLabelLoss.ForeColor = Color.Black;
                toolStripLabelProfit.ForeColor = Color.Black;
            }
            else
            {
                toolStripLabelRiskReward.ForeColor = Color.Green;
                toolStripLabelLoss.ForeColor = Color.Green;
                toolStripLabelProfit.ForeColor = Color.Green;
            }
        }
        //#####################################################################	
        #endregion

        #region Properties
        //#####################################################################
        #region Alerts
        //===================================================================
        [Category("Alerts")]
        [Description("Indicates if an alert is send to the alert window when an AB=CD signal occured.")]
        [Gui.Design.DisplayName("Use AB=CD alerts")]
        public bool UseAbcAlerts
        {
            get { return useAbcAlerts; }
            set { useAbcAlerts = value; }
        }
        [Category("Alerts")]
        [Description("Indicates if an alert is send to the alert window when an AB=CD entry is triggered.")]
        [Gui.Design.DisplayName("Use AB=CD entry alerts")]
        public bool UseAbcEntryAlerts
        {
            get { return useAbcEntryAlerts; }
            set { useAbcEntryAlerts = value; }
        }
        [Category("Alerts")]
        [Description("Represents the sound file name for an AB=CD long signal, which must be located in 'installation path/NinjaTrader 7/sounds'. e.g. 'Alert1.wav'.")]
        [Gui.Design.DisplayName("File name AC=CD long")]
        public String FileNameAbcLong
        {
            get { return fileNameAbcLong; }
            set { fileNameAbcLong = value; }
        }
        [Category("Alerts")]
        [Description("Represents the sound file name for an AB=CD long entry signal, which must be located in 'installation path/NinjaTrader 7/sounds'. e.g. 'Alert1.wav'.")]
        [Gui.Design.DisplayName("File name AC=CD long entry")]
        public String FileNameAbcLongEntry
        {
            get { return fileNameAbcLongEntry; }
            set { fileNameAbcLongEntry = value; }
        }
        [Category("Alerts")]
        [Description("Represents the sound file name for an AB=CD short signal, which must be located in 'installation path/NinjaTrader 7/sounds'. e.g. 'Alert1.wav'.")]
        [Gui.Design.DisplayName("File name AC=CD short")]
        public String FileNameAbcShort
        {
            get { return fileNameAbcShort; }
            set { fileNameAbcShort = value; }
        }
        [Category("Alerts")]
        [Description("Represents the sound file name for an AB=CD short entry signal, which must be located in 'installation path/NinjaTrader 7/sounds'. e.g. 'Alert1.wav'.")]
        [Gui.Design.DisplayName("File name AC=CD short entry")]
        public String FileNameAbcShortEntry
        {
            get { return fileNameAbcShortEntry; }
            set { fileNameAbcShortEntry = value; }
        }
        //===================================================================
        #endregion

        #region DataSeries
        // DataSeries =========================================================
        /// <summary>
        /// Represents the price position in relation to the swings.
        /// -2 = DT | -1 = LL and LH | 0 = price is nowhere | 1 = HH and HL | 2 = DB
        /// </summary>
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        public DataSeries SwingRelation
        {
            get { return swingRelation; }
        }
        /// <summary>
        /// Represents the trend. -1 = LL/LH | 0 = no trend | 1 = HH/HL
        /// </summary>
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        public DataSeries SwingTrend
        {
            get { return swingTrend; }
        }
        /// <summary>
        /// Represents the swing slope direction. -1 = down | 0 = init | 1 = up.
        /// </summary>
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        public int SwingSlope
        {
            get { return swingSlope; }
        }
        /// <summary>
        /// Indicates if a new swing high is found.
        /// </summary>
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        public bool NewHigh
        {
            get { return newHigh; }
        }
        /// <summary>
        /// Indicates if a new swing low is found.
        /// </summary>
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        public bool NewLow
        {
            get { return newLow; }
        }
        /// <summary>
        /// Represents the ABC signals. -2 = short entry | -1 = possible short entry 
        /// | 0 = no signal | 1 = possible long entry | 2 = long entry
        /// </summary>
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        public DataSeries AbcSignals
        {
            get { return abcSignals; }
        }
        /// <summary>
        /// Indicates if the swing direction changed form down to up swing.
        /// </summary>
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        public BoolSeries UpFlip
        {
            get { return upFlip; }
        }
        /// <summary>
        /// Indicates if the swing direction changed form up to down swing.
        /// </summary>
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        public BoolSeries DnFlip
        {
            get { return dnFlip; }
        }
        // Plots ==============================================================
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries DoubleBottom
        {
            get { return Values[0]; }
        }
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries LowerLow
        {
            get { return Values[1]; }
        }
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries HigherLow
        {
            get { return Values[2]; }
        }
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries DoubleTop
        {
            get { return Values[3]; }
        }
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries LowerHigh
        {
            get { return Values[4]; }
        }
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries HigherHigh
        {
            get { return Values[5]; }
        }
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries GannSwing
        {
            get { return Values[6]; }
        }
        //=====================================================================
        #endregion

        #region Parameters
        //=====================================================================
        /// <summary>
        /// Represents the swing size. e.g. 1 = small swings and 5 = bigger swings.
        /// </summary>
#if NT7
        [GridCategory("Parameters")]
#else
        [Category("Parameters")]
#endif
        [Description("Represents the swing size. e.g. 1 = small swings and 5 = bigger swings.")]
        [Gui.Design.DisplayName("Swing size")]
        public int SwingSize
        {
            get { return swingSize; }
            set { swingSize = Math.Max(1, value); }
        }
        /// <summary>
        /// Represents the swing type. Standard | Gann
        /// </summary>
#if NT7
        [GridCategory("Parameters")]
#else
        [Category("Parameters")]
#endif
        [Description("Represents the swing type. Standard | Gann")]
        [Gui.Design.DisplayName("Swing type")]
        public SwingTypes SwingType
        {
            get { return swingType; }
            set { swingType = value; }
        }
        /// <summary>
        /// Represents the double top/-bottom strength.
        /// </summary>
#if NT7
        [GridCategory("Parameters")]
#else
        [Category("Parameters")]
#endif
        [Description("Represents the double top/-bottom strength. Increase the value to get more DB/DT.")]
        [Gui.Design.DisplayName("Double top/-bottom strength")]
        public int DtbStrength
        {
            get { return dtbStrength; }
            set { dtbStrength = Math.Max(1, value); }
        }
        //=====================================================================
        #endregion

        #region Gann Swings
        //=====================================================================
        [Category("Gann Swings")]
        [Description("Indicates if inside bars are ignored. If set to true it is possible that between consecutive up/down bars are inside bars. Only used if calculationSize > 1.")]
        [Gui.Design.DisplayName("Ignore inside Bars")]
        public bool IgnoreInsideBars
        {
            get { return ignoreInsideBars; }
            set { ignoreInsideBars = value; }
        }
        [Category("Gann Swings")]
        [Description("Indicates if the bars are colorized.")]
        [Gui.Design.DisplayName("Paint bars")]
        public bool PaintBars
        {
            get { return paintBars; }
            set { paintBars = value; }
        }
        [Category("Gann Swings")]
        [Description("Indicates if the swings are updated if the last swing high/low is broken. Only used if calculationSize > 1.")]
        [Gui.Design.DisplayName("Use breakouts")]
        public bool UseBreakouts
        {
            get { return useBreakouts; }
            set { useBreakouts = value; }
        }
        //===================================================================
        #endregion

        #region Swing features
        //=====================================================================
        [Category("Swing features")]
        [Description("Represents the ABC pattern recognition mode.")]
        [Gui.Design.DisplayName("ABC Pattern")]
        public AbcPatternMode AbcPattern
        {
            get { return abcPattern; }
            set { abcPattern = value; }
        }
        [Category("Swing features")]
        [Description("Indicates if the Fibonacci extension is shown.")]
        [Gui.Design.DisplayNameAttribute("Fibonacci extensions")]
        public bool AddFibExt
        {
            get { return addFibExt; }
            set { addFibExt = value; }
        }
        [Category("Swing features")]
        [Description("Indicates if the fast Fibonacci retracement is shown.")]
        [Gui.Design.DisplayNameAttribute("Fibonacci retracement (fast)")]
        public bool AddFastFibRet
        {
            get { return addFastFibRet; }
            set { addFastFibRet = value; }
        }
        [Category("Swing features")]
        [Description("Indicates if the slow Fibonacci retracement is shown.")]
        [Gui.Design.DisplayNameAttribute("Fibonacci retracement (slow)")]
        public bool AddSlowFibRet
        {
            get { return addSlowFibRet; }
            set { addSlowFibRet = value; }
        }
        [Category("Swing features")]
        [Description("Represents the statistic position. If false no statistic is calculated.")]
        [Gui.Design.DisplayName("Statistic")]
        public StatisticPositions StatisticPosition
        {
            get { return statisticPosition; }
            set { statisticPosition = value; }
        }
        [Category("Swing features")]
        [Description("Represents the number of the last swings used for average swing length and duration calculation.")]
        [Gui.Design.DisplayNameAttribute("Statistic number of swings")]
        public int StatisticLength
        {
            get { return statisticLength; }
            set { statisticLength = Math.Max(1, value); }
        }
        [Category("Swing features")]
        [Description("Indicates if the risk management is displayed. If ABC pattern is on, it calculates the risk reward ratio and the trade quantity.")]
        [Gui.Design.DisplayNameAttribute("Risk management")]
        public bool ShowRiskManagement
        {
            get { return showRiskManagement; }
            set { showRiskManagement = value; }
        }
        [Category("Swing features")]
        [Description("Represents the account risk for each trade. Only used if risk management and ABC pattern is on.")]
        [Gui.Design.DisplayName("Account risk per trade")]
        public double AccountRisk
        {
            get { return accountRisk; }
            set { accountRisk = Math.Max(0.001, value); }
        }
        [Category("Swing features")]
        [Description("Represents the account size. Only used if risk management and ABC pattern is on.")]
        [Gui.Design.DisplayName("Account size")]
        public double AccountSize
        {
            get { return accountSize; }
            set { accountSize = Math.Max(1.0, value); }
        }
        [Category("Swing features")]
        [Description("Represents the ABC target in percent of the AB move.")]
        [Gui.Design.DisplayName("ABC target in percent")]
        public double AbcTarget
        {
            get { return abcTarget; }
            set { abcTarget = Math.Max(1.0, value); }
        }
        //=====================================================================
        #endregion

        #region Swings values
        //=====================================================================
        [Category("Swing values")]
        [Description("Indicates if the swing duration in bars is shown.")]
        [Gui.Design.DisplayName("Swing duration")]
        public bool ShowSwingDuration
        {
            get { return showSwingDuration; }
            set { showSwingDuration = value; }
        }
        [Category("Swing values")]
        [Description("Indicates if the swing label is shown (HH, HL, LL, LH, DB, DT).")]
        [Gui.Design.DisplayName("Swing labels")]
        public bool ShowSwingLabel
        {
            get { return showSwingLabel; }
            set { showSwingLabel = value; }
        }
        [Category("Swing values")]
        [Description("Indicates if the swing length is shown and in which style.")]
        [Gui.Design.DisplayName("Swing length")]
        public SwingLengthTypes SwingLengthType
        {
            get { return swingLengthType; }
            set { swingLengthType = value; }
        }
        [Category("Swing values")]
        [Description("Indicates if the swing percentage in relation to the last swing is shown.")]
        [Gui.Design.DisplayName("Swing percentage")]
        public bool ShowSwingPercent
        {
            get { return showSwingPercent; }
            set { showSwingPercent = value; }
        }
        [Category("Swing values")]
        [Description("Indicates if the swing time is shown.")]
        [Gui.Design.DisplayName("Swing time")]
        public bool ShowSwingTime
        {
            get { return showSwingTime; }
            set { showSwingTime = value; }
        }
        [Category("Swing values")]
        [Description("Represents the swing visualization type. Dots | Zig-Zag lines | Dots and lines | Gann style")]
        [Gui.Design.DisplayName("Swing visualization type")]
        public VisualizationTypes VisualizationType
        {
            get { return visualizationType; }
            set { visualizationType = value; }
        }
        [Category("Swing values")]
        [Description("Represents the swing volume mode.")]
        [Gui.Design.DisplayName("Swing volume")]
        public SwingVolumeTypes SwingVolumeType
        {
            get { return swingVolumeType; }
            set { swingVolumeType = value; }
        }
        //=====================================================================
        #endregion

        #region Visualize patterns
        //=====================================================================
        [Category("Visualize patterns")]
        [Description("Represents the line style for pattern lines.")]
        [Gui.Design.DisplayName("Line style")]
        public DashStyle AbcLineStyle
        {
            get { return abcLineStyle; }
            set { abcLineStyle = value; }
        }
        [Category("Visualize patterns")]
        [Description("Represents the line style for pattern ratio lines.")]
        [Gui.Design.DisplayName("Line style ratio")]
        public DashStyle AbcLineStyleRatio
        {
            get { return abcLineStyleRatio; }
            set { abcLineStyleRatio = value; }
        }
        [Category("Visualize patterns")]
        [Description("Represents the line width for pattern lines.")]
        [Gui.Design.DisplayName("Line width")]
        public int AbcLineWidth
        {
            get { return abcLineWidth; }
            set { abcLineWidth = Math.Max(1, value); }
        }
        [Category("Visualize patterns")]
        [Description("Represents the line width for pattern ratio lines.")]
        [Gui.Design.DisplayName("Line width ratio")]
        public int AbcLineWidthRatio
        {
            get { return abcLineWidthRatio; }
            set { abcLineWidthRatio = Math.Max(1, value); }
        }
        [XmlIgnore()]
        [Category("Visualize patterns")]
        [Description("Represents the text font for the displayed swing information.")]
        [Gui.Design.DisplayNameAttribute("Text font")]
        public Font AbcTextFont
        {
            get { return abcTextFont; }
            set { abcTextFont = value; }
        }
        [Browsable(false)]
        public string AbcTextFontSerialize
        {
            get { return NinjaTrader.Gui.Design.SerializableFont.ToString(abcTextFont); }
            set { abcTextFont = NinjaTrader.Gui.Design.SerializableFont.FromString(value); }
        }
        [Category("Visualize patterns")]
        [Description("Represents the offset value in pixels from within the text box area that display the swing label.")]
        [Gui.Design.DisplayName("Text offset label")]
        public int AbcTextOffsetLabel
        {
            get { return abcTextOffsetLabel; }
            set { abcTextOffsetLabel = Math.Max(1, value); }
        }
        [Category("Visualize patterns")]
        [Description("Represents the text colour for down patterns.")]
        [Gui.Design.DisplayName("Text colour down")]
        public Color AbcTextColourDn
        {
            get { return abcTextColourDn; }
            set { abcTextColourDn = value; }
        }
        [Browsable(false)]
        public string AbcTextColourDnSerialize
        {
            get { return NinjaTrader.Gui.Design.SerializableColor.ToString(abcTextColourDn); }
            set { abcTextColourDn = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
        }
        [Category("Visualize patterns")]
        [Description("Represents the text colour for up patterns.")]
        [Gui.Design.DisplayName("Text colour up")]
        public Color AbcTextColourUp
        {
            get { return abcTextColourUp; }
            set { abcTextColourUp = value; }
        }
        [Browsable(false)]
        public string AbcTextColourUpSerialize
        {
            get { return NinjaTrader.Gui.Design.SerializableColor.ToString(abcTextColourUp); }
            set { abcTextColourUp = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
        }
        [Category("Visualize patterns")]
        [Description("Represents the line colour for down patterns.")]
        [Gui.Design.DisplayName("Line colour down")]
        public Color AbcZigZagColourDn
        {
            get { return abcZigZagColourDn; }
            set { abcZigZagColourDn = value; }
        }
        [Browsable(false)]
        public string AbcZigZagColourDnSerialize
        {
            get { return NinjaTrader.Gui.Design.SerializableColor.ToString(abcZigZagColourDn); }
            set { abcZigZagColourDn = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
        }
        [Category("Visualize patterns")]
        [Description("Represents the line colour for up patterns.")]
        [Gui.Design.DisplayName("Line colour up")]
        public Color AbcZigZagColourUp
        {
            get { return abcZigZagColourUp; }
            set { abcZigZagColourUp = value; }
        }
        [Browsable(false)]
        public string AbcZigZagColourUpSerialize
        {
            get { return NinjaTrader.Gui.Design.SerializableColor.ToString(abcZigZagColourUp); }
            set { abcZigZagColourUp = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
        }
        [Category("Visualize patterns")]
        [Description("Represents the maximum value in percent for a retracement in relation to the last swing. The price must retrace between this two values, otherwise the pattern is not valid.")]
        [Gui.Design.DisplayName("Retracement maximum (percent)")]
        public double AbcMaxRetracement
        {
            get { return abcMaxRetracement; }
            set { abcMaxRetracement = Math.Max(1, Math.Min(99, value)); }
        }
        [Category("Visualize patterns")]
        [Description("Represents the minimum value in percent for a retracement in relation to the last swing. The price must retrace between this two values, otherwise the pattern is not valid.")]
        [Gui.Design.DisplayName("Retracement minimum (percent)")]
        public double AbcMinRetracement
        {
            get { return abcMinRetracement; }
            set { abcMinRetracement = Math.Max(1, Math.Min(99, value)); }
        }
        [Category("Visualize patterns")]
        [Description("Represents the entry line colour for down patterns.")]
        [Gui.Design.DisplayName("Entry line colour down")]
        public Color EntryLineColourDn
        {
            get { return entryLineColourDn; }
            set { entryLineColourDn = value; }
        }
        [Browsable(false)]
        public string EntryLineColourDnSerialize
        {
            get { return NinjaTrader.Gui.Design.SerializableColor.ToString(entryLineColourDn); }
            set { entryLineColourDn = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
        }
        [Category("Visualize patterns")]
        [Description("Represents the entry line colour for up patterns.")]
        [Gui.Design.DisplayName("Entry line colour up")]
        public Color EntryLineColourUp
        {
            get { return entryLineColourUp; }
            set { entryLineColourUp = value; }
        }
        [Browsable(false)]
        public string EntryLineColourUpSerialize
        {
            get { return NinjaTrader.Gui.Design.SerializableColor.ToString(entryLineColourUp); }
            set { entryLineColourUp = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
        }
        [Category("Visualize patterns")]
        [Description("Represents the line style for the entry lines.")]
        [Gui.Design.DisplayName("Entry line style")]
        public DashStyle EntryLineStyle
        {
            get { return entryLineStyle; }
            set { entryLineStyle = value; }
        }
        [Category("Visualize patterns")]
        [Description("Represents the line width for pattern lines.")]
        [Gui.Design.DisplayName("Entry line width")]
        public int EntryLineWidth
        {
            get { return entryLineWidth; }
            set { entryLineWidth = Math.Max(1, value); }
        }
        [Category("Visualize patterns")]
        [Description("If bar close above/below the entry retracement an entry is triggered.")]
        [Gui.Design.DisplayName("Entry retracement")]
        public double RetracementEntryValue
        {
            get { return retracementEntryValue; }
            set { retracementEntryValue = Math.Max(1.0, Math.Min(99.0, value)); }
        }
        [Category("Visualize patterns")]
        [Description("Indicates if entry arrows are displayed.")]
        [Gui.Design.DisplayName("Entry arrows")]
        public bool ShowEntryArrows
        {
            get { return showEntryArrows; }
            set { showEntryArrows = value; }
        }
        [Category("Visualize patterns")]
        [Description("Indicates if historical entry lines are displayed.")]
        [Gui.Design.DisplayName("Entry line historical")]
        public bool ShowHistoricalEntryLine
        {
            get { return showHistoricalEntryLine; }
            set { showHistoricalEntryLine = value; }
        }
        [Category("Visualize patterns")]
        [Description("Represents the offset value in ticks from the high/low that triggered the entry.")]
        [Gui.Design.DisplayName("Entry arrows offset (ticks)")]
        public int YTickOffset
        {
            get { return yTickOffset; }
            set { yTickOffset = Math.Max(0, value); }
        }
        //=====================================================================
        #endregion

        #region Visualize swings
        //=====================================================================
        [XmlIgnore()]
        [Category("Visualize swings")]
        [Description("Represents the text font for the swing value output.")]
        [Gui.Design.DisplayNameAttribute("Text font")]
        public Font TextFont
        {
            get { return textFont; }
            set { textFont = value; }
        }
        [Browsable(false)]
        public string TextFontSerialize
        {
            get { return NinjaTrader.Gui.Design.SerializableFont.ToString(textFont); }
            set { textFont = NinjaTrader.Gui.Design.SerializableFont.FromString(value); }
        }
        [Category("Visualize swings")]
        [Description("Represents the text offset in pixel for the swing length/duration.")]
        [Gui.Design.DisplayNameAttribute("Text offset length / duration")]
        public int TextOffsetLength
        {
            get { return textOffsetLength; }
            set { textOffsetLength = Math.Max(1, value); }
        }
        [Category("Visualize swings")]
        [Description("Represents the text offset in pixel for the swing labels.")]
        [Gui.Design.DisplayNameAttribute("Text offset swing labels")]
        public int TextOffsetLabel
        {
            get { return textOffsetLabel; }
            set { textOffsetLabel = Math.Max(1, value); }
        }
        [Category("Visualize swings")]
        [Description("Represents the text offset in pixel for the retracement value.")]
        [Gui.Design.DisplayNameAttribute("Text offset percent")]
        public int TextOffsetPercent
        {
            get { return textOffsetPercent; }
            set { textOffsetPercent = Math.Max(1, value); }
        }
        [Category("Visualize swings")]
        [Description("Represents the text offset in pixel for the time value.")]
        [Gui.Design.DisplayNameAttribute("Text offset time")]
        public int TextOffsetTime
        {
            get { return textOffsetTime; }
            set { textOffsetTime = Math.Max(1, value); }
        }
        [Category("Visualize swings")]
        [Description("Represents the text offset in pixel for the swing volume.")]
        [Gui.Design.DisplayNameAttribute("Text offset volume")]
        public int TextOffsetVolume
        {
            get { return textOffsetVolume; }
            set { textOffsetVolume = Math.Max(1, value); }
        }
        [Category("Visualize swings")]
        [Description("Represents the colour of the swing value output for higher swings.")]
        [Gui.Design.DisplayNameAttribute("Text colour higher")]
        public Color TextColourHigher
        {
            get { return textColourHigher; }
            set { textColourHigher = value; }
        }
        [Browsable(false)]
        public string TextColourHigherSerialize
        {
            get { return NinjaTrader.Gui.Design.SerializableColor.ToString(textColourHigher); }
            set { textColourHigher = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
        }
        [Category("Visualize swings")]
        [Description("Represents the colour of the swing value output for lower swings.")]
        [Gui.Design.DisplayNameAttribute("Text colour lower")]
        public Color TextColourLower
        {
            get { return textColorLower; }
            set { textColorLower = value; }
        }
        [Browsable(false)]
        public string TextColourLowerSerialize
        {
            get { return NinjaTrader.Gui.Design.SerializableColor.ToString(textColorLower); }
            set { textColorLower = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
        }
        [Category("Visualize swings")]
        [Description("Represents the colour of the swing value output for double bottoms/-tops.")]
        [Gui.Design.DisplayNameAttribute("Text colour DTB")]
        public Color TextColourDtb
        {
            get { return textColourDtb; }
            set { textColourDtb = value; }
        }
        [Browsable(false)]
        public string TextColourDTBSerialize
        {
            get { return NinjaTrader.Gui.Design.SerializableColor.ToString(textColourDtb); }
            set { textColourDtb = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
        }
        [Category("Visualize swings")]
        [Description("Represents the colour of the zig-zag up lines.")]
        [Gui.Design.DisplayNameAttribute("Zig-Zag colour up")]
        public Color ZigZagColourUp
        {
            get { return zigZagColourUp; }
            set { zigZagColourUp = value; }
        }
        [Browsable(false)]
        public string ZigZagColourUpSerialize
        {
            get { return NinjaTrader.Gui.Design.SerializableColor.ToString(zigZagColourUp); }
            set { zigZagColourUp = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
        }
        [Category("Visualize swings")]
        [Description("Represents the colour of the zig-zag down lines.")]
        [Gui.Design.DisplayNameAttribute("Zig-Zag colour down")]
        public Color ZigZagColourDn
        {
            get { return zigZagColourDn; }
            set { zigZagColourDn = value; }
        }
        [Browsable(false)]
        public string ZigZagColourDnSerialize
        {
            get { return NinjaTrader.Gui.Design.SerializableColor.ToString(zigZagColourDn); }
            set { zigZagColourDn = NinjaTrader.Gui.Design.SerializableColor.FromString(value); }
        }
        [Category("Visualize swings")]
        [Description("Represents the line style of the zig-zag lines.")]
        [Gui.Design.DisplayNameAttribute("Zig-Zag style")]
        public DashStyle ZigZagStyle
        {
            get { return zigZagStyle; }
            set { zigZagStyle = value; }
        }
        [Category("Visualize swings")]
        [Description("Represents the line width of the zig-zag lines.")]
        [Gui.Design.DisplayNameAttribute("Zig-Zag width")]
        public int ZigZagWidth
        {
            get { return zigZagWidth; }
            set { zigZagWidth = Math.Max(1, value); }
        }
        //=====================================================================
        #endregion
        //#####################################################################
        #endregion
    }
}

#region PriceActionSwing.Utility
namespace PriceActionSwing.Utility
{
    /// <summary>
    /// Represents the possible swing calculation types. Standard | Gann
    /// </summary>
    public enum SwingTypes
    {
        /// <summary>
        /// Represents the standard swing calculation.
        /// </summary>
        Standard,
        /// <summary>
        /// Represents the Gann swing calculation.
        /// </summary>
        Gann,
    }
}
#endregion

#region NinjaScript generated code. Neither change nor remove.
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    public partial class Indicator : IndicatorBase
    {
        private PriceActionSwing[] cachePriceActionSwing = null;

        private static PriceActionSwing checkPriceActionSwing = new PriceActionSwing();

        /// <summary>
        /// PriceActionSwing calculate swings, visualize them in different ways and display several information about them. Features: ABC pattern recognition | Risk management | Fibonacci retracements/-extensions | Statistic
        /// </summary>
        /// <returns></returns>
        public PriceActionSwing PriceActionSwing(int dtbStrength, int swingSize, SwingTypes swingType)
        {
            return PriceActionSwing(Input, dtbStrength, swingSize, swingType);
        }

        /// <summary>
        /// PriceActionSwing calculate swings, visualize them in different ways and display several information about them. Features: ABC pattern recognition | Risk management | Fibonacci retracements/-extensions | Statistic
        /// </summary>
        /// <returns></returns>
        public PriceActionSwing PriceActionSwing(Data.IDataSeries input, int dtbStrength, int swingSize, SwingTypes swingType)
        {
            if (cachePriceActionSwing != null)
                for (int idx = 0; idx < cachePriceActionSwing.Length; idx++)
                    if (cachePriceActionSwing[idx].DtbStrength == dtbStrength && cachePriceActionSwing[idx].SwingSize == swingSize && cachePriceActionSwing[idx].SwingType == swingType && cachePriceActionSwing[idx].EqualsInput(input))
                        return cachePriceActionSwing[idx];

            lock (checkPriceActionSwing)
            {
                checkPriceActionSwing.DtbStrength = dtbStrength;
                dtbStrength = checkPriceActionSwing.DtbStrength;
                checkPriceActionSwing.SwingSize = swingSize;
                swingSize = checkPriceActionSwing.SwingSize;
                checkPriceActionSwing.SwingType = swingType;
                swingType = checkPriceActionSwing.SwingType;

                if (cachePriceActionSwing != null)
                    for (int idx = 0; idx < cachePriceActionSwing.Length; idx++)
                        if (cachePriceActionSwing[idx].DtbStrength == dtbStrength && cachePriceActionSwing[idx].SwingSize == swingSize && cachePriceActionSwing[idx].SwingType == swingType && cachePriceActionSwing[idx].EqualsInput(input))
                            return cachePriceActionSwing[idx];

                PriceActionSwing indicator = new PriceActionSwing();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.DtbStrength = dtbStrength;
                indicator.SwingSize = swingSize;
                indicator.SwingType = swingType;
                Indicators.Add(indicator);
                indicator.SetUp();

                PriceActionSwing[] tmp = new PriceActionSwing[cachePriceActionSwing == null ? 1 : cachePriceActionSwing.Length + 1];
                if (cachePriceActionSwing != null)
                    cachePriceActionSwing.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cachePriceActionSwing = tmp;
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
        /// PriceActionSwing calculate swings, visualize them in different ways and display several information about them. Features: ABC pattern recognition | Risk management | Fibonacci retracements/-extensions | Statistic
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.PriceActionSwing PriceActionSwing(int dtbStrength, int swingSize, SwingTypes swingType)
        {
            return _indicator.PriceActionSwing(Input, dtbStrength, swingSize, swingType);
        }

        /// <summary>
        /// PriceActionSwing calculate swings, visualize them in different ways and display several information about them. Features: ABC pattern recognition | Risk management | Fibonacci retracements/-extensions | Statistic
        /// </summary>
        /// <returns></returns>
        public Indicator.PriceActionSwing PriceActionSwing(Data.IDataSeries input, int dtbStrength, int swingSize, SwingTypes swingType)
        {
            return _indicator.PriceActionSwing(input, dtbStrength, swingSize, swingType);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// PriceActionSwing calculate swings, visualize them in different ways and display several information about them. Features: ABC pattern recognition | Risk management | Fibonacci retracements/-extensions | Statistic
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.PriceActionSwing PriceActionSwing(int dtbStrength, int swingSize, SwingTypes swingType)
        {
            return _indicator.PriceActionSwing(Input, dtbStrength, swingSize, swingType);
        }

        /// <summary>
        /// PriceActionSwing calculate swings, visualize them in different ways and display several information about them. Features: ABC pattern recognition | Risk management | Fibonacci retracements/-extensions | Statistic
        /// </summary>
        /// <returns></returns>
        public Indicator.PriceActionSwing PriceActionSwing(Data.IDataSeries input, int dtbStrength, int swingSize, SwingTypes swingType)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.PriceActionSwing(input, dtbStrength, swingSize, swingType);
        }
    }
}
#endregion
