package fortuneteller.aq
import org.apache.log4j.PropertyConfigurator;
import java.io.File

object Main {
   PropertyConfigurator.configure("./log.properties");
   val cat = org.apache.log4j.Category.getInstance("fortuneteller");
   
  def main(args: Array[String]): Unit = {
    new Connect().setupConnection("")
  }

}