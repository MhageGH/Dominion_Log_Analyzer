﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApp1
{
    // 女魔術師対応
    class PreviousMyCards
    {
        public List<string> myBar;

        public List<string> myDuration;

        public List<string> myPermanentDuration;

        public List<string> myArchive;

        public List<string> myCrypt;

        public List<string> myDiscard;

        public List<string> myHand;

        public List<string> myDeck;
    }

    class LineAnalyzer
    {
        // action時のカードの移動元と移動先はstateによって変わる。手札と場の札は区別しない。
        // stateは自分または相手によるカードの使用と購入とクリーンアップによって変わる。
        // normal以外のstateは併用できる。stateで記述していないactionはnormalの挙動が適用される。
        // (購入時も続くstateの有無を確認)
        [Flags]
        private enum state : UInt64
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
            normal = 1L << 0,

            // 「捨て札にした。」山札→捨て札
            deck_to_discard = 1L << 1,

            // 「廃棄した。」山札→廃棄置き場
            deck_to_trash = 1L << 2,

            // 「置いた。」捨て札→山札
            discard_to_deck = 1L << 3,

            // 「獲得した。」サプライ→手札
            getting_in_hand = 1L << 4,

            // 「獲得した。」サプライ→山札
            getting_on_deck = 1L << 5,

            // 「見た。」山札→手札
            look_to_draw = 1L << 6,

            // "家臣"使用中。
            //「捨て札にした。」で山札を捨て札にした後、捨て札にしたカードと同名のカードを使用した場合は、捨て札にしたカードを手札に戻す。
            // (捨て札にしたカードを使用せずに手札から同名のカードを使用した場合はログから判別できない。)
            vassal = 1L << 7,

            // 「置いた。」手札→持続場
            hand_to_duration = 1L << 8,

            // 「置いた。」山札→原住民の村マット、「手札に加えた。」原住民の村マット→手札
            native_village = 1L << 10,

            // 「置いた。」無効
            no_put = 1L << 11,

            // 「公開した。」山札→手札
            open_to_draw = 1L << 12,

            // 「手札に加えた。」捨て札→手札
            discard_to_hand = 1L << 13,

            // 「廃棄した。」捨て札→廃棄置き場
            discard_to_trash = 1L << 14,

            // ターン開始時
            turn_start = 1L << 15,

            // 使用無効
            no_use = 1L << 16, 

            // "城塞"廃棄中
            fortress = 1L << 17,

            // "隠遁者"使用中
            // 手札にあれば手札から廃棄し、手札になければ捨て札から廃棄する。
            // 手札にあるが捨て札から廃棄した場合はログから判別できない。
            hermit = 1L << 18,

            // "伝令官"使用中
            // 「公開した。」カードがアクションカードであれば、山札→手札
            herald = 1L << 19,

            // 購入フェイズ終了後
            afterBuy = 1L << 20,

            // "資料庫"使用中
            // myArchiveにカードを置く。資料庫のカードはターン開始時に一部を手札に加える。
            // 使用時に"資料庫"自体もmyArchiveに入れておく。myArchiveが空になる("資料庫"自体を除く)と"資料庫"は手札に戻る。
            // myArchiveは共通。"資料庫"を複数使用した場合、myArchiveが全て空になるまで"資料庫"は戻らない。
            // 本来は使用した"資料庫"ごとに個別にmyArchiveが用意される。しかしどのmyArchiveから取り出したかはログから判別出来ないため共通としている。
            // このため複数の資料庫を使用中にシャフルが入ると、山札のカウントが実際と食い違うことがある。
            archive = 1L << 21,

            // "石"の獲得・廃棄時効果中
            // この効果中に"銀貨"を獲得すると、購入フェイズ中であれば山札に、それ以外であれば手札に獲得する。
            stone = 1L << 22,

            // 「戻した。」の実行準備
            // 取り替え子対応のため。この直後が「取り替え子を受け取った。」であれば、捨て札から戻し、そうでなければ手札から戻す。
            readyToReturn = 1L << 23,

            // カブラー効果中
            cobbler = 1L << 24,

            // "納骨堂"使用中
            // myCryptにカードを置く。納骨堂のカードはターン開始時に一部を手札に加える。注意点は資料庫と同じ。
            crypt = 1L << 25,

            // 「戻した。」無効
            // カササギは「山札の上に置いた。」ではなく「山札に戻した。」というログのため
            no_return = 1L << 26,

            // 廃棄無効
            no_trash = 1L << 27,

            // 他のカードをプレイするカード使用中
            play_other_card = 1L << 28,

            // "研究"使用中
            // 研究は1以上のコストを持つカードを廃棄して何かを持続場に置いた時点で持続場に入る
            research = 1L << 29,

            // 相手による"聖なる木立ち"使用中
            // 相手の祝福効果を得る
            sacredGrove = 1L << 30,

            // "飢饉"使用中
            // 山札の上から3枚を公開後、「混ぜシャフル」ログ問題対応のためにno_shuffleにする
            famine = 1L << 31,

            // "秘密の洞窟"使用中
            // 手札を3枚捨て札にした時点で持続場に入る
            secretCave = 1L << 32,
        };

        // カードの使用、購入、クリーンアップのいずれかでリセットされないstate
        [Flags]
        private enum nonVolatileState
        {
            normal = 0,

            // "保存"使用中
            // クリーンアップ後の「手札に加えた。」持続場→手札、stateリセット
            save = 1 << 0,

            // 購入フェイズ中
            // 「購入した」、「購入・獲得した。」でこの状態になり、カードの使用とクリーンアップでリセットされる。
            duringBuy = 1 << 1,

            // "寄付"購入効果
            // ターン間の「手札に加えた。」は捨て札と山札の両方を手札に引くため、引く前に捨て札全てを山札に置く。
            donate = 1 << 2,

            // シャッフル無効
            no_shuffle = 1 << 3,

            // 技術革新
            // 技術革新かつ購入フェイズ中に、「脇に置いた。」捨て札→手札
            innovation = 1 << 4,

            // 回廊
            // 回廊かつターン開始時に、open_to_draw状態になる
            piazza = 1 << 5,
        }

        // 山札を捨て札にするカード
        private readonly string[] deckToDiscardCards = {
                "山賊",                         // 基本 
                "海賊船", "海の妖婆",           // 海辺
                "念視の泉",                     // 錬金術
                "借金", "大衆",                 // 繁栄
                "農村", "狩猟団", "道化師",     // 収穫祭
                "地下墓所", "賢者",             // 暗黒
                "助言者", "熟練工",             // ギルド
                "公使",                         // プロモ
                "倒壊", "ウォリアー",           // 冒険
                "国境警備隊",                   // ルネサンス
            };

        // 山札を廃棄するカード
        private readonly string[] deckToTrashCards = {
                "山賊",            // 基本
                "詐欺師",          // 陰謀
                "海賊船",          // 海辺
                "借金",            // 繁栄
                "ゾンビの石工",    // 夜想曲
            };

        // 捨て札を廃棄するカード
        private readonly string[] discardToTrashCards = {
                "ウォリアー", // 冒険
            };

        // 捨て札から山札の上に札を置くカード
        private readonly string[] discardToDeckCards = {
                "前駆者",       // 基本。
                "身代わり",     // 陰謀
                "ゴミあさり",   // 暗黒
            };

        // 手札に獲得するカード
        private readonly string[] gettingInHandCards = {
                "職人", "鉱山",     // 基本
                "拷問人", "交易場", // 陰謀
                "探検家",           // 海辺
                "不正利得",         // 異郷
                "物乞い",           // 暗黒
                "願い",             // 夜想曲
                "彫刻家",           // ルネサンス
            };

        // 山札の上に獲得するカード
        private readonly string[] gettingOnDeckCards = {
                "役人",                 // 基本
                "海の妖婆", "宝の地図", // 海辺
                "金貨袋", "馬上槍試合", // 収穫祭
                "開発",                 // 異郷
                "武器庫",               // 暗黒
                "収税吏",               // ギルド
                "工匠",                 // 冒険
            };

        // 見ることが引くことになることで辻褄が合うログを持つカード。
        private readonly string[] lookToDrawCards = {
                "衛兵",                             // 基本
                "見張り", "航海士", "真珠採り",     // 海辺
                "地図職人", "公爵夫人", "よろずや", // 異郷
                "生存者",                           // 暗黒
                "夜警", "ゾンビの密偵",             // 夜想曲
            };

        // 持続カード
        private readonly string[] durationCards = {
                "隊商", "漁村", "停泊所", "灯台", "商船", "前哨地", "策士", "船着場",                   // 海辺
                "教会", "船長",                                                                         // プロモ
                "魔除け", "橋の下のトロル", "隊商の護衛", "地下牢", "呪いの森", "沼の妖婆",     // 冒険
                "女魔術師",                                                                             // 帝国
                "カブラー", "悪人のアジト", "ゴーストタウン", "守護者", "夜襲", "幽霊",   // 夜想曲
            };

        // 永久持続カード
        private readonly string[] permanentDurationCards = {
                "王子",                 // プロモ
                "雇人", "チャンピオン", // 冒険
            };

        // 手札を持続場に置くカード
        private readonly string[] asideCards = {
                "停泊所",       // 海辺
                "王子", "教会", // プロモ
                "道具",         // 冒険
            };

        // 手札を島に置くカード
        private readonly string[] islandCards = {
                "島",    // 海辺
            };

        // 原住民の村マットを使うカード
        private readonly string[] nativeVillageCards = {
                "原住民の村", // 海辺
            };

        // 置くことを無効にすることで辻褄が合うログを持つカード。
        private readonly string[] noPutCards = {
                "書庫",                    // 基本
                "パトロール",              // 陰謀
                "薬師", "念視の泉",        // 錬金術
                "大衆",                    // 繁栄
                "浮浪者",                  // 暗黒
                "ウィル・オ・ウィスプ",    // 夜想曲
                "易者",                    // ルネサンス
            };

        // 公開することが引くことになることで辻褄が合うログを持つカード。
        private readonly string[] openToDrawCards = {
                "ゴーレム",       // 錬金術
                "投機",           // 繁栄
                "占い師", "収穫", // 収穫祭
                "義賊", "神託",   // 異郷
                "デイム・アンナ", "デイム・ジョセフィーヌ", "デイム・モリー", "デイム・ナタリー", "デイム・シルビア", // 暗黒
                "サー・ベイリー", "サー・デストリー", "サー・マーチン", "サー・マイケル", "サー・ヴァンデル",         // 暗黒
                "盗賊", "建て直し", "吟遊詩人", "金物商", // 暗黒
                "医者",           // ギルド
                "巨人",           // 冒険
                "幽霊",           // 夜想曲
            };

        // 捨て札を手札に入れるカード
        private readonly string[] discardToHandCards = {
                "会計所",                  // 繁栄
                "騒がしい村", "開拓者",    // 帝国
                "山村",                    // ルネサンス
            };

        // 隠遁者
        private readonly string[] hermitCard = {
                "隠遁者", // 暗黒
            };

        // 伝令官
        private readonly string[] heraldCard = {
                "伝令官", //  ギルド
            };

        // 家臣
        private readonly string[] vassalCard = {
                "家臣", // 基本
            };

        // 資料庫
        private readonly string[] archiveCard = {
                "資料庫", // 帝国
            };

        // 納骨堂
        private readonly string[] cryptCard = {
                "納骨堂", // 夜想曲
            };

        // 戻すことを無効にすることで辻褄が合うログを持つカード
        private readonly string[] noReturnCards = {
                "カササギ", // 冒険
            };

        // 他のカードをプレイするカード
        private readonly string[] playOtherCards =
        {
                "はみだし者",        // 暗黒時代
                "大君主",            // 帝国
                "ネクロマンサー",    // 夜想曲
            };

        // 研究
        private readonly string[] researchCard = {
                "研究", // ルネサンス
            };

        // 再使用するカード
        // 持続を再使用すると持続場に行く
        private readonly string[] reuseCards = {
                "玉座の間",   // 基本
                "宮廷",       // 繁栄
                "門下生",     // 冒険
                "冠",         // 帝国
                "幽霊",       // 夜想曲
                "王笏",       // ルネサンス
            };

        // 廃棄を無効にするカード
        private readonly string[] noTrashCard = {
                "剣闘士",  // 帝国
            };

        // 聖なる木立ち
        private readonly string[] sacredGroveCard = {
                "聖なる木立ち", // 夜想曲
            };

        // 秘密の洞窟
        private readonly string[] secretCaveCard = {
                "秘密の洞窟",
            };

        private void UseCard(string card, bool me, bool reuse)
        {
            previousMyCards.myArchive = new List<string>(myArchive);
            previousMyCards.myBar= new List<string>(myBar);
            previousMyCards.myCrypt = new List<string>(myCrypt);
            previousMyCards.myDeck = new List<string>(myDeck);
            previousMyCards.myDiscard = new List<string>(myDiscard);
            previousMyCards.myDuration = new List<string>(myDuration);
            previousMyCards.myHand = new List<string>(myHand);
            previousMyCards.myPermanentDuration = new List<string>(myPermanentDuration);

            bool playingOtherCard = (current_state.HasFlag(state.play_other_card))? true : false;
            if (me)
            {
                if (vassalDiscard != null)
                {
                    if (card.Equals(vassalDiscard))
                    {
                        myHand.Add(card);
                        myDiscard.Remove(card);
                    }
                    vassalDiscard = null;
                }
                if (durationCards.Any(card.Equals))
                {
                    if (playingOtherCard)
                    {
                        myDuration.Add(agency);
                        myHand.Remove(agency);
                    }
                    else if (reuse && reusing != null)
                    {
                        myDuration.Add(reusing);
                        myHand.Remove(reusing);
                        reusing = null; // 宮廷対応
                    }
                    else
                    {
                        myDuration.Add(card);
                        myHand.Remove(card);
                    }
                }
                if (permanentDurationCards.Any(card.Equals))
                {
                    if (playingOtherCard)
                    {
                        myPermanentDuration.Add(agency);
                        myHand.Remove(agency);
                    }
                    else
                    {
                        myPermanentDuration.Add(card);
                        myHand.Remove(card);
                    }
                }
                if (archiveCard.Any(card.Equals))
                {
                    myArchive.Add(card);
                    myHand.Remove(card);
                }
                if (cryptCard.Any(card.Equals))
                {
                    myCrypt.Add(card);
                    myHand.Remove(card);
                }
                if (reuseCards.Any(card.Equals))
                    reusing = card;
            }
            current_state = 0;
            if (deckToDiscardCards.Any(card.Equals))
                current_state |= state.deck_to_discard;
            if (deckToTrashCards.Any(card.Equals))
                current_state |= state.deck_to_trash;
            if (discardToTrashCards.Any(card.Equals))
                current_state |= state.discard_to_trash;
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
            if (nativeVillageCards.Any(card.Equals))
                current_state |= state.native_village;
            if (noPutCards.Any(card.Equals))
                current_state |= state.no_put;
            if (openToDrawCards.Any(card.Equals))
                current_state |= state.open_to_draw;
            if (discardToHandCards.Any(card.Equals))
                current_state |= state.discard_to_hand;
            if (hermitCard.Any(card.Equals) && me)
                current_state |= state.hermit;
            if (heraldCard.Any(card.Equals) && me)
                current_state |= state.herald;
            if (archiveCard.Any(card.Equals) && me)
                current_state |= state.archive;
            if (cryptCard.Any(card.Equals) && me)
                current_state |= state.crypt;
            if (noReturnCards.Any(card.Equals) && me)
                current_state |= state.no_return;
            if (playOtherCards.Any(card.Equals) && me)
            {
                current_state |= state.play_other_card;
                agency = card;
            }
            if (researchCard.Any(card.Equals))
                current_state |= state.research;
            if (noTrashCard.Any(card.Equals))
                current_state |= state.no_trash;
            if (secretCaveCard.Any(card.Equals))
                current_state |= state.secretCave;
            if (!me && sacredGroveCard.Any(card.Equals))
                current_state |= state.sacredGrove;
            if (current_state == 0)
                current_state = state.normal;
            current_nonVolatileState ^= nonVolatileState.duringBuy;
        }

        // "家臣"によって山札から捨て札にされたカード
        private string vassalDiscard = null;   

        private int numAtShuffle;

        private state current_state = state.normal;

        private nonVolatileState current_nonVolatileState = nonVolatileState.normal;

        private string gotCard;         // 直前に獲得したカード(玉璽対応：ログには山札に置いたカードが「カード」としか表示されないため)

        private string agency;          // 代理アクションカード(大君主など)

        private string reusing;         // 再利用カード

        private List<string> returnedCards;    // 戻されるカード(取り替え子対応)

        private List<string> myBar = new List<string>();

        private List<string> myIsland = new List<string>();

        private List<string> myNativeVillage = new List<string>();

        private List<string> myDuration = new List<string>();

        private List<string> myPermanentDuration = new List<string>();

        private List<string> myArchive = new List<string>();

        private List<string> myCrypt = new List<string>();

        private List<string> myDiscard = new List<string>();

        private List<string> myHand = new List<string>();

        private List<string> myDeck = new List<string>();

        private PreviousMyCards previousMyCards = new PreviousMyCards();

        private string[] shortPlayerNames;

        private int myTurnNumber;

        static readonly string[] boons = { "大地の恵み", "田畑の恵み", "炎の恵み", "森の恵み", "月の恵み",
                            "山の恵み", "川の恵み", "海の恵み", "空の恵み", "太陽の恵み", "沼の恵み", "風の恵み" };

        static readonly string[] hexes = { "凶兆", "幻惑", "羨望", "飢饉", "恐怖", "貪欲", "憑依", "蝗害", "みじめな生活", "疫病", "貧困", "戦争" };

        static readonly string[] states = { "森の迷子", "錯乱", "嫉妬", "生活苦", "二重苦" };

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
            if (line.Contains("女魔術師により") && line.Contains("無効化された。"))
            {
                current_state = state.normal;
                myArchive = new List<string>(previousMyCards.myArchive);
                myBar = new List<string>(previousMyCards.myBar);
                myCrypt = new List<string>(previousMyCards.myCrypt);
                myDeck = new List<string>(previousMyCards.myDeck);
                myDiscard = new List<string>(previousMyCards.myDiscard);
                myDuration = new List<string>(previousMyCards.myDuration);
                myHand = new List<string>(previousMyCards.myHand);
                myPermanentDuration = new List<string>(previousMyCards.myPermanentDuration);
                return;
            }
            var (name, action, cards, destination, inParentheses) = Extractor.Extract(line);
            if (current_state.HasFlag(state.readyToReturn)) //　取り替え子対応のため、「戻した。」は次の行で処理する。
            {
                if (name == myName && action == "受け取った。" && cards[0] == "取り替え子")
                {
                    myDiscard.Add(cards[0]);
                    if (myDiscard.Contains(returnedCards[0])) myDiscard.Remove(returnedCards[0]);
                    else throw new Exception("戻すカードが捨て札にありません。");
                }
                else
                {
                    Remove(ref myHand, returnedCards, "戻すカードが手札にありません。");
                }
                current_state ^= state.readyToReturn;
            }
            if (name == myName)
            {
                switch (action)
                {
                    case "購入した。":   // 購入するが獲得しない。獲得ログはこの後発生する。
                        current_state = state.normal;
                        current_nonVolatileState |= nonVolatileState.duringBuy;
                        // 伝令官の購入時効果：2019年10月20日現在、この効果で山札に行くカード名が匿名の「カード」となる不具合があるため正常動作しない。
                        if (cards[0] == "伝令官") current_state |= state.discard_to_deck;
                        if (cards[0] == "医者") current_state |= state.look_to_draw;
                        if (cards[0] == "召喚") current_state |= state.getting_in_hand;
                        if (cards[0] == "義賊") current_state |= state.open_to_draw;
                        if (cards[0] == "保存") current_nonVolatileState |= nonVolatileState.save;
                        if (cards[0] == "偵察隊") current_state |= state.look_to_draw;
                        if (cards[0] == "寄付") current_nonVolatileState |= nonVolatileState.donate;
                        if (cards[0] == "技術革新") current_nonVolatileState |= nonVolatileState.innovation;
                        if (cards[0] == "回廊") current_nonVolatileState |= nonVolatileState.piazza;
                        if (cards[0] == "大地への塩まき") current_state |= state.no_trash;
                        if (cards[0] == "併合")
                        {
                            current_state |= state.discard_to_deck;
                            current_nonVolatileState |= nonVolatileState.no_shuffle;
                        }
                        break;
                    case "購入・獲得した。":
                        current_state = state.normal;
                        current_nonVolatileState |= nonVolatileState.duringBuy;
                        if (cards[0] == "遊牧民の野営地") myDeck.AddRange(cards);
                        else if (cards[0] == "悪人のアジト" || cards[0] == "ゴーストタウン" || cards[0] == "守護者")
                            myHand.AddRange(cards);
                        else myDiscard.AddRange(cards);
                        if (cards[0] == "石") current_state |= state.getting_on_deck;
                        if (cards[0] == "ヴィラ") current_state |= state.discard_to_hand;
                        if (cards[0] == "宿屋")
                        {
                            current_state |= state.discard_to_deck;
                            current_nonVolatileState |= nonVolatileState.no_shuffle;
                        }
                        gotCard = cards[0];
                        break;
                    case "受け取った。":
                    case "獲得した。":
                    case "廃棄置き場から獲得した。":
                        if (current_state.HasFlag(state.getting_in_hand))
                            myHand.AddRange(cards);
                        else if (current_state.HasFlag(state.getting_on_deck) || cards[0] == "遊牧民の野営地" || destination == "山札の上")
                            myDeck.AddRange(cards);
                        else if (cards[0] == "悪人のアジト" || cards[0] == "ゴーストタウン" || cards[0] == "守護者" || cards[0] == "夜警")
                            myHand.AddRange(cards);
                        else if (current_state.HasFlag(state.turn_start) && current_state.HasFlag(state.cobbler))
                            myHand.AddRange(cards);
                        else
                            myDiscard.AddRange(cards);
                        if (cards[0] == "宿屋")
                        {
                            current_state |= state.discard_to_deck;
                            current_nonVolatileState |= nonVolatileState.no_shuffle;
                        }
                        if (cards[0] == "石")
                        {
                            if (current_nonVolatileState.HasFlag(nonVolatileState.duringBuy)) current_state |= state.getting_on_deck;
                            else current_state |= state.getting_in_hand;
                        }
                        if (cards[0] == "ヴィラ") current_state |= state.discard_to_hand;
                        gotCard = cards[0];
                        break;
                    case "シャッフルした。":
                        if (current_nonVolatileState.HasFlag(nonVolatileState.no_shuffle)) break;
                        numAtShuffle = myDeck.Count;
                        myDeck.AddRange(myDiscard);
                        myDiscard.Clear();
                        break;
                    // 「〇を山札に混ぜシャッフルした。」は、直前に「山札をシャッフルした。」というログが現れるがこれは無視する。
                    case "混ぜシャッフルした。":
                        if (destination != "山札") throw new Exception("混ぜシャッフル失敗");
                        myDeck.AddRange(cards);
                        if (current_state.HasFlag(state.discard_to_deck))
                            Remove(ref myDiscard, cards, "混ぜるカードが捨て札にありません。");
                        else if (current_nonVolatileState.HasFlag(nonVolatileState.donate))
                        {
                            Remove(ref myDiscard, cards, "混ぜるカードが捨て札にありません。");
                            current_nonVolatileState ^= nonVolatileState.donate;
                        }
                        else
                            Remove(ref myHand, cards, "混ぜるカードが手札にありません。");
                        current_nonVolatileState ^= nonVolatileState.no_shuffle;
                        break;
                    case "引いた。":
                    case "指定し、的中した。":   // 願いの井戸、秘術師。的中しない場合のテキストは「Tを銅貨を指定したが、香辛料商人が公開された。」のように主語の後が「を」になっていて解析に失敗するが無視しても問題なし。
                        Remove(ref myDeck, cards, "引くカードが山札にありません。");
                        myHand.AddRange(cards);
                        if (current_nonVolatileState.HasFlag(nonVolatileState.donate))
                        {
                            myDeck.AddRange(myDiscard);
                            myDiscard.Clear();
                            current_nonVolatileState |= nonVolatileState.no_shuffle;
                        }
                        break;
                    case "見た。":
                        if (current_state.HasFlag(state.look_to_draw))
                        {
                            Remove(ref myDeck, cards, "引くカードが山札にありません。");
                            myHand.AddRange(cards);
                        }
                        break;
                    case "クリーンアップした。":
                        myDiscard.AddRange(myHand);
                        myHand.Clear();
                        current_state = state.normal;
                        current_nonVolatileState ^= nonVolatileState.duringBuy;
                        break;
                    case "捨て札にした。":
                        if (cards.Any() && (boons.Any(cards[0].Equals) || hexes.Any(cards[0].Equals))) break;
                        if (current_state.HasFlag(state.deck_to_discard) || current_state.HasFlag(state.vassal))
                        {
                            myDiscard.AddRange(cards);
                            Remove(ref myDeck, cards, "捨てるカードが山札にありません。");
                            if (current_state == state.vassal) vassalDiscard = cards[0];
                        }
                        else if (current_state.HasFlag(state.afterBuy) && cards.Contains("ワイン商"))
                        {
                            myDiscard.AddRange(cards);
                            Remove(ref myBar, cards, "捨てるカードが酒場マットにありません。");
                        }
                        else
                        {
                            myDiscard.AddRange(cards);
                            Remove(ref myHand, cards, "捨てるカードが手札にありません。");
                            if (current_state.HasFlag(state.secretCave) && cards.Count == 3)
                            {
                                myDuration.Add("秘密の洞窟");
                                myHand.Remove("秘密の洞窟");
                            }
                        }
                        break;
                    case "廃棄した。":
                        if (current_state.HasFlag(state.no_trash))
                            break;
                        if (cards.Contains("城塞"))
                            current_state |= state.fortress;
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
                        if (cards.Contains("石"))
                        {
                            if (current_nonVolatileState.HasFlag(nonVolatileState.duringBuy)) current_state |= state.getting_on_deck;
                            else current_state |= state.getting_in_hand;
                        }
                        break;
                    case "終了した。":
                        if (cards[0] == "購入フェイズ")  // 購入フェイズを終了した。
                            current_state |= state.afterBuy;
                        break;
                    case "呼び出した。":
                        myHand.AddRange(cards);
                        Remove(ref myBar, cards, "呼び出すカードが酒場にありません。");
                        if (cards.Contains("変容")) current_state |= state.getting_in_hand;
                        if (cards.Contains("御料車")) reusing = "御料車";
                        break;
                    case "置いた。":
                        if (current_state.HasFlag(state.no_put)) break;
                        else if (cards.Any() && boons.Any(cards[0].Equals)) break;  // 恵みの村対応
                        else if (current_state.HasFlag(state.discard_to_deck) && destination != "捨て札置き場")
                        {
                            if (cards.Any() && cards[0] == "カード")  // 玉璽対応
                            {
                                cards.Clear();
                                cards.Add(gotCard);
                            }
                            myDeck.AddRange(cards);
                            Remove(ref myDiscard, cards, "置くカードが捨て札にありません。");
                        }
                        else if (current_state.HasFlag(state.archive))
                        {
                            myArchive.AddRange(cards);
                            Remove(ref myDeck, cards, "置くカードが山札にありません。");
                        }
                        else if (current_state.HasFlag(state.crypt))
                        {
                            myCrypt.AddRange(cards);
                            Remove(ref myHand, cards, "置くカードが手札にありません。");
                        }
                        else if (current_state.HasFlag(state.hand_to_duration) || destination == "脇")
                        {
                            if (inParentheses == "貨物船")
                            {
                                myDuration.AddRange(cards);
                                Remove(ref myDiscard, cards, "置くカードが捨て札にありません。");
                                myDuration.Add("貨物船");
                                Remove(ref myHand, new List<string> { "貨物船" }, "貨物船が手札にありません。");    // 貨物船は貨物を入れたときに持続場に入る
                            }
                            else if (inParentheses == "道具")
                            {
                                myDuration.AddRange(cards);
                                Remove(ref myHand, cards, "置くカードが手札にありません。");
                                myDuration.Add("道具");
                                Remove(ref myHand, new List<string> { "道具" }, "道具が手札にありません。");    // 道具は1枚以上のカードを持続場に入れたときに持続場に入る
                            }
                            else if (current_state.HasFlag(state.research))
                            {
                                myDuration.AddRange(cards);
                                Remove(ref myDeck, cards, "置くカードが山札にありません。");
                                myDuration.Add("研究");
                                Remove(ref myHand, new List<string> { "研究" }, "研究が手札にありません。");    // 研究は何かを持続場に入れたときにと一緒に持続場に入る
                            }
                            else if (inParentheses == "原住民の村")
                            {
                                myNativeVillage.AddRange(cards);
                                Remove(ref myDeck, cards, "置くカードが山札にありません。");
                            }
                            else if (current_nonVolatileState.HasFlag(nonVolatileState.innovation))
                            {
                                myHand.AddRange(cards);
                                Remove(ref myDiscard, cards, "置くカードが捨て札にありません。");
                            }
                            else
                            {
                                myDuration.AddRange(cards);
                                Remove(ref myHand, cards, "置くカードが手札にありません。");
                            }
                        }
                        else if (destination == "島マット")
                        {
                            myIsland.AddRange(cards);
                            Remove(ref myHand, cards, "置くカードが手札にありません。");
                        }
                        else if (destination == "酒場マット")
                        {
                            myBar.AddRange(cards);
                            Remove(ref myHand, cards, "置くカードが手札にありません。");
                        }
                        else if (destination == "山札の上")
                        {
                            myDeck.AddRange(cards);
                            Remove(ref myHand, cards, "置くカードが手札にありません。");
                        }
                        if (cards.Any() && cards[0] == "山札" && destination == "捨て札置き場")
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
                            else if (current_state.HasFlag(state.turn_start))
                            {
                                if (myArchive.Any() && inParentheses == "資料庫")
                                {
                                    myHand.AddRange(cards);
                                    Remove(ref myArchive, cards, "引くカードが資料庫にありません。");
                                    if (myArchive.FindIndex(m => m != "資料庫") == -1)
                                    {
                                        myHand.AddRange(myArchive);
                                        myArchive.Clear();
                                    }
                                }
                                if (myCrypt.Any() && inParentheses == "納骨堂")
                                {
                                    myHand.AddRange(cards);
                                    Remove(ref myCrypt, cards, "引くカードが納骨堂にありません。");
                                    if (myCrypt.FindIndex(m => m != "納骨堂") == -1)
                                    {
                                        myHand.AddRange(myCrypt);
                                        myCrypt.Clear();
                                    }
                                }
                                break;    // ターン開始時に持続カードと同時に手札に加えているので無視する
                            }
                            else if (current_state.HasFlag(state.fortress)) myHand.AddRange(cards); // 城塞を手札に加える
                            else if (current_nonVolatileState.HasFlag(nonVolatileState.save))
                            {
                                myHand.AddRange(cards);
                                Remove(ref myDuration, cards, "保存したカードがありません。");
                                current_nonVolatileState ^= nonVolatileState.save;
                            }
                            else if (inParentheses == "ターン終了時" && cards.Contains("忠犬"))
                            {
                                myHand.AddRange(cards);
                                Remove(ref myDuration, cards, "忠犬がありません。");
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
                        if (cards[0] == "ターン")    // ターンを開始した。
                        {
                            current_state |= state.turn_start;
                            if (myDuration.Contains("カブラー")) current_state |= state.cobbler;
                            myHand.AddRange(myDuration);
                            myDuration.Clear();
                            if (current_nonVolatileState.HasFlag(nonVolatileState.piazza))
                            {
                                current_state |= state.open_to_draw;
                            }
                        }
                        break;
                    case "リアクションした。":
                        if (cards[0] == "玉璽") current_state |= state.discard_to_deck;
                        if (cards[0] == "望楼") current_state |= state.discard_to_deck | state.discard_to_trash;
                        if (cards[0] == "馬商人") current_state |= state.hand_to_duration;
                        if (cards[0] == "愚者の黄金") current_state |= state.getting_on_deck;
                        if (cards[0] == "移動遊園地") current_state |= state.discard_to_deck;
                        if (cards[0] == "忠犬")
                        {
                            myHand.AddRange(cards);
                            Remove(ref myDiscard, cards, "捨て札に忠犬がありません");
                        }
                        if (cards[0] == "追跡者" && !current_state.HasFlag(state.getting_in_hand)) current_state |= state.discard_to_deck;
                        break;
                    case "公開した。":
                        if (current_state.HasFlag(state.open_to_draw))
                        {
                            if (current_state.HasFlag(state.famine))
                                current_nonVolatileState |= nonVolatileState.no_shuffle;
                            myHand.AddRange(cards);
                            Remove(ref myDeck, cards, "引くカードが山札にありません。");
                        }
                        else if (current_state.HasFlag(state.herald) && CardList.actionCards.Any(cards[0].Equals))
                        {
                            myHand.AddRange(cards);
                            Remove(ref myDeck, cards, "引くカードが山札にありません。");
                        }
                        break;
                    case "戻した。":
                        if (states.Any(cards[0].Equals)) break;
                        if (cards[0].Contains("トークン")) break;
                        if (cards.Contains("陣地") && destination == "陣地の山")
                            Remove(ref myDuration, cards, "戻すカードが脇にありません。");
                        else if (current_state.HasFlag(state.no_return) && destination == "山札")
                            break;
                        else
                        {
                            current_state |= state.readyToReturn;
                            returnedCards = cards;
                        }
                        break;
                    case "受けた。":
                        if (cards[0] == "月の恵み") current_state |= state.discard_to_deck;
                        if (cards[0] == "太陽の恵み") current_state |= state.look_to_draw;
                        if (cards[0] == "凶兆") current_state |= state.discard_to_deck;
                        if (cards[0] == "貪欲") current_state |= state.getting_on_deck;
                        if (cards[0] == "蝗害") current_state |= state.deck_to_trash;
                        if (cards[0] == "疫病") current_state |= state.getting_in_hand;
                        if (cards[0] == "戦争") current_state |= state.open_to_draw;
                        if (cards[0] == "飢饉")
                        {
                            current_state |= state.open_to_draw;
                            current_state |= state.famine;
                        }
                        break;
                }
            }
            else if (name == opponentName)
            {
                if (action == "渡した。") myHand.AddRange(cards);
                // 相手の隊商の護衛によるリアクションのカード使用でstateがリセットされないようにする
                if (action == "リアクションした。" && cards[0] == "隊商の護衛") current_state |= state.no_use;
                // 相手の聖なる木立ちは相手が祝福受けるログでこちらが祝福効果を受ける
                if (current_state.HasFlag(state.sacredGrove) && action == "受けた。" && cards[0] == "月の恵み") current_state |= state.discard_to_deck;
                if (current_state.HasFlag(state.sacredGrove) && action == "受けた。" && cards[0] == "太陽の恵み") current_state |= state.look_to_draw;
            }
            if (action == "使用した。")
            {
                if (current_state.HasFlag(state.no_use)) current_state ^= state.no_use;
                else UseCard(cards[0], name == myName, false);
            }
            if (action == "再使用した。" || action == "再々使用した。") UseCard(cards[0], name == myName, true);
        }
    }
}
