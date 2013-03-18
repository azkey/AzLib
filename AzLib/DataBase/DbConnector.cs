using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using AzLib.Extension;
using AzLib.Properties;

namespace AzLib.DataBase {
	public class DbConnector : IDisposable {
		#region プロパティ
		#region 非公開
		private bool IsDisposed { get; set; }
		private ConnectionStringSettings Settings { get; set; }
		private DbConnection Connection { get; set; }
		private DbCommand Command { get; set; }
		private DbDataAdapter DataAdapter { get; set; }
		private DbTransaction Transaction { get; set; }
		private IsolationLevel? Isolation { get; set; }
		#endregion

		public ConnectionState State {
			get {
				return Connection.State;
			}
		}
		public bool UseTransaction { get; set; }
		public bool EndedTransaction { get; set; }
		#endregion

		#region コンストラクタ・デストラクタ
		public DbConnector(IsolationLevel? isolationLevel = IsolationLevel.ReadCommitted, bool isOpenConnection = true) {
			this.Initialize(DbSettings.Manager.DefaultSettings, isolationLevel, isOpenConnection);
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="settings">接続設定</param>
		/// <param name="isolationLevel">トランザクション分離レベル</param>
		/// <param name="isOpenConnection">同時接続フラグ</param>
		public DbConnector(ConnectionStringSettings settings, IsolationLevel? isolationLevel = IsolationLevel.ReadCommitted, bool isOpenConnection = true) {
			this.Initialize(settings, isolationLevel, isOpenConnection);
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="settingName">設定名</param>
		/// <param name="isolationLevel">トランザクション分離レベル</param>
		/// <param name="isOpenConnection">同時接続フラグ</param>
		public DbConnector(string settingName, IsolationLevel? isolationLevel = IsolationLevel.ReadCommitted, bool isOpenConnection = true) {
			this.Initialize(DbSettings.Manager[settingName], isolationLevel, isOpenConnection);
		}

		/// <summary>
		/// デストラクタ
		/// </summary>
		~DbConnector() {
			Dispose(false);
		}
		#endregion

		#region メソッド
		#region 接続操作メソッド
		/// <summary>
		/// 接続の開始
		/// </summary>
		/// <param name="isolationLevel">分離レベル</param>
		/// <returns>接続の成否</returns>
		public bool Open() {
			if (!this.IsDisposed) {
				this.Connection.Open();
			} else {
				throw new ObjectDisposedException(Resources.ObjectAlreadyDisposedException);
			}

			if (UseTransaction) {
				this.Transaction = this.Connection.BeginTransaction(this.Isolation.Value);
			}

			return this.State == ConnectionState.Open;
		}

		/// <summary>
		/// 接続の終了(Disposeと同等)
		/// </summary>
		/// <remarks>
		/// <para>接続を終了します。一度終了した接続を、再度オープンする事は出来ません。</para>
		/// <para>using構文を使用する場合、本メソッドを呼び出す必要はありません。</para>
		/// </remarks>
		public void Close() {
			if (this.IsDisposed) return;

			this.Dispose();
		}

		/// <summary>
		/// トランザクションのコミット
		/// </summary>
		/// <remarks>
		/// <para>トランザクションをコミットします。</para>
		/// <para>トランザクションを本メソッドによってコミットしなかった場合、Dispose時にコミットされます。</para>
		/// </remarks>
		public void Commit() {
			try {
				this.Transaction.Commit();
				this.EndedTransaction = true;
			} catch {
				this.Transaction.Rollback();
				this.EndedTransaction = true;
			}
		}

		/// <summary>
		/// トランザクションのロールバック
		/// </summary>
		/// <remarks>
		/// <para>トランザクションをロールバックします。</para>
		/// </remarks>
		public void Rollback() {
			this.Transaction.Rollback();
			this.EndedTransaction = true;
		}
		#endregion

		#region クエリ発行メソッド
		/// <summary>
		/// スカラ値の取得
		/// </summary>
		/// <typeparam name="T">取得値のデータ型</typeparam>
		/// <param name="tableName">テーブル名</param>
		/// <param name="columnName">カラム名</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>スカラ値</returns>
		/// <remarks>
		/// <para>単一の値を取得します。</para>
		/// <para>null非許容型を型パラメータに使用し、返却値がnullだった場合、default(T)を返します。</para>
		/// </remarks>
		public T Scalar<T>(string tableName, string columnName, object parameters = null) {
			var val = this.Scalar(tableName, columnName, parameters);
			return val == null ? default(T) : (T)val;
		}

		/// <summary>
		/// スカラ値の取得
		/// </summary>
		/// <typeparam name="T">取得値のデータ型</typeparam>
		/// <param name="sql">SQLクエリ</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>スカラ値</returns>
		/// <remarks>
		/// <para>単一の値を取得します。</para>
		/// <para>null非許容型を型パラメータに使用し、返却値がnullだった場合、default(T)を返します。</para>
		/// <para>複数の値が返却されるクエリを発行した場合、他の値を全て無視し、最初に取得した値のみを返します。</para>
		/// </remarks>
		public T Scalar<T>(string sql, object parameters = null) {
			var val = this.Scalar(sql, parameters);
			return val == null ? default(T) : (T)val;
		}

		/// <summary>
		/// スカラ値の取得
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		/// <param name="columnName">カラム名</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>スカラ値</returns>
		/// <remarks>
		/// <para>単一の値を取得します。</para>
		/// <para>返却値がnullだった場合、DBNullではなく通常のnullを返します。</para>
		/// </remarks>
		public object Scalar(string tableName, string columnName, object parameters = null) {
			var selectQuery = string.Format(Sql.SelectQueryFormat, columnName, tableName);

			if (parameters != null) {
				selectQuery += string.Format(Sql.WhereQueryFormat, AnonymousTypeToWhereString(parameters));
			}

			return this.Scalar(selectQuery, parameters);
		}

		/// <summary>
		/// スカラ値を返却するクエリの発行
		/// </summary>
		/// <param name="sql">SQLクエリ</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>スカラ値</returns>
		/// <remarks>
		/// <para>単一の値を取得します。</para>
		/// <para>返却値がnullだった場合、DBNullではなく通常のnullを返します。</para>
		/// <para>複数の値が返却されるクエリを発行した場合、他の値を全て無視し、最初に取得した値のみを返します。</para>
		/// </remarks>
		public object Scalar(string sql, object parameters = null) {
			InitializeCommand(sql);

			if (parameters != null) {
				this.Command.Parameters.AddRange(this.AnonymousTypeToNormalParameters(parameters));
			}

			var val = this.Command.ExecuteScalar();

			return val == DBNull.Value ? null : val;
		}

		/// <summary>
		/// 複数行を返却するクエリの発行
		/// </summary>
		/// <param name="sql">SQLクエリ</param>
		/// <param name="behavior">条件パラメータ</param>
		/// <returns>IDataRecordの列挙</returns>
		public IEnumerable<IDataRecord> Reader(string sql, CommandBehavior behavior = CommandBehavior.Default) {
			return this.Reader(sql, null, behavior);
		}

		/// <summary>
		/// 複数行を返却するクエリの発行
		/// </summary>
		/// <param name="sql">SQLクエリ</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <param name="behavior">DataReaderのオプション</param>
		/// <returns>IDataRecordの列挙</returns>
		public IEnumerable<IDataRecord> Reader(string sql, object parameters, CommandBehavior behavior = CommandBehavior.Default) {
			InitializeCommand(sql);

			if (parameters != null) {
				this.Command.Parameters.AddRange(this.AnonymousTypeToNormalParameters(parameters));
			}

			using (var reader = Command.ExecuteReader(behavior)) {
				while (reader.Read()) {
					yield return reader;
				}
			}
		}

		/// <summary>
		/// 返却値がないクエリの発行
		/// </summary>
		/// <param name="sql">SQLクエリ</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>影響を与えた行数</returns>
		public int NonQuery(string sql, object parameters = null) {
			InitializeCommand(sql);

			if (parameters != null) {
				this.Command.Parameters.AddRange(this.AnonymousTypeToNormalParameters(parameters));
			}

			return this.Command.ExecuteNonQuery();
		}

		public DataTable CreateDataTable(string tableName, object parameters = null) {
		}

		/// <summary>
		/// DataTableへの充填
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>充填されたDataTable</returns>
		public DataTable Fill(string tableName, object parameters = null) {
			var sql = string.Format(Sql.SelectQueryFormat, tableName);

			if (parameters != null) {
				sql += AnonymousTypeToWhereString(parameters);
			}

			return this.FillSql(sql, parameters);
		}

		/// <summary>
		/// DataTableへの充填
		/// </summary>
		/// <param name="sql">SQLクエリ</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>充填されたDataTable</returns>
		public DataTable FillSql(string sql, object parameters = null) {
			var table = new DataTable();
			InitializeCommand(sql);

			this.Command.Parameters.AddRange(this.AnonymousTypeToNormalParameters(parameters));
			DataAdapter.SelectCommand = this.Command;

			DataAdapter.Fill(table);

			return table;
		}

		/// <summary>
		/// INSERTクエリの発行
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		/// <param name="values">挿入値</param>
		public void Insert(string tableName, object values) {
			var insertQuery = string.Format(Sql.InsertQueryFormat, tableName, AnonymousTypeToInsertColumnString(values), AnonymousTypeToInsertValueString(values));

			this.NonQuery(insertQuery, values);
		}

		/// <summary>
		/// ID列を含んだINSERTクエリの発行
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		/// <param name="values">挿入値</param>
		public int InsertContainsIdentity(string tableName, string identityColumn, object values) {
			var insertQuery = string.Format(Sql.InsertQueryFormat, tableName, string.Format(Sql.InsertedOutputFormat, identityColumn), AnonymousTypeToInsertValueString(values));

			return this.Scalar<int>(insertQuery, values);
		}

		/// <summary>
		/// UPDATEクエリの発行
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		/// <param name="values">更新値</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>更新した行数</returns>
		public int Update(string tableName, object values, object parameters = null) {
			var updateQuery = string.Format(Sql.UpdateQueryFormat, tableName, AnonymousTypeToUpdateValueString(values));

			if (parameters != null) {
				updateQuery += string.Format(Sql.WhereQueryFormat, AnonymousTypeToWhereString(parameters));
			}

			InitializeCommand(updateQuery);

			if (parameters != null) {
				this.Command.Parameters.AddRange(this.AnonymousTypeToNormalParameters(parameters));
				this.Command.Parameters.AddRange(this.AnonymousTypeToUpdateValueParameters(values));
			}

			return this.Command.ExecuteNonQuery();
		}

		/// <summary>
		/// UPDATEクエリの発行し、変更対象となったデータを得る
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		/// <param name="values">更新値</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>更新したデータ</returns>
		public IEnumerable<IDataReader> UpdateAndResults(string tableName, object values, object parameters = null) {
			var output = string.Format(Sql.UpdatedOutputFormat, AnonymousTypeToUpdateValueString(values), AnonymousTypeToOutputDeletedString(values), AnonymousTypeToOutputInsertedString(values));
			var updateQuery = string.Format(Sql.UpdateQueryFormat, tableName, output);

			if (parameters != null) {
				updateQuery += string.Format(Sql.WhereQueryFormat, AnonymousTypeToWhereString(parameters));
			}

			InitializeCommand(updateQuery);

			if (parameters != null) {
				this.Command.Parameters.AddRange(this.AnonymousTypeToNormalParameters(parameters));
				this.Command.Parameters.AddRange(this.AnonymousTypeToUpdateValueParameters(values));
			}

			using (var reader = this.Command.ExecuteReader()) {
				while (reader.Read()) {
					yield return reader;
				}
			}
		}

		/// <summary>
		/// DELETEクエリの発行
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>削除された行数</returns>
		public int Delete(string tableName, object parameters = null) {
			var deleteQuery = string.Format(Sql.DeleteQueryFormat, tableName, "");

			if (parameters != null) {
				deleteQuery += string.Format(Sql.WhereQueryFormat, AnonymousTypeToWhereString(parameters));
			}

			return this.NonQuery(deleteQuery, parameters);
		}

		/// <summary>
		/// DELETEクエリの発行
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>削除された行数</returns>
		public IEnumerable<IDataRecord> DeleteAndResults(string tableName, object parameters = null) {
			var deleteQuery = string.Format(Sql.DeleteQueryFormat, tableName, Sql.DeletedOutput);

			if (parameters != null) {
				deleteQuery += string.Format(Sql.WhereQueryFormat, AnonymousTypeToWhereString(parameters));
			}

			return this.Reader(deleteQuery, parameters);
		}

		/// <summary>
		/// テーブルデータの高速削除(ロールバック不可)
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		public void Truncate(string tableName) {
			this.NonQuery(string.Format(Sql.TruncateFormat, tableName));
		}

		/// <summary>
		/// テーブルの削除
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		public void Drop(string tableName) {
			this.NonQuery(string.Format(Sql.DropFormat, tableName));
		}

		/// <summary>
		/// テーブルの存在を確認
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		public bool ExistsTable(string tableName) {
			switch (this.Settings.ProviderName) {
				case "System.Data.SqlClient":
					return this.ExistsSql("SELECT * FROM sysobjects WHERE xtype = 'u' AND name = @name", new { name = tableName });

				default:
					throw new NotSupportedException();
			}
		}

		/// <summary>
		/// 対象データの存在を確認
		/// </summary>
		/// <param name="tableName">テーブル名</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>確認結果</returns>
		public bool Exists(string tableName, object parameters = null) {
			var sql = string.Format(Sql.SelectQueryFormat, "*", tableName);

			if (parameters != null) {
				sql += string.Format(Sql.WhereQueryFormat, AnonymousTypeToWhereString(parameters));
			}

			return this.ExistsSql(sql, parameters);
		}

		/// <summary>
		/// 対象データの存在を確認
		/// </summary>
		/// <param name="sql">SQLクエリ</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>確認結果</returns>
		public bool ExistsSql(string sql, object parameters = null) {
			InitializeCommand(sql);

			if (parameters != null) {
				this.Command.Parameters.AddRange(this.AnonymousTypeToNormalParameters(parameters));
			}

			using (var reader = this.Command.ExecuteReader()) {
				return reader.HasRows;
			}
		}

		/// <summary>
		/// 1列のデータを対象にListを出力
		/// </summary>
		/// <typeparam name="T">データ型</typeparam>
		/// <param name="tableName">テーブル名</param>
		/// <param name="column">列名</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>Listオブジェクト</returns>
		public List<T> Single<T>(string tableName, string column, object parameters = null) {
			var sql = string.Format(Sql.SelectQueryFormat, column, tableName);

			if (parameters != null) {
				sql += string.Format(Sql.WhereQueryFormat, AnonymousTypeToWhereString(parameters));
			}

			return this.Single<T>(sql, parameters);
		}

		/// <summary>
		/// 1列のデータを対象にListを出力
		/// </summary>
		/// <typeparam name="T">データ型</typeparam>
		/// <param name="sql">SQLクエリ</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>Listオブジェクト</returns>
		public List<T> Single<T>(string sql, object parameters = null) {
			return this.Reader(sql, parameters).Select(rec => rec.IsDBNull(0) ? default(T) : (T)rec.GetValue(0)).ToList();
		}

		/// <summary>
		/// 2列のデータを対象にDictionaryを出力
		/// </summary>
		/// <typeparam name="TKey">キーの型</typeparam>
		/// <typeparam name="TValue">値の型</typeparam>
		/// <param name="tableName">テーブル名</param>
		/// <param name="keyColumn">キーとする列名</param>
		/// <param name="valueColumn">値とする列名</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>Dictionaryオブジェクト</returns>
		public Dictionary<TKey, TValue> Pair<TKey, TValue>(string tableName, string keyColumn, string valueColumn, object parameters = null) {
			var sql = string.Format(Sql.SelectQueryFormat, string.Format("{0}\n\t,\t{1}", keyColumn, valueColumn), tableName);

			if (parameters != null) {
				sql += string.Format(Sql.WhereQueryFormat, AnonymousTypeToWhereString(parameters));
			}

			return this.Pair<TKey, TValue>(sql, parameters);
		}

		/// <summary>
		/// 2列のデータを対象にDictionaryを出力
		/// </summary>
		/// <typeparam name="TKey">キーの型</typeparam>
		/// <typeparam name="TValue">値の型</typeparam>
		/// <param name="sql">SQLクエリ</param>
		/// <param name="parameters">条件パラメータ</param>
		/// <returns>Dictionaryオブジェクト</returns>
		public Dictionary<TKey, TValue> Pair<TKey, TValue>(string sql, object parameters = null) {
			return this.Reader(sql, parameters)
				.Select(rec => new { Key = rec.IsDBNull(0) ? default(TKey) : (TKey)rec.GetValue(0), Value = rec.IsDBNull(1) ? default(TValue) : (TValue)rec.GetValue(1) })
				.ToDictionary(pair => pair.Key, pair => pair.Value);
		}

		/// <summary>
		/// 指定した型に射影
		/// </summary>
		/// <typeparam name="T">射影対象型</typeparam>
		/// <param name="sql">SQLクエリ</param>
		/// <param name="parameters">パラメータ</param>
		/// <returns>射影された列挙</returns>
		public IEnumerable<T> Select<T>(string tableName, object parameters = null) where T : new() {
			var propNames = typeof(T).GetProperties().Select(prop => prop.Name);
			var columns = string.Join("\t,\t", propNames);
			var sql = string.Format(Sql.SelectQueryFormat, columns, tableName);
			var resultEnum = this.Reader(sql, parameters);


			foreach (var rec in resultEnum) {
				var returnObj = new T();

				for (int i = 0; i < rec.FieldCount; i++) {
					var name = rec.GetName(i);

					if (propNames.Contains(name)) {
						returnObj.SetPropertyValue(name, rec.GetValue(i));
					}
				}

				yield return returnObj;
			}
		}

		/// <summary>
		/// 指定した型に射影
		/// </summary>
		/// <typeparam name="T">射影対象型</typeparam>
		/// <param name="sql">SQLクエリ</param>
		/// <param name="parameters">パラメータ</param>
		/// <returns>射影された列挙</returns>
		public IEnumerable<T> SelectSql<T>(string sql, object parameters = null) where T : new() {
			var propNames = typeof(T).GetProperties().Select(prop => prop.Name);
			var resultEnum = this.Reader(sql, parameters);


			foreach (var rec in resultEnum) {
				var returnObj = new T();

				for (int i = 0; i < rec.FieldCount; i++) {
					var name = rec.GetName(i);

					if (propNames.Contains(name)) {
						returnObj.SetPropertyValue(name, rec.GetValue(i));
					}
				}

				yield return returnObj;
			}
		}
		#endregion

		#region IDisposeの実装
		/// <summary>
		/// リソースの解放
		/// </summary>
		public void Dispose() {
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// リソースの解放
		/// </summary>
		/// <param name="disposing">明確な呼び出しフラグ</param>
		protected virtual void Dispose(bool disposing) {
			if (this.IsDisposed) return;

			if (disposing) {
				//トランザクションの後始末
				if (this.UseTransaction && !this.EndedTransaction) this.Transaction.Commit();

				//マネージドリソースの解放
				if (this.Transaction != null) this.Transaction.Dispose();
				if (this.Command != null) this.Command.Dispose();
				if (this.DataAdapter != null) this.DataAdapter.Dispose();
			}

			//アンマネージドリソースの解放
			this.Connection.Close();
			this.Connection.Dispose();

			this.IsDisposed = true;
		}
		#endregion

		#region ヘルパ
		/// <summary>
		/// 初期化処理
		/// </summary>
		/// <param name="settings">接続設定</param>
		/// <param name="isolationLevel">トランザクション分離レベル</param>
		private void Initialize(ConnectionStringSettings settings, IsolationLevel? isolationLevel, bool isOpenConnection) {
			//設定の確定
			this.Settings = settings == null ? DbSettings.Manager.DefaultSettings : settings;

			//各DB関連プロパティの初期化
			this.Connection = this.Settings.GetProviderFactory().CreateConnection();
			this.Connection.ConnectionString = this.Settings.ConnectionString;
			this.DataAdapter = this.Settings.GetProviderFactory().CreateDataAdapter();

			//トランザクションの初期化
			if (isolationLevel.HasValue) {
				this.UseTransaction = true;
				this.EndedTransaction = false;
				this.Isolation = isolationLevel.Value;
			}

			if (isOpenConnection) {
				this.Open();
			}
		}

		/// <summary>
		/// DbCommandオブジェクトの初期化
		/// </summary>
		/// <param name="sql">SQLクエリ</param>
		private void InitializeCommand(string sql) {
			this.Command = this.Connection.CreateCommand();

			if (this.UseTransaction && !this.EndedTransaction) {
				this.Command.Transaction = this.Transaction;
			}

			this.Command.CommandText = sql;
		}

		#region SQLクエリパーツへの変換
		/// <summary>
		/// UPDATEクエリのSET部文字列の取得
		/// </summary>
		/// <param name="obj">値を含んだ匿名型</param>
		/// <returns>取得結果</returns>
		private string AnonymousTypeToUpdateValueString(object obj) {
			return string.Join("\t,\t", obj.GetAllPropertyEnumerator()
				.Select(param => string.Format("{0} = {1}_UPDATE_{2}\n", param.Item1, this.Settings.GetPlaceHolder(), param.Item1)));
		}

		/// <summary>
		/// INSERTクエリのカラム指定部分文字列の取得
		/// </summary>
		/// <param name="obj">値を含んだ匿名型</param>
		/// <returns>取得結果</returns>
		private string AnonymousTypeToInsertColumnString(object obj) {
			return "(" + string.Join(", ", obj.GetAllPropertyEnumerator()
				.Select(param => param.Item1)) + ")";
		}

		/// <summary>
		/// INSERTクエリの値指定部分文字列の取得
		/// </summary>
		/// <param name="obj">値を含んだ匿名型</param>
		/// <returns>取得結果</returns>
		private string AnonymousTypeToInsertValueString(object obj) {
			return string.Join(", ", obj.GetAllPropertyEnumerator()
				.Select(param => this.Settings.GetPlaceHolder() + param.Item1));
		}

		/// <summary>
		/// 各SQLクエリのWHERE部分文字列の取得
		/// </summary>
		/// <param name="obj">条件を含んだ匿名型</param>
		/// <returns>取得結果</returns>
		/// <remarks>
		/// <para>「値 = DBNull.Value」とした条件は「値 IS NULL」へ変換されます。</para></remarks>
		private string AnonymousTypeToWhereString(object obj) {
			return string.Join("\tAND\t", obj.GetAllPropertyEnumerator()
				.Select(param => {
					if (param.Item2 == DBNull.Value) {
						return string.Format("{0} IS NULL\n", param.Item1);
					} else {
						return string.Format("{0} = {1}{2}\n", param.Item1, this.Settings.GetPlaceHolder(), param.Item1);
					}
				}));
		}

		private string AnonymousTypeToOutputInsertedString(object obj) {
			return string.Join("\t,\t", obj.GetAllPropertyEnumerator()
				.Select(param => string.Format("INSERTED.{0} AS '{1}'\n", param.Item1, "New" + param.Item1)));
		}

		private string AnonymousTypeToOutputDeletedString(object obj) {
			return string.Join("\t,\t", obj.GetAllPropertyEnumerator()
				.Select(param => string.Format("DELETED.{0} AS '{1}'\n", param.Item1, "Old" + param.Item1)));
		}
		#endregion

		#region パラメータへの変換
		/// <summary>
		/// 各SQLクエリのWHERE部のパラメータオブジェクトを得る
		/// </summary>
		/// <param name="obj">条件を含んだ匿名型</param>
		/// <returns>条件パラメータオブジェクト</returns>
		private DbParameter[] AnonymousTypeToNormalParameters(object obj) {
			return obj.GetAllPropertyEnumerator()
				.Where(tpl => tpl.Item2 != DBNull.Value)
				.Select(tpl => {
					var param = this.Command.CreateParameter();
					param.ParameterName = this.Settings.GetPlaceHolder() + tpl.Item1;
					param.Value = tpl.Item2;
					return param;
				}).ToArray();
		}

		/// <summary>
		/// UPDATEクエリの値パラメータオブジェクトを得る
		/// </summary>
		/// <param name="obj">値を含んだ匿名型</param>
		/// <returns>値パラメータオブジェクト</returns>
		private DbParameter[] AnonymousTypeToUpdateValueParameters(object obj) {
			return obj.GetAllPropertyEnumerator()
				.Select(tpl => {
					var param = this.Command.CreateParameter();
					param.ParameterName = string.Format(@"{0}_UPDATE_{1}", this.Settings.GetPlaceHolder(), tpl.Item1);
					param.Value = tpl.Item2;
					return param;
				}).ToArray();
		}
		#endregion
		#endregion
		#endregion
	}
}
