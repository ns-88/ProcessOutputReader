using System.Runtime.CompilerServices;

namespace ProcessOutputReader.Infrastructure
{
	internal static class Guard
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThrowIfNull<T>(T source, out T target, [CallerArgumentExpression("source")] string? paramName = null)
		{
			target = source ?? throw new ArgumentNullException(paramName);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThrowIfNull<T>(T source, [CallerArgumentExpression("source")] string? paramName = null)
		{
			if (source == null)
				throw new ArgumentNullException(paramName);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T ThrowIfNullRet<T>(T source, [CallerArgumentExpression("source")] string? paramName = null)
		{
			if (source == null)
				throw new ArgumentNullException(paramName);

			return source;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThrowIfEmptyString(string source, [CallerArgumentExpression("source")] string? paramName = null)
		{
			if (string.IsNullOrWhiteSpace(source))
				throw new ArgumentException(paramName);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ThrowIfEmptyStringRet(string source, [CallerArgumentExpression("source")] string? paramName = null)
		{
			if (string.IsNullOrWhiteSpace(source))
				throw new ArgumentException(paramName);

			return source;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThrowIfEmptyString(string source, out string target, [CallerArgumentExpression("source")] string? paramName = null)
		{
			if (string.IsNullOrWhiteSpace(source))
				throw new ArgumentException(paramName);

			target = source;
		}
	}
}