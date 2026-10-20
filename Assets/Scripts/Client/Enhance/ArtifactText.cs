using System;
using System.Globalization;

// 아티팩트 등급과 능력치를 화면에 쓸 글자로 바꾼다. 강화 탭과 가챠 결과 팝업이 같은 표기를 쓴다
public static class ArtifactText
{
    public static string GetGradeName(ArtifactGrade grade)
    {
        string gradeName = string.Empty;
        switch (grade)
        {
            case ArtifactGrade.Common:
                gradeName = "일반";
                break;
            case ArtifactGrade.Rare:
                gradeName = "희귀";
                break;
            case ArtifactGrade.Epic:
                gradeName = "영웅";
                break;
            case ArtifactGrade.Legendary:
                gradeName = "전설";
                break;
        }

        return gradeName;
    }

    public static string GetStatName(StatType statType)
    {
        switch (statType)
        {
            case StatType.AttackSpeed:
                return "공격 속도";
            case StatType.MaxHp:
                return "최대 체력";
            case StatType.CritRate:
                return "치명타 확률";
            case StatType.Lifesteal:
                return "생명력 흡수";
        }
        return string.Empty;
    }

    // 체력은 정수로, 나머지는 비율이라 백분율로 쓴다
    public static string FormatStat(StatType statType, double value)
    {
        switch (statType)
        {
            case StatType.AttackSpeed:
            case StatType.CritRate:
            case StatType.Lifesteal:
                return $"{(value * 100).ToString("0.#", CultureInfo.InvariantCulture)}%";
        }
        return Math.Floor(value).ToString("N0", CultureInfo.InvariantCulture);
    }
}
