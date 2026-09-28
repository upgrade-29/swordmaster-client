using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
	
	public static string ExtractNumber(string _text)
	{
		string strTmp = Regex.Replace(_text, "[^0-9]", string.Empty);
		return strTmp;
	}
	
	public static string RemoveNumber(string _text)
	{
		string strTmp = Regex.Replace(_text, @"[\d-]", string.Empty);
		return strTmp;
	}
	
	public static string RemoveAsteriskSymbol(string _text)
	{
		string strTmp = Regex.Replace(_text, @"[.]|[+]|[-]|[%]|[,]|[[]|[]]|[(]|[)]|[\n]|[*]|[:]/g", "");
		return strTmp;
	}
	
	public static string RemoveHtmlTags(string _text)
	{
		string strTmp = Regex.Replace(_text, @"<.*?>", "");
		return strTmp;
	}
	
	public static string GetStringToEncodingType(string _msg, Encoding _toType)
	{
		byte[] defaultBytes = Encoding.Default.GetBytes(_msg);
		byte[] utf8Bytes = Encoding.Convert(Encoding.Default, _toType, defaultBytes);
		string resultString = _toType.GetString(utf8Bytes);
		return resultString;
	}
	
	public static string ColoringText(Color32 color, string str)
	{
		return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{str}</color>";
	}
	
	public static bool IsEnglish(char ch)
	{
		if ((0x61 <= ch && ch <= 0x7A) //대문자
		    || (0x41 <= ch && ch <= 0x5A))
		{
			//소문자
			return true;
		}
		else
		{
			return false;
		}

	}
	
	public static bool IsKorean(char ch, bool isAllowInitial)
	{
		if ((0xAC00 <= ch && ch <= 0xD7A3))
		{
			// 한글완성형			
			return true;
		}

		if (isAllowInitial)
		{
			if ((0x1100 <= ch && ch <= 0x11FF) // Hangul Jamo
			    || (0x3131 <= ch && ch <= 0x318E))
			{
				//Hangul Compatibility Jamo
				return true;
			}
		}

		return false;
	}
	
	public static bool IsJapanese(char ch)
	{
		if ((0x3040 <= ch && ch <= 0x309F) // 히라가나
		    || (0x30A0 <= ch && ch <= 0x30FF) // 카타카나
		    || (0x4E00 <= ch && ch <= 0x9FBF) // 간지
		    || 0x3005 == ch)
		{
			// 々
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public static bool IsChinese(char ch)
	{
		if ((ch >= 0x4E00 && ch <= 0x9FFF) || // CJK Unified Ideographs
		    (ch >= 0x3400 && ch <= 0x4DB5) || // CJK Unified Ideographs Extension A			
		    (ch >= 0x2E80 && ch <= 0x2EFF) || // CJK Radicals Supplement or Etc.
		    (ch >= 0xF900 && ch <= 0xFA6A))
		{
			// CJK Compatibility Ideographs
			//(ch >= 0x20000 && ch <= 0x2CEAF) || 
			//(ch >= 0xF2800 && ch <= 0x2FA1F)
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public static bool IsThai(string ch)
	{
		char[] textArray = ch.ToCharArray();

		if (textArray.Length <= 0)
		{
			return false;
		}

		for (int i = 0; i < textArray.Length; i++)
		{
			if (textArray[i] >= 0x0E01 && textArray[i] <= 0x0E5B)
			{
				return true;
			}
		}

		return false;
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
	
	public static bool IsBasicLatin(char ch)
	{
		if (0x0020 <= ch && ch <= 0x007F)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public static bool IsValidSpeech(string text)
	{
		char[] charArray = text.ToCharArray();
		foreach (char x in charArray)
		{
			if (IsEnglish(x)
			    || IsKorean(x, true)
			    || IsJapanese(x)
			    || IsChinese(x)
			    || IsNumeric(x)
			    || IsBasicLatin(x))
			{
			}
			else
			{
				return true;
			}
		}
		
		return false;
	}
	
	public static bool IsVaildStr(string strText)
	{
		char[] charArray = strText.ToCharArray();
		foreach (char x in charArray)
		{
			if (IsEnglish(x)
			    || IsKorean(x, false)
			    || IsJapanese(x)
			    || IsChinese(x)
			    || IsNumeric(x))
			{
			}
			else
			{
				return false;
			}
		}

		return true;
	}
	
	public static bool IsIncludedSpaceChar(string _sInfo)
	{
		if (_sInfo.Contains(" "))
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public static bool IsIncludedLineFeed(string _string)
	{
		if (_string.Contains("\n"))
		{
			return true;
		}
		else
		{
			return false;
		}
	}
	
	public static string NumberMeasure(int param_value)
	{
		string result_value = param_value.ToStringCached();
		char addNumberChar = 'Z'; // K, M, B 붙일 단위
		if (result_value.Length < 4) // 1천 이하면 그냥 사용
		{
			return result_value;
		}
		else if (result_value.Length < 7) // 1천 <= X < 100만    (K)
		{
			addNumberChar = 'K';
		}
		else if (result_value.Length < 10) // 100만 <= X < 10억      (M)
		{
			addNumberChar = 'M';
		}
		else if (result_value.Length < 13) // 10억 <= X < 21.4억      (B)    <--- int 범위 제한
		{
			addNumberChar = 'B';
		}

		if (result_value.Length % 3 == 0) // 100k
		{
			result_value = result_value.Substring(0, 3) + addNumberChar;
		}
		else if (result_value.Length % 3 == 1) // 1.00k
		{
			result_value = string.Format("{0}.{1}{2}{3}", result_value[0], result_value[1], result_value[2],
				addNumberChar);
		}
		else // result_value.Length % 3 == 2 // 10.0k
		{
			result_value = string.Format("{0}{1}.{2}{3}", result_value[0], result_value[1], result_value[2],
				addNumberChar);
		}

		return result_value;
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
	
	public static string UpperFirstChar(this string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return null;
		}

		return char.ToUpper(input[0]) + input.Substring(1).ToLower();
	}
}