# Menu, eksplorasi, dan presentasi

Scene `Assets/Scenes/VeinVanguard.unity` menyediakan menu Start, Settings, dan Credits; area eksplorasi yang memakai `Assets/map_1.png`; encounter saat pemain mendekati Lipid Golem atau Gluco Slime; dan battle dengan komponen visual dari `Assets/Unity_HUD/Sprites`.

Jalankan scene dalam landscape. Pada komputer gunakan WASD/panah atau klik/tap area daratan sebagai tujuan. Pada layar sentuh tersedia tombol arah. Kamera peta mengikuti nanobot. Mendekati musuh memulai battle; setelah menang, pemain kembali ke tempat eksplorasi dan dapat mencari musuh lain.

Credits mencantumkan Hilmi Aminuddien, Mohammad Ferry Irwansyah, Christian Gideon, dan Syachrul Ramadhan. Settings mengatur volume musik dan efek suara, disimpan secara lokal. Musik berubah antara menu, peta, dan battle. Efek suara dipasang untuk tombol, serangan, charge, pertahanan, menang, dan kalah.

HUD memakai panel, track/fill, empat state tombol, ikon Diagnose/Synthesize/Restore, serta Homeostasis. Safe area Android diterapkan pada antarmuka.

Menu Editor `Tools > Vein Vanguard > Build Playable Prototype` memasang/menyegarkan referensi aset pada scene prototipe dan mendaftarkannya untuk build. Ini masih berupa peta ilustrasi satu area, bukan dunia yang dibangun dari tile; collision air/pohon masih batas eksplorasi kasar dan membutuhkan art-pass/kalibrasi. BGM menu sumbernya besar, maka Unity diatur menggunakan audio streaming. Musik dan efek menggunakan file yang sudah disediakan di `Assets/Sound`.
