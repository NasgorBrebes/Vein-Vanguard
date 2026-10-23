# Vein Vanguard — prototipe playable

## Menjalankan

Buka `Assets/Scenes/VeinVanguard.unity` di Unity 6000.3.9f1, lalu tekan Play.
UI dibuat saat Play oleh komponen BattleManager pada objek Vein Vanguard.
Gunakan Game View landscape, misalnya 1600 × 900 atau 1920 × 1080.

1. Mulai Misi, lalu Diagnose: panel scan/hasil menampilkan kedua rekaman sumber A/B tanpa langkah klasifikasi tambahan. Hasil tersimpan selama encounter; membuka Diagnose lagi tidak mengulang scan.
2. Tutup diagnosis, lalu tekan Synthesize untuk membuka dock bawah terpisah, paling tinggi 36% layar, dengan MC dan musuh tetap terlihat di atasnya. Seret ikon ke soket A/B senjata plasma, atau ketuk ikon lalu ketuk soket. Kedua musuh memakai enam formula pixel art retro bersama; ikon yang sama boleh dipasang di kedua soket.
3. Dua soket terisi memulai charge otomatis 0,65 detik, terlepas dari kecocokan jawaban. Setelah terisi penuh, tembakkan plasma: biaya 20 MP dan hasil 0/20/40 damage baru diterapkan saat proyektil tiba. Formula yang salah dijelaskan sesudah tembakan.
4. Restore membuka kuis jika MP belum penuh. Jawaban benar memberi 40 MP; salah memberi 10 MP, dibatasi maksimum 60 MP. Keduanya memakai satu giliran. Homeostasis diaktifkan melalui tombol terpisah.
5. Kalahkan kedua musuh untuk mencapai ringkasan akhir. Menu dan Main Lagi memulai sesi baru.

## Cakupan

- Menu awal, dua encounter, animasi frame MC/musuh, HP/MP, diagnosis wajib.
- Panel diagnosis untuk scan/hasil tersimpan, lalu dock Synthesize terpisah dengan enam ikon bersama untuk kedua musuh; pembahasan A/B setelah serangan. Lihat `Assets/Game/README-Diagnose-Synthesize.md` untuk pilihan formula, aturan kecocokan, dan pemeriksaan interaksi/impact.
- Bank soal literasi sumber pangan dan label makanan dengan pembahasan serta pilihan yang diacak, tanpa QTE/AUTO.
- Homeostasis aktif dengan dua parry dan pantulan serangan pertama; menang/kalah dan pengulangan permainan.
- Canvas responsif dan safe area; orientasi Android landscape.

Komposisi musuh dan efek nutrisi adalah model permainan, bukan representasi terapi klinis.
Angka balancing: 100 HP pemain, 60 MP, hasil racikan 0/20/40 damage; musuh 100/130 HP dan 14/18 damage.
Setelah encounter pertama, bonus 25 HP dan 20 MP, dibatasi nilai maksimum.
Soal awal berfokus pada perhitungan label; materi klinis tambahan perlu validasi tim sesuai GDD.

## Struktur

- `Assets/Game/NutrientSystem.cs`: model giliran, damage, energi, dan bank soal.
- `Assets/Game/BattleManager.cs` dan `DiagnosticWorkbench.cs`: UI, interaksi, animasi frame, alur pertempuran.
- `Assets/Game/CombatSamples.cs`: rekaman dan flag sifat sumber; `UniversalFormula.cs`: enam formula bersama, skor, dan pembahasan. Kecocokan mengikuti sifat sumber, bukan resep khusus per musuh.
- `Assets/Game/Editor/PrototypeBuilder.cs`: perakitan scene melalui Editor dan pemeriksaan aturan.
- `Assets/Game/Fonts`: font Inter dan pemberitahuan lisensinya.

Menu `Tools > Vein Vanguard > Build Playable Prototype` membuka scene yang sudah ada;
menu ini tidak menimpa scene jadi. Metode `VerifyRules()` menjalankan pemeriksaan aturan tanpa mengubah sesi game.

APK Android dan perangkat fisik belum diuji untuk remake ini. Belum ada penyimpanan progres antarsesi.
