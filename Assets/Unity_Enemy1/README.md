# Enemy 1 untuk Unity

1. Salin seluruh folder Unity_Enemy1 ke Assets proyek Unity.
2. Tunggu kompilasi. Jalankan Tools > Vein Vanguard > Prepare Enemy 1 Animations.
3. Importer mengatur 16 PNG di frames sebagai Sprite Single dan membuat 4 klip di Animations: idle (6 FPS, loop), attack (10 FPS), hurt (10 FPS), defeat (7 FPS).
4. Tambahkan klip ke Animator Controller pada objek yang memiliki SpriteRenderer. Hubungkan transisi dengan sistem battle.

Ukuran frame 384x384, alpha transparan, pivot custom (0.5, 0.08333333), PPU 100, Bilinear, mipmap mati, tanpa kompresi. Sheet alternatif menggunakan grid 4x4, sel 384x384, pivot yang sama.

Menu menerapkan ulang pengaturan impor PNG tetapi mempertahankan klip .anim yang sudah ada. Animator Controller dan koneksi BattleManager tidak dibuat otomatis.

Paket memakai sprite Enemy 1 yang sebelumnya telah dibuat. File asli di folder induk tetap tersedia. Kelengkapan PNG dan ZIP diperiksa lokal; script belum dikompilasi dan playback belum diuji di Unity.
