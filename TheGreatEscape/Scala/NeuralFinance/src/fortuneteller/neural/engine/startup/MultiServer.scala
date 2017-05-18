
package fortuneteller.neural.engine.startup
import org.apache.log4j.PropertyConfigurator;
import fortuneteller.neural.engine.NeuralNetServer;
import java.io.File
object MultiServer {
   var port=5500	
   PropertyConfigurator.configure("./productionlog.properties");
   val cat = org.apache.log4j.Category.getInstance("fortuneteller");
   def recursiveParser(rootDir:String, id:String) : Unit={
        for (file <- new File(rootDir).listFiles) {
         if (!file.isDirectory) {
            
            if (file.getName().substring(0,file.getName().length-4)==id){
            	cat.debug("Start network from  " + file)
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

    val id=args(0)
    val count=args(1).toInt
    cat.info(id)
    val idparts=id.split('.')
    
    var instr=idparts(0) + "." + idparts(1) + "."
    cat.info(instr)
    var num=idparts(2).toInt
    cat.info(num)
    for (c<-0 until count){
      val f=instr + num.toString
      recursiveParser("./config/neuralnets/done",f)
      port=port+1
      num=num+1
    }
    
  }
}