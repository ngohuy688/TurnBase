namespace NueGames.NueDeck.Scripts.Enums
{
    /// <summary>
    /// Loại effect được chọn trong RelicEffectData. ConditionType quyết định lúc chạy,
    /// TargetType quyết định mục tiêu nếu effect có xử lý mục tiêu.
    /// </summary>
    public enum RelicEffectType
    {
        // Tăng sát thương hiện tại theo phần trăm: Value 20 biến 10 damage thành 12.
        // Thường cấu hình với OnDamageDealt; khác IncreaseAttackDamage ở chỗ đây là %.
        IncreaseDamage = 0,

        // Tăng MaxHealth và CurrentHealth của nhân vật chính cùng một lượng điểm cố định.
        // Có thể cấp trong combat hoặc cập nhật dữ liệu máu lưu ngoài combat.
        IncreaseMaxHealth = 1,

        // Cộng Value mana vào mana hiện tại; không đổi giới hạn MaxMana.
        GainMana = 2,

        // Cộng Value điểm sát thương cố định vào parameters.Value của event hiện tại.
        // Thường dùng với OnDamageDealt; khác IncreaseDamage ở chỗ đây không phải %.
        IncreaseAttackDamage = 3,

        // Hồi Value máu cho mục tiêu được cấu hình. Ngoài combat, hồi cho ally đầu tiên
        // trong party đã lưu; máu hồi không vượt MaxHealth.
        Heal = 5,

        // Cấp Value Block cố định cho mục tiêu; mặc định là Ally nếu TargetType không hợp lệ.
        GainBlock = 6,

        // Tăng Block theo phần trăm của parameters.Value. Value 25 nghĩa là +25%;
        // bonus được cộng vào target và tối thiểu là 1 nếu phần trăm dương.
        IncreaseBlock = 7,

        // Gây lượng damage cố định bằng Value lên source. Thường dùng với OnDamageTaken.
        Thorns = 8,

        // Tăng cả MaxMana và CurrentMana thêm Value, trong hoặc ngoài combat.
        IncreaseMaxMana = 9,

        // Lưu Value làm mức giảm mana cost cho lá bài kế tiếp. Có hàm ConsumeReduction
        // để xóa mức giảm, nhưng hiện chưa có call site dùng hàm đó trong gameplay.
        ReduceCardCost = 10,

        // Passive helper cộng các Value để giảm cost của mọi lá bài. Cần được gọi khi
        // tính cost; hiện chưa được nối vào luồng tính cost của card.
        ReduceAllCardCost = 11,

        // Rút ngay Value lá bài từ CollectionManager.
        DrawCard = 12,

        // Passive helper cộng Value để tăng giới hạn bài trên tay; CollectionManager
        // dùng bonus này khi tính max hand size.
        IncreaseHandSize = 13,

        // Cộng Value stack Poison cho các mục tiêu; mặc định là tất cả Enemy.
        // Poison gây damage theo số stack rồi giảm 1 stack mỗi lượt.
        ApplyPoison = 14,

        // Cộng Value stack Weak cho mục tiêu; mặc định là tất cả Enemy.
        // Weak giảm sát thương tấn công theo số stack và không tự giảm mỗi lượt.
        ApplyWeak = 15,

        // Cộng Value stack Vulnerable cho mục tiêu; mặc định là tất cả Enemy.
        // Khi còn Vulnerable, mỗi đòn đánh nhận vào tăng 50%; stack giảm 1 mỗi lượt.
        ApplyVulnerable = 16,

        // Passive helper cộng các Value thành bonus thời lượng debuff. Hiện chưa có
        // luồng áp dụng debuff nào gọi helper này.
        IncreaseDebuffDuration = 17,

        // Hiện chỉ kiểm tra dữ liệu và ghi log; chưa chọn hoặc cấp relic ngẫu nhiên.
        GainArtifact = 18,

        // Cộng Value stack Strength cho mục tiêu; mặc định là Ally.
        // Mỗi stack cộng 1 damage phẳng vào đòn đánh và không tự giảm mỗi lượt.
        GainStrength = 19,

        // Dùng Value làm số lần chặn trong encounter. Hiện effect chỉ đặt damage event
        // về 0, nhưng OnDamageTaken được gọi sau khi damage đã trừ máu nên chưa chặn
        // được damage thực tế.
        PreventDamage = 20,

        // Cộng Value vàng vào CurrentGold và cập nhật UI vàng.
        gainGold = 21,

        // Passive helper cộng các Value thành % giảm giá. GetDiscountedCost làm tròn
        // cost và chặn không cho xuống dưới 0; cần được gọi khi tạo giá shop.
        ShopDiscount = 22,

        // Lưu Value vào RepeatCount. Bản thân effect chưa phát lại card; cần consumer
        // đọc và gọi ConsumeRepeat để thực hiện số lần lặp.
        CopyLastCard = 23,

        // Lặp Value lần qua các relic đang sở hữu, gọi lại condition OnRelicEquipped
        // của chúng. Bỏ qua chính relic chứa effect này.
        TriggerEffectTwice = 24
    }
}