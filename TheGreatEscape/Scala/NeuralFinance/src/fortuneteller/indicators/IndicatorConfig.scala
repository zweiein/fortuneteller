package fortuneteller.indicators
import scala.xml._
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import fortuneteller.neural.calibration.CalibratedIndicator

trait IndicatorConfig {
  val categ = org.apache.log4j.Category.getInstance("fortuneteller.indicatorconfig");
  def getConfigField(config: String, field: String): String = {
    val data = XML.loadString(config)
    val lookup = ("@" + field).toString
    val retVal = (data \\ lookup).text
    retVal
  }

  def getNormalization(cab: CalibratedIndicator): NormalizedField = {
    if (cab == null)
      new NormalizedField(NormalizationAction.Normalize, "NONE", 1, 0, 1, 0);
    else{
    //  categ.info("Using normalization " + cab.name + " " + cab.maxVal + " " + cab.minVal)
      new NormalizedField(NormalizationAction.Normalize, cab.normId, cab.maxVal, cab.minVal, cab.normMax, cab.normMin);
    }
  }

  def findMax(origMax: Double, origMin: Double): Double = {
   fortuneteller.utils.MiscUtils.findMax(origMax,origMin)
  }
}