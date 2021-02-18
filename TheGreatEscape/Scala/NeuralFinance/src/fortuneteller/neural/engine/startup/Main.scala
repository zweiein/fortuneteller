package fortuneteller.neural.engine.startup
import org.apache.log4j.PropertyConfigurator;
import java.io.File
import fortuneteller.neural.engine.NeuralNetServer;
object Main {
   var port=5128	
   PropertyConfigurator.configure("./log.properties");
   val cat = org.apache.log4j.Category.getInstance("fortuneteller");
   def recursiveParser(rootDir:String, id:String) : Unit={
        for (file <- new File(rootDir).listFiles) {
         if (!file.isDirectory) {
            
            if (file.getName().substring(0,file.getName().length-4)==id){
            	cat.info("Start network from  " + file)
            	val xml =new java.util.Scanner(new File(file.getAbsolutePath())).useDelimiter("\\Z").next()
                val nc=new NeuralNetServer(port,new fortuneteller.neural.engine.NeuralNetConfig(xml.toString(),file.getName()))
              	nc.run()  
            }
         }else{
           // It was directory and not a file. Recursively lookup sub dir
            recursiveParser(file.getAbsolutePath(),id)
         }
        }  
    }  
  def main(args: Array[String]): Unit = {
    if (args.length>1)port=args(1).toInt
    recursiveParser("./config/neuralnets/done", args(0))
  }

}