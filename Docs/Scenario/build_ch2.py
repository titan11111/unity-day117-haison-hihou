# 第2章シナリオ → Project/Assets/D117/Resources/Scenario/ch2.json
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
    d = {"id": id, "type": type, "chapter": 2, "lines": list(lines)}; d.update(k); N.append(d)

node("c2_open", bg="tavern", place="骸骨の酒場", lines=[
  L("narr", "第二章　骸骨の酒場", show=[]),
  L("narr", "灯りの消えた酒場。床下から、コン……コン……と、規則的な音が響いている。"),
  L("rosa", "ちょっと……あそこ。椅子に、誰か座ってる。", "fear", show=["rosa:C:fear"]),
  L("yuma", "が、骸骨……！", "fear", show=["yuma:L:fear", "rosa:R:fear"]),
  L("gantz", "落ち着け。昔の盗掘者だろう。……音は、床下からだな。", "think", show=["gantz:C:think"]),
  L("lumi", "床下の音は、一定の間隔です。何かの仕掛けかもしれません。"),
], next="c2_tavern")

node("c2_tavern", type="explore", bg="tavern", place="骸骨の酒場",
  require=["diary", "key"], exitText="床下の扉を開ける", exitTo="c2_floor",
  hint="ヒントです。骸骨が、何かを握っていますね。それと、散らばった紙の順番が気になります。",
  lines=[
  L("narr", "ほこりをかぶったカウンター、倒れた椅子、壁際に並ぶ酒樽。隅の椅子には、骸骨が一体。", show=[], cg="skeleton"),
  L("yuma", "怖いけど……調べないと、先に進めないよな。", "normal", show=["yuma:C:normal"]),
  ], choices=[
  C("調べる：骸骨", "c2_skel", once="skel"),
  C("調べる：散らばった紙", "c2_diary", once="diary"),
  C("調べる：骸骨の握る手", "c2_key", once="key", need="skel"),
  C("見る：壁の張り紙", "c2_poster", once="poster"),
  C("仲間に聞く：ダンテ", "c2_dante", once="dantetalk"),
  C("調べる：酒樽", "c2_barrel", miss=True),
  C("骸骨にあいさつする", "c2_hello", miss=True),
])

node("c2_skel", bg="tavern", lines=[
  L("narr", "骸骨は、古いつるはしを抱えたまま座っていた。足元には、ばらばらになった紙の束。", show=[], cg="skeleton"),
  L("eris", "服の生地からして、五十年くらい前の人かな。村人じゃなくて、外から来た人だね。", "think", show=["eris:C:think"]),
  L("narr", "骨ばった右手が、何かを固く握りしめている。", cg="skeleton", clue="骸骨は五十年前の盗掘者", flag="skel"),
], next="c2_tavern")

node("c2_diary", bg="tavern", lines=[
  L("narr", "日付の消えかけた日記が、三枚。", show=[]),
  L("leon", "順番がばらばらだ。中身から並べ直してみよう。", "think", show=["leon:C:think"]),
  L("narr", "【ア】ついに地下への入口を見つけた。鐘の下だ。\n【イ】村に着いた。誰もいない。宝の噂は本当か。\n【ウ】床下から音がする。出られない。誰か――"),
], next="c2_diary_q")
node("c2_diary_q", type="choice", bg="tavern", prompt="日記を正しい順に並べると？", lines=[], choices=[
  C("ア → イ → ウ", "c2_diary_ng"),
  C("イ → ア → ウ", "c2_diary_ok", clue="盗掘者は「鐘の下」に地下への入口を見つけた", flag="diary", trust="leon"),
  C("ウ → イ → ア", "c2_diary_ng"),
])
node("c2_diary_ng", bg="tavern", lines=[
  L("leon", "いや、それだと話がつながらない。村に着いたのは、いちばん最初のはずだ。", "normal", show=["leon:C:normal"]),
], next="c2_diary_q")
node("c2_diary_ok", bg="tavern", lines=[
  L("leon", "村に着き、鐘の下に入口を見つけ、そして……床下に閉じ込められた。", "smile", show=["leon:C:smile"]),
  L("mio", "鐘の下……。反転した古地図だと、鐘楼は西。入口もそこね。", "think", show=["leon:L:normal", "mio:R:think"]),
  L("narr", "手掛かり「鐘の下に地下への入口」を探検手帳に記した。"),
], next="c2_tavern")

node("c2_key", bg="tavern", lines=[
  L("narr", "骸骨の指をそっと開くと、古い鉄の鍵が出てきた。", show=[], cg="skeleton"),
  L("sakura", "刻印があるわ。古い字で……「湯守」。", "think", show=["sakura:C:think"]),
], next="c2_key_q")
node("c2_key_q", type="choice", bg="tavern", prompt="鍵の刻印「湯守」は、何と読む？", lines=[], choices=[
  C("ゆまもる", "c2_key_ng"),
  C("とうしゅ", "c2_key_ng"),
  C("ゆもり", "c2_key_ok", clue="鍵の刻印「湯守」", flag="key", trust="sakura"),
])
node("c2_key_ng", bg="tavern", lines=[
  L("sakura", "惜しいわね。湯を守る人、と書いて……。", "normal", show=["sakura:C:normal"]),
], next="c2_key_q")
node("c2_key_ok", bg="tavern", lines=[
  L("sakura", "正解。湯守――温泉を守る役目の人のことよ。", "smile", show=["sakura:C:smile"]),
  L("dante", "湯守……。", "surprise", show=["sakura:L:normal", "dante:R:surprise"]),
  L("narr", "その言葉を聞いた瞬間、ダンテの手が、小さく震えた。"),
], next="c2_tavern")

