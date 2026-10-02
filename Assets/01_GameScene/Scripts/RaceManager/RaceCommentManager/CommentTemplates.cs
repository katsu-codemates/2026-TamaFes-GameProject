using Unity.AppUI.UI;
using System.Collections.Generic;
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
        "レーススタート!",
        "さあ、レースが始まりました!",
        "ゲートが開いた!各者いっせいに飛び出した!",
        "スタートしました!熱い戦いの幕開けです!",
        "号砲一発!激戦の火ぶたが切って落とされた!",
        "各者そろって好スタート!",
        "いよいよ始まりました!栄冠は誰の手に!?",
        "さあ走り出した!目が離せない一戦!",
        "砂煙が舞い上がるスタートダッシュだ!",
        "待ちに待ったこの瞬間!レース開始です!",
        "運命のレース、今スタート!",
        "一斉に駆け出した!観客席も大興奮!",
        "飛び出した!さあ誰が抜け出すのか!",
        "今、一斉に走者が動き出した!",
        "みんな勢いよく飛び出しスタート!",
        "レース開始!手に汗握るレースになりそうだ!",
        "さあ行った!全員まっすぐゴールを目指す!",
        "出走!会場のボルテージが一気に上がる!",
        "合図が鳴った!弾かれたように走り出す!",
        "いざ勝負!真剣勝負の開幕です!",
    };

    private static readonly string[] NewLeaderTemplates =
    {
        "{0}が先頭に立った!",
        "ここで{0}が前に出る!",
        "トップに躍り出たのは{0}だ!",
        "{0}、ついにハナを奪った!",
        "先頭が入れ替わった!今度は{0}!",
        "{0}がグイッと前へ!首位に立つ!",
        "主導権を握ったのは{0}!",
        "{0}、集団の一番前に顔を出した!",
        "新たなリーダーは{0}だ!",
        "{0}がレースの先頭を奪い取った!",
        "なんと、{0}が抜け出してトップへ!",
        "先頭に変化!{0}が前を行く!",
        "{0}、ここで首位に浮上!",
        "{0}がレースを引っ張る形に!",
        "今トップにいるのは{0}!",
        "{0}、先陣を切って駆けていく!",
        "{0}が一番手に上がってきた!",
        "流れが変わった!前を行くのは{0}!",
        "{0}、鮮やかにリードを奪う!",
        "さあ{0}が主役に躍り出た!",
    };

    private static readonly string[] LeadingTemplates =
    {
        "{0}が独走態勢!",
        "{0}、このままリードを守れるか!?",
        "先頭は変わらず{0}!",
        "すごい勢いだ!{0}!",
        "{0}の前に出る者はいない!!",
        "{0}が飛ばしていく!!",
        "{0}、快調なペースで逃げる!",
        "依然としてトップは{0}!",
        "{0}、後ろを寄せつけない!",
        "軽やかな足取り!{0}が先頭をキープ!",
        "{0}の逃げ足が止まらない!",
        "このまま押し切るのか{0}!",
        "{0}、リードをじわじわ広げる!",
        "余裕の走りだ{0}!",
        "誰か{0}を止められるのか!?",
        "{0}、堂々の一人旅!",
        "レースを支配しているのは{0}!",
        "{0}のペースで展開が進む!",
        "さすがの貫禄!{0}が首位を譲らない!",
        "{0}、ぐんぐん突き進む!",
    };

    // {0}=先頭, {1}=2番手
    private static readonly string[] CloseRaceTemplates =
    {
        "{0}の後を{1}が狙っている!",
        "{1}が{0}を追いかける展開となりました!",
        "{0}の後ろには{1}がいる!",
        "{0}と{1}、ほとんど差がない!",
        "{1}がぴったりマーク!{0}は逃げ切れるか!?",
        "並んだ並んだ!{0}と{1}の競り合い!",
        "{0}のすぐ背後に{1}が迫る!",
        "{1}、{0}をとらえにかかる!",
        "{0}対{1}、一歩も譲らない!",
        "デッドヒート!{0}と{1}が火花を散らす!",
        "{1}が差を詰めてきた!{0}危うし!",
        "{0}、{1}の気配を感じているか!?",
        "わずかにリードは{0}!しかし{1}も食い下がる!",
        "横一線!{0}と{1}がもつれ合う!",
        "{1}、あと一歩で{0}に届く!",
        "激しいつばぜり合い!{0}と{1}!",
        "{0}を{1}が猛追!",
        "息をのむ接戦!先頭{0}、2番手{1}!",
        "{1}がじりじりと{0}に並びかける!",
        "鼻先の勝負!{0}か、{1}か!",
    };

    private static readonly string[] SpurtTemplates =
    {
        "{0}、ここでラストスパート!",
        "{0}が一気にペースを上げた!",
        "{0}、勝負に出た!",
        "{0}のギアが上がった!",
        "来た来た!{0}の猛ダッシュ!",
        "{0}、全力疾走に切り替えた!",
        "{0}がエンジン全開!",
        "ここが勝負どころ!{0}が仕掛けた!",
        "{0}、力をふりしぼって加速!",
        "{0}の本気が見えた!",
        "{0}がスピードに乗ってきた!",
        "一気に行った{0}!",
        "{0}、ためていた脚を解き放つ!",
        "{0}が猛然と駆け出した!",
        "{0}、ここからが本領発揮だ!",
        "さあ{0}が動いた!",
        "{0}の目の色が変わった!",
        "{0}、弾丸のように飛び出す!",
        "ロングスパート開始!{0}!",
        "{0}、ぐいぐい伸びてくる!",
    };

    private static readonly string[] AccidentTemplates =
    {
        "{0}に何かアクシデントが!?",
        "おっと、{0}がよろけた!",
        "{0}、ここで痛恨のペースダウン!",
        "あっと!{0}がつまずいた!",
        "{0}、足がもつれたか!?",
        "{0}にトラブル発生!",
        "これは痛い!{0}が失速!",
        "{0}、バランスを崩した!",
        "なんと{0}が転びそうに!",
        "{0}、思わぬハプニング!",
        "{0}の動きがおかしいぞ!?",
        "ああっと!{0}に不運が襲いかかる!",
        "{0}、ここでまさかのミス!",
        "{0}がふらついている!大丈夫か!?",
        "{0}、急ブレーキ!何があった!?",
        "{0}、リズムが乱れた!",
        "会場から悲鳴!{0}が大ピンチ!",
        "{0}、立て直せるか!?",
        "{0}が大きく後退!",
        "波乱の予感!{0}が脚を止めた!",
    };

    private static readonly string[] MiracleTemplates =
    {
        "まさかの大逆転劇!{0}が一気に加速!",
        "{0}に何かが降りてきた!?驚異の追い上げ!",
        "信じられない勢い!{0}が急浮上!",
        "奇跡だ!{0}が覚醒した!",
        "{0}、秘めた力が爆発!",
        "これはすごい!{0}が別次元の走り!",
        "{0}にレースの女神がほほえんだ!",
        "なんということだ!{0}が次々と抜いていく!",
        "{0}、まるで風になったようだ!",
        "会場騒然!{0}の大進撃!",
        "{0}、限界を超えた!",
        "目を疑う光景!{0}がぶっ飛んでくる!",
        "{0}の中で何かが目覚めた!",
        "ミラクル発動!{0}!",
        "{0}、奇跡の末脚!",
        "今日の主役はこの子か!?{0}が猛チャージ!",
        "{0}、流星のような追い込み!",
        "誰も予想できなかった!{0}の大爆発!",
        "{0}がとんでもない伸び!",
        "神がかっている!{0}の走り!",
    };

    private static readonly string[] WinnerTemplates =
    {
        "{0}がゴール!優勝です!",
        "決着!勝ったのは{0}!",
        "{0}が一着でゴールイン!",
        "{0}、堂々の優勝!",
        "栄光のゴールテープを切ったのは{0}!",
        "やりました{0}!見事な勝利!",
        "{0}、トップでフィニッシュ!",
        "王者誕生!{0}!",
        "{0}が栄冠をつかんだ!",
        "先頭で駆け抜けたのは{0}!",
        "{0}、完璧な走りで1着!",
        "勝者は{0}!おめでとう!",
        "{0}、ついに頂点に立った!",
        "大歓声!{0}がチャンピオンだ!",
        "{0}が真っ先に飛び込んだ!",
        "{0}、圧巻の勝ちっぷり!",
        "制したのは{0}!会場が揺れている!",
        "{0}、最高の瞬間を迎えた!",
        "今日一番速かったのは{0}!",
        "{0}が伝説を刻んだ!",
    };

    // {0}=抜いた走者, {1}=抜かれた走者, {2}=新しい順位
    private static readonly string[] OvertakeTemplates =
    {
        "{0}が{1}を抜いた!",
        "{0}、{1}をかわして{2}位に浮上!",
        "{0}が{1}をとらえた!{2}位に上がる!",
        "{0}、{1}の外から並びかけて前へ!",
        "{1}を置き去りに!{0}が{2}位!",
        "{0}がするりと{1}を抜き去る!",
        "{0}、{1}をパス!順位を上げる!",
        "{2}位浮上!{0}が{1}の前に出た!",
        "{1}、{0}に先を越された!",
        "{0}が{1}を差した!",
        "{0}、{1}を追い越して{2}番手!",
        "順位変動!{0}が{1}より前へ!",
        "{0}、{1}をぶち抜いた!",
        "鮮やかな追い抜き!{0}が{1}を攻略!",
        "{1}の横を{0}が駆け抜ける!",
        "{0}、{1}を振り切って{2}位へ!",
        "{0}が{1}をまくった!",
        "{1}は{0}に前を譲る形!",
        "{0}、ぐんぐん迫って{1}を逆転!",
        "{0}が{1}の前に躍り出た!",
    };

    // {0}=抜いた走者, {1}=抜かれた走者
    private static readonly string[] OvertakeForLeadTemplates =
    {
        "{0}が{1}をかわして先頭へ!",
        "{0}、{1}を抜いてトップに立った!",
        "先頭交代!{0}が{1}を抜き去った!",
        "{0}、ついに{1}をとらえて首位!",
        "{1}の天下もここまでか!{0}がトップへ!",
        "{0}が{1}を差し切って先頭!",
        "首位奪取!{0}が{1}の前へ!",
        "{0}、{1}からトップの座を奪う!",
        "入れ替わった!先頭は{1}から{0}へ!",
        "{0}が{1}を捕まえた!一番手だ!",
        "{1}、先頭を明け渡す!{0}が前に!",
        "{0}、{1}を置き去りにしてトップ!",
        "逆転!{0}が{1}を上回った!",
        "{0}、{1}に並ぶ間もなく抜き去った!",
        "{0}がついにハナを奪う!{1}は2番手に後退!",
        "{0}、{1}をまくってリーダーに!",
        "トップ争いを制したのは{0}!{1}を抜いた!",
        "{0}が{1}を追い越し、レースの先頭へ!",
        "{1}から{0}へ、主役が交代!",
        "大歓声!{0}が{1}を抜いて首位浮上!",
    };

    private static readonly string[] StaminaDepletedTemplates =
    {
        "{0}、ここでスタミナ切れか!?",
        "おっと{0}の脚が止まった!苦しい!",
        "{0}、ペースが落ちてきた!踏ん張れるか!?",
        "{0}、息が上がってきた!",
        "{0}の体力が限界か!?",
        "{0}、苦しい表情!がんばれ!",
        "{0}がバテてきた!",
        "{0}、燃料切れの様子!",
        "ガス欠か!?{0}の動きが重い!",
        "{0}、ここは我慢のしどころ!",
        "{0}の足取りが鈍くなった!",
        "飛ばしすぎたか{0}!",
        "{0}、へとへとだ!それでも走る!",
        "{0}、肩で息をしている!",
        "{0}の勢いに陰りが見えた!",
        "{0}、ここからが根性の見せどころ!",
        "{0}がずるずると後退していく!",
        "もう限界か!?{0}!",
        "{0}、力を使い果たしてしまったか!",
        "{0}、ふらふらだ!持ちこたえられるか!",
    };

    // {0}=走者, {1}=着順
    private static readonly string[] FinishedTemplates =
    {
        "{0}が{1}着でゴール!",
        "続いて{0}がゴールイン!{1}着!",
        "{1}着は{0}!よく頑張った!",
        "{0}、{1}着でフィニッシュ!",
        "{1}着に飛び込んだのは{0}!",
        "{0}もゴール!順位は{1}着!",
        "いい走りだった!{0}が{1}着!",
        "{0}、{1}番目にゴールを駆け抜けた!",
        "{1}着確定!{0}!",
        "{0}がゴールテープへ!{1}着です!",
        "お見事{0}!{1}着でゴール!",
        "{0}、{1}着を守りきった!",
        "ここで{0}がゴール!{1}着!",
        "{1}着は{0}に決まった!",
        "{0}、{1}着入線!",
        "拍手を送りましょう!{0}、{1}着!",
        "{0}がやってきた!{1}着でゴール!",
        "{0}、最後まで粘って{1}着!",
        "{1}着で{0}がレースを終えた!",
        "ナイスラン{0}!{1}着!",
    };

    private static readonly string[] LastFinisherTemplates =
    {
        "最後に{0}もゴール!全員完走です!",
        "{0}もゴールイン!みんなよく走りきった!",
        "{0}が最後まで走りきった!全員完走!",
        "しんがりの{0}もゴール!全員そろいました!",
        "{0}がゴール!これで全員がフィニッシュ!",
        "ラストランナー{0}もゴール!",
        "{0}、あきらめずにゴール!拍手!",
        "全員ゴール!最後を飾ったのは{0}!",
        "{0}も無事ゴールイン!お疲れさま!",
        "{0}がゴール!みんながんばりました!",
        "大きな拍手を!{0}が最後にゴール!",
        "{0}、最後までよく粘った!これで全員完走!",
        "{0}もたどり着いた!レース終了です!",
        "{0}のゴールで、全員のレースが終わりました!",
        "最後尾から{0}もゴール!感動のフィナーレ!",
        "{0}、ゴール!みんなの走りに拍手!",
        "{0}がゴールイン!全員無事に完走!",
        "{0}もゴール!最高のレースでした!",
        "{0}が走り抜けた!これにて全員フィニッシュ!",
        "{0}のゴールで幕引き!ナイスファイト!",
    };

    // {0}=前の走者, {1}=後ろの走者, {2}=争っている着順
    private static readonly string[] PlaceBattleTemplates =
    {
        "{2}着争いは{0}と{1}!",
        "{0}と{1}、{2}着をかけて激しい争い!",
        "{2}着の座を狙って{0}と{1}が競り合う!",
        "{0}か{1}か!{2}着はどっちだ!",
        "{2}着をめぐるデッドヒート!{0}と{1}!",
        "{0}、{1}、並んで{2}着を争う!",
        "{0}のすぐ後ろに{1}!{2}着争いが熱い!",
        "{2}着はまだわからない!{0}と{1}が接戦!",
        "譲れない{2}着!{0}と{1}がぶつかり合う!",
        "{0}と{1}、どちらも{2}着を諦めない!",
        "{1}が{0}に迫る!{2}着の行方は!?",
        "{2}着争いも白熱!{0}対{1}!",
        "{0}が一歩前!しかし{1}も{2}着を狙う!",
        "{0}と{1}の{2}着バトル!",
        "横並びの{0}と{1}!{2}着を手にするのは!?",
        "{2}着をかけた意地の張り合い!{0}と{1}!",
        "{0}、{1}を振り切って{2}着を取れるか!?",
        "{1}、{0}をかわして{2}着に届くか!?",
        "まだまだ終わらない!{2}着争いは{0}と{1}!",
        "{2}着へ向けて{0}と{1}が火花!",
    };

    // {0}=走者, {1}=目指す着順
    private static readonly string[] HeadingToGoalTemplates =
    {
        "{0}、ゴールまであと少し!",
        "{0}が{1}着を目指して駆ける!",
        "{0}、{1}着が見えてきた!",
        "ゴールはもうすぐ!{0}!",
        "{0}、このまま{1}着に飛び込むか!",
        "{1}着はほぼ{0}か!?",
        "{0}が{1}着へ向けてラストラン!",
        "{0}、ゴールに向かってまっしぐら!",
        "あと少しだ{0}!{1}着が待っている!",
        "{0}、最後の力を振りしぼる!",
        "{1}着をつかみ取れ{0}!",
        "{0}がゴールへ一直線!",
        "{0}、{1}着圏内をキープ!",
        "見えたぞゴール!{0}が迫る!",
        "残りわずか!{0}が{1}着を狙う!",
        "{0}、ゴール板はすぐそこ!",
        "さあ{0}、{1}着でゴールできるか!",
        "{0}、ゴールを目前に踏ん張る!",
        "{1}着争いは{0}が一歩リード!",
        "がんばれ{0}!{1}着はもう目の前!",
    };

    private static readonly string[] FinalStretchTemplates =
    {
        "さあ最後の直線!先頭は{0}!",
        "ゴールが見えてきた!{0}が逃げ切るか!?",
        "いよいよゴール前!{0}が先頭だ!",
        "最終局面!トップは{0}!",
        "残りわずか!{0}がそのまま行くか!",
        "クライマックス!{0}が先頭でゴールを目指す!",
        "泣いても笑ってもあと少し!先頭{0}!",
        "勝負の直線に入った!{0}が前!",
        "{0}、フィニッシュへ向かって一直線!",
        "最後の攻防!{0}がリードを守れるか!",
        "フィナーレが近づく!首位は{0}!",
        "ゴールは目前!{0}が逃げる!",
        "ラストの直線!{0}、このまま押し切るか!?",
        "運命の瞬間が迫る!先頭は{0}!",
        "{0}がトップのまま最後の勝負へ!",
        "会場が総立ち!{0}が先頭でゴールへ!",
        "いよいよ決着のとき!{0}がリード!",
        "このまま{0}が勝つのか!?ゴールはすぐそこ!",
        "最後の力比べ!{0}が一歩前!",
        "栄光まであとわずか!{0}!",
    };

    private static readonly string[] ChaserShotTemplates =
    {
        "後続集団は{0}が引っ張る!",
        "{0}、先頭を追いかける!",
        "後ろでは{0}がチャンスをうかがっている!",
        "後方の様子はどうだ!{0}が頑張っている!",
        "{0}も負けていない!懸命に追走!",
        "集団の中で{0}が力強く走る!",
        "{0}、まだまだ諦めていない!",
        "後続から{0}がじわじわ前へ!",
        "{0}、虎視眈々と前を見据える!",
        "こちらでは{0}が粘りの走り!",
        "{0}、一発逆転を狙っているぞ!",
        "集団をリードするのは{0}!",
        "後ろも熱い!{0}が奮闘中!",
        "{0}、ひたむきに前を追う!",
        "{0}の目はまだ死んでいない!",
        "追走グループの中心に{0}!",
        "{0}、いつでも仕掛けられる位置!",
        "{0}もいい脚色だ!",
        "さあ{0}、ここから巻き返せるか!",
        "{0}、黙々と距離を詰めている!",
    };

    private static readonly string[] SpotlightLastTemplates =
    {
        "最後方は{0}!ここから巻き返せるか!?",
        "{0}は最後方から追走!まだまだ諦めない!",
        "しんがりは{0}!逆転の一手はあるか!",
        "{0}、最後尾でじっと機をうかがう!",
        "一番後ろに{0}!ここからが見ものだ!",
        "{0}、後ろから大外一気を狙う!?",
        "最後尾の{0}、マイペースで走る!",
        "{0}はうしろで力をためている!",
        "まだ勝負は終わっていないぞ{0}!",
        "最後方の{0}、ここから追い上げなるか!",
        "{0}、一番後ろから前をにらむ!",
        "後方待機の{0}!末脚に期待!",
        "{0}、いまは最後尾!でもあきらめない!",
        "{0}、後ろからの逆襲はあるか!",
        "最後尾でも{0}は楽しそうだ!",
        "{0}、ここから大逆転を見せてくれ!",
        "後ろから全体を見渡す{0}!",
        "{0}、最後方でじっくり構える!",
        "がんばれ{0}!まだチャンスはある!",
        "{0}、追い込みにかける!",
    };

    // {0}=走者, {1}=現在の順位
    private static readonly string[] SpotlightMidTemplates =
    {
        "{1}位には{0}、じっくり脚をためている!",
        "{0}は現在{1}位!虎視眈々と前を狙う!",
        "{1}位を走るのは{0}!",
        "{0}、{1}位で様子をうかがう!",
        "中団の{0}、現在{1}位!",
        "{0}は{1}位!ここからどう出る!?",
        "{1}番手に{0}!いい位置につけている!",
        "{0}、{1}位から上を目指す!",
        "{1}位の{0}、まだ余力を残しているか!",
        "{0}は{1}位で淡々と走る!",
        "{1}位に{0}!好位をキープ!",
        "{0}、{1}位!仕掛けどころを探っている!",
        "注目は{1}位の{0}!",
        "{0}、{1}位でしぶとく食らいつく!",
        "{1}位は{0}!前との差はどうだ!",
        "{0}、{1}位で力を温存中!",
        "{1}位の{0}、ひとつでも順位を上げたい!",
        "{0}は今{1}位!ペースは悪くない!",
        "{1}位で踏ん張る{0}!",
        "{0}、{1}位からの浮上を狙う!",
    };

    // {0}=走者, {1}=一番高いステータス名(スピード/パワー/かしこさ/運/スタミナ)
    private static readonly string[] SpotlightStatTemplates =
    {
        "{1}自慢の{0}、見せ場はまだこれからだ!",
        "{1}が武器の{0}、ここからどう動く!?",
        "{0}の持ち味は{1}!",
        "{1}なら誰にも負けない{0}!",
        "{0}、得意の{1}で勝負をかけるか!",
        "{1}に定評のある{0}!",
        "{0}、{1}を生かした走りに注目!",
        "一番の強みは{1}!{0}に期待!",
        "{0}の{1}が火を吹くか!?",
        "{1}の高さが光る{0}!",
        "{0}、その{1}でレースをかき回せ!",
        "{1}には自信あり!{0}!",
        "{0}の{1}、ここで発揮されるか!",
        "秘密兵器は{1}!{0}から目が離せない!",
        "{0}、{1}ではトップクラス!",
        "{1}を信じて走れ{0}!",
        "{0}の{1}がレースを左右するかも!",
        "{1}で勝負する{0}!",
        "{0}といえば{1}!さあ見せてくれ!",
        "{1}に恵まれた{0}!",
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

    // テンプレート配列ごとのシャッフル袋(まだ使っていない添字の山)と、直前に使った添字
    private static readonly Dictionary<string[], List<int>> bags = new Dictionary<string[], List<int>>();
    private static readonly Dictionary<string[], int> lastPicked = new Dictionary<string[], int>();

    /// <summary>
    /// シャッフル袋方式でテンプレートを1つ選ぶ。
    /// 袋が空になるまで同じ文は二度と出ず、袋を詰め直したときも直前と同じ文が先頭に来ないようにする。
    /// これで同じ実況文が連続して渡されることはない。
    /// </summary>
    private static string Pick(string[] templates)
    {
        if (templates.Length == 1) return templates[0];

        if (!bags.TryGetValue(templates, out List<int> bag))
        {
            bag = new List<int>(templates.Length);
            bags[templates] = bag;
        }

        if (bag.Count == 0)
        {
            for (int i = 0; i < templates.Length; i++) bag.Add(i);

            // Fisher-Yatesでシャッフル
            for (int i = bag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (bag[i], bag[j]) = (bag[j], bag[i]);
            }

            // 詰め直した直後に前回と同じ文が出ないよう、末尾(次に取り出す位置)とずらす
            int last = bag.Count - 1;
            if (lastPicked.TryGetValue(templates, out int prev) && bag[last] == prev)
            {
                (bag[last], bag[0]) = (bag[0], bag[last]);
            }
        }

        int index = bag[bag.Count - 1];
        bag.RemoveAt(bag.Count - 1);
        lastPicked[templates] = index;
        return templates[index];
    }
    private static string Format(string[] templates, params object[] args) 
        => string.Format(Pick(templates), args);
}
