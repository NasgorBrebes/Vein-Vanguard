# MC untuk Unity

48 PNG RGBA, masing-masing 384x384, 12 animasi dengan 4 frame. Sheet alternatif tersedia di Sheets (1536x1536, grid 4x4). File asli tetap disimpan di folder induk.

## Impor

1. Salin folder Unity_MC ini ke folder Assets proyek Unity.
2. Tunggu kompilasi lalu pilih Tools > Vein Vanguard > Prepare MC Animations.
3. Importer mengatur Sprite Single, PPU 100, alpha, Bilinear, tanpa mipmap/kompresi, serta pivot custom (0.5, 0.08333333). Importer membuat 12 klip di Animations dengan 8 FPS awal. Idle, trivia_wait, low_energy dibuat loop; lainnya sekali jalan.
4. Masukkan klip ke Animator Controller milik objek dengan SpriteRenderer. Sambungkan transisi dengan BattleManager sesuai giliran. Kecepatan dan timing impact perlu disesuaikan gameplay.

Alternatif manual: impor PNG di Frames sebagai Sprite (2D and UI), Single, pivot Custom X=0.5 Y=0.08333333, PPU 100. Empat file berurutan *_00 hingga *_03 membentuk satu animasi. Sheet dapat di-slice Grid by Cell Size 384x384 memakai pivot yang sama.

## Verifikasi dan batasan

- 48 gambar tidak kosong, berukuran 384x384, memiliki alpha dan ruang transparan minimal 16px di sisi kanvas.
- Titik pijak kemasan berada pada (192,352) dari kiri atas. Titik horizontal ditaksir dari bagian kaki, garis bawah dari batas alpha. Perubahan proporsi/pose bawaan gambar generatif dapat tetap menimbulkan gerakan yang perlu art polish.
- Dua sheet pertama dibersihkan dengan generator gambar bawaan agar efek scanner, shield, dan energi yang sebelumnya melintasi grid tidak mencemari frame tetangga. PNG baru adalah animasi badan saja. VFX terpisah belum termasuk; versi lama dengan efek tetap berada di folder induk.
- Prompt edit: pertahankan 16 pose dan desain robot dalam grid 4x4, hilangkan shield/beam/orb/partikel/glow, rekonstruksi badan, beri margin transparan, pertahankan urutan pose.
- Script editor belum dikompilasi atau dijalankan di Unity pada lingkungan ini. PNG sudah diverifikasi lokal. Script tidak mengganti klip .anim yang sudah ada, tetapi menerapkan ulang pengaturan impor PNG paket saat menu dijalankan.
