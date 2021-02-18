package fortuneteller.neural

import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.io.PrintWriter;

import org.encog.Encog;
import org.encog.EncogError;
import org.encog.cloud.indicator.basic.BasicIndicator;
import org.encog.cloud.indicator.basic.InstrumentHolder;
import org.encog.cloud.indicator.server.IndicatorLink;
import org.encog.cloud.indicator.server.IndicatorPacket;
import org.encog.ml.MLRegression;
import org.encog.ml.data.MLData;
import org.encog.ml.data.basic.BasicMLData;
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import org.encog.util.csv.CSVFormat;
import scala.collection.JavaConversions._
import org.encog.ml.data.buffer.BufferedMLDataSet;
import org.encog.util.csv.CSVFormat;
import org.encog.util.csv.ReadCSV;
import org.encog.neural.networks.BasicNetwork
import org.encog.engine.network.activation._
import org.encog.neural.networks.layers.BasicLayer
import scala.collection.mutable.ListBuffer

class IndicatorManager(theMethod: MLRegression, thePath: java.io.File) extends BasicIndicator(theMethod != null) {
  val cat = org.apache.log4j.Category.getInstance("fortuneteller");
  val method = theMethod;
  val path = thePath;
  val startIndexCsv = 100
  var holder = new InstrumentHolder();
  var rowsDownloaded: Int = 0

  val outputIndicator = Config.loadOutputIndicator
  for (req <- outputIndicator.requestedValues.toList)
    requestData(req.valueSpec)

  val indies = Config.loadIndicators
  for (i <- indies) {
    cat.info("Req " + i.getName)
    for (req <- i.requestedValues.toList)
      requestData(req.valueSpec)
  }

  //	val fieldDifference = new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.DIFF_RANGE,-Config.getConfig.DIFF_RANGE,1,-1);
  val fieldOutcome = new NormalizedField(NormalizationAction.Normalize, "out", Config.getConfig.PIP_RANGE, -Config.getConfig.PIP_RANGE, 1, -1);

  /**
   * The path to the training file.
   */
  var trainingFile = new File(this.path, Config.getConfig.FILENAME_TRAIN); ;
  val window = new fortuneteller.utils.SlidingWindow(Config.getConfig.PREDICT_WINDOW); //new WindowDouble(Config.PREDICT_WINDOW);

  /**
   * Called to notify the indicator that a bar has been received.
   * @param packet The packet received.
   */

  override def notifyPacket(packet: IndicatorPacket): Unit = {
    val security = packet.getArgs()(1);
    val when = java.lang.Long.parseLong(packet.getArgs()(0));
    val key = security.toLowerCase();

    if (this.method == null) {
      if (holder.record(when, 2, packet.getArgs())) {
        rowsDownloaded = rowsDownloaded + 1;
      }
    } else {
      //QQE(14, 5).Value1[0]
      val input = new BasicMLData(1 + getNetworkInputCount);
      var baseIndex = 3
      var indiResIndex = 1
      for (i <- indies) {
        cat.debug(i.getName)
        val count = i.getInputCount
        val inputDbls = new ListBuffer[Double]();
        for (j <- baseIndex until baseIndex + count) {
          cat.debug("Input " + j + " =" + packet.getArgs()(j))
          inputDbls.append(CSVFormat.EG_FORMAT.parse(packet.getArgs()(j)))
        }
        baseIndex = baseIndex + count
        val indiRes = i.processInput(inputDbls.toList)
        for (v <- indiRes) {
          input.setData(indiResIndex, v);
          indiResIndex = indiResIndex + 1
        }
      }

      val result = this.method.compute(input);
      var d = result.getData(0);
      d = this.fieldOutcome.deNormalize(d);
      cat.debug("Out " + d)
      val args: List[String] = List(
        "?", // line 1
        "?", // line 2
        "?", // line 3
        CSVFormat.EG_FORMAT.format(d, Encog.DEFAULT_PRECISION), // bar 1
        "?", // bar 2
        "?", // bar 3
        "?", // arrow 1
        "?"); // arrow 2

      this.getLink().writePacket(IndicatorLink.PACKET_IND, args.toArray);
    }
  }

