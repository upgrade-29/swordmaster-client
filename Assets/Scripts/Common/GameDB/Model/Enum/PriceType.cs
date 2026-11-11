using Newtonsoft.Json;

// 상품 가격의 결제 방식. 서버 PriceType(GOLD, DIAMOND, FREE, CASH)과 이름을 맞춘다.
[JsonConverter(typeof(UpperSnakeEnumConverter))]
public enum PriceType
{
    None = 0,
    Gold,
    Diamond,
    Free
}