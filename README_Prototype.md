# Vein Vanguard — prototipe playable

## Menjalankan

Buka `Assets/Scenes/VeinVanguard.unity` di Unity 6000.3.9f1, lalu tekan Play.
UI dibuat saat Play oleh komponen BattleManager pada objek Vein Vanguard.
Gunakan Game View landscape, misalnya 1600 × 900 atau 1920 × 1080.

1. Mulai Misi, lalu Diagnose untuk membuka komposisi musuh.
2. Tutup hasil diagnosis dengan Siapkan Nutrisi.
3. Synthesize memilih Omega-3, Serat, atau Air. Biaya tiap serangan: 20 MP.
4. Restore membuka kuis jika MP belum penuh. Jawaban benar memberi 40 MP dan 10% Homeostasis; salah memberi 10 MP. Keduanya memakai satu giliran.
5. Kalahkan kedua musuh untuk mencapai ringkasan akhir. Menu dan Main Lagi memulai sesi baru.

## Cakupan

- Menu awal, dua encounter, animasi frame MC/musuh, HP/MP, diagnosis wajib.
- Multiplier sesuai GDD: 2× cocok, 1× netral, 0,1× salah.
- Enam soal literasi label makanan dengan pembahasan, tanpa QTE/AUTO.
- Perlindungan pasif kumulatif sampai 50%, menang/kalah, pengulangan permainan.
- Canvas responsif dan safe area; orientasi Android landscape.

Komposisi musuh dan efek nutrisi adalah model permainan, bukan representasi terapi klinis.
Angka balancing awal: 100 HP pemain, 60 MP, 20 damage dasar; musuh 100/130 HP dan 14/18 damage.
Setelah encounter pertama, bonus 25 HP dan 20 MP, dibatasi nilai maksimum.
Soal awal berfokus pada perhitungan label; materi klinis tambahan perlu validasi tim sesuai GDD.

## Struktur

- `Assets/Game/NutrientSystem.cs`: model giliran, damage, energi, dan bank soal.
- `Assets/Game/BattleManager.cs`: UI, interaksi, animasi frame, alur pertempuran.
- `Assets/Game/Editor/PrototypeBuilder.cs`: perakitan scene melalui Editor dan pemeriksaan aturan.
- `Assets/Game/Fonts`: font Inter dan pemberitahuan lisensinya.

Menu `Tools > Vein Vanguard > Build Playable Prototype` membuka scene yang sudah ada;
menu ini tidak menimpa scene jadi. Metode `VerifyRules()` menjalankan pemeriksaan aturan tanpa mengubah sesi game.

Ini prototipe pertama. APK Android, pengujian perangkat fisik, audio final, VFX terpisah,
peta interaktif, dan penyempurnaan visual belum termasuk. Belum ada penyimpanan progres antarsesi.
