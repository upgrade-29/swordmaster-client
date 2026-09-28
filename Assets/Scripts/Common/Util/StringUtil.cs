using System.Text.RegularExpressions;
using UnityEngine;

public static class StringUtil
{
	public static bool IsOverSizeANSI(string text, int maxSize)
	{
		char[] charArray = text.ToCharArray();
		int length = 0;
		foreach (char x in charArray)
		{
			if (IsEnglish(x) || IsNumeric(x))
			{
				length += 1;
			}
			else
			{
				length += 2;
			}
		}
		
		if (length > maxSize)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public static bool ValidateTextLength(string str, int minLength, int maxLength)
	{
		if (IsOverSizeANSI(str, minLength) == false)
		{
			return true;
		}

		if (IsOverSizeANSI(str, maxLength))
		{
			return true;
		}

		return false;
	}

	public static string ExtractNumber(string text)
	{
		string strTmp = Regex.Replace(text, "[^0-9]", string.Empty);
		return strTmp;
	}
	
	public static string RemoveNumber(string text)
	{
		string strTmp = Regex.Replace(text, @"[\d-]", string.Empty);
		return strTmp;
	}
	
	public static string RemoveAsteriskSymbol(string text)
	{
		string strTmp = Regex.Replace(text, @"[.]|[+]|[-]|[%]|[,]|[[]|[]]|[(]|[)]|[\n]|[*]|[:]/g", "");
		return strTmp;
	}
	
	public static string RemoveHtmlTags(string text)
	{
		string strTmp = Regex.Replace(text, @"<.*?>", "");
		return strTmp;
	}

	public static string ColoringText(Color32 color, string str)
	{
		return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{str}</color>";
	}
	
	public static bool IsEnglish(char ch)
	{
		if ((0x61 <= ch && ch <= 0x7A) // 대문자
		    || (0x41 <= ch && ch <= 0x5A)) // 소문자
		{
			return true;
		}
		else
		{
			return false;
		}

	}

	public static bool IsNumeric(char ch)
	{
		if (0x30 <= ch && ch <= 0x39)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public static bool IsIncludedSpaceChar(string str)
	{
		if (str.Contains(" "))
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public static bool IsIncludedLineFeed(string str)
	{
		if (str.Contains("\n"))
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public static string NumberMeasure(int value)
	{
		string resultValue = value.ToStringCached();
		char addNumberChar = 'Z'; // K, M, B 붙일 단위
		if (resultValue.Length < 4) // 1천 이하면 그냥 사용
		{
			return resultValue;
		}
		else if (resultValue.Length < 7) // 1천 <= X < 100만(K)
		{
			addNumberChar = 'K';
		}
		else if (resultValue.Length < 10) // 100만 <= X < 10억(M)
		{
			addNumberChar = 'M';
		}
		else if (resultValue.Length < 13) // 10억 <= X < 21.4억(B)    <--- int 범위 제한
		{
			addNumberChar = 'B';
		}

		if (resultValue.Length % 3 == 0) // 100k
		{
			resultValue = resultValue[..3] + addNumberChar;
		}
		else if (resultValue.Length % 3 == 1) // 1.00k
		{
			resultValue = $"{resultValue[0]}.{resultValue[1]}{resultValue[2]}{addNumberChar}";
		}
		else // result_value.Length % 3 == 2 // 10.0k
		{
			resultValue = $"{resultValue[0]}{resultValue[1]}.{resultValue[2]}{addNumberChar}";
		}

		return resultValue;
	}
}