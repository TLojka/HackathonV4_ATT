using ATTM2X;
using ATTM2X.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace HandsonMIkeR25
{
    class M2X
    {
        private static M2XClient m2x = new M2XClient("9ddb44e32cd115d5b5adc670f252a256");
        M2XDevice device = m2x.Device("7787671fb86b9e2bc3ccaa23ad934bf6");
        M2XStream stream;
        private static M2XResponse response;
        DateTime limit;
        DateTime lastFall;

        public String getData() {
            stream = device.Stream("hasFallen");
            string a="";

            response = stream.Values(new ATTM2X.Classes.StreamValuesFilter { start = M2XClient.DateTimeToString(DateTime.UtcNow.AddMinutes(-3)) }, M2XStreamValuesFormat.Json).Result;
            var data = response.Json<StreamValues>();

            if (DateTime.Compare(limit, Convert.ToDateTime(data.end)) < 0)
            {
                if (data.values.Length != 0)
                {
                    TimeSpan span = (Convert.ToDateTime(data.values[0].timestamp) - lastFall);
                    if (span.Seconds > 30 ) { 
                    a = data.values[0].value;
                        if (a == "fall")
                            lastFall = Convert.ToDateTime(data.values[0].timestamp);
                            System.Console.WriteLine("FALLLL---------------------------------/////////");
                    }
                }
            }
            else
                a = "";

            limit = Convert.ToDateTime(data.end);

                return a;
        }       
    }
}
