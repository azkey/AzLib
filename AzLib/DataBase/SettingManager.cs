using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;
using AzLib.Properties;

namespace AzLib.DataBase {
	/// <summary>
	/// 接続設定管理クラス
	/// </summary>
	/// <remarks>
	/// <para>DbConnectorで使用する接続設定の管理を行います。</para>
	/// <para>本クラスはシングルトンオブジェクトです。Managerプロパティからアクセスを行って下さい。</para>
	/// </remarks>
	public partial class DbSettings : ICollection<ConnectionStringSettings> {
		#region メンバ変数
		/// <summary>
		/// 唯一のインスタンス
		/// </summary>
		private static DbSettings _instance;

		/// <summary>
		/// 接続設定コレクション
		/// </summary>
		private ConnectionStringSettingsCollection _settingsCollection = new ConnectionStringSettingsCollection();
		#endregion

		#region 静的プロパティ
		/// <summary>
		/// 接続設定管理プロパティ
		/// </summary>
		/// <remarks>
		/// <para>本プロパティから、接続設定のコレクションにアクセスを行います。</para>
		/// </remarks>
		/// <example>
		/// 使用例:
		/// <code>
		/// using AzLib.DataBase;
		/// 
		/// namespace Hoge {
		///		class Fuga {
		///			DbSettings.Manager.Add("TestSetting", @"Server=(local);Integrated Security=true;Initial Catalog=FooBarDb", "System.Data.SqlClient");
		///		}
		///	}
		/// </code>
		/// </example>
		public static DbSettings Manager {
			get {
				if (_instance == null) {
					_instance = new DbSettings();
				}
				return _instance;
			}
		}
		#endregion

		#region プロパティ
		/// <summary>
		/// 既定の接続設定
		/// </summary>
		/// <remarks>
		/// <para>デフォルトの接続設定です。DbConnectorをパラメータ無しでインスタンス化した場合、本設定が使用されます。</para>
		/// </remarks>
		public ConnectionStringSettings DefaultSettings { get; set; }

		/// <summary>
		/// 既定の接続文字列
		/// </summary>
		/// <remarks>
		/// <para>デフォルトの接続文字列です。DbConnectorをパラメータ無しでインスタンス化した場合、本接続文字列でDBへ接続されます。</para>
		/// </remarks>
		public string DefaultConnectionString {
			get {
				return DefaultSettings.ConnectionString;
			}
		}

		/// <summary>
		/// 既定のプロバイダ
		/// </summary>
		/// <remarks>
		/// <para>デフォルトのDBプロバイダです。</para>
		/// </remarks>
		public DbProviderFactory DefaultProviderFactory {
			get {
				return DbProviderFactories.GetFactory(DefaultSettings.ProviderName);
			}
		}
		#endregion

		#region インデクサ
		/// <summary>
		/// インデックスで接続設定を取得する
		/// </summary>
		/// <param name="index">インデックス</param>
		/// <returns>接続設定</returns>
		public ConnectionStringSettings this[int index] {
			get {
				return _settingsCollection[index];
			}
		}

		/// <summary>
		/// 設定名で接続設定を取得する
		/// </summary>
		/// <param name="settingName">接続設定名</param>
		/// <returns>接続設定</returns>
		public ConnectionStringSettings this[string settingName] {
			get {
				return _settingsCollection[settingName];
			}
		}
		#endregion

		#region メソッド
		/// <summary>
		/// 接続設定の生成
		/// </summary>
		/// <param name="settingName">設定名</param>
		/// <param name="connectionString">接続文字列</param>
		/// <param name="providerName">プロバイダ名</param>
		/// <returns>接続設定</returns>
		/// <remarks>
		/// <para>接続設定のインスタンスを生成します。</para>
		/// </remarks>
		public ConnectionStringSettings CreateConnectionSettings(string settingName, string connectionString, string providerName) {
			return new ConnectionStringSettings(settingName, connectionString, providerName);
		}

