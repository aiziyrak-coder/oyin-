import json, copy, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")
sys.path.insert(0, r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\verify_edu")
import numpy as np
from m import ink
SP = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\spec"
s = json.load(open(SP + r"\education.json", encoding="utf-8"))
E = {e["id"]: e for e in s["elements"]}
log = []
def setbox(i, b, why):
    old = E[i]["box"]; E[i]["box"] = [round(v, 1) for v in b]
    log.append(f"{i}: box {old} -> {E[i]['box']} ({why})")
def setf(i, k, v, why):
    old = E[i].get(k); E[i][k] = v
    log.append(f"{i}: {k} {old!r} -> {v!r} ({why})")

# ---- 1. automatic ink re-measure of text/icon boxes (50% crossing), apply when off by > 0.3 px
skip = {"nav.brand_icon", "header.title", "nav.bell", "nav.bell_dot"}
for e in s["elements"]:
    if e["kind"] not in ("text", "tab", "icon") or e["id"] in skip:
        continue
    b = e["box"]; g = 2
    X0, Y0, X1, Y1 = int(np.floor(b[0]-g)), int(np.floor(b[1]-g)), int(np.ceil(b[2]+g)), int(np.ceil(b[3]+g))
    r = ink(X0, Y0, X1, Y1, quiet=True)["box"]
    if max(abs(r[k]-b[k]) for k in range(4)) > 0.3:
        setbox(e["id"], r, "re-measured ink box, 50% coverage crossing")

# ---- 2. manual corrections
setbox("header.title", [14.0, 45.8, 140.3, 61.8], "ink: 'q' descender ends at 61.7-61.8, ascender top 45.85, 'i' right edge 140.2 (was 62.3/45.6/140.5)")
setbox("nav.brand_icon", [12.9, 11.4, 28.2, 27.1], "logo tile edges measured on saturation")
setbox("nav.profile_pill", [419.3, 10.0, 493.0, 30.0], "right edge 493.0 (px 492 fill, 493 background); left end hidden under avatar")
setbox("nav.profile_avatar", [419.5, 10.4, 438.2, 28.8], "avatar disc incl. its light left ring spans x 419.5-438.2 (was shifted ~1 px right)")
setf("nav.profile_name", "fontPx", 6.5, "cap height ~4.4-4.7 px -> ~6.5 px em")

# side rows: pixel-aligned 24 px rows, 1 px light border on the first/last pixel row/col
rowsT = [110, 135, 159, 184, 209, 234, 259]
for n, T in enumerate(rowsT, start=1):
    setbox(f"side.item_{n}", [12.9, T, 117.0, T + 24], "row outer edges: border pixel col 13 / col 116 and rows T / T+23 -> box [12.9,T,117,T+24]; spec was ~0.5-1 px too high and 0.6 px too narrow")
    E[f"side.item_{n}"]["notes"] = ("Dark glass row, 1 px lighter border (#26374B-ish). Composite fill has a lighting gradient: darker on the left "
        "(~#152335) to lighter on the right (~#1F3046); 'fill' is the right-side sample. Rows are 24 px tall, pitch ~24.8, gap 0-1 px. No enclosing side.panel.")
setbox("side.item_0", [12.7, 88.8, 116.3, 108.7], "selected pill: top 88.8 (50% crossing), right 116.3")
setf("side.item_0", "radius", 5, "corner profile fits r~5")
setf("side.item_0", "pad", 4, "blue outer glow reaches ~5 px left / ~3 px right, above and below the pill; pad 2 left a glow ring")
E["side.item_0"]["notes"] = ("Selected row: solid bright blue #2264FC, r~5, soft blue outer glow ~3-5 px (#1E4FD0 ~50%) most visible on the left and below. "
    "Shorter (19.9 px) than the normal rows (24 px) and vertically centred on its label.")

# hero
setbox("education.hero_image", [133.0, 42.0, 493.0, 180.0], "outer edge incl. the 1 px light border (border pixels: col 133, col 492, row 42, row 179); picture content is [134,43,492,179]")
E["education.hero_image"]["notes"] = ("ART ASSET (not removed from page). Futuristic education campus atrium: curved glass balconies, palm trees, warm interior lights, "
    "blue sky oval at top. Box = outer edge incl. a faint 1 px light border (~#3A4658 composite, i.e. white ~12%); the picture itself fills the 1 px inset "
    "[134,43,492,179] (asset box). Corner radius ~5.")
setbox("education.hero_card", [336.5, 51.0, 492.5, 167.8], "top border pixel row 51 -> top 51.0; bottom border row 167 -> ~167.8; left 50% crossing 336.5")
# rec cards
tiles = [(133.0, 219.0), (224.0, 310.0), (315.0, 401.0), (407.0, 492.0)]
for n, (a, b) in enumerate(tiles):
    setbox(f"education.rec_{n}", [a, 214.0, b, 292.3], "outer edges of the 1 px card border: top border row 214 (spec 213.5), bottom border row 291 + AA -> 292.3" + (", left border col 407 (spec 406.4)" if n == 3 else ""))
    setbox(f"education.rec_{n}_image", [a + 1, 215.0, b - 1, 269.3], "picture only, inside the 1 px border")
    setbox(f"education.rec_{n}_caption", [a, 269.3, b, 292.3], "caption bar to the tile's outer edge")
for asset in s["assets"]:
    if asset["name"] == "education.hero_image":
        asset["box"] = [134.0, 43.0, 492.0, 179.0]
        asset["notes"] = ("Hero picture content inside its 1 px light border (futuristic campus atrium). Remove the 'Lynxos Education' overlay card "
            "[336.5,51,492.5,167.8] incl. its title/subtitle/'Tashrif buyurish' button (inpaint), and the header title's trailing 'ri' letters "
            "overlapping the top-left corner (x 133-142.5, y 43-63.8 with pad). The card covers ~43% of the width on the right, so the inpaint "
            "there is approximate (sky, glass balconies, palm, lobby floor continue under it). Rounded corners r~4 when used.")
    else:
        n = int(asset["name"].split("_")[1])
        a, b = tiles[n]
        asset["box"] = [a + 1, 215.0, b - 1, 269.3]
        asset["notes"] = asset["notes"].replace("top corners rounded r~5 + 1px border to trim.", "box is inside the 1 px card border; top corners rounded r~4.")
log.append("assets: hero asset box -> [134,43,492,179]; rec_N_image asset boxes -> inside the 1 px card border (match rec_N_image elements)")

# text
setf("header.subtitle", "text", "Bilm \u2014 kelajak kalti", "the first word is drawn B-i-l-m (i with dot at x22, l at x24, then m), not 'Bilim'")
E["header.subtitle"]["intended"] = "Bilim \u2014 kelajak kaliti"
# colours
setf("education.rec_all", "color", "#A2C2F4", "brightest core pixels #A5C8FA/#A4BDF2/#9ABEFB; old value was AA-darkened")
setf("education.rec_all_arrow", "color", "#A2C2F4", "same link colour as the 'Barchasi' text; the thin arrow's AA core only reaches ~#87ACDF")
# pads: text directly on background art -> 2
for i in ["nav.brand_text", "nav.tab_home", "nav.tab_world", "nav.tab_friends", "nav.tab_top", "nav.tab_settings", "nav.lang_globe", "nav.lang_text",
          "nav.lang_chevron", "nav.bell", "header.subtitle", "education.rec_title", "education.rec_all", "education.rec_all_arrow"]:
    if E[i].get("pad", 0) < 2:
        E[i]["pad"] = 2
log.append("pad 1.5 -> 2 for text/icons drawn directly on the background (nav, header subtitle, rec heading/link): 1 px dark text halo")
setf("nav.bell_dot", "pad", 1.5, "red AA fringe reaches ~1.5 px")
s["background"]["notes"] += (" A soft light streak/band in the blurred backdrop runs horizontally at y~34-42 (x~130-450) and a faint blurred "
    "reflection of the hero sits under it (y~180-190): background art, not a nav bar or UI.")
json.dump(s, open(SP + r"\education.verified.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("\n".join(log)); print(len(s["elements"]), "elements")
