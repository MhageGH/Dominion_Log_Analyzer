using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WindowsFormsApp1
{
    class Extractor
    {
        // ログ1行から主語と動詞と目的語を抽出する
        static public (string name, string action, List<string> cards, string destination, string inParentheses) Extract(string line)
        {
            // line = "Tは銅貨3枚、屋敷2枚を引いた。" → name = "T", l = "銅貨3枚、屋敷2枚を引いた。"
            var l = line;
            int idx = l.IndexOf("は");
            if (idx == -1) return (null, null, null, null, null);
            var name = l.Substring(0, idx);
            l = l.Substring(idx + 1);

            // 修飾の削除
            // l = "停泊所により呪いを脇に置いた。" → l = "呪いを脇に置いた。"
            l = Regex.Replace(l, @".*により", "");

            // リアクションの処理
            // l = "玉璽でリアクションした。" → action = "リアクションした。", cards = {"玉璽"}
            if (l.Contains("リアクションした。"))
            {
                idx = l.IndexOf("で");
                return (name, "リアクションした。", new List<string> { l.Remove(idx) }, null, null);
            }

            // l = "銅貨3枚、屋敷2枚を引いた。" → cards = {"銅貨", "銅貨", "銅貨", "屋敷", "屋敷"}, l = "引いた。"
            idx = l.IndexOf("を");
            if (idx == -1) return (name, null, null, null, null);
            var obj = l.Remove(idx);                                           
            var ss = obj.Split(new string[] { "、" }, StringSplitOptions.None);
            var cards = new List<string>();
            foreach (var s in ss)
            {
                var n = Regex.Replace(s, @"[^0-9]", "");         // 数字を抽出
                if (n == "" || !s.Contains("枚")) cards.Add(s);  // 〇枚がない -> 1枚 or 枚数ではない
                else for (int i = 0; i < int.Parse(n); ++i) cards.Add(s.Remove(s.IndexOf(n)));
            }
            l = l.Substring(idx + 1);

            // l = "酒場マットに置いた。" → destination = "酒場", l = "置いた。"
            string destination;
            if (l == "捨て札にした。") destination = null;  // "捨て札にした。"はまとめて一つの動詞とする
            else
            {
                idx = l.IndexOf("に");                               
                if (idx != -1)
                {
                    destination = l.Remove(idx); 
                    l = l.Substring(idx + 1);
                }
                else destination = null;
            }

            // l = "引いた(隊商)。" → action = "引いた。", inParentheses = "隊商"
            // l = "投機を使用した。 (+$1)" → action = "投機を使用した。", inParentheses = "+$1"
            var action = Regex.Replace(l, @"\(.*\)", "").TrimEnd();
            var inParentheses = Regex.Replace(Regex.Replace(l, @".*\(", ""), @"\).*", "");

            return (name, action, cards, destination, inParentheses);
        }
    }

    class DominionLogAnalyzer
    {
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

        // 所持カードを取得する
        private List<string>[] GetOwnCards(string[] lines, string[] shortPlayerNames)
        {
            string[] get_strings = new string[] { "受け取った。", "獲得した。", "購入・獲得した。", "廃棄置き場から獲得した。" };
            string[] lost_strings = new string[] { "廃棄した。", "戻した。" };
            string give_string = "渡した。";                                    // 仮面舞踏会
            var ownCards = new List<string>[2] { new List<string>(), new List<string>() };
            foreach (var line in lines)
            {
                var (name, action, cards, destination, inParentheses) = Extractor.Extract(line);
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
                    if (lines[i - 1].Contains("山札に混ぜシャッフルした。") && lines[i - 2].Contains("山札をシャッフルした。")) // イベント"寄付"の不自然なログ対応
                        idxs.Add(i - 2);
                    else if (lines[i - 1].Contains("シャッフルした。"))
                        idxs.Add(i - 1);
                    else
                        idxs.Add(i);
                }
                if (lines[i].Contains("手札に加えた。") && lines[i + 1] == "") // イベント"保存"、リアクション"忠犬"
                {
                    if (lines[i - 2].Contains("シャッフルした。")) idxs.Add(i - 2);
                    else idxs.Add(i - 1);
                    lines[i] += "(ターン終了時)";
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

        private void SaveLog(string[] lines)
        {
            var extractedLog = new StringBuilder();
            foreach (var line in lines)
            {
                var (name, action, cards, destination, inParentheses) = Extractor.Extract(line);
                if (name != null) extractedLog.Append(
                    "name = " + name 
                    + "\taction = " + action 
                    + "\tcards = " + string.Join(",", cards)
                    + "\tdestination = " + destination
                    + "\tinParentheses = " + inParentheses
                    + Environment.NewLine);
            }
            using (var sw = new System.IO.StreamWriter("extracted_log.txt")) sw.Write(extractedLog);
            using (var sw = new System.IO.StreamWriter("game_log.txt")) sw.Write(string.Join(Environment.NewLine, lines));
        }

        /// <summary>プレイヤーのフルネーム</summary>
        public string[] playerNames;

        /// <summary>自分の手番</summary>
        public int myTurnNumber;

        /// <summary>所持カード</summary>
        public List<string>[] ownCards;

        /// <summary>自分の山札</summary>
        public List<string> myDeck;

        /// <summary>解析開始</summary>
        /// <param name="text">解析するテキスト</param>
        /// <return>成否</return>
        public bool Run(string text)
        {
            var lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            myTurnNumber = GetMyTurnNumber(lines);
            playerNames = GetPlayerNames(lines);
            var shortPlayerNames = GetShortPlayerNames(playerNames);

            lines = InsertCleanup(lines);
            SaveLog(lines);
            ownCards = GetOwnCards(lines, shortPlayerNames);
            var errorLog = new StringBuilder();
            var lineAnalyzer = new LineAnalyzer(shortPlayerNames, myTurnNumber);
            for (int i = 0; i < lines.Length; ++i)
            {
                try
                {
                    lineAnalyzer.Transact(lines[i]);
                }
                catch (Exception e)
                {
                    errorLog.Append("行番号" + i.ToString() + ": " + e.Message + Environment.NewLine);
                }
            }
            myDeck = lineAnalyzer.GetMyDeck();
            using (var sw = new System.IO.StreamWriter("error_log.txt")) sw.Write(errorLog);
            return errorLog.Length == 0 ? true : false;
        }
    }
}
