// Lưu ý: Class này KHÔNG kế thừa MonoBehaviour
public static class GameSettings
{
    // Biến này sẽ lưu trữ xem người chơi có chọn Arena Mode hay không
    // static giúp biến này tồn tại xuyên suốt giữa các Scene
    public static bool isArenaMode = false;
    // THÊM DÒNG NÀY: Xác định xem có chơi với Máy không (Mặc định AI sẽ là Player 2)
    public static bool isVsAI = false;
}