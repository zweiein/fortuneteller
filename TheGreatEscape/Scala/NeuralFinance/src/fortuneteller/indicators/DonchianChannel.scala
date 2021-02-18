package fortuneteller.indicators
import fortuneteller.neural.Config
import scala.collection.mutable.ListBuffer
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import fortuneteller.neural.calibration.CalibratedIndicator
class DonchianChannel(inputWindow:Int,config:String,calibration:List[CalibratedIndicator]) extends NeuralIndicator with IndicatorConfig{
	name=getConfigField(config,"name")
	description=getConfigField(config,"description")
	version=getConfigField(config,"version")
	val cat = org.apache.log4j.Category.getInstance("fortuneteller.donchian");
	val fieldWidth = getNormalization(lookupCalibration("donchian.width",calibration));//new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.DONCHIAN_WIDTH_MAX,Config.getConfig.DONCHIAN_WIDTH_MIN,1,0);
	val fieldDirection = getNormalization(lookupCalibration("donchian.direction",calibration));//new NormalizedField(NormalizationAction.Normalize,"diff",1,-1,1,-1);

	requestedValues.append(new RequestedValue(true,"DonchianChannel(" + getConfigField(config,"parameters") +").Lower[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"DonchianChannel(" + getConfigField(config,"parameters") +").Upper[" + inputWindow + "]"))
	var minVal=1000.0
	var maxVal=(-1000.0)	
	var minWidth=1000.0
	var maxWidth=(-1000.0)

	override def initCalibration():Unit={
	}	
	override def calibration(values:List[Double]):Unit={

	  val upper=inputWindow
	  for (i<-0 until (inputWindow)){
		  val width=Math.abs(values(i)-values(upper+i))
		  minWidth = Math.min(minWidth, width);
		  maxWidth = Math.max(maxWidth, width);
		  minVal = Math.min(minVal, values(i));
		  maxVal = Math.max(maxVal, values(i));		  
	  }
	} 
	override def finalizeCalibration():List[CalibratedIndicator]={
	  var out=new ListBuffer[CalibratedIndicator]();
	  out.append(CalibratedIndicator(name, version.toInt,"donchian.width", maxWidth, minWidth,1,0))
	  out.append(CalibratedIndicator(name, version.toInt,"donchian.direction", 1, -1,1,-1))
	  out.toList
	}
	override def processInput(values:List[Double] ):List[Double]={
	  var out=new ListBuffer[Double]();
	  val upper=inputWindow
	  for (i<-0 until (inputWindow)){

	     val width=Math.abs(values(i)-values(upper+i))
	     cat.debug("DonchianDiff " + values(i) + " vs " + values(upper+i) + " giving " + width)
		 out.append(fieldWidth.normalize(width))
		 if (i==0)
		   out.append(fieldDirection.normalize(0))
		 else if (values(i)==values(i-1))
		    out.append(fieldDirection.normalize(0))
		 else if (values(i)>values(i-1))
		    out.append(fieldDirection.normalize(1))
		 else if (values(i)<values(i-1))
		    out.append(fieldDirection.normalize(-1))

		  
	  }
	  out.toList
	}	
	override def getInputCount():Int={
	 (2* inputWindow)
	}
	override def getOutputCount():Int={
	 (2* inputWindow)
	}	

}