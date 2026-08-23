using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// 実況分のテンプレート集。
/// 同じ状況でもパターンをいくつか用意することで、マンネリ化を防ぐ。
/// </summary>
public class CommentTempletes
{
    private static readonly string[] RaceStartTempletes =
    {
        "レーススタート！",
        "さあ、レースが始まりました！"  
    };

    private static readonly string[] NewLeaderTempletes =
    {
        "{0}が先頭に立った！",
        "ここで{0}が前に出る！",
        "トップに躍り出たのは{0}だ！"
    };

    private static readonly string[] LeadingTemplates =
    {
        "{0}が独走態勢!",
        "{0}、このままリードを守れるか!?",
        "先頭は変わらず{0}!",
        "すごい勢いだ!{0}!",
        "{0}の前に出る者はいない!!",
        "{0}が飛ばしていく!!"
    };
 
    private static readonly string[] CloseRaceTemplates =
    {
        "{0}と{1}、まさかの大接戦!",
        "{0}と{1}が並んだ!目が離せない展開!",
        "僅差の争い!{0}か{1}か!",
    };
 
    private static readonly string[] SpurtTemplates =
    {
        "{0}、ここでラストスパート!",
        "{0}が一気にペースを上げた!",
        "{0}、勝負に出た!",
    };
 
    private static readonly string[] AccidentTemplates =
    {
        "{0}に何かアクシデントが!?",
        "おっと、{0}がよろけた!",
        "{0}、ここで痛恨のペースダウン!",
    };
 
    private static readonly string[] MiracleTemplates =
    {
        "まさかの大逆転劇!{0}が一気に加速!",
        "{0}に何かが降りてきた!?驚異の追い上げ!",
        "信じられない勢い!{0}が急浮上!",
    };
 
    private static readonly string[] WinnerTemplates =
    {
        "{0}がゴール!優勝です!",
        "決着!勝ったのは{0}!",
        "{0}が一着でゴールイン!",
    };

    public static string RaceStart() 
        => Pick(RaceStartTempletes);
    public static string NewLeader(RaceParticipant p) 
        => Format(NewLeaderTempletes, p.animalData.animalName);
    public static string Leading(RaceParticipant p) 
        => Format(LeadingTemplates, p.animalData.animalName);
    public static string CloseRace(RaceParticipant a, RaceParticipant b) 
        => Format(CloseRaceTemplates, a.animalData.animalName, b.animalData.animalName);
    public static string Spurt(RaceParticipant p) 
        => Format(SpurtTemplates, p.animalData.animalName);
    public static string Accident(RaceParticipant p) 
        => Format(AccidentTemplates, p.animalData.animalName);
    public static string Miracle(RaceParticipant p) 
        => Format(MiracleTemplates, p.animalData.animalName);
    public static string Winner(RaceParticipant p) 
        => Format(WinnerTemplates, p.animalData.animalName);

    private static string Pick(string[] templetes) 
        => templetes[Random.Range(0, templetes.Length)];
    private static string Format(string[] templetes, params object[] args) 
        => string.Format(Pick(templetes), args);
}
