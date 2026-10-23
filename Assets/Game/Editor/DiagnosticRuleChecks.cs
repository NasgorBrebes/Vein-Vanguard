using System;

namespace VeinVanguard.Editor
{
    public static class DiagnosticRuleChecks
    {
        public static string VerifyDiagnosisGate()
        {
            var model = new BattleModel();
            model.Begin(1);
            if (model.Diagnose() || model.diagnosed || model.CanAttack)
                throw new Exception("Diagnosis must not unlock combat before both samples are scanned and classified.");
            return "PASS: interactive diagnosis gate";
        }

        public static string VerifyAll()
        {
            VerifyDiagnosisGate();
            for (int encounter = 0; encounter < 2; encounter++)
            {
                int kind = encounter == 0 ? 1 : 0;
                int module = encounter == 0 ? 1 : 0;
                int firstAdapter = encounter == 0 ? 2 : 0;
                for (int target = 0; target < 2; target++)
                {
                    var model = new BattleModel(); model.Begin(encounter);
                    Check(!Classify(model, target, kind), "Unscanned sample was classified");
                    Check(Scan(model, target) && !Scan(model, target), "Scan must register once");
                    Check(!Classify(model, target, 2) && Count(model) == 0, "Wrong classification unlocked a sample");
                    Check(Classify(model, target, kind) && Count(model) == 1, "Correct classification was not recorded");
                    Check(!Classify(model, target, 2) && Classified(model, target), "A completed classification must stay locked");
                    Check(!model.Diagnose() && !model.CanAttack && !model.Homeostasis(), "One sample unlocked combat");
                    int other = 1 - target;
                    Check(Scan(model, other) && Classify(model, other, kind), "Second sample failed");
                    Check(!model.CanAttack && model.Diagnose() && model.CanAttack, "Diagnosis completion gate failed");
                    Check(!model.Diagnose() && !Scan(model, target) && !Classify(model, target, kind), "Completed diagnosis accepted duplicate input");

                    int[] invalid = { -1, 2, int.MaxValue };
                    foreach (int id in invalid)
                        Check(!Scan(model, id) && !Classify(model, id, kind) && !Classified(model, id) && !Scanned(model, id), "Invalid sample ID accepted");
                    Check(!CanSynthesize(model, target, -1, firstAdapter) && !CanSynthesize(model, target, 99, firstAdapter), "Invalid module accepted");
                    Check(!CanSynthesize(model, target, module, -1) && !CanSynthesize(model, target, module, 99), "Invalid adapter accepted");
                    Check(!CanSynthesize(model, target, module, encounter == 0 ? 0 : 2), "Other encounter's adapter accepted");
                    Check(Attack(model, -1, module, firstAdapter) == -1 && model.energy == 60, "Invalid attack changed energy");
                    model.energy = 19;
                    Check(!model.CanAttack && Attack(model, target, module, firstAdapter + target) == -1 && model.energy == 19, "Insufficient energy gate failed");

                    foreach (int damage in new[] { 0, 20, 40 })
                    {
                        model = Ready(encounter); model.energy = 20;
                        int chosenModule = damage == 0 ? 2 : module;
                        int adapter = firstAdapter + (damage == 20 ? 1 - target : target);
                        Check(CanSynthesize(model, target, chosenModule, adapter), "A complete valid assembly was rejected");
                        int beforeHp = model.enemyHp;
                        Check(Attack(model, target, chosenModule, adapter) == damage && model.enemyHp == beforeHp - damage && model.energy == 0 && model.phase == BattlePhase.Enemy, "Synthesis outcome or single cost failed");
                        Check(Attack(model, target, chosenModule, adapter) == -1 && model.energy == 0 && model.enemyHp == beforeHp - damage, "Repeat attack bypassed turn lock");
                    }
                    model = Ready(encounter);
                    Check(Attack(model, target, 1 - module, firstAdapter + target) == 0 && model.energy == 40 && model.phase == BattlePhase.Enemy, "Other nutrient module must also cost 20 MP and deal zero damage");

                    model = Ready(encounter); model.enemyHp = 30;
                    Check(Attack(model, target, module, firstAdapter + target) == 40 && model.enemyHp == 0 && model.phase == BattlePhase.Victory, "Lethal synthesis failed");
                    Check(model.EnemyAttack() == -1 && model.hp == 100, "Defeated enemy counterattacked");
                }

                var pending = new BattleModel(); pending.Begin(encounter);
                foreach (int id in new[] { -1, 2, 32, int.MaxValue })
                    Check(!Scan(pending, id) && !Classify(pending, id, kind) && !Scanned(pending, id) && !Classified(pending, id), "Invalid diagnosis target changed state");
                Check(Scan(pending, 0) && !Classify(pending, 0, -1) && !Classify(pending, 0, 99) && Count(pending) == 0 && pending.phase == BattlePhase.Diagnose && pending.energy == 60, "Invalid classification changed state");
                pending.diagnosed = true; pending.phase = BattlePhase.Player;
                Check(!pending.CanAttack && !CanSynthesize(pending, 0, module, firstAdapter), "Public diagnosed flag bypassed sample gates");
                foreach (BattlePhase phase in Enum.GetValues(typeof(BattlePhase)))
                {
                    if (phase == BattlePhase.Diagnose) continue;
                    pending = new BattleModel(); pending.Begin(encounter); pending.phase = phase;
                    Check(!Scan(pending, 0) && !Classify(pending, 0, kind) && !pending.Diagnose() && Count(pending) == 0, "Wrong-phase diagnosis changed state");
                }
                foreach (BattlePhase phase in Enum.GetValues(typeof(BattlePhase)))
                {
                    if (phase == BattlePhase.Player) continue;
                    pending = Ready(encounter); pending.phase = phase;
                    int beforeHp = pending.enemyHp;
                    Check(!pending.CanAttack && Attack(pending, 0, module, firstAdapter) == -1 && pending.energy == 60 && pending.enemyHp == beforeHp && pending.phase == phase, "Wrong-phase synthesis changed state");
                }
                pending = Ready(encounter); pending.energy = 37; pending.hp = 42;
                RejectsArgument(() => pending.Begin(-1));
                Check(pending.energy == 37 && pending.hp == 42 && pending.encounter == encounter && pending.diagnosed && Count(pending) == 2, "Invalid encounter mutated the model");
                pending.Begin(1 - encounter);
                Check(Count(pending) == 0 && !Scanned(pending, 0) && !Scanned(pending, 1) && !pending.diagnosed && !pending.CanAttack && pending.energy == 37 && pending.hp == 42, "Encounter reset lost persistent resources or retained samples");
                VerifyParry(encounter);
            }
            VerifySampleData();
            VerifyRechargeAndTrivia();
            return "PASS: both encounters, scan/classification gates, invalid inputs, synthesis 0/20/40, single MP cost, turn/reset gates, parry/reflection, recharge and trivia";
        }

