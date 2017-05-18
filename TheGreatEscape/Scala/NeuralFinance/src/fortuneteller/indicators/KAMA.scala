package fortuneteller.indicators
import fortuneteller.neural.Config
import scala.collection.mutable.ListBuffer
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import fortuneteller.neural.calibration.CalibratedIndicator
class KAMA(inputWindow:Int,config:String,calibration:List[CalibratedIndicator]) extends NeuralIndicator with IndicatorConfig{
	name=getConfigField(config,"name")
	description=getConfigField(config,"description")
	version=getConfigField(config,"version")

	val cat = org.apache.log4j.Category.getInstance("fortuneteller.kama");
	val field = getNormalization(lookupCalibration("kama.value",calibration));//new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.LAGUERRE_MAX,Config.getConfig.LAGUERRE_MIN,1,0);
	requestedValues.append(new RequestedValue(true,"CLOSE[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"LOW[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"HIGH[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"KAMA(" + getConfigField(config,"parameters") +")[" + inputWindow + "]"))
	var minVal=1000.0
	var maxVal=(-1000.0)	
	

	override def initCalibration():Unit={
	}	
	override def calibration(values:List[Double]):Unit={
	  val kamaBase=9
	  
	  for (i<-0 until (inputWindow)){
		  minVal = Math.min(minVal, values(kamaBase+i));
		  maxVal = Math.max(maxVal, values(kamaBase+i));
	  }
	} 
	override def finalizeCalibration():List[CalibratedIndicator]={
	  var out=new ListBuffer[CalibratedIndicator]();
	  if (version=="1")
	    out.append(CalibratedIndicator(name, version.toInt,"kama.value",maxVal, minVal,1,0))
	  else
		  out.append(CalibratedIndicator(name, version.toInt,"kama.value", 2, -2,1,-1))
	  out.toList
	}
	
	override def processInput(values:List[Double] ):List[Double]={
	  var out=new ListBuffer[Double]();
	  val kamaBase=9
	  val lowBase=3
	  val highBase=6
	  for (i<-0 until (inputWindow)){
	   
	    if (version=="1")
		  out.append(field.normalize(values(kamaBase+i)))
	    if (version=="2"){
	      if (values(lowBase+i)>values(kamaBase+i))
	    	  out.append(field.normalize(2))
	      else if (values(highBase+i)<values(kamaBase+i))
	    	  out.append(field.normalize(-2))
	      else if (values(i)<values(kamaBase+i))
	    	  out.append(field.normalize(-1))
	      else 
	    	  out.append(field.normalize(1))
	    }
	  }
	  out.toList
	}	
	override def getInputCount():Int={
	 (4* inputWindow)
	}
	override def getOutputCount():Int={
	 ( inputWindow)
	}	

}