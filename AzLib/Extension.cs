using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AzLib.HelperClass;
using Microsoft.CSharp.RuntimeBinder;

namespace AzLib.Extension {
	public static class ReflectionExtension {
		private static Dictionary<Type, GetSiteCollection> _getSiteCollectionCache = new Dictionary<Type, GetSiteCollection>();

		/// <summary>
		/// プロパティ名から値を取得する
		/// </summary>
		/// <param name="obj">取得対象のオブジェクト</param>
		/// <param name="propertyName">プロパティ名</param>
		/// <returns>プロパティの値</returns>
		public static object GetPropertyValue(this object obj, string propertyName) {
			var type = obj.GetType();
			GetSiteCollection getSiteCollection = null;
			CallSite<Func<CallSite, object, object>> siteGet = null;

			//型キャッシュの存在確認
			if (_getSiteCollectionCache.TryGetValue(type, out getSiteCollection)) {
				siteGet = getSiteCollection[propertyName];

				//Getterキャッシュの存在確認
				if (siteGet != null) {
					return siteGet.Target(siteGet, obj);
				}
			}

			var argInfo = new CSharpArgumentInfo[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) };
			siteGet = CallSite<Func<CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(CSharpBinderFlags.None, propertyName, type, argInfo));

			if (getSiteCollection == null) {
				getSiteCollection = new GetSiteCollection(propertyName, siteGet);
				_getSiteCollectionCache.Add(type, getSiteCollection);
			} else {
				getSiteCollection.Add(propertyName, siteGet);
			}

			return siteGet.Target(siteGet, obj);
		}

		/// <summary>
		/// 指定したプロパティに値をセットする
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj"></param>
		/// <param name="propertyName"></param>
		/// <param name="value"></param>
		public static void SetPropertyValue(this object obj, string propertyName, object value) {
			var prop = obj.GetType().GetProperty(propertyName);

			prop.SetValue(obj, value, null);
		}

		/// <summary>
		/// プロパティ名と値を全て取得する
		/// </summary>
		/// <param name="obj">取得対象のオブジェクト</param>
		/// <returns>プロパティ名と値を含んだDictionary</returns>
		public static Dictionary<string, object> GetAllPropertyToDictionary(this object obj) {
			var propNames = obj.GetType().GetProperties().Select(prop => prop.Name);

			return propNames
				.Select(propName => new { PropName = propName, Value = obj.GetPropertyValue(propName) })
				.ToDictionary(propObj => propObj.PropName, propObj => propObj.Value);
		}

		/// <summary>
		/// プロパティ名と値を全て取得する
		/// </summary>
		/// <param name="obj">取得対象のオブジェクト</param>
		/// <returns>プロパティ名と値を含んだDictionary</returns>
		public static IEnumerable<Tuple<string, object>> GetAllPropertyEnumerator(this object obj) {
			var propNames = obj.GetType().GetProperties().Select(prop => prop.Name);

			return propNames
				.Select(propName => Tuple.Create(propName, obj.GetPropertyValue(propName)));
		}
	}
}
