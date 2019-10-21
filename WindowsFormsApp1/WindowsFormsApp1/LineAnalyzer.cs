using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApp1
{
    class LineAnalyzer
    {
        // action時のカードの移動元と移動先はstateによって変わる。手札と場の札は区別しない。
        // stateは自分または相手によるカードの使用と購入とクリーンアップによって変わる。
        // normal以外のstateは併用できる。stateで記述していないactionはnormalの挙動が適用される。
        // (購入時も続くstateの有無を確認)
        [Flags]
        private enum state
        {
            // 「獲得した。」「購入・獲得した。」「受け取った。」サプライ→捨て札
            // 「引いた。」、「指定し、的中した。」山札→手札
            // 「捨て札にした。」手札→捨て札
            // 「廃棄した。」手札→廃棄置き場
            // 「呼び出した。」酒場→手札
            // 「置いた。」手札→山札
            // 「渡した。」手札→相手の手札
            // 「戻した。」手札→サプライ
            // 「シャフルした。」捨て札→山札
            // 「クリーンアップした。」手札→捨て札
            // 「開始した。」持続場→手札
            // 「手札に加えた。」山札→手札
            // 「山札に加えた。」手札→山札
            // 「見た。」「公開した。」移動なし
            normal = 1 << 0,

            // 「捨て札にした。」山札→捨て札
            deck_to_discard = 1 << 1,

            // 「廃棄した。」山札→廃棄置き場
            deck_to_trash = 1 << 2,

            // 「置いた。」捨て札→山札
            discard_to_deck = 1 << 3,

            // 「獲得した。」サプライ→手札
            getting_in_hand = 1 << 4,

            // 「獲得した。」サプライ→山札
            getting_on_deck = 1 << 5,

            // 「見た。」山札→手札
            look_to_draw = 1 << 6,

            // "家臣"使用中。
            //「捨て札にした。」で山札を捨て札にした後、捨て札にしたカードと同名のカードを使用した場合は、捨て札にしたカードを手札に戻す。
            // (捨て札にしたカードを使用せずに手札から同名のカードを使用した場合はログから判別できない。)
            vassal = 1 << 7,

            // 「置いた。」手札→持続場
            hand_to_duration = 1 << 8,

            // 「置いた。」手札→島
            hand_to_island = 1 << 9,

            // 「置いた。」山札→原住民の村マット、「手札に加えた。」原住民の村マット→手札
            native_village = 1 << 10,

            // 「置いた。」無効
            no_put = 1 << 11,

            // 「公開した。」山札→手札
            open_to_draw = 1 << 12,

            // 「手札に加えた。」捨て札→手札
            discard_to_hand = 1 << 13,

            // 「廃棄した。」捨て札→廃棄置き場
            discard_to_trash = 1 << 14,

            // ターン開始時
            turn_start = 1 << 15,

            // シャッフル無効
            no_shuffle = 1 << 16,

            // "城塞"廃棄中
            fortress = 1 << 17,

            // "隠遁者"使用中
            // 手札にあれば手札から廃棄し、手札になければ捨て札から廃棄する。
            // 手札にあるが捨て札から廃棄した場合はログから判別できない。
            hermit = 1 << 18,

            // "伝令官"使用中
            // 「公開した。」カードがアクションカードであれば、山札→手札
            herald = 1 << 19,
        };

        private void UseCard(string card)
        {
            // 山札を捨て札にするカード
            string[] deckToDiscardCards = {
                "山賊",                         // 基本 
                "海賊船", "海の妖婆",           // 海辺
                "念視の泉",                     // 錬金術
                "借金", "大衆",                 // 繁栄
                "農村", "狩猟団", "道化師",     // 収穫祭
                "地下墓所", "金物商", "賢者",   // 暗黒
                "助言者", "熟練工",             // ギルド
                "公使",                         // プロモ
            };

            // 山札を廃棄するカード
            string[] deckToTrashCards = {
                "山賊",   // 基本
                "詐欺師", // 陰謀
                "海賊船", // 海辺
                "借金",   // 繁栄
            };

            // 捨て札から山札の上に札を置くカード
            string[] discardToDeckCards = { 
                "前駆者",       // 基本。
                "身代わり",     // 陰謀
                "ゴミあさり",   // 暗黒
            };

            // 手札に獲得するカード
            string[] gettingInHandCards = { 
                "職人", "鉱山",     // 基本
                "拷問人", "交易場", // 陰謀
                "探検家",           // 海辺
                "不正利得",         // 異郷
                "物乞い",           // 暗黒
            };

            // 山札の上に獲得するカード
            string[] gettingOnDeckCards = { 
                "役人",                 // 基本
                "海の妖婆", "宝の地図", // 海辺
                "金貨袋", "馬上槍試合", // 収穫祭
                "開発",                 // 異郷
                "武器庫",               // 暗黒
                "収税吏",               // ギルド
            };

            // 見ることが引くことになることで辻褄が合うログを持つカード。
            string[] lookToDrawCards = { 
                "衛兵",                             // 基本
                "見張り", "航海士", "真珠採り",     // 海辺
                "地図職人", "公爵夫人", "よろずや", // 異郷
                "生存者",                           // 暗黒
            };

            // 酒場に置くカード
            string[] reserveCards = { 
                "法貨", "複製", "案内人", "鼠取り", "御料車", "変容", "ワイン商", "遠隔地", "教師"  // 冒険
            };

            // 持続カード
            string[] durationCards = {
                "隊商", "漁村", "停泊所", "灯台", "商船", "前哨地", "策士", "船着場", // 海辺
                "教会", "船長", // プロモ
            };

            // 永久持続カード
            string[] permanentDurationCards = {
                "王子", // プロモ
            };

            // 手札を持続場に置くカード
            string[] asideCards = {
                "停泊所",       // 海辺
                "王子", "教会", // プロモ
            };

            // 手札を島に置くカード
            string[] islandCards = {
                "島",    // 海辺
            };

            // 原住民の村マットを使うカード
            string[] nativeVillageCards = {
                "原住民の村", // 海辺
            };

            // 置くことを無効にすることで辻褄が合うログを持つカード。
            string[] noPutCards = {
                "薬師", "念視の泉", // 錬金術
                "大衆",             // 繁栄
                "浮浪者",           // 暗黒
            };

            // 公開することが引くことになることで辻褄が合うログを持つカード。
            string[] openToDrawCards = {
                "ゴーレム",       // 錬金術
                "投機",           // 繁栄
                "占い師", "収穫", // 収穫祭
                "義賊", "神託",   // 異郷
                "デイム・アンナ", "デイム・ジョセフィーヌ", "デイム・モリー", "デイム・ナタリー", "デイム・シルビア", // 暗黒
                "サー・ベイリー", "サー・デストリー", "サー・マーチン", "サー・マイケル", "サー・ヴァンデル",         // 暗黒
                "盗賊", "建て直し", "吟遊詩人", // 暗黒
                "医者",           // ギルド
            };

            // 捨て札を手札に入れるカード
            string[] discardToHandCards = {
                "会計所", // 繁栄
            };

            // 隠遁者
            string[] hermitCard = {
                "隠遁者", // 暗黒
            };

            // 伝令官
            string[] heraldCard = {
                "伝令官", //  ギルド
            };

            // 家臣
            string[] vassalCard = {
                "家臣", // 基本
            };

            current_state = 0;
            if (deckToDiscardCards.Any(card.Equals))
                current_state |= state.deck_to_discard;
            if (deckToTrashCards.Any(card.Equals))
                current_state |= state.deck_to_trash;
            if (discardToDeckCards.Any(card.Equals))
                current_state |= state.discard_to_deck;
            if (gettingInHandCards.Any(card.Equals))
                current_state |= state.getting_in_hand;
            if (gettingOnDeckCards.Any(card.Equals))
                current_state |= state.getting_on_deck;
            if (lookToDrawCards.Any(card.Equals))
                current_state |= state.look_to_draw;
            if (vassalCard.Any(card.Equals))
                current_state |= state.vassal;
            if (asideCards.Any(card.Equals))
                current_state |= state.hand_to_duration;
            if (islandCards.Any(card.Equals))
                current_state |= state.hand_to_island;
            if (nativeVillageCards.Any(card.Equals))
                current_state |= state.native_village;
            if (noPutCards.Any(card.Equals))
                current_state |= state.no_put;
            if (openToDrawCards.Any(card.Equals))
                current_state |= state.open_to_draw;
            if (discardToHandCards.Any(card.Equals))
                current_state |= state.discard_to_hand;
            if (hermitCard.Any(card.Equals))
                current_state |= state.hermit;
            if (heraldCard.Any(card.Equals))
                current_state |= state.herald;
            if (current_state == 0)
                current_state = state.normal;

            if (vassalDiscard != null)
            {
                if (card.Equals(vassalDiscard))
                {
                    myHand.Add(card);
                    myDiscard.Remove(card);
                }
                vassalDiscard = null;
            }
            if (reserveCards.Any(card.Equals))
            {
                myBar.Add(card);
                myHand.Remove(card);
            }
            if (durationCards.Any(card.Equals))
            {
                myDuration.Add(card);
                myHand.Remove(card);
            }
            if (permanentDurationCards.Any(card.Equals))
            {
                myPermanentDuration.Add(card);
                myHand.Remove(card);
            }
        }

        // "家臣"によって山札から捨て札にされたカード
        private string vassalDiscard = null;   

        private bool justAfterShuffle = false;

        private int numAtShuffle;

        private state current_state = state.normal;

        private List<string> myBar = new List<string>();

        private List<string> myIsland = new List<string>();

        private List<string> myNativeVillage = new List<string>();

        private List<string> myDuration = new List<string>();

        private List<string> myPermanentDuration = new List<string>();

        private List<string> myDiscard = new List<string>();

        private List<string> myHand = new List<string>();

        private string[] shortPlayerNames;

        private int myTurnNumber;

        private List<string> myDeck = new List<string>();

        private void Remove(ref List<string> removed_cards, List<string> cards, string errorMessage)
        {
            foreach (var card in cards)
            {
                if (removed_cards.Contains(card)) removed_cards.Remove(card);
                else throw new Exception(errorMessage);
            }
        }

        /// <param name="shortPlayerNames">プレイヤ短縮名の配列(手番順)</param>
        /// <param name="myTurnNumber">自分の手番</param>
        public LineAnalyzer(string[] shortPlayerNames, int myTurnNumber)
        {
            this.shortPlayerNames = shortPlayerNames;
            this.myTurnNumber = myTurnNumber;
        }

        /// <summary>自分の山札を取得</summary>
        public List<string> GetMyDeck()
        {
            return myDeck;
        }

        /// <summary>1行を処理する</summary>
        /// <param name="line">行</param>
        public void Transact(string line)
        {
            var myName = shortPlayerNames[myTurnNumber];
            var opponentName = shortPlayerNames[(myTurnNumber + 1) % 2];
            if (line.Contains("前哨地は不発となる。"))
            {
                myHand.Add("前哨地");
                Remove(ref myDuration, new List<string> { "前哨地" }, "持続場に前哨地がありません");
                return;
            }
            var (name, action, cards, destination) = Extractor.Extract(line);
            if (name == myName)
            {
                switch (action)
                {
                    case "購入した。":   // 購入するが獲得しない。獲得ログはこの後発生する。
                        current_state = state.normal;
                        // 伝令官の購入時効果：2019年10月20日現在、この効果で山札に行くカード名が匿名の「カード」となる不具合があるため正常動作しない。
                        if (cards[0] == "伝令官") current_state |= state.discard_to_deck;
                        if (cards[0] == "医者") current_state |= state.look_to_draw;
                        if (cards[0] == "召喚") current_state |= state.getting_in_hand;
                        if (cards[0] == "義賊") current_state |= state.open_to_draw;
                        break;
                    case "購入・獲得した。":
                        current_state = state.normal;
                        if (cards[0] == "遊牧民の野営地") myDeck.AddRange(cards);
                        else myDiscard.AddRange(cards);
                        // 宿屋は獲得時効果で「山札をシャッフルした。」と「〇を山札に混ぜシャッフルした。」の2つのログが現れる
                        // 捨て札を山札に入れる通常シャッフルは行わない
                        if (cards[0] == "宿屋") current_state |= state.no_shuffle;   // 獲得時効果
                        break;
                    case "受け取った。":
                    case "獲得した。":
                    case "廃棄置き場から獲得した。":
                        if (current_state.HasFlag(state.getting_in_hand))
                            myHand.AddRange(cards);
                        else if (current_state.HasFlag(state.getting_on_deck) || cards[0] == "遊牧民の野営地" || destination == "山札の上")
                            myDeck.AddRange(cards);
                        else
                            myDiscard.AddRange(cards);
                        if (cards[0] == "宿屋") current_state |= state.no_shuffle;
                        break;
                    case "シャッフルした。":
                        if (current_state.HasFlag(state.no_shuffle)) break;
                        justAfterShuffle = true;
                        numAtShuffle = myDeck.Count;
                        myDeck.AddRange(myDiscard);
                        myDiscard.Clear();
                        break;
                    case "混ぜシャッフルした。":  // 宿屋
                        if (destination == "山札")
                        {
                            myDeck.AddRange(cards);
                            Remove(ref myDiscard, cards, "混ぜるカードが捨て札にありません。");
                        }
                        current_state ^= state.no_shuffle;
                        break;
                    case "引いた。":
                    case "指定し、的中した。":   // 願いの井戸、秘術師。的中しない場合のテキストは「Tを銅貨を指定したが、香辛料商人が公開された。」のように主語の後が「を」になっていて解析に失敗するが無視しても問題なし。
                        if (justAfterShuffle && numAtShuffle >= cards.Count)
                            throw new Exception("山札が残っているのにシャッフルしました。");
                        justAfterShuffle = false;
                        Remove(ref myDeck, cards, "引くカードが山札にありません。");
                        myHand.AddRange(cards);
                        break;
                    case "見た。":
                        if (current_state.HasFlag(state.look_to_draw))
                        {
                            if (justAfterShuffle && numAtShuffle >= cards.Count)
                                throw new Exception("山札が残っているのにシャッフルしました。");
                            justAfterShuffle = false;
                            Remove(ref myDeck, cards, "引くカードが山札にありません。");
                            myHand.AddRange(cards);
                        }
                        break;
                    case "クリーンアップした。":
                        myDiscard.AddRange(myHand);
                        myHand.Clear();
                        current_state = state.normal;
                        break;
                    case "捨て札にした。":
                        if (current_state.HasFlag(state.deck_to_discard) || current_state.HasFlag(state.vassal))
                        {
                            myDiscard.AddRange(cards);
                            Remove(ref myDeck, cards, "捨てるカードが山札にありません。");
                            if (current_state == state.vassal) vassalDiscard = cards[0];
                        }
                        else
                        {
                            myDiscard.AddRange(cards);
                            Remove(ref myHand, cards, "捨てるカードが手札にありません。");
                        }
                        break;
                    case "廃棄した。":
                        if (cards.Contains("城塞")) current_state |= state.fortress;
                        if (current_state.HasFlag(state.deck_to_trash))
                            Remove(ref myDeck, cards, "廃棄するカードが山札にありません。");
                        else if (current_state.HasFlag(state.discard_to_trash))
                            Remove(ref myDiscard, cards, "廃棄するカードが捨て札にありません。");
                        else if (current_state.HasFlag(state.hermit))
                        {
                            if (myHand.Contains(cards[0])) myHand.Remove(cards[0]);
                            else Remove(ref myDiscard, cards, "廃棄するカードが捨て札にありません。");
                        }
                        else
                            Remove(ref myHand, cards, "廃棄するカードが手札にありません。");
                        break;
                    case "呼び出した。":
                        // TODO
                        // ワイン商は「購入フェイズを終了した」のあと「ワイン商(〇枚)を捨て札にした」のログで酒場から捨て札に戻る。
                        // 直後にクリーンアップなので場に呼び出したと考えても同じ。
                        myHand.AddRange(cards);
                        Remove(ref myBar, cards, "呼び出すカードが酒場にありません。");
                        break;
                    case "置いた。":
                        if (current_state.HasFlag(state.no_put)) break;
                        else if (current_state.HasFlag(state.discard_to_deck) && destination != "捨て札置き場")
                        {
                            myDeck.AddRange(cards);
                            Remove(ref myDiscard, cards, "置くカードが捨て札にありません。");
                        }
                        else if (current_state.HasFlag(state.hand_to_duration) || destination == "脇")
                        {
                            myDuration.AddRange(cards);
                            Remove(ref myHand, cards, "置くカードが手札にありません。");
                        }
                        else if (current_state.HasFlag(state.native_village))
                        {
                            myNativeVillage.AddRange(cards);
                            Remove(ref myDeck, cards, "置くカードが山札にありません。");
                        }
                        else if (destination != "捨て札置き場")
                        {
                            myDeck.AddRange(cards);
                            Remove(ref myHand, cards, "置くカードが手札にありません。");
                        }
                        if (cards[0] == "山札" && destination == "捨て札置き場")
                        {
                            myDiscard.AddRange(myDeck);
                            myDeck.Clear();
                        }
                        break;

                    case "加えた。":
                        if (destination == "山札")
                        {
                            myDeck.AddRange(cards);
                            Remove(ref myHand, cards, "置くカードが手札にありません。");
                        }
                        else if (destination == "手札")
                        {
                            if (current_state.HasFlag(state.native_village))
                            {
                                myHand.AddRange(cards);
                                Remove(ref myNativeVillage, cards, "引くカードが原住民の村マットにありません。");
                            }
                            else if (current_state.HasFlag(state.discard_to_hand))
                            {
                                myHand.AddRange(cards);
                                Remove(ref myDiscard, cards, "引くカードが捨て札にありません。");
                            }
                            else if (current_state.HasFlag(state.turn_start)) break;    // ターン開始時に持続カードと同時に手札に加えているので無視する
                            else if (current_state.HasFlag(state.fortress)) myHand.AddRange(cards); // 城塞を手札に加える
                            else
                            {
                                myHand.AddRange(cards);
                                Remove(ref myDeck, cards, "引くカードが山札にありません。");
                            }
                        }
                        break;
                    case "渡した。":
                        Remove(ref myHand, cards, "渡すカードが手札にありません。");
                        break;
                    case "開始した。":
                        if (cards[0] == "ターン")    // ターンを開始した。
                        {
                            myHand.AddRange(myDuration);
                            myDuration.Clear();
                            current_state |= state.turn_start;
                        }
                        break;
                    case "リアクションした。":
                        if (cards[0] == "玉璽") current_state |= state.discard_to_deck;
                        if (cards[0] == "望楼") current_state |= state.discard_to_deck | state.discard_to_trash;
                        if (cards[0] == "馬商人") current_state |= state.hand_to_duration;
                        if (cards[0] == "愚者の黄金") current_state |= state.getting_on_deck;
                        break;
                    case "公開した。":
                        if (current_state.HasFlag(state.open_to_draw))
                        {
                            myHand.AddRange(cards);
                            Remove(ref myDeck, cards, "引くカードが山札にありません。");
                        }
                        if (current_state.HasFlag(state.herald) && CardList.actionCards.Any(cards[0].Equals))
                        {
                            myHand.AddRange(cards);
                            Remove(ref myDeck, cards, "引くカードが山札にありません。");
                        }
                        break;
                    case "戻した。":
                        Remove(ref myHand, cards, "戻すカードが手札にありません。");
                        break;
                }
            }
            else if (name == opponentName)
            {
                if (action == "渡した。") myHand.AddRange(cards);
            }
            if (action == "使用した。" || action == "再使用した。" || action == "再々使用した。") UseCard(cards[0]);
        }
    }
}
