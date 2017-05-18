#!/bin/sh
# Runs the data relay  
java -cp target/activequant-p2-1.3-SNAPSHOT.jar;target/aq2o-apps-2.2-SNAPSHOT-jar-with-dependencies.jar -DJMS_HOST=localhost -DJMS_PORT=7676 -DARCHIVE_BASE_FOLDER=/var/www/archive2 org.activequant.util.RudeCandleRecorder 


