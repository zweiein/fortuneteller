package fortuneteller.indicators
import fortuneteller.neural.Config
import scala.collection.mutable.ListBuffer
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import fortuneteller.neural.calibration.CalibratedIndicator
class BWAlligator(inputWindow:Int,config:String,calibration:List[CalibratedIndicator]) extends NeuralIndicator with IndicatorConfig{
	name=getConfigField(config,"name")
	description=getConfigField(config,"description")
	version=getConfigField(config,"version")
	val cat = org.apache.log4j.Category.getInstance("fortuneteller.alligator");
	val field = getNormalization(lookupCalibration("alligator.value",calibration));//new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.FISHER_MAX,Config.getConfig.FISHER_MIN,1,-1);
	val field2 = getNormalization(lookupCalibration("alligator.value2",calibration));
	requestedValues.append(new RequestedValue(true,"bwAlligator().Jaw[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"bwAlligator().Teeth[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"bwAlligator().Lips[" + inputWindow + "]"))
	var minVal=1000.0
	var maxVal=(-1000.0)	
	var minVal2=1000.0
	var maxVal2=(-1000.0)	

	override def initCalibration():Unit={
	}	
	override def calibration(values:List[Double]):Unit={
      val jawBase=0
      val teethBase=3
      val lipsBase=6
      
	  for (i<-0 until (inputWindow)){
	      val diff1=values(teethBase+i)-values(lipsBase+i)
	      val diff2=values(jawBase+i)-values(teethBase+i)
		  minVal = Math.min(minVal, diff1);
		  maxVal = Math.max(maxVal, diff1);
		  minVal2 = Math.min(minVal, diff1);
		  maxVal2 = Math.max(maxVal, diff1);
	  
	  }
	} 
	override def finalizeCalibration():List[CalibratedIndicator]={
	  var out=new ListBuffer[CalibratedIndicator]();
	  maxVal=findMax(maxVal,minVal)
	  minVal=(-findMax(maxVal,minVal))	  	  
	  out.append(CalibratedIndicator(name, version.toInt,"alligator.value", maxVal, minVal,1,-1))
	  out.append(CalibratedIndicator(name, version.toInt,"alligator.value2", maxVal2, minVal2,1,-1))
	  out.toList
	}
	override def processInput(values:List[Double] ):List[Double]={
      val jawBase=0
      val teethBase=3
      val lipsBase=6	  
	  var out=new ListBuffer[Double]();
	  for (i<-0 until (inputWindow)){
	      val diff1=values(teethBase+i)-values(lipsBase+i)
	      val diff2=values(jawBase+i)-values(teethBase+i)	      
		  out.append(field.normalize(diff1))
		  out.append(field2.normalize(diff2))
	  }
	  out.toList
	}	
	override def getInputCount():Int={
	 ( 3*inputWindow)
	}
	override def getOutputCount():Int={
	 ( 2*inputWindow)
	}	

}