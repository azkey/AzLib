using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace AzLib.Extension {
	public static class DataTableExtension {
		public static DataTable AnyDataToDataTable<T>(this IEnumerable<T> source) where T : new() {
			var resultTable = new DataTable();
			var type = typeof(T);
			var propEnum = type.GetProperties().Select(prop => Tuple.Create(prop.Name, prop.PropertyType));

			resultTable.Columns.AddRange(propEnum.Select(tpl => new DataColumn(tpl.Item1, tpl.Item2)).ToArray());

			foreach (var itm in source) {
				resultTable.Rows.Add(itm.GetAllPropertyEnumerator().Select(tpl => tpl.Item2).ToArray());
			}

			return resultTable;
		}

		public static IEnumerable<T> ToObject<T>(this DataTable tbl) where T : new() {
			var propNames = typeof(T).GetProperties().Select(prop => prop.Name);

			foreach (DataRow row in tbl.Rows) {
				T obj = new T();

				foreach (var name in propNames) {
					obj.SetPropertyValue(name, row[name]);
				}

				yield return obj;
			}
		}
	}
}
