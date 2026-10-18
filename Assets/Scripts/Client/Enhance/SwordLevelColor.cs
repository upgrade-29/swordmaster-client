// 검 강화 단계 글자(+3)를 검 재질 구간별 색으로 감싼다. TMP 리치 텍스트를 켠 글자에 쓴다
public static class SwordLevelColor
{
    public static string Format(int level)
    {
        return $"<color=#{GetHexColor(level)}>+{level.ToString()}</color>";
    }

    private static string GetHexColor(int level)
    {
        string hexColor = GameConstants.SwordLevelColors.Wood;
        if (level >= 25)
        {
            hexColor = GameConstants.SwordLevelColors.Celestial;
        }
        else if (level >= 19)
        {
            hexColor = GameConstants.SwordLevelColors.Frost;
        }
        else if (level >= 14)
        {
            hexColor = GameConstants.SwordLevelColors.Flame;
        }
        else if (level >= 11)
        {
            hexColor = GameConstants.SwordLevelColors.NobleMetal;
        }
        else if (level >= 5)
        {
            hexColor = GameConstants.SwordLevelColors.Iron;
        }

        return hexColor;
    }
}
