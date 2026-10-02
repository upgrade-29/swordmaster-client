using System;

public class EnhanceRequestException : Exception
{
    public const string NotEnoughGold = "NOT_ENOUGH_GOLD";
    public const string LevelMismatch = "LEVEL_MISMATCH";
    public const string SwordMaxLevel = "SWORD_MAX_LEVEL";
    public const string SwordNotSellable = "SWORD_NOT_SELLABLE";
    public const string ArtifactNotOwned = "ARTIFACT_NOT_OWNED";
    public const string ArtifactMaxLevel = "ARTIFACT_MAX_LEVEL";
    public const string NotEnoughMaterial = "NOT_ENOUGH_MATERIAL";

    public string Code { get; }

    public EnhanceRequestException(string code, string message) : base(message)
    {
        Code = code;
    }
}
