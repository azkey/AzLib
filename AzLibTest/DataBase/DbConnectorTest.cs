using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using AzLib.DataBase;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AzLibTest.DataBase {
	[TestClass]
	public class DbConnectorTest {
		public const string TEST_DB_NAME = "AzLibTest";
		public const string TEST_TABLE_NAME = "tTest";
		public static DbConnector dbCon;

		[TestInitialize()]
		public void DbConnectorTestInitialize() {
			dbCon.Insert(TEST_TABLE_NAME, new {
				Int = 123,
				String = "Test",
				Date = new DateTime(2012, 1, 1),
			});
			dbCon.Insert(TEST_TABLE_NAME, new {
				Int = 456,
				String = DBNull.Value,
				Date = DBNull.Value,
			});
		}

		[TestCleanup()]
		public void DbConnectorTestCleanup() {
			if (dbCon.ExistsTable(TEST_TABLE_NAME)) {
				dbCon.Truncate(TEST_TABLE_NAME);
			}
		}

		#region 接続系テスト
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
			//簡易型
			Assert.AreEqual(dbCon.Scalar(TEST_TABLE_NAME, "Int", new { Int = 123 }), 123);
			Assert.AreEqual(dbCon.Scalar(TEST_TABLE_NAME, "String", new { Int = 123 }), "Test");

			//スカラ値取得メソッドなのに複数の値を返却するケース
			Assert.AreEqual(dbCon.Scalar(TEST_TABLE_NAME, "Int"), 123);

			//NULLの検証
			Assert.AreEqual(dbCon.Scalar(TEST_TABLE_NAME, "Date", new { Int = 456 }), null);
		}

		[TestMethod]
		public void ScalarGenericTest() {
			Assert.AreEqual(dbCon.Scalar<int>(TEST_TABLE_NAME, "Int", new { Int = 123 }), 123);
			Assert.AreEqual(dbCon.Scalar<string>(TEST_TABLE_NAME, "String", new { Int = 123 }), "Test");

			//NULLの検証
			Assert.AreEqual(dbCon.Scalar<DateTime>(TEST_TABLE_NAME, "Date", new { Int = 456 }), default(DateTime));
			Assert.AreEqual(dbCon.Scalar<DateTime?>(TEST_TABLE_NAME, "Date", new { Int = 456 }), null);
		}

		[TestMethod]
		public void ReaderTest() {
			var definedColumnNames = new string[] { "ID", "Int", "String" };
			var columnNameList = new List<string>();
			var valueTestEnum = dbCon.Reader("SELECT * FROM " + TEST_TABLE_NAME + " ORDER BY Int")
				.Select((rec, count) => {
					if (count == 0) {
						for (int i = 0; i < rec.FieldCount; i++) {
							columnNameList.Add(rec.GetName(i));
						}
					}

					return Tuple.Create(new {
						ID = rec["ID"],
						Int = rec["Int"],
						String = rec["String"]
					}, count);
				});

			Assert.AreEqual(definedColumnNames
				.Zip(columnNameList, (def, col) => new { Def = def, Column = col })
				.Count(itm => itm.Def != itm.Column), 0);

			foreach (var tpl in valueTestEnum) {
				if (tpl.Item2 == 0) {
					Assert.AreEqual(tpl.Item1.Int, 123);
					Assert.AreEqual(tpl.Item1.String, "Test");
				} else if (tpl.Item2 == 1) {
					Assert.AreEqual(tpl.Item1.Int, 456);
					Assert.AreEqual(tpl.Item1.String, DBNull.Value);
				}
			}
		}

		[TestMethod]
		public void NonQueryTest() {
			dbCon.NonQuery("INSERT INTO " + TEST_TABLE_NAME + " (Int, String) VALUES(@Int, @String)", new { Int = 5000, String = "NonQuery" });

			Assert.AreEqual(dbCon.Scalar<int>("tTest", "Int", new { Int = 5000 }), 5000);
			Assert.AreEqual(dbCon.Scalar<string>("tTest", "String", new { Int = 5000 }), "NonQuery"); ;
		}

		[TestMethod]
		public void InsertTest() {
			dbCon.Insert(TEST_TABLE_NAME, new {
				Int = 789,
				String = "Insert",
				Date = new DateTime(2000, 1, 1),
			});

			Assert.AreEqual(dbCon.Scalar(TEST_TABLE_NAME, "Int", new { Int = 789 }), 789);
			Assert.AreEqual(dbCon.Scalar(TEST_TABLE_NAME, "String", new { Int = 789 }), "Insert");
			Assert.AreEqual(dbCon.Scalar(TEST_TABLE_NAME, "Date", new { Int = 789 }), new DateTime(2000, 1, 1));
		}

		[TestMethod]
		public void InsertContainsIdTest() {
			var id = dbCon.InsertContainsIdentity(TEST_TABLE_NAME, "ID", new {
				Int = 789,
				String = "Insert",
				Date = new DateTime(2000, 1, 1),
			});

			Assert.AreEqual(dbCon.Scalar(TEST_TABLE_NAME, "Int", new { ID = id }), 789);
		}

		[TestMethod]
		public void UpdateTest() {
			int count = dbCon.Update(TEST_TABLE_NAME, new { String = "Updated" }, new { Int = 123 });

			Assert.AreEqual(dbCon.Scalar(TEST_TABLE_NAME, "String", new { Int = 123 }), "Updated");
			Assert.AreEqual(count, 1);
		}

		[TestMethod]
		public void UpdateAndResults() {
			var valueTestEnum = dbCon.UpdateAndResults(TEST_TABLE_NAME, new { String = "Updated" })
				.Select((rec, count) => {
					return Tuple.Create(new {
						OldString = rec.IsDBNull(0) ? null : rec.GetString(0),
						NewString = rec.GetString(1),
					}, count);
				});

			foreach (var itm in valueTestEnum) {
				if (itm.Item2 == 0) {
					Assert.AreEqual(itm.Item1.OldString, "Test");
					Assert.AreEqual(itm.Item1.NewString, "Updated");
				} else if (itm.Item2 == 1) {
					Assert.AreEqual(itm.Item1.OldString, null);
					Assert.AreEqual(itm.Item1.NewString, "Updated");
				}
			}
		}

		[TestMethod]
		public void DeleteTest() {
			int count = dbCon.Delete(TEST_TABLE_NAME, new { Int = 123 });
			Assert.AreEqual(dbCon.Exists(TEST_TABLE_NAME, new { Int = 123 }), false);
			Assert.AreEqual(count, 1);

			int count2 = dbCon.Delete(TEST_TABLE_NAME);
			Assert.AreEqual(dbCon.Exists(TEST_TABLE_NAME, new { Int = 456 }), false);
			Assert.AreEqual(count, 1);
		}

		[TestMethod]
		public void DeleteAndResultTest() {
			var deleteTestEnum = dbCon.DeleteAndResults(TEST_TABLE_NAME)
				.Select((rec, count) => Tuple.Create(new { Int = rec["Int"], String = rec["String"] }, count));

			foreach (var tpl in deleteTestEnum) {
				if (tpl.Item2 == 0) {
					Assert.AreEqual(tpl.Item1.Int, 123);
					Assert.AreEqual(tpl.Item1.String, "Test");
				} else if (tpl.Item2 == 1) {
					Assert.AreEqual(tpl.Item1.Int, 456);
					Assert.AreEqual(tpl.Item1.String, DBNull.Value);
				}
			}
		}

		[TestMethod]
		public void TruncateTest() {
			dbCon.Truncate(TEST_TABLE_NAME);

			Assert.AreEqual(dbCon.Exists(TEST_TABLE_NAME), false);
		}

		[TestMethod]
		public void DropTableTest() {
			dbCon.DropTable(TEST_TABLE_NAME);

			Assert.AreEqual(dbCon.ExistsTable(TEST_TABLE_NAME), false);
		}

		[TestMethod]
		public void DropDatabaseTest() {
			dbCon.NonQuery("USE master");
			dbCon.DropDatabase(TEST_DB_NAME);

			Assert.AreEqual(dbCon.ExistsDatabase(TEST_DB_NAME), false);
		}

		[TestMethod]
		public void ExistsTableTest() {
			Assert.AreEqual(dbCon.ExistsTable(TEST_TABLE_NAME), true);
			Assert.AreEqual(dbCon.ExistsTable("nothing"), false);
		}

		[TestMethod]
		public void ExistsTest() {
			Assert.AreEqual(dbCon.Exists(TEST_TABLE_NAME, new { Int = 123 }), true);
			Assert.AreEqual(dbCon.Exists(TEST_TABLE_NAME, new { Int = 321 }), false);
		}

		[TestMethod]
		public void SingleTest() {
			var list = dbCon.Single<string>(TEST_TABLE_NAME, "String");

			Assert.AreEqual(list[0], "Test");
			Assert.AreEqual(list[1], null);
		}

		[TestMethod]
		public void PairTest() {
			var dic = dbCon.Pair<int, string>(TEST_TABLE_NAME, "Int", "String");

			Assert.AreEqual(dic[123], "Test");
			Assert.AreEqual(dic[456], null);
		}

		[TestMethod]
		public void SelectTest() {
			var resultEnum = dbCon.Select<TestClass>("tTest");

			foreach (var itm in resultEnum) {
				if (itm.Int == 123) {
					Assert.AreEqual(itm.String, "Test");
				} else {
					Assert.AreEqual(itm.Int, 456);
					Assert.AreEqual(itm.String, null);
				}
			}
		}
		#endregion

		#region トランザクションテスト
		[TestMethod]
		public void TransactionTest() {

		}

		#endregion

		private class TestClass {
			public int Int { get; set; }
			public string String { get; set; }
		}
	}
}
