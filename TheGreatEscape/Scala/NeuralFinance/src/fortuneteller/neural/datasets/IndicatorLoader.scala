package fortuneteller.neural.datasets
import fortuneteller.indicators._
import fortuneteller.neural.calibration.CalibratedIndicator
import scala.xml._
import java.io.File
object IndicatorLoader {
	val cat = org.apache.log4j.Category.getInstance("fortuneteller");
	def loadFullDataset(datasetDef:String, calibration:List[CalibratedIndicator]):List[NeuralIndicator]={
	  val indieList=new scala.collection.mutable.ListBuffer[NeuralIndicator]();
	  val dirFile=datasetDef.split(",")
	  val dataset=new java.util.Scanner(new File(new File(new File("datasets\\" + dirFile(0)), dirFile(1) + ".xml").getAbsolutePath())).useDelimiter("\\Z").next()
	  val xmlData=XML.loadString(dataset)
	  val window=(xmlData \ "@lookbackWindow").text.toInt
	  val officialName=(xmlData \ "@name").text
	  for ( entry<- xmlData \\ "indicator"){
	     val name=(entry \ "@name").text
         match {
          case "QQE" => indieList.append(new QQE(window,entry.toString,calibration))
          case "ParabolicSAR" => indieList.append(new ParabolicSAR(window,entry.toString,calibration))
          case "ADX" => indieList.append(new ADX(window,entry.toString,calibration))
          case "RSquared" => indieList.append(new RSquared(window,entry.toString,calibration))
          case "Stochastics" => indieList.append(new Stochastic(window,entry.toString,calibration))
          case "BollingerBandwidth" => indieList.append(new BollingerBandwidth(window,entry.toString,calibration))
          case "Laguerre" => indieList.append(new Laguerre(window,entry.toString,calibration))
          case "CandlePattern" => indieList.append(new CandlePattern(window,entry.toString,calibration))
          case "Donchian" => indieList.append(new DonchianChannel(window,entry.toString,calibration))
          case "MACD" => indieList.append(new MACD(window,entry.toString,calibration))
          case "FisherTransform" => indieList.append(new FisherTransform(window,entry.toString,calibration))
          case "KAMA" => indieList.append(new KAMA(window,entry.toString,calibration))
          case "RSI" => indieList.append(new RSI(window,entry.toString,calibration))
          case "ROC" => indieList.append(new ROC(window,entry.toString,calibration))
          case "T3" => indieList.append(new T3(window,entry.toString,calibration))
          case "BWAwesome" => indieList.append(new BWAwesome(window,entry.toString,calibration))
          case "BWAlligator" => indieList.append(new BWAlligator(window,entry.toString,calibration))
          case "BWFractals" => indieList.append(new BWFractals(window,entry.toString,calibration))  
        //  case "MurreyMath" => indieList.append(new MurreyMath(window,entry.toString,calibration))  
          case _ => null
        }
	  }	 
	  indieList.toList
	}
}