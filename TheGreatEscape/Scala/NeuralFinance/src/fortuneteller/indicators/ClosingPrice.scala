package fortuneteller.indicators

class ClosingPrice extends NeuralIndicator{
  //	val field = new NormalizedField(NormalizationAction.Normalize,"diff",1000,0,1,0);

	requestedValues.append(new RequestedValue(true,"CLOSE[0]"))
	requestedValues.append(new RequestedValue(true,"OPEN[0]"))
	requestedValues.append(new RequestedValue(true,"HMA(10)[0]"))

	
	override def processInput(values:List[Double] ):List[Double]={
	  values
	}	
	override def getInputCount():Int={
	 3
	}
	override def getOutputCount():Int={
	 3 
	}	

}