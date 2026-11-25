# 第1章シナリオ → Project/Assets/D117/Resources/Scenario/ch1.json
import json, os
def L(who, text, expr=None, show=None, **k):
    d = {"who": who, "text": text}
    if expr: d["expr"] = expr
    if show is not None:
        d["hasShow"] = True
        d["show"] = [{"id": s.split(':')[0], "pos": s.split(':')[1], "expr": (s.split(':')[2] if s.count(':') > 1 else "normal")} for s in show]
    d.update(k); return d
def C(text, to, **k): d = {"text": text, "to": to}; d.update(k); return d
N = []
def node(id, type="story", lines=(), **k):
    d = {"id": id, "type": type, "chapter": 1, "lines": list(lines)}; d.update(k); N.append(d)

node("c1_open", bg="black", place="", lines=[
  L("narr", "「その村に入った者は、宝を見つけるまで帰れない。」", show=[]),
  L("narr", "山奥の地図から消えた村――隠れ湯村。百年前のある夜、村人がひとり残らず姿を消したという。"),
  L("narr", "そして今、八人のお宝ハンターが、その村の入口に立っていた。"),
], next="c1_gate")

node("c1_gate", bg="gate", place="隠れ湯村・入口", lines=[
  L("yuma", "ここが隠れ湯村……！　じいちゃんの巻物と、同じ景色だ！", "smile", show=["yuma:C:smile"]),
  L("leon", "地図によれば、この村の地下に眠っているのが――世界最大の宝石『龍神の珠』だ。", "normal", show=["yuma:L:normal", "leon:R:normal"]),
  L("sakura", "龍神の珠……。一度でいいから、この手で鑑定してみたかったの。", "smile", show=["yuma:L:normal", "sakura:R:smile"]),
  L("gantz", "浮かれるなよ、坊主。この霧じゃ、一歩先も分からん。", "think", show=["yuma:L:normal", "gantz:R:think"]),
  L("rosa", "いいじゃない。賭けは、降りた方が負けよ。", "smile", show=["gantz:L:normal", "rosa:R:smile"]),
  L("dante", "資金も装備も、全部わしが用意した。さあ、楽しい宝探しの始まりだ！", "smile", show=["rosa:L:normal", "dante:R:smile"]),
  L("mio", "……ねえ、レオンさん。その古地図、あとで見せてもらえる？", "think", show=["mio:L:think", "leon:R:normal"]),
  L("eris", "その前に紹介するね！　わたしの自信作、ルミ！", "smile", show=["yuma:L:normal", "eris:R:smile"]),
  L("lumi", "はじめまして。案内役のルミです。皆さんが迷子になる確率は、現在八十七パーセントです。", lumiGlow=True),
  L("yuma", "高っ！？", "surprise", yomi="たかっ！？"),
  L("lumi", "なお、宝石に近づくほど、わたしは明るく光ります。せいぜい頼りにしてください。"),
], next="c1_square")

node("c1_square", type="explore", bg="square", place="朽ちた広場",
  require=["map", "warm"], exitText="村の奥へ進む", exitTo="c1_bridge",
  hint="ヒントです。古地図をよく見てください。それから、湯気の立つ建物も気になりますね。",
  lines=[
  L("narr", "崩れた家々が並ぶ広場。中央には苔むした井戸、その向こうに古い鐘楼が霧の中にそびえている。", show=[]),
  L("yuma", "よし、まずはこの広場から調べよう！", "normal", show=["yuma:C:normal"]),
  ], choices=[
  C("見る：古地図", "c1_map", once="map"),
  C("調べる：崩れた家", "c1_house", once="house"),
  C("調べる：井戸", "c1_well", once="well"),
  C("見る：鐘楼", "c1_tower", once="tower"),
  C("移動：湯気の立つ建物", "c1_bath", once="warm"),
  C("仲間に聞く：ガンツ", "c1_gantz", once="gantztalk"),
  C("調べる：地面", "c1_ground", miss=True),
  C("ルミをなでる", "c1_pet", miss=True),
])

node("c1_map", bg="square", lines=[
  L("mio", "やっぱり変。地図だと鐘楼は村の東。でも、本当は西にあるの。", "think", show=["mio:L:think", "leon:R:normal"]),
  L("leon", "地図が間違っている、と？", "think"),
  L("mio", "井戸も、湯屋も、ぜんぶ反対側。まるで……", "think"),
], next="c1_map_q")
node("c1_map_q", type="choice", bg="square", prompt="地図のずれの正体は？", lines=[], choices=[
  C("上下が逆になっている", "c1_map_ng"),
  C("左右が反転している", "c1_map_ok", clue="古地図は左右が反転している", flag="map", trust="mio"),
  C("縮尺が違う", "c1_map_ng"),
])
node("c1_map_ng", bg="square", lines=[
  L("mio", "うーん……それだと、井戸の位置が合わないよ。", "normal"),
  L("lumi", "ヒントです。鏡に映すと、どう見えますか？"),
], next="c1_map_q")
node("c1_map_ok", bg="square", lines=[
  L("mio", "そう、左右が反転してる！　鏡に映して描かれた地図なんだ。", "smile"),
  L("leon", "わざと読めないように、か。この地図を描いた人間は、何かを隠したかったようだね。", "surprise"),
  L("narr", "手掛かり『左右反転の古地図』を探検手帳に記した。"),
], next="c1_square")

