package fortuneteller.neural.engine
import scala.xml._
class NeuralNetConfig(config:String, configName:String) {
  val xmlData=XML.loadString(config)
  val configId =(xmlData \\ "neural" \\ "@id").text  
  val dataset =(xmlData \\ "neural" \\ "dataset").text  
  val instrument =(xmlData \\ "neural" \\ "instrument").text
  val tickSize =(xmlData \\ "neural" \\ "tickSize").text.toDouble 
  val resolution =(xmlData \\ "neural" \\ "resolution").text 
  val allIndicators= fortuneteller.neural.datasets.IndicatorLoader.loadFullDataset(dataset, List())
  val predictionBars =(xmlData \\ "neural" \\ "predictionBars").text.toInt 
  val network =(xmlData \\ "neural" \\ "network").text
  val activationFunction =(xmlData \\ "neural" \\ "activationFunction").text
  val trainingMethod =(xmlData \\ "neural" \\ "trainingMethod").text
  val trainingMinutes =(xmlData \\ "neural" \\ "trainingMinutes").text.toInt
  val hiddenLayers =(xmlData \\ "neural" \\ "hiddenLayers").text.toInt
  val hiddenLayerNodes =(xmlData \\ "neural" \\ "hiddenLayerNodes").text.toInt
  val output =(xmlData \\ "neural" \\ "outputAlgorithm").text
  
  def getConfig():String=config
  def getConfigName():String=configName
 
}