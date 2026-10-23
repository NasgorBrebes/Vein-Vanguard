# Diagnose / Synthesize — Fast Icon Fusion

Implementasi 17 Oktober 2026 untuk Lipid Golem dan Gluco Slime. Buka scene `Assets/Scenes/VeinVanguard.unity`, Play, mulai misi, lalu dekati salah satu musuh.

## Alur pemain

1. **Diagnose** membuka panel diagnosis yang hanya menjalankan satu animasi scan dan menampilkan kedua rekaman A/B. Tidak ada langkah klasifikasi tambahan; scan tidak memakai MP atau giliran. Bukti tersimpan sampai encounter berganti; membuka kembali Diagnose menampilkan hasil tersimpan tanpa scan ulang.
2. Baca kedua sumber, tutup hasil diagnosis, lalu tekan tombol **Synthesize** untuk membuka dock plasma tersendiri di bawah layar. Diagnosis tidak otomatis membuka Synthesize. Dock memakai paling banyak **36% tinggi layar**, sehingga MC dan musuh tetap terlihat di atasnya.
3. Seret ikon formula ke soket A/B pada senjata, atau ketuk ikon lalu ketuk soket. Soket A selalu menangani sumber A dan soket B sumber B. Ikon yang sama boleh dipasang di kedua soket. Ketuk soket terisi tanpa pilihan ikon untuk melepasnya.
4. Dua soket terisi memulai charge otomatis selama **0,65 detik**, termasuk pasangan yang nantinya menghasilkan 0 damage. Charge menyatakan rakitan lengkap, bukan jawaban benar. Mengganti/melepas ikon memulai ulang/membatalkan charge; menutup panel membatalkannya, dan membuka kembali rakitan lengkap memulai charge baru. Belum ada MP atau HP yang berubah.
5. Setelah charge selesai dan MP minimal 20, **Tembakkan Plasma** menutup dock saat animasi MC dan proyektil berjalan. HP musuh dan biaya **20 MP** berubah tepat saat proyektil tiba, satu kali saja. Input tembakan ganda dikunci selama aksi.
6. Hasil non-lethal muncul di panel bawah dengan pembahasan A/B. Tekan Lanjut untuk giliran musuh. Hasil lethal langsung menuju notifikasi kemenangan bawah; musuh yang kalah tidak membalas.

## Formula bersama dan bukti sumber

Kedua musuh memakai enam pilihan yang sama: **Batasi tambahan**, **Air putih**, **Buah utuh**, **Lemak tak jenuh**, **Kukus/rebus**, dan **Variasi olesan**. Variasi olesan berarti mengganti sebagian mentega dengan alpukat. Tidak ada ikon atau pasangan jawaban khusus yang dibuka untuk satu musuh.

| Musuh | Soket | Rekaman | Formula bernilai penuh untuk sumber ini |
| --- | --- | --- | --- |
| Lipid Golem | A | Mentega sebagai olesan roti; lemak jenuh dan bahan tambahan | Batasi tambahan / Lemak tak jenuh / Variasi olesan |
| Lipid Golem | B | Minyak kelapa untuk menggoreng; lemak jenuh | Lemak tak jenuh |
| Gluco Slime | A | Sirup ditambahkan sebagai pemanis minuman; gula bebas | Batasi tambahan / Air putih |
| Gluco Slime | B | Jus buah tanpa gula tambahan; minuman sumber gula bebas | Air putih / Buah utuh |

Aturan membaca flag `SampleTraits` pada setiap `CombatSample`: `AddedIngredient`, `Beverage`, `Juice`, `SaturatedFat`, `Fried`, dan `ButterSpread`. Kecocokan mengikuti sifat sumber tersebut, bukan satu resep rahasia per musuh. **Kukus/rebus** bernilai sebagian untuk sumber yang digoreng: dapat mengurangi penggunaan minyak, tetapi tidak otomatis mengubah jenis lemaknya.

Setiap soket dinilai 0 (belum menyasar bukti), 1 (sebagian), atau 2 (penuh). Kedua soket bernilai 2 menghasilkan **40 damage**; jika belum keduanya penuh tetapi setidaknya satu bernilai positif, hasilnya **20 damage**; keduanya 0 menghasilkan **0 damage**. Semua tembakan dengan dua ikon valid memakai 20 MP dan satu giliran, termasuk hasil 0 damage. Soket kosong, charge belum selesai, atau MP kurang dari 20 menonaktifkan tembakan. Kesalahan formula dijelaskan setelah tembakan, bukan dibocorkan oleh charge atau penolakan ikon sebelumnya.

Mengurangi olesan mentega mengubah jumlah yang dikonsumsi, bukan jenis lemaknya. Mengganti sebagian dengan alpukat atau memilih sumber lemak tak jenuh menyasar penggantian sumber. Pembahasan menjelaskan keputusan pangan; tidak ada klaim bahwa air, buah, nutrisi, atau alat fiktif menetralkan gula/lemak yang sudah berada di tubuh. Perangkat plasma, MP, dan damage adalah aturan fiksi untuk pembelajaran, bukan terapi atau dosis klinis.

