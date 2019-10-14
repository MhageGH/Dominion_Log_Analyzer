using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    class DominionLogAnalyzer2
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

        // 末尾の主語のない文を削除する
        private string[] TrimLog(string[] log, string[] shortPlayerNames)
        {
            var n = log.ToList().FindLastIndex(l => shortPlayerNames.Any(s => l.StartsWith(s + "は")));
            return log.Take(n + 1).ToArray();
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
            var log = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            myTurnNumber = GetMyTurnNumber(log);
            playerNames = GetPlayerNames(log);
            var shortPlayerNames = GetShortPlayerNames(playerNames);
            log = TrimLog(log, shortPlayerNames);

            var sequentialAnalyzer = new SequentialAnalyzer();
            var result = sequentialAnalyzer.Run(log, shortPlayerNames, myTurnNumber);
            ownCards = sequentialAnalyzer.ownCards;
            myDeck = sequentialAnalyzer.myDeck;
            return result;
        }
    }
}
