using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    class Extractor
    {
        // ログ1行から目的語の複数の種類のカードの名前と枚数を抽出してリスト化する
        static public List<string> ExtractObjectCards(string line, string shortPlayerName)
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
    }

    class SequentialAnalyzer
    {
        // 所持カードを取得する
        private List<string>[] GetOwnCards(string[] log, string[] shortPlayerNames)
        {
            string[] get_strings = new string[] { "を受け取った。", "獲得した。" };
            string[] lost_strings = new string[] { "を廃棄した。", "戻した。" };
            string give_string = "渡した。";                                    // 仮面舞踏会
            var ownCards = new List<string>[2] { new List<string>(), new List<string>() };
            foreach (var line in log)
            {
                for (int i = 0; i < 2; ++i)
                {
                    foreach (var get_string in get_strings)
                    {
                        if (line.StartsWith(shortPlayerNames[i]) && line.Contains(get_string))
                        {
                            var cards = Extractor.ExtractObjectCards(line, shortPlayerNames[i]);
                            ownCards[i].AddRange(cards);
                        }
                    }
                    foreach (var lost_string in lost_strings)
                    {
                        if (line.StartsWith(shortPlayerNames[i]) && line.Contains(lost_string))
                        {
                            var cards = Extractor.ExtractObjectCards(line, shortPlayerNames[i]);
                            foreach (var card in cards) ownCards[i].Remove(card);
                        }
                    }
                    if (line.StartsWith(shortPlayerNames[i]) && line.Contains(give_string))
                    {
                        var cards = Extractor.ExtractObjectCards(line, shortPlayerNames[i]);
                        foreach (var card in cards) ownCards[i].Remove(card);
                        int opponent = (i + 1) % 2;
                        ownCards[opponent].AddRange(cards);
                    }
                }
            }
            return ownCards;
        }

        /// <summary>所持カード</summary>
        public List<string>[] ownCards;

        /// <summary>自分の山札</summary>
        public List<string> myDeck;

        public void Run(string[] log, string[] shortPlayerNames, int myTurnNumber)
        {
            ownCards = GetOwnCards(log, shortPlayerNames);
            // TODO
            myDeck = new List<string>();
        }
    }
}
