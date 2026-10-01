using System;
using UnityEngine;

namespace VeinVanguard
{
    public enum Nutrient { Omega3, Fiber, Water }
    public enum BattlePhase { Menu, Diagnose, Player, Trivia, Feedback, Enemy, Victory, Defeat, Complete }

    [Serializable]
    public class BattleModel
    {
        public int hp = 100, energy = 60, enemyHp, encounter, shield, correct, answered, turn;
        public int shieldHits;
        public bool diagnosed;
        public BattlePhase phase = BattlePhase.Menu;
        public int MaxEnemyHp => encounter == 0 ? 100 : 130;
        public Nutrient Weakness => encounter == 0 ? Nutrient.Omega3 : Nutrient.Fiber;
        public bool CanAttack => phase == BattlePhase.Player && diagnosed && energy >= 20;
        public void Begin(int index)
        {
            encounter = index; enemyHp = MaxEnemyHp; diagnosed = false; turn = 1; shield = shieldHits = 0;
            phase = BattlePhase.Diagnose;
        }
        public bool Diagnose()
        {
            if (phase != BattlePhase.Diagnose && phase != BattlePhase.Player) return false;
            diagnosed = true; phase = BattlePhase.Player; return true;
        }
        public static float Multiplier(Nutrient chosen, Nutrient weakness)
            => chosen == weakness ? 2f : chosen == Nutrient.Water ? 1f : .1f;
        public int Attack(Nutrient nutrient)
        {
            if (!CanAttack) return -1;
            energy -= 20;
            int damage = Mathf.RoundToInt(20 * Multiplier(nutrient, Weakness));
            enemyHp = Mathf.Max(0, enemyHp - damage);
            phase = enemyHp == 0 ? BattlePhase.Victory : BattlePhase.Enemy;
            return damage;
        }
        public bool Restore()
        {
            if (phase != BattlePhase.Player || energy >= 60) return false;
            phase = BattlePhase.Trivia; return true;
        }
        public bool Answer(bool isCorrect)
        {
            if (phase != BattlePhase.Trivia) return false;
            answered++;
            if (isCorrect) { correct++; energy = Mathf.Min(60, energy + 40); }
            else energy = Mathf.Min(60, energy + 10);
            phase = BattlePhase.Feedback; return true;
        }
        public bool Homeostasis(int cost = 10, int reduction = 50, int hits = 2)
        {
            if (phase != BattlePhase.Player || !diagnosed || shieldHits > 0 || cost < 0 || energy < cost || reduction < 1 || reduction > 100 || hits < 1) return false;
            energy -= cost; shield = reduction; shieldHits = hits; phase = BattlePhase.Enemy;
            return true;
        }
        public int EnemyAttack()
        {
            if (phase != BattlePhase.Enemy) return -1;
            int damage = Mathf.RoundToInt((encounter == 0 ? 14 : 18) * (1 - shield / 100f));
            hp = Mathf.Max(0, hp - damage); turn++;
            if (shieldHits > 0 && --shieldHits == 0) shield = 0;
            phase = hp == 0 ? BattlePhase.Defeat : BattlePhase.Player;
            return damage;
        }
    }

    public class TriviaQuestion
    {
        public string prompt, explanation;
        public string[] options;
        public int answer;
        public TriviaQuestion(string p, string[] o, int a, string e) { prompt = p; options = o; answer = a; explanation = e; }
    }
    public static class TriviaBank
    {
        public static readonly TriviaQuestion[] Questions = {
            new TriviaQuestion("Label minuman mencantumkan 12 g gula per sajian. Ada 2 sajian dalam satu botol. Berapa gula jika seluruh botol diminum?", new[]{"12 gram", "24 gram", "6 gram"}, 1, "12 g × 2 sajian = 24 g. Periksa jumlah sajian per kemasan, bukan hanya angka per sajian."),
            new TriviaQuestion("Dua produk memiliki ukuran sajian yang sama. Produk A mengandung 8 g gula, produk B 15 g. Mana yang lebih rendah gula?", new[]{"Produk A", "Produk B", "Keduanya sama"}, 0, "Pada ukuran sajian yang sama, 8 g lebih rendah daripada 15 g. Bandingkan juga informasi gizi lainnya."),
            new TriviaQuestion("Sereal mengandung 4 g serat per sajian. Dua sajian mengandung berapa gram serat?", new[]{"2 gram", "4 gram", "8 gram"}, 2, "4 g × 2 sajian = 8 g serat. Ukuran porsi memengaruhi jumlah zat gizi yang dikonsumsi."),
            new TriviaQuestion("Label menyebutkan 150 mg natrium per sajian. Jika makan 3 sajian, berapa natrium yang tercatat?", new[]{"450 mg", "150 mg", "50 mg"}, 0, "150 mg × 3 sajian = 450 mg natrium. Selalu perhatikan satuan dan jumlah sajian."),
            new TriviaQuestion("Produk A memiliki 100 kkal per 25 g, produk B 160 kkal per 40 g. Pada porsi 100 g, bagaimana energinya?", new[]{"A lebih tinggi", "B lebih tinggi", "Sama: 400 kkal"}, 2, "A: 100 × 4 = 400 kkal. B: 160 × 2,5 = 400 kkal. Samakan ukuran porsi sebelum membandingkan."),
            new TriviaQuestion("Satu kemasan berisi 90 g. Ukuran saji pada label adalah 30 g. Ada berapa sajian di dalamnya?", new[]{"1 sajian", "3 sajian", "30 sajian"}, 1, "90 ÷ 30 = 3 sajian. Menghabiskan kemasan berarti mengonsumsi tiga kali angka gizi per sajian.")
        };
    }
}
