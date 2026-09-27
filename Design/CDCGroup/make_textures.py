"""CDCGroup logo qatlamlarini qirqib, Unity loyihasiga joylaydi.
Ishlatish: make_textures.py <render_papkasi> <Art_papkasi> <layout.json>"""
import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "tools"))
from texlib import crop_layer, write_layout

src, dst, layout_path = sys.argv[1:4]
layout = {}
crop_layer(src, dst, layout, "cdc", "CDC_Letters", pad=2)
crop_layer(src, dst, layout, "line", "CDC_Line", pad=0)
crop_layer(src, dst, layout, "group", "CDC_Group", pad=2)
write_layout(layout, layout_path)
