package fortuneteller.utils
import fortuneteller.neural.Config
import scala.collection.mutable.ListBuffer

class Element(count:Int){
  var a=Array.fill[Double](count)(0.0)
}

class SlidingWindow(size:Int) {
	var window=new ListBuffer[Element]();
	def clear():Unit={
	  window=new ListBuffer[Element]();
	}
	def add(a:Element):Unit={
	  window.append(a);
	  if (window.size>size) window.remove(0);
	}
	def getElement(index:Int):Element={
	  window(index)
	}
	def isFull():Boolean={
	 (window.size==size)
	}

	def calcTotal(elIndex:Int, startIndex:Int, currVal:Double):Double={
		var res:Double=0
		for (i<-startIndex until window.size) {
			val el = getElement(i);
			res = res + (el.a(elIndex)-currVal)
		}
		res;
	}	
	
	def calcMax(elIndex:Int, startIndex:Int):Double={
		var res:Double=(-10000.0)
		for (i<-startIndex until window.size) {
			val el = getElement(i);
			res = Math.max(el.a(elIndex), res);
		}
		res;
	}
	def calcMin(elIndex:Int, startIndex:Int):Double={
		var res:Double=(10000.0)
		for (i<-startIndex until window.size) {
			val el = getElement(i);
			res = Math.min(el.a(elIndex), res);
		}
		res;
	}
	
	def calcDirection(elIndex:Int, startIndex:Int):Double={
		var c=window.size-1
		var dirVal=0
		while(c>0){
		  val diff = window(c).a(elIndex)- window(c-1).a(elIndex);
		  dirVal=dirVal + (if (diff<0)-1 else if (diff>0) 1 else 0)
		  c=c-1
		}		
		dirVal;
	}	
	
	def calcTurningPoint(elIndex:Int, startIndex:Int):Double={
	    var ret=0
		var c=window.size-1
		if ((window(c).a(elIndex)- window(c-1).a(elIndex)>0) && (window(1).a(elIndex)- window(0).a(elIndex)<0)){
			ret=1					  
		}else if ((window(c).a(elIndex)- window(c-1).a(elIndex)<0) && (window(1).a(elIndex)- window(0).a(elIndex)>0)){
			ret=(-1)
		}
	    ret
	}	
	
	def calcTurningPointValue(elIndex:Int, startIndex:Int):Double={
	    var ret=0.0
		var c=window.size-1
		if ((window(c).a(elIndex)- window(c-1).a(elIndex)>0) && (window(1).a(elIndex)- window(0).a(elIndex)<0)){
			ret=Math.abs((window(c).a(0)-window(0).a(0)))					  
		}else if ((window(c).a(elIndex)- window(c-1).a(elIndex)<0) && (window(1).a(elIndex)- window(0).a(elIndex)>0)){
			ret=(-Math.abs((window(c).a(0)-window(0).a(0))))
		}
	    ret
	}		
	
}