using System.Linq;
using UnityEngine;

namespace VeinVanguard
{
    public partial class BattleManager
    {
        static readonly string[] NanobotDirections =
        {
            "down", "right", "left", "up", "down-right", "down-left", "up-right", "up-left"
        };
        static readonly string[] NanobotBattlePoses =
        {
            "idle", "diagnose", "synthesize", "nutrient_attack", "restore", "trivia_wait",
            "homeostasis_activate", "homeostasis_hit", "hurt", "low_energy", "victory", "shutdown"
        };
        static readonly int[] NanobotBattleFrames =
        {
            48, 30, 30, 36, 30, 48, 30, 30, 30, 48, 36, 36
        };
        readonly Sprite[][] nanobotWalk = new Sprite[8][];
        readonly Sprite[][] nanobotBattle = new Sprite[12][];
        Sprite[] nanobotWorldIdle;
        float playerPoseTime;
        const float NanobotFootPivot = 40f / 384f;

        static Sprite[] LoadNanobotClip(string name, int count)
        {
            // If the sprite sheet has been sliced as Sprite Multiple in the asset database,
            // load all sliced sprites directly.
            var multi = Resources.LoadAll<Sprite>("NanobotMotion/Nanobot-" + name);
            if (multi != null && multi.Length == count)
            {
                var sorted = multi.OrderBy(s => s.name).ToArray();
                return sorted;
            }

            const int columns = 6;
            var sheet = Resources.Load<Texture2D>("NanobotMotion/Nanobot-" + name);
            if (!sheet || sheet.width % columns != 0) return System.Array.Empty<Sprite>();
            int cell = sheet.width / columns;
            if (sheet.height != ((count + columns - 1) / columns) * cell)
                return System.Array.Empty<Sprite>();
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                var rect = new Rect(i % columns * cell, sheet.height - (i / columns + 1) * cell, cell, cell);
                frames[i] = Sprite.Create(sheet, rect, new Vector2(.5f, NanobotFootPivot), 100, 0, SpriteMeshType.FullRect);
                frames[i].name = name + "-" + i.ToString("00");
            }
            return frames;
        }

        bool ApplyNanobotWorldPose(bool moving, int direction, float cycle)
        {
            var frames = moving
                ? (nanobotWalk[direction] ??= LoadNanobotClip("walk-" + NanobotDirections[direction], 24))
                : (nanobotWorldIdle ??= LoadNanobotClip("exploration-idle", 48));
            if (frames.Length == 0) return false;
            int frame = Mathf.Min(frames.Length - 1, (int)(Mathf.Repeat(cycle, 1) * frames.Length));
            worldPlayerImage.sprite = frames[frame];
            worldPlayer.pivot = new Vector2(.5f, NanobotFootPivot);
            worldPlayer.sizeDelta = Vector2.one * (moving ? 158 : 165);
            return true;
        }

        bool ApplyNanobotBattlePose(int pose)
        {
            if (pose < 0 || pose >= NanobotBattlePoses.Length) return false;
            bool loop = pose == 0 || pose == 5 || pose == 9;
            int count = NanobotBattleFrames[pose];
            var frames = nanobotBattle[pose] ??= LoadNanobotClip(NanobotBattlePoses[pose].Replace('_', '-'), count);
            if (frames.Length == 0) return false;
            // Battle actions run at 30 FPS. One-shot actions hold the last frame until pose changes.
            int frame = loop ? (int)(playerPoseTime * 30) % frames.Length :
                Mathf.Min(frames.Length - 1, (int)(playerPoseTime * 30));
            playerArt.sprite = frames[frame];
            return true;
        }

        void DisposeNanobotMotion()
        {
            foreach (var clip in nanobotWalk) DestroyNanobotClip(clip);
            foreach (var clip in nanobotBattle) DestroyNanobotClip(clip);
            DestroyNanobotClip(nanobotWorldIdle);
        }

        static void DestroyNanobotClip(Sprite[] frames)
        {
            if (frames == null) return;
            foreach (var frame in frames)
            {
#if UNITY_EDITOR
                if (frame && !UnityEditor.AssetDatabase.Contains(frame)) Destroy(frame);
#else
                if (frame) Destroy(frame);
#endif
            }
        }
    }
}