		/// <summary>
		/// SqlServerの接続設定を生成
		/// </summary>
		/// <param name="settingName">設定名</param>
		/// <param name="connectionString">接続文字列</param>
		/// <returns>接続設定</returns>
		/// <remarks>
		/// <para>SqlServerの接続設定のインスタンスを生成します。</para>
		/// </remarks>
		public ConnectionStringSettings CreateSqlServerSettings(string settingName, string connectionString) {
			return new ConnectionStringSettings(settingName, connectionString, "System.Data.SqlClient");
		}

		/// <summary>
		/// 接続設定の追加
		/// </summary>
		/// <param name="settingName">設定名</param>
		/// <param name="connectionString">接続文字列</param>
		/// <param name="providerName">プロバイダ名</param>
		/// <param name="isSetDefault">既定の接続に設定</param>
		/// <remarks>
		/// <para>接続設定を追加します。</para>
		/// </remarks>
		public void Add(string settingName, string connectionString, string providerName, bool isSetDefault = true) {
			if (this.Contains(settingName)) throw new ArgumentException(Resources.SettingNameAlreadyExistsException);

			var conSet = new ConnectionStringSettings(settingName, connectionString, providerName);
			_settingsCollection.Add(conSet);
			if (isSetDefault) DefaultSettings = conSet;
		}

		/// <summary>
		/// SqlServerの接続設定を追加
		/// </summary>
		/// <param name="settingName">設定名</param>
		/// <param name="connectionString">接続文字列</param>
		/// <param name="isSetDefault">既定の接続に設定</param>
		/// <remarks>
		/// <para>SqlServerの接続設定を追加します。</para>
		/// </remarks>
		public void AddSqlServer(string settingName, string connectionString, bool isSetDefault = true) {
			if (this.Contains(settingName)) throw new ArgumentException(Resources.SettingNameAlreadyExistsException);

			var conSet = new ConnectionStringSettings(settingName, connectionString, "System.Data.SqlClient");
			_settingsCollection.Add(conSet);
			if (isSetDefault) DefaultSettings = conSet;
		}

		/// <summary>
		/// 接続設定の存在確認
		/// </summary>
		/// <param name="settingName">接続設定名</param>
		/// <returns>判定結果</returns>
		/// <remarks>
		/// <para>接続設定の存在を設定名で確認します。</para>
		/// </remarks>
		public bool Contains(string settingName) {
			return _settingsCollection.Cast<ConnectionStringSettings>().Count(conSet => conSet.Name == settingName) != 0;
		}

		/// <summary>
		/// 接続設定の削除
		/// </summary>
		/// <param name="settingName">接続設定名</param>
		/// <returns>削除結果</returns>
		/// <remarks>接続設定の削除を設定名で行います。</remarks>
		public bool Remove(string settingName) {
			if (this.Contains(settingName)) {
				_settingsCollection.Remove(_settingsCollection[settingName]);
				return true;
			} else {
				return false;
			}
		}
		#endregion

		#region ICollection<ConnectionStringSettings>の実装
		public void Add(ConnectionStringSettings item) {
			_settingsCollection.Add(item);
		}

		public void Clear() {
			_settingsCollection.Clear();
		}

		public bool Contains(ConnectionStringSettings item) {
			return _settingsCollection.Cast<ConnectionStringSettings>().Contains(item);
		}

		public void CopyTo(ConnectionStringSettings[] array, int arrayIndex) {
			for (int i = arrayIndex; i < _settingsCollection.Count; i++) {
				array[i] = _settingsCollection[i - arrayIndex];
			}
		}

		public int Count {
			get {
				return _settingsCollection.Count;
			}
		}

		public bool IsReadOnly {
			get {
				return false;
			}
		}

		public bool Remove(ConnectionStringSettings item) {
			if (_settingsCollection.Cast<ConnectionStringSettings>().Contains(item)) {
				_settingsCollection.Remove(item);
				return true;
			} else {
				return false;
			}
		}

		public IEnumerator<ConnectionStringSettings> GetEnumerator() {
			foreach (ConnectionStringSettings set in _settingsCollection) {
				yield return set;
			}
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			foreach (ConnectionStringSettings set in _settingsCollection) {
				yield return set;
			}
		}
		#endregion
	}
}
