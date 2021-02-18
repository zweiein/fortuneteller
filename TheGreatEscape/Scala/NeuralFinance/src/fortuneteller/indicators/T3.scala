package fortuneteller.indicators
import fortuneteller.neural.Config
import scala.collection.mutable.ListBuffer
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import fortuneteller.neural.calibration.CalibratedIndicator
class T3(inputWindow:Int,config:String,calibration:List[CalibratedIndicator]) extends NeuralIndicator with IndicatorConfig{
	name=getConfigField(config,"name")
	description=getConfigField(config,"description")
	version=getConfigField(config,"version")
	val cat = org.apache.log4j.Category.getInstance("fortuneteller.t3");
	val field = getNormalization(lookupCalibration("t3.diff",calibration));//new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.DONCHIAN_WIDTH_MAX,Config.getConfig.DONCHIAN_WIDTH_MIN,1,0);

	requestedValues.append(new RequestedValue(true,"T3(7, 3, 0.7)[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"T3(14, 3, 0.7)[" + inputWindow + "]"))
	var minDiff=1000.0
	var maxDiff=(-1000.0)	


	override def initCalibration():Unit={
	}	
	override def calibration(values:List[Double]):Unit={
	 
	  val upper=inputWindow
	  for (i<-0 until (inputWindow)){
	    if (values(i)!=0 && values(upper+i)!=0){
		  val diff=values(i)-values(upper+i)
		  if (Math.abs(diff)>1000){
		    cat.info(values)
		  }
		  minDiff = Math.min(minDiff, diff);
		  maxDiff = Math.max(maxDiff, diff);	
	    }
	  }
	} 
	override def finalizeCalibration():List[CalibratedIndicator]={
	  var out=new ListBuffer[CalibratedIndicator]();
	  out.append(CalibratedIndicator(name, version.toInt,"t3.diff",maxDiff,minDiff,1,-1))
	  cat.info("T3 min=" + minDiff + " max=" + maxDiff)
	  out.toList
	}
	override def processInput(values:List[Double] ):List[Double]={
	  var out=new ListBuffer[Double]();
	  val upper=inputWindow
	  for (i<-0 until (inputWindow)){
	     val diff=(values(i)-values(upper+i))
	      out.append(field.normalize(diff))
	  }
	  out.toList
	}	
	override def getInputCount():Int={
	 (2* inputWindow)
	}
	override def getOutputCount():Int={
	 (inputWindow)
	}	

}