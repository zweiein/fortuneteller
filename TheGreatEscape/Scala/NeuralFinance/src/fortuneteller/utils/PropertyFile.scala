package fortuneteller.utils
import java.io.BufferedInputStream;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;
import java.util.Properties;
object PropertyFile {

  def getProperty(configProperties: Properties, name: String): String = {
    val value = configProperties.get(name);
    if (value == null)
      return null
    value.toString.trim();
  }
  def readPropertyFile(fileName: String): Properties = {
    var input: InputStream = null;
    try {
      input = new BufferedInputStream(new FileInputStream(fileName));
      val configProperties = new Properties();
      configProperties.load(input);
      if (input != null)
        input.close();
      return configProperties;

    } catch {
      case e: Exception =>
        println("Error reading prop file ")
        return null
    }

  }
}