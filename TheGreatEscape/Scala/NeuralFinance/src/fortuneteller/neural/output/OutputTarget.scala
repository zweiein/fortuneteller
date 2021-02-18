package fortuneteller.neural.output
import scala.collection.mutable.ListBuffer
import fortuneteller.neural.calibration.CalibratedIndicator
import org.encog.ml.data.basic.BasicMLData;

case class RequestedValue(propagate:Boolean, valueSpec:String)

trait OutputTarget {
	val requestedValues=new ListBuffer[RequestedValue] 
	var name=""
	var description=""
	var version=""  
	def getInputCount():Int={
	 0 
	}
	def getOutputCount():Int={
	 0 
	}	
	def initCalibration():Unit={
	}	
   def calibration(window: fortuneteller.utils.SlidingWindow): Unit = {}
	def finalizeCalibration():List[CalibratedIndicator]={
	   List[CalibratedIndicator]()
	}
	
	def getName():String={
	  name
	}
	def getVersion():String={
	 version 
	}
	def getDescription():String={
	  description
	}
	def getNormalizer():org.encog.util.arrayutil.NormalizedField={
	 null
	}
	def processInput(window: fortuneteller.utils.SlidingWindow): Double= {0}
	def lookupCalibration(id:String,clist:List[CalibratedIndicator]):CalibratedIndicator={
	  
	  for (c<-clist){
	    if (c.name==name && c.version.toString()==version && c.normId==id)
	      return c
	  }
	  null
	}	
}