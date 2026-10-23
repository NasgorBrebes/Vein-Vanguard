using System;

namespace VeinVanguard.Editor
{
    public static class UniversalFormulaRuleChecks
    {
        public static string VerifyAll()
        {
            VerifySourceRules();
            VerifyInvalidInputs();
            for (int encounter = 0; encounter < 2; encounter++) VerifyBattle(encounter);
            return "PASS: universal source traits, alternate/duplicate formulas, 0/20/40 damage, invalid inputs, one-scan diagnosis, phase/MP/turn/reset gates";
        }

        static void VerifySourceRules()
        {
            int[,] scores = {
                { 2, 0, 0, 2, 0, 2 }, // Butter: added ingredient, saturated fat, spread.
                { 0, 0, 0, 2, 1, 0 }, // Coconut oil: saturated fat used for frying.
                { 2, 2, 0, 0, 0, 0 }, // Syrup: an added beverage ingredient.
                { 0, 2, 2, 0, 0, 0 }, // Juice: beverage and juice, no added ingredient.
            };
            string[] icons = { "formula_added", "formula_water", "formula_whole", "formula_unsaturated", "formula_steam", "formula_avocado" };
            for (int source = 0; source < 4; source++)
                for (int icon = 0; icon < 6; icon++)
                    Check(UniversalFormula.Score(source / 2, source % 2, (FormulaIcon)icon) == scores[source, icon], "Source trait matched the wrong formula");
            for (int icon = 0; icon < 6; icon++)
            {
                var choice = (FormulaIcon)icon;
                Check(UniversalFormula.IsValid(choice) && UniversalFormula.Complete(choice, choice), "Repeated known icons must fill both sockets");
                Check(!string.IsNullOrWhiteSpace(UniversalFormula.Title(choice)) && UniversalFormula.IconName(choice) == icons[icon], "Formula cannot resolve its visible title/icon");
            }
            Check(UniversalFormula.Damage(0, FormulaIcon.ReduceAdded, FormulaIcon.UnsaturatedFat) == 40, "Lipid source-specific pair failed");
            Check(UniversalFormula.Damage(0, FormulaIcon.UnsaturatedFat, FormulaIcon.UnsaturatedFat) == 40, "Repeated unsaturated-fat replacements failed");
            Check(UniversalFormula.Damage(0, FormulaIcon.VariedSpread, FormulaIcon.UnsaturatedFat) == 40, "Avocado spread replacement failed");
            Check(UniversalFormula.Damage(1, FormulaIcon.ReduceAdded, FormulaIcon.WholeFruit) == 40, "Sugar source-specific pair failed");
            Check(UniversalFormula.Damage(1, FormulaIcon.Water, FormulaIcon.Water) == 40, "Repeated water replacements failed");
            Check(UniversalFormula.Damage(1, FormulaIcon.Water, FormulaIcon.WholeFruit) == 40, "Alternative beverage replacement failed");
            Check(UniversalFormula.Damage(0, FormulaIcon.ReduceAdded, FormulaIcon.Steam) == 20, "Cooking must remain partial without changing fat type");
            Check(UniversalFormula.Damage(0, FormulaIcon.Water, FormulaIcon.Steam) == 20, "One partial source must produce partial damage");
            Check(UniversalFormula.Damage(1, FormulaIcon.WholeFruit, FormulaIcon.ReduceAdded) == 0, "Sockets must stay mapped to source A/B");
            Check(UniversalFormula.Damage(0, FormulaIcon.Water, FormulaIcon.WholeFruit) == 0, "Unrelated choices produced damage");
            Check(CombatSamples.Get(0, 1).record.Contains("menggoreng"), "Partial cooking has no frying evidence in the record");
            for (int encounter = 0; encounter < 2; encounter++)
                foreach (FormulaIcon icon in Enum.GetValues(typeof(FormulaIcon)))
                {
                    string feedback = UniversalFormula.Feedback(encounter, icon, icon);
                    Check(feedback.Contains(CombatSamples.Get(encounter, 0).title) && feedback.Contains(CombatSamples.Get(encounter, 1).title), "Formula feedback omitted a source");
                }
        }

        static void VerifyInvalidInputs()
        {
            Check(!UniversalFormula.IsValid(FormulaIcon.None) && UniversalFormula.IconName(FormulaIcon.None) == null, "Empty socket was treated as a formula");
            Check(!string.IsNullOrWhiteSpace(UniversalFormula.Title(FormulaIcon.None)), "Empty socket has no visible label");
            foreach (int id in new[] { -2, -1, 6, int.MaxValue })
            {
                var icon = (FormulaIcon)id;
                Check(!UniversalFormula.IsValid(icon) && !UniversalFormula.Complete(icon, FormulaIcon.Water) && !UniversalFormula.Complete(FormulaIcon.Water, icon), "Unknown or empty socket completed a formula");
                Check(UniversalFormula.Score(1, 0, icon) == 0, "Unknown icon matched a source");
                if (icon == FormulaIcon.None) continue;
                RejectsArgument(() => UniversalFormula.Title(icon));
                RejectsArgument(() => UniversalFormula.IconName(icon));
                RejectsArgument(() => UniversalFormula.Feedback(0, icon, FormulaIcon.Water));
            }
            foreach (int id in new[] { -1, 2, int.MaxValue })
            {
                RejectsArgument(() => UniversalFormula.Score(id, 0, FormulaIcon.Water));
                RejectsArgument(() => UniversalFormula.Score(0, id, FormulaIcon.Water));
                RejectsArgument(() => UniversalFormula.Damage(id, FormulaIcon.Water, FormulaIcon.Water));
                RejectsArgument(() => UniversalFormula.Feedback(id, FormulaIcon.Water, FormulaIcon.Water));
            }
        }

