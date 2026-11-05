# Menu, eksplorasi, dan presentasi

Scene `Assets/Scenes/VeinVanguard.unity` menyediakan menu Start, Settings, dan Credits; area eksplorasi yang memakai `Assets/map_2.png`; encounter saat pemain mendekati Lipid Golem atau Gluco Slime; dan battle yang memakai ulang ikon dari `Assets/Unity_HUD/Sprites`.

Jalankan scene dalam landscape. Pada komputer gunakan WASD/panah atau klik/tap area daratan sebagai tujuan. Pada layar sentuh tersedia joystick bulat. Kamera peta mengikuti nanobot. Mendekati musuh memulai battle; setelah menang, pemain kembali ke tempat eksplorasi dan dapat mencari musuh lain.

Credits mencantumkan Hilmi Aminuddien, Mohammad Ferry Irwansyah, Christian Gideon, dan Syachrul Ramadhan. Settings mengatur volume musik dan efek suara, disimpan secara lokal. Musik berubah antara menu, peta, dan battle. Efek suara dipasang untuk tombol, serangan, charge, pertahanan, menang, dan kalah.

HUD kiri atas mengelompokkan MAP 02, bar/angka HP dan MP, serta objektif. SETTINGS/MENU memakai tombol kecil di kanan atas; petunjuk bawah memudar setelah pemain bergerak dan kembali pada sesi baru. Nameplate musuh memakai kapsul gelap. Panel navy transparan dan tombol berbevel memakai geometry uGUI melalui `OutlinedUiGraphic`; `UiPresentation` menerapkan susunan yang sama pada jalur fresh dan baked. Safe area tetap diterapkan pada kedua Canvas.

Empat aksi battle mempertahankan label/subteks putih di permukaan gelap: cyan untuk Diagnose, oranye untuk Synthesize, hijau untuk Restore, dan amber untuk Homeostasis. SETTINGS dibatasi 460 unit Canvas; slider tetap memakai penyimpanan volume yang sama. Tab/Shift+Tab berpindah kontrol, Enter/Space mengaktifkan tombol, dan Escape menutup settings atau drawer yang dapat dibatalkan. Fokus slider memakai navigasi vertikal di dalam modal; panah horizontal tetap mengubah volume.

Menu Editor `Tools > Vein Vanguard > UI Refactor > Run Acceptance and Capture` (Ctrl+Shift+Y, dari Edit Mode) menyimpan scene yang sudah ditata, menjalankan pemeriksaan model/Play Mode, dan mengambil screenshot Game View pada empat ukuran landscape. Pengujian memulai ulang sesi; jangan jalankan untuk mempertahankan progres permainan yang sedang diuji. Hasil tersimpan di `Temp/UiRefactorResults.txt`; screenshot di `docs/superpowers/verification/ui-ux-refactor/`. Laporan penerimaan berada di `docs/superpowers/verification/2026-11-05-ui-ux-refactor.md`. APK dan perangkat Android fisik perlu diuji terpisah.

Menu Editor `Tools > Vein Vanguard > Build Playable Prototype` memasang/menyegarkan referensi aset pada scene prototipe dan mendaftarkannya untuk build. Ini masih berupa peta ilustrasi satu area, bukan dunia yang dibangun dari tile; collision air/pohon masih batas eksplorasi kasar dan membutuhkan art-pass/kalibrasi. BGM menu sumbernya besar, maka Unity diatur menggunakan audio streaming. Musik dan efek menggunakan file yang sudah disediakan di `Assets/Sound`.
