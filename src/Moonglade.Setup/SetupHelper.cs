﻿using System;
using System.IO;
using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Moonglade.Setup
{
    public class SetupHelper
    {
        public string DatabaseConnectionString { get; set; }

        public SetupHelper(string databaseConnectionString)
        {
            if (string.IsNullOrWhiteSpace(databaseConnectionString))
            {
                throw new ArgumentNullException(nameof(databaseConnectionString));
            }

            DatabaseConnectionString = databaseConnectionString;
        }

        public void InitFirstRun()
        {
            SetupDatabase();
            ResetDefaultConfiguration();
            InitSampleData();
        }

        /// <summary>
        /// Check if the blog system is first run
        /// Either BlogConfiguration table does not exist or it has empty data is treated as first run.
        /// </summary>
        public bool IsFirstRun()
        {
            using var conn = new SqlConnection(DatabaseConnectionString);
            var tableExists = conn.ExecuteScalar<int>("SELECT TOP 1 1 " +
                                                      "FROM INFORMATION_SCHEMA.TABLES " +
                                                      "WHERE TABLE_NAME = N'BlogConfiguration'") == 1;
            if (tableExists)
            {
                var dataExists = conn.ExecuteScalar<int>("SELECT TOP 1 1 FROM BlogConfiguration") == 1;
                return !dataExists;
            }

            return true;
        }

        /// <summary>
        /// Execute SQL to build database schema
        /// </summary>
        public void SetupDatabase()
        {
            using var conn = new SqlConnection(DatabaseConnectionString);
            var sql = GetEmbeddedSqlScript("schema-mssql-140");
            if (!string.IsNullOrWhiteSpace(sql))
            {
                conn.Execute(sql);
            }
            throw new InvalidOperationException("Database Schema Script is empty.");
        }

        /// <summary>
        /// Clear all data in database but preserve tables schema
        /// </summary>
        public void ClearData()
        {
            using var conn = new SqlConnection(DatabaseConnectionString);
            // Clear Relation Tables
            conn.Execute("DELETE FROM PostTag");
            conn.Execute("DELETE FROM PostCategory");
            conn.Execute("DELETE FROM CommentReply");

            // Clear Individual Tables
            conn.Execute("DELETE FROM Category");
            conn.Execute("DELETE FROM Tag");
            conn.Execute("DELETE FROM Comment");
            conn.Execute("DELETE FROM FriendLink");
            conn.Execute("DELETE FROM PingbackHistory");
            conn.Execute("DELETE FROM PostExtension");
            conn.Execute("DELETE FROM Post");
            conn.Execute("DELETE FROM Menu");

            // Clear Configuration Table
            conn.Execute("DELETE FROM BlogConfiguration");

            // Clear AuditLog Table
            conn.Execute("DELETE FROM AuditLog");
        }

        public void ResetDefaultConfiguration()
        {
            using var conn = new SqlConnection(DatabaseConnectionString);
            var sql = GetEmbeddedSqlScript("init-blogconfiguration");
            if (!string.IsNullOrWhiteSpace(sql))
            {
                conn.Execute(sql);
            }
            throw new InvalidDataException("SQL Script is empty.");
        }

        public void InitSampleData()
        {
            using var conn = new SqlConnection(DatabaseConnectionString);
            var sql = GetEmbeddedSqlScript("init-sampledata");
            if (!string.IsNullOrWhiteSpace(sql))
            {
                conn.Execute(sql);
            }
            throw new InvalidDataException("SQL Script is empty.");
        }

        public bool TestDatabaseConnection(Action<Exception> errorLogAction = null)
        {
            try
            {
                using var conn = new SqlConnection(DatabaseConnectionString);
                var result = conn.ExecuteScalar<int>("SELECT 1");
                return result == 1;
            }
            catch (Exception e)
            {
                errorLogAction?.Invoke(e);
                return false;
            }
        }

        private static string GetEmbeddedSqlScript(string scriptName)
        {
            var assembly = typeof(SetupHelper).GetTypeInfo().Assembly;
            using var stream = assembly.GetManifestResourceStream($"Moonglade.Setup.Data.{scriptName}.sql");
            if (stream == null) return null;
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();
            return sql;
        }
    }
}
