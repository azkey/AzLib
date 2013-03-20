using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace AzLib.Extension {
	public static class CsvExtension {
		public static IEnumerable<string[]> CsvToEnumerator(this string csv, bool containHeader = true, bool trimItem = false, char delimiter = ',') {
			if (string.IsNullOrWhiteSpace(csv)) yield break;

			var csvItemList = new List<string>();
			var csvItemBuilder = new StringBuilder();
			var inDoubleQuote = false;
			var chrArray = csv.ToCharArray();
			bool isHeader = containHeader;

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

				} else if (chrArray[i] == '\r' || chrArray[i] == '\n') {	//改行判定

					if (!isHeader) {
						if (trimItem) {
							csvItemList.Add(csvItemBuilder.ToString().Trim());
						} else {
							csvItemList.Add(csvItemBuilder.ToString());
						}

						yield return csvItemList.ToArray();
					}

					csvItemList.Clear();
					csvItemBuilder.Clear();

					if (chrArray.Length > i && (chrArray[i + 1] == '\n' || chrArray[i] == '\r')) {
						i++;
					}
					continue;

				} else {
					csvItemBuilder.Append(chrArray[i]);
					continue;
				}
			}

			if (csvItemList.Count != 0 || string.IsNullOrWhiteSpace(csvItemBuilder.ToString())) {
				if (!isHeader) {
					if (trimItem) {
						csvItemList.Add(csvItemBuilder.ToString().Trim());
					} else {
						csvItemList.Add(csvItemBuilder.ToString());
					}

					yield return csvItemList.ToArray();
				}
			}
		}

		public static IEnumerable<Dictionary<string, string>> CsvToDictionary(this string csv, bool containHeader = true, bool trimItem = false, char delimiter = ',') {
			if (string.IsNullOrWhiteSpace(csv)) yield break;

			string[] header = null;
			IEnumerable<string[]> csvBodyEnum = null;

			if (containHeader) {
				header = csv.CsvToEnumerator(false).Take(1).FirstOrDefault().ToArray();
				csvBodyEnum = csv.CsvToEnumerator(containHeader);
			} else {
				header = Enumerable.Range(0, csvBodyEnum.FirstOrDefault().Length).Select(num => num.ToString()).ToArray();
				csvBodyEnum = csv.CsvToEnumerator(containHeader);
			}

			foreach (var csvRow in csvBodyEnum) {
				yield return csvRow.Zip(header, (itm, head) => new { Itm = itm, Header = head }).ToDictionary(obj => obj.Header, obj => obj.Itm);
			}
		}

		public static DataTable CsvToDataTable(this string csv, bool containHeader = true, bool trimItem = false, char delimiter = ',') {
			if (string.IsNullOrWhiteSpace(csv)) return null;

			string[] header = null;
			IEnumerable<string[]> csvBodyEnum = null;
			var resultTable = new DataTable();

			if (containHeader) {
				header = csv.CsvToEnumerator(false).Take(1).FirstOrDefault().ToArray();
				csvBodyEnum = csv.CsvToEnumerator(containHeader);
			} else {
				header = Enumerable.Range(0, csvBodyEnum.FirstOrDefault().Length).Select(num => num.ToString()).ToArray();
				csvBodyEnum = csv.CsvToEnumerator(containHeader);
			}

			resultTable.Columns.AddRange(header.Select(itm => new DataColumn(itm)).ToArray());

			foreach (var itm in csvBodyEnum) {
				resultTable.Rows.Add(itm);
			}

			return resultTable;
		}

		public static void FillFromCsv(object csv, DataTable table, char delimiter = ',', bool containHeader = true, bool trimItem = true) {
		}
	}
}
