using System;
using System.Text;

namespace GMPCalc
{
	public class utils
	{
		public static string pretty(string a)
		{
			pretty(ref a);
			return a;
		}

		public static string pretty(ref string a)
		{
			if (a == "")
				return "";

			int v;
			bool neg = a.Substring(0, 1) == "-";

			if (neg || Int32.TryParse(a.Substring(0, 1), out v))
			{
				string whole;
				string fract = "";

				if (a.Contains("."))
				{
					whole = a.Substring(0, a.IndexOf('.'));
					fract = a.Substring(a.IndexOf('.'));
				}
				else
					whole = a;

				if (whole.Length > 3)
				{
					if (neg)
						whole = whole.Remove(0, 1);

					int m3 = whole.Length % 3;

					StringBuilder sb = new StringBuilder((m3 > 0) ? (whole.Substring(0, m3) + ",") : "", whole.Length + whole.Length / 3 + 64);

					for (int i = m3; i < whole.Length; i += 3)
					{
						sb.Append(whole.Substring(i, 3) + ",");
					}

					sb.Remove(sb.Length - 1, 1);

					if (neg)
						sb.Insert(0, '-');

					whole = sb.ToString();

				}

				a = whole + fract;

			}

			return "";

		}
	}
}
