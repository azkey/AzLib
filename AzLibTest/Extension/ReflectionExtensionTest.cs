using System;
using System.Diagnostics;
using System.Linq;
using AzLib.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AzLibTest {
	[TestClass]
	public class ReflectionExtensionTest {
		[TestMethod]
		public void GetPropertyValueTest() {
			var test = new TestClass();
			var anon = new { Str = "Test2", Int = 9000, Date = new DateTime(1900, 1, 1) };

			test.StringProperty = "Test";
			test.IntProperty = 9999;
			test.DateTimeProperty = new DateTime(2000, 1, 1);

			var stringResult = (string)test.GetPropertyValue("StringProperty");
			var intResult = (int)test.GetPropertyValue("IntProperty");
			var dateResult = (DateTime)test.GetPropertyValue("DateTimeProperty");

			var anonStr = (string)anon.GetPropertyValue("Str");
			var anonInt = (int)anon.GetPropertyValue("Int");
			var anonDateTime = (DateTime)anon.GetPropertyValue("Date");

			Assert.AreEqual<string>(stringResult, "Test");
			Assert.AreEqual<int>(intResult, 9999);
			Assert.AreEqual<DateTime>(dateResult, new DateTime(2000, 1, 1));

			Assert.AreEqual<string>(anonStr, "Test2");
			Assert.AreEqual<int>(anonInt, 9000);
			Assert.AreEqual<DateTime>(anonDateTime, new DateTime(1900, 1, 1));
		}

		[TestMethod]
		public void GetAllPropertyToDictionary() {
			var test = new TestClass();
			var anon = new { Str = "Test2", Int = 9000, Date = new DateTime(1900, 1, 1) };

			test.StringProperty = "Test";
			test.IntProperty = 9999;
			test.DateTimeProperty = new DateTime(2000, 1, 1);

			var dic1 = test.GetAllPropertyToDictionary();
			var dic2 = anon.GetAllPropertyToDictionary();

			Assert.AreEqual<string>((string)dic1["StringProperty"], "Test");
			Assert.AreEqual<int>((int)dic1["IntProperty"], 9999);
			Assert.AreEqual<DateTime>((DateTime)dic1["DateTimeProperty"], new DateTime(2000, 1, 1));

			Assert.AreEqual<string>((string)dic2["Str"], "Test2");
			Assert.AreEqual<int>((int)dic2["Int"], 9000);
			Assert.AreEqual<DateTime>((DateTime)dic2["Date"], new DateTime(1900, 1, 1));
		}

		[TestMethod]
		public void GetAllPropertyEnumeratorTest() {
			var test = new TestClass();
			var anon = new { Str = "Test2", Int = 9000, Date = new DateTime(1900, 1, 1) };

			test.StringProperty = "Test";
			test.IntProperty = 9999;
			test.DateTimeProperty = new DateTime(2000, 1, 1);

			var enum1 = test.GetAllPropertyEnumerator();
			var enum2 = anon.GetAllPropertyEnumerator();

			Assert.AreEqual<string>((string)enum1.First().Item2, "Test");
			Assert.AreEqual<int>((int)enum1.Skip(1).First().Item2, 9999);
			Assert.AreEqual<DateTime>((DateTime)enum1.Skip(2).First().Item2, new DateTime(2000, 1, 1));

			Assert.AreEqual<string>((string)enum2.First().Item2, "Test2");
			Assert.AreEqual<int>((int)enum2.Skip(1).First().Item2, 9000);
			Assert.AreEqual<DateTime>((DateTime)enum2.Skip(2).First().Item2, new DateTime(1900, 1, 1));
		}

		[TestMethod]
		public void SetPropertyValueTest() {
			var stopWatch = new Stopwatch();
			var test = new TestClass();

			stopWatch.Restart();
			for (int i = 0; i < 10000; i++) {
				test.SetPropertyValue("StringProperty", "Test");
				test.SetPropertyValue("IntProperty", 12345);
				test.SetPropertyValue("DateTimeProperty", new DateTime(2000, 1, 1));
			}
			stopWatch.Stop();

			stopWatch.Restart();
			for (int i = 0; i < 10000; i++) {
				test.GetType().GetProperty("StringProperty").SetValue(test, "Test");
				test.GetType().GetProperty("IntProperty").SetValue(test, 12345);
				test.GetType().GetProperty("DateTimeProperty").SetValue(test, new DateTime(2000, 1, 1));
			}
			stopWatch.Stop();

			stopWatch.Restart();
			for (int i = 0; i < 10000; i++) {
				test.SetPropertyValue("StringProperty", "Test");
				test.SetPropertyValue("IntProperty", 12345);
				test.SetPropertyValue("DateTimeProperty", new DateTime(2000, 1, 1));
			}
			stopWatch.Stop();

			Assert.AreEqual(test.StringProperty, "Test");
			Assert.AreEqual(test.IntProperty, 12345);
			Assert.AreEqual(test.DateTimeProperty, new DateTime(2000, 1, 1));
		}
	}

	public class TestClass {
		public string StringProperty { get; set; }
		public int IntProperty { get; set; }
		public DateTime DateTimeProperty { get; set; }
	}
}
