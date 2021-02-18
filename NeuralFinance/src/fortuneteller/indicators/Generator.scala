package fortuneteller.indicators
import java.io.File;

import org.encog.ml.data.MLData;
import org.encog.ml.data.basic.BasicMLData;
import org.encog.ml.data.buffer.BufferedMLDataSet;
import org.encog.util.arrayutil.NormalizationAction;
import org.encog.util.arrayutil.NormalizedField;
import org.encog.util.arrayutil.WindowDouble;
import org.encog.util.csv.CSVFormat;
import org.encog.util.csv.ReadCSV;

class Generator(thePath:File) {
	 val cat = org.apache.log4j.Category.getInstance("fortuneteller");
	/**
	 * The path that the data files will be stored at.
	 */
	val  path:File=thePath;	
	
	/**
	 * The path to the training file.
	 */
	var  trainingFile=new File(this.path,Config.FILENAME_TRAIN);	;
	/**
	 * Used to normalize the difference between the two fields.
	 */
	val fieldDifference = new NormalizedField(NormalizationAction.Normalize,"diff",Config.DIFF_RANGE,-Config.DIFF_RANGE,1,-1);
	/**
	 * Used to normalize the outcome (gain or loss).
	 */
	val fieldOutcome = new NormalizedField(NormalizationAction.Normalize,"out",Config.PIP_RANGE,-Config.PIP_RANGE,1,-1);
	
	
	/**
	 * A moving window used to track future gains.
	 */
	val  window = new fortuneteller.utils.SlidingWindow(Config.PREDICT_WINDOW);//new WindowDouble(Config.PREDICT_WINDOW);

	
	/** 
	 * The maximum difference.
	 */
	var maxDifference=0.0;
	
	/**
	 * The minimum difference.
	 */
	var minDifference=0.0;
	
	/**
	 * The max pip gain/loss.
	 */
	var maxPIPs=0;
	
	/**
	 * The min pip gain/loss.
	 */
	var minPIPs=0;
	

	
	/**
	 * Process the individual training file.
	 * @param file The training file to process.
	 * @param output The data set to output to.
	 */
	def processFile(file:File,  output:BufferedMLDataSet) :Unit={
		
		val inputData = new BasicMLData(output.getInputSize());
		val idealData = new BasicMLData(output.getIdealSize());
				
		val csv = new ReadCSV(file.toString(),true,CSVFormat.ENGLISH);
		while(csv.next()) {
			var el=new fortuneteller.utils.Element();//Array.fill[Double](Config.INPUT_WINDOW+1)(0.0)

		//	var a =Array [Double] (Config.INPUT_WINDOW+1)
 //new double[]Config.INPUT_WINDOW+1];
			var close = csv.getDouble(1);
			
			var fastIndex = 2;
			var slowIndex = fastIndex + Config.INPUT_WINDOW;
			
			el.a(0) = close;
			for(i<- 0 until 3) {
			    //cat.info("i=" + i)
				val fast = csv.getDouble(fastIndex+i);
				val slow = csv.getDouble(slowIndex+i);
				val diff = this.fieldDifference.normalize( (fast - slow)/Config.PIP_SIZE);	
				cat.info("Close " + close + " SMA " + fast + " " + slow + " " + diff)
				el.a(i+1) = diff;
			}
			//cat.info(a)
			window.add(el);
			
			
			if( window.isFull() ) {
			  

				val currEval=window.getElement(0)
				//  cat.info("this.window.calculateMax(0,0) " + this.window.calculateMax(0,0))
			  //	cat.info("close " + close  + " Config.PIP_SIZE " + Config.PIP_SIZE)
			  	val max = (this.window.calcMax(0,1)-currEval.a(0))/Config.PIP_SIZE;
				val min = (this.window.calcMin(0,1)-currEval.a(0))/Config.PIP_SIZE;
				cat.info("max=" + max + " min=" + min)
				var o=0.0;
				
				if( Math.abs(max)>Math.abs(min) ) {
					o = max;
				} else {
					o = min;
				}
				cat.info("o=" +o)
				//a = window.getLast();
				for(i<- 0 until 3) {							
					inputData.setData(i, currEval.a(i+1));
					cat.info("Input " + currEval.a(i+1))
				}
				cat.info("Ideal " + o)
				o = this.fieldOutcome.normalize(o);
				idealData.setData(0, o);
				
				output.add(inputData, idealData);
			}			
		}
	}
	
