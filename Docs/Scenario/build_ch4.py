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
    d = {"id": id, "type": type, "chapter": 4, "lines": list(lines)}; d.update(k); N.append(d)


MAP = ["############",
       "#S...P....##",
       "#########.##",
       "##..V.....##",
       "##.#########",
       "##.....T...#",
       "##########.#",
       "#H....D....#",
       "#.##########",
       "#G##########",
       "############",
       "############"]

node("c4_open", bg="under", place="地下迷宮・入口", lines=[
  L("narr", "第四章　地下迷宮", show=[]),
  L("narr", "鐘楼の下の階段を降りきると、そこは石と木の梁でできた、迷路のような坑道だった。"),
  L("mio", "通った道は、わたしが全部描く。みんなは前を見てて。", "think", show=["mio:C:think"]),
  L("gantz", "坑道は俺の庭みたいなもんだ。足元は任せろ。", "smile", show=["mio:L:normal", "gantz:R:smile"]),
  L("lumi", "龍神の珠の反応は、迷宮の奥です。近づくほど、わたしは明るくなります。", lumiGlow=True),
], next="c4_maze")

node("c4_maze", type="dungeon", bg="black", place="地下迷宮", lines=[],
  map="/".join(MAP), events="P:c4_pit,V:c4_vapor,T:c4_statue,D:c4_door,H:c4_helmet,G:c4_exit")

node("c4_pit", type="choice", bg="under", prompt="床が抜けそうだ。どうする？", lines=[
  L("gantz", "止まれ！　その先の床、下が空洞だ。音が違う。", "angry", show=["gantz:C:angry"]),
], choices=[
  C("一気に跳び越える", "c4_pit_jump"),
  C("ガンツの言う通り、端を伝って歩く", "c4_pit_edge", trust="gantz"),
])
node("c4_pit_jump", bg="under", lines=[
  L("narr", "ズボッ！　着地した床が抜け、ユウマの片足が穴に落ちた！", flash=True, shake=True, se="scare", life=-1),
  L("yuma", "いってて……！", "fear", show=["yuma:C:fear"]),
  L("gantz", "だから言ったろう。坑道をなめるな。", "think"),
], next="c4_maze")
node("c4_pit_edge", bg="under", lines=[
  L("narr", "壁に背中をつけ、穴の縁を一歩ずつ進む。全員、無事に渡りきった。", show=[]),
  L("gantz", "上出来だ、坊主。", "smile", show=["gantz:C:smile"]),
], next="c4_maze")

node("c4_vapor", type="choice", bg="under", prompt="毒の湯気だ。どうする？", lines=[
  L("narr", "通路の床の割れ目から、ツンとにおう白い湯気が噴き出している。", show=[]),
  L("eris", "待って！　これ、吸ったらまずいやつ。百年前に村を襲った湯気と同じだよ！", "surprise", show=["eris:C:surprise"]),
], choices=[
  C("息を止めて走り抜ける", "c4_vapor_run"),
  C("エリスに中和してもらう", "c4_vapor_eris", trust="eris", clue="毒の湯気は鉱物の反応。中和できる"),
])
node("c4_vapor_run", bg="under", lines=[
  L("narr", "ゴホッ、ゴホッ！　走り抜けたものの、目と喉がひりひりする。", flash=True, se="scare", life=-1),
  L("eris", "もう、無茶するんだから！", "angry", show=["eris:C:angry"]),
], next="c4_maze")
node("c4_vapor_eris", bg="under", lines=[
  L("eris", "この粉をまけば……よし！　湯気の毒が消えた！", "smile", show=["eris:C:smile"]),
  L("eris", "やっぱり。この毒、龍神の珠の熱と、地下の鉱物が反応して出てるんだ。", "think"),
  L("dante", "……熱と、鉱物の反応。", "think", show=["eris:L:normal", "dante:R:think"]),
], next="c4_maze")

node("c4_statue", bg="under", lines=[
  L("narr", "回廊の両側に、苔むした石像がずらりと並んでいる。", show=[]),
  L("narr", "ゴゴゴ……。石像の目が、蒼く光った。", se="drop"),
  L("narr", "石像たちが、腕を振り上げて動き出した！", flash=True, shake=True, se="scare"),
  L("leon", "数が多い！　でも動きは単純だ。順番に押し返せば抜けられる！", "surprise", show=["leon:C:surprise"]),
], next="c4_statue_act")
node("c4_statue_act", type="action", bg="under", place="罠突破：石像の回廊", title="動き出した石像を、タップで押し返せ！", hits=10, window=1.4, sprite="statue", lines=[], next="c4_statue_ok")
node("c4_statue_ok", bg="under", lines=[
  L("narr", "最後の石像が、ゴトン、と元の台座に戻った。", show=[]),
  L("gantz", "この仕掛け……酒場の骸骨と同じ作りだ。同じ人間が作ったんだな。", "think", show=["gantz:C:think"]),
], next="c4_maze")

node("c4_door", bg="under", lines=[
  L("narr", "行く手を、分厚い鉄の扉がふさいでいた。扉には、数字を合わせる古い錠前。", show=[]),
  L("leon", "錠前の上に、何か刻まれている。「鐘の声を、ふたたび」……。", "think", show=["leon:C:think"]),
  L("leon", "鐘の声。あの鐘は、二回、そして四回鳴った。", "smile"),
], next="c4_door_q")
node("c4_door_q", type="input", bg="under", place="数字錠", prompt="錠前の数字を合わせよう", kana="1234567890", answer="24", note="扉の文字：「鐘の声を、ふたたび」\n鐘楼の鐘：二回鳴って、間をおいて四回", noteLabel="▼ 扉と鐘を見る",
  hint="ヒントです。鐘楼の鐘は、何回、何回と鳴りましたか？", next="c4_door_ok", lines=[])
node("c4_door_ok", bg="under", lines=[
  L("narr", "カチリ。重い扉が、ゆっくりと開いた。", se="drop"),
  L("leon", "数字は、裏切らないね。", "smile", show=["leon:C:smile"], trust="leon"),
], next="c4_maze")

node("c4_helmet", bg="under", lines=[
  L("narr", "通路の行き止まりに、古びたヘルメットと、折りたたまれた手紙が置かれていた。", show=[], cg="helmet"),
  L("gantz", "これは……親父のヘルメットだ！", "surprise", show=["gantz:C:surprise"]),
  L("narr", "手紙には、震える字でこう書かれていた。", cg="helmet"),
  L("narr", "「湯気にやられたところを、湯守の老人に助けられた。宝のことは、もういい。この村は、そっとしておくべきだ」"),
  L("gantz", "親父……生きて、ここを出たのか……？", "fear", show=["gantz:C:fear"], clue="ガンツの父は湯守に助けられた", trust="gantz"),
  L("yuma", "きっとそうだよ。一緒に、確かめに行こう。", "smile", show=["yuma:L:smile", "gantz:R:normal"]),
], next="c4_maze")

node("c4_exit", bg="under", place="迷宮の最奥", lines=[
  L("narr", "迷宮の最奥。そこには、蒼く光る紋様が刻まれた、巨大な石の扉がそびえていた。", show=[]),
  L("lumi", "反応、最大です。……この扉の向こうに、龍神の珠があります。", lumiGlow=True),
  L("dante", "……ついに、ここまで来たか。", "think", show=["dante:C:think"]),
  L("rosa", "ダンテさん。そろそろ、本当のことを話してくれてもいいんじゃない？", "think", show=["dante:L:fear", "rosa:R:think"]),
  L("narr", "ダンテは答えず、ただ、封印された扉を見つめていた――。"),
], next="c4_end")
node("c4_end", type="end", bg="black", title="第4章　地下迷宮 ― 完", lines=[], text="第5章「封印された扉」へつづく")

n = 0
for nd in N:
    for ln in nd["lines"]:
        n += 1; ln["voice"] = f"d117_c4_{n:03d}"
out = os.path.expanduser("~/mnt/TitanUnity100/Days/day117/Project/Assets/D117/Resources/Scenario/ch4.json")
json.dump({"title": "第4章 地下迷宮", "start": "c4_open", "nodes": N}, open(out, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
ids = {x["id"] for x in N}
bad = [(x["id"], t) for x in N for t in [x.get("next"), x.get("exitTo")] + [c["to"] for c in x.get("choices", [])] if t and t not in ids]
bad += [e for e in "P:c4_pit,V:c4_vapor,T:c4_statue,D:c4_door,H:c4_helmet,G:c4_exit".split(",") if e.split(":")[1] not in ids]
print("nodes", len(N), "lines", n, "bad", bad, "maprows", set(len(r) for r in MAP))
V={"narr":(84,0.95),"yuma":(12,1.05),"gantz":(13,1.0),"leon":(52,1.0),"eris":(8,1.08),"rosa":(65,0.97),"mio":(14,1.0),"sakura":(2,1.0),"dante":(53,1.0),"lumi":(47,1.05)}
EX={("yuma","smile"):32,("yuma","fear"):33,("yuma","angry"):34,("gantz","angry"):81,("gantz","smile"):83}
out=[]
for nd in N:
    for l in nd["lines"]:
        sp,spd=V[l["who"]]; sp=EX.get((l["who"],l.get("expr")),sp)
        t=l["text"].replace("「","").replace("」","")
        out.append({"speaker":sp,"text":t,"filename":l["voice"],"speed":spd})
for i in range(0,len(out),20): json.dump(out[i:i+20],open(f"/tmp/v4b{i//20}.json","w"),ensure_ascii=False)