node("c2_poster", bg="tavern", lines=[
  L("narr", "色あせた張り紙。かすれた文字で「湯の龍神さま　ご開帳」と読める。", show=[]),
  L("yuma", "龍神……！　じいちゃんの巻物の絵と、同じだ！", "surprise", show=["yuma:C:surprise"]),
  L("narr", "ユウマは、祖父の形見の巻物を広げた。そこには鐘楼と、蒼く光る大きな珠が描かれていた。", cg="gem_scroll", clue="張り紙「湯の龍神さま ご開帳」", flag="poster"),
], next="c2_tavern")

node("c2_dante", bg="tavern", lines=[
  L("dante", "わしか？　なに、ただの金持ちの道楽だよ。宝探しは、男のロマンだからな！", "smile", show=["dante:C:smile"]),
  L("rosa", "……嘘をつくとき、人は少しだけ笑いすぎるのよね。", "think", show=["dante:L:normal", "rosa:R:think"]),
  L("dante", "は、はは。手厳しいな。", "surprise", trust="rosa", flag="dantetalk"),
], next="c2_tavern")
node("c2_barrel", bg="tavern", lines=[
  L("rosa", "百年物のお酒？　……さすがに、やめておくわ。", "smile", show=["rosa:C:smile"], yomi="百年ものの、お酒？　……さすがに、やめておくわ。"),
  L("lumi", "賢明です。"),
], next="c2_tavern")
node("c2_hello", bg="tavern", lines=[
  L("yuma", "お、おじゃましてます……。", "smile", show=["yuma:C:smile"]),
  L("lumi", "ユウマさん。骸骨は、返事をしません。"),
], next="c2_tavern")

node("c2_floor", bg="tavern", lines=[
  L("narr", "鍵を差し込むと、床の扉が、ギギ……と開いた。", show=[]),
  L("narr", "次の瞬間――。"),
  L("narr", "カタカタカタッ！　床下から、いくつもの骸骨が飛び出してきた！", flash=True, shake=True, se="scare"),
  L("gantz", "仕掛け人形だ！　叩いて、押し戻せ！", "angry", show=["gantz:C:angry"]),
], next="c2_action")
node("c2_action", type="action", bg="tavern", place="罠突破：骸骨の酒場", title="飛び出す骸骨を、タップで押し戻せ！", hits=8, window=1.7, lines=[], next="c2_action_ok")
node("c2_action_ok", bg="tavern", lines=[
  L("narr", "最後の一体を押し戻すと、カタン、と音がして、骸骨たちは動きを止めた。", show=[]),
  L("eris", "バネ仕掛けだ……。誰かが、侵入者を追い返すために作ったんだね。", "think", show=["eris:L:think", "gantz:R:normal"]),
  L("gantz", "この作り……どこかで……。", "think"),
], next="c2_under")
node("c2_under", bg="under", place="地下通路の入口", lines=[
  L("narr", "床下には、石造りの通路が、奥へと続いていた。", show=[]),
  L("lumi", "反応が強くなりました。……この先です。", lumiGlow=True),
  L("mio", "この通路、鐘楼の方角へ、まっすぐ延びてる。", "think", show=["mio:C:think"]),
  L("yuma", "よし、行こう！　龍神の珠は、きっとこの先だ！", "smile", show=["yuma:L:smile", "mio:R:normal"]),
  L("narr", "そのとき、通路の奥で、何かが動く音がした――。"),
], next="c2_end")
node("c2_end", type="end", bg="black", title="第2章　骸骨の酒場 ― 完", lines=[], text="第3章「鳴らない鐘楼」へつづく", next="c3_open")

n = 0
for nd in N:
    for ln in nd["lines"]:
        n += 1; ln["voice"] = f"d117_c2_{n:03d}"
out = os.path.expanduser("~/mnt/TitanUnity100/Days/day117/Project/Assets/D117/Resources/Scenario/ch2.json")
json.dump({"title": "第2章 骸骨の酒場", "start": "c2_open", "nodes": N}, open(out, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
ids = {x["id"] for x in N} | {"c2_open"}
bad = [(x["id"], t) for x in N for t in [x.get("next"), x.get("exitTo")] + [c["to"] for c in x.get("choices", [])] if t and t not in ids]
print("nodes", len(N), "lines", n, "bad", bad)
# 声の割り当て
V={"narr":(84,0.95),"yuma":(12,1.05),"gantz":(13,1.0),"leon":(52,1.0),"eris":(8,1.08),"rosa":(65,0.97),"mio":(14,1.0),"sakura":(2,1.0),"dante":(53,1.0),"lumi":(47,1.05)}
EX={("yuma","smile"):32,("yuma","fear"):33,("yuma","angry"):34,("gantz","angry"):81,("gantz","smile"):83}
out=[]
for nd in N:
    for l in nd["lines"]:
        sp,spd=V[l["who"]]; sp=EX.get((l["who"],l.get("expr")),sp)
        t=l["text"].replace("\n","。").replace("【","").replace("】","、")
        out.append({"speaker":sp,"text":t,"filename":l["voice"],"speed":spd})
for i in range(0,len(out),20): json.dump(out[i:i+20],open(f"/tmp/v2b{i//20}.json","w"),ensure_ascii=False)
