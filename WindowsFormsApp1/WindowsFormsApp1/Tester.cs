using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    // DominionLogAnalyzerクラスをテストするクラス
    // ドロー時にテスト
    // シャフル時にテスト
    class Tester
    {
        private DominionLogAnalyzer analyzer;

        public Tester(DominionLogAnalyzer analyzer)
        {
            this.analyzer = analyzer;
        }

        private string myName;

        // ターン3以降で自分がドローしたときのログ番号のリストを取得
        private List<int> GetLogNumbersAtMyDraw(string[] log)
        {
            string turn3_string = "ターン 3 - ";
            string draw_string = "を引いた。";
            var n = log.ToList().FindIndex(l => l.Contains(turn3_string));
            var logNumbers = new List<int>();
            for (int i = n; i < log.Count(); ++i)
                if (log[i].Contains(myName) && log[i].Contains(draw_string))
                    logNumbers.Add(i);
            return logNumbers;
        }

        // 山札にないカードをドローしたり、ドロー分の山札があるのにシャフルをしたときはメッセージボックスで知らせる
        public void Run(string text)
        {
            analyzer.Run(text);
            myName = analyzer.playerNames[analyzer.myTurnNumber];
            var log = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            var logNumbers = GetLogNumbersAtMyDraw(log);
            foreach (var n in logNumbers)
            {
                log = log.Take(n + 1).ToArray();    // ドロー以前のログ
            }
        }
    }
}
