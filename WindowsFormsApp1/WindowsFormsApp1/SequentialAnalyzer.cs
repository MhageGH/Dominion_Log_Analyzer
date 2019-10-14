using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    class Extractor
    {
        // ログ1行から主語と動詞と目的語を抽出する
        static public (string name, string action, List<string> cards, string destination) Extract(string line)
        {                                                                       // 例："Tは銅貨3枚、屋敷2枚を引いた。"
            var l = line;
            int idx = l.IndexOf("は");
            if (idx == -1) return (null, null, null, null);
            var name = l.Substring(0, idx);
            l = l.Substring(idx + 1);                                     // 例："銅貨3枚、屋敷2枚を引いた。"

            idx = l.IndexOf("を");
            if (idx == -1) return (name, null, null, null);
            var obj = l.Remove(idx);                                           // 例："銅貨3枚、屋敷2枚"
            var ss = obj.Split(new string[] { "、" }, StringSplitOptions.None);   // 例：{"銅貨3枚", "屋敷2枚"}
            var cards = new List<string>();
            foreach (var s in ss)
            {
                var n = System.Text.RegularExpressions.Regex.Replace(s, @"[^0-9]", "");         // 数字を抽出
                if (n == "" || !s.Contains("枚")) cards.Add(s);                                 // 〇枚がない -> 1枚 or 枚数ではない
                else for (int i = 0; i < int.Parse(n); ++i) cards.Add(s.Remove(s.IndexOf(n)));  // 例： リスト {"銅貨", "銅貨", "銅貨"}
            }
            l = l.Substring(idx + 1);                                     // 例："引いた。"

            string destination;
            if (l == "捨て札にした。") destination = null;           // "捨て札にした。"はまとめて一つの動詞とする
            else
            {
                idx = l.IndexOf("に");                               // 例："酒場マットに置いた。"
                if (idx != -1)
                {
                    destination = l.Remove(idx);                     // 例："酒場マット"
                    l = l.Substring(idx + 1);                        // 例："置いた。"
                }
                else destination = null;
            }

            var action = l;
            return (name, action, cards, destination);
        }
    }

    class SequentialAnalyzer
    {
        // 所持カードを取得する
        private List<string>[] GetOwnCards(string[] lines, string[] shortPlayerNames)
        {
            string[] get_strings = new string[] { "受け取った。", "獲得した。", "購入・獲得した。" };
            string[] lost_strings = new string[] { "廃棄した。", "戻した。" };
            string give_string = "渡した。";                                    // 仮面舞踏会
            var ownCards = new List<string>[2] { new List<string>(), new List<string>() };
            foreach (var line in lines)
            {
                var (name, action, cards, destination) = Extractor.Extract(line);
                for (int i = 0; i < 2; ++i)
                {
                    if (name == shortPlayerNames[i])
                    {
                        if (get_strings.Any(s => s == action)) ownCards[i].AddRange(cards);
                        if (lost_strings.Any(s => s == action)) foreach (var card in cards) ownCards[i].Remove(card);
                        if (give_string == action)
                        {
                            foreach (var card in cards) ownCards[i].Remove(card);
                            int opponent = (i + 1) % 2;
                            ownCards[opponent].AddRange(cards);
                        }
                    }
                }
            }
            return ownCards;
        }

        private string[] InsertCleanup(string[] lines)
        {
            if (lines.Any(l => l.Contains("クリーンアップした。"))) return lines;
            var idxs = new List<int>();
            int n = lines.ToList().FindIndex(l => l.Contains("ターン 1"));
            for (int i = n; i < lines.Length - 1; ++i)
            {
                if (lines[i].Contains("引いた。") && lines[i + 1] == "")
                {
                    if (lines[i - 1].Contains("シャッフルした。")) idxs.Add(i - 1);
                    else idxs.Add(i);
                }
            }
            var lines_list = lines.ToList();
            for (int i = 0; i < idxs.Count; ++i)
            {
                var name = Extractor.Extract(lines_list[idxs[i] + i]).name;
                lines_list.Insert(idxs[i] + i, name + "はカードをクリーンアップした。");
            }
            return lines_list.ToArray();
        }

        private void SaveHistory(string[] lines)
        {
            var extractedLog = new StringBuilder();
            foreach (var line in lines)
            {
                var (name, action, cards, destination) = Extractor.Extract(line);
                if (name != null) extractedLog.Append("name = " + name + "\taction = " + action + "\tcards = " + string.Join(",", cards) + "\tdestination = " + destination + Environment.NewLine);
            }
            using (var sw = new System.IO.StreamWriter("extracted_log.txt")) sw.Write(extractedLog);
            using (var sw = new System.IO.StreamWriter("log.txt")) sw.Write(string.Join(Environment.NewLine, lines));
        }

        /// <summary>所持カード</summary>
        public List<string>[] ownCards;

        /// <summary>自分の山札</summary>
        public List<string> myDeck;

        /// <summary>解析開始</summary>
        /// <param name="lines">解析する行の配列</param>
        /// <param name="shortPlayerNames">プレイヤ短縮名の配列(手番順)</param>
        /// <param name="myTurnNumber">自分の手番</param>
        public void Run(string[] lines, string[] shortPlayerNames, int myTurnNumber)
        {
            lines = InsertCleanup(lines);

            // test
            SaveHistory(lines);

            // temp
            ownCards = GetOwnCards(lines, shortPlayerNames);

            var errorLog = new StringBuilder();
            var analyzer = new LineAnalyzer();
            for (int i = 0; i < lines.Length; ++i)
            {
                try
                {
                    analyzer.Run(lines[i], shortPlayerNames, myTurnNumber);
                }
                catch (Exception e)
                {
                    errorLog.Append("行番号" + i.ToString() + ": " + e.Message + Environment.NewLine);
                }
            }
            myDeck = analyzer.myDeck;
            using (var sw = new System.IO.StreamWriter("error_log.txt")) sw.Write(errorLog);
        }
    }
}
