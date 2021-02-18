package fortuneteller.aq
import scala.collection.mutable.ListBuffer
import com.activequant.dao.IDaoFactory
import com.activequant.domainmodel.Future
import org.springframework.context.support.ClassPathXmlApplicationContext
class Connect{
  val cat = org.apache.log4j.Category.getInstance("fortuneteller");
	def setupConnection(instr:String):Unit={

	val appContext = new ClassPathXmlApplicationContext("springtest.xml");
	val idf = appContext.getBean("ibatisDao", classOf[IDaoFactory]);
	val idao = idf.instrumentDao();
	
	val future = new Future();
	future.setCreationTime(0L);
	future.setDeletionTime(0L);
	future.setName("FDAX");
	future.setDescription("The dark dax");
	future.setExpiry(20111231l);
	future.setShortName("FDAX");
	future.setTickSize(10.0);
	future.setTickValue(10.0);
	idao.create(future);
	
	// load the future
	val loadedFuture =  idao.load(future.getId());
	cat.info(loadedFuture.getDescription());
	}
}