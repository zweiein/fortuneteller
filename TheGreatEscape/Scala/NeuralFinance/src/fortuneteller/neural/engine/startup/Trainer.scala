package fortuneteller.neural.engine.startup
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
import fortuneteller.neural.datasets.IndicatorLoader
import fortuneteller.neural.engine.CollectorServer
import java.io.File
object Trainer {
   PropertyConfigurator.configure("./trainerlog.properties");
   val cat = org.apache.log4j.Category.getInstance("fortuneteller");
   val system = ActorSystem("Trainer")
   val p=fortuneteller.utils.PropertyFile.readPropertyFile("./trainer.properties")
   val actorcount = fortuneteller.utils.PropertyFile.getProperty(p,"actor_count").toInt
   var actors=new ListBuffer[akka.actor.ActorRef]();
   for (i <- 0 until actorcount){
	   actors.append(system.actorOf(Props[fortuneteller.neural.actors.NetworkCreatorActor], name = "actor" + i))
   }
   var count=0
   def recursiveParser(rootDir:String) : Unit={
		 
        for (file <- new File(rootDir).listFiles) {
         if (!file.isDirectory) {
           val scanner=new java.util.Scanner(new File(file.getAbsolutePath()))
           val xml =scanner.useDelimiter("\\Z").next()
           val index= (count % actorcount)
           actors(index) ! new fortuneteller.neural.engine.NeuralNetConfig(xml.toString(), file.getName)
           count=count+1
           scanner.close()
           file.delete()
         }else{
           // It was directory and not a file. Recursively lookup sub dir
            recursiveParser(file.getAbsolutePath())
         }
        }  
    }  
  
  def main(args: Array[String]): Unit = {
    recursiveParser("./config/neuralnets/process")
  }

}