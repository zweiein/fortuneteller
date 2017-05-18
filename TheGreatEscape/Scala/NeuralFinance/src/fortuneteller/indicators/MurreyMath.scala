package fortuneteller.indicators
import fortuneteller.neural.Config
import scala.collection.mutable.ListBuffer
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import fortuneteller.neural.calibration.CalibratedIndicator
class MurreyMath(inputWindow:Int,config:String,calibration:List[CalibratedIndicator]) extends NeuralIndicator with IndicatorConfig{
	name=getConfigField(config,"name")
	description=getConfigField(config,"description")
	version=getConfigField(config,"version")

	val cat = org.apache.log4j.Category.getInstance("fortuneteller.murreymath");
	val field = getNormalization(lookupCalibration("murreymath.value",calibration));//new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.LAGUERRE_MAX,Config.getConfig.LAGUERRE_MIN,1,0);
	requestedValues.append(new RequestedValue(true,"CLOSE[" + inputWindow + "]"))

	requestedValues.append(new RequestedValue(true,"MurreyMathSimple().N18[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"MurreyMathSimple().N08[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"MurreyMathSimple().P18[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"MurreyMathSimple().P28[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"MurreyMathSimple().P38[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"MurreyMathSimple().P48[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"MurreyMathSimple().P58[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"MurreyMathSimple().P68[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"MurreyMathSimple().P78[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"MurreyMathSimple().P88[" + inputWindow + "]"))


	override def initCalibration():Unit={
	}	
	override def calibration(values:List[Double]):Unit={

	} 
	override def finalizeCalibration():List[CalibratedIndicator]={
	  var out=new ListBuffer[CalibratedIndicator]();

	  out.append(CalibratedIndicator(name, version.toInt,"murreymath.value", 10, 0,1,0))
	  out.toList
	}
	
	override def processInput(values:List[Double] ):List[Double]={
	  var out=new ListBuffer[Double]();

	  for (i<-0 until (inputWindow)){
	   
	     var value=0
	      for(i<-3 until 10){
	     //   cat.info("i= " + i + " " + values(0) + " " + values(i)  + " " + values(i+1))
	        if (values(0)>=values(i) && values(0)<values(i+1))
	          value=i
	      }
	    out.append(field.normalize(value))
	
	  }
	  out.toList
	}	
	override def getInputCount():Int={
	 (11* inputWindow)
	}
	override def getOutputCount():Int={
	 ( inputWindow)
	}	

}