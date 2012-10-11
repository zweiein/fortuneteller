package fortuneteller.indicators
import org.encog.ml.factory.MLMethodFactory;
import org.encog.ml.factory.MLTrainFactory;
object Config {
	/**
	 * The maximum range (either positive or negative) that the difference between the fast and slow will be normalized to.
	 */
	val DIFF_RANGE = 50;
	/**
	 * The maximum range (either positive or negative) that the pip profit(or loss) will be in.
	 */
	val PIP_RANGE = 35;
	/**
	 * The size of a single PIP (i.e. 0.0001 for EURUSD)
	 */
	val PIP_SIZE = 0.0001;
	
	/**
	 * The size of the input window.  This is the number of previous bars to consider.
	 */
	val INPUT_WINDOW = 3;
	
	/**
	 * The number of bars to look forward to determine a max profit, or loss.
	 */
	val PREDICT_WINDOW = 10;
	
	/**
	 * The targeted error.  Once the training error reaches this value, training will stop.
	 */
	val TARGET_ERROR = 0.03f;
	
	/**
	 * The type of method.  This is an Encog factory code.
	 */
	val METHOD_TYPE = MLMethodFactory.TYPE_FEEDFORWARD;
	
	/**
	 * The architecture of the method.  This is an Encog factory code.
	 */
	val METHOD_ARCHITECTURE = "?:B->TANH->20:B->TANH->?";
	
	/**
	 * The type of training.  This is an Encog factory code.
	 */
	val TRAIN_TYPE = MLTrainFactory.TYPE_RPROP;
	
	/**
	 * The training parameters.  This is an Encog factory code.
	 */
	val TRAIN_PARAMS = "";
	
	/**
	 * The filename for the training data.
	 */
	val FILENAME_TRAIN = "training.egb";
	
	/**
	 * The filename to store the method to.
	 */
	val METHOD_NAME = "method.eg";
	
}