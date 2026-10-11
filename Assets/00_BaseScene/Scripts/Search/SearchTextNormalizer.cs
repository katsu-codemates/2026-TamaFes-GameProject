using System;
using System.Text;

/// <summary>
/// 検索用に文字列の表記ゆれをそろえるクラス。
/// ひらがな/カタカナ・全角/半角・英字の大文字/小文字を区別せずに比較できるようにする。
/// </summary>
public static class SearchTextNormalizer
{
    /// <summary>
    /// 検索用に正規化した文字列を返す。
    /// 例: 「ｱｻﾞﾗｼ」「アザラシ」「あざらし」→「あざらし」、「ＡＢＣ」→「abc」
    /// </summary>
    public static string Normalize(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        // NFKC正規化: 全角英数→半角、半角カナ→全角カナ（濁点も1文字にまとまる）
        string normalized = text.Normalize(NormalizationForm.FormKC).ToLowerInvariant();

        var sb = new StringBuilder(normalized.Length);
        foreach (char c in normalized)
        {
            // 空白は無視する
            if (char.IsWhiteSpace(c)) continue;

            // カタカナ(ァ〜ヶ)をひらがなに変換する
            if (c >= 'ァ' && c <= 'ヶ')
            {
                sb.Append((char)(c - 0x60));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// targetにkeywordが含まれているか（部分一致）を、表記ゆれを無視して判定する。
    /// </summary>
    public static bool Contains(string target, string keyword)
    {
        string normalizedKeyword = Normalize(keyword);
        if (normalizedKeyword.Length == 0) return false;

        return Normalize(target).IndexOf(normalizedKeyword, StringComparison.Ordinal) >= 0;
    }
}
