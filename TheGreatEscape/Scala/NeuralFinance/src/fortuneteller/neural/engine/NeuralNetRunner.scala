package fortuneteller.neural.engine
import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.io.PrintWriter;
import fortuneteller.indicators._
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

class NeuralNetRunner(allIndicators:List[NeuralIndicator], selIndicators:List[SelectedIndicator], outputNormalizer:NormalizedField ,theMethod: MLRegression) extends BasicIndicator(true) {
  val cat = org.apache.log4j.Category.getInstance("fortuneteller");

  def selected(i:NeuralIndicator):Boolean={    
	for (j<-selIndicators) if (j.name==i.getName && j.version==i.getVersion) return true
	false
  } 
  def getNetworkInputCount(): Int = {
    var res = 0;
    for (i <- allIndicators) {
      if (selected(i))
    	  res = res + i.getOutputCount
    }
    res
  }   

  val method = theMethod;
  var holder = new InstrumentHolder();
  var rowsDownloaded: Int = 0

  val outputIndicator = new fortuneteller.indicators.ClosingPrice()
  for (req <- outputIndicator.requestedValues.toList)
    requestData(req.valueSpec)

  for (i <- allIndicators) {
    for (req <- i.requestedValues.toList)
     if (selected(i)) requestData(req.valueSpec)
  }  
  override def notifyPacket(packet: IndicatorPacket): Unit = {
    val security = packet.getArgs()(1);
    val when = java.lang.Long.parseLong(packet.getArgs()(0));
    val key = security.toLowerCase();
    val input = new BasicMLData(1 + getNetworkInputCount);
    var baseIndex = 5
    var indiResIndex = 1
    for (i <- allIndicators) {
      if (selected(i)){
      val count = i.getInputCount
      val inputDbls = new ListBuffer[Double]();
      for (j <- baseIndex until baseIndex + count) {
        inputDbls.append(CSVFormat.EG_FORMAT.parse(packet.getArgs()(j)))
      }
      baseIndex = baseIndex + count
      //cat.info(i.getDescription + " " + inputDbls)
      val indiRes = i.processInput(inputDbls.toList)
      for (v <- indiRes) {
        
        input.setData(indiResIndex, if (v.toString=="NaN") 0 else v);
        indiResIndex = indiResIndex + 1
      }
    }}

    val result = this.method.compute(input);
    var d = result.getData(0);
    //cat.info("Output " + d)
    d = outputNormalizer.deNormalize(d);
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
  override def notifyTermination(): Unit = {

  }
}