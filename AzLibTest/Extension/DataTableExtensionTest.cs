using System;
using System.Data;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AzLib.Extension;

namespace AzLibTest.Extension {
	[TestClass]
	public class DataTableExtensionTest {
		[TestMethod]
		public void AnyDataToDataTableTest() {
			var test = new TestClass[]{
				new TestClass(){ Int = 1, String = "Test1" },
				new TestClass(){ Int = 2, String = "Test2" },
			};

			var table = test.AnyDataToDataTable();

			Assert.AreEqual(table.Rows[0]["Int"], 1);
			Assert.AreEqual(table.Rows[0]["String"], "Test1");
			Assert.AreEqual(table.Rows[1]["Int"], 2);
			Assert.AreEqual(table.Rows[1]["String"], "Test2");
		}

		[TestMethod]
		public void DataTableToObjectTest() {
			var table = new DataTable();
			table.Columns.AddRange(new DataColumn[]{
				new DataColumn("Int",typeof(int)),
				new DataColumn("String",typeof(string)),
			});

			table.Rows.Add(1, "Test1");
			table.Rows.Add(2, "Test2");

			var test = table.ToObject<TestClass>().Select((obj, count) => Tuple.Create(obj, count));

			foreach (var tpl in test) {
				if (tpl.Item2 == 0) {
					Assert.AreEqual(tpl.Item1.Int, 1);
					Assert.AreEqual(tpl.Item1.String, "Test1");
				} else if (tpl.Item2 == 1) {
					Assert.AreEqual(tpl.Item1.Int, 2);
					Assert.AreEqual(tpl.Item1.String, "Test2");
				}
			}
		}

		private class TestClass {
			public int Int { get; set; }
			public string String { get; set; }
		}
	}
}
