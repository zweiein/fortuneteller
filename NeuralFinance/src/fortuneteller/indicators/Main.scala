package fortuneteller.indicators
import java.io.File;

import org.encog.cloud.indicator.IndicatorConnectionListener;
import org.encog.cloud.indicator.IndicatorFactory;
import org.encog.cloud.indicator.IndicatorListener;
import org.encog.cloud.indicator.server.IndicatorLink;
import org.encog.cloud.indicator.server.IndicatorServer;
import org.encog.ml.MLMethod;
import org.encog.ml.MLRegression;
import org.encog.ml.MLResettable;
import org.encog.ml.data.MLDataSet;
import org.encog.ml.factory.MLMethodFactory;
import org.encog.ml.factory.MLTrainFactory;
import org.encog.ml.train.MLTrain;
import org.encog.ml.train.strategy.RequiredImprovementStrategy;
import org.encog.neural.networks.training.propagation.manhattan.ManhattanPropagation;
import org.encog.persist.EncogDirectoryPersistence;
import org.encog.util.simple.EncogUtility;
import org.apache.log4j.Category;
import org.apache.log4j.PropertyConfigurator;
object Main extends IndicatorConnectionListener {
  val cat = org.apache.log4j.Category.getInstance("fortuneteller");
  val PORT = 5128;
  var filePath: java.io.File = null
  def run(collectMode: Boolean): Unit = {
    var method: MLRegression = null;

    if (collectMode) {
      method = null;
      cat.info("Ready to collect data from remote indicator.");
    } else {
      val regObj = EncogDirectoryPersistence.loadObject(new File(filePath, Config.METHOD_NAME))
      regObj match {
        case g2: MLRegression => { method = g2; cat.info("Indicator is ready") }
        case _ => cat.warn("UNknown")
      }

    }
    cat.info("Waiting for connections on port " + PORT);

    val server = new IndicatorServer();
    server.addListener(this);
    server.addIndicatorFactory(new IndicatorFactory() {

      override def getName(): String = {
        return "DemoIndicator";
      }

      override def create(): IndicatorListener = {
        return new DemoIndicator(method, filePath);
      }
    });

    server.start();
  }

  def main(args: Array[String]): Unit = {
	PropertyConfigurator.configure("./log.properties");	  
    filePath = new File(args(0))
    run(true)
  }

  override def notifyConnections(link: IndicatorLink, hasOpened: Boolean): Unit = {
    if (hasOpened) {
      cat.info("Connection from " + link.getSocket().toString()
        + " established.");
    } else if (!hasOpened) {
      cat.info("Connection from " + link.getSocket().toString()
        + " terminated.");
    }
  }
}