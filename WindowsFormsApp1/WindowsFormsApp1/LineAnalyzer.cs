using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    class LineAnalyzer
    {
        // action時のカードの移動元と移動先はstateによって変わる。手札と場の札は区別しない。
        // stateはカードの使用とクリーンアップによって変わる。
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

            putting_from_discard, // 「置いた。」：捨て札→山札

            getting_in_hand,    // 「獲得した。」：サプライ→手札

            getting_on_deck,    // 「獲得した。」：サプライ→山札
        };


        private void UseCard(string card)
        {
            // TODO
        }

        private bool justAfterShuffle = false;

        private int numAtShuffle;

        private state current_state = state.normal;

        private List<string> myDiscard = new List<string>();

        private List<string> myHand = new List<string>();

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
                    case "受け取った":
                    case "購入・獲得した。":
                    case "獲得した。":
                        if (current_state == state.normal) myDiscard.AddRange(cards);
                        else if (current_state == state.getting_in_hand) myHand.AddRange(cards);
                        else if (current_state == state.getting_on_deck) myDeck.AddRange(cards);
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
                        foreach (var card in cards)
                        {
                            if (myDeck.Contains(card)) myDeck.Remove(card);
                            else throw new Exception("引くカードが山札にありません。");
                        }
                        myHand.AddRange(cards);
                        break;
                    case "クリーンアップした。":
                        myDiscard.AddRange(myHand);
                        myHand.Clear();
                        current_state = state.normal;
                        break;
                    case "使用した。":
                        UseCard(cards[0]);
                        break;
                }
            }
        }
    }
}
