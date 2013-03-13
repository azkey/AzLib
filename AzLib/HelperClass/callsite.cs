using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace AzLib.HelperClass {
	internal class GetSiteCollection {
		private Dictionary<string, CallSite<Func<CallSite, object, object>>> _getSiteColletion = new Dictionary<string, CallSite<Func<CallSite, object, object>>>();

		internal GetSiteCollection(string propName, CallSite<Func<CallSite, object, object>> siteGet) {
			_getSiteColletion.Add(propName, siteGet);
		}

		public CallSite<Func<CallSite, object, object>> this[string propertyName] {
			get {
				CallSite<Func<CallSite, object, object>> siteGet = null;
				_getSiteColletion.TryGetValue(propertyName, out siteGet);

				return siteGet;
			}
		}

		public void Add(string propertyName, CallSite<Func<CallSite, object, object>> siteGet) {
			_getSiteColletion.Add(propertyName, siteGet);
		}
	}
}
