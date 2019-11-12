namespace WindowsFormsApp1
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.textBox_log = new System.Windows.Forms.TextBox();
            this.button_analyze = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label_ownCard0 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label_name1 = new System.Windows.Forms.Label();
            this.label_name0 = new System.Windows.Forms.Label();
            this.label_ownCard1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label_deck0 = new System.Windows.Forms.Label();
            this.label_name0_deck = new System.Windows.Forms.Label();
            this.label_message = new System.Windows.Forms.Label();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.button_OutputError = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox_log
            // 
            this.textBox_log.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.textBox_log.Location = new System.Drawing.Point(12, 72);
            this.textBox_log.Multiline = true;
            this.textBox_log.Name = "textBox_log";
            this.textBox_log.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox_log.Size = new System.Drawing.Size(387, 436);
            this.textBox_log.TabIndex = 0;
            // 
            // button_analyze
            // 
            this.button_analyze.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.button_analyze.Location = new System.Drawing.Point(12, 514);
            this.button_analyze.Name = "button_analyze";
            this.button_analyze.Size = new System.Drawing.Size(103, 23);
            this.button_analyze.TabIndex = 1;
            this.button_analyze.Text = "ログの解析";
            this.button_analyze.UseVisualStyleBackColor = true;
            this.button_analyze.Click += new System.EventHandler(this.button_analyze_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label1.Location = new System.Drawing.Point(12, 51);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(140, 18);
            this.label1.TabIndex = 2;
            this.label1.Text = "ログを貼り付けて下さい";
            // 
            // label_ownCard0
            // 
            this.label_ownCard0.AutoSize = true;
            this.label_ownCard0.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label_ownCard0.Location = new System.Drawing.Point(6, 39);
            this.label_ownCard0.Name = "label_ownCard0";
            this.label_ownCard0.Size = new System.Drawing.Size(13, 18);
            this.label_ownCard0.TabIndex = 3;
            this.label_ownCard0.Text = "-";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label_name1);
            this.groupBox1.Controls.Add(this.label_name0);
            this.groupBox1.Controls.Add(this.label_ownCard1);
            this.groupBox1.Controls.Add(this.label_ownCard0);
            this.groupBox1.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.groupBox1.Location = new System.Drawing.Point(405, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(383, 496);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "所持カード";
            // 
            // label_name1
            // 
            this.label_name1.AutoSize = true;
            this.label_name1.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label_name1.Location = new System.Drawing.Point(189, 21);
            this.label_name1.Name = "label_name1";
            this.label_name1.Size = new System.Drawing.Size(13, 18);
            this.label_name1.TabIndex = 7;
            this.label_name1.Text = "-";
            // 
            // label_name0
            // 
            this.label_name0.AutoSize = true;
            this.label_name0.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label_name0.Location = new System.Drawing.Point(6, 21);
            this.label_name0.Name = "label_name0";
            this.label_name0.Size = new System.Drawing.Size(13, 18);
            this.label_name0.TabIndex = 6;
            this.label_name0.Text = "-";
            // 
            // label_ownCard1
            // 
            this.label_ownCard1.AutoSize = true;
            this.label_ownCard1.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label_ownCard1.Location = new System.Drawing.Point(189, 39);
            this.label_ownCard1.Name = "label_ownCard1";
            this.label_ownCard1.Size = new System.Drawing.Size(13, 18);
            this.label_ownCard1.TabIndex = 5;
            this.label_ownCard1.Text = "-";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label_deck0);
            this.groupBox2.Controls.Add(this.label_name0_deck);
            this.groupBox2.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.groupBox2.Location = new System.Drawing.Point(794, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(204, 496);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "山札";
            // 
            // label_deck0
            // 
            this.label_deck0.AutoSize = true;
            this.label_deck0.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label_deck0.Location = new System.Drawing.Point(6, 39);
            this.label_deck0.Name = "label_deck0";
            this.label_deck0.Size = new System.Drawing.Size(13, 18);
            this.label_deck0.TabIndex = 6;
            this.label_deck0.Text = "-";
            // 
            // label_name0_deck
            // 
            this.label_name0_deck.AutoSize = true;
            this.label_name0_deck.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label_name0_deck.Location = new System.Drawing.Point(6, 21);
            this.label_name0_deck.Name = "label_name0_deck";
            this.label_name0_deck.Size = new System.Drawing.Size(13, 18);
            this.label_name0_deck.TabIndex = 3;
            this.label_name0_deck.Text = "-";
            // 
            // label_message
            // 
            this.label_message.AutoSize = true;
            this.label_message.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.label_message.Location = new System.Drawing.Point(139, 516);
            this.label_message.Name = "label_message";
            this.label_message.Size = new System.Drawing.Size(13, 18);
            this.label_message.TabIndex = 6;
            this.label_message.Text = "-";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.checkBox1.Location = new System.Drawing.Point(15, 12);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(272, 22);
            this.checkBox1.TabIndex = 7;
            this.checkBox1.Text = "自動更新 (FirefoxでDominion Onlineを起動)";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // button_OutputError
            // 
            this.button_OutputError.Enabled = false;
            this.button_OutputError.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.button_OutputError.Location = new System.Drawing.Point(895, 514);
            this.button_OutputError.Name = "button_OutputError";
            this.button_OutputError.Size = new System.Drawing.Size(103, 23);
            this.button_OutputError.TabIndex = 8;
            this.button_OutputError.Text = "エラーを出力";
            this.button_OutputError.UseVisualStyleBackColor = true;
            this.button_OutputError.Click += new System.EventHandler(this.button_OutputError_Click);
            // 
            // timer1
            // 
            this.timer1.Interval = 5000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1010, 552);
            this.Controls.Add(this.button_OutputError);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.label_message);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button_analyze);
            this.Controls.Add(this.textBox_log);
            this.Name = "Form1";
            this.Text = "Dominion Log Analyzer";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_log;
        private System.Windows.Forms.Button button_analyze;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label_ownCard0;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label_ownCard1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label_name0_deck;
        private System.Windows.Forms.Label label_name1;
        private System.Windows.Forms.Label label_name0;
        private System.Windows.Forms.Label label_deck0;
        private System.Windows.Forms.Label label_message;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Button button_OutputError;
        private System.Windows.Forms.Timer timer1;
    }
}

