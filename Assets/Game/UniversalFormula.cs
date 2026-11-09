using System;

namespace VeinVanguard
{
    public enum FormulaIcon { None = -1, ReduceAdded, Water, WholeFruit, UnsaturatedFat, Steam, VariedSpread,
        Nicotine, CarbonMonoxide, TobaccoTar, Benzene, Formaldehyde, Cadmium,
        Ethanol, Acetaldehyde, Acetate, AlcoholDehydrogenase, AldehydeDehydrogenase, Liver }

    public static class UniversalFormula
    {
        public static bool IsValid(FormulaIcon icon) => icon >= FormulaIcon.ReduceAdded && icon <= FormulaIcon.Liver;
        public static bool Complete(FormulaIcon first, FormulaIcon second) => IsValid(first) && IsValid(second);

        static readonly FormulaIcon[][] palettes = {
            new[] { FormulaIcon.ReduceAdded, FormulaIcon.Water, FormulaIcon.WholeFruit, FormulaIcon.UnsaturatedFat, FormulaIcon.Steam, FormulaIcon.VariedSpread },
            new[] { FormulaIcon.ReduceAdded, FormulaIcon.Water, FormulaIcon.WholeFruit, FormulaIcon.UnsaturatedFat, FormulaIcon.Steam, FormulaIcon.VariedSpread },
            new[] { FormulaIcon.Nicotine, FormulaIcon.CarbonMonoxide, FormulaIcon.TobaccoTar, FormulaIcon.Benzene, FormulaIcon.Formaldehyde, FormulaIcon.Cadmium },
            new[] { FormulaIcon.ReduceAdded, FormulaIcon.Water, FormulaIcon.WholeFruit, FormulaIcon.UnsaturatedFat, FormulaIcon.Steam, FormulaIcon.VariedSpread },
            new[] { FormulaIcon.Ethanol, FormulaIcon.Acetaldehyde, FormulaIcon.Acetate, FormulaIcon.AlcoholDehydrogenase, FormulaIcon.AldehydeDehydrogenase, FormulaIcon.Liver },
            new[] { FormulaIcon.Nicotine, FormulaIcon.CarbonMonoxide, FormulaIcon.TobaccoTar, FormulaIcon.Ethanol, FormulaIcon.Acetaldehyde, FormulaIcon.AldehydeDehydrogenase }
        };
        static readonly string[] dataTitles = {
            "Nikotin", "Karbon monoksida", "Tar", "Benzena", "Formaldehida", "Kadmium",
            "Etanol", "Asetaldehida", "Asetat", "Enzim ADH", "Enzim ALDH", "Hati"
        };
        static readonly string[] dataBadges = { "NIK", "CO", "TAR", "BEN", "FOR", "Cd", "EtOH", "AcH", "Ac", "ADH", "ALDH", "HATI" };
        static readonly string[] dataFacts = {
            "Nikotin adalah zat dalam tembakau yang menyebabkan ketergantungan.",
            "Karbon monoksida dalam asap rokok mengurangi kemampuan darah membawa oksigen.",
            "Tar merupakan campuran partikel dalam asap tembakau, bukan satu molekul tunggal.",
            "Benzena adalah salah satu zat penyebab kanker yang ditemukan dalam asap tembakau.",
            "Formaldehida terdapat dalam asap tembakau dan dapat mengiritasi mata serta saluran napas.",
            "Kadmium adalah logam beracun yang dapat ditemukan dalam tembakau dan asapnya.",
            "Etanol adalah jenis alkohol dalam minuman beralkohol.",
            "Asetaldehida adalah hasil penguraian etanol yang bersifat toksik.",
            "Asetat dihasilkan saat asetaldehida diuraikan lebih lanjut.",
            "ADH membantu mengubah etanol menjadi asetaldehida.",
            "ALDH membantu mengubah asetaldehida menjadi asetat.",
            "Sebagian besar penguraian alkohol berlangsung di hati."
        };

        public static FormulaIcon[] Palette(int encounter)
        {
            if (encounter < 0 || encounter >= palettes.Length) throw new ArgumentOutOfRangeException(nameof(encounter));
            return (FormulaIcon[])palettes[encounter].Clone();
        }

        public static bool Contains(int encounter, FormulaIcon icon)
        {
            if (encounter < 0 || encounter >= palettes.Length) throw new ArgumentOutOfRangeException(nameof(encounter));
            return Array.IndexOf(palettes[encounter], icon) >= 0;
        }

        public static string Fact(FormulaIcon icon)
        {
            return icon >= FormulaIcon.Nicotine && IsValid(icon) ? dataFacts[(int)icon - 6] : null;
        }

        public static string Badge(FormulaIcon icon)
        {
            return icon >= FormulaIcon.Nicotine && IsValid(icon) ? dataBadges[(int)icon - 6] : "";
        }

        public static string Title(FormulaIcon icon)
        {
            if (icon >= FormulaIcon.Nicotine && IsValid(icon)) return dataTitles[(int)icon - 6];
            switch (icon)
            {
                case FormulaIcon.None: return "Pilih formula";
                case FormulaIcon.ReduceAdded: return "Batasi tambahan";
                case FormulaIcon.Water: return "Air putih";
                case FormulaIcon.WholeFruit: return "Buah utuh";
                case FormulaIcon.UnsaturatedFat: return "Lemak tak jenuh";
                case FormulaIcon.Steam: return "Kukus/rebus";
                case FormulaIcon.VariedSpread: return "Variasi olesan";
                default: throw new ArgumentOutOfRangeException(nameof(icon));
            }
        }

