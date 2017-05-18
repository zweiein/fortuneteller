package fortuneteller.neural
import org.encog.ml.factory.MLMethodFactory
import org.encog.ml.factory.MLTrainFactory;
import fortuneteller.indicators.CandlePattern

import fortuneteller.indicators._



class ConfigData(propFile:java.io.File, instrument:String) {
    val p=fortuneteller.utils.PropertyFile.readPropertyFile(propFile.getAbsolutePath)


	/**
	 * The maximum range (either positive or negative) that the pip profit(or loss) will be in.
	 */
    
	val PIP_RANGE = fortuneteller.utils.PropertyFile.getProperty(p,"tick_range_result").toDouble//100;	
	val TRAINING_MINUTES = fortuneteller.utils.PropertyFile.getProperty(p,"training_minutes").toInt	
	val NETWORK_TYPE = fortuneteller.utils.PropertyFile.getProperty(p,"network_type")
// Indie values


	val MACD_MAX = fortuneteller.utils.PropertyFile.getProperty(p,"indie_macd_max").toDouble
	val MACD_MIN = fortuneteller.utils.PropertyFile.getProperty(p,"indie_macd_min").toDouble
	val FISHER_MIN = fortuneteller.utils.PropertyFile.getProperty(p,"indie_fishertransform_min").toDouble
	val FISHER_MAX = fortuneteller.utils.PropertyFile.getProperty(p,"indie_fishertransform_max").toDouble	


	val DONCHIAN_WIDTH_MAX = fortuneteller.utils.PropertyFile.getProperty(p,"indie_donchianwidth_max").toDouble
	val DONCHIAN_WIDTH_MIN = fortuneteller.utils.PropertyFile.getProperty(p,"indie_donchianwidth_min").toDouble
	
	val CANDLE_PATTERN_MAX = fortuneteller.utils.PropertyFile.getProperty(p,"indie_candlepattern_max").toDouble
	val CANDLE_PATTERM_MIN = fortuneteller.utils.PropertyFile.getProperty(p,"indie_candlepattern_min").toDouble
	
	val RSQUARED_MAX = fortuneteller.utils.PropertyFile.getProperty(p,"indie_rsquared_max").toDouble
	val RSQUARED_MIN = fortuneteller.utils.PropertyFile.getProperty(p,"indie_rsquared_min").toDouble	
	
	val LAGUERRE_MAX = fortuneteller.utils.PropertyFile.getProperty(p,"indie_laguerre_max").toDouble
	val LAGUERRE_MIN = fortuneteller.utils.PropertyFile.getProperty(p,"indie_laguerre_min").toDouble		
	
	val BOLLINGER_MAX = fortuneteller.utils.PropertyFile.getProperty(p,"indie_bollinger_width_max").toDouble
	val BOLLINGER_MIN = fortuneteller.utils.PropertyFile.getProperty(p,"indie_bollinger_width_min").toDouble	
	val PARSAR_DIFF_MIN = fortuneteller.utils.PropertyFile.getProperty(p,"indie_parsar_diff_min").toDouble
	val PARSAR_DIFF_MAX = fortuneteller.utils.PropertyFile.getProperty(p,"indie_parsar_diff_max").toDouble	
	
	val ADX_MAX = fortuneteller.utils.PropertyFile.getProperty(p,"indie_adx_max").toDouble
	val ADX_MIN = fortuneteller.utils.PropertyFile.getProperty(p,"indie_adx_min").toDouble	
	val QQE_MAX = fortuneteller.utils.PropertyFile.getProperty(p,"indie_qqe_max").toDouble
	val QQE_MIN = fortuneteller.utils.PropertyFile.getProperty(p,"indie_qqe_min").toDouble
	val QQE_DIFF_MAX = fortuneteller.utils.PropertyFile.getProperty(p,"indie_qqe_diff_max").toDouble
	val QQE_DIFF_MIN = fortuneteller.utils.PropertyFile.getProperty(p,"indie_qqe_diff_min").toDouble
	
