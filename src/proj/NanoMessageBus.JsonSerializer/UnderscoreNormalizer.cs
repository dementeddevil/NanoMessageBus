namespace NanoMessageBus.Serialization
{
	using System.Text;

	public class UnderscoreNormalizer
	{
		public string Normalize(string value)
		{
			if (string.IsNullOrEmpty(value))
				return value;

			var builder = new StringBuilder(32);

			var hasUpper = false;
			var len = value.Length;
			for (var i = 0; i < len; i++)
			{
				var letter = value[i];
				if (char.IsUpper(letter))
				{
					if (hasUpper && (i + 1) < len && char.IsLower(value[i + 1]))
						builder.Append("_");

					letter = char.ToLower(letter);
					hasUpper = true;
				}

				builder.Append(letter);
			}

			return builder.ToString();
		}
	}
}