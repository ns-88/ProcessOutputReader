namespace ProcessOutputReader.Infrastructure
{
	internal class DisposableHelper
	{
		private readonly string _objectName;
		public bool IsDisposed { get; private set; }

		public DisposableHelper(string objectName)
		{
			_objectName = objectName;
		}

		public void SetIsDisposed()
		{
			IsDisposed = true;
		}

		public void ThrowIfDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(_objectName);
			}
		}
	}
}