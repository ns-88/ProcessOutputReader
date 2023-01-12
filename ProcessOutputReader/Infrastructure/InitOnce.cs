namespace ProcessOutputReader.Infrastructure
{
	public class InitOnce<T> where T : class
	{
		#region Value
		private T? _value;
		public T Value
		{
			get => _value ?? throw new InvalidOperationException("Значение не инициализировано.");
			private set => _value = value;
		}
		#endregion

		public void Set(T value)
		{
			if (_value != null)
			{
				throw new InvalidOperationException("Значение было инициализировано ранее.");
			}

			Value = value;
		}

		public static implicit operator T(InitOnce<T> other)
		{
			return other.Value;
		}
	}
}