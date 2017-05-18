package fortuneteller.neural
import java.io.File
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
import org.apache.log4j.PropertyConfigurator;


object Main extends IndicatorConnectionListener {
  val cat = org.apache.log4j.Category.getInstance("fortuneteller");
  val PORT = 5128;
  var filePath: java.io.File = null
  var ticker: String = "dummy"
  def run(collectMode: Boolean): Unit = {
    var method: MLRegression = null;

    if (collectMode) {
      method = null;
      cat.info("Ready to collect data from remote indicator.");
    } else {

      val regObj = EncogDirectoryPersistence.loadObject(new File(filePath, Config.getConfig.METHOD_NAME))
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
        return "NeuralIndicator";
      }

      override def create(): IndicatorListener = {
        return new IndicatorManager(method, filePath);
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
    var method = methodFactory.create(Config.getConfig.METHOD_TYPE, Config.getConfig.METHOD_ARCHITECTURE, Config.getConfig.INPUT_WINDOW, 1);

    // second, create the data set	
    val filename = new File(filePath, Config.getConfig.FILENAME_TRAIN);
    val dataSet = EncogUtility.loadEGB2Memory(filename);

    // third, create the trainer
    val trainFactory = new MLTrainFactory();
    val train = trainFactory.create(method, dataSet, Config.getConfig.TRAIN_TYPE, Config.getConfig.TRAIN_PARAMS);
    // reset if improve is less than 1% over 5 cycles
    if (method.isInstanceOf[MLResettable] && !(train.isInstanceOf[ManhattanPropagation])) {
      train.addStrategy(new RequiredImprovementStrategy(500));
    }
    val networkFile=new File(filePath,"supernet.egb"); 
    val network =EncogDirectoryPersistence.loadObject(networkFile) 
    // fourth, train and evaluate.
    EncogUtility.trainToError(train, Config.getConfig.TARGET_ERROR);
    
    network match  {
      case bn:org.encog.neural.networks.BasicNetwork =>{
        
    	  EncogUtility.trainConsole(bn, dataSet, 3)
      }
      case _ =>
    }
    

    // EncogUtility.tr
    method = train.getMethod();
    EncogDirectoryPersistence.saveObject(new File(filePath, Config.getConfig.METHOD_NAME), method);

    // finally, write out what we did
    cat.info("Machine Learning Type: " + Config.getConfig.METHOD_TYPE);
    cat.info("Machine Learning Architecture: " + Config.getConfig.METHOD_ARCHITECTURE);

    cat.info("Training Method: " + Config.getConfig.TRAIN_TYPE);
    cat.info("Training Args: " + Config.getConfig.TRAIN_PARAMS);
  }

  /**
   * Perform the training option.
   */
  def train2(): Unit = {
    val netType=Config.getConfig.NETWORK_TYPE
    val gen =new IndicatorManager(null, filePath);
    val netw=if (netType=="ELLIOT") gen.createElliot else gen.createTANH
    org.encog.persist.EncogDirectoryPersistence.saveObject(new File(filePath,Config.getConfig.METHOD_NAME), netw);
 
    // second, create the data set	
    val filename = new File(filePath, Config.getConfig.FILENAME_TRAIN);
    val dataSet = EncogUtility.loadEGB2Memory(filename);
    val networkFile=new File(filePath,Config.getConfig.METHOD_NAME); 
    val network =EncogDirectoryPersistence.loadObject(networkFile) 
    network match  {
      case bn:org.encog.neural.networks.BasicNetwork =>{
        
    	  	EncogUtility.trainConsole(bn, dataSet, Config.getConfig.TRAINING_MINUTES)
		    // finally, write out what we did
    	  	cat.info("Final Error: " + bn.calculateError(dataSet));
    	  	cat.info("Training complete, saving network.");
    	  	EncogDirectoryPersistence.saveObject(networkFile, network);
    	  	cat.info("Network saved.");
      }
      case _ =>
    }
  }  
  
  /**
   * Perform the generate option.
   */
  def generate(): Unit = {
    cat.info("Generating training data... please wait...");
    val gen =new IndicatorManager(null, filePath);// new Generator(filePath);
    gen.generate();
    cat.info("Training data has been generated.");
  }
  /**
   * Perform the generate option.
   */
  def calibrate(): Unit = {
    cat.info("Calibrating training data... please wait...");
    val gen =new IndicatorManager(null, filePath);// new Generator(filePath);
    gen.calibrate();
    cat.info("Training data has been calibrated.");
  } 
  def main(args: Array[String]): Unit = {
    PropertyConfigurator.configure("./log.properties");
    val cticker = args(1)
    ticker = args(2)
    Config.setConfig(new File(new File("config"), cticker + ".properties"))
    Config.setInstrument(ticker)
    filePath = new File("data")
    
 //   Config.loadIndicators
    if (args(0) == "collect")
      run(true)
     if (args(0) == "calibrate")
      calibrate()
    if (args(0) == "generate")
      generate()
    if (args(0) == "train")
      train()
    if (args(0) == "train2")
      train2()
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