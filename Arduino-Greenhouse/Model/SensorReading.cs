using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arduino_Greenhouse.Model
{
    public class SensorReading
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int light { get; set; }
        public float temp1 { get; set; }
        public float temp2 { get; set; }
        public float RH1 { get; set; }
        public float RH2 { get; set; }

        //public DateTime timestamp { get; set; } = DateTime.UtcNow;
        public long timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public float getFahr(float temp)
        {
            return temp * (float)1.8 + (float)32.0;
        }
    }
}
