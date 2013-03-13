using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AzLib.DataBase;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AzLibTest.DataBase {
	[TestClass]
	public class DbConnectorTest {
		private static int _id;
		public static DbConnector dbCon;

		[AssemblyInitialize()]
		public static void AssemblyInitialize(TestContext myTestContext) {
			DbSettings.Manager.AddSqlServer("Test", @"Data Source=.\SQLEXPRESS;Integrated Security=true;Initial Catalog=Test");
			dbCon = new DbConnector(isolationLevel: null);
		}

		#region 接続操作系メソッド
		[TestMethod]
		public void OpenTest() {
			using (var dbCon = new DbConnector(isOpenConnection: false)) {

				dbCon.Open();

				Assert.AreEqual(dbCon.State, ConnectionState.Open);
			}
		}

		[TestMethod]
		public void CloseTest() {
			using (var dbCon = new DbConnector()) {
				dbCon.Close();
				Assert.AreEqual(dbCon.State, ConnectionState.Closed);

				//何度も発行しても大丈夫か
				dbCon.Close();
			}
		}
		#endregion

		#region クエリ発行メソッド
		[TestMethod]
		public void ScalarTest() {
			using (var dbCon = new DbConnector()) {
				//簡易型
				Assert.AreEqual(dbCon.Scalar("tTest", "String", new { Int = 1 }), "Test");
				Assert.AreEqual(dbCon.Scalar("tTest", "Money", new { Int = 1 }), (decimal)7);
				Assert.AreEqual(dbCon.Scalar("tTest", "Numeric", new { Int = 1 }), (decimal)6);

				//SQL型
				Assert.AreEqual(dbCon.Scalar("SELECT Text From tTest Where Int = @Int", new { Int = 1 }), "テスト");
				Assert.AreEqual(dbCon.Scalar("SELECT Date From tTest Where Int = @Int", new { Int = 1 }), new DateTime(2000, 1, 1));
				Assert.AreEqual(dbCon.Scalar("SELECT Bool From tTest Where Int = @Int", new { Int = 1 }), true);

				//スカラ値取得メソッドなのに複数値取得クエリ
				Assert.AreEqual(dbCon.Scalar("SELECT Bool, String From tTest Where Int = @Int", new { Int = 1 }), true);
				Assert.AreEqual(dbCon.Scalar("SELECT Bool, String From tTest"), true);

				//NULLの検証
				Assert.AreEqual(dbCon.Scalar("tTest", "Date", new { Int = DBNull.Value }), null);
			}
		}

		[TestMethod]
		public void ScalarGenericTest() {
			Assert.AreEqual(dbCon.Scalar<string>("tTest", "String", new { Int = 1 }), "Test");
			Assert.AreEqual(dbCon.Scalar<decimal>("tTest", "Money", new { Int = 1 }), 7);
			Assert.AreEqual(dbCon.Scalar<decimal>("tTest", "Numeric", new { Int = 1 }), 6);
			Assert.AreEqual(dbCon.Scalar<string>("tTest", "Text", new { Int = 1 }), "テスト");
			Assert.AreEqual(dbCon.Scalar<DateTime>("tTest", "Date", new { Int = 1 }), new DateTime(2000, 1, 1));
			Assert.AreEqual(dbCon.Scalar<bool>("tTest", "Bool", new { Int = 1 }), true);

			//NULLの検証
			Assert.AreEqual(dbCon.Scalar<DateTime>("tTest", "Date", new { Int = DBNull.Value }), default(DateTime));
			Assert.AreEqual(dbCon.Scalar<DateTime?>("tTest", "Date", new { Int = DBNull.Value }), null);
		}

		[TestMethod]
		public void ReaderTest() {
			var headerList = new List<string>(new string[]{
						"Int","TinyInt","BigInt","Float","Decimal","Numeric","Money","String","Date","DateTime","Bool","Binary","Text","TimeStamp","Nul"
					});
			var gotHeader = new List<string>();
			var resultEnum = dbCon.Reader("SELECT * FROM tTest ORDER BY Int");
			var valueTestEnum = resultEnum
				.Select(rec => {
					if (gotHeader.Count == 0) {
						for (int i = 0; i < rec.FieldCount; i++) {
							gotHeader.Add(rec.GetName(i));
						}
					}

					return new {
						Int = rec.GetInt32(0),
						Float = rec.GetDouble(3),
						Decimal = rec.GetDecimal(4),
						String = rec["String"],
						Date = rec["Date"],
						Nul = rec.IsDBNull(14)
					};
				});

			Assert.AreEqual(headerList
				.Zip(headerList, (str, lst) => new { List = str, Db = lst })
				.Count(itm => itm.List != itm.Db), 0);

			Assert.AreEqual(valueTestEnum.First().Int, 1);
			Assert.AreEqual(valueTestEnum.First().Float, 4f);
			Assert.AreEqual(valueTestEnum.First().Decimal, (decimal)5.1);
			Assert.AreEqual(valueTestEnum.First().String, "Test");
			Assert.AreEqual(valueTestEnum.First().Date, new DateTime(2000, 1, 1));
			Assert.AreEqual(valueTestEnum.First().Nul, true);

			Assert.AreEqual(valueTestEnum.Skip(1).First().Int, 2);
			Assert.AreEqual(valueTestEnum.Skip(1).First().Float, 5f);
			Assert.AreEqual(valueTestEnum.Skip(1).First().Decimal, (decimal)6.1);
			Assert.AreEqual(valueTestEnum.Skip(1).First().String, "Test2");
			Assert.AreEqual(valueTestEnum.Skip(1).First().Date, new DateTime(2020, 1, 1));
			Assert.AreEqual(valueTestEnum.Skip(1).First().Nul, true);
		}

		[TestMethod]
		public void NonQueryTest() {
			dbCon.NonQuery("INSERT INTO tTest (Int, String) VALUES(@Int, @String)", new { Int = 5000, String = "NonQuery" });

			Assert.AreEqual(dbCon.Scalar<int>("tTest", "Int", new { Int = 5000 }), 5000);
			Assert.AreEqual(dbCon.Scalar<string>("tTest", "String", new { Int = 5000 }), "NonQuery"); ;

			dbCon.Delete("tTest", new { Int = 5000 });
		}

		[TestMethod]
		public void InsertTest() {
			dbCon.Insert("tTest", new {
				Int = 3,
				TinyInt = 4,
				String = "InsertTest",
				Date = new DateTime(1999, 1, 1)
			});

			var reader = dbCon.Reader("SELECT * FROM tTest WHERE Int = @Int", new {
				Int = 3
			});

			foreach (var rec in reader) {
				Assert.AreEqual(rec["Int"], 3);
				Assert.AreEqual(rec["TinyInt"], (byte)4);
				Assert.AreEqual(rec["String"], "InsertTest");
				Assert.AreEqual(rec["Date"], new DateTime(1999, 1, 1));
			};

			dbCon.Delete("tTest", new { Int = 3, String = "InsertTest" });
		}

		[TestMethod]
		public void InsertContainsIdTest() {
			_id = dbCon.InsertContainsIdentity("tIdTest", "ID", new {
				DATA = "Test",
			});

			Assert.AreEqual(dbCon.Scalar("tIdTest", "DATA", new { ID = _id }), "Test");

			dbCon.Delete("tIdTest", new { ID = _id });
		}

		[TestMethod]
		public void UpdateTest() {
			dbCon.Insert("tTest", new { Int = 9999, String = "UpdateTest" });

			int count = dbCon.Update("tTest", new { String = "Updated" }, new { Int = 9999 });

			Assert.AreEqual(dbCon.Scalar("tTest", "String", new { Int = 9999 }), "Updated");
			Assert.AreEqual(count, 1);

			dbCon.Delete("tTest", new { Int = 9999 });
		}

		[TestMethod]
		public void UpdateAndResults() {
			dbCon.Insert("tTest", new { Int = 9000, String = "UpdateTest" });

			var valueTestEnum = dbCon.UpdateAndResults("tTest", new { Int = 8000, String = "Updated" }, new { Int = 9000 })
				.Select(rec => {
					return new {
						OldInt = rec.GetInt32(0),
						OldString = rec.GetString(1),
						NewInt = rec.GetInt32(2),
						NewString = rec.GetString(3),
					};
				});

			var itm = valueTestEnum.First();

			Assert.AreEqual(itm.OldInt, 9000);
			Assert.AreEqual(itm.OldString, "UpdateTest");
			Assert.AreEqual(itm.NewInt, 8000);
			Assert.AreEqual(itm.NewString, "Updated");

			dbCon.Delete("tTest", new { Int = 8000 });
		}

		[TestMethod]
		public void DeleteTest() {
			dbCon.Insert("tTest", new { Int = 4000, String = "DeleteTarget" });
			int count = dbCon.Delete("tTest", new { Int = 4000 });
			Assert.AreEqual(dbCon.Exists("tTest", new { Int = 4000 }), false);
			Assert.AreEqual(count, 1);
		}

		[TestMethod]
		public void DeleteAndResultTest() {
			dbCon.Insert("tTest", new { Int = 3000, String = "DeleteTarget" });

			var result = dbCon.DeleteAndResults("tTest", new { Int = 3000 })
				.Select(rec => new { Int = rec["Int"], String = rec["String"] });

			var itm = result.First();

			Assert.AreEqual(itm.Int, 3000);
			Assert.AreEqual(itm.String, "DeleteTarget");
		}

		[TestMethod]
		public void TruncateTest() {
			for (int i = 0; i < 100; i++) {
				dbCon.Insert("tIdTest", new { DATA = i });
			}

			dbCon.Truncate("tIdTest");

			Assert.AreEqual(dbCon.Scalar("tIdTest", "count(*)"), 0);
		}

		[TestMethod]
		public void DropTest() {
			dbCon.NonQuery("CREATE TABLE tDropTest(Test int)");
			dbCon.Drop("tDropTest");
		}

		[TestMethod]
		public void ExistsTableTest() {
			Assert.AreEqual(dbCon.ExistsTable("tTest"), true);
			Assert.AreEqual(dbCon.ExistsTable("nothing"), false);
		}

		[TestMethod]
		public void ExistsTest() {
			Assert.AreEqual(dbCon.Exists("tTest", new { Int = 1 }), true);
			Assert.AreEqual(dbCon.Exists("tTest", new { Int = 5 }), false);
			Assert.AreEqual(dbCon.ExistsSql("SELECT * FROM tTest WHERE Int = @Int", new { Int = 1 }), true);
			Assert.AreEqual(dbCon.ExistsSql("SELECT * FROM tTest WHERE Int = @Int", new { Int = 5 }), false);
		}

		[TestMethod]
		public void SingleTest() {
			var list = dbCon.Single<string>("tTest", "String");

			Assert.AreEqual(list.First(), "Test");
			Assert.AreEqual(list.Skip(1).First(), "Test2");
		}

		[TestMethod]
		public void PairTest() {
			var dic = dbCon.Pair<int, string>("tTest", "Int", "String");

			Assert.AreEqual(dic[1], "Test");
			Assert.AreEqual(dic[2], "Test2");
		}

		[TestMethod]
		public void SelectSqlTest() {
			var resultEnum = dbCon.SelectSql<TestClass>("SELECT Int, String FROM tTest");

			foreach (var itm in resultEnum) {
				if (itm.Int == 1) {
					Assert.AreEqual(itm.Int, 1);
					Assert.AreEqual(itm.String, "Test");
				} else {
					Assert.AreEqual(itm.Int, 2);
					Assert.AreEqual(itm.String, "Test2");
				}
			}
		}

		[TestMethod]
		public void SelectTest() {
			var resultEnum = dbCon.Select<TestClass>("tTest");

			foreach (var itm in resultEnum) {
				if (itm.Int == 1) {
					Assert.AreEqual(itm.Int, 1);
					Assert.AreEqual(itm.String, "Test");
				} else {
					Assert.AreEqual(itm.Int, 2);
					Assert.AreEqual(itm.String, "Test2");
				}
			}
		}
		#endregion

		[AssemblyCleanup()]
		public static void AssemblyCleanup() {
			dbCon.Dispose();
		}

		private class TestClass {
			public int Int { get; set; }
			public string String { get; set; }
		}
	}
}
