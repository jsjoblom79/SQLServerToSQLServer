using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLServerToSQLServer
{
    public class ProgramService
    {
        public static void CreateFolderStructure(string url)
        {
            if(!Directory.Exists(url))
            {
                Directory.CreateDirectory(url);
                Directory.CreateDirectory($"{url}\\Logs");
            }
        }
    }
}
