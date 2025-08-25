using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminaldiagnostic;

namespace ABTUploader
{
    class GeneralVar
    {
        public static string sqlServer = ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString;
        public static string mysqlUploadsPath = ConfigurationManager.AppSettings["mysqlUploadsPath"];

         private static Logger _Logger;
        public static Logger Logger
        {
            get
            {
                if (_Logger == null)
                    _Logger = new Logger(ConfigurationManager.AppSettings["LogPath"], ConfigurationManager.AppSettings["LogMode"], Convert.ToInt32(ConfigurationManager.AppSettings["LogKepDuration"]));

                return _Logger;
            }
        }

    }
}
