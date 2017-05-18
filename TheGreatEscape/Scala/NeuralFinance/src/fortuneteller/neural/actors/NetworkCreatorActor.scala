package fortuneteller.neural.actors;

import akka.actor.Actor;
import akka.actor.ActorSystem;
import akka.actor.Props;


class NetworkCreatorActor extends Actor {
  val cat = org.apache.log4j.Category.getInstance("fortuneteller");
  
 
  def receive = {
    case net:fortuneteller.neural.engine.NeuralNetConfig => {
    	cat.info(this + " going to process " + net.configId)    
    	try{
	        val nc=new fortuneteller.neural.engine.NeuralNetCreator(net)
	        nc.createNeuralNet()
			val cout = new java.io.FileWriter("./config/neuralnets/done/" + net.getConfigName)
			cout.write(net.getConfig)
			cout.close
		
		} catch {
		  case e: Exception => {
			  e.printStackTrace()
		  	  val cout = new java.io.FileWriter("./config/neuralnets/error/" + net.getConfigName)
			  cout.write(net.getConfig)
			  cout.close
		  }
		}	        
    }
    case _       => println("Not supported")
  }
}