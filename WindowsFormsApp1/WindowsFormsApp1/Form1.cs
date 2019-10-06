using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }


        // ログ1行から目的語の複数の種類のカードの名前と枚数を抽出してリスト化
        private List<string> ExtractObjectCards(string line, string shortPlayerName)
        {                                                                             // 例：文字列 "Tは銅貨3枚、屋敷2枚を引いた。"
            var l = line.Substring((shortPlayerName + "は").Length);                  // 例：文字列 "銅貨3枚、屋敷2枚を引いた。"
            l = l.Remove(l.IndexOf("を"));                                            // 例：文字列 "銅貨3枚、屋敷2枚"
            var ss = l.Split(new string[] { "、" }, StringSplitOptions.None);         // 例：文字列配列 {"銅貨3枚", "屋敷2枚"}
            var cards = new List<string>();
            foreach (var s in ss)
            {
                var n = System.Text.RegularExpressions.Regex.Replace(s, @"[^0-9]", "");         // 数字を抽出
                if (n == "") cards.Add(s);                                                      // 枚数がない->1枚
                else for (int i = 0; i < int.Parse(n); ++i) cards.Add(s.Remove(s.IndexOf(n)));  // 例： リスト {"銅貨", "銅貨", "銅貨"}
            }
            return cards;                                                                       // 例： リスト {"銅貨", "銅貨", "銅貨", "屋敷", "屋敷"}
        }

        private List<string>[] GetOwnCards(string[] log, string[] shortPlayerNames)
        {
            string[] get_strings = new string[] { "を受け取った。", "獲得した。" };
            string[] lost_strings = new string[] { "を廃棄した。", "戻した。"};
            string give_string = "渡した。";                                    // 仮面
            var ownCards = new List<string>[2] { new List<string>(), new List<string>() };
            foreach (var line in log)
            {
                for (int i = 0; i < 2; ++i)
                {
                    foreach (var get_string in get_strings)
                    {
                        if (line.StartsWith(shortPlayerNames[i]) && line.Contains(get_string))
                        {
                            var cards = ExtractObjectCards(line, shortPlayerNames[i]);
                            ownCards[i].AddRange(cards);
                        }
                    }
                    foreach (var lost_string in lost_strings)
                    {
                        if (line.StartsWith(shortPlayerNames[i]) && line.Contains(lost_string))
                        {
                            var cards = ExtractObjectCards(line, shortPlayerNames[i]);
                            foreach (var card in cards) ownCards[i].Remove(card);
                        }
                    }
                    if (line.StartsWith(shortPlayerNames[i]) && line.Contains(give_string))
                    {
                        var cards = ExtractObjectCards(line, shortPlayerNames[i]);
                        foreach (var card in cards) ownCards[i].Remove(card);
                        int opponent = (i + 1) % 2;
                        ownCards[opponent].AddRange(cards);
                    }
                }
            }
            return ownCards;
        }

        private int GetLastShuffleLineNumber(string[] log, string shortPlayerName)
        {
            string shuffle_string = "山札をシャッフルした。";
            int lastShuffleLineNumber = 0;
            foreach (var (line, i) in log.Select((line, i) => (line, i)))
                if (line.StartsWith(shortPlayerName) && line.Contains(shuffle_string)) lastShuffleLineNumber = i;
            return lastShuffleLineNumber;
        }

        private List<string> GetDrawCards(string[] log, string myName)
        {
            string draw_string = "を引いた。";
            var drawCards = new List<string>();
            foreach (var line in log)
            {
                if (line.StartsWith(myName) && line.Contains(draw_string))
                {
                    var cards = ExtractObjectCards(line, myName);
                    drawCards.AddRange(cards);
                }
            }
            return drawCards;
        }

        // 次が空行の行がクリーンアップ。
        // ログの最後の行がクリーンアップの場合は判別できない。したがって任意のタイミングのログで利用できるわけではない。
        // ただしこのメソッドを山札カウントに用いる場合、シャフル直前のログ(logBeforeLastShuffle)を使用するのでこの問題は起こりえない。クリーンナップ直後(ターンの開始)にシャフルをすることはないため。
        private int GetLastCleanupLineNumber(string[] log, string shortPlayerName)
        {
            if (!log.Any(s => s.Contains("ターン 1"))) return 0;   // ターン1より手前
            int lastCleanupLineNumber = log.ToList().LastIndexOf("") - 1;
            if (!log[lastCleanupLineNumber].StartsWith(shortPlayerName))    // 自分のクリーンアップでない場合、その前が自分のクリーンアップ
                lastCleanupLineNumber = log.Take(lastCleanupLineNumber).ToList().LastIndexOf("") - 1;
            if (!log[lastCleanupLineNumber].StartsWith(shortPlayerName)) throw new Exception("GetLastCleanupLineNumber failed");
            return lastCleanupLineNumber;
        }

        // 複数種類のカードのうちのどれかを使用して、あるアクション(捨てる、廃棄するなど)をした時の目的語のカードを抽出する
        // 使用したカードを調べる理由：手札を捨て札(or廃棄)にしたか山札から捨て札(or廃棄)にしたかは使用したカードの種類を調べなければ分からない
        private List<string> ExtractObjectCardsByActions(string[] log, string myName, string[] actionCards, string action_string)
        {
            string use_string = "使用した。";    // 玉座の間による「再使用した。」を含む
            var cards = new List<string>();
            while (log.Any())
            {
                log = log.SkipWhile(l => !(actionCards.Any(l.Contains) && l.Contains(use_string))).ToArray();
                var effectiveLog = log.TakeWhile(l => l != "" || !l.Contains(use_string)).ToArray();    // 使用したカードの有効範囲ログはターン終了か次のカードを使用するまで
                foreach (var line in effectiveLog)
                    if (line.StartsWith(myName) && line.Contains(action_string))
                        cards.AddRange(ExtractObjectCards(line, myName));
                log = log.SkipWhile(l => l != "" || !l.Contains(use_string)).ToArray();
            }
            return cards;
        }

        private List<string> GetDiscardedCardsFromHand(string[] log, string myName)
        {            
            string[] discardActionCards = { // 手札を捨て札にするカード
                "地下貯蔵庫", "書庫", "民兵", "密猟者", "家臣"    // 基本。書庫のログは手札に引いた後捨てるので手札からの捨て札を適用。
                // TODO
            };
            string discard_string = "を捨て札にした。";
            return ExtractObjectCardsByActions(log, myName, discardActionCards, discard_string);
        }

        private List<string> GetTrashedCardsFromHand(string[] log, string myName)
        {
            string[] trashActionCards = { // 手札を廃棄するカード
                "礼拝堂", "鉱山", "金貸し", "改築"     // 基本
                // TODO
            };
            string trash_strings = "を廃棄した。";
            return ExtractObjectCardsByActions(log, myName, trashActionCards, trash_strings);
        }

        // 手札と場の札 = 
        //  前回のクリーンアップフェイズ時以降 引いたカード (実装済み)
        //  + 前回のクリーンアップフェイズ時以降 脇や酒場から手札や場に戻したカード (未実装)
        //  - 前回のクリーンアップフェイズ時以降 手札から捨て札にしたカード  (基本のみ実装済み)
        //  - 前回のクリーンアップフェイズ時以降 手札から廃棄したカード (基本のみ実装済み)
        //  - 前回のクリーンアップフェイズ時以降 手札から山に戻したカード (未実装)
        //  - 前回のクリーンアップフェイズ時以降 脇や酒場に置いたカード (未実装) 
        //  - 前回のクリーンアップフェイズ時以降 相手に渡したカード (未実装) 
        //  + 前回のクリーンアップフェイズ時以降 相手から受け取ったカード (未実装) 
        private List<string> GetHandFieldCards(string[] log, string myName)
        {
            int lastCleanupLineNumber = GetLastCleanupLineNumber(log, myName);
            var logAfterLastCleanup = log.Skip(lastCleanupLineNumber).ToArray();
            var handFieldCards = GetDrawCards(logAfterLastCleanup, myName);
            var discardedCardsFromHand = GetDiscardedCardsFromHand(logAfterLastCleanup, myName);
            foreach (var card in discardedCardsFromHand) handFieldCards.Remove(card);
            var trashedCardsFromHand = GetTrashedCardsFromHand(logAfterLastCleanup, myName);
            foreach (var card in trashedCardsFromHand) handFieldCards.Remove(card);
            // TODO
            return handFieldCards;
        }
        
        // 山札 = 
        //  前回のシャフル時の所持カード (実装済み)
        //  - シャフル時の手札と場の札 (仮実装済み)
        //  - シャフル時の脇カード (未実装)
        //  - シャフル時の酒場カード (未実装)
        //  - シャフル時以降 引いたカード (実装済み)
        //  - シャフル時以降 山札から捨て札にしたカード (未実装)
        //  - シャフル時以降 山札から廃棄したカード (未実装)
        //  + シャフル時以降 山札に戻したカード (未実装)
        //  + シャフル時以降 山札の上に獲得したカード (未実装)
        // 
        // 捨て札に獲得しても、交易場で手札に獲得しても、工匠で山札に獲得しても、「～を獲得した。」であり、獲得先は明記されない。
        // 宝の地図のように「金貨4枚を山札の上に獲得した。」と獲得先が明記されるものもある。
        // 工匠で手札から捨て札にしても、地図職人で山札から見たものを捨て札にしても「～を捨て札にした。」であり、捨てた元は明記されない。
        // これらはアクションの原因となるカードの名前を調べなければカウント出来ない。
        // まずカードの名前を調べる必要がないものを実装する。カードの名前を調べる必要のあるものはあとで追加していく。
        private List<string> GetMyDecks(string[] log, string[] shortPlayerNames, int myTurnNumber)
        {
            var myName = shortPlayerNames[myTurnNumber];
            var lastShuffleLineNumber = GetLastShuffleLineNumber(log, myName);
            var logBeforeLastShuffle = log.Take(lastShuffleLineNumber).ToArray();
            var ownCardAtLastShuffle = GetOwnCards(logBeforeLastShuffle, shortPlayerNames)[myTurnNumber];
            var handFiledCardsAtLastShuffle = GetHandFieldCards(logBeforeLastShuffle, myName);
            var deckCards = new List<string>(ownCardAtLastShuffle);
            foreach (var card in handFiledCardsAtLastShuffle) deckCards.Remove(card);
            var logAfterLastShuffle = log.Skip(lastShuffleLineNumber + 1).ToArray();
            var drawCardsAfterLastShuffle = GetDrawCards(logAfterLastShuffle, myName);
            foreach (var card in drawCardsAfterLastShuffle) deckCards.Remove(card);
            // TODO
            return deckCards;
        }

        // プレイヤ名はターン1開始のログから取得
        private string[] GetPlayerNames(string[] log)
        {
            var playerNames = new List<string>();
            string turn1_string = "ターン 1 - ";
            foreach (var line in log)
            {
                if (line.Contains(turn1_string))
                {
                    playerNames.Add(line.Substring(turn1_string.Length));
                }
            }
            return playerNames.ToArray();
        }

        // 省略名は名前の先頭から一致しない文字が最初に現れるまでの文字列
        private string[] GetShortPlayerNames(string[] names)
        {
            var shortNames = new string[] { "", "" };
            for (int i = 0; shortNames[0] == shortNames[1]; ++i)
            {
                for (int j = 0; j < names.Length; ++j) if (i < names[j].Length) shortNames[j] += names[j][i];
            }
            return shortNames;
        }

        // 自分が先手か後手か。先手なら0、後手なら1。
        private int GetMyTurnNumber(string[] log)
        {
            string draw_string = "を引いた。";
            string card_string = "カード";
            foreach (var line in log)
            {
                if (line.Contains(draw_string))
                {
                    if (line.Contains(card_string)) return 1;
                    else return 0;
                }
            }
            throw new Exception("GetMyTurnNumber failed");
        }

        // 末尾の主語のない文を削除する
        private string[] TrimLog(string[] log, string[] shortPlayerNames)
        {
            var n = log.ToList().FindLastIndex(l => shortPlayerNames.Any(s => l.StartsWith(s + "は")));
            return log.Take(n + 1).ToArray();
        }

        private void button_analyze_Click(object sender, EventArgs e)
        {
            var log = textBox_log.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            int myTurnNumber = GetMyTurnNumber(log);
            var playerNames = GetPlayerNames(log);
            var shortPlayerNames = GetShortPlayerNames(playerNames);
            var label_names = new Label[2] { label_name0, label_name1 };
            var label_names_deck = new Label[2] { label_name0_deck, label_name1_deck };
            for (int i = 0; i < 2; ++i) label_names[i].Text = playerNames[i] + ((i == myTurnNumber) ? " (自分)" : "");
            for (int i = 0; i < 2; ++i) label_names_deck[i].Text = playerNames[i] + ((i == myTurnNumber) ? " (自分)" : "");
            while (!shortPlayerNames.Any(s => log.Last().StartsWith(s + "は"))) log = log.Take(log.Count() - 1).ToArray();
            log = TrimLog(log, shortPlayerNames);

            var ownCards = GetOwnCards(log, shortPlayerNames);
            var label_ownCards = new Label[2] { label_ownCard0, label_ownCard1 };
            for (int i = 0; i < 2; ++i)
            {
                var ownCardsQuery = ownCards[i]
                    .GroupBy(s => s)
                    .Select(g => g.Key + " " + g.Count().ToString() + "枚");
                label_ownCards[i].Text = string.Join(Environment.NewLine, ownCardsQuery);
            }

            var decks = GetMyDecks(log, shortPlayerNames, myTurnNumber);
            var label_decks = new Label[2] { label_deck0, label_deck1 };
            var decksQuery = decks
                .GroupBy(s => s)
                .Select(g => g.Key + " " + g.Count().ToString() + "枚");
            label_decks[myTurnNumber].Text = string.Join(Environment.NewLine, decksQuery);

            // test 手札の表示
            var handFieldCards = GetHandFieldCards(log, shortPlayerNames[myTurnNumber]);
            var handFieldCardsQuery = handFieldCards
                .GroupBy(s => s)
                .Select(g => g.Key + " " + g.Count().ToString() + "枚");
            MessageBox.Show(string.Join(Environment.NewLine, handFieldCardsQuery), "手札と場札");
        }
    }
}