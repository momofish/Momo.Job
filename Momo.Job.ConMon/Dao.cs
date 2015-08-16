using Microsoft.Practices.EnterpriseLibrary.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Momo.Job.ConMon
{
    public class Dao
    {
        public enum SqlType
        {
            Select,
            Insert,
            Update,
            Delete
        }

        public Database Db { get; set; }

        public Dao(string name)
        {
            var factory = new DatabaseProviderFactory();
            var db = factory.Create(name);
            Db = db;
        }

        public static Dao Get(string name)
        {
            return new Dao(name);
        }

        private DbCommand PrepareParams<T>(SqlType sqlType, object param, string sql) where T : class
        {
            var tableName = typeof(T).Name;
            var paramDict = param as IDictionary<string, object> ?? param.GetType().GetProperties().ToDictionary(f => f.Name, f => f.GetValue(param));

            if (string.IsNullOrEmpty(sql))
            {
                switch (sqlType)
                {
                    case SqlType.Select:
                        sql = string.Format("select * from {0} where 1=1 {1}", tableName,
                            string.Join(" ", paramDict.Select(f => string.Format("and {0}=@{0}", f.Key))));
                        break;
                    case SqlType.Insert:
                        sql = string.Format("insert into {0}({1}) values({2})", tableName,
                            string.Join(",", paramDict.Select(f => f.Key)),
                            string.Join(",", paramDict.Select(f => "@" + f.Key)));
                        break;
                    case SqlType.Update:
                        sql = string.Format("update {0} set {1} where {2}=@{2}", tableName,
                            string.Join(",", paramDict.Skip(1).Select(f => string.Format("{0}=@{0}", f.Key))),
                            paramDict.First().Key);
                        break;
                    case SqlType.Delete:
                        sql = string.Format("delete {0} where {1}=@{1}", tableName,
                            paramDict.First().Key);
                        break;
                }
            }

            var command = Db.GetSqlStringCommand(sql);
            paramDict.All(f =>
            {
                Db.AddInParameter(command, f.Key, DbType.Object, f.Value);
                return true;
            });
            return command;
        }

        private static List<T> ExtractList<T>(IDataReader reader) where T : class
        {
            var list = new List<T>();
            var typeFields = typeof(T).GetProperties().ToDictionary(p => p.Name);
            while (reader.Read())
            {
                var item = Activator.CreateInstance<T>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    if (!typeFields.ContainsKey(name))
                        continue;
                    var field = typeFields[name];
                    if (reader[i] != DBNull.Value)
                        field.SetValue(item, reader[i]);
                }
                list.Add(item);
            }
            return list;
        }

        public T Get<T>(object param, string sql = null) where T : class
        {
            return List<T>(param, sql).FirstOrDefault();
        }

        public List<T> List<T>(object param, string sql = null) where T : class
        {
            var command = PrepareParams<T>(SqlType.Select, param, sql);
            using (var reader = Db.ExecuteReader(command))
            {
                return ExtractList<T>(reader);
            }
        }

        public void Insert<T>(object param, string sql = null) where T : class
        {
            var command = PrepareParams<T>(SqlType.Insert, param, sql);
            Db.ExecuteNonQuery(command);
        }

        public void Update<T>(object param, string sql = null) where T : class
        {
            var command = PrepareParams<T>(SqlType.Update, param, sql);
            Db.ExecuteNonQuery(command);
        }

        public void Delete<T>(object param, string sql = null) where T : class
        {
            var command = PrepareParams<T>(SqlType.Delete, param, sql);
            Db.ExecuteNonQuery(command);
        }
    }
}
