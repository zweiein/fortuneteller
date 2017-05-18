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
import fortuneteller.neural.calibration.CalibratedIndicator
import fortuneteller.neural.output._
case class SelectedIndicator(name:String, version:String)

class NeuralNetCreator(nconfig:NeuralNetConfig) {
 
  def loadOutputIndicator(calibration:List[CalibratedIndicator]):OutputTarget={
    nconfig.output match {
      case "TicksResult" => new fortuneteller.neural.output.TicksResult(nconfig.tickSize, calibration)
      case "TicksTotalResult" => new fortuneteller.neural.output.TicksTotalResult(nconfig.tickSize, calibration)
      case "RenkoCount" => new fortuneteller.neural.output.RenkoCount(nconfig.tickSize, calibration)
      case "TurningPoint" => new fortuneteller.neural.output.TurningPoint(nconfig.tickSize, calibration)
      case "MaxTurningPoint" => new fortuneteller.neural.output.MaxTurningPoint(nconfig.tickSize, calibration)
      case _ => new fortuneteller.neural.output.TicksResult(nconfig.tickSize, calibration)
    }
  }
  val  outputTarget:OutputTarget=loadOutputIndicator(List())
  var selectedIndicators =
    for ( entry<- nconfig.xmlData \\ "neural" \\ "indicators" \\ "indicator")yield SelectedIndicator((entry \ "@name").text,(entry \ "@version").text)    
  
  def createNeuralNet():Unit={
	val dirFile=nconfig.dataset.toString().split(",")
	val fCollected=new File("data\\collected\\" + dirFile(0) + "\\" + dirFile(1), nconfig.instrument + "." + nconfig.resolution + ".csv")   
	val calibrator=new Calibration(outputTarget,  nconfig.predictionBars,nconfig.allIndicators, selectedIndicators.toList)
	val calibList=calibrator.calibrateFile(fCollected)	
	val calFile=new File("data\\calibration\\" + dirFile(0) + "\\" + dirFile(1), nconfig.configId + ".xml")   	
	val xml=fortuneteller.neural.calibration.CalibratedIndicator.toXml(calibList)
	calFile.delete()
	val cout = new java.io.FileWriter(calFile.getAbsolutePath())
	cout.write(xml.toString)
	cout.close	
	
	
	val reloadedIndicators= fortuneteller.neural.datasets.IndicatorLoader.loadFullDataset(nconfig.dataset,calibList)
	val reloadedTarget=loadOutputIndicator(calibList)
	val generator=new Generation(reloadedTarget, nconfig.predictionBars,reloadedIndicators, selectedIndicators.toList)
	val inputNodes=generator.getNetworkInputCount
	val outputNodes=1
	val trainingFile=new File("data\\training\\" + dirFile(0) + "\\" + dirFile(1), nconfig.configId + ".dat")   
    trainingFile.delete();
    val output = new BufferedMLDataSet(trainingFile);
    output.beginLoad(inputNodes, outputNodes);
    generator.processFile(fCollected, output);
    output.endLoad();
    output.close();

    val networkFile=new File("data\\neuralnet\\" + dirFile(0) + "\\" + dirFile(1),nconfig.configId + ".egb")  
    val training=new Training(nconfig,inputNodes,nconfig.hiddenLayers,nconfig.hiddenLayerNodes,nconfig.trainingMinutes,trainingFile,networkFile)
    training.train
  }
}