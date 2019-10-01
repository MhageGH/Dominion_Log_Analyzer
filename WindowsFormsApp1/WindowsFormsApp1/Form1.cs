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
                    for (int j = 0; j < get_strings.Length; ++j)
                    {
                        if (line.StartsWith(shortPlayerNames[i]) && line.Contains(get_strings[j]))
                        {
                            var cards = ExtractObjectCards(line, shortPlayerNames[i]);
                            ownCards[i].AddRange(cards);
                        }
                    }
                    for (int j = 0; j < lost_strings.Length; ++j)
                    {
                        if (line.StartsWith(shortPlayerNames[i]) && line.Contains(lost_strings[j]))
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
            for (int i = 0; i < log.Length; ++i)
            {
                var line = log[i];
                if (line.StartsWith(shortPlayerName) && line.Contains(shuffle_string)) lastShuffleLineNumber = i;
            }
            return lastShuffleLineNumber;
        }

        private List<string> GetDrawCards(string[] log, string shortPlayerName)
        {
            string draw_string = "を引いた。";
            var drawCards = new List<string>();
            foreach (var line in log)
            {
                if (line.StartsWith(shortPlayerName) && line.Contains(draw_string))
                {
                    var cards = ExtractObjectCards(line, shortPlayerName);
                    drawCards.AddRange(cards);
                }
            }
            return drawCards;
        }

        // 山札 = 前回のシャフル時の所持カード - シャフル時の手札 - シャフル時の場のカード - シャフル時の脇カード - シャフル時の酒場カード 
        // - シャフル時以降引いたカード - シャフル時以降山札から捨て札にしたカード  - シャフル時以降山札から廃棄したカード
        // + シャフル時以降山札に戻したカード + シャフル時以降山札の上に獲得したカード
        // 
        // 捨て札に獲得しても、交易場で手札に獲得しても、工匠で山札に獲得しても、「～を獲得した。」であり、獲得先は明記されない。
        // 宝の地図のように「金貨4枚を山札の上に獲得した。」と獲得先が明記されるものもある。
        // 工匠で手札から捨て札にしても、地図職人で山札から見たものを捨て札にしても「～を捨て札にした。」であり、捨てた元は明記されない。
        // これらはアクションの原因となるカードの名前を調べなければカウント出来ない。
        // まずカードの名前を調べる必要がないものを実装する。カードの名前を調べる必要のあるものはあとで追加していく。
        private List<string> GetMyDecks(string[] log, string[] shortPlayerNames, int firstOrSecond)
        {
            var shortPlayerName = shortPlayerNames[firstOrSecond];
            int lastShuffleLineNumber = GetLastShuffleLineNumber(log, shortPlayerName);
            var logBeforeLastShuffle = new string[lastShuffleLineNumber];
            for (int i = 0; i < logBeforeLastShuffle.Length; ++i) logBeforeLastShuffle[i] = log[i];
            var ownCardAtLastShuffle = GetOwnCards(logBeforeLastShuffle, shortPlayerNames)[firstOrSecond];
            var logAfterLastShuffle = new string[log.Length - lastShuffleLineNumber - 1];
            for (int i = 0; i < logAfterLastShuffle.Length; ++i) logAfterLastShuffle[i] = log[lastShuffleLineNumber + 1 + i];
            var drawCards = GetDrawCards(logAfterLastShuffle, shortPlayerName);
            var deckCards = new List<string>(ownCardAtLastShuffle);
            foreach (var drawCard in drawCards) deckCards.Remove(drawCard);
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
        private int GetFirstOrSecond(string[] log)
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
            throw new Exception("GetFirstOrSecond failed");
        }

        private void button_analyze_Click(object sender, EventArgs e)
        {
            var log = textBox_log.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            int firstOrSecond = GetFirstOrSecond(log);
            var playerNames = GetPlayerNames(log);
            var shortPlayerNames = GetShortPlayerNames(playerNames);
            var label_names = new Label[2] { label_name0, label_name1 };
            var label_names_deck = new Label[2] { label_name0_deck, label_name1_deck };
            for (int i = 0; i < 2; ++i) label_names[i].Text = playerNames[i] + ((i == firstOrSecond) ? " (自分)" : "");
            for (int i = 0; i < 2; ++i) label_names_deck[i].Text = playerNames[i] + ((i == firstOrSecond) ? " (自分)" : "");

            var ownCards = GetOwnCards(log, shortPlayerNames);
            var label_ownCards = new Label[2] { label_ownCard0, label_ownCard1 };
            for (int i = 0; i < 2; ++i)
            {
                var ownCardsQuery = ownCards[i]
                    .GroupBy(s => s)
                    .Select(g => g.Key + " " + g.Count().ToString() + "枚");
                label_ownCards[i].Text = string.Join(Environment.NewLine, ownCardsQuery);
            }

            var decks = GetMyDecks(log, shortPlayerNames, firstOrSecond);
            var label_decks = new Label[2] { label_deck0, label_deck1 };
            var decksQuery = decks
                .GroupBy(s => s)
                .Select(g => g.Key + " " + g.Count().ToString() + "枚");
            label_decks[firstOrSecond].Text = string.Join(Environment.NewLine, decksQuery);
        }
    }
}