package fortuneteller.neural.datasets
import akka.actor.ActorSystem
import scala.collection.mutable.ListBuffer
import org.apache.log4j.PropertyConfigurator;
import akka.actor.Props
import org.encog.cloud.indicator.IndicatorConnectionListener
import org.encog.cloud.indicator.IndicatorFactory
import org.encog.cloud.indicator.IndicatorListener
import org.encog.cloud.indicator.server.IndicatorLink
import org.encog.cloud.indicator.server.IndicatorServer
import org.encog.ml.MLRegression
import org.encog.ml.MLResettable
import org.encog.ml.factory.MLMethodFactory
import org.encog.ml.factory.MLTrainFactory
import org.encog.ml.train.strategy.RequiredImprovementStrategy
import org.encog.neural.networks.training.propagation.manhattan.ManhattanPropagation
import org.encog.persist.EncogDirectoryPersistence
import org.encog.util.simple.EncogUtility
import fortuneteller.neural.engine.CollectorServer
import java.io.File
object TickSizes {
  
  def getTickSize(instrType:String,instrument: String):String={
   instrType match {
      case "ose" => getOseTickSize(instrument)
      case "futures" => getFutTickSize(instrument)
      case _ => "0.01"
    }
  }
  def getFutTickSize(instrument: String): String = {
    instrument match {
      case "cl" => "0.01"
      case "cc" => "1.00"
      case "gc" => "0.1"
      case "ng" => "0.001"
      case "si" => "0.005"
      case "hg" => "0.0005"
      case "nq"|"es"|"zo" => "0.25"
      case "zs"|"zw"|"zc" => "0.25"
      case "zb" => "0.03125"
      case "6a"|"6e" => "0.0001"
      case _ => "0.01"
    }
  }
  def getOseTickSize(instrument: String): String = {
    instrument match {
      case "stl" => "0.05"
      case "mhg" => "0.01"
      case _ => "0.01"
    }
  }  
}