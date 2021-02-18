package fortuneteller.neural.engine.startup
import akka.actor.ActorSystem
import scala.xml._
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
import fortuneteller.neural.datasets.TickSizes
import fortuneteller.neural.engine.CollectorServer
import java.io.File
object MultiTrainer {
  PropertyConfigurator.configure("./trainerlog.properties");
  val cat = org.apache.log4j.Category.getInstance("fortuneteller");
  val system = ActorSystem("Trainer")
  val p = fortuneteller.utils.PropertyFile.readPropertyFile("./trainer.properties")
  val actorcount = fortuneteller.utils.PropertyFile.getProperty(p, "actor_count").toInt
  var actors = new ListBuffer[akka.actor.ActorRef]();
  for (i <- 0 until actorcount) {
    actors.append(system.actorOf(Props[fortuneteller.neural.actors.NetworkCreatorActor], name = "actor" + i))
  }

  def getConfigTemplate(): Node = {
    <neural id="cl.15min.5">
      <dataset>default,public</dataset>
      <instrument>cl</instrument>
      <tickSize>0.01</tickSize>
      <resolution>15min</resolution>
      <indicators>
        <indicator name="MACD" version="1"/>
        <indicator name="Stochastics" version="1"/>
      </indicators>
      <predictionBars>8</predictionBars>
      <network>JORDAN</network>
      <activationFunction>TANH</activationFunction>
      <trainingMethod>MANHATTAN</trainingMethod>
      <hiddenLayers>1</hiddenLayers>
      <hiddenLayerNodes>30</hiddenLayerNodes>
      <trainingMinutes>10</trainingMinutes>
      <outputAlgorithm>TicksResult</outputAlgorithm>
    </neural>
  }

  def updateConfig(xmlInput: String, confid: String, instrument: String, ticksize: String, resolution: String, predBars: Int, output: String, hiddLayers: Int, minutes: Int, indic1: String, indic2: String, indic3: String,  indic4: String, network: String): String = {
    def updateVersion(node: Node): Node = {
      def updateElements(seq: Seq[Node]): Seq[Node] =
        for (subNode <- seq) yield updateVersion(subNode)
      node match {
        case <neural>{ ch @ _* }</neural> => <neural id={ confid }>{ updateElements(ch) }</neural>
        case <predictionBars>{ ch @ _* }</predictionBars> => <predictionBars>{ predBars }</predictionBars>
        case <instrument>{ ch @ _* }</instrument> => <instrument>{ instrument }</instrument>
        case <tickSize>{ ch @ _* }</tickSize> => <tickSize>{ ticksize }</tickSize>
        case <network>{ ch @ _* }</network> => <network>{ network }</network>
        case <trainingMinutes>{ ch @ _* }</trainingMinutes> => <trainingMinutes>{ minutes }</trainingMinutes>
        case <resolution>{ ch @ _* }</resolution> => <resolution>{ resolution }</resolution>
        case <outputAlgorithm>{ ch @ _* }</outputAlgorithm> => <outputAlgorithm>{ output }</outputAlgorithm>
        case <hiddenLayers>{ ch @ _* }</hiddenLayers> => <hiddenLayers>{ hiddLayers }</hiddenLayers>
        case <indicators>{ ch @ _* }</indicators> =>
          <indicators><indicator name={ indic3 } version="1"/><indicator name={ indic1 } version="1"/><indicator name={ indic4 } version="1"/><indicator name={ indic2 } version="1"/></indicators>
        case other @ _ => other
      }
    }
    val data = XML.loadString(xmlInput)
    val xmlData = updateVersion(data)
    xmlData.toString
  }

  var count = 50
  var minutes = 10

  def main(args: Array[String]): Unit = {

    //count=args(0).toInt  // Base counter
    minutes = args(1).toInt

    val indies = Array("RSquared", "Stochastics", "QQE", "MACD", "BWAlligator", "RSI", "Donchian", "T3", "ParabolicSAR", "BollingerBandwidth", "FisherTransform")
    for (instr <- List("mhg")) {
      for (resol <- List("day")) {
        count = args(0).toInt
        for (i <- 0 until (indies.length - 3)) {
          for (j <- (i + 1) until (indies.length - 2)) {
            for (k <- (j + 1) until (indies.length -1 )) {
              for (l <- (k + 1) until (indies.length )) {
              //  for (m <- (l + 1) until (indies.length)) {

                  for (predBars <- List(2, 6)) {
                    for (outputIndi <- List("TicksResult", "TicksTotalResult")) {
                      for (hiddenLayers <- List(1, 2)) {
                        for (network <- List("BASIC", "JORDAN", "ELMAN")) {
                          for (minutes <- List(60)) {
                            val id = instr + "." + resol + "." + count
                            count = count + 1
                            val xml = updateConfig(getConfigTemplate.toString, id, instr, TickSizes.getTickSize("ose", instr), resol, predBars, outputIndi, hiddenLayers, minutes, indies(i), indies(j), indies(k),indies(l), network)
                            val index = (count % actorcount)
                            actors(index) ! new fortuneteller.neural.engine.NeuralNetConfig(xml.toString(), id + ".csv")
                          }
                        }
                      }
                 //   }
                 }
                }
              }
            }
          }
        }
      }
    }

  }

}