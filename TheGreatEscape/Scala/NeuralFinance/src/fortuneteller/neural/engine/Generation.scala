package fortuneteller.neural.engine
import fortuneteller.indicators._


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
import fortuneteller.neural.output.OutputTarget
class Generation(outputTarget:OutputTarget,  predictWindow:Int,allIndicators:List[NeuralIndicator], selIndicators:List[SelectedIndicator]) {
  val cat = org.apache.log4j.Category.getInstance("fortuneteller");
  val window = new fortuneteller.utils.SlidingWindow(predictWindow); 
  val startIndexCsv = 100

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
        var el = new fortuneteller.utils.Element(3 + getNetworkInputCount);
        el.a(0) = csv.getDouble(1)
        el.a(1) = csv.getDouble(2)
        el.a(2) = csv.getDouble(3)
        var baseIndex = 4
        var indiResIndex = 3 // CLose and HMA value part of buffer, so start next to it
        for (i <- allIndicators) {          
          val count = i.getInputCount
          val inputDbls = new ListBuffer[Double]();
          for (j <- baseIndex until baseIndex + count) {
            inputDbls.append(csv.getDouble(j))
          }
          baseIndex = baseIndex + count
          if (selected(i)){
	          val indiRes = i.processInput(inputDbls.toList)
	          for (v <- indiRes) {
	            el.a(indiResIndex) = v;
	            indiResIndex = indiResIndex + 1
	          }
          }
        }

        window.add(el);
        if (window.isFull()) {
          val currEval = window.getElement(0)
          for (i <- 0 until getNetworkInputCount) {
            inputData.setData(i, currEval.a(i +3));
          }
          val out=outputTarget.processInput(window)
          idealData.setData(0, out);
          output.add(inputData, idealData);
        }
      }
    }

  }  
  
  
  

  
  def generate():Unit={
    
  }
}