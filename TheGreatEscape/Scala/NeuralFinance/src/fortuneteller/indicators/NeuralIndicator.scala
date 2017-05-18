package fortuneteller.indicators
import scala.collection.mutable.ListBuffer
import fortuneteller.neural.calibration.CalibratedIndicator

case class RequestedValue(propagate:Boolean, valueSpec:String)

trait NeuralIndicator {
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
	def calibration(values:List[Double]):Unit={
	}
	def finalizeCalibration():List[CalibratedIndicator]={
	   List[CalibratedIndicator]()
	}
	def processInput(values:List[Double] ):List[Double]={
	  	  List()
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
	
	def lookupCalibration(id:String,clist:List[CalibratedIndicator]):CalibratedIndicator={
	  
	  for (c<-clist){
	    if (c.name==name && c.version.toString()==version && c.normId==id)
	      return c
	  }
	  null
	}	
}