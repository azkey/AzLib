using System.Configuration;
using System.Data.Common;

namespace AzLib.DataBase {
	internal static class DbConnectorExtension {
		internal static DbProviderFactory GetProviderFactory(this ConnectionStringSettings setting) {
			return DbProviderFactories.GetFactory(setting.ProviderName);
		}

		internal static string GetPlaceHolder(this ConnectionStringSettings setting) {
			switch (setting.ProviderName) {
				case "System.Data.SqlClient":
					return "@";
				case "System.Data.OracleClient":
					return ":";
				default:
					return "@";
			}
		}
	}
}
