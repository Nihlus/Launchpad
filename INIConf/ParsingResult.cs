namespace INIConf
{
	public class ParsingResult<T>
	{
		public bool IsSuccessful { get; }
		public T Result { get; }
	}
}