	/**
	 * The size of a single PIP (i.e. 0.0001 for EURUSD)
	 */
	val PIP_SIZE = fortuneteller.utils.PropertyFile.getProperty(p,"tick_size").toDouble//0.01;

	/**
	 * The size of the input window.  This is the number of previous bars to consider.
	 */
	val INPUT_WINDOW = fortuneteller.utils.PropertyFile.getProperty(p,"input_window").toInt//3;
	
	/**
	 * The number of bars to look forward to determine a max profit, or loss.
	 */
	val PREDICT_WINDOW = fortuneteller.utils.PropertyFile.getProperty(p,"predict_window").toInt//5;
	
	/**
	 * The targeted error.  Once the training error reaches this value, training will stop.
	 */
	val TARGET_ERROR = fortuneteller.utils.PropertyFile.getProperty(p,"target_error").toDouble//0.076f;
	
	/**
	 * The type of method.  This is an Encog factory code.
	 */
	val METHOD_TYPE = MLMethodFactory.TYPE_FEEDFORWARD;
	
	/**
	 * The architecture of the method.  This is an Encog factory code.
	 */
	val METHOD_ARCHITECTURE = "?:B->TANH->20:B->TANH->?";
	
	/**
	 * The type of training.  This is an Encog factory code.
	 */
	val TRAIN_TYPE = MLTrainFactory.TYPE_RPROP;
	
	/**
	 * The training parameters.  This is an Encog factory code.
	 */
	val TRAIN_PARAMS = "";
	
	/**
	 * The filename for the training data.
	 */
	val FILENAME_COLLECT = "collect_" + instrument + ".csv";	
	
	/**
	 * The filename for the training data.
	 */
	val FILENAME_TRAIN = "training_" + instrument + ".egb";
	val NETWORK_FILE="neuralnetwork2_" + instrument + ".eg"
	/**
	 * The filename to store the method to.
	 */
	val METHOD_NAME = "neuralnet_" + instrument + ".eg";
	
}

object Config{
  var propFile:java.io.File=null
  var instrument:String=""
  def setConfig(thePropFile:java.io.File):Unit={
    propFile=thePropFile
  }
   def setInstrument(theInstrument:String):Unit={
    instrument=theInstrument
  }
  def getConfig():ConfigData={
   new ConfigData(propFile, instrument) 
  }
    
  def loadIndicators():List[NeuralIndicator]={
     val indieList=new scala.collection.mutable.ListBuffer[NeuralIndicator]();
    val p=fortuneteller.utils.PropertyFile.readPropertyFile(propFile.getAbsolutePath)
    val indies=fortuneteller.utils.PropertyFile.getProperty(p,"indicators")
    for (i<-indies.toString.split(",")){
      def parseIndicator():NeuralIndicator={
        i match {
          case "QQE_SIMPLE" => new QQE(3,"", List())
          case "QQE" => new QQE(3,"", List())
          case "ParabolicSAR" => new ParabolicSAR(3,"", List())
          case "ADX" => new ADX(3,"", List())
          case "RSquared" => new RSquared(3,"", List())
          case "BollingerBandwidth" => new BollingerBandwidth(3,"", List())
          case "Laguerre" => new Laguerre(3,"", List())
          case "CandlePattern" => new CandlePattern(3,"", List())
          case "Donchian" => new DonchianChannel(3,"", List())
          case "MACD" => new MACD(3,"", List())
          case "FisherTransform" => new FisherTransform(3,"", List())
          case _ => null
        }
      }
      val indi=parseIndicator
      if (indi!=null){
        indieList.append(indi)
      }
    }
    indieList.toList//return List(new QQE(),new ParabolicSAR(), new ADX())
  }
  def loadOutputIndicator():NeuralIndicator={
    return new fortuneteller.indicators.ClosingPrice()
  }
  
}