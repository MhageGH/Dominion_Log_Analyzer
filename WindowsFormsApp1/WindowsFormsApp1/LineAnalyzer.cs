using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApp1
{
    // 家臣は山札から捨て札にしたものを場に戻して使用する。しかし場に戻したか次のアクションで手札から出したかはログから判別不可能なため、場に戻す操作は対応しないこととする。
    // このため家臣がサプライに含まれる場合は山札の数が正しくカウントされないことがある。
    class LineAnalyzer
    {
        // action時のカードの移動元と移動先はstateによって変わる。手札と場の札は区別しない。
        // stateは自分または相手によるカードの使用と購入とクリーンアップによって変わる。(闇市場要確認。購入時も続くstateの有無を確認)
        private enum state {
            normal,             // 「獲得した。」「購入・獲得した。」「受け取った。」：サプライ→捨て札
                                // 「引いた。」：山札→手札
                                // 「捨て札にした。」：手札→捨て札
                                // 「廃棄した。」：手札→廃棄置き場
                                // 「呼び出した。」：酒場→手札
                                // 「置いた。」：手札→山札
                                // 「渡した。」：手札→相手の手札
                                // 「戻した。」：手札→サプライ
                                // 「シャフルした。」：捨て札→山札
                                // 「クリーンアップした。」手札→捨て札

            discarding_deck,    // 「捨て札にした。」：山札→捨て札

            trashing_deck,      // 「廃棄した。」：山札→廃棄置き場

            discarding_trashing_deck, // 「捨て札にした。」：山札→捨て札、「廃棄した。」：山札→廃棄置き場

            putting_from_discard, // 「置いた。」：捨て札→山札

            getting_in_hand,    // 「獲得した。」：サプライ→手札

            getting_on_deck,    // 「獲得した。」：サプライ→山札
        };


        private void UseCard(string card)
        {
            string[] discardingDeckCards = { // 山札を捨て札にするカード
                "家臣",       // 基本 
            };
            string[] trashingDeckCards = { // 山札を廃棄するカード
                              // 基本
            };
            string[] discardingTrashingDeckCards = { // 山札を捨て札にし、山札を廃棄するカード
                "山賊", "衛兵", // 基本
            };
            string[] puttingFromDiscardCards = { // 捨て札から山札の上に札を置くカード
                "前駆者", // 基本。
            };
            string[] gettingInHandCards = { // 手札に獲得するカード
                "職人", "鉱山", // 基本
            };
            string[] gettingOnDeckCards = { // 山札の上に獲得するカード
                "役人",  // 基本
            };
            if (discardingDeckCards.Any(card.Equals)) current_state = state.discarding_deck;
            else if (trashingDeckCards.Any(card.Equals)) current_state = state.trashing_deck;
            else if (discardingTrashingDeckCards.Any(card.Equals)) current_state = state.discarding_trashing_deck;
            else if (puttingFromDiscardCards.Any(card.Equals)) current_state = state.putting_from_discard;
            else if (gettingInHandCards.Any(card.Equals)) current_state = state.getting_in_hand;
            else if (gettingOnDeckCards.Any(card.Equals)) current_state = state.getting_on_deck;
            else current_state = state.normal;
        }

        private bool justAfterShuffle = false;

        private int numAtShuffle;

        private state current_state = state.normal;

        private List<string> myBar = new List<string>();

        private List<string> myDiscard = new List<string>();

        private List<string> myHand = new List<string>();

        private void Remove(ref List<string> removed_cards, List<string> cards, string errorMessage)
        {
            foreach (var card in cards)
            {
                if (removed_cards.Contains(card)) removed_cards.Remove(card);
                else throw new Exception(errorMessage);
            }
        }
        /// <summary>自分の山札</summary>
        public List<string> myDeck = new List<string>();

        /// <summary>解析開始</summary>
        /// <param name="lines">解析する行</param>
        /// <param name="shortPlayerNames">プレイヤ短縮名の配列(手番順)</param>
        /// <param name="myTurnNumber">自分の手番</param>
        public void Run(string line, string[] shortPlayerNames, int myTurnNumber)
        {
            var myName = shortPlayerNames[myTurnNumber];
            var opponentName = shortPlayerNames[(myTurnNumber + 1) % 2];
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
                        if (current_state == state.normal)
                            myDiscard.AddRange(cards);
                        else if (current_state == state.getting_in_hand)
                            myHand.AddRange(cards);
                        else if (current_state == state.getting_on_deck)
                            myDeck.AddRange(cards);
                        else
                            throw new Exception("stateが不適切です。 action = " + action + ", state = " + current_state.ToString());
                        break;
                    case "シャッフルした。":
                        justAfterShuffle = true;
                        numAtShuffle = myDeck.Count;
                        myDeck.AddRange(myDiscard);
                        myDiscard.Clear();
                        break;
                    case "引いた。":
                        if (justAfterShuffle && numAtShuffle >= cards.Count)
                            throw new Exception("山札が残っているのにシャッフルしました。");
                        justAfterShuffle = false;
                        Remove(ref myDeck, cards, "引くカードが山札にありません。");
                        myHand.AddRange(cards);
                        break;
                    case "クリーンアップした。":
                        myDiscard.AddRange(myHand);
                        myHand.Clear();
                        current_state = state.normal;
                        break;
                    case "捨て札にした。":
                        if (current_state == state.normal)
                        {
                            myDiscard.AddRange(cards);
                            Remove(ref myHand, cards, "捨てるカードが手札にありません。");
                        }
                        else if (current_state == state.discarding_deck || current_state == state.discarding_trashing_deck)
                        {
                            myDiscard.AddRange(cards);
                            Remove(ref myDeck, cards, "捨てるカードが山札にありません。");
                        }
                        else throw new Exception("stateが不適切です。 action = " + action + ", state = " + current_state.ToString());
                        break;
                    case "廃棄した。":
                        if (current_state == state.normal)
                            Remove(ref myHand, cards, "廃棄するカードが手札にありません。");
                        else if (current_state == state.trashing_deck || current_state == state.discarding_trashing_deck)
                            Remove(ref myDeck, cards, "廃棄するカードが山札にありません。");
                        else
                            throw new Exception("stateが不適切です。 action = " + action + ", state = " + current_state.ToString());
                        break;
                    case "呼び出した。":
                        myHand.AddRange(cards);
                        Remove(ref myBar, cards, "呼び出すカードが酒場にありません。");
                        break;
                    case "置いた。":
                        if (current_state == state.normal)
                        {
                            myDeck.AddRange(cards);
                            Remove(ref myHand, cards, "置くカードが手札にありません。");
                        }
                        else if (current_state == state.putting_from_discard)
                        {
                            myDeck.AddRange(cards);
                            Remove(ref myDiscard, cards, "置くカードが捨て札にありません。");
                        }
                        else throw new Exception("stateが不適切です。 action = " + action + ", state = " + current_state.ToString());
                        break;
                    case "渡した。":
                        Remove(ref myHand, cards, "渡すカードが手札にありません。");
                        break;
                }
            }
            else if (name == opponentName)
            {
                if (action == "渡した。") myHand.AddRange(cards);
            }
            if (action == "使用した。") UseCard(cards[0]);
        }
    }
}