        static void VerifySampleData()
        {
            for (int encounter = 0; encounter < 2; encounter++)
                for (int target = 0; target < 2; target++)
                {
                    var sample = CombatSamples.Get(encounter, target);
                    Check(sample.kind == (encounter == 0 ? SampleClass.SaturatedFat : SampleClass.FreeSugar), "Wrong source classification");
                    Check(sample.module == (encounter == 0 ? SynthesisModule.ImproveFatChoices : SynthesisModule.LimitFreeSugars), "Wrong source module");
                    Check((int)sample.adapter == (encounter == 0 ? 2 + target : target), "Wrong source-specific adapter");
                    Check(!string.IsNullOrWhiteSpace(sample.record) && !string.IsNullOrWhiteSpace(sample.icon) && !string.IsNullOrWhiteSpace(sample.classificationFeedback) && !string.IsNullOrWhiteSpace(sample.adapterFeedback), "Missing source record or explanation");
                }
            foreach (int invalid in new[] { -1, 2, int.MaxValue })
            {
                RejectsArgument(() => CombatSamples.Get(invalid, 0));
                RejectsArgument(() => CombatSamples.Get(0, invalid));
            }
            RejectsArgument(() => CombatSamples.ModuleTitle((SynthesisModule)99));
            RejectsArgument(() => CombatSamples.ModuleIcon((SynthesisModule)99));
            RejectsArgument(() => CombatSamples.AdapterTitle((SourceAdapter)99));
            RejectsArgument(() => CombatSamples.AdapterIcon((SourceAdapter)99));
            RejectsArgument(() => CombatSamples.AttackFeedback(0, 0, (SynthesisModule)99, SourceAdapter.ReduceButter));
            RejectsArgument(() => CombatSamples.AttackFeedback(0, 0, SynthesisModule.ImproveFatChoices, (SourceAdapter)99));
            Check(TriviaBank.LipidQuestions[0].options[TriviaBank.LipidQuestions[0].answer] == "Lemak jenuh", "Lipid trivia contradicts diagnosis");
            Check(TriviaBank.GlucoQuestions[0].options[TriviaBank.GlucoQuestions[0].answer] == "Gula bebas", "Gluco trivia contradicts diagnosis");
        }

