package fortuneteller.adapter.ose
import scala.collection.mutable.ListBuffer

case class DailyStats(ticker:String, tradeDate:java.util.Date, open:Double, high:Double, low:Double, close:Double, volume:Double, value:Double)
case class TradeData(propagate:Boolean, valueSpec:String)
