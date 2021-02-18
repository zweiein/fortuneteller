package fortuneteller.neural.engine
import fortuneteller.indicators._
import fortuneteller.neural.output.OutputTarget

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
class Calibration(outputTarget:OutputTarget, predictWindow:Int,allIndicators:List[NeuralIndicator], selIndicators:List[SelectedIndicator]) {
  val cat = org.apache.log4j.Category.getInstance("fortuneteller");
  val window = new fortuneteller.utils.SlidingWindow(predictWindow); 
  val startIndexCsv = 100
  var maxTicks: Int = (-1000)
  var minTicks: Int = 1000  
  def selected(i:NeuralIndicator):Boolean={    
	for (j<-selIndicators) if (j.name==i.getName && j.version==i.getVersion) return true
	false
  } 

  def calibrateFile(file: java.io.File): List[CalibratedIndicator] = {
    for (i <- allIndicators)    {     
      if (selected(i))
    	  i.initCalibration
    }
    window.clear()
    var c = 0
    val csv = new ReadCSV(file.toString(), true, CSVFormat.ENGLISH);
    while (csv.next()) {
      c = c + 1
      if (c > startIndexCsv) {

        var el = new fortuneteller.utils.Element(3);
        el.a(0) = csv.getDouble(1)
        el.a(1) = csv.getDouble(2)
        el.a(2) = csv.getDouble(3)
        var baseIndex = 4
        for (i <- allIndicators) {          
          val count = i.getInputCount
          val inputDbls = new ListBuffer[Double]();
          for (j <- baseIndex until baseIndex + count) {            
            inputDbls.append(csv.getDouble(j))
          }	
          baseIndex = baseIndex + count
          if (selected(i)){
	
        	  i.calibration(inputDbls.toList)
          }
        }
        window.add(el);
        if (window.isFull()) {
          outputTarget.calibration(window)
        }

      }
    }
    var out=new ListBuffer[CalibratedIndicator]();
    for (i <- allIndicators)    {      
      if (selected(i))
    	  out.appendAll(i.finalizeCalibration)
    }
    out.appendAll(outputTarget.finalizeCalibration)
    out.toList

  }
}