Rujukan materi: [WHO - Healthy diet](https://www.who.int/news-room/fact-sheets/detail/healthy-diet) untuk gula bebas dan jus tanpa gula tambahan; [WHO - Fats and carbohydrates](https://www.who.int/news/item/17-07-2023-who-updates-guidelines-on-fats-and-carbohydrates) untuk mentega/minyak kelapa, pilihan lemak, dan kualitas karbohidrat. Rekaman menggambarkan sumber pangan, bukan diagnosis klinis berdasarkan penampilan monster.

## Aset dan integrasi

- `Assets/Game/UI/Diagnostics/`: 27 PNG (20 aset diagnosis sebelumnya, satu senjata plasma, dan enam ikon formula universal), semua tetap file terpisah tanpa trimming atlas. Enam ikon formula dibuat ulang sebagai pixel art retro dengan nama file `formula_*` yang sama.
- Sprite Single, full canvas, pivot Center, PPU 100, alpha aktif, NPOT None, uncompressed, mipmaps nonaktif, dan Clamp. Keenam `formula_*` memakai tekstur runtime **128 px / Point** melalui batas ukuran TextureImporter; master PNG full canvas tetap disimpan. Aset diagnosis dan senjata tetap Bilinear dengan batas 2048 px. Canvas Scaler, kamera, dan kualitas global tetap memakai pengaturan lama.
- Referensi sprite disimpan pada `BattleManager.learningSprites` di scene. `DiagnosticAssetSetup.Configure(game)` mengonfigurasi import melalui Editor dengan pemeriksaan kemampuan pivot; tidak ada penulisan metadata manual.
- `DiagnosticWorkbench.cs`: panel scan/hasil diagnosis dan dock Synthesize yang terpisah, tap/drag ke soket, dan charge. `FormulaIconDrag.cs` / `FormulaSocketDrop.cs`: event drag/drop. `CombatSamples.cs`: rekaman dan sifat sumber. `UniversalFormula.cs`: enam ikon bersama, skor kecocokan, damage, serta pembahasan A/B. `NutrientSystem.cs`: diagnosis, validasi energi, dan giliran; jalur diagnosis/serangan lama tetap tersedia untuk pemeriksaan regresi model.
- MC, musuh, HUD, VFX, suara, map, dan mekanik Homeostasis/parry lama dipakai ulang. Restore tetap sama; soal yang masih menyebut kelemahan/pengali serangan lama disesuaikan agar tidak bertentangan.

## Pemeriksaan Editor

Di Edit Mode: `PrototypeBuilder.VerifyRules()` menjalankan `DiagnosticRuleChecks.VerifyAll()` untuk kompatibilitas model lama dan `UniversalFormulaRuleChecks.VerifyAll()` untuk aturan baru. Pemeriksaan baru mencakup kedua musuh, alternatif/ikon berulang, semua skor sumber, hasil 0/20/40, input tidak valid, batas 20 MP, fase, penguncian serangan ganda, lethal, dan reset. `DiagnosticWorkbenchChecks.VerifyAssets()` memeriksa 27 sprite beserta pengaturan import.

Di Play Mode: `PlasmaWorkbenchChecks.VerifyEntry()` memeriksa panel diagnosis dan gerbang scan; setelah minimal satu frame, `PlasmaWorkbenchChecks.VerifyInteractions()` memeriksa scan tunggal dan hasil tersimpan, pemisahan diagnosis/Synthesize, dock bawah, enam ikon, tap/drag/drop, ikon berulang, charge, penutupan/pembukaan dock, serta menu/reset. Nama lama `DiagnosticWorkbenchChecks.VerifyEntry()` / `VerifyInteractions()` meneruskan ke pemeriksaan plasma ini. `PrototypeBuilder.VerifyAttackImpact()` memeriksa biaya/damage hanya saat impact, termasuk penguncian tembakan ganda dan serangan lethal. Pemeriksaan yang tetap relevan: `PrototypeBuilder.VerifyHomeostasisIcon()`, `PrototypeBuilder.VerifyClickAudio()`, dan `PrototypeBuilder.VerifyExploration()`. Pemeriksaan interaksi/impact memulai ulang sesi uji; jangan jalankan saat ingin mempertahankan sesi permainan.

Dengan dock Synthesize sudah terbuka dan minimal satu frame sudah dirender, `PlasmaWorkbenchChecks.VerifyVisibleHitTargets()` (atau alias `DiagnosticWorkbenchChecks.VerifyVisibleHitTargets()`) memeriksa routing pointer melalui EventSystem/GraphicRaycaster. Jangan memeriksa hit test pada frame yang sama dengan pembuatan/aktivasi panel: Canvas belum memberi graphic depth untuk raycast. Uji manual juga mencakup hasil diagnosis yang dapat dibuka kembali, dock paling tinggi 36% layar, MC/musuh yang terlihat, drag/tap, pergantian ikon selama charge, semua pasangan benar/sebagian/salah, dan kembalinya kontrol setelah dock ditutup.

`PlasmaWorkbenchChecks.VerifyFeedbackLayout()` memeriksa pembahasan kedua musuh muat pada panel hasil synthesis. Panel ini memakai ruang bawah yang lebih tinggi untuk teks A/B; notifikasi kemenangan tetap memakai panel bawah lama.

APK dan perangkat Android fisik perlu diuji terpisah. Prompt paket ikon diagnosis awal dibuat dengan imagegen bawaan dan disimpan bersama paket sumber di `D:/Simulasi Game dan Komputer/Assets_Diagnose_Synthesize_20261016/PROMPTS.md` pada mesin pengembang.
