# Animations

Exploration loads Resources/VeinVanguard/ExplorationWalk.png and creates sixteen sprites from the 4x4 sheet. Rows from top: down, right, left, up. Actual displacement controls direction and animation; blocking terrain or releasing movement stops the cycle. The last facing direction is retained. Playback is 8 frames per second.

Battle uses the existing Unity_MC/Frames poses. Diagnose plays for 0.65 seconds before its result popup. Synthesize asks for a nutrient first, then plays synthesize and nutrient_attack. Restore asks the trivia question first, then plays restore after the answer; a correct answer also plays homeostasis_activate before feedback. Enemy hits use homeostasis_hit while a shield is active. Input is locked during action animations.

Verified in live Unity Play Mode: sixteen walking sprites load, world movement changes position, Diagnose popup is hidden during animation and shown afterward, nutrient selection starts synthesize with popup hidden, Restore applies energy and shield while playing with popup hidden. Existing battle-rule verification passes. Homeostasis feedback and compilation status checked after playback. Visual alignment and animation smoothness still require an art pass on the generated walking sheet.