node("c1_house", bg="square", lines=[
  L("narr", "傾いた家の中には、ちゃぶ台の上に茶碗が並んだまま残っていた。", show=[]),
  L("rosa", "食事の途中で、みんな消えたってこと……？", "fear", show=["rosa:C:fear"]),
  L("eris", "ううん、見て。荷物を入れる行李が空っぽ。慌てて持ち出したんだよ。", "think", show=["rosa:L:normal", "eris:R:think"], clue="村人は荷物をまとめて出ていった", flag="house"),
  L("narr", "手掛かり『村人は荷物をまとめて出ていった』を探検手帳に記した。"),
], next="c1_square")

node("c1_well", type="choice", bg="square", prompt="井戸をどう調べる？", lines=[
  L("narr", "井戸をのぞき込むと、暗い底から、生ぬるい風が吹き上げてきた。", show=["yuma:C:surprise"]),
  L("yuma", "なんだこれ……あったかい？", "surprise"),
], choices=[
  C("石を落として深さを測る", "c1_well_stone", flag="well", clue="井戸の底がかすかに蒼く光る"),
  C("身を乗り出してのぞく", "c1_well_lean"),
])
node("c1_well_stone", bg="square", lines=[
  L("narr", "……ポチャン。思ったより浅い。そして、水面がかすかに蒼く光った気がした。", se="drop"),
  L("lumi", "……今、わたし、少しだけ明るくなりました。", lumiGlow=True),
], next="c1_square")
node("c1_well_lean", bg="square", lines=[
  L("narr", "ガラッ！　足元の石が崩れた！", flash=True, shake=True, se="scare", life=-1),
  L("gantz", "危ねえ！", "angry", show=["yuma:L:fear", "gantz:R:angry"]),
  L("narr", "ガンツの太い腕が、ユウマの襟首をつかんで引き戻した。"),
  L("gantz", "この村の石は、全部もろい。次から気をつけろ。", "normal"),
  L("yuma", "た、助かった……。", "fear"),
], next="c1_square")

node("c1_tower", bg="square", lines=[
  L("narr", "霧の向こうの鐘楼。大きな釣鐘が、吊られたままになっている。", show=[]),
  L("sakura", "あの鐘、百年前から一度も鳴っていないそうよ。言い伝えでは……『鐘が鳴るとき、龍が目を覚ます』。", "think", show=["sakura:C:think"]),
  L("yuma", "じいちゃんの巻物にも、鐘楼が描いてあった……。", "think", show=["yuma:L:think", "sakura:R:normal"], clue="鐘が鳴るとき、龍が目を覚ます", flag="tower"),
], next="c1_square")

node("c1_gantz", bg="square", lines=[
  L("gantz", "……実はな、坊主。俺はこの村、初めてじゃねえ気がするんだ。", "think", show=["yuma:L:normal", "gantz:R:think"]),
  L("yuma", "えっ？", "surprise"),
  L("gantz", "いや、なんでもねえ。昔のことだ。先を急ごう。", "normal", trust="gantz", flag="gantztalk"),
], next="c1_square")
node("c1_pet", bg="square", lines=[
  L("yuma", "よしよし、ルミ。", "smile", show=["yuma:L:smile", "eris:R:smile"]),
  L("lumi", "……わたしはランタンです。なでても、明るさは変わりません。"),
  L("eris", "あはは、照れてる照れてる！", "smile"),
], next="c1_square")
node("c1_ground", bg="square", lines=[
  L("narr", "地面を調べた。……ただの地面だ。", show=["yuma:C:normal"]),
  L("lumi", "ユウマさん、それは地面です。"),
], next="c1_square")

