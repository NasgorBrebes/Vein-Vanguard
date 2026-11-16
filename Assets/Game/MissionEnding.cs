using System;
using System.IO;
using UnityEngine;

namespace VeinVanguard
{
    [Serializable]
    public sealed class EndingMissionResult
    {
        public string phase;
        public bool[] defeated;
        public int hp, correct, answered;
        public static EndingMissionResult Capture(BattleManager game)
        {
            var m = game.model;
            if (m.phase != BattlePhase.Complete || m.hp <= 0 || m.hp > 100 ||
                m.enemyHp != 0 || m.correct < 0 || m.answered < m.correct ||
                game.defeated == null || game.defeated.Length != EncounterCatalog.Count)
                throw new InvalidOperationException("Mission result is not a successful completed session.");
            foreach (bool flag in game.defeated)
                if (!flag) throw new InvalidOperationException("An encounter remains unfinished.");
            return new EndingMissionResult { phase = "Complete", defeated = (bool[])game.defeated.Clone(),
                hp = m.hp, correct = m.correct, answered = m.answered };
        }
    }

    [Serializable]
    public sealed class EndingMissionSnapshot
    {
        public int schemaVersion = 1;
        public string capturedAtUtc;
        public EndingMissionResult result;
    }

    public partial class BattleManager
    {
        MissionEndingPlayback missionEnding;
        bool missionEndingCaptured;
        public bool EndingPlaying => missionEnding && missionEnding.Playing;
        public static string EndingSnapshotPath => Path.Combine(Application.persistentDataPath, "ending-mission.json");

        bool TryBeginMissionEnding()
        {
            if (missionEndingCaptured) return EndingPlaying;
            EndingMissionResult snapshot;
            try
            {
                snapshot = EndingMissionResult.Capture(this);
                var envelope = new EndingMissionSnapshot { capturedAtUtc = DateTime.UtcNow.ToString("O"), result = snapshot };
                Directory.CreateDirectory(Application.persistentDataPath);
                string temporary = EndingSnapshotPath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(envelope, true));
                if (File.Exists(EndingSnapshotPath)) File.Replace(temporary, EndingSnapshotPath, null);
                else File.Move(temporary, EndingSnapshotPath);
            }
            catch (Exception error)
            {
                Debug.LogWarning("Ending snapshot could not be captured: " + error.Message);
                return false;
            }
            missionEndingCaptured = true;
            var clip = Resources.Load<UnityEngine.Video.VideoClip>(ReducedArenaMotion ? "VeinVanguardEndingV21Reduced" : "VeinVanguardEndingV21");
            if (!clip)
            {
                if (Debug.isDebugBuild) Debug.LogWarning("Ending video missing; using the existing mission result dialog. Snapshot saved to " + EndingSnapshotPath);
                return false;
            }
            if (!missionEnding) missionEnding = gameObject.AddComponent<MissionEndingPlayback>();
            HideDialog();
            actionAnimating = true;
            if (joystick) joystick.ResetInput();
            heldDirection = Vector2.zero;
            hasTarget = false;
            if (effectSource) effectSource.Stop();
            missionEnding.Begin(clip, snapshot, MusicVolume,
                () => { CancelMissionEnding(); ShowMenu(); StartGameFromUi(); },
                () => { CancelMissionEnding(); ShowMenu(); },
                () => {
                    CancelMissionEnding();
                    Dialog("MISI SELESAI", $"Patogen dinetralisir: {snapshot.defeated.Length}\nSisa integritas: {snapshot.hp}/100 • Kuis benar: {snapshot.correct}/{snapshot.answered}",
                        new[]{"MAIN LAGI","MENU UTAMA"},
                        new UnityEngine.Events.UnityAction[]{() => {ShowMenu(); StartGameFromUi();}, ShowMenu});
                });
            return true;
        }

        void CancelMissionEnding()
        {
            if (missionEnding) missionEnding.StopPlayback();
            actionAnimating = false;
        }

        void ResetMissionEnding()
        {
            CancelMissionEnding();
            missionEndingCaptured = false;
            try { if (File.Exists(EndingSnapshotPath)) File.Delete(EndingSnapshotPath); }
            catch (IOException error) { Debug.LogWarning("Cannot reset ending snapshot: " + error.Message); }
        }
    }
}
