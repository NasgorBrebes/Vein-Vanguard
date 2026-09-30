## VEIN VANGUARD

## Game Design Document (GDD)

Disusun oleh: Christian Gideon Valent, Hilmi Aminuddien, Syachrul Ramadhan, Mohammad Ferry Irwansyah

## 1. Overview

## The Elevator Pitch

Game edukasi RPG turn-based 2D di mana pemain mengendalikan nanobot medis untuk mengalahkan monster patogen menggunakan taktik literasi nutrisi dan strategi gizi, menggantikan mekanik refleks motorik.

## Project Description

Vein Vanguard adalah serious game 2D side-view yang mengedukasi pemain terkait dampak makanan tidak sehat. Pemain tidak lagi mengandalkan ketangkasan mekanik Quick Time Event (QTE), melainkan pemahaman komposisi nutrisi untuk menetralisir amalgamasi junk food di dalam sirkulasi darah manusia. Kemenangan murni bergantung pada akurasi diagnosis dan pemilihan penawar medis.

## Theme / Setting / Genre

- Theme: Edukasi Kesehatan, Nutrisi, dan Strategi Klinis.

- Genre: 2D Turn-Based Educational RPG / Serious Game.

- Setting: Lingkungan mikroskopis organik pembuluh darah.

## Core Gameplay Mechanics Brief

- Mechanic 1: Tactical Nutrient Combat (Mencocokkan nutrisi penawar dengan kelemahan komposisi musuh).


- Mechanic 2: Diagnose & Counter (Memindai kandungan bahaya musuh untuk menentukan jenis serangan).

- Mechanic 3: Homeostasis Defense (Mitigasi perlindungan yang dibangun dari akumulasi jawaban benar/status kesehatan pemain).

- Mechanic 4: Trivia Recharge (Pemulihan energi tempur melalui kuis studi kasus gizi).

## Targeted Platforms & Monetization

- Platform: Android (Memanfaatkan layar sentuh untuk navigasi panel nutrisi dan opsi trivia).

- Monetization: Free / Educational Project (Tidak ada monetisasi).

## 2. Project Scope

Time Scale: 10 Minggu (estimasi 80-100 jam kerja efektif).

- Cost: Rp 0 (Menggunakan engine Unity gratis dan aset open-source).

- Team Size: 4 Orang (Christian Gideon Valent: Programmer, Hilmi Aminuddien: Game Designer, Syachrul Ramadhan: 2D Artist, Mohammad Ferry Irwansyah: Sound Designer / Project Lead).

- Unique Selling Point (USP): Gamifikasi edukasi di mana kemenangan 100% bertumpu pada pengetahuan gizi pemain, menghilangkan elemen arcade murni untuk memastikan validitasnya sebagai media edukasi kesehatan.

## 3. Story and Gameplay

## Story (Brief)

Sebuah nanobot medis dimasukkan ke dalam aliran darah pasien untuk menstimulasi penyembuhan dengan cara menghancurkan tumpukan kalori jahat yang bermutasi menjadi golem patogen.

## Gameplay (Detailed)


Pemain dan Junk Food Golem saling berhadapan dalam pertempuran side-view. Antarmuka menampilkan Integritas Pembuluh Darah (HP) dan Energi Metabolisme (MP).

- Fase Pemain (Player Turn): Pemain wajib menggunakan perintah Diagnose di awal pertemuan untuk melihat persentase komposisi musuh (misal: 70% Lemak Jenuh). Pemain kemudian masuk ke menu Synthesize untuk memilih nutrisi penawar. Serangan yang relevan (misal: Omega-3) memberikan Critical Damage; serangan yang salah akan diabaikan. Jika Energi Metabolisme (MP) habis, pemain menekan Restore yang akan memicu Trivia Recharge untuk memulihkan energi berdasarkan akurasi jawaban kuis gizi.

- Fase Musuh (Enemy Turn): Musuh menyerang menggunakan efek penyakit klinis. Damage direduksi secara pasif berdasarkan Buff kesehatan yang dikumpulkan pemain dari keberhasilan fase kuis sebelumnya.

## Core Gameplay Mechanics (Detailed)

- Weakness Multiplier System: Menggantikan kalkulasi QTE. Rumus sistem ini adalah: TotalDamage = BaseAttack * NutrientMultiplier. Nilai Multiplier adalah 2.0x untuk nutrisi akurat, 1.0x untuk netral, dan 0.1x jika salah nutrisi.

- Educational Input Detection: Menggunakan sistem touch input layar untuk menavigasi opsi berganda pada kuis edukasi atau melakukan drag-and-drop pada panel elemen nutrisi.

## 4. Assets & Graphic Style

Graphic Style: 2D Flat Vector bergaya Cartoonish dengan kontras tinggi untuk membedakan entitas player yang klinis/putih dan musuh bersaturasi kusam.

## Assets Needed

- Aset Visual Dipertahankan: Background pembuluh darah, Sprite Sheet Nanobot, Sprite Sheet Junk Food Golem, VFX kilatan cahaya, Audio hantaman/UI.

- Pembaruan 2D HUD: Penghapusan QTE Ring Indicator dan Tombol AUTO. Penambahan antarmuka Diagnose Scanner, Panel Menu Synthesize (Ikon Nutrisi), dan Pop-up Window untuk kuis Trivia.


- Code Scripts: Modifikasi signifikan pada BattleManager.cs, penghapusan QTEManager.cs, penambahan TriviaManager.cs dan NutrientSystem.cs.

## 5. Schedule and Milestones

- Objective 1 (Minggu 1-2): Implementasi Finite State Machine (FSM) untuk manajemen giliran, setup stat dasar karakter, dan kamera 2D.

- Objective 2 (Minggu 3-4): Pengembangan TriviaManager.cs untuk sistem kuis gizi interaktif beserta antarmuka tanya-jawab, menggantikan fokus pada indikator ritme QTE.

- Objective 3 (Minggu 5-6): Integrasi Weakness Multiplier System berdasarkan kalkulator kecocokan nutrisi, efek visual pantulan musuh, dan logika mitigasi pertahanan pasif (Homeostasis).

- Objective 4 (Minggu 7-8): Finalisasi UI baru (Panel Synthesize, menu Trivia). Integrasi seluruh aset visual akhir dan penggabungan audio.

- Objective 5 (Minggu 9-10): Balancing tingkat kesulitan kuis, validasi materi literasi gizi, debugging sistem baru, dan build .apk final.
