// See https://aka.ms/new-console-template for more information

using Serilog;
using SQLServerToSQLServer;

string copyFrom, copyTo;
Console.Title = "SQLServer to SQLServer Copy";
Console.ForegroundColor = ConsoleColor.Green;
Console.Write("Enter a SQL Server Instance to Copy From: ");
copyFrom = Console.ReadLine();

Console.Write("Enter a SQL Server Instance to Copy To: ");
copyTo = Console.ReadLine();

Console.WriteLine("Log Files can be found at C:\\Logs");

Log.Logger = new LoggerConfiguration()
    .WriteTo.File($"C:\\Logs\\SqlMigrationScript_.logs", rollingInterval: RollingInterval.Day)
    .MinimumLevel.Warning()
    .CreateLogger();

SQLService.CopySQLInstance(copyFrom, copyTo);
//Added to prevent the console window from closeing
Console.ReadKey();
