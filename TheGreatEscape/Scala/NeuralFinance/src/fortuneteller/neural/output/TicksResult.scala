package fortuneteller.neural.output
import fortuneteller.neural.Config
import scala.collection.mutable.ListBuffer
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import fortuneteller.neural.calibration.CalibratedIndicator
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
import fortuneteller.neural.Config
import scala.collection.mutable.ListBuffer
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import fortuneteller.neural.calibration.CalibratedIndicator
import fortuneteller.indicators.NeuralIndicator

class TicksResult(tickSize: Double, calibration: List[CalibratedIndicator]) extends OutputTarget with fortuneteller.indicators.IndicatorConfig {
  name = "TicksResult"
  version="1"
  val cat = org.apache.log4j.Category.getInstance("fortuneteller.tickresult");
  val field = getNormalization(lookupCalibration("output.tickresult", calibration)); //new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.DONCHIAN_WIDTH_MAX,Config.getConfig.DONCHIAN_WIDTH_MIN,1,0);

  override def getNormalizer(): org.encog.util.arrayutil.NormalizedField = {
    field
  }

  var minTicks = 1000.0
  var maxTicks = (-1000.0)

  override def initCalibration(): Unit = {
  }
  override def calibration(window: fortuneteller.utils.SlidingWindow): Unit = {
    val currEval = window.getElement(0)
    val max = (window.calcMax(0, 1) - currEval.a(0)) / tickSize;
    val min = (window.calcMin(0, 1) - currEval.a(0)) / tickSize;
    this.maxTicks = Math.max(this.maxTicks, max);
    this.minTicks = Math.min(this.minTicks, min);
  }
  override def finalizeCalibration(): List[CalibratedIndicator] = {
    var out = new ListBuffer[CalibratedIndicator]();
	maxTicks=findMax(maxTicks,minTicks)
	minTicks=(-findMax(maxTicks,minTicks))	    
    cat.info(CalibratedIndicator(name, 1, "output.tickresult", maxTicks, minTicks, 1, -1))
    out.append(CalibratedIndicator(name, 1, "output.tickresult", maxTicks, minTicks, 1, -1))
    out.toList
  }
  override def processInput(window: fortuneteller.utils.SlidingWindow): Double = {
    val currEval = window.getElement(0)
    val max = (window.calcMax(0, 1) - currEval.a(0)) / tickSize;
    val min = (window.calcMin(0, 1) - currEval.a(0)) / tickSize;
    var o = 0.0;
    if (Math.abs(max) > Math.abs(min)) {
      o = max;
    } else {
      o = min;
    }
    o = field.normalize(o);
    o
  }

}