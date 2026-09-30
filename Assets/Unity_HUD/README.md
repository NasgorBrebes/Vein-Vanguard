# Vein Vanguard — Medical HUD

Paket visual HUD: 12 PNG komponen transparan, atlas, dan mockup landscape. Ini aset visual dan importer, belum prefab HUD interaktif atau integrasi BattleManager.

## Isi
- status_panel: panel putih dengan motif EKG.
- bar_track, hp_fill (mint), energy_fill (cyan).
- button_normal, button_selected, button_pressed, button_disabled.
- icon_diagnose, icon_synthesize, icon_restore, icon_homeostasis.
- Preview/battle_hud_mockup.png: contoh penempatan saja. Karakter dan arena pada preview adalah ilustrasi generatif, bukan pengganti sprite proyek.

## Unity
1. Salin Unity_HUD ke Assets. Jalankan Tools > Vein Vanguard > Import Medical HUD Sprites.
2. Buat Canvas Screen Space Overlay, Canvas Scaler Scale With Screen Size 1920x1080, Match 0.5; sesuaikan safe area Android.
3. Panel kiri atas: Integritas Pembuluh Darah (HP) dan Energi Metabolisme (MP). Tengah atas: giliran pemain/musuh. Kanan atas: nama dan HP musuh.
4. Bawah: tombol Diagnose, Synthesize, Restore. Gunakan label dan nilai sebagai TextMeshPro terpisah, bukan teks yang menyatu dalam PNG.
5. Bar fill memakai Image Filled, Horizontal, origin Left. Isi fillAmount dari nilai sekarang/maksimum. Sesuaikan padding agar fill masuk di dalam track; gambar tidak dirancang sebagai mask presisi.
6. Tombol memakai Button dengan Sprite Swap. Pasang normal/selected/pressed/disabled sesuai status. Ikon dan teks menjadi child Image/Text dengan raycastTarget=false.
7. Ikon Homeostasis di kiri bawah. Pause dapat dibuat dari tombol UI dan label terpisah.

Gunakan Preserve Aspect untuk ikon. Panel dan tombol belum memiliki border 9-slice yang dikalibrasi; atur di Sprite Editor sebelum memperlebar secara signifikan. Ketebalan outline dan ukuran visual antar-state hasil generatif masih perlu penyesuaian saat merakit UI.

PNG dipisahkan lokal dan memiliki alpha transparan. Importer belum dijalankan di Unity. Panel detail Diagnose, pemilihan nutrisi Synthesize, dan kuis Restore belum termasuk dalam paket HUD utama ini.

## Arahan generasi
Generator gambar bawaan. Atlas: gaya alat medis kartun putih/cyan/navy/mint, EKG, grid 4x3 berisi panel, track dan dua fill, empat state tombol, empat ikon medis; transparan dan tanpa teks. Mockup: landscape, HUD HP/MP atas kiri, giliran tengah, musuh kanan, tiga aksi di bawah, panggung battle di tengah, tanpa QTE/AUTO.
