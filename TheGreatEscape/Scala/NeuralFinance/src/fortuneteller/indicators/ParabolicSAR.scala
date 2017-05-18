package fortuneteller.indicators
import fortuneteller.neural.Config
import scala.collection.mutable.ListBuffer
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import fortuneteller.neural.calibration.CalibratedIndicator
class ParabolicSAR(inputWindow:Int,config:String,calibration:List[CalibratedIndicator]) extends NeuralIndicator with IndicatorConfig{
	name=getConfigField(config,"name")
	description=getConfigField(config,"description")
	version=getConfigField(config,"version")
	val cat = org.apache.log4j.Category.getInstance("fortuneteller.qqe");
	val field = getNormalization(lookupCalibration("parsar.value",calibration));//new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.PARSAR_DIFF_MAX,Config.getConfig.PARSAR_DIFF_MIN,1,-1);
//	val difffield = new NormalizedField(NormalizationAction.Normalize,"diff",50,-50,1,-1);
	requestedValues.append(new RequestedValue(true,"CLOSE[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"ParabolicSAR(0.02, 0.05, 0.02)[" + inputWindow + "]"))
	var minVal=1000.0
	var maxVal=(-1000.0)	

	override def initCalibration():Unit={
	}	
	
	def calcParSar(close:Double, par:Double):Double={
	  if (par<close) -1 else 1
	  }
	override def calibration(values:List[Double]):Unit={

	  for (i<-0 until (inputWindow)){
		  val upper=inputWindow
		  val diff=calcParSar(values(i),values(upper+i))
		  minVal = Math.min(minVal, diff);
		  maxVal = Math.max(maxVal, diff);
	  }
	} 
	override def finalizeCalibration():List[CalibratedIndicator]={
	  var out=new ListBuffer[CalibratedIndicator]();
	  maxVal=findMax(maxVal,minVal)
	  minVal=(-findMax(maxVal,minVal))	
	  out.append(CalibratedIndicator(name, version.toInt,"parsar.value", maxVal, minVal,1,-1))
	  out.toList
	}
	override def processInput(values:List[Double] ):List[Double]={
	  val upper=inputWindow
	  var out=new ListBuffer[Double]();
	  for (i<-0 until (inputWindow)){
	      val diff=calcParSar(values(i),values(upper+i))
	      out.append(field.normalize(diff))

	  }
	  out.toList
	}	
	override def getInputCount():Int={
	 2*( inputWindow)
	}
	override def getOutputCount():Int={
	  ( inputWindow)
	}	

}