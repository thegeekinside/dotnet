#if XUNIT_NULLABLE
#nullable enable

using System.Diagnostics.CodeAnalysis;
#endif

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit.Sdk;

namespace Xunit.Internal
{
	internal static class AssertHelper
	{
#if XUNIT_NULLABLE
		static ConcurrentDictionary<Type, Dictionary<string, Func<object?, object?>>> gettersByType = new ConcurrentDictionary<Type, Dictionary<string, Func<object?, object?>>>();
#else
		static ConcurrentDictionary<Type, Dictionary<string, Func<object, object>>> gettersByType = new ConcurrentDictionary<Type, Dictionary<string, Func<object, object>>>();
#endif

#if XUNIT_NULLABLE
		static Dictionary<string, Func<object?, object?>> GetGettersForType(Type type)
#else
		static Dictionary<string, Func<object, object>> GetGettersForType(Type type)
#endif
		{
			return gettersByType.GetOrAdd(type, _type =>
			{
				var fieldGetters =
					_type
						.GetRuntimeFields()
						.Where(f => f.IsPublic && !f.IsStatic)
#if XUNIT_NULLABLE
						.Select(f => new { name = f.Name, getter = (Func<object?, object?>)f.GetValue });
#else
						.Select(f => new { name = f.Name, getter = (Func<object, object>)f.GetValue });
#endif

				var propertyGetters =
					_type
						.GetRuntimeProperties()
						.Where(p => p.CanRead)
#if XUNIT_NULLABLE
						.Select(p => new { name = p.Name, getter = (Func<object?, object?>)p.GetValue });
#else
						.Select(p => new { name = p.Name, getter = (Func<object, object>)p.GetValue });
#endif

				return
					fieldGetters
						.Concat(propertyGetters)
						.ToDictionary(g => g.name, g => g.getter);
			});
		}

#if XUNIT_NULLABLE
		public static EquivalentException? VerifyEquivalence(
			object? expected,
			object? actual,
#else
		public static EquivalentException VerifyEquivalence(
			object expected,
			object actual,
#endif
			bool strict)
		{
			return VerifyEquivalence(expected, actual, strict, string.Empty, new HashSet<object>(), new HashSet<object>());
		}

#if XUNIT_NULLABLE
		static EquivalentException? VerifyEquivalence(
			object? expected,
			object? actual,
#else
		public static EquivalentException VerifyEquivalence(
			object expected,
			object actual,
#endif
			bool strict,
			string prefix,
			HashSet<object> expectedRefs,
			HashSet<object> actualRefs)
		{
			// Check for null equivalence
			if (expected == null)
				return
					actual == null
						? null
						: EquivalentException.ForMemberValueMismatch(expected, actual, prefix);

			if (actual == null)
				return EquivalentException.ForMemberValueMismatch(expected, actual, prefix);

			// Check for identical references
			if (object.ReferenceEquals(expected, actual))
				return null;

			// Prevent circular references
			if (expectedRefs.Contains(expected))
				return EquivalentException.ForCircularReference($"{nameof(expected)}.{prefix}");

			if (actualRefs.Contains(actual))
				return EquivalentException.ForCircularReference($"{nameof(actual)}.{prefix}");

			expectedRefs.Add(expected);
			actualRefs.Add(actual);

			try
			{
				var expectedType = expected.GetType();
				var expectedTypeInfo = expectedType.GetTypeInfo();
				var actualType = actual.GetType();
				var actualTypeInfo = actualType.GetTypeInfo();

				// KeyValuePair<,> should compare key and value for equivalence; since KVP is a value
				// type, we want to avoid just calling Equals(), since it's not a good enough test of
				// equivalence for us (we want equivalence tests for both key and value).
				if (expectedTypeInfo.IsGenericType &&
					expectedTypeInfo.GetGenericTypeDefinition() == typeof(KeyValuePair<,>) &&
					actualTypeInfo.IsGenericType &&
					actualTypeInfo.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
				{
					var prefixDot = prefix == string.Empty ? string.Empty : prefix + ".";

					var expectedKeyGetter = expectedType.GetRuntimeProperty("Key");
					var actualKeyGetter = actualType.GetRuntimeProperty("Key");
					if (expectedKeyGetter == null || actualKeyGetter == null)
						throw new InvalidOperationException("Catastrophic failure: Could not find KeyValuePair<,>.Key via reflection");

					var expectedKey = expectedKeyGetter.GetValue(expected);
					var actualKey = actualKeyGetter.GetValue(actual);

					var keyResult = VerifyEquivalence(expectedKey, actualKey, strict, prefixDot + "Key", expectedRefs, actualRefs);
					if (keyResult != null)
						return keyResult;

					var expectedValueGetter = expectedType.GetRuntimeProperty("Value");
					var actualValueGetter = actualType.GetRuntimeProperty("Value");
					if (expectedValueGetter == null || actualValueGetter == null)
						throw new InvalidOperationException("Catastrophic failure: Could not find KeyValuePair<,>.Value via reflection");

					var expectedValue = expectedValueGetter.GetValue(expected);
					var actualValue = actualValueGetter.GetValue(actual);

					return VerifyEquivalence(expectedValue, actualValue, strict, prefixDot + "Value", expectedRefs, actualRefs);
				}

				// Value types and strings should just fall back to their Equals implementation
				if (expectedTypeInfo.IsValueType || expectedType == typeof(string))
					return
						expected.Equals(actual)
							? null
							: EquivalentException.ForMemberValueMismatch(expected, actual, prefix);

				// Enumerables? Check equivalence of individual members
				var enumerableExpected = expected as IEnumerable;
				var enumerableActual = actual as IEnumerable;
				if (enumerableExpected != null && enumerableActual != null)
					return VerifyEquivalenceEnumerable(enumerableExpected, enumerableActual, strict, prefix, expectedRefs, actualRefs);

				return VerifyEquivalenceReference(expected, actual, strict, prefix, expectedRefs, actualRefs);
			}
			finally
			{
				expectedRefs.Remove(expected);
				actualRefs.Remove(actual);
			}
		}

#if XUNIT_NULLABLE
		static EquivalentException? VerifyEquivalenceEnumerable(
#else
		static EquivalentException VerifyEquivalenceEnumerable(
#endif
			IEnumerable expected,
			IEnumerable actual,
			bool strict,
			string prefix,
			HashSet<object> expectedRefs,
			HashSet<object> actualRefs)
		{
#if XUNIT_NULLABLE
			var expectedValues = expected.Cast<object?>().ToList();
			var actualValues = actual.Cast<object?>().ToList();
#else
			var expectedValues = expected.Cast<object>().ToList();
			var actualValues = actual.Cast<object>().ToList();
#endif
			var actualOriginalValues = actualValues.ToList();

			// Walk the list of expected values, and look for actual values that are equivalent
			foreach (var expectedValue in expectedValues)
			{
				var actualIdx = 0;
				for (; actualIdx < actualValues.Count; ++actualIdx)
					if (VerifyEquivalence(expectedValue, actualValues[actualIdx], strict, "", expectedRefs, actualRefs) == null)
						break;

				if (actualIdx == actualValues.Count)
					return EquivalentException.ForMissingCollectionValue(expectedValue, actualOriginalValues, prefix);

				actualValues.RemoveAt(actualIdx);
			}

			if (strict && actualValues.Count != 0)
				return EquivalentException.ForExtraCollectionValue(expectedValues, actualOriginalValues, actualValues, prefix);

			return null;
		}

#if XUNIT_NULLABLE
		static EquivalentException? VerifyEquivalenceReference(
#else
		static EquivalentException VerifyEquivalenceReference(
#endif
			object expected,
			object actual,
			bool strict,
			string prefix,
			HashSet<object> expectedRefs,
			HashSet<object> actualRefs)
		{
			var prefixDot = prefix == string.Empty ? string.Empty : prefix + ".";

			// Enumerate over public instance fields and properties and validate equivalence
			var expectedGetters = GetGettersForType(expected.GetType());
			var actualGetters = GetGettersForType(actual.GetType());

			if (strict && expectedGetters.Count != actualGetters.Count)
				return EquivalentException.ForMemberListMismatch(expectedGetters.Keys, actualGetters.Keys, prefixDot);

			foreach (var kvp in expectedGetters)
			{
#if XUNIT_NULLABLE
				Func<object?, object?>? actualGetter;
#else
				Func<object, object> actualGetter;
#endif

				if (!actualGetters.TryGetValue(kvp.Key, out actualGetter))
					return EquivalentException.ForMemberListMismatch(expectedGetters.Keys, actualGetters.Keys, prefixDot);

				var expectedMemberValue = kvp.Value(expected);
				var actualMemberValue = actualGetter(actual);

				var ex = VerifyEquivalence(expectedMemberValue, actualMemberValue, strict, prefixDot + kvp.Key, expectedRefs, actualRefs);
				if (ex != null)
					return ex;
			}

			return null;
		}
	}
}