        public static string IconName(FormulaIcon icon)
        {
            if (icon >= FormulaIcon.Nicotine && IsValid(icon)) return null;
            switch (icon)
            {
                case FormulaIcon.None: return null;
                case FormulaIcon.ReduceAdded: return "formula_added";
                case FormulaIcon.Water: return "formula_water";
                case FormulaIcon.WholeFruit: return "formula_whole";
                case FormulaIcon.UnsaturatedFat: return "formula_unsaturated";
                case FormulaIcon.Steam: return "formula_steam";
                case FormulaIcon.VariedSpread: return "formula_avocado";
                default: throw new ArgumentOutOfRangeException(nameof(icon));
            }
        }

        public static int Score(int encounter, int target, FormulaIcon icon)
        {
            var sample = CombatSamples.Get(encounter, target);
            if (!Contains(encounter, icon)) return 0;
            if (sample.kind == SampleClass.Data)
            {
                FormulaIcon answer = encounter == 2 ? (target == 0 ? FormulaIcon.Nicotine : FormulaIcon.CarbonMonoxide) :
                    encounter == 4 ? (target == 0 ? FormulaIcon.Ethanol : FormulaIcon.Acetaldehyde) :
                    target == 0 ? FormulaIcon.CarbonMonoxide : FormulaIcon.Acetaldehyde;
                return icon == answer ? 2 : 0;
            }
            var traits = sample.traits;
            SampleTraits required;
            switch (icon)
            {
                case FormulaIcon.ReduceAdded: required = SampleTraits.AddedIngredient; break;
                case FormulaIcon.Water: required = SampleTraits.Beverage; break;
                case FormulaIcon.WholeFruit: required = SampleTraits.Juice; break;
                case FormulaIcon.UnsaturatedFat: required = SampleTraits.SaturatedFat; break;
                case FormulaIcon.Steam: required = SampleTraits.Fried; break;
                case FormulaIcon.VariedSpread: required = SampleTraits.ButterSpread; break;
                default: return 0;
            }
            return (traits & required) == 0 ? 0 : icon == FormulaIcon.Steam ? 1 : 2;
        }

        public static int Damage(int encounter, FormulaIcon first, FormulaIcon second)
        {
            int a = Score(encounter, 0, first), b = Score(encounter, 1, second);
            return a == 2 && b == 2 ? 40 : a > 0 || b > 0 ? 20 : 0;
        }

        public static string Feedback(int encounter, FormulaIcon first, FormulaIcon second)
        {
            return CombatSamples.Get(encounter, 0).title + " / " + Title(first) + ": " + Explanation(encounter, 0, first) + "\n\n" +
                CombatSamples.Get(encounter, 1).title + " / " + Title(second) + ": " + Explanation(encounter, 1, second);
        }

        static string Explanation(int encounter, int target, FormulaIcon icon)
        {
            bool fits = Score(encounter, target, icon) > 0;
            if (icon >= FormulaIcon.Nicotine && IsValid(icon) && CombatSamples.Get(encounter, target).kind != SampleClass.Data)
                return "Kartu data ini tidak menyasar rekaman pangan.";
            if (CombatSamples.Get(encounter, target).kind == SampleClass.Data)
                return (fits ? "Sesuai. " : "Tidak sesuai. ") + CombatSamples.Get(encounter, target).classificationFeedback;
            switch (icon)
            {
                case FormulaIcon.None: return "Soket belum terisi formula.";
                case FormulaIcon.ReduceAdded:
                    return fits ? "Mengurangi bahan yang ditambahkan langsung menyasar sumber pada rekaman. Jumlah bahan berkurang; jenis lemak atau gulanya tidak berubah." :
                        "Rekaman ini tidak menunjukkan tambahan bahan yang dimaksud. Mengurangi tambahan yang tidak ada belum menyasar sumbernya.";
                case FormulaIcon.Water:
                    return fits ? "Mengganti minuman sumber gula bebas dengan air mengurangi sumber tersebut; air tidak menetralkan gula di dalam tubuh." :
                        "Sumber ini bukan minuman. Penggantian minuman belum menjawab rekaman bahan lemak ini.";
                case FormulaIcon.WholeFruit:
                    return fits ? "Mengganti sajian jus dengan buah utuh sesuai dengan bukti bentuk pangan pada rekaman. Label tanpa gula tambahan tidak berarti bebas gula." :
                        "Sampel ini bukan sajian jus. Tindakan yang dipasang belum menyasar rekaman ini.";
                case FormulaIcon.UnsaturatedFat:
                    return fits ? "Penggantian menyasar kualitas lemak: gunakan pilihan kaya lemak tak jenuh, misalnya minyak kanola, kedelai, atau bunga matahari." :
                        "Rekaman ini berfokus pada sumber gula bebas, bukan sumber lemak jenuh. Pengganti harus menjawab bukti yang ditemukan.";
                case FormulaIcon.Steam:
                    return fits ? "Mengukus atau merebus dapat mengurangi minyak yang digunakan. Ini tidak otomatis mengubah lemak jenuh menjadi lemak tak jenuh, sehingga hasil formula masih sebagian." :
                        "Rekaman ini tidak menunjukkan proses menggoreng. Mengubah cara memasak belum menyasar bukti yang sedang diuji.";
                case FormulaIcon.VariedSpread:
                    return fits ? "Variasi ini mengganti sebagian mentega dengan alpukat sebagai pilihan kaya lemak tak jenuh, bukan sekadar menambah makanan di samping sumber lama." :
                        "Tindakan ini khusus penggantian olesan mentega. Rekaman sasaran tidak menunjukkan sumber tersebut.";
                default: throw new ArgumentOutOfRangeException(nameof(icon));
            }
        }
    }
}
