using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLServerToSQLServer
{
    public class SQLService
    {
        private static string? _connectionString;
        private static int _errorCount;
        private static string? _directory;
        public static void CopySQLInstance(string fromServer,string toServer, string directory)
        {
            string connectionString = $"Server={fromServer};Database=master;Integrated Security=True;Trust Server Certificate=True;";
            SetToServerConnectionString($"Server={toServer};Database=master;Integrated Security=True;Trust Server Certificate=True;");
            _directory = directory;
            
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                ServerConnection serverConnection = new ServerConnection(sqlConnection);
                Server server = new Server(serverConnection);

                foreach (Database db in server.Databases)
                {
                    if (!db.IsSystemObject)
                    {
                        CreateDBFolderForErrors(db);
                        if(db.Status == DatabaseStatus.Offline)
                        {
                            db.SetOnline();
                            db.Refresh();
                        }

                        if(db.Status == DatabaseStatus.Normal)
                        {
                            Scripter scripter = new Scripter(server);
                            ScriptingOptions options = new ScriptingOptions
                            {
                                ScriptForCreateDrop = true,
                                IncludeIfNotExists = true,
                                SchemaQualify = true,
                                ScriptBatchTerminator = true,
                                IncludeHeaders = true,
                                IncludeDatabaseContext = true,
                                DriAll = true,
                                Permissions = false,
                                ScriptOwner = false
                            };
                            scripter.Options = options;

                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.WriteLine($"Gathering information for {db.Name} - {DateTime.Now:MM/dd/yyyy hh:mm:ss tt}");
                            _errorCount = 0;
                            List<Table> tables = new List<Table>();
                            DataSet udTables = server.ConnectionContext.ExecuteWithResults($"USE [{db.Name}] SELECT name FROM sys.tables WHERE is_ms_shipped = 0;");
                            foreach (DataRow row in udTables.Tables[0].Rows)
                            {
                                var name = row["name"].ToString();
                                tables.Add(db.Tables[name]);
                            }
                            List<View> views = new List<View>();
                            DataSet udViews = server.ConnectionContext.ExecuteWithResults($"USE [{db.Name}] SELECT name FROM sys.views;");
                            foreach (DataRow row in udViews.Tables[0].Rows)
                            {
                                var name = row["name"].ToString();
                                views.Add(db.Views[name]);
                            }
                            List<UserDefinedFunction> functions = new List<UserDefinedFunction>();
                            DataSet udFunctions = server.ConnectionContext.ExecuteWithResults($"USE [{db.Name}] SELECT name FROM sys.objects WHERE type IN ('FN', 'IF', 'TF');");
                            foreach(DataRow row in udFunctions.Tables[0].Rows)
                            {
                                var name = row["name"].ToString();
                                functions.Add(db.UserDefinedFunctions[name]);
                            }
                            
                            List<StoredProcedure> procedures = new List<StoredProcedure>();
                            DataSet udProcedures = server.ConnectionContext.ExecuteWithResults($"USE [{db.Name}] SELECT name FROM sys.procedures;");
                            foreach (DataRow row in udProcedures.Tables[0].Rows)
                            {
                                var name = row["name"].ToString();
                                procedures.Add(db.StoredProcedures[name]);
                            }
                            var types = db.UserDefinedTypes;


                            CreateDatabase(db, scripter);
                            if (tables.Count > 0)
                            {
                                Console.WriteLine("Creating Tables.");
                                CreateTables(db, tables, scripter);
                            }
                            

                            if (types.Count > 0)
                            {
                                Console.WriteLine("Creating User Defined Types.");
                                CreateUserDefinedTypes(db, types, scripter);
                            }
                                
                            if (functions.Count > 0)
                            {
                                Console.WriteLine("Creating User Defined Functions. ");
                                CreateFunctions(db, functions, scripter);
                            }
                                
                            if (views.Count > 0)
                            {
                                Console.WriteLine("Creating Views. ");
                                CreateViews(db, views, scripter);
                            }
                                
                            if (procedures.Count > 0)
                            {
                                Console.WriteLine("Creating Procedures. ");
                                CreateProcedures(db, procedures, scripter);
                            }
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"All Scripts for {db.Name} have been completed. - {DateTime.Now:MM/dd/yyyy hh:mm:ss tt}");
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            if(_errorCount > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"Check the Log file for errors. Error Count for {db.Name} was {_errorCount}");
                                Console.ForegroundColor = ConsoleColor.DarkCyan;
                            }
                        }
                    }
                }
            }
        }

        private static void CreateProcedures(Database db, List<StoredProcedure> procedures, Scripter scripter)
        {
            
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                ServerConnection serverConnection = new ServerConnection(sqlConnection);
                Server server = new Server(serverConnection);
                procedures = procedures.Where(t => !t.IsSystemObject).ToList();
                foreach(StoredProcedure item in procedures) 
                {
                    var query = "";
                    try
                    {
                        var script = scripter.Script(new Urn[] { item.Urn });

                        
                        foreach (var line in script)
                        {
                            query += line.AddNewLine().AddGoBetweenBatch();
                        }
                        var result = server.ConnectionContext.ExecuteNonQuery(query);
                    }
                    catch (Exception ex)
                    {
                        
                        var logFileName = $"C:\\{_directory}\\{db.Name}\\StoredProcedures\\{item.Name}_ProcedureCreate_FailedScript.sql";
                        File.WriteAllText(logFileName, query);
                        Log.Error(ex.InnerException, ex.Message);
                        _errorCount++;
                    }
                };
            }
            
        }

        private static void CreateFunctions(Database db, List<UserDefinedFunction> functions, Scripter scripter)
        {
           
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                ServerConnection serverConnection = new ServerConnection(sqlConnection);
                Server server = new Server(serverConnection);
                
                foreach (UserDefinedFunction item in functions)
                {
                    var query = "";
                    try
                    {
                        var script = scripter.Script(new Urn[] { item.Urn });

                        
                        foreach (var line in script)
                        {
                            query += line.AddNewLine();
                        }
                        var result = server.ConnectionContext.ExecuteNonQuery(query);
                    }
                    catch (Exception ex)
                    {
                        
                        var logFileName = $"C:\\{_directory}\\{db.Name}\\Functions\\{item.Name}_FunctionCreate_FailedScript.sql";
                        File.WriteAllText(logFileName, query);
                        Log.Error(ex.InnerException, ex.Message);
                        _errorCount++;
                    }
                };
            }
            
        }

        private static void CreateViews(Database db, List<View> views, Scripter scripter)
        {
           
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                ServerConnection serverConnection = new ServerConnection(sqlConnection);
                Server server = new Server(serverConnection);
                views = views.Where(t => !t.IsSystemObject).ToList();
                foreach(View item in views)
                {
                    var query = "";
                    try
                    {
                        var script = scripter.Script(new Urn[] { item.Urn });

                        
                        foreach (var line in script)
                        {
                            query += line.AddNewLine();
                        }
                        var result = server.ConnectionContext.ExecuteNonQuery(query);
                    }
                    catch (Exception ex)
                    {
                        
                        var logFileName = $"C:\\{_directory}\\{db.Name}\\Views\\{item.Name}_ViewCreate_FailedScript.sql";
                        File.WriteAllText(logFileName, query);
                        Log.Error(ex.InnerException, ex.Message);
                        _errorCount++;
                    }

                };
            }
           
        }

        private static void CreateTables(Database db, List<Table> tables, Scripter scripter)
        {

            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                ServerConnection serverConnection = new ServerConnection(sqlConnection);
                Server server = new Server(serverConnection);
                //tables = tables.Where(t => !t.IsSystemObject).ToList();
                foreach(Table item in tables)
                {
                    var query = "";
                    try
                    {
                        var script = scripter.Script(new Urn[] { item.Urn });

                        
                        foreach (var line in script)
                        {
                            query += line.AddNewLine();
                        }
                        var result = server.ConnectionContext.ExecuteNonQuery(query);
                    }
                    catch (Exception ex)
                    {
                        
                        var logFileName = $"C:\\{_directory}\\{db.Name}\\Tables\\{item.Name}_TableCreate_FailedScript.sql";
                        File.WriteAllText(logFileName, query);
                        Log.Error(ex.InnerException, ex.Message);
                        _errorCount++;
                    }

                }
            }
        }
            
        private static void CreateUserDefinedTypes(Database db, UserDefinedTypeCollection types, Scripter scripter)
        {
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                ServerConnection serverConnection = new ServerConnection(sqlConnection);
                Server server = new Server(serverConnection);
                var query = "";
                foreach (UserDefinedType item in types)
                {
                    try
                    {
                        var script = scripter.Script(new Urn[] { item.Urn });

                        
                        foreach (var line in script)
                        {
                            query += line.AddNewLine();
                        }
                        var result = server.ConnectionContext.ExecuteNonQuery(query);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.InnerException, ex.Message);
                        _errorCount++;
                    }

                }
            }
        }
            
    

        public static void CreateDatabase(Database db,Scripter scripter)
        {
            
            var dbScript = scripter.Script(new Urn[] { db.Urn });
            
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                ServerConnection serverConnection = new ServerConnection(sqlConnection);
                Server server = new Server(serverConnection);
                var query = "";
                try
                {
                    
                    foreach (var line in dbScript)
                    {
                        query += line.AddNewLine();
                    }
                    var result = server.ConnectionContext.ExecuteNonQuery(query.ToCleanStatement(db.Name));
         
                }
                catch (Exception ex)
                {
                    
                    var queryFileName = $"C:\\{_directory}\\{db.Name}\\{db.Name}_DatabaseCreate_FailedScript.sql";
                    File.WriteAllText(queryFileName, query.ToCleanStatement(db.Name));
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.InnerException);
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Log.Error(ex.InnerException, ex.Message);
                    _errorCount++;
                }
                
            }
            
        }

        private static void CreateDBFolderForErrors(Database db)
        {
            var folder = @$"{_directory}\{db.Name}";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                Directory.CreateDirectory($"{folder}\\Tables");
                Directory.CreateDirectory($"{folder}\\Views");
                Directory.CreateDirectory($"{folder}\\Functions");
                Directory.CreateDirectory($"{folder}\\StoredProcedures");
            }
        }

        private static void SetToServerConnectionString(string connectionString)
        {
            _connectionString = connectionString;
        }
    }
}
