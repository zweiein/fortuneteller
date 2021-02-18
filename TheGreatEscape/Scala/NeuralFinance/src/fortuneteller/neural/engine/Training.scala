package fortuneteller.neural.engine
import fortuneteller.indicators._
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

import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.io.PrintWriter;

import org.encog.Encog;
import org.encog.EncogError;
import org.encog.cloud.indicator.basic.BasicIndicator;
import org.encog.cloud.indicator.basic.InstrumentHolder;
import org.encog.cloud.indicator.server.IndicatorLink;
import org.encog.cloud.indicator.server.IndicatorPacket;
import org.encog.ml.MLRegression;
import org.encog.ml.data.MLData;
import org.encog.ml.data.basic.BasicMLData;
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import org.encog.util.csv.CSVFormat;
import scala.collection.JavaConversions._
import org.encog.ml.data.buffer.BufferedMLDataSet;
import org.encog.util.csv.CSVFormat;
import org.encog.util.csv.ReadCSV;
import org.encog.neural.networks.BasicNetwork
import org.encog.engine.network.activation._
import org.encog.neural.networks.layers.BasicLayer
import org.joda.time.DateTime
import org.joda.time.Duration
class Training(config: NeuralNetConfig, inputNodeCount: Int, hiddenLayers: Int, hiddenLayerNodes: Int, minutes: Int, trainingFile: File, networkFile: File) {
  val cat = org.apache.log4j.Category.getInstance("fortuneteller");
  def selectActivationFunction(): ActivationFunction = {
    config.activationFunction match {
      case "TANH" => new ActivationTANH()
      case "SIGMOID" => new ActivationSigmoid()
      case "GAUSSIAN" => new ActivationGaussian()
      case "LOG" => new ActivationLOG()
      case "ELLIOTT" => new ActivationElliottSymmetric()      
      case _ => new ActivationTANH()
    }    
  }
  
  def createJordanNetwork(): BasicNetwork = {
    val pattern = new org.encog.neural.pattern.JordanPattern()
    pattern.setActivationFunction(selectActivationFunction());
    pattern.setInputNeurons(inputNodeCount);
    pattern.addHiddenLayer(hiddenLayerNodes);
    pattern.setOutputNeurons(1);
    val net=pattern.generate       
    var netObj:BasicNetwork = net.asInstanceOf[BasicNetwork]
    netObj
   /* 
    pattern match {
      case p: BasicNetwork => p
      case _ => null  // Or throw exception further
    }*/
  }  
  
  def createElmanNetwork(): BasicNetwork = {
    val pattern = new org.encog.neural.pattern.ElmanPattern()
    pattern.setActivationFunction(selectActivationFunction());
    pattern.setInputNeurons(inputNodeCount);
    pattern.addHiddenLayer(hiddenLayerNodes);
    pattern.setOutputNeurons(1);
    val net=pattern.generate    
    var netObj:BasicNetwork = net.asInstanceOf[BasicNetwork]
    netObj
    /*
    pattern match {
      case p: BasicNetwork => p
      case _ => null
    }*/
  }

  def createBasicNetwork(): BasicNetwork = {
    val network = new BasicNetwork();
    network.addLayer(new BasicLayer(null, true, inputNodeCount));
    for (i <- 0 until hiddenLayers) {
      network.addLayer(new BasicLayer(selectActivationFunction(), true, hiddenLayerNodes));
    }
    network.addLayer(new BasicLayer(selectActivationFunction(), false, 1));
    network.getStructure().finalizeStructure();
    network.reset();
    network;
  }

