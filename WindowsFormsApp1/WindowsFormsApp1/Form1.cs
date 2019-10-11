using System;
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

        // 解析ボタンを押した時のイベント
        private void button_analyze_Click(object sender, EventArgs e)
        {
            var analyzer = new DominionLogAnalyzer();
            analyzer.Run(textBox_log.Text);

            // 名前を表示
            var label_names = new Label[2] { label_name0, label_name1 };
            for (int i = 0; i < 2; ++i) label_names[i].Text = analyzer.playerNames[i] + ((i == analyzer.myTurnNumber) ? " (自分)" : "");
            label_name0_deck.Text = analyzer.playerNames[analyzer.myTurnNumber] + " (自分)";

            // 所持カードを表示
            var label_ownCards = new Label[2] { label_ownCard0, label_ownCard1 };
            for (int i = 0; i < 2; ++i)
            {
                var ownCardsQuery = analyzer.ownCards[i]
                    .GroupBy(s => s)
                    .Select(g => g.Key + " " + g.Count().ToString() + "枚");
                label_ownCards[i].Text = string.Join(Environment.NewLine, ownCardsQuery);
            }

            // 山札を表示
            var decksQuery = analyzer.myDeck
                .GroupBy(s => s)
                .Select(g => g.Key + " " + g.Count().ToString() + "枚");
            label_deck0.Text = string.Join(Environment.NewLine, decksQuery);

            var tester = new Tester(analyzer);
            tester.Run(textBox_log.Text);
        }
    }
}