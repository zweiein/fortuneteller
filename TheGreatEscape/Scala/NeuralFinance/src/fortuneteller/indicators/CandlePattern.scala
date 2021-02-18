package fortuneteller.indicators
import fortuneteller.neural.Config
import scala.collection.mutable.ListBuffer
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import fortuneteller.neural.calibration.CalibratedIndicator
class CandlePattern(inputWindow: Int, config: String, calibration: List[CalibratedIndicator]) extends NeuralIndicator with IndicatorConfig {
  name = getConfigField(config, "name")
  description = getConfigField(config, "description")
  version = getConfigField(config, "version")
  val cat = org.apache.log4j.Category.getInstance("fortuneteller.candlepattern");
  val field = getNormalization(lookupCalibration("candle.value", calibration)); //new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.CANDLE_PATTERN_MAX,Config.getConfig.CANDLE_PATTERM_MIN,1,-1);
  val fieldRatio1 = getNormalization(lookupCalibration("candle.ratio1", calibration)); //new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.CANDLE_PATTERN_MAX,Config.getConfig.CANDLE_PATTERM_MIN,1,-1);
  val fieldRatio2 = getNormalization(lookupCalibration("candle.ratio2", calibration));
  val fieldRatio3 = getNormalization(lookupCalibration("candle.ratio3", calibration));
  
  requestedValues.append(new RequestedValue(true, "CLOSE[" + inputWindow + "]"))
  requestedValues.append(new RequestedValue(true, "OPEN[" + inputWindow + "]"))
  requestedValues.append(new RequestedValue(true, "HIGH[" + inputWindow + "]"))
  requestedValues.append(new RequestedValue(true, "LOW[" + inputWindow + "]"))
  var minDiff = 1000.0
  var maxDiff = (-1000.0)
  var maxBar = 0.0
  var maxRatio1 = 0.0
  var maxRatio2 = 0.0
  var maxRatio3 = 0.0

  override def initCalibration(): Unit = {
  }
  override def calibration(values: List[Double]): Unit = {
    val closeBase = 0
    val openBase = (inputWindow)
    val highBase = ((inputWindow) * 2)
    val lowBase = ((inputWindow) * 3)
    for (i <- 0 until (inputWindow)) {
      val highBody = Math.max(values(closeBase + i), values(openBase + i));
      val lowBody = Math.min(values(closeBase + i), values(openBase + i));
      val diff = values(closeBase + i) - values(openBase + i)
      val body = Math.abs(diff)
      val full = Math.abs(values(highBase + i) - values(lowBase + i))
      val top = Math.abs(values(highBase + i) - highBody)
      val bottom = Math.abs(lowBody - values(lowBase + i))
      
      val ratio1=if (full==0)0 else 100.0*(body / full)
      val ratio2=if (full==0)0 else 100.0*(top / full)
      val ratio3=if (full==0)0 else 100.0*(bottom / full)
      maxDiff = Math.max(maxDiff, diff);
      minDiff = Math.min(minDiff, diff);
      maxRatio1 = Math.max(maxRatio1, ratio1);
      maxRatio2 = Math.max(maxRatio2, ratio2);
      maxRatio3 = Math.max(maxRatio3, ratio3);
    }
  }
  override def finalizeCalibration(): List[CalibratedIndicator] = {
    var out = new ListBuffer[CalibratedIndicator]();
	  maxDiff=findMax(maxDiff,minDiff)
	  minDiff=(-findMax(maxDiff,minDiff))	      
    out.append(CalibratedIndicator(name, version.toInt, "candle.value", maxDiff,minDiff, 1, -1))
    out.append(CalibratedIndicator(name, version.toInt, "candle.ratio1",100, 0, 1, 0))
    out.append(CalibratedIndicator(name, version.toInt, "candle.ratio2",100, 0, 1, 0))
    out.append(CalibratedIndicator(name, version.toInt, "candle.ratio3",100, 0, 1, 0))
    out.toList
  }
  override def processInput(values: List[Double]): List[Double] = {
    var out = new ListBuffer[Double]();
    val closeBase = 0
    val openBase = (inputWindow)
    val highBase = ((inputWindow) * 2)
    val lowBase = ((inputWindow) * 3)
    for (i <- 0 until (inputWindow)) {

      val highBody = Math.max(values(closeBase + i), values(openBase + i));
      val lowBody = Math.min(values(closeBase + i), values(openBase + i));
      val diff = values(closeBase + i) - values(openBase + i)
      val body = Math.abs(diff)
      val full = Math.abs(values(highBase + i) - values(lowBase + i))
      val top = Math.abs(values(highBase + i) - highBody)
      val bottom = Math.abs(lowBody - values(lowBase + i))
      
      val ratio1=if (full==0)0 else 100.0*(body / full)
      val ratio2=if (full==0)0 else 100.0*(top / full)
      val ratio3=if (full==0)0 else 100.0*(bottom / full)      
      
      out.append(fieldRatio1.normalize(ratio1))
      if (version=="2"){
    	out.append(fieldRatio2.normalize(ratio2))
      	out.append(fieldRatio3.normalize(ratio3))
      }
      out.append(field.normalize(diff))
    }

    out.toList
  }
  override def getInputCount(): Int = {
    4 * (inputWindow)
  }
  override def getOutputCount(): Int = {
    if (version=="2")
    	4*inputWindow
    else
        2*inputWindow
  }

}