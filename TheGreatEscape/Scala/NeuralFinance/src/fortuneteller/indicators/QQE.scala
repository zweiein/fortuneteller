package fortuneteller.indicators
import fortuneteller.neural.Config
import scala.collection.mutable.ListBuffer
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import fortuneteller.neural.calibration.CalibratedIndicator
class QQE(inputWindow:Int,config:String,calibration:List[CalibratedIndicator]) extends NeuralIndicator with IndicatorConfig{
	name=getConfigField(config,"name")
	description=getConfigField(config,"description")
	version=getConfigField(config,"version")
	val simple=(version=="1")
	val cat = org.apache.log4j.Category.getInstance("fortuneteller.qqe");
	val field = getNormalization(lookupCalibration("qqe.value",calibration));//new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.QQE_MAX,Config.getConfig.QQE_MIN,1,0);
	val difffield = getNormalization(lookupCalibration("qqe.diff",calibration));//new NormalizedField(NormalizationAction.Normalize,"diff",Config.getConfig.QQE_DIFF_MAX,Config.getConfig.QQE_DIFF_MIN,1,-1);
	var minDiff=1000.0
	var maxDiff=(-1000.0)
	var minQQE=1000.0
	var maxQQE=(-1000.0)
	
	requestedValues.append(new RequestedValue(true,"QQE(14, 5).Value1[" + inputWindow + "]"))
	requestedValues.append(new RequestedValue(true,"QQE(14, 5).Value2[" + inputWindow + "]"))

	override def initCalibration():Unit={
	}	
	override def calibration(values:List[Double]):Unit={
	  val slowBase=0
	  val fastBase=(inputWindow)
	  for (i<-0 until (inputWindow)){
		  var diff=values(slowBase+i)-values(fastBase+i)
		  minDiff = Math.min(minDiff, diff);
		  maxDiff = Math.max(maxDiff, diff);
		  minQQE = 0.0//Math.min(minQQE, values(slowBase+i));
		  maxQQE = 100.0//Math.max(maxQQE, values(slowBase+i));
	  }
	}
	override def finalizeCalibration():List[CalibratedIndicator]={
	  var out=new ListBuffer[CalibratedIndicator]();
	  maxDiff=findMax(maxDiff,minDiff)
	  minDiff=(-findMax(maxDiff,minDiff))	
	  out.append(CalibratedIndicator(name, version.toInt,"qqe.diff", maxDiff, minDiff,1,-1))
	  out.append(CalibratedIndicator(name, version.toInt,"qqe.value", maxQQE, minQQE,1,0))
	  out.toList
	}
	override def processInput(values:List[Double] ):List[Double]={
	  var out=new ListBuffer[Double]();
	  val slowBase=0
	  val fastBase=(inputWindow)
	  for (i<-0 until (inputWindow)){
		  var diff=values(slowBase+i)-values(fastBase+i)
		 // cat.info("QQE diff " + values(slowBase+i) + " " + values(fastBase+i) + " = " + diff)
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