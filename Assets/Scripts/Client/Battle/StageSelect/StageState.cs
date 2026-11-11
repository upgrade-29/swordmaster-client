// 스테이지 선택 화면에서 스테이지가 가지는 상태
public enum StageState
{
    Cleared,   // 이미 깬 스테이지. 다시 도전할 수 있다
    Available, // 다음에 도전할 스테이지
    Locked,    // 아직 열리지 않은 스테이지. 도전할 수 없다
}
