using System.Data.SqlClient;
using System.Diagnostics;
using AzLib.DataBase;
using Codeplex.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AzLibTest {
	[TestClass]
	public class UnitTest1 {
		[TestMethod]
		public void Test() {
			var stopWatch = new Stopwatch();

			stopWatch.Restart();
			for (int i = 0; i < 1000; i++) {
				using (var dbExe = new DbExecutor(new SqlConnection(@"Data Source=.\SQLEXPRESS;Integrated Security=true;Initial Catalog=Test"))) {
					var result = dbExe.ExecuteScalar<int>("SELECT Int FROM tTest WHERE Int = @Int", new { Int = 1 });
				}
			}

			stopWatch.Restart();
			for (int i = 0; i < 1000; i++) {
				using (var dbCon = new DbConnector()) {
					var result = dbCon.Scalar<int>("SELECT Int FROM tTest WHERE Int = @Int", new { Int = 1 });
				}
			}

			stopWatch.Restart();
			for (int i = 0; i < 1000; i++) {
				using (var dbExe = new DbExecutor(new SqlConnection(@"Data Source=.\SQLEXPRESS;Integrated Security=true;Initial Catalog=Test"))) {
					var result = dbExe.ExecuteScalar<int>("SELECT Int FROM tTest WHERE Int = @Int", new { Int = 1 });
				}
			}

			stopWatch.Restart();
			for (int i = 0; i < 1000; i++) {
				using (var dbCon = new DbConnector()) {
					var result = dbCon.Scalar<int>("SELECT Int FROM tTest WHERE Int = @Int", new { Int = 1 });
				}
			}
		}
	}
}
