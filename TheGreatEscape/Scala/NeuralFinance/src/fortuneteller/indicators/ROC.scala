package fortuneteller.indicators
import fortuneteller.neural.Config
import scala.collection.mutable.ListBuffer
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import fortuneteller.neural.calibration.CalibratedIndicator
class ROC(inputWindow:Int,config:String,calibration:List[CalibratedIndicator]) extends NeuralIndicator with IndicatorConfig{
	name=getConfigField(config,"name")
	description=getConfigField(config,"description")
	version=getConfigField(config,"version")
	val cat = org.apache.log4j.Category.getInstance("fortuneteller.roc");
	val field = getNormalization(lookupCalibration("roc.value",calibration));//new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.RSQUARED_MAX,Config.getConfig.RSQUARED_MIN,1,0);
	requestedValues.append(new RequestedValue(true,"ROC(" + getConfigField(config,"parameters") +")[" + inputWindow + "]"))
	var minVal=1000.0
	var maxVal=(-1000.0)	
	

	override def initCalibration():Unit={
	}	
	override def calibration(values:List[Double]):Unit={

	  for (i<-0 until (inputWindow)){
		  minVal = Math.min(minVal, values(i));
		  maxVal = Math.max(maxVal, values(i));
	  }
	} 
	override def finalizeCalibration():List[CalibratedIndicator]={
	  var out=new ListBuffer[CalibratedIndicator]();
	  out.append(CalibratedIndicator(name, version.toInt,"roc.value", maxVal, minVal,1,-1))
	  out.toList
	}
	override def processInput(values:List[Double] ):List[Double]={
	  var out=new ListBuffer[Double]();
	  for (i<-0 until (inputWindow)){
	   out.append(field.normalize(values(i)))
	  }
	  out.toList
	}	
	override def getInputCount():Int={
	 ( inputWindow)
	}
	override def getOutputCount():Int={
	 ( inputWindow)
	}	

}