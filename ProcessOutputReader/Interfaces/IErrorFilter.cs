namespace ProcessOutputReader.Interfaces
{
	public interface IErrorFilter
	{
		bool Filter(string value);
	}
}