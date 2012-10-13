package fortuneteller.indicators

import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.io.PrintWriter;

import org.encog.Encog;
import org.encog.EncogError;
import org.encog.cloud.indicator.basic.BasicIndicator;
import org.encog.cloud.indicator.basic.InstrumentHolder;
import org.encog.cloud.indicator.server.IndicatorLink;
import org.encog.cloud.indicator.server.IndicatorPacket;
import org.encog.ml.MLRegression;
import org.encog.ml.data.MLData;
import org.encog.ml.data.basic.BasicMLData;
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import org.encog.util.csv.CSVFormat;
import scala.collection.JavaConversions._
class DemoIndicator( theMethod:MLRegression,  thePath:java.io.File) extends BasicIndicator(theMethod!=null) {
		val method = theMethod;
		val path = thePath;
		var holder = new InstrumentHolder();
		var rowsDownloaded:Int=0
		requestData("CLOSE[1]");
		requestData("SMA(10)["+Config.INPUT_WINDOW+"]");
		requestData("AaLaguerreMA(70)["+Config.INPUT_WINDOW+"]");
		//requestData("AaLaguerreMA(70)["+Config.INPUT_WINDOW+"]");
		val fieldDifference = new NormalizedField(NormalizationAction.Normalize,"diff",Config.DIFF_RANGE,-Config.DIFF_RANGE,1,-1);
		val fieldOutcome = new NormalizedField(NormalizationAction.Normalize,"out",Config.PIP_RANGE,-Config.PIP_RANGE,1,-1);
	/**
	 * Called to notify the indicator that a bar has been received.
	 * @param packet The packet received.
	 */

	override def notifyPacket( packet:IndicatorPacket):Unit= {
		val security = packet.getArgs()(1);
		val when = java.lang.Long.parseLong(packet.getArgs()(0));
		val key = security.toLowerCase();

		if (this.method==null) {
			if (holder.record(when, 2, packet.getArgs())) {
				rowsDownloaded=rowsDownloaded+1;
			}
		} else {
			val input = new BasicMLData(Config.PREDICT_WINDOW);
			
			val fastIndex = 2;
			val slowIndex = fastIndex + Config.INPUT_WINDOW;
			
			for(i<-List(0,1,2)) {
				val fast = CSVFormat.EG_FORMAT.parse(packet.getArgs()(fastIndex+i));
				val slow = CSVFormat.EG_FORMAT.parse(packet.getArgs()(slowIndex+i));
				val diff = this.fieldDifference.normalize( (fast - slow)/Config.PIP_SIZE);		
				input.setData(i, this.fieldDifference.normalize(diff) );
			}
						
			val result = this.method.compute(input);
			
			var d = result.getData(0);
			d = this.fieldOutcome.deNormalize(d);
			
			val args:List[String] = List(
					"?",	// line 1
					"?",	// line 2
					"?",	// line 3
					CSVFormat.EG_FORMAT.format(d,Encog.DEFAULT_PRECISION), // bar 1
					"?", // bar 2
					"?", // bar 3
					"?", // arrow 1
					"?"); // arrow 2
			
			this.getLink().writePacket(IndicatorLink.PACKET_IND, args.toArray);
		}
	}

	/**
	 * Determine the next file to process.
	 * @return The next file.
	 */
	def  nextFile():java.io.File= {
		var mx = -1;
		val list = this.path.listFiles();

		for (file<-list) {
			val fn = file.getName();
			if (fn.startsWith("collected") && fn.endsWith(".csv")) {
				val idx = fn.indexOf(".csv");
				val str = fn.substring(9, idx);
				val n = Integer.parseInt(str);
				mx = Math.max(n, mx);
			}
		}

		return new java.io.File(path, "collected" + (mx + 1) + ".csv");
	}

	/**
	 * Write the files that were collected.
	 */
	 def writeCollectedFile() :Unit={
		val targetFile = nextFile();

		try {
			val outFile = new FileWriter(targetFile);
			val out = new PrintWriter(outFile);

			// output header
			out.print("\"WHEN\"");
			var index = 0;
			for (str<-this.getDataRequested()) {
				var str2="";
				
				// strip off [ if needed
				var ix = str.indexOf('[');
				if (ix != -1) {
					str2 = str.substring(0, ix).trim();
				} else {
					str2 = str;
				}
				
				val c = getDataCount().get(index);
				index=index+1
				if (c <= 1) {
					out.print(",\"" + str2 + "\"");
				} else {
					 for( i <-0 to c){						
						out.print(",\"" + str2 + "-b" + i + "\"");
					}
				}
			}
			out.println();

			// output data

			for (key<-holder.getSorted()) {
				val str = holder.getData().get(key);
				out.println(key + "," + str);
			}

			out.close();
		} catch{
			 case e: IOException =>  throw new EncogError(e);
		}
	}

	/**
	 * Notify on termination, write the collected file.
	 */
	
	override def notifyTermination():Unit= {
		if (this.method==null) {
			writeCollectedFile();
		}
	}

	/**
	 * @return The number of rows downloaded.
	 */
	def getRowsDownloaded():Int ={
		return rowsDownloaded;
	}
	
	
}