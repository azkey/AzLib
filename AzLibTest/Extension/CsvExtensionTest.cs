using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AzLib.Extension;

namespace AzLibTest.Extension {
	[TestClass]
	public class CsvExtensionTest {
		[TestMethod]
		public void CsvAsEnumerableTest() {
			string testCsv = @"Test1,Test2,Test3
""Test1"",""Test2"",""Test3""
""Te
st1"",""Te""""st2"",Test3
,"""",
,"""""""",
";

			var result = testCsv.CsvAsEnumerable().Select((itm, count) => new { Itm = itm, Count = count });

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
	}
}
