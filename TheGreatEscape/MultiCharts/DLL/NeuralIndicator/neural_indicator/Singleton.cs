using System;
using System.Collections.Generic;
using System.Text;

namespace neural_indicator
{
    public class Singleton
    {
        private static Singleton instance;

        private Singleton() { }
        private String serverHost;
        private int serverPort;
        public void setServerPort(String host, int port)
        {
            serverHost = host;
            serverPort = port;
        }

        public static Singleton Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Singleton();
                }
                return instance;
            }
        }
    }
}
