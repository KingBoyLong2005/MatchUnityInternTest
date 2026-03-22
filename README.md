Board.cs
Bỏ toàn bộ cơ chế swap, shift-down, refill từ trên.
Thêm hàng 5 ô bên dưới (m_belowCells) làm khu nhận item.
Fill() viết lại: dùng BuildDivisiblePool() đảm bảo mỗi type xuất hiện đúng bội số của 3.
SendItemBelow(): item từ board bay animation xuống hàng dưới, sau đó kiểm tra match ngang.
CheckBelowMatch() + CompactBelowSlots(): xóa khi đủ 3 cùng loại liền kề, dồn item về trái.
ReturnItemToBoard(): trả item từ hàng dưới lên ô trống trên board (dùng cho Undo mode).
Thêm event OnBoardCleared — bắn khi board chính hết sạch item.
Thêm event OnBelowFull — bắn khi hàng dưới đầy 5 ô.
Thêm Refill() — xóa board chính và fill lại wave mới, giữ nguyên hàng dưới.
Thêm các query: IsBoardEmpty, IsBelowCell(), IsBoardCell(), FindEmptyBoardCell(), GetNonEmptyBoardCells(), GetFilledBelowCells().


BoardController.cs
Bỏ cơ chế drag/swap hoàn toàn. Input chuyển sang click đơn.
Click cell trên board → SendItemBelow().
Click cell hàng dưới (chỉ khi UndoEnabled = true) → ReturnItemToBoard() với scale feedback.
Thêm quản lý wave: TOTAL_WAVES = 3, đếm m_currentWave. Khi board cleared → load wave tiếp; hết wave 3 → GameWin().
OnBelowFull → GameOver().
Thêm public API cho AutoPlayer: IsGameActive, MatchMin, GetNonEmptyBoardCells(), GetBelowCells(), ExecuteAutoMove().
Bỏ hoàn toàn logic Moves.


GameManager.cs
Đổi eLevelMode.MOVES → eLevelMode.FREE (không điều kiện, thắng khi clear 3 wave).
Thêm eStateGame.GAME_WIN — trạng thái thắng riêng biệt.
GameWin() và GameOver() đều dùng chung FinishGame(isWin).
Thêm Singleton Instance.
ClearLevel() bật lại UI điều kiện khi về màn hình chính.


LevelCondition.cs
Thêm ConditionWinEvent và OnConditionWin() — đối xứng với thua, dùng khi cần điều kiện thắng từ LevelCondition.
Xóa overload Setup(float, Text, BoardController) vì LevelMoves không còn dùng.


LevelTime.cs
Thêm FindFirstObjectByType<BoardController>() trong Setup() để tự bật UndoEnabled = true cho Timer mode.

AutoPlayer.cs

eAutoMode.WIN: ưu tiên gửi type nào đang thiếu 1 item để match 3; fallback type phổ biến nhất trong hàng dưới.
eAutoMode.LOSE: ưu tiên gửi type chưa có trong hàng dưới để tránh match và làm đầy nhanh.
StartAutoplay(), StopAutoplay(), ToggleAutoplay() — API điều khiển.
StartAutoplayWin(), StartAutoplayLose(), Stop() — wrapper cho Button.onClick.
Tự FindFirstObjectByType<BoardController>() nếu không gán trong Inspector.