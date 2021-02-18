package fortuneteller.adapter.ose
import scala.collection.mutable.ListBuffer

class ParserDailyStats extends GeneralParser{
  val cat = org.apache.log4j.Category.getInstance("fortuneteller");
	override def parseLine(line:String):Unit={
	  val elems=line.split(",")
	  val dt = new org.joda.time.DateTime(elems(0).substring(0,4) + "-" + elems(0).substring(4,6) + "-" + elems(0).substring(6,8) + "T00:00:00+01:00");
	  val dstat=new  DailyStats(elems(1),dt.toDate, elems(3).toDouble, elems(4).toDouble, elems(5).toDouble, elems(6).toDouble, elems(7).toDouble, elems(8).toDouble)
	  cat.info( dstat)
	}
}