  def train(): Unit = {
    val methodFactory = new MLMethodFactory();
    var network = if (config.network == "ML") {
      methodFactory.create(MLMethodFactory.TYPE_FEEDFORWARD,
        "?:B->TANH->20:B->TANH->?", inputNodeCount, 1);
    } else if (config.network == "ELMAN") createElmanNetwork else if (config.network == "JORDAN") createJordanNetwork else createBasicNetwork

    val dataSet = EncogUtility.loadEGB2Memory(trainingFile);

    // third, create the trainer
    val trainFactory = new MLTrainFactory();
    val train = if (config.trainingMethod == "MANHATTAN") trainFactory.create(network, dataSet, MLTrainFactory.TYPE_MANHATTAN, "lr=0.0001");
    else if (config.trainingMethod == "LMA") trainFactory.create(network, dataSet, MLTrainFactory.TYPE_LMA, "");
    else trainFactory.create(network, dataSet, MLTrainFactory.TYPE_RPROP, "");

    // reset if improve is less than 1% over 5 cycles
    if (network.isInstanceOf[MLResettable] && !(train.isInstanceOf[ManhattanPropagation])) {
      train.addStrategy(new RequiredImprovementStrategy(500));
    }
    var dt = new DateTime()
    var stopTime = dt.plusMinutes(minutes)
    var epoch = 0
    var stop = false
    while (stop == false) {
      train.iteration
      epoch = epoch + 1
      val currTime = new DateTime()

      val dur = new Duration(currTime, stopTime);
      if (train.getError < 0.00001) stop = true
      if (stopTime.isBefore(currTime)) stop = true
      else {
        val currEpochMod = epoch % 100
        if (currEpochMod == 0) cat.info(config.configId + " epoch " + epoch + " err:" + (train.getError() * 100.0).formatted("%.3f") + ". Rem mins:" + dur.getStandardMinutes())
      }
    }
    cat.info("Final Error: " + train.getError);
    cat.info("Training complete, saving network.");
    EncogDirectoryPersistence.saveObject(networkFile, network);
    cat.info("Network saved.");
  }

  /**
   * Perform the training option.
   */
  def train2(): Unit = {
    if (config.network == "ML") {
      // first, create the machine learning method
      val methodFactory = new MLMethodFactory();
      var method = methodFactory.create(MLMethodFactory.TYPE_FEEDFORWARD,
        "?:B->TANH->20:B->TANH->?", inputNodeCount, 1);

      val dataSet = EncogUtility.loadEGB2Memory(trainingFile);

      // third, create the trainer
      val trainFactory = new MLTrainFactory();
      val train = trainFactory.create(method, dataSet, MLTrainFactory.TYPE_RPROP, "");
      // reset if improve is less than 1% over 5 cycles
      if (method.isInstanceOf[MLResettable] && !(train.isInstanceOf[ManhattanPropagation])) {
        train.addStrategy(new RequiredImprovementStrategy(500));
      }

      var dt = new DateTime()

      var stopTime = dt.plusMinutes(minutes)
      var epoch = 1
      var stop = false
      while (stop == false) {
        train.iteration
        epoch = epoch + 1
        if (train.getError < 0.0001) stop = true
        if (stopTime.isBefore(DateTime.now)) stop = true
        val curr = epoch % 100
        if (curr == 0) cat.info("Epoch " + epoch + " error is " + train.getError())
      }

      cat.info("Final Error: " + train.getError);
      cat.info("Training complete, saving network.");
      EncogDirectoryPersistence.saveObject(networkFile, method);
      cat.info("Network saved.");
      return
    }

    val netw = createBasicNetwork
    org.encog.persist.EncogDirectoryPersistence.saveObject(networkFile, netw);
    // second, create the data set	
    val dataSet = EncogUtility.loadEGB2Memory(trainingFile);

    val network = EncogDirectoryPersistence.loadObject(networkFile)
    network match {
      case bn: org.encog.neural.networks.BasicNetwork => {
        EncogUtility.trainConsole(bn, dataSet, minutes)
        // finally, write out what we did
        cat.info("Final Error: " + bn.calculateError(dataSet));
        cat.info("Training complete, saving network.");
        EncogDirectoryPersistence.saveObject(networkFile, network);
        cat.info("Network saved.");
      }
      case _ =>
    }
  }
}