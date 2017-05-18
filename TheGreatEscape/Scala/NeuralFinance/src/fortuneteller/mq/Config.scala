package fortuneteller.mq

object Config {
  
  val RABBITMQ_HOST = "localhost"//ConfigFactory.load().getString("rabbitmq.host");
  val RABBITMQ_QUEUE ="queue1"// ConfigFactory.load().getString("rabbitmq.queue");
  val RABBITMQ_EXCHANGEE ="exchange1"// ConfigFactory.load().getString("rabbitmq.exchange");
}