node("c1_bath", type="choice", bg="bath", place="枯れた湯屋", prompt="どうする？", lines=[
  L("narr", "『隠れ湯』と書かれた額が傾いた湯屋。湯船は乾いているのに、かすかに湯気が漂っている。", show=[]),
  L("eris", "待って、この岩……まだ温かい！　百年も前に枯れた温泉なのに！", "surprise", show=["eris:C:surprise"]),
], choices=[
  C("岩に触ってみる", "c1_bath_touch"),
  C("エリスに調べてもらう", "c1_bath_eris", trust="eris"),
  C("ルミを近づける", "c1_bath_lumi", trust="eris"),
])
node("c1_bath_touch", bg="bath", lines=[
  L("narr", "ジュッ！", flash=True, se="scare", life=-1),
  L("yuma", "あっちぃ！", "fear", show=["yuma:L:fear", "eris:R:angry"]),
  L("eris", "もう、だから待ってって言ったのに！", "angry"),
], next="c1_bath_after")
node("c1_bath_eris", bg="bath", lines=[
  L("eris", "岩の奥から熱が伝わってきてる。地下のどこかに、すごく熱いものがあるんだ。", "think", show=["eris:C:think"]),
], next="c1_bath_after")
node("c1_bath_lumi", bg="bath", lines=[
  L("narr", "ルミの光が、ふっと蒼く強まった。", lumiGlow=True),
  L("lumi", "反応あり。この下に、何か大きなものがあります。"),
  L("eris", "宝石レーダー、大成功！", "smile", show=["eris:C:smile"]),
], next="c1_bath_after")
node("c1_bath_after", bg="bath", lines=[
  L("dante", "……地下の熱、か。", "think", show=["eris:L:normal", "dante:R:think"]),
  L("narr", "ダンテが一瞬だけ、険しい顔をした。", clue="湯屋の岩が今も温かい", flag="warm"),
], next="c1_square")

node("c1_bridge", type="choice", bg="bridge", place="谷の吊り橋", prompt="どうやって渡る？", lines=[
  L("narr", "村の奥へ続く道は、深い谷にかかる古い吊り橋で途切れていた。", show=[]),
  L("gantz", "板が何枚か腐ってやがる。全員で一度に渡るのは無理だ。", "think", show=["yuma:L:normal", "gantz:R:think"]),
  L("rosa", "さあ、どうする？　主人公さん。", "smile", show=["yuma:L:normal", "rosa:R:smile"]),
], choices=[
  C("一気に走って渡る", "c1_bridge_run", trust="rosa"),
  C("ガンツに板を確かめてもらう", "c1_bridge_gantz", trust="gantz"),
  C("ミオに別の道を探してもらう", "c1_bridge_mio", need="map", trust="mio", clue="谷の石段（地図の隠し道）"),
])
node("c1_bridge_run", bg="bridge", lines=[
  L("narr", "バキッ！　板が割れ、ユウマの片足が宙に浮く！", flash=True, shake=True, se="scare", life=-1),
  L("yuma", "うわあああっ！", "fear", show=["yuma:L:fear", "rosa:R:surprise"]),
  L("rosa", "つかまって！", "surprise"),
  L("narr", "ローザが手を伸ばし、ユウマはなんとか向こう岸へ転がり込んだ。"),
], next="c1_bridge_end")
node("c1_bridge_gantz", bg="bridge", lines=[
  L("gantz", "よし、青い印をつけた板だけ踏め。それ以外は信用するな。", "smile", show=["gantz:C:smile"]),
  L("narr", "一人ずつ、慎重に。八人とルミは、無事に谷を渡りきった。"),
], next="c1_bridge_end")
node("c1_bridge_mio", bg="bridge", lines=[
  L("mio", "反転した地図で見ると……谷の下に、古い石段があるはず。", "smile", show=["mio:C:smile"]),
  L("narr", "ミオの言う通り、霧に隠れた石段が見つかった。遠回りだが、確実な道だ。"),
], next="c1_bridge_end")
node("c1_bridge_end", bg="bridge", lines=[
  L("narr", "谷を越えた先に、灯りの消えた酒場が見えた。", show=[]),
  L("narr", "そのとき――。"),
  L("narr", "ゴォォォン……。", flash=True, se="bell"),
  L("yuma", "か、鐘が……鳴った！？", "surprise", show=["yuma:L:surprise", "sakura:R:fear"]),
  L("sakura", "百年、鳴らなかったはずの鐘が……。", "fear"),
  L("lumi", "皆さん、注意してください。……酒場の床下から、規則的な音がします。", lumiGlow=True),
], next="c1_end")
node("c1_end", type="end", bg="black", title="第1章　誰もいない村 ― 完", lines=[], text="第2章「骸骨の酒場」へつづく", next="c2_open")

# 声のファイル名を自動で付ける（narr以外）
n = 0
for nd in N:
    for ln in nd["lines"]:
        n += 1
        ln["voice"] = f"d117_c1_{n:03d}"
out = os.path.expanduser("~/mnt/TitanUnity100/Days/day117/Project/Assets/D117/Resources/Scenario/ch1.json")
json.dump({"title": "第1章 誰もいない村", "start": "c1_open", "nodes": N}, open(out, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
# 検証：飛び先の存在
ids = {x["id"] for x in N}
bad = [(x["id"], t) for x in N for t in [x.get("next"), x.get("exitTo")] + [c["to"] for c in x.get("choices", [])] if t and t not in ids]
print("nodes", len(N), "lines", n, "bad", bad)
