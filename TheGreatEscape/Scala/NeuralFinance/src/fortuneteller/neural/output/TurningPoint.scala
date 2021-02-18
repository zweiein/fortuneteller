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

class TurningPoint(tickSize: Double, calibration: List[CalibratedIndicator]) extends OutputTarget with fortuneteller.indicators.IndicatorConfig {
  name = "TurningPoint"
  version="1"
  val cat = org.apache.log4j.Category.getInstance("fortuneteller.turningpoint");
  val field = getNormalization(lookupCalibration("output.turningpoint", calibration)); //new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.DONCHIAN_WIDTH_MAX,Config.getConfig.DONCHIAN_WIDTH_MIN,1,0);

  override def getNormalizer(): org.encog.util.arrayutil.NormalizedField = {
    field
  }
  var minTicks = 1000.0
  var maxTicks = (-1000.0)

  override def initCalibration(): Unit = {
  }
  override def calibration(window: fortuneteller.utils.SlidingWindow): Unit = {

  }
  override def finalizeCalibration(): List[CalibratedIndicator] = {
    var out = new ListBuffer[CalibratedIndicator]();
    out.append(CalibratedIndicator(name, 1, "output.turningpoint", 1, -1, 1, -1))
    out.toList
  }
  override def processInput(window: fortuneteller.utils.SlidingWindow): Double = {
    var o =window.calcTurningPoint(2, 0)
    o = field.normalize(o);
    o
  }

}