        static void VerifyBattle(int encounter)
        {
            FormulaIcon first = encounter == 0 ? FormulaIcon.ReduceAdded : FormulaIcon.Water;
            FormulaIcon second = encounter == 0 ? FormulaIcon.UnsaturatedFat : FormulaIcon.WholeFruit;
            var model = new BattleModel();
            Check(!model.DiagnoseAll(), "Menu allowed diagnosis");
            model.Begin(encounter);
            Check(!model.CanSynthesizeFormula(first, second) && model.AttackFormula(first, second) == -1 && model.energy == 60 && model.enemyHp == model.MaxEnemyHp, "Undiagnosed attack changed combat");
            Check(model.DiagnoseAll() && model.diagnosed && model.ClassifiedCount == 2 && model.WasScanned(0) && model.WasScanned(1) && model.CanAttack && model.phase == BattlePhase.Player, "Single scan failed to reveal both sources");
            Check(!model.DiagnoseAll() && model.CanSynthesizeFormula(first, second) && model.energy == 60 && model.enemyHp == model.MaxEnemyHp, "Diagnosis/preview mutated a ready turn");

            foreach (int id in new[] { -2, -1, 6, int.MaxValue })
            {
                var invalid = (FormulaIcon)id;
                Check(!model.CanSynthesizeFormula(invalid, second) && !model.CanSynthesizeFormula(first, invalid), "Invalid socket enabled firing");
                Check(model.AttackFormula(invalid, second) == -1 && model.AttackFormula(first, invalid) == -1 && model.energy == 60 && model.enemyHp == model.MaxEnemyHp && model.phase == BattlePhase.Player, "Invalid formula mutated battle");
            }
            model.energy = 19;
            Check(!model.CanSynthesizeFormula(first, second) && model.AttackFormula(first, second) == -1 && model.energy == 19 && model.phase == BattlePhase.Player, "Low-energy attack was accepted");

            foreach (int damage in new[] { 0, 20, 40 })
            {
                model = Ready(encounter); model.energy = 20;
                var a = damage == 0 ? (encounter == 0 ? FormulaIcon.Water : FormulaIcon.Steam) : first;
                var b = damage == 40 ? second : (encounter == 0 ? FormulaIcon.WholeFruit : FormulaIcon.VariedSpread);
                int beforeHp = model.enemyHp;
                Check(model.CanSynthesizeFormula(a, b) && model.energy == 20 && model.enemyHp == beforeHp, "Complete preview was rejected or spent resources");
                Check(model.AttackFormula(a, b) == damage && model.energy == 0 && model.enemyHp == beforeHp - damage && model.phase == BattlePhase.Enemy, "Formula damage or single 20-MP cost failed");
                Check(model.AttackFormula(a, b) == -1 && model.energy == 0 && model.enemyHp == beforeHp - damage && model.phase == BattlePhase.Enemy, "Duplicate shot bypassed turn lock");
            }
            foreach (BattlePhase phase in Enum.GetValues(typeof(BattlePhase)))
            {
                if (phase == BattlePhase.Player) continue;
                model = Ready(encounter); model.phase = phase;
                Check(!model.CanSynthesizeFormula(first, second) && model.AttackFormula(first, second) == -1 && model.energy == 60 && model.enemyHp == model.MaxEnemyHp && model.phase == phase, "Wrong-phase shot mutated combat");
            }
            foreach (BattlePhase phase in Enum.GetValues(typeof(BattlePhase)))
            {
                if (phase == BattlePhase.Diagnose) continue;
                model = new BattleModel(); model.Begin(encounter); model.phase = phase;
                Check(!model.DiagnoseAll() && !model.diagnosed && model.ClassifiedCount == 0 && !model.WasScanned(0) && !model.WasScanned(1) && model.phase == phase, "Wrong-phase scan mutated diagnosis");
            }
            model = new BattleModel(); model.Begin(encounter);
            model.diagnosed = true; model.phase = BattlePhase.Player;
            Check(!model.CanSynthesizeFormula(first, second) && model.AttackFormula(first, second) == -1, "Public diagnosis flag bypassed source masks");

            model = new BattleModel(); model.Begin(encounter);
            Check(model.ScanSample(0) && model.ClassifySample(0, encounter == 0 ? SampleClass.SaturatedFat : SampleClass.FreeSugar) && model.DiagnoseAll() && model.IsClassified(1), "Single scan could not finish an existing partial diagnosis");
            model = Ready(encounter); model.enemyHp = 30;
            Check(model.AttackFormula(first, second) == 40 && model.enemyHp == 0 && model.energy == 40 && model.phase == BattlePhase.Victory, "Lethal formula failed");
            Check(model.AttackFormula(first, second) == -1 && model.EnemyAttack() == -1 && model.hp == 100, "Victory allowed another combat action");
            model.hp = 42; model.energy = 37;
            model.Begin(1 - encounter);
            Check(!model.diagnosed && model.ClassifiedCount == 0 && !model.WasScanned(0) && !model.WasScanned(1) && !model.CanSynthesizeFormula(first, second) && model.phase == BattlePhase.Diagnose && model.hp == 42 && model.energy == 37 && model.enemyHp == model.MaxEnemyHp, "Encounter reset kept a formula gate or lost persistent resources");
        }

        static BattleModel Ready(int encounter)
        {
            var model = new BattleModel(); model.Begin(encounter);
            Check(model.DiagnoseAll(), "Formula setup diagnosis failed");
            return model;
        }
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void RejectsArgument(Action action)
        {
            try { action(); }
            catch (ArgumentOutOfRangeException) { return; }
            throw new Exception("Invalid formula data argument was accepted");
        }
    }
}
