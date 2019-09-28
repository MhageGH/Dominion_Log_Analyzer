using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // ログ1行から目的語のカードの名前と枚数を抽出
        private (string name, int num) ExtractObjectCard(string line, string shortPlayerName)
        {
            var s = line.Substring((shortPlayerName + "は").Length);
            s = s.Remove(s.IndexOf("を"));                                            // 例：文字列 "金貨"、文字列 "金貨3枚"
            var n = System.Text.RegularExpressions.Regex.Replace(s, @"[^0-9]", "");   // 数字を抽出
            if (n == "") return (s, 1);                                               // 枚数がない->1枚
            else return (s.Remove(s.IndexOf(n)), Convert.ToInt32(n));
        }

        private Dictionary<string, int>[] GetOwnCards(string[] log, string[] shortPlayerNames)
        {
            string[] get_string = new string[] { "を受け取った。", "を購入・獲得した。", "を獲得した。" };    // TODO トラベラー、仮面、手札に獲得、山札に獲得
            string discard_string = "を廃棄した。";   // TODO 山札から廃棄 (山札から公開⇒通常廃棄？)
            string back_string = "の山に戻した。";
            var ownCards = new Dictionary<string, int>[2] { new Dictionary<string, int>(), new Dictionary<string, int>() };
            foreach (var line in log)
            {
                for (int i = 0; i < 2; ++i)
                {
                    for (int j = 0; j < get_string.Length; ++j)
                    {
                        if (line.StartsWith(shortPlayerNames[i]) && line.Contains(get_string[j]))
                        {
                            var card = ExtractObjectCard(line, shortPlayerNames[i]);
                            if (ownCards[i].ContainsKey(card.name)) ownCards[i][card.name] += card.num;
                            else ownCards[i].Add(card.name, card.num);
                        }
                    }
                    if (line.StartsWith(shortPlayerNames[i]) && line.Contains(discard_string))
                    {
                        var card = ExtractObjectCard(line, shortPlayerNames[i]);
                        if (ownCards[i].ContainsKey(card.name)) ownCards[i][card.name] -= card.num;
                        else MessageBox.Show("Discard failed!");
                    }
                    if (line.StartsWith(shortPlayerNames[i]) && line.Contains(back_string))
                    {
                        var card = ExtractObjectCard(line, shortPlayerNames[i]);
                        if (ownCards[i].ContainsKey(card.name)) ownCards[i][card.name] -= card.num;
                        else MessageBox.Show("back failed!");
                    }
                }
            }
            return ownCards;
        }

        // 山札 = 前回のシャフル時のデッキ - シャフル時の手札 - シャフル時の場のカード - シャフル時の脇カード - シャフル時の酒場カード 
        // - シャフル時以降引いたカード - シャフル時以降山札から捨て札にしたカード  - シャフル時以降山札から廃棄したカード
        // + シャフル時以降山札に戻したカード + シャフル時以降山札の上に獲得したカード
        private void ShowDecks()
        {
            string bar_string = "を酒場マットに置いた。";
            string shuffle_string = "山札をシャッフルした。";
            // TODO
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

        private void button1_Click(object sender, EventArgs e)
        {
            var log = textBox_log.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            var playerNames = GetPlayerNames(log);
            var shortPlayerNames = GetShortPlayerNames(playerNames);
            var label_names = new Label[2] { label_name0, label_name1 };
            for (int i = 0; i < 2; ++i) label_names[i].Text = playerNames[i];

            var ownCards = GetOwnCards(log, shortPlayerNames);
            var label_ownCards = new Label[2] { label_ownCard0, label_ownCard1 };
            for (int i = 0; i < 2; ++i)
            {
                label_ownCards[i].Text = "";
                foreach (var card in ownCards[i]) if (card.Value != 0) label_ownCards[i].Text += card.Key + " " + card.Value.ToString() + "枚" + Environment.NewLine;
            }
            ShowDecks();
        }
    }
}