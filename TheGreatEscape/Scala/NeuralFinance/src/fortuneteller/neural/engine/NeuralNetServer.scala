package fortuneteller.neural.engine
import fortuneteller.indicators._

import scala.xml._

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
import org.encog.cloud.indicator.IndicatorConnectionListener
import org.encog.cloud.indicator.IndicatorFactory
import org.encog.cloud.indicator.IndicatorListener
import org.encog.cloud.indicator.server.IndicatorLink
import org.encog.cloud.indicator.server.IndicatorServer
import org.encog.ml.MLRegression
import org.encog.ml.MLResettable
import org.encog.ml.factory.MLMethodFactory
import org.encog.ml.factory.MLTrainFactory
import org.encog.ml.train.strategy.RequiredImprovementStrategy
import org.encog.neural.networks.training.propagation.manhattan.ManhattanPropagation
import org.encog.persist.EncogDirectoryPersistence
import org.encog.util.simple.EncogUtility
import org.apache.log4j.PropertyConfigurator;
import fortuneteller.neural.calibration.CalibratedIndicator
import fortuneteller.neural.output._
class NeuralNetServer(thePort: Int, nconfig: NeuralNetConfig) extends IndicatorConnectionListener {
  val cat = org.apache.log4j.Category.getInstance("fortuneteller");
  def loadOutputIndicator(calibration:List[CalibratedIndicator]):OutputTarget={
    nconfig.output match {
      case "TicksResult" => new fortuneteller.neural.output.TicksResult(nconfig.tickSize, calibration)
      case "RenkoCount" => new fortuneteller.neural.output.RenkoCount(nconfig.tickSize, calibration)
      case "TurningPoint" => new fortuneteller.neural.output.TurningPoint(nconfig.tickSize, calibration)
      case "MaxTurningPoint" => new fortuneteller.neural.output.MaxTurningPoint(nconfig.tickSize, calibration)
      case _ => new fortuneteller.neural.output.TicksResult(nconfig.tickSize, calibration)
    }
  }
 
  
  
  val PORT = thePort
  val dirFile=nconfig.dataset.toString().split(",")
  val networkFile=new File("data\\neuralnet\\" + dirFile(0) + "\\" + dirFile(1),nconfig.configId + ".egb")  
  var selectedIndicators =
    for ( entry<- nconfig.xmlData \\ "neural" \\ "indicators" \\ "indicator")yield SelectedIndicator((entry \ "@name").text,(entry \ "@version").text)    
    
  def run(): Unit = {
    var method: MLRegression = null;
    def printSelIndics():String={
      var res=""
      for (i<-selectedIndicators){
        res=res + i.name + ","
      }
      res
    }
    cat.info("Exec " + nconfig.configId  + " (" + printSelIndics + ")" + " " + nconfig.predictionBars + " " + nconfig.output)
    cat.info("(" + nconfig.network + "," + nconfig.hiddenLayers +  "," + nconfig.trainingMinutes + ")")
	val calFile=new File("data\\calibration\\" + dirFile(0) + "\\" + dirFile(1), nconfig.configId + ".xml")   	
	val xmlConf=new java.util.Scanner(calFile).useDelimiter("\\Z").next()
	val calibList=fortuneteller.neural.calibration.CalibratedIndicator.fromXMLDoc(xmlConf)
	val reloadedIndicators= fortuneteller.neural.datasets.IndicatorLoader.loadFullDataset(nconfig.dataset,calibList.toList)
//	val outputNormalizer=Calibration.constructOutputNormalization(calibList.toList)
	val outputTarget:OutputTarget=loadOutputIndicator(calibList.toList)  
	val generator=new Generation(outputTarget, nconfig.predictionBars,reloadedIndicators, selectedIndicators.toList )
	val inputNodes=generator.getNetworkInputCount   
    val regObj = EncogDirectoryPersistence.loadObject(networkFile)
    regObj match {
      case g2: MLRegression => { method = g2; cat.info("Indicator is ready") }
      case _ => cat.warn("Unknown")
    }
    cat.info("Waiting for connections on port " + PORT);
    val server = new IndicatorServer(PORT);
    server.addListener(this);
    server.addIndicatorFactory(new IndicatorFactory() {
      override def getName(): String = {
        return "NeuralIndicator";
      }
      override def create(): IndicatorListener = {
        return new NeuralNetRunner(reloadedIndicators,selectedIndicators.toList, outputTarget.getNormalizer, method);
      }
    });
    server.start();
  }

  override def notifyConnections(link: IndicatorLink, hasOpened: Boolean): Unit = {
    if (hasOpened) {
      cat.info("Connection from " + link.getSocket().toString()
        + " established.");
    } else if (!hasOpened) {
      cat.info("Connection from " + link.getSocket().toString()
        + " terminated.");
    }
  }
}