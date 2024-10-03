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
            //string use = @"USE\s*\[\w+\]";
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

        public static string ToCleanCreateDatabaseStatement(this string statement, string databaseName, string mdf, string ldf)
        {
            string mdfPattern = @"[A-Z]:\\.*?\.mdf";
            string ndfPattern = @"FILENAME = N'(.*?\\)([^\\]+\.ndf)'";
            string ldfPattern = @"[A-Z]:\\.*?\.ldf";
            string batch = @"IF\s*\(\s*1\s*=\s*FULLTEXTSERVICEPROPERTY";
            var mdfFileName = Path.GetFileNameWithoutExtension(mdf); 
            mdf = mdf.Replace(mdfFileName, databaseName);
            var ldfFileName = Path.GetFileNameWithoutExtension(ldf);
            ldf = ldf.Replace(ldfFileName, databaseName + "_log");

            var newPath = Path.GetDirectoryName(mdf);
            statement = Regex.Replace(statement, ndfPattern, m => $"FILENAME = N'{newPath}\\{m.Groups[2].Value}'", RegexOptions.IgnoreCase);

            var resultString = Regex.Replace(statement, mdfPattern, mdf, RegexOptions.IgnoreCase);
            resultString = Regex.Replace(resultString, ldfPattern, ldf, RegexOptions.IgnoreCase);
            //resultString = Regex.Replace(resultString,ndfPattern, mdf, RegexOptions.IgnoreCase);
            resultString = Regex.Replace(resultString, batch, $"GO\nIF (1 = FULLTEXTSERVICEPROPERTY");

            return resultString;
        }

        public static string ToUseDBName(this string statement, string databaseName)
        {
            return $"USE [{databaseName}]\nGO\n{statement}";
        }

        public static string AddGoBetweenBatch(this string statement)
        {
            if(statement.StartsWith("IF NOT EXISTS"))
            {
                return statement + "\nGO\n";
            }
            return statement;
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
