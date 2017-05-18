package fortuneteller.neural.engine.startup
import org.apache.log4j.PropertyConfigurator;
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
object Collector extends IndicatorConnectionListener {
  def main(args: Array[String]): Unit = {
    val PORT = 5128;
    val filename=args(0)
    PropertyConfigurator.configure("./log.properties");
    val cat = org.apache.log4j.Category.getInstance("fortuneteller");
    val indicators = IndicatorLoader.loadFullDataset("default,public", List())
    for (i <- indicators) {
      cat.info(i.getName)
    }
    cat.info("Waiting for connections on port " + PORT);
    val server = new IndicatorServer();
    server.addListener(this);
    server.addIndicatorFactory(new IndicatorFactory() {

      override def getName(): String = {
        return "NeuralIndicator Collector";
      }
      override def create(): IndicatorListener = {
        return new CollectorServer(new java.io.File("data\\collected\\default\\public"), filename);
      }
    });
    
    server.start();
  }

  override def notifyConnections(link: IndicatorLink, hasOpened: Boolean): Unit = {
    val cat = org.apache.log4j.Category.getInstance("fortuneteller");
    if (hasOpened) {
      cat.info("Connection from " + link.getSocket().toString()
        + " established.");
    } else if (!hasOpened) {
      cat.info("Connection from " + link.getSocket().toString()
        + " terminated.");
    }

  }

}