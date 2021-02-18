package fortuneteller.indicators
import fortuneteller.neural.Config
import scala.collection.mutable.ListBuffer
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import fortuneteller.neural.calibration.CalibratedIndicator
class Stochastic(inputWindow:Int,config:String,calibration:List[CalibratedIndicator]) extends NeuralIndicator with IndicatorConfig{
	name=getConfigField(config,"name")
	description=getConfigField(config,"description")
	version=getConfigField(config,"version")
	val simple=(version=="1")
	val cat = org.apache.log4j.Category.getInstance("fortuneteller.stochastic");
	val field = getNormalization(lookupCalibration("stochastic.value",calibration));//new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.QQE_MAX,Config.getConfig.QQE_MIN,1,0);
	val difffield = getNormalization(lookupCalibration("stochastic.diff",calibration));//new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.QQE_DIFF_MAX,Config.getConfig.QQE_DIFF_MIN,1,-1);
	var minStoch=1000.0
	var maxStoch=(-1000.0)
	var minStochDiff=1000.0
	var maxStochDiff=(-1000.0)
	
	requestedValues.append(new RequestedValue(true,"Stochastics(7,14,3).D[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"Stochastics(7,14,3).K[" + inputWindow + "]"))
	

	override def initCalibration():Unit={
	}	
	override def calibration(values:List[Double]):Unit={
	  val slowBase=0
	  val fastBase=(inputWindow)
	  for (i<-0 until (inputWindow)){
		  var diff=values(slowBase+i)-values(fastBase+i)
		  minStochDiff = Math.min(minStochDiff, diff);
		  maxStochDiff = Math.max(maxStochDiff, diff);
		  minStoch = 0.0//Math.min(minQQE, values(slowBase+i));
		  maxStoch = 100.0//Math.max(maxQQE, values(slowBase+i));
	  }
	}
	override def finalizeCalibration():List[CalibratedIndicator]={
	  var out=new ListBuffer[CalibratedIndicator]();
	  maxStochDiff=findMax(maxStochDiff,minStochDiff)
	  minStochDiff=(-findMax(maxStochDiff,minStochDiff))	
	  out.append(CalibratedIndicator(name, version.toInt,"stochastic.diff", maxStochDiff, minStochDiff,1,-1))
	  out.append(CalibratedIndicator(name, version.toInt,"stochastic.value", maxStoch, minStoch,1,0))
	  out.toList
	}
	override def processInput(values:List[Double] ):List[Double]={
	  var out=new ListBuffer[Double]();
	  val slowBase=0
	  val fastBase=(inputWindow)
	  for (i<-0 until (inputWindow)){
		  var diff=values(slowBase+i)-values(fastBase+i)
		  diff=difffield.normalize(diff)
		  
		  out.append(diff)
		  if (simple==false){		   
			out.append(field.normalize(values(slowBase+i)))
		  }
	  }
	  out.toList
	}	
	override def getInputCount():Int={
	 2*( inputWindow)
	}
	override def getOutputCount():Int={
	  if (simple==false)
		  2*inputWindow
		  else
		    inputWindow
		    
		    
	}	

}