  def getTrainingInputCount(): Int = {
    var res = 0;
    for (i <- indies) {
      res = res + i.getInputCount
    }
    res
  }
  def getNetworkInputCount(): Int = {
    var res = 0;
    for (i <- indies) {
      res = res + i.getOutputCount
    }
    res
  }  
  
  var maxTicks: Int = (-1000)
  var minTicks: Int = 1000
  /**
   * Used to calibrate the training file.
   * @param file The file to consider.
   */
  def calibrateFile(file: File): Unit = {
    for (i <- indies) {
      i.initCalibration
    }
    window.clear()
    var c = 0
    val csv = new ReadCSV(file.toString(), true, CSVFormat.ENGLISH);
    while (csv.next()) {
      c = c + 1
      if (c > startIndexCsv) {
        var el = new fortuneteller.utils.Element(1);
        el.a(0) = csv.getDouble(1)
        var baseIndex = 2
        var indiResIndex = 1 // CLose value part of buffer, so start next to it
        for (i <- indies) {
          val count = i.getInputCount
          val inputDbls = new ListBuffer[Double]();
          for (j <- baseIndex until baseIndex + count) {
            inputDbls.append(csv.getDouble(j))
          }
          baseIndex = baseIndex + count
          i.calibration(inputDbls.toList)
        }
        window.add(el);
        if (window.isFull()) {

          val currEval = window.getElement(0)
          val max = (this.window.calcMax(0, 1) - currEval.a(0)) / Config.getConfig.PIP_SIZE;
          val min = (this.window.calcMin(0, 1) - currEval.a(0)) / Config.getConfig.PIP_SIZE;
          var o = 0.0;
          if (Math.abs(max) > Math.abs(min)) {
            o = max;
          } else {
            o = min;
          }
          this.maxTicks = Math.max(this.maxTicks, o.toInt);
          this.minTicks = Math.min(this.minTicks, o.toInt);
        }

      }
    }
    for (i <- indies) {
      i.finalizeCalibration
    }
    cat.info("Results. Min=" + minTicks + " Max=" + maxTicks)
  }
  /**
   * Process the individual training file.
   * @param file The training file to process.
   * @param output The data set to output to.
   */
  def processFile(file: File, output: BufferedMLDataSet): Unit = {

    val inputData = new BasicMLData(output.getInputSize());
    val idealData = new BasicMLData(output.getIdealSize());

    val csv = new ReadCSV(file.toString(), true, CSVFormat.ENGLISH);
    window.clear()
    var c = 0
    while (csv.next()) {
      c = c + 1
      if (c > startIndexCsv) {
        var el = new fortuneteller.utils.Element(1 + getNetworkInputCount);
        el.a(0) = csv.getDouble(1)
        var baseIndex = 2
        var indiResIndex = 1 // CLose value part of buffer, so start next to it
        for (i <- indies) {
          cat.debug(i.getName)
          val count = i.getInputCount
          val inputDbls = new ListBuffer[Double]();
          cat.debug("Looping from " + baseIndex + " until " + (baseIndex + count))
          for (j <- baseIndex until baseIndex + count) {
            cat.debug("Input " + j + " =" + csv.getDouble(j))
            inputDbls.append(csv.getDouble(j))
          }
          baseIndex = baseIndex + count
          val indiRes = i.processInput(inputDbls.toList)
          for (v <- indiRes) {
            el.a(indiResIndex) = v;
            indiResIndex = indiResIndex + 1
          }
        }
        cat.debug(el)
        window.add(el);
        if (window.isFull()) {

          val currEval = window.getElement(0)
          val max = (this.window.calcMax(0, 1) - currEval.a(0)) / Config.getConfig.PIP_SIZE;
          val min = (this.window.calcMin(0, 1) - currEval.a(0)) / Config.getConfig.PIP_SIZE;
          var o = 0.0;
          if (Math.abs(max) > Math.abs(min)) {
            o = max;
          } else {
            o = min;
          }
          for (i <- 0 until getNetworkInputCount) {
            inputData.setData(i, currEval.a(i + 1));
          }
          cat.debug("Specified output " + o)
          o = this.fieldOutcome.normalize(o);
          idealData.setData(0, o);
          output.add(inputData, idealData);
        }
      }
    }
    cat.info("Done")
  }
  def createTANH(): BasicNetwork = {
    val network = new BasicNetwork();
    network.addLayer(new BasicLayer(null, true, getNetworkInputCount()));
    network.addLayer(new BasicLayer(new ActivationTANH(), true, 20));//if( (2*getNetworkInputCount())>20)(2*getNetworkInputCount()) else 20));
    network.addLayer(new BasicLayer(new ActivationTANH(), false, 1));
    network.getStructure().finalizeStructure();
    network.reset();
    network;
  }

  def createElliot(): BasicNetwork = {
    val network = new BasicNetwork();
    network.addLayer(new BasicLayer(null, true, getNetworkInputCount()));
    network.addLayer(new BasicLayer(new ActivationElliottSymmetric(), true, 20));
    network.addLayer(new BasicLayer(new ActivationElliottSymmetric(), false, 1));
    network.getStructure().finalizeStructure();
    network.reset();
    network;
  }
  /**
   * Called to generate the training file.
   */
  def generate(): Unit = {
    this.trainingFile.delete();
    val output = new BufferedMLDataSet(this.trainingFile);
    //output.beginLoad(Config.getConfig.INPUT_WINDOW, 1);
    output.beginLoad(getNetworkInputCount(), 1);
    val file = new File(this.path, Config.getConfig.FILENAME_COLLECT)
    processFile(file, output);
    output.endLoad();
    output.close();
    /*  
	// create a network
	val network = org.encog.util.simple.EncogUtility.simpleFeedForward(
			getTrainingInputCount(), 
			20, 
			20, 
			1, 
			true);	

	// save the network and the training
	org.encog.persist.EncogDirectoryPersistence.saveObject(new File(this.path,Config.getConfig.NETWORK_FILE), network);
	*/
  }

  /**
   * Called to calibrate the data.  Does not actually do anything, other
   * than display a range report.
   */
  def calibrate(): Unit = {

    //		this.maxDifference = Double.NEGATIVE_INFINITY;
    //		this.minDifference = Double.POSITIVE_INFINITY;
    //		this.maxPIPs = Integer.MIN_VALUE;
    //		this.minPIPs = Integer.MAX_VALUE;
    //		
    val file = new File(this.path, Config.getConfig.FILENAME_COLLECT)
    calibrateFile(file)
    /*	
		System.out.println("Max difference: " + this.maxDifference);
		System.out.println("Min difference: " + this.minDifference);
		System.out.println("Max PIPs: " + this.maxPIPs);
		System.out.println("Min PIPs: " + this.minPIPs);
		System.out.println("\nSuggested calibration: ");
		System.out.println("DIFF_RANGE = " + (int)(Math.max(this.maxDifference,Math.abs(this.minDifference)) * 1.2) );
		System.out.println("PIP_RANGE = " + (int)(Math.max(this.maxPIPs,Math.abs(this.minPIPs)) * 1.2) );

*/ }

  /**
   * Write the files that were collected.
   */
  def writeCollectedFile(): Unit = {
    val targetFile = new java.io.File(path, Config.getConfig.FILENAME_COLLECT)

    try {
      val outFile = new FileWriter(targetFile);
      val out = new PrintWriter(outFile);

      // output header
      out.print("\"WHEN\"");
      var index = 0;
      for (str <- this.getDataRequested()) {
        var str2 = "";

        // strip off [ if needed
        var ix = str.indexOf('[');
        if (ix != -1) {
          str2 = str.substring(0, ix).trim();
        } else {
          str2 = str;
        }

        val c = getDataCount().get(index);
        index = index + 1
        if (c <= 1) {
          out.print(",\"" + str2 + "\"");
        } else {
          for (i <- 0 to c) {
            out.print(",\"" + str2 + "-b" + i + "\"");
          }
        }
      }
      out.println();

      // output data

      for (key <- holder.getSorted()) {
        val str = holder.getData().get(key);
        out.println(key + "," + str);
      }

      out.close();
    } catch {
      case e: IOException => throw new EncogError(e);
    }
  }

  /**
   * Notify on termination, write the collected file.
   */

  override def notifyTermination(): Unit = {
    if (this.method == null) {
      writeCollectedFile();
    }
  }

  /**
   * @return The number of rows downloaded.
   */
  def getRowsDownloaded(): Int = {
    return rowsDownloaded;
  }

}