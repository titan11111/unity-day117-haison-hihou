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
    d = {"id": id, "type": type, "chapter": 3, "lines": list(lines)}; d.update(k); N.append(d)


node("c3_open", bg="tower", place="鳴らない鐘楼", lines=[
  L("narr", "第三章　鳴らない鐘楼", show=[]),
  L("narr", "地下通路の先の階段をのぼると、そこは鐘楼の真下だった。見上げれば、大きな青銅の釣鐘。"),
  L("yuma", "ここ……じいちゃんの巻物に描いてあった鐘楼だ！", "surprise", show=["yuma:C:surprise"]),
  L("sakura", "さっき鳴ったのは、この鐘ね。でも……誰が鳴らしたのかしら。", "fear", show=["yuma:L:normal", "sakura:R:fear"]),
  L("lumi", "周囲に人の反応はありません。……ただ、鐘の下に、何かが隠されています。", lumiGlow=True),
], next="c3_tower")

node("c3_tower", type="explore", bg="tower", place="鳴らない鐘楼",
  require=["stele", "scroll", "bell"], exitText="合言葉を唱える", exitTo="c3_input_intro",
  hint="ヒントです。石碑の文字と、鐘の鳴った回数。その二つを合わせると、言葉になりそうです。",
  lines=[
  L("narr", "鐘楼の足元には、苔むした石碑。軒先には、龍の木彫りの飾りが見下ろしている。", show=[]),
  L("yuma", "よし、手分けして調べよう！", "normal", show=["yuma:C:normal"]),
  ], choices=[
  C("調べる：石碑", "c3_stele", once="stele"),
  C("巻物と見比べる", "c3_scroll", once="scroll"),
  C("鐘を鳴らす", "c3_bell", once="bell", need="stele"),
  C("移動：湯守の屋敷", "c3_mansion", once="mansion"),
  C("仲間に聞く：ガンツ", "c3_gantz", once="gantz3"),
  C("鐘の下で叫ぶ", "c3_shout", miss=True),
  C("見る：空", "c3_sky", miss=True),
])

node("c3_stele", bg="tower", lines=[
  L("narr", "石碑には、八つの文字が、番号と一緒に刻まれていた。", show=[]),
  L("narr", "一・り　二・た　三・の　四・ま　五・じ　六・ゆ　七・う　八・ん"),
  L("sakura", "その下にも一行。「鐘の声を数えよ。その数が、龍の眠る扉をひらく」……ですって。", "think", show=["sakura:C:think"], clue="石碑：一り 二た 三の 四ま 五じ 六ゆ 七う 八ん", flag="stele"),
], next="c3_tower")

node("c3_scroll", bg="tower", lines=[
  L("yuma", "じいちゃんの巻物と見比べてみる。……あれ？　何か違う。", "think", show=["yuma:C:think"], cg="gem_scroll"),
], next="c3_scroll_q")
node("c3_scroll_q", type="choice", bg="tower", prompt="巻物の鐘楼と、本物の鐘楼。違うところは？", lines=[], choices=[
  C("鐘の数", "c3_scroll_ng"),
  C("屋根の龍の飾りの向き", "c3_scroll_ok", clue="巻物の龍は下を向いている＝入口は鐘の真下", flag="scroll", trust="mio"),
  C("柱の本数", "c3_scroll_ng"),
])
node("c3_scroll_ng", bg="tower", lines=[
  L("mio", "そこは同じみたい。もっと上の方、見て。", "normal", show=["mio:C:normal"]),
], next="c3_scroll_q")
node("c3_scroll_ok", bg="tower", lines=[
  L("yuma", "わかった！　本物の龍は空を見てるのに、巻物の龍は下を向いてる！", "smile", show=["yuma:L:smile", "mio:R:surprise"]),
  L("mio", "龍の目線の先……つまり、鐘の真下ね！", "surprise"),
  L("narr", "手掛かり「巻物の龍は下を向いている」を探検手帳に記した。"),
], next="c3_tower")

node("c3_bell", bg="tower", lines=[
  L("gantz", "坊主、下がってろ。俺が鳴らす。", "normal", show=["gantz:C:normal"]),
  L("narr", "ガンツが撞木を引くと、鐘がひとりでに鳴りはじめた。", se="bell", flash=True),
  L("narr", "ゴォン……ゴォン……。　――静寂。　ゴォン……ゴォン……ゴォン……ゴォン……。"),
  L("leon", "二回、間を置いて、四回。……数字の暗号だ。", "think", show=["gantz:L:surprise", "leon:R:think"], clue="鐘の声：二回、間を置いて四回", flag="bell", trust="leon"),
], next="c3_tower")

node("c3_mansion", bg="mansion", place="湯守の屋敷", lines=[
  L("narr", "鐘楼の裏に、小さな屋敷があった。囲炉裏のそばに、古い帳面が開いたまま残っている。", show=[]),
  L("eris", "これ、日誌だよ。最後のページ……読むね。", "think", show=["eris:C:think"]),
  L("narr", "「黒崎の掘削により、源泉より毒の湯気あふる。村人は皆、山向こうへ逃がした。龍神の珠は、我ら湯守が封ず」", yomi="「黒崎の掘削により、源泉より毒の湯気あふる。村人は皆、山向こうへ逃がした。龍神の珠は、我ら湯守がふうず」"),
  L("dante", "……黒崎。", "fear", show=["eris:L:surprise", "dante:R:fear"]),
  L("rosa", "知ってる名前みたいね、ダンテさん。", "think", show=["dante:L:fear", "rosa:R:think"]),
  L("dante", "い、いや……。昔、どこかで聞いたような気がしただけだ。", "think", clue="湯守の日誌：村人は山向こうへ逃げのびた", flag="mansion", trust="rosa"),
], next="c3_tower")

node("c3_gantz", bg="tower", lines=[
  L("gantz", "……この鐘楼、親父の手紙に書いてあったのと同じだ。", "think", show=["gantz:C:think"]),
  L("yuma", "ガンツさんのお父さん？", "surprise", show=["yuma:L:surprise", "gantz:R:normal"]),
  L("gantz", "二十年前、この村に入ったきり帰ってこなかった。……俺は、親父を探しに来たんだ。", "normal", trust="gantz", flag="gantz3"),
], next="c3_tower")
node("c3_shout", bg="tower", lines=[
  L("yuma", "おーい！　誰かいませんかー！", "smile", show=["yuma:C:smile"]),
  L("lumi", "やまびこしか返ってきません。ユウマさんの声が、三回。"),
], next="c3_tower")
node("c3_sky", bg="tower", lines=[
  L("narr", "霧の向こうで、日が沈みかけている。", show=[]),
  L("lumi", "暗くなる前に、急いだほうがよさそうです。"),
], next="c3_tower")

node("c3_input_intro", bg="tower", lines=[
  L("leon", "鐘は二回、そして四回。石碑の二番と四番の文字を、順に読めば……。", "think", show=["leon:C:think"]),
], next="c3_input")
node("c3_input", type="input", bg="tower", place="合言葉", prompt="石碑の文字で、合言葉を入力しよう",
  kana="りたのまじゆうん", answer="たま", note="石碑：一り 二た 三の 四ま 五じ 六ゆ 七う 八ん\n鐘：二回鳴って、間をおいて四回", noteLabel="▼ 石碑と鐘を見る", hint="ヒントです。鐘は「二回」、そして「四回」。石碑の二番目と四番目の文字です。", next="c3_open_gate", lines=[])

node("c3_open_gate", bg="tower", lines=[
  L("narr", "「たま」――そう唱えた瞬間、鐘楼の床板が、音を立てて沈んでいった。", shake=True, se="drop"),
  L("sakura", "珠……！　龍神の珠のことね！", "surprise", show=["sakura:C:surprise"]),
  L("narr", "床の下には、闇の奥へと続く石の階段。ひんやりとした風が吹き上げてくる。"),
  L("mio", "この先は、地図にも載ってない。……迷宮よ。", "fear", show=["mio:L:fear", "yuma:R:normal"]),
  L("yuma", "大丈夫。みんながいる。行こう！", "smile"),
  L("lumi", "光を強くします。足元に気をつけて。", lumiGlow=True),
], next="c3_end")
node("c3_end", type="end", bg="black", title="第3章　鳴らない鐘楼 ― 完", lines=[], text="第4章「地下迷宮」へつづく", next="c4_open")

n = 0
for nd in N:
    for ln in nd["lines"]:
        n += 1; ln["voice"] = f"d117_c3_{n:03d}"
        if ln["text"].startswith("ガンツが撞木"): ln["voice"] = "d117_c3_017b"
out = os.path.expanduser("~/mnt/TitanUnity100/Days/day117/Project/Assets/D117/Resources/Scenario/ch3.json")
json.dump({"title": "第3章 鳴らない鐘楼", "start": "c3_open", "nodes": N}, open(out, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
ids = {x["id"] for x in N}
bad = [(x["id"], t) for x in N for t in [x.get("next"), x.get("exitTo")] + [c["to"] for c in x.get("choices", [])] if t and t not in ids]
print("nodes", len(N), "lines", n, "bad", bad)
V={"narr":(84,0.95),"yuma":(12,1.05),"gantz":(13,1.0),"leon":(52,1.0),"eris":(8,1.08),"rosa":(65,0.97),"mio":(14,1.0),"sakura":(2,1.0),"dante":(53,1.0),"lumi":(47,1.05)}
EX={("yuma","smile"):32,("yuma","fear"):33,("yuma","angry"):34,("gantz","angry"):81,("gantz","smile"):83}
out=[]
for nd in N:
    for l in nd["lines"]:
        sp,spd=V[l["who"]]; sp=EX.get((l["who"],l.get("expr")),sp)
        t=l["text"].replace("「","").replace("」","").replace("・","、")
        out.append({"speaker":sp,"text":t,"filename":l["voice"],"speed":spd})
for i in range(0,len(out),20): json.dump(out[i:i+20],open(f"/tmp/v3b{i//20}.json","w"),ensure_ascii=False)
