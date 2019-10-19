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
        // (闇市場要確認。購入時も続くstateの有無を確認)
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
            discarding_deck = 1 << 1,

            // 「廃棄した。」山札→廃棄置き場
            trashing_deck = 1 << 2,

            // 「置いた。」捨て札→山札
            putting_from_discard = 1 << 3,

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
            putting_to_duration = 1 << 8,

            // 「置いた。」手札→島
            putting_to_island = 1 << 9,

            // 「置いた。」山札→原住民の村マット、「手札に加えた。」原住民の村マット→手札
            native_village = 1 << 10,

            // 「置いた。」無効
            no_put = 1 << 11,

            // 「公開した。」山札→手札
            open_to_draw = 1 << 12,
        };


        private void UseCard(string card)
        {
            // 山札を捨て札にするカード
            string[] discardingDeckCards = { 
                "山賊", // 基本 
                "海賊船", "海の妖婆", // 海辺
                "念視の泉", // 錬金術
            };

            // 山札を廃棄するカード
            string[] trashingDeckCards = { 
                "山賊",   // 基本
                "詐欺師", // 陰謀
                "海賊船", // 海辺
            };

            // 捨て札から山札の上に札を置くカード
            string[] puttingFromDiscardCards = { 
                "前駆者", // 基本。
                "身代わり", // 陰謀
            };

            // 手札に獲得するカード
            string[] gettingInHandCards = { 
                "職人", "鉱山", // 基本
                "拷問人", "交易場", // 陰謀
                "探検家", // 海辺
            };

            // 山札の上に獲得するカード
            string[] gettingOnDeckCards = { 
                "役人",     // 基本
                "海の妖婆", "宝の地図", // 海辺
            };

            // 見ることが引くことになることで辻褄が合うログを持つカード。
            string[] lookToDrawCards = { 
                "衛兵",  // 基本
                "見張り", "航海士", "真珠採り", // 海辺
            };

            // 酒場に置くカード
            string[] reserveCards = { 
                "法貨", "複製", "案内人", "鼠取り", "御料車", "変容", "ワイン商", "遠隔地", "教師"  // 冒険
            };

            // 持続カード
            string[] durationCards = {
                "隊商", "漁村", "停泊所", "灯台", "商船", "前哨地", "策士", "船着場", // 海辺
            };

            // 手札を持続場に置くカード
            string[] asideCards = {
                "停泊所",  // 海辺
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
            };

            // 公開することが引くことになることで辻褄が合うログを持つカード。
            string[] openToDrawCards = {
                "ゴーレム", // 錬金術
            };

            current_state = 0;
            if (discardingDeckCards.Any(card.Equals))
                current_state |= state.discarding_deck;
            if (trashingDeckCards.Any(card.Equals))
                current_state |= state.trashing_deck;
            if (puttingFromDiscardCards.Any(card.Equals))
                current_state |= state.putting_from_discard;
            if (gettingInHandCards.Any(card.Equals))
                current_state |= state.getting_in_hand;
            if (gettingOnDeckCards.Any(card.Equals))
                current_state |= state.getting_on_deck;
            if (lookToDrawCards.Any(card.Equals))
                current_state |= state.look_to_draw;
            if (card.Equals("家臣"))
                current_state = state.vassal;
            if (asideCards.Any(card.Equals))
                current_state |= state.putting_to_duration;
            if (islandCards.Any(card.Equals))
                current_state |= state.putting_to_island;
            if (nativeVillageCards.Any(card.Equals))
                current_state |= state.native_village;
            if (noPutCards.Any(card.Equals))
                current_state |= state.no_put;
            if (openToDrawCards.Any(card.Equals))
                current_state |= state.open_to_draw;
            if (current_state == 0)
                current_state = state.normal;

            if (card.Equals(vassalDiscard))
            {
                myHand.Add(card);
                myDiscard.Remove(card);
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
        }

        // "家臣"によって山札から捨て札にされたカード
        private string vassalDiscard;   

        private bool justAfterShuffle = false;

        private int numAtShuffle;

        private state current_state = state.normal;

        private List<string> myBar = new List<string>();

        private List<string> myIsland = new List<string>();

        private List<string> myNativeVillage = new List<string>();

        private List<string> myDuration = new List<string>();

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
                    case "購入・獲得した。":
                        current_state = state.normal;
                        myDiscard.AddRange(cards);
                        break;
                    case "受け取った。":
                    case "獲得した。":
                        if (current_state.HasFlag(state.getting_in_hand))
                            myHand.AddRange(cards);
                        else if (current_state.HasFlag(state.getting_on_deck))
                            myDeck.AddRange(cards);
                        else
                            myDiscard.AddRange(cards);
                        break;
                    case "シャッフルした。":
                        justAfterShuffle = true;
                        numAtShuffle = myDeck.Count;
                        myDeck.AddRange(myDiscard);
                        myDiscard.Clear();
                        break;
                    case "引いた。":
                    case "指定し、的中した。":   // 願いの井戸
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
                        if (current_state.HasFlag(state.discarding_deck) || current_state.HasFlag(state.vassal))
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
                        if (current_state.HasFlag(state.trashing_deck))
                            Remove(ref myDeck, cards, "廃棄するカードが山札にありません。");
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
                        else if (current_state.HasFlag(state.putting_from_discard))
                        {
                            myDeck.AddRange(cards);
                            Remove(ref myDiscard, cards, "置くカードが捨て札にありません。");
                        }
                        else if (current_state.HasFlag(state.putting_to_duration))
                        {
                            myDuration.AddRange(cards);
                            Remove(ref myHand, cards, "置くカードが手札にありません。");
                        }
                        else if (current_state.HasFlag(state.native_village))
                        {
                            myNativeVillage.AddRange(cards);
                            Remove(ref myDeck, cards, "置くカードが山札にありません。");
                        }
                        else
                        {
                            myDeck.AddRange(cards);
                            Remove(ref myHand, cards, "置くカードが手札にありません。");
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
                        myHand.AddRange(myDuration);
                        myDuration.Clear();
                        break;
                }
            }
            else if (name == opponentName)
            {
                if (action == "渡した。") myHand.AddRange(cards);
            }
            if (action == "使用した。" || action == "再使用した。") UseCard(cards[0]);
        }
    }
}
