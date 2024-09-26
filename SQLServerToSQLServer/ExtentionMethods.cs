using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SQLServerToSQLServer
{
    public static class ExtentionMethods
    {

        public static string ToCleanStatement(this string statement, string databaseName)
        {
            string mdfPattern = @"[A-Z]:\\.*?\.mdf";
            string ldfPattern = @"[A-Z]:\\.*?\.ldf";
            string batch = @"IF\s*\(\s*1\s*=\s*FULLTEXTSERVICEPROPERTY";
            string use = @"USE\s*\[\w+\]";
            string mdf = $"E:\\Program Files\\Microsoft SQL Server\\MSSQL16.MSSQLSERVER\\MSSQL\\DATA\\{databaseName}.mdf";
            string ldf = $"F:\\Program Files\\Microsoft SQL Server\\MSSQL16.MSSQLSERVER\\MSSQL\\Data\\{databaseName}_log.ldf";
            var resultString = Regex.Replace(statement, mdfPattern, mdf,RegexOptions.IgnoreCase);
            resultString = Regex.Replace(resultString, ldfPattern, ldf, RegexOptions.IgnoreCase);
            resultString = Regex.Replace(resultString, batch, $"GO\nIF (1 = FULLTEXTSERVICEPROPERTY");
            //if(resultString.Contains("CREATE DATABASE"))
            //{
            //    resultString += " GO\n";
            //}
            return resultString;
        }

        public static string ToUseDBName(this string statement, string databaseName)
        {
            return $"USE [{databaseName}]\nGO\n{statement}";
        }

        public static string AddNewLine(this string statement)
        {
            if (!statement.EndsWith("\r\n"))
            {
                return $"{statement}\r\n";
            }
            return statement ;
        }
    }
}
