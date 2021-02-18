package fortuneteller.adapter.ose
import scala.io.Source

object WebLoader {
	
	def load(url:String, parser:GeneralParser):Unit={
	    val source = Source.fromURL(url)
	    var count=0
	    for (line<-source.getLines){
	      if (count>0) parser.parseLine(line)
	      count=count+1
	    }
	}
}