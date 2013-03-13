using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AzLib.DataBase;
using System.Configuration;
using System.Threading;

namespace AzLibTest {
	[TestClass]
	public class SettingManagerTest {
		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Add() {
			DbSettings.Manager.Add("Test", @"Server=.\SQLEXPRESS;Integrated Security=true;", "System.Data.SqlClient");

			Assert.AreEqual(DbSettings.Manager["Test"].ConnectionString, @"Server=.\SQLEXPRESS;Integrated Security=true;");

			DbSettings.Manager.Add("Test", @"Server=.\SQLEXPRESS;Integrated Security=true;", "System.Data.SqlClient");

		}
	}
}
