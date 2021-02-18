package fortuneteller.utils
import fortuneteller.indicators.Config
import scala.collection.mutable.ListBuffer

class Element(){
  var a=Array.fill[Double](Config.INPUT_WINDOW+1)(0.0)
}

class SlidingWindow(size:Int) {
	var window=new ListBuffer[Element]();
	
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
}