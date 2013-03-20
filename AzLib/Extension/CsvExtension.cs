using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace AzLib.Extension {
	public static class CsvExtension {
		public static IEnumerable<string[]> CsvToEnumerator(this string csv, char delimiter = ',', bool trimItem = false) {
			var csvItemList = new List<string>();
			var csvItemBuilder = new StringBuilder();
			var inDoubleQuote = false;
			var chrArray = csv.ToCharArray();

			for (long i = 0; i < chrArray.LongLength; i++) {
				if (inDoubleQuote) {	//ダブルクォート内判定
					if (chrArray[i] == '"') {
						if (chrArray[i + 1] != '"') {
							inDoubleQuote = false;
							continue;
						} else if (chrArray[i + 1] == '"') {
							csvItemBuilder.Append(chrArray[i]);
							i++;
							continue;
						}
					} else {
						csvItemBuilder.Append(chrArray[i]);
						continue;
					}

				} else if (chrArray[i] == '"') {	//ダブルクォート開始判定
					inDoubleQuote = true;
					continue;

				} else if (chrArray[i] == delimiter) {	//アイテム区切り
					if (trimItem) {
						csvItemList.Add(csvItemBuilder.ToString().Trim());
					} else {
						csvItemList.Add(csvItemBuilder.ToString());
					}
					csvItemBuilder.Clear();
					continue;

				} else if (chrArray[i] == '\r') {	//改行判定
					if (trimItem) {
						csvItemList.Add(csvItemBuilder.ToString().Trim());
					} else {
						csvItemList.Add(csvItemBuilder.ToString());
					}

					yield return csvItemList.ToArray();

					csvItemList.Clear();
					csvItemBuilder.Clear();

					if (chrArray[i + 1] == '\n') {
						i++;
					}
					continue;

				} else if (chrArray[i] == '\n') {	//改行判定
					if (trimItem) {
						csvItemList.Add(csvItemBuilder.ToString().Trim());
					} else {
						csvItemList.Add(csvItemBuilder.ToString());
					}

					yield return csvItemList.ToArray();

					csvItemList.Clear();
					csvItemBuilder.Clear();

					if (chrArray[i + 1] == '\r') {
						i++;
					}
					continue;
				} else {
					csvItemBuilder.Append(chrArray[i]);
					continue;
				}
			}
		}

		//public static DataTable CsvToDataTable(object csv, char delimiter = ',', bool containHeader = true, bool trimItem = true) {
		//}

		public static void FillFromCsv(object csv, DataTable table, char delimiter = ',', bool containHeader = true, bool trimItem = true) {
		}
	}
}
