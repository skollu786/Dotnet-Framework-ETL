using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection;

namespace ChoETL
{
    internal class ChoETLSQLiteDbContext<T> : DbContext
    {
        public Action<string> Log = Console.WriteLine;

        public ChoETLSQLiteDbContext(string connectionString)
        {
            
        }

        //public ChoETLSQLiteDbContext(string connectionString) :
        //    base(new SQLiteConnection()
        //    {
        //        ConnectionString = connectionString // new SQLiteConnectionStringBuilder() { DataSource = dbFilePath, ForeignKeys = true }.ConnectionString
        //    }, true)
        //{
        //    Database.Log = new Action<string>((m) =>
        //    {
        //        if (Log != null)
        //            Log(m);
        //        else
        //            ChoETLLog.Info(m);
        //    });
        //    Database.SetInitializer<ChoETLSQLiteDbContext<T>>(null);
        //}
        //protected override void OnModelCreating(DbModelBuilder modelBuilder)
        //{
        //    modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        //    typeof(T).ScanAndDefineKeyToEntity();

        //    MethodInfo method = modelBuilder.GetType().GetMethod("Entity");
        //    method = method.MakeGenericMethod(new Type[] { typeof(T) });
        //    method.Invoke(modelBuilder, null);
        //    base.OnModelCreating(modelBuilder);
        //}
    }


}
