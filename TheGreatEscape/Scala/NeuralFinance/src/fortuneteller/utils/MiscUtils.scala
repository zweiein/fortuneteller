package fortuneteller.utils
import java.io.BufferedInputStream;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;
import java.util.Properties;
object MiscUtils {

  def findMax(origMax: Double, origMin: Double): Double = {
    if (Math.abs(origMax) > Math.abs(origMin)) {
      return Math.abs(origMax)
    }
    Math.abs(origMin)
  }
}

