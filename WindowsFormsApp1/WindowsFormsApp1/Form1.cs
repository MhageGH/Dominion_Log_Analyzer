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


        // ログ1行から目的語の複数の種類のカードの名前と枚数を抽出してリスト化する
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

        // 所持カードを取得する
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

        // 最後にシャフルした時のログの行番号を取得する
        private int GetLastShuffleLineNumber(string[] log, string shortPlayerName)
        {
            string shuffle_string = "山札をシャッフルした。";
            int lastShuffleLineNumber = 0;
            foreach (var (line, i) in log.Select((line, i) => (line, i)))
                if (line.StartsWith(shortPlayerName) && line.Contains(shuffle_string)) lastShuffleLineNumber = i;
            return lastShuffleLineNumber;
        }

        // 引いたカードを取得する
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

        // 最後にクリーンアップした時のログの行番号を取得する
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
            string use_string = "使用した。";    // 玉座の間による「再使用した。」を含む。使用するのはどちらのプレイヤーでも良い。
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

        // 手札から捨て札にしたカードを取得する
        private List<string> GetDiscardedCardsFromHand(string[] log, string myName)
        {
            string[] discardActionCards = { // 手札を捨て札にするカード
                "地下貯蔵庫", "書庫", "民兵", "密猟者", "家臣"    // 基本。書庫のログは手札に引いた後捨てるので手札からの捨て札を適用。
                // TODO
            };
            string discard_string = "を捨て札にした。";
            return ExtractObjectCardsByActions(log, myName, discardActionCards, discard_string);
        }

        // 山札から捨て札にしたカードを取得する
        // 家臣は山札から捨て札にしたものを場に戻して使用する。しかし場に戻したか次のアクションで手札から出したかはログから判別不可能なため、場に戻す操作は対応しないこととする。
        private List<string> GetDiscardedCardsFromDeck(string[] log, string myName)
        {
            string[] discardActionCards = { // 山札を捨て札にするカード
                "山賊", "衛兵", "家臣",   // 基本 
                // TODO
            };
            string discard_string = "を捨て札にした。";
            return ExtractObjectCardsByActions(log, myName, discardActionCards, discard_string);
        }

        // 手札から廃棄したカードを取得する
        private List<string> GetTrashedCardsFromHand(string[] log, string myName)
        {
            string[] trashActionCards = { // 手札を廃棄するカード
                "礼拝堂", "鉱山", "金貸し", "改築"     // 基本
                // TODO
            };
            string trash_strings = "を廃棄した。";
            return ExtractObjectCardsByActions(log, myName, trashActionCards, trash_strings);
        }

        // 山札から廃棄したカードを取得する
        private List<string> GetTrashedCardsFromDeck(string[] log, string myName)
        {
            string[] trashActionCards = { // 山札を廃棄するカード
                "山賊", "衛兵", // 基本
                // TODO
            };
            string trash_string = "を廃棄した。";
            return ExtractObjectCardsByActions(log, myName, trashActionCards, trash_string);
        }

        // 酒場から呼び出したカードを取得する
        private List<string> GetCalledCard(string[] log, string myName)
        {
            string call_string = "呼び出した。";              // 酒場のカードを使用
            var calledCards = new List<string>();
            foreach (var line in log)
            {
                if (line.StartsWith(myName) && line.Contains(call_string))
                {
                    var cards = ExtractObjectCards(line, myName);
                    calledCards.AddRange(cards);
                }
            }
            // TODO
            // ワイン商は「購入フェイズを終了した」のあと「ワイン商(〇枚)を捨て札にした」のログで酒場から捨て札に戻る。
            // 直後にクリーンアップなので場に呼び出したと考えても同じ。

            return calledCards;
        }

        // 酒場に置いたカードを取得する
        private List<string> GetPutCardOnBar(string[] log, string myName)
        {
            string[] reserveCards = { // 酒場に置くカード
                "法貨", "複製", "案内人", "鼠取り", "御料車", "変容", "ワイン商", "遠隔地", "教師"  // 冒険。リザーブカードは冒険のみ。
            };
            string use_string = "使用した。";
            var putCards = new List<string>();
            foreach (var line in log)
            {
                if (reserveCards.Any(c=>(line.StartsWith(myName) && line.Contains(c) && line.Contains(use_string))))
                {
                    var cards = ExtractObjectCards(line, myName);
                    putCards.AddRange(cards);
                }
            }
            return putCards;
        }

        // 手札から山札に置いたカードを取得する
        private List<string> GetPutCardsOnDeckFromHand(string[] log, string myName)
        {
            string[] putActionCards = { // 手札から山札の上に札を置くカード
                "職人", "役人", // 基本。
                // TODO
            };
            string put_string = "置いた。";
            return ExtractObjectCardsByActions(log, myName, putActionCards, put_string);
        }

        // 捨て札から山札に置いたカードを取得する
        private List<string> GetPutCardsOnDeckFromDiscard(string[] log, string myName)
        {
            string[] putActionCards = { // 捨て札から山札の上に札を置くカード
                "前駆者", // 基本。
                // TODO
            };
            string put_string = "置いた。";
            return ExtractObjectCardsByActions(log, myName, putActionCards, put_string);
        }

        // 手札に獲得したカードを取得する
        private List<String> GetGotCardsToHand(string[] log, string myName)
        {
            string[] getActionCards = { // 手札に獲得するカード
                "職人", "鉱山", // 基本。
                // TODO
            };
            string get_string = "獲得した。";
            return ExtractObjectCardsByActions(log, myName, getActionCards, get_string);

        }

        // 山札に獲得したカードを取得する
        private List<String> GetGotCardsToDeck(string[] log, string myName)
        {
            string[] getActionCards = { // 山札の上に獲得するカード
                "役人",  // 基本。
                // TODO
            };
            string get_string = "獲得した。";
            return ExtractObjectCardsByActions(log, myName, getActionCards, get_string);

        }

        // 酒場の上のカードを取得する
        private List<string> GetCardsOnBar(string[] log, string myname)
        {
            var cardsOnBar = new List<string>();
            // TODO
            return cardsOnBar;
        }

        // 脇のカードを取得する
        private List<string> GetAsideCards(string[] log, string myname)
        {
            var asideCards = new List<string>();
            // TODO
            return asideCards;
        }

        // 脇から手札や場に戻した1ターン持続カードとそのオプションを取得する
        // 持続カードの戻りログは表記に一貫性がない。このためこのログからは判断しない。
        // 1ターン持続カードの場合は「ターンを開始した。」の行があったとき、その前のターンに脇に置いたカードを場と手札に戻す。
        private List<string> GetReturnedOneTurnDurationCards(string[] log, string myname)
        {
            var cards = new List<string>();
            // TODO
            return cards;
        }

        // 脇から手札や場に戻した複数ターン持続カードとそのオプションを取得する
        private List<string> GetReturnedMultipleTurnsDurationCards(string[] log, string myname)
        {
            var cards = new List<string>();
            // TODO
            return cards;
        }

        // 脇から手札や場に戻した原住民の村とそのオプションを取得する
        private List<string> GetReturnedNativeVillage(string[] log, string myname)
        {
            var cards = new List<string>();
            // TODO
            return cards;
        }

        // 相手から受け取ったカードを取得する
        private List<string> GetReceivedCards(string[] log, string myname)
        {
            var cards = new List<string>();
            // TODO
            return cards;
        }

        // 脇に置いた1ターン持続カードとそのオプションを取得する
        private List<string> GetPutOneTurnDurationCards(string[] log, string myname)
        {
            var cards = new List<string>();
            // TODO
            return cards;
        }

        // 脇に置いた複数ターン持続カードとそのオプションを取得する
        private List<string> GetPutMultipleTurnsDurationCards(string[] log, string myname)
        {
            var cards = new List<string>();
            // TODO
            return cards;
        }

        // 脇に置いた原住民の村とそのオプションを取得する
        private List<string> GetPutNativeVillage(string[] log, string myname)
        {
            var cards = new List<string>();
            // TODO
            return cards;
        }

        // 相手に渡したカードを取得する
        private List<string> GetGivenCards(string[] log, string myname)
        {
            var cards = new List<string>();
            // TODO
            return cards;
        }

        // 手札からサプライの山に戻したカードを取得する
        private List<string> GetReturnedCardsOnSupplyDeckFromHand(string[] log, string myname)
        {
            var cards = new List<string>();
            // TODO
            return cards;
        }

        // 手札と場の札 = 
        //  前回のクリーンアップフェイズ時以降 引いたカード
        //  + 前回のクリーンアップフェイズ時以降 酒場から呼び出したカード
        //  + 前回のクリーンアップフェイズ時以降 脇から手札や場に戻した1ターン持続カードとそのオプション
        //  + 前回のクリーンアップフェイズ時以降 脇から手札や場に戻した複数ターン持続カードとそのオプション
        //  + 前回のクリーンアップフェイズ時以降 脇から手札や場に戻した原住民の村とそのオプション
        //  + 前回のクリーンアップフェイズ時以降 相手から受け取ったカード
        //  + 前回のクリーンアップフェイズ時以降 手札に獲得したカード
        //  - 前回のクリーンアップフェイズ時以降 手札から捨て札にしたカード
        //  - 前回のクリーンアップフェイズ時以降 手札から廃棄したカード
        //  - 前回のクリーンアップフェイズ時以降 手札から山に置いたカード
        //  - 前回のクリーンアップフェイズ時以降 酒場に置いたカード
        //  - 前回のクリーンアップフェイズ時以降 脇に置いた1ターン持続カードとそのオプション
        //  - 前回のクリーンアップフェイズ時以降 脇に置いた複数ターン持続カードとそのオプション
        //  - 前回のクリーンアップフェイズ時以降 脇に置いた原住民の村とそのオプション
        //  - 前回のクリーンアップフェイズ時以降 相手に渡したカード
        //  - 前回のクリーンアップフェイズ時以降 手札からサプライの山に戻したカード
        private List<string> GetHandCardsAndFieldCards(string[] log, string myName)
        {
            int lastCleanupLineNumber = GetLastCleanupLineNumber(log, myName);
            var logAfterLastCleanup = log.Skip(lastCleanupLineNumber).ToArray();
            var handCardsAndFieldCards = GetDrawCards(logAfterLastCleanup, myName);

            var cards = GetCalledCard(logAfterLastCleanup, myName);
            foreach (var card in cards) handCardsAndFieldCards.Add(card);

            cards = GetReturnedOneTurnDurationCards(logAfterLastCleanup, myName);
            foreach (var card in cards) handCardsAndFieldCards.Add(card);

            cards = GetReturnedMultipleTurnsDurationCards(logAfterLastCleanup, myName);
            foreach (var card in cards) handCardsAndFieldCards.Add(card);

            cards = GetReturnedNativeVillage(logAfterLastCleanup, myName);
            foreach (var card in cards) handCardsAndFieldCards.Add(card);

            cards = GetReceivedCards(logAfterLastCleanup, myName);
            foreach (var card in cards) handCardsAndFieldCards.Add(card);

            cards = GetGotCardsToHand(logAfterLastCleanup, myName);
            foreach (var card in cards) handCardsAndFieldCards.Add(card);

            cards = GetDiscardedCardsFromHand(logAfterLastCleanup, myName);
            foreach (var card in cards) handCardsAndFieldCards.Remove(card);

            cards = GetTrashedCardsFromHand(logAfterLastCleanup, myName);
            foreach (var card in cards) handCardsAndFieldCards.Remove(card);

            cards = GetPutCardsOnDeckFromHand(logAfterLastCleanup, myName);
            foreach (var card in cards) handCardsAndFieldCards.Remove(card);

            cards = GetPutCardOnBar(logAfterLastCleanup, myName);
            foreach (var card in cards) handCardsAndFieldCards.Remove(card);

            cards = GetPutOneTurnDurationCards(logAfterLastCleanup, myName);
            foreach (var card in cards) handCardsAndFieldCards.Remove(card);

            cards = GetPutMultipleTurnsDurationCards(logAfterLastCleanup, myName);
            foreach (var card in cards) handCardsAndFieldCards.Remove(card);

            cards = GetPutNativeVillage(logAfterLastCleanup, myName);
            foreach (var card in cards) handCardsAndFieldCards.Remove(card);

            cards = GetGivenCards(logAfterLastCleanup, myName);
            foreach (var card in cards) handCardsAndFieldCards.Remove(card);

            cards = GetReturnedCardsOnSupplyDeckFromHand(logAfterLastCleanup, myName);
            foreach (var card in cards) handCardsAndFieldCards.Remove(card);

            return handCardsAndFieldCards;
        }

        // 山札 = 
        //  前回のシャフル時の所持カード
        //  - 前回のシャフル時の手札と場の札
        //  - 前回のシャフル時の脇にあるカード
        //  - 前回のシャフル時の酒場にあるカード
        //  + 前回のシャフル時以降 手札から山札に置いたカード
        //  + 前回のシャフル時以降 捨て札から山札に置いたカード
        //  + 前回のシャフル時以降 山札の上に獲得したカード
        //  - 前回のシャフル時以降 引いたカード
        //  - 前回のシャフル時以降 山札から捨て札にしたカード
        //  - 前回のシャフル時以降 山札から廃棄したカード
        private List<string> GetMyDecks(string[] log, string[] shortPlayerNames, int myTurnNumber)
        {
            var myName = shortPlayerNames[myTurnNumber];
            var lastShuffleLineNumber = GetLastShuffleLineNumber(log, myName);
            var logBeforeLastShuffle = log.Take(lastShuffleLineNumber).ToArray();
            var logAfterLastShuffle = log.Skip(lastShuffleLineNumber + 1).ToArray();
            var ownCardAtLastShuffle = GetOwnCards(logBeforeLastShuffle, shortPlayerNames)[myTurnNumber];
            var deckCards = new List<string>(ownCardAtLastShuffle);

            var cards = GetHandCardsAndFieldCards(logBeforeLastShuffle, myName);
            foreach (var card in cards) deckCards.Remove(card);

            cards = GetAsideCards(logBeforeLastShuffle, myName);
            foreach (var card in cards) deckCards.Remove(card);

            cards = GetCardsOnBar(logBeforeLastShuffle, myName);
            foreach (var card in cards) deckCards.Remove(card);

            cards = GetPutCardsOnDeckFromHand(logAfterLastShuffle, myName);
            foreach (var card in cards) deckCards.Add(card);

            cards = GetPutCardsOnDeckFromDiscard(logAfterLastShuffle, myName);
            foreach (var card in cards) deckCards.Add(card);

            cards = GetGotCardsToDeck(logAfterLastShuffle, myName);
            foreach (var card in cards) deckCards.Add(card);

            cards = GetDrawCards(logAfterLastShuffle, myName);
            foreach (var card in cards) deckCards.Remove(card);

            cards = GetDiscardedCardsFromDeck(logAfterLastShuffle, myName);
            foreach (var card in cards) deckCards.Remove(card);

            cards = GetTrashedCardsFromDeck(logAfterLastShuffle, myName);
            foreach (var card in cards) deckCards.Remove(card);

            return deckCards;
        }

        // プレイヤー名を取得
        // ターン1開始のログから確認する
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

        // プレイヤー名の省略名を取得
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

        // 自分の手番を取得
        // 先手なら0、後手なら1。
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

        // 解析ボタンを押した時のイベント
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
            //var handFieldCards = GetHandCardsAndFieldCards(log, shortPlayerNames[myTurnNumber]);
            //var handFieldCardsQuery = handFieldCards
            //    .GroupBy(s => s)
            //    .Select(g => g.Key + " " + g.Count().ToString() + "枚");
            //MessageBox.Show(string.Join(Environment.NewLine, handFieldCardsQuery), "手札と場札");
        }
    }
}