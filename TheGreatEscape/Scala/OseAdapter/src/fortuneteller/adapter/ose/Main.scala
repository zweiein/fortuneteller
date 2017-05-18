package fortuneteller.adapter.ose
import org.apache.log4j.PropertyConfigurator;
import java.io.File

object Main {
   PropertyConfigurator.configure("./log.properties");
   val cat = org.apache.log4j.Category.getInstance("fortuneteller");
   
  def main(args: Array[String]): Unit = {
    WebLoader.load("http://www.netfonds.no/quotes/paperhistory.php?paper=STL.OSE&csv_format=csv", new ParserDailyStats())
  }

}