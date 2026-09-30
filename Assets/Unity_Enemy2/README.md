# Enemy 2 — Unity
16 transparent RGBA frames, 384x384. Sheet: enemy2_sheet_4x4.png (1536x1536).
Rows: idle (6 FPS, loop), attack (10 FPS), hurt (10 FPS), defeat (7 FPS). Four frames each.
Pivot: (0.5, 0.08333333), or (192,352) from top-left. PPU 100 to match MC.
All sprites use one shared scale; changing height during squash/stretch is intentional.

## Install
1. Copy this entire Unity_Enemy2 folder into your Unity project's Assets folder.
2. After compilation select Tools > Vein Vanguard > Prepare Enemy 2 Animations.
3. Four .anim clips are created in Animations. Add them to the Animator on your SpriteRenderer object and connect the battle logic.
Existing .anim files are preserved. Running the menu reapplies PNG import settings.
Manual import: frames/*.png as Sprite Single; custom pivot above, alpha transparency on, no mipmaps, Bilinear, no compression.
Sheet alternative: Grid by Cell Size 384x384, same custom pivot.

## Validation and limitations
Sixteen isolated foreground components extracted; adjacent sprites excluded even where bounding boxes overlap. Transparent padding retained, same canvas and bottom baseline for all frames. Original generation retained as enemy2_original.png.
PNG packaging verified locally; Unity editor script and animation playback have not been executed here. Generated artwork can still need hand polish for smooth motion.
Built-in image generator used. Prompt follows; image is reference enemy_2.png.

Use case stylized-concept. Generate 16 animation frames for the EXACT green slime pathogen in the reference image, for a 2D side-view turn-based RPG. Preserve identity: bright lime green tall asymmetrical slime silhouette, darker green shading, huge irregular dark green empty mouth opening on its LEFT side, dripping lobes above and below mouth, broad spreading slime base, short sparse BLACK bristle hairs along outer contour. NO EYES, NO TEETH, NO ARMS, NO LEGS, no accessories. Smooth bold dark outlines and flat cartoon colored shapes matching reference. Facing LEFT in all poses. Exact 4 columns by 4 rows grid, square transparent RGBA PNG. Sixteen complete bodies strictly separated by generous transparent gutters on all sides; each sprite limited to central 70 percent of own equal cell. No labels text grid lines shadows background checkerboard or VFX. Row1: 4-frame idle breathing squash and stretch loop, gently shifting mouth. Row2: 4-frame body lunge attack toward LEFT: compress and lean back, stretch left mouth wide, forward impact lunge while base remains grounded, recover. Row3: 4-frame hurt: neutral, flinch leaning right, strong compressed hurt pose, recover. Row4: 4-frame defeat: sag, halfway melt down, almost collapsed, final flat green puddle with bristles and small dark mouth remnant. Keep entire slime including every bristle and puddle inside each cell. Same scale camera and baseline, no detached droplets or particles. Real alpha transparent background. Output image only.

