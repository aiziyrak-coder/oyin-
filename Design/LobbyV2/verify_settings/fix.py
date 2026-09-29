import json, copy

SRC = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\spec\settings.json"
DST = r"C:\Users\alocomputers\AppData\Local\Temp\claude\D--Game1\4f1a5cc1-c33d-4082-8691-871fd7139403\scratchpad\v2\spec\settings.verified.json"

s = json.load(open(SRC, encoding="utf-8"))
E = {e["id"]: e for e in s["elements"]}


def setbox(i, b):
    E[i]["box"] = [round(float(v), 2) for v in b]


def note(i, txt, replace=False):
    if replace or not E[i].get("notes"):
        E[i]["notes"] = txt
    else:
        E[i]["notes"] = E[i]["notes"].rstrip() + " " + txt


# ---------------- nav ----------------
setbox("nav.brand_icon", [13.9, 11.3, 29.1, 27.0])
setbox("nav.lang_globe", [335.1, 14.3, 347.2, 26.9])
setbox("nav.lang_text", [352.9, 17.6, 362.0, 23.4])
note("nav.lang_text", "Z right edge measured at x=362.0 (column 361 is full ink).")
setbox("nav.bell", [386.2, 14.2, 397.0, 26.9])
note("nav.bell", "Bell knob top at y=14.2; the red dot overlaps its top-right shoulder.")
setbox("nav.bell_dot", [394.3, 14.4, 398.1, 18.0])
note("nav.bell_dot", "Centre ~(396.2,16.2), d~3.7 (50% crossings of the R-G channel).")
setbox("nav.profile_avatar", [407.0, 11.0, 426.0, 29.8])
note("nav.profile_avatar", "Measured: the avatar's left rim is flush with the pill's left edge (x=407.0); diameter ~19, centre ~(416.5,20.4).",)
for i in ("nav.tab_home", "nav.tab_world", "nav.tab_friends", "nav.tab_top", "nav.tab_settings"):
    pass

# ---------------- header ----------------
# (boxes verified within 0.1 px)

# ---------------- side list ----------------
tiles = {
    0: [13.0, 85.4, 125.2, 105.7],
    1: [13.0, 107.0, 125.3, 128.3],
    2: [13.0, 129.3, 125.3, 150.9],
    3: [13.0, 151.9, 125.3, 172.7],
    4: [13.0, 173.7, 125.3, 196.0],
    5: [13.0, 197.0, 125.3, 218.7],
    6: [13.0, 219.7, 125.3, 242.0],
    7: [13.0, 243.0, 125.3, 265.1],
}
for k, b in tiles.items():
    setbox(f"side.item_{k}", b)
    if k > 0:
        E[f"side.item_{k}"]["fill"] = "#1C2A3CE6"
        note(f"side.item_{k}", "Dark near-opaque glass tile: observed composite #1A2839..#1D2B3C, fill modelled as #1C2A3C @ 90% (fit over the page bg left/right of the list); 1px lighter inner border (~#FFFFFF1A, reads as L~41-48). Tiles are separated by a ~1px dark gap; measured heights 21.0-22.3 px, pitch ~22.7; item_7 bottom (265.1) aligns with the content panel bottom (265.0).", replace=True)
note("side.item_0", "Selected row: solid bright blue (#1F5EFD centre, sampled), slightly lighter 1px top/bottom rim; soft blue OUTER GLOW (#2060FF, ~5px to the left, ~4px below, ~3px right, weak above) - covered by pad 6. Height only ~20.3 px (thinner than the grey tiles). A 2px +7-lum speck at (114,94) inside it is a JPEG artifact, not an icon.", replace=True)
note("side.item_0_icon", "User icon: head drawn as an OUTLINE ring, shoulders/body FILLED, white. Icon centre x~29.0.", replace=True)
setbox("side.item_6_label", [46.0, 227.4, 53.6, 233.4])

# ---------------- content panel ----------------
setbox("settings.panel", [183.5, 53.0, 484.0, 265.0])
E["settings.panel"]["fill"] = "#36425A68"
note("settings.panel", "Large glass card with backdrop blur. Observed composite #25334A..#26354A on the left, #1D314A..#344055 on the right (it follows the page gradient), i.e. only +8..+20 per channel over the bg; least-squares fit of 8 inside/outside pairs gives tint #36425A @ ~41% (0x68). 1px lighter border (~#FFFFFF1F) drawn as the outermost pixel row/column (x=184, x=482-483, y=53, y=264). Radius ~6. Bottom edge y=265.0 aligns with the side list bottom.", replace=True)

setbox("settings.avatar", [197.5, 64.2, 244.3, 111.1])
note("settings.avatar", "Circular portrait photo (young man, dark hair, dark suit, white shirt, light grey studio bg), diameter ~46.8, centre (220.9, 87.65); ~1.5px light steel ring at r~22-23 (median #828C9D, brightest ~#8892A8).", replace=True)

setbox("settings.edit_button", [420.3, 78.4, 470.8, 98.1])
E["settings.edit_button"]["radius"] = 4.0
E["settings.edit_button"]["fill"] = "#95ADEF40"
note("settings.edit_button", "Secondary glass button. Observed composite #3B4D6A (panel around it #1D2D3F..#1D3048); fill #95ADEF @ 25% reproduces it. Faint 1px lighter top rim. Label is centred (label centre 445.6/88.0 vs button centre 445.55/88.25).", replace=True)

seps = {0: [119.2, 120.2], 1: [147.2, 148.2], 2: [175.0, 176.0], 3: [203.2, 204.2], 4: [231.2, 232.2]}
for k, (a, b) in seps.items():
    i = f"settings.sep_{k}"
    setbox(i, [200.0, a, 470.0, b])
    note(i, "1px hairline divider (~8% white over the panel, peak +8..+13 L). Horizontal alpha ramp: 10% at x~209, 50% at x~219, full from ~x235 to ~440, 50% at x~457, ~20% under the chevrons, 0 at x=470. Dividers are 28.0 px apart (centres 119.7, 147.7, 175.5, 203.7, 231.7)." + (" This first divider separates the profile header from the rows." if k == 0 else ""), replace=True)

# chevrons: use 50%-crossing ink boxes
chev = {0: [462.3, 130.5, 466.0, 136.7], 1: [462.3, 158.7, 466.1, 164.7], 2: [462.2, 186.4, 466.0, 192.8],
        3: [462.4, 214.5, 466.1, 220.6], 4: [462.4, 242.5, 466.1, 248.6]}
for k, b in chev.items():
    i = f"settings.row_{k}_chevron"
    setbox(i, b)
    note(i, "Right chevron, ~1.2px stroke, ink box ~3.7x6.1 (50% crossings), right edge x~466.0 (18 px from the panel's right edge); vertically centred on the row label.", replace=True)

# text transcription: visible vs intended
E["settings.id"]["text"] = "ID: \u00f8842193"
E["settings.id"]["intended"] = "ID: #842193"
note("settings.id", "Cap top to baseline. The '#' is drawn as a blurry slashed-o glyph; intended '#'. Muted grey-blue.", replace=True)
E["settings.row_2_value"]["text"] = "O'zbekist\u00f3n"
E["settings.row_2_value"]["intended"] = "O'zbekiston"
note("settings.row_2_value", "Cap top to baseline (no descenders). Value column x=269. The 'o' of '-ston' carries a stray AI accent; intended plain O'zbekiston.", replace=True)

# drop invisible logical row boxes (nothing drawn); geometry moved to top-level notes
s["elements"] = [e for e in s["elements"] if not (e["id"].startswith("settings.row_") and e["id"].count("_") == 1)]

# remove flags + pads (all elements are UI drawn over the blurred background)
pad = {"text": 1.5, "tab": 1.5, "icon": 1.5, "dot": 1.0, "pill": 2.0, "avatar": 1.0, "button": 2.0, "panel": 3.0, "line": 1.5}
for e in s["elements"]:
    e["remove"] = True
    e["pad"] = pad.get(e["kind"], 2.0)
E["header.title"]["pad"] = 2.0
E["nav.brand_icon"]["pad"] = 2.0
E["side.item_0"]["pad"] = 6.0
E["settings.avatar"]["pad"] = 1.5
E["settings.edit_button"]["pad"] = 2.0

# assets follow the corrected avatar boxes
for a in s["assets"]:
    if a["name"] == "settings.avatar_photo":
        a["box"] = [197.5, 64.2, 244.3, 111.1]
        a["notes"] = "Circular portrait photo of the player (young man, dark hair, suit). Crop and apply a circular mask (centre 220.9,87.65, r~22.2 inside the ring); the ~1.5px light ring (#8892A8) is UI and should be drawn separately."
    if a["name"] == "nav.profile_avatar_photo":
        a["box"] = [407.0, 11.0, 426.0, 29.8]
    if a["name"] == "nav.brand_icon":
        a["box"] = [13.9, 11.3, 29.1, 27.0]

s["notes"] = (s["notes"].rstrip() + " Content rows (no visible row backgrounds): 5 rows between dividers at pitch 28.0 px; row centres y~133.7, 161.6, 189.6, 217.5, 245.5; label column x=201, value column x=269, chevrons right-aligned at x~466. "
              "All UI elements carry remove:true (page bg is plain blurred navy, so a cleaned bg can be produced with mask+inpaint if wanted). Verified by an independent pass (50% ink crossings + pixel profiles).")

json.dump(s, open(DST, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("written", DST, len(s["elements"]), "elements")
