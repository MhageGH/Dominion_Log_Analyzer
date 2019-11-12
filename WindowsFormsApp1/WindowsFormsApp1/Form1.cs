using System;
using System.Linq;
using System.Windows.Forms;
using OpenQA.Selenium.Firefox;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        string errorLog = "";

        string gameLog = "";

        FirefoxDriver driver;

        public Form1()
        {
            InitializeComponent();
        }

        // 解析ボタンを押した時のイベント
        private void button_analyze_Click(object sender, EventArgs e)
        {
            var analyzer = new DominionLogAnalyzer();
            var result = analyzer.Run(textBox_log.Text);
            label_message.Text = result ? "エラーなし" : "エラーあり。error_log.txtを見て下さい。";
            button_OutputError.Enabled = result ? false : true;
            this.gameLog = textBox_log.Text;
            this.errorLog = analyzer.errorLog;

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
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                label1.Enabled = false;
                button_analyze.Enabled = false;
                if (driver == null)
                {
                    driver = new FirefoxDriver();
                    driver.Navigate().GoToUrl("https://dominion.games/");
                }
                timer1.Enabled = true;
            }
            else
            {
                label1.Enabled = true;
                button_analyze.Enabled = true;
                timer1.Enabled = false;
            }
        }

        private void button_OutputError_Click(object sender, EventArgs e)
        {
            using (var sw = new System.IO.StreamWriter("game_log.txt")) sw.Write(gameLog);
            using (var sw = new System.IO.StreamWriter("error_log.txt")) sw.Write(errorLog);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                textBox_log.Text = driver.FindElementByClassName("game-log").Text;
                button_analyze_Click(null, null);
                textBox_log.SelectionStart = textBox_log.Text.Length;
                textBox_log.Focus();
                textBox_log.ScrollToCaret();
            }
            catch (Exception) {}
        }
    }
}