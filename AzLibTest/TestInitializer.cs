using System;
using System.Data.SqlClient;
using System.Diagnostics;
using AzLib.DataBase;
using Codeplex.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AzLibTest.DataBase;

namespace AzLibTest {
	[TestClass]
	public class TestInitializer {

		/// <summary>
		/// アセンブリの初期化
		/// </summary>
		/// <param name="myTestContext"></param>
		[AssemblyInitialize()]
		public static void AssemblyInitialize(TestContext myTestContext) {
			DbSettings.Manager.AddSqlServer("Test", @"Data Source=.\SQLEXPRESS;Integrated Security=true");

			DbConnectorTest.dbCon = new DbConnector(isolationLevel: null);
			if (DbConnectorTest.dbCon.ExistsDatabase(DbConnectorTest.TEST_DB_NAME)) {
				DbConnectorTest.dbCon.DropDatabase(DbConnectorTest.TEST_DB_NAME);
			}
			DbConnectorTest.dbCon.NonQuery(@"CREATE DATABASE " + DbConnectorTest.TEST_DB_NAME);
			DbConnectorTest.dbCon.NonQuery("USE " + DbConnectorTest.TEST_DB_NAME);
			DbConnectorTest.dbCon.NonQuery(string.Format(@"
CREATE TABLE {0} (
	ID int IDENTITY(0, 1) PRIMARY KEY,
	Int int,
	String nvarchar(MAX),
	Date datetime
)
", DbConnectorTest.TEST_TABLE_NAME));
		}

		/// <summary>
		/// アセンブリのクリーンアップ
		/// </summary>
		[AssemblyCleanup()]
		public static void AssemblyCleanup() {
			DbConnectorTest.dbCon.NonQuery("USE master");
			if (DbConnectorTest.dbCon.ExistsDatabase(DbConnectorTest.TEST_DB_NAME)) {
				DbConnectorTest.dbCon.DropDatabase(DbConnectorTest.TEST_DB_NAME);
			}
			if (DbConnectorTest.dbCon.ExistsTable(DbConnectorTest.TEST_TABLE_NAME)) {
				DbConnectorTest.dbCon.DropTable(DbConnectorTest.TEST_TABLE_NAME);
			}
			DbConnectorTest.dbCon.Dispose();
		}
	}
}