        static void VerifyParry(int encounter)
        {
            var model = Ready(encounter);
            int incoming = encounter == 0 ? 14 : 18;
            int originalHp = model.enemyHp;
            Check(model.Homeostasis() && !model.Homeostasis() && model.energy == 50, "Homeostasis activation or duplicate gate failed");
            Check(model.EnemyAttack() == 0 && model.hp == 100 && model.enemyHp == originalHp - incoming && model.lastReflectedDamage == incoming && model.shieldHits == 1, "First parry must fully block and reflect");
            Check(!model.Homeostasis(), "Shield stacked");
            model.phase = BattlePhase.Enemy;
            Check(model.EnemyAttack() == 0 && model.hp == 100 && model.enemyHp == originalHp - incoming && model.lastReflectedDamage == 0 && model.shieldHits == 0 && model.shield == 0, "Second parry must fully block without reflection");
            model.phase = BattlePhase.Enemy;
            Check(model.EnemyAttack() == incoming && model.hp == 100 - incoming, "Unprotected hit was reduced");
            model = Ready(encounter); model.enemyHp = incoming;
            Check(model.Homeostasis() && model.EnemyAttack() == 0 && model.enemyHp == 0 && model.phase == BattlePhase.Victory, "Lethal reflection failed");
        }

        static void VerifyRechargeAndTrivia()
        {
            var model = Ready(1); model.energy = 30;
            Check(model.Restore() && model.Answer(true) && model.energy == 60 && model.correct == 1 && model.answered == 1 && model.shieldHits == 0, "Correct recharge cap or passive shield changed");
            Check(!model.Answer(true) && model.answered == 1, "Duplicate trivia answer accepted");
            model.phase = BattlePhase.Player; model.energy = 57;
            Check(model.Restore() && model.Answer(false) && model.energy == 60 && model.correct == 1 && model.answered == 2, "Wrong recharge cap changed");
            model.phase = BattlePhase.Player;
            Check(!model.Restore(), "Full-energy recharge accepted");
            var deck = new TriviaDeck(new Random(17));
            for (int encounter = 0; encounter < 2; encounter++)
            {
                var bank = TriviaBank.ForEncounter(encounter);
                var seen = new System.Collections.Generic.HashSet<string>();
                foreach (var source in bank)
                {
                    var question = deck.Next(encounter);
                    Check(seen.Add(question.prompt), "Trivia repeated before the encounter deck was exhausted");
                    var original = Array.Find(bank, candidate => candidate.prompt == question.prompt);
                    Check(original != null && question.options[question.answer] == original.options[original.answer], "Shuffled answer lost correctness");
                }
            }
        }

        static BattleModel Ready(int encounter)
        {
            var model = new BattleModel(); model.Begin(encounter);
            for (int target = 0; target < 2; target++)
                Check(Scan(model, target) && Classify(model, target, encounter == 0 ? 1 : 0), "Diagnosis setup failed");
            Check(model.Diagnose(), "Diagnosis setup did not complete");
            return model;
        }
        static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void RejectsArgument(Action action)
        {
            try { action(); }
            catch (ArgumentOutOfRangeException) { return; }
            throw new Exception("Invalid data argument was accepted");
        }
        static bool Scan(BattleModel model, int target) => model.ScanSample(target);
        static bool Scanned(BattleModel model, int target) => model.WasScanned(target);
        static bool Classified(BattleModel model, int target) => model.IsClassified(target);
        static bool Classify(BattleModel model, int target, int choice) => model.ClassifySample(target, (SampleClass)choice);
        static int Count(BattleModel model) => model.ClassifiedCount;
        static bool CanSynthesize(BattleModel model, int target, int module, int adapter) => model.CanSynthesize(target, (SynthesisModule)module, (SourceAdapter)adapter);
        static int Attack(BattleModel model, int target, int module, int adapter) => model.Attack(target, (SynthesisModule)module, (SourceAdapter)adapter);
    }
}
