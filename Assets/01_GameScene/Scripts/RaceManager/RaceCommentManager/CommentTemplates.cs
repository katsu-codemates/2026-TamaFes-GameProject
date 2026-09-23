using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// 実況分のテンプレート集。
/// 同じ状況でもパターンをいくつか用意することで、マンネリ化を防ぐ。
/// </summary>
public class CommentTemplates
{
    private static readonly string[] RaceStartTemplates =
    {
        "レーススタート！",
        "さあ、レースが始まりました！"  
    };

    private static readonly string[] NewLeaderTemplates =
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

    private static readonly string[] OvertakeTemplates =
    {
        "{0}が{1}を抜いた!",
        "{0}、{1}をかわして{2}位に浮上!",
        "{0}が{1}をとらえた!{2}位に上がる!",
    };

    private static readonly string[] OvertakeForLeadTemplates =
    {
        "{0}が{1}をかわして先頭へ!",
        "{0}、{1}を抜いてトップに立った!",
        "先頭交代!{0}が{1}を抜き去った!",
    };

    private static readonly string[] StaminaDepletedTemplates =
    {
        "{0}、ここでスタミナ切れか!?",
        "おっと{0}の脚が止まった!苦しい!",
        "{0}、ペースが落ちてきた!踏ん張れるか!?",
    };

    private static readonly string[] FinishedTemplates =
    {
        "{0}が{1}着でゴール!",
        "続いて{0}がゴールイン!{1}着!",
        "{1}着は{0}!よく頑張った!",
    };

    private static readonly string[] LastFinisherTemplates =
    {
        "最後に{0}もゴール!全員完走です!",
        "{0}もゴールイン!みんなよく走りきった!",
        "{0}が最後まで走りきった!全員完走!",
    };

    private static readonly string[] PlaceBattleTemplates =
    {
        "{2}着争いは{0}と{1}!",
        "{0}と{1}、{2}着をかけて激しい争い!",
    };

    private static readonly string[] HeadingToGoalTemplates =
    {
        "{0}、ゴールまであと少し!",
        "{0}が{1}着を目指して駆ける!",
    };

    private static readonly string[] FinalStretchTemplates =
    {
        "さあ最後の直線!先頭は{0}!",
        "ゴールが見えてきた!{0}が逃げ切るか!?",
        "いよいよゴール前!{0}が先頭だ!",
    };

    private static readonly string[] ChaserShotTemplates =
    {
        "後続集団は{0}が引っ張る!",
        "{0}、先頭を追いかける!",
        "後ろでは{0}がチャンスをうかがっている!",
    };

    private static readonly string[] SpotlightLastTemplates =
    {
        "最後方は{0}!ここから巻き返せるか!?",
        "{0}は最後方から追走!まだまだ諦めない!",
    };

    private static readonly string[] SpotlightMidTemplates =
    {
        "{1}位には{0}、じっくり脚をためている!",
        "{0}は現在{1}位!虎視眈々と前を狙う!",
    };

    private static readonly string[] SpotlightStatTemplates =
    {
        "{1}自慢の{0}、見せ場はまだこれからだ!",
        "{1}が武器の{0}、ここからどう動く!?",
    };

    public static string RaceStart() 
        => Pick(RaceStartTemplates);
    public static string NewLeader(RaceParticipant p) 
        => Format(NewLeaderTemplates, p.animalData.animalName);
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

    public static string Overtake(RaceParticipant passer, RaceParticipant passed, int newRank)
        => newRank == 1
            ? Format(OvertakeForLeadTemplates, passer.animalData.animalName, passed.animalData.animalName)
            : Format(OvertakeTemplates, passer.animalData.animalName, passed.animalData.animalName, newRank);
    public static string StaminaDepleted(RaceParticipant p)
        => Format(StaminaDepletedTemplates, p.animalData.animalName);
    public static string Finished(RaceParticipant p, int rank)
        => Format(FinishedTemplates, p.animalData.animalName, rank);
    public static string LastFinisher(RaceParticipant p)
        => Format(LastFinisherTemplates, p.animalData.animalName);
    public static string PlaceBattle(RaceParticipant a, RaceParticipant b, int place)
        => Format(PlaceBattleTemplates, a.animalData.animalName, b.animalData.animalName, place);
    public static string HeadingToGoal(RaceParticipant p, int place)
        => Format(HeadingToGoalTemplates, p.animalData.animalName, place);
    public static string FinalStretch(RaceParticipant p)
        => Format(FinalStretchTemplates, p.animalData.animalName);
    public static string ChaserShot(RaceParticipant p)
        => Format(ChaserShotTemplates, p.animalData.animalName);
    public static string SpotlightLast(RaceParticipant p)
        => Format(SpotlightLastTemplates, p.animalData.animalName);
    public static string SpotlightMid(RaceParticipant p, int rank)
        => Format(SpotlightMidTemplates, p.animalData.animalName, rank);
    public static string SpotlightStat(RaceParticipant p)
        => Format(SpotlightStatTemplates, p.animalData.animalName, GetBestStatName(p.animalData));

    // 一番高いステータスの名前を返す
    private static string GetBestStatName(AnimalData data)
    {
        string bestName = "スピード";
        float best = data.speed;
        if (data.power > best) { best = data.power; bestName = "パワー"; }
        if (data.wisdom > best) { best = data.wisdom; bestName = "かしこさ"; }
        if (data.luck > best) { best = data.luck; bestName = "運"; }
        if (data.stamina > best) { best = data.stamina; bestName = "スタミナ"; }
        return bestName;
    }

    private static string Pick(string[] templates) 
        => templates[Random.Range(0, templates.Length)];
    private static string Format(string[] templates, params object[] args) 
        => string.Format(Pick(templates), args);
}