	/**
	 * Used to calibrate the training file. 
	 * @param file The file to consider.
	 */
	def calibrateFile(file:File):Unit= {
						
		val csv = new ReadCSV(file.toString(),true,CSVFormat.ENGLISH);
		while(csv.next()) {
			//var a=Array [Double] (1)
			var el=new fortuneteller.utils.Element();
		//	double a[] = new double[1];
			val close = csv.getDouble(1);
			
			val fastIndex = 2;
			val slowIndex = fastIndex + Config.INPUT_WINDOW;
			el.a(0) = close;
			for(i <- 0 until Config.INPUT_WINDOW) {
				val fast = csv.getDouble(fastIndex+i);
				val slow = csv.getDouble(slowIndex+i);
				cat.info("fast " + fast + "slow " + slow)
				if( !java.lang.Double.isNaN(fast) && !java.lang.Double.isNaN(slow) ) {
					val diff = (fast - slow)/Config.PIP_SIZE;
					this.minDifference = Math.min(this.minDifference, diff);
					this.maxDifference = Math.max(this.maxDifference, diff);
					cat.info("SMA " + fast + " " + slow + " " + diff)
				}
			}
			window.add(el);
			
			if( window.isFull() ) {
			  
				val currEval=window.getElement(0)
			  	val max = (this.window.calcMax(0,1)-currEval.a(0))/Config.PIP_SIZE;
				val min = (this.window.calcMin(0,1)-currEval.a(0))/Config.PIP_SIZE;
				var o=0.0;
				
				if( Math.abs(max)>Math.abs(min) ) {
					o = max;
				} else {
					o = min;
				}
				
				this.maxPIPs = Math.max(this.maxPIPs, o.toInt);
				this.minPIPs = Math.min(this.minPIPs, o.toInt);
			}			
		}
	}

	/**
	 * Called to generate the training file.
	 */
	def generate() :Unit={
		val list = this.path.listFiles();
		
		this.trainingFile.delete();
		val output = new BufferedMLDataSet(this.trainingFile);
		output.beginLoad(Config.INPUT_WINDOW, 1);

		for (file<- list) {
			val fn = file.getName();
			if (fn.startsWith("collected") && fn.endsWith(".csv")) {
				processFile(file, output);
			}
		}
		
		output.endLoad();
		output.close();
	}

	/**
	 * Called to calibrate the data.  Does not actually do anything, other
	 * than display a range report.
	 */
	def calibrate():Unit= {
		val list = this.path.listFiles();
		
		this.maxDifference = java.lang.Double.NEGATIVE_INFINITY;
		this.minDifference = java.lang.Double.POSITIVE_INFINITY;
		this.maxPIPs = Integer.MIN_VALUE;
		this.minPIPs = Integer.MAX_VALUE;
		
		for (file <- list) {
			val fn = file.getName();
			if (fn.startsWith("collected") && fn.endsWith(".csv")) {
				calibrateFile(file);
			}
		}		
		
		System.out.println("Max difference: " + this.maxDifference);
		System.out.println("Min difference: " + this.minDifference);
		System.out.println("Max PIPs: " + this.maxPIPs);
		System.out.println("Min PIPs: " + this.minPIPs);
		System.out.println("\nSuggested calibration: ");
		System.out.println("DIFF_RANGE = " + (Math.max(this.maxDifference,Math.abs(this.minDifference)) * 1.2) );
		System.out.println("PIP_RANGE = " + (Math.max(this.maxPIPs,Math.abs(this.minPIPs)) * 1.2) );

	}
}
