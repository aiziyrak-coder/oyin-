# Background quality / 2026-09-27

Status: NOT 4K. Built-in image generation was explicitly asked for 3840x2160, but returned 1672x941. Do not label this output native 4K or silently resize it. The sharper home preview is retained here, not installed in the game. Other backgrounds have not yet been regenerated.

Mode: built-in image_gen, edit of clean-home.png. Output: home-detail-preview.png (1672x941).

Exact prompt:

Edit the attached Lynxos game home background only to restore sharpness and genuinely detailed 4K quality. Output exactly 3840x2160 pixels landscape 16:9. Preserve this exact penthouse architecture, camera perspective, positions of sofas and planters, open central reflective floor for a separate game avatar, dark left wall, enormous window, futuristic waterfront skyline with two tall spires and two flying vehicles, warm sunset from right and cool blue palette. Reconstruct fine architectural window grids, crisp foliage, marble grain, fabric texture and realistic reflections with natural detailed photorealistic AAA-game environment rendering, not blurred enlargement. Deep focus with clear city details; avoid excessive bloom, haze, painterly smearing or sharpening halos. No people, no UI, no text, no watermark, no new objects. Preserve composition and overall colors. This is a production background texture, not a UI mockup. Deliver the full native 3840 by 2160 raster.

The Unity lobby importer now uses uncompressed RGBA32 on desktop with a 4096 cap and validates imported dimensions against the source. This prevents compression/downscaling; it does not restore missing source detail. Exact-resolution CLI/API generation requires user confirmation and a locally configured OPENAI_API_KEY; do not paste secrets in chat.
