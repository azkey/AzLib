using System;
using System.Data;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AzLib.Extension;

namespace AzLibTest.Extension {
	[TestClass]
	public class CsvExtensionTest {
		private string testCsv = @"Header1,Header2,Header3
Test1,Test2,Test3
""Test1"",""Test2"",""Test3""
""Te
st1"",""Te""""st2"",Test3
,"""",
,"""""""",
";

		[TestMethod]
		public void CsvToEnumeratorTest() {
			var result = testCsv.CsvToEnumerator().Select((itm, count) => new { Itm = itm, Count = count });

			foreach (var itm in result) {
				if (itm.Count == 0) {
					Assert.AreEqual(itm.Itm[0], "Test1");
					Assert.AreEqual(itm.Itm[1], "Test2");
					Assert.AreEqual(itm.Itm[2], "Test3");
				} else if (itm.Count == 1) {
					Assert.AreEqual(itm.Itm[0], "Test1");
					Assert.AreEqual(itm.Itm[1], "Test2");
					Assert.AreEqual(itm.Itm[2], "Test3");
				} else if (itm.Count == 2) {
					Assert.AreEqual(itm.Itm[0], @"Te
st1");
					Assert.AreEqual(itm.Itm[1], "Te\"st2");
					Assert.AreEqual(itm.Itm[2], "Test3");
				} else if (itm.Count == 3) {
					Assert.AreEqual(itm.Itm[0], "");
					Assert.AreEqual(itm.Itm[1], "");
					Assert.AreEqual(itm.Itm[2], "");
				} else if (itm.Count == 4) {
					Assert.AreEqual(itm.Itm[0], "");
					Assert.AreEqual(itm.Itm[1], "\"");
					Assert.AreEqual(itm.Itm[2], "");
				}
			}
		}

		[TestMethod]
		public void CsvToDataDictionaryTest() {
			var result = testCsv.CsvToDictionary().Select((itm, count) => new { Itm = itm, Count = count });

			foreach (var itm in result) {
				if (itm.Count == 0) {
					Assert.AreEqual(itm.Itm["Header1"], "Test1");
					Assert.AreEqual(itm.Itm["Header2"], "Test2");
					Assert.AreEqual(itm.Itm["Header3"], "Test3");
				} else if (itm.Count == 1) {
					Assert.AreEqual(itm.Itm["Header1"], "Test1");
					Assert.AreEqual(itm.Itm["Header2"], "Test2");
					Assert.AreEqual(itm.Itm["Header3"], "Test3");
				} else if (itm.Count == 2) {
					Assert.AreEqual(itm.Itm["Header1"], @"Te
st1");
					Assert.AreEqual(itm.Itm["Header2"], "Te\"st2");
					Assert.AreEqual(itm.Itm["Header3"], "Test3");
				} else if (itm.Count == 3) {
					Assert.AreEqual(itm.Itm["Header1"], "");
					Assert.AreEqual(itm.Itm["Header2"], "");
					Assert.AreEqual(itm.Itm["Header3"], "");
				} else if (itm.Count == 4) {
					Assert.AreEqual(itm.Itm["Header1"], "");
					Assert.AreEqual(itm.Itm["Header2"], "\"");
					Assert.AreEqual(itm.Itm["Header3"], "");
				}
			}
		}

		[TestMethod]
		public void CsvToDataTableTest() {
			var resultTable = testCsv.CsvToDataTable();

			for (int i = 0; i < resultTable.Rows.Count; i++) {
				if (i == 0) {
					Assert.AreEqual(resultTable.Rows[i]["Header1"], "Test1");
					Assert.AreEqual(resultTable.Rows[i]["Header2"], "Test2");
					Assert.AreEqual(resultTable.Rows[i]["Header3"], "Test3");
				} else if (i == 1) {
					Assert.AreEqual(resultTable.Rows[i]["Header1"], "Test1");
					Assert.AreEqual(resultTable.Rows[i]["Header2"], "Test2");
					Assert.AreEqual(resultTable.Rows[i]["Header3"], "Test3");
				} else if (i == 2) {
					Assert.AreEqual(resultTable.Rows[i]["Header1"], @"Te
st1");
					Assert.AreEqual(resultTable.Rows[i]["Header2"], "Te\"st2");
					Assert.AreEqual(resultTable.Rows[i]["Header3"], "Test3");
				} else if (i == 3) {
					Assert.AreEqual(resultTable.Rows[i]["Header1"], "");
					Assert.AreEqual(resultTable.Rows[i]["Header2"], "");
					Assert.AreEqual(resultTable.Rows[i]["Header3"], "");
				} else if (i == 4) {
					Assert.AreEqual(resultTable.Rows[i]["Header1"], "");
					Assert.AreEqual(resultTable.Rows[i]["Header2"], "\"");
					Assert.AreEqual(resultTable.Rows[i]["Header3"], "");
				}
			}
		}
	}
}
