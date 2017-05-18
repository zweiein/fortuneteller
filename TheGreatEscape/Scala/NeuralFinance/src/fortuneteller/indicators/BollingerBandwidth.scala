package fortuneteller.indicators
import fortuneteller.neural.Config
import scala.collection.mutable.ListBuffer
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import fortuneteller.neural.calibration.CalibratedIndicator
class BollingerBandwidth(inputWindow: Int, config: String, calibration: List[CalibratedIndicator]) extends NeuralIndicator with IndicatorConfig {

  val cat = org.apache.log4j.Category.getInstance("fortuneteller.bollinger");

  var minWidth = 1000.0
  var maxWidth = (-1000.0)
  name = getConfigField(config, "name")
  description = getConfigField(config, "description")
  version = getConfigField(config, "version")

  val field = getNormalization(lookupCalibration("bollinger.width", calibration)); //new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.BOLLINGER_MAX,Config.getConfig.BOLLINGER_MIN,1,0);
  requestedValues.append(new RequestedValue(true, "CLOSE[" + inputWindow + "]"))
  requestedValues.append(new RequestedValue(true, "Bollinger(" + getConfigField(config, "parameters") + ").Lower[" + inputWindow + "]"))
  requestedValues.append(new RequestedValue(true, "Bollinger(" + getConfigField(config, "parameters") + ").Upper[" + inputWindow + "]"))

  override def initCalibration(): Unit = {
  }
  override def calibration(values: List[Double]): Unit = {
    val lower = 3
    val upper = 6
    for (i <- 0 until (inputWindow)) {
      val width = Math.abs(values(lower + i) - values(upper + i))
      minWidth = Math.min(minWidth, width);
      maxWidth = Math.max(maxWidth, width);
    }
  }
  override def finalizeCalibration(): List[CalibratedIndicator] = {
    var out = new ListBuffer[CalibratedIndicator]();
    if (version == "1") {
      out.append(CalibratedIndicator(name, version.toInt, "bollinger.width", maxWidth, minWidth, 1, 0))
    } else
      out.append(CalibratedIndicator(name, version.toInt, "bollinger.width", maxWidth, -maxWidth, 1, -1))
    out.toList
  }
  override def processInput(values: List[Double]): List[Double] = {
    var out = new ListBuffer[Double]();
    val lower = 3
    val upper = 6
    for (i <- 0 until (inputWindow)) {
      val width = Math.abs(values(lower + i) - values(upper + i))
      if (version == "1") {
        out.append(field.normalize(width))
      }
      if (version == "2") {
        val useIndex = if (i == 0) 1 else i
        if (values(useIndex) < values(useIndex - 1)) {
          out.append(field.normalize(-width))
        } else
          out.append(field.normalize(width))
      }
    }
    out.toList
  }
  override def getInputCount(): Int = {
    (3 * inputWindow)
  }
  override def getOutputCount(): Int = {
    (inputWindow)
  }

}