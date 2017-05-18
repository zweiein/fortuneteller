package fortuneteller.neural.engine.test
import scala.xml._
import org.apache.log4j.PropertyConfigurator;
object TestCreator {
    def getConfig():Node={
    <neural id="6a.60min.1">
    		<dataset>default,public</dataset>
    		<instrument>6a</instrument>
    		<tickSize>0.0001</tickSize>
    		<resolution>60min</resolution>
    		<indicators>
    				<indicator name="QQE" version="1"/>	
    				<indicator name="RSquared" version="1"/>	
    		</indicators>
    		<predictionBars>6</predictionBars>
    		<network>TANH</network>
    		<hiddenLayers>1</hiddenLayers>
    		<hiddenLayerNodes>20</hiddenLayerNodes>
    		<trainingMinutes>40</trainingMinutes>
    </neural>
    }
  def main(args: Array[String]): Unit = {
    PropertyConfigurator.configure("./log.properties");
    val cat = org.apache.log4j.Category.getInstance("fortuneteller");
    val xml=getConfig
    
    val nc=new fortuneteller.neural.engine.NeuralNetCreator(new fortuneteller.neural.engine.NeuralNetConfig(xml.toString(),""))
    nc.createNeuralNet()
  }

}