"""CDCGroup logo qatlamlarini qirqadi va neytral (kumush) nurni yaratadi.
Ishlatish: make_textures.py <render_papkasi> <Art_papkasi> <layout.json>"""
import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "tools"))
from texlib import crop_layer, radial_glow, write_layout

src, dst, layout_path = sys.argv[1:4]
layout = {}
crop_layer(src, dst, layout, "emblem", "CDC_Emblem")
crop_layer(src, dst, layout, "wordmark", "CDC_Wordmark")
crop_layer(src, dst, layout, "highlight", "CDC_Highlight")
crop_layer(src, dst, layout, "line", "CDC_Line", pad=2)
radial_glow(f"{dst}/CDC_Glow.png", [235, 240, 248], [150, 160, 175])
write_layout(layout, layout_path)
