using System;
using System.Collections.Generic;
using System.Reflection;

namespace ATTM2X
{
	public static class Extensions
	{
		/// <summary>
		/// Returns all fields declared in type and its base
		/// </summary>
		public static IEnumerable<FieldInfo> GetFields(this Type type)
		{
			Type t = type;
			do
			{
				TypeInfo ti = t.GetTypeInfo();
				foreach (var p in ti.DeclaredFields)
					yield return p;
				t = ti.BaseType;
			} while (t != null);
		}

		/// <summary>
		/// Returns all properties declared in type and its base
		/// </summary>
		public static IEnumerable<PropertyInfo> GetProperties(this Type type)
		{
			Type t = type;
			do
			{
				TypeInfo ti = t.GetTypeInfo();
				foreach (var p in ti.DeclaredProperties)
					yield return p;
				t = ti.BaseType;
			} while (t != null);
		}
	}
}
