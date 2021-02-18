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

  /**
   * Perform the training option.
   */
  def train(): Unit = {
    // first, create the machine learning method
    val methodFactory = new MLMethodFactory();
    var method = methodFactory.create(Config.METHOD_TYPE, Config.METHOD_ARCHITECTURE, Config.INPUT_WINDOW, 1);

    // second, create the data set	
    val filename = new File(filePath, Config.FILENAME_TRAIN);
    val dataSet = EncogUtility.loadEGB2Memory(filename);

    // third, create the trainer
    val trainFactory = new MLTrainFactory();
    val train = trainFactory.create(method, dataSet, Config.TRAIN_TYPE, Config.TRAIN_PARAMS);
    // reset if improve is less than 1% over 5 cycles
    if (method.isInstanceOf[MLResettable] && !(train.isInstanceOf[ManhattanPropagation])) {
      train.addStrategy(new RequiredImprovementStrategy(500));
    }

    // fourth, train and evaluate.
    EncogUtility.trainToError(train, Config.TARGET_ERROR);
    
   // EncogUtility.tr
    method = train.getMethod();
    EncogDirectoryPersistence.saveObject(new File(filePath, Config.METHOD_NAME), method);

    // finally, write out what we did
    cat.info("Machine Learning Type: " + Config.METHOD_TYPE);
    cat.info("Machine Learning Architecture: " + Config.METHOD_ARCHITECTURE);

    cat.info("Training Method: " + Config.TRAIN_TYPE);
    cat.info("Training Args: " + Config.TRAIN_PARAMS);
  }

  /**
   * Perform the generate option.
   */
  def generate(): Unit = {
    cat.info("Generating training data... please wait...");
    val gen = new Generator(filePath);
    gen.generate();
    cat.info("Training data has been generated.");
  }
  def main(args: Array[String]): Unit = {
    PropertyConfigurator.configure("./log.properties");
    filePath = new File(args(1))
    if (args(0) == "collect")
      run(true)
    if (args(0) == "generate")
      generate()
    if (args(0) == "train")
      train()
    if (args(0) == "run")
      run(false)
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