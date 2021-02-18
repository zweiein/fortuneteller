package fortuneteller.neural.engine

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
import fortuneteller.neural.datasets._

class CollectorServer(thePath: java.io.File, fileName:String) extends BasicIndicator(false) {
  val cat = org.apache.log4j.Category.getInstance("fortuneteller");
  val path = thePath;
  var holder = new InstrumentHolder();
  var rowsDownloaded: Int = 0
  val outputIndicator = new fortuneteller.indicators.ClosingPrice()
  for (req <- outputIndicator.requestedValues.toList){
   cat.info(req)
    requestData(req.valueSpec)
  }
  val indicators = IndicatorLoader.loadFullDataset("default,public", List())  
  for (i <- indicators) {
    cat.info("Req " + i.getDescription)
    for (req <- i.requestedValues.toList)
      requestData(req.valueSpec)
  }
  
  override def notifyPacket(packet: IndicatorPacket): Unit = {
    val security = packet.getArgs()(1);
    val when = java.lang.Long.parseLong(packet.getArgs()(0));
    val key = security.toLowerCase();
      if (holder.record(when, 2, packet.getArgs())) {
        rowsDownloaded = rowsDownloaded + 1;
      }
   }

  /**
   * Write the files that were collected.
   */
  def writeCollectedFile(): Unit = {
    val targetFile = new java.io.File(path, fileName)
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
      writeCollectedFile();
  }

  /**
   * @return The number of rows downloaded.
   */
  def getRowsDownloaded(): Int = {
    return rowsDownloaded;
  }

}