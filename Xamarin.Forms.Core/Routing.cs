using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Xamarin.Forms
{
	public static class Routing
	{
		static int s_routeCount = 0;

#if NETSTANDARD1_0
		static Dictionary<string, RouteFactory> s_routes = new Dictionary<string, RouteFactory>();
#else
		static Dictionary<string, RouteFactory> s_routes = new Dictionary<string, RouteFactory>(StringComparer.InvariantCultureIgnoreCase);
#endif

		internal const string ImplicitPrefix = "IMPL_";

		internal static string GenerateImplicitRoute (string source)
		{
			if (source.StartsWith(ImplicitPrefix, StringComparison.Ordinal))
				return source;
			return ImplicitPrefix + source;
		}

		internal static bool CompareRoutes(string route, string compare, out bool isImplicit)
		{
			if (isImplicit = route.StartsWith(ImplicitPrefix, StringComparison.Ordinal))
				route = route.Substring(ImplicitPrefix.Length);

			if (compare.StartsWith(ImplicitPrefix, StringComparison.Ordinal))
				throw new Exception();

			return route == compare;
		}

		public static readonly BindableProperty RouteProperty =
			BindableProperty.CreateAttached("Route", typeof(string), typeof(Routing), null, 
				defaultValueCreator: CreateDefaultRoute);

		static object CreateDefaultRoute(BindableObject bindable)
		{
			return bindable.GetType().Name + ++s_routeCount;
		}

		public static Element GetOrCreateContent(string route)
		{
			Element result = null;
#if NETSTANDARD1_0
			route = route.ToLowerInvariant();
#endif
			if (s_routes.TryGetValue(route, out var content))
				result = content.GetOrCreate();

			if (result == null)
			{
				// okay maybe its a type, we'll try that just to be nice to the user
				var type = Type.GetType(route);
				if (type != null)
					result = Activator.CreateInstance(type) as Element;
			}

			if (result != null)
				SetRoute(result, route);

			return result;
		}

		public static string GetRoute(Element obj)
		{
			return (string)obj.GetValue(RouteProperty);
		}

		public static void RegisterRoute(string route, RouteFactory factory)
		{
			if (!ValidateRoute(route))
				throw new ArgumentException("Route contain invalid letters");

#if NETSTANDARD1_0
			route = route.ToLowerInvariant();
#endif
			s_routes[route] = factory;
		}

		public static void RegisterRoute(string route, Type type)
		{
			if (!ValidateRoute(route))
				throw new ArgumentException("Route contain invalid letters");

#if NETSTANDARD1_0
			route = route.ToLowerInvariant();
#endif
			s_routes[route] = new TypeRouteFactory(type);
		}

		public static void SetRoute(Element obj, string value)
		{
			obj.SetValue(RouteProperty, value);
		}

		static bool ValidateRoute(string route)
			=> new Regex(@"^[-a-zA-Z0-9@:%._\+~#=]{1,100}$").IsMatch(route);

		class TypeRouteFactory : RouteFactory
		{
			readonly Type _type;

			public TypeRouteFactory(Type type)
			{
				_type = type;
			}

			public override Element GetOrCreate()
			{
				return (Element)Activator.CreateInstance(_type);
			}
		}
	}
}