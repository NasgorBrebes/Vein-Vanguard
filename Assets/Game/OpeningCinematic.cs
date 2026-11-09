using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace VeinVanguard
{
    public partial class BattleManager
    {
        public AudioClip openingMusic;
        CanvasGroup openingGroup;
        UnityEngine.UI.Button openingSkip;
        TextMeshProUGUI openingSubtitle, openingSpeaker, openingChapter;
        UnityEngine.UI.Image openingCurtain;
        float openingTime, openingMusicVolume;
        double openingLastTick;
        int openingCue = -1, openingScene = -1, openingStartedFrame, openingSound = -1;
        bool openingFocused = true, openingPaused, openingReducedMotion, openingWaitForMovementRelease, openingMapPrepared;

        public bool OpeningPlaying => openingGroup && openingGroup.blocksRaycasts;

        readonly struct OpeningCue
        {
            public readonly float time;
            public readonly string speaker, text;
            public OpeningCue(float time, string speaker, string text)
            {
                this.time = time; this.speaker = speaker; this.text = text;
            }
        }

        static readonly OpeningCue[] openingScript =
        {
            new OpeningCue(.8f, "SISTEM", "PERINGATAN SISTEM"),
            new OpeningCue(3.5f, "SISTEM", "Anomali terdeteksi dalam simulasi pembuluh darah."),
            new OpeningCue(6.8f, "SISTEM", "Stabilitas sistem berada dalam ancaman."),
            new OpeningCue(10, "SISTEM", "Analisis awal selesai."),
            new OpeningCue(13, "SISTEM", "Ancaman berkaitan dengan pola konsumsi pangan, paparan asap tembakau, dan alkohol."),
            new OpeningCue(18.5f, "SISTEM", "Seluruh ancaman harus dianalisis untuk memulihkan kestabilan simulasi."),
            new OpeningCue(22, "SISTEM", "Mengaktifkan unit pertahanan medis..."),
            new OpeningCue(25, "SISTEM", "UNIT: VEIN VANGUARD"),
            new OpeningCue(28, "SISTEM", "STATUS: ONLINE"),
            new OpeningCue(30, "NANOBOT", "Perintah diterima."),
            new OpeningCue(32, "NANOBOT", "Saya siap menjalankan misi."),
            new OpeningCue(35, "SISTEM", "Nanobot, kekuatan saja tidak cukup."),
            new OpeningCue(38, "SISTEM", "Setiap ancaman memiliki dua rekaman yang harus dianalisis."),
            new OpeningCue(41, "SISTEM", "Temukan informasi yang sesuai, susun formula, dan gunakan energi dengan bijak."),
            new OpeningCue(44.5f, "NANOBOT", "Memahami ancaman adalah kunci kemenangan."),
            new OpeningCue(47, "SISTEM", "Enam ancaman telah teridentifikasi."),
            new OpeningCue(50, "SISTEM", "Mulai dari sektor pertama."),
            new OpeningCue(52.5f, "SISTEM", "Keberhasilan misi bergantung pada setiap keputusanmu."),
            new OpeningCue(56, "NANOBOT", "Vein Vanguard, memulai operasi!")
        };

        readonly struct OpeningSoundCue
        {
            public readonly float time, gain;
            public readonly int clip;
            public readonly bool stopPrevious;
            public OpeningSoundCue(float time, int clip, float gain, bool stopPrevious = false)
            {
                this.time = time; this.clip = clip; this.gain = gain; this.stopPrevious = stopPrevious;
            }
        }

        static readonly OpeningSoundCue[] openingSounds =
        {
            new OpeningSoundCue(.4f, 0, .10f), new OpeningSoundCue(1.5f, 0, .10f),
            new OpeningSoundCue(2.75f, 1, .26f, true), new OpeningSoundCue(4.2f, 0, .10f),
            new OpeningSoundCue(6.2f, 0, .10f), new OpeningSoundCue(10.1f, 0, .18f, true),
            new OpeningSoundCue(13.5f, 0, .18f, true), new OpeningSoundCue(16.9f, 0, .18f, true),
            new OpeningSoundCue(20.2f, 5, .13f, true), new OpeningSoundCue(22.2f, 2, .24f, true),
            new OpeningSoundCue(28, 3, .38f, true), new OpeningSoundCue(30, 0, .12f),
            new OpeningSoundCue(35.15f, 0, .20f, true), new OpeningSoundCue(38.1f, 2, .12f, true),
            new OpeningSoundCue(40, 4, .22f, true), new OpeningSoundCue(41.2f, 2, .22f, true),
            new OpeningSoundCue(44.1f, 3, .30f, true), new OpeningSoundCue(47.1f, 5, .14f, true),
            new OpeningSoundCue(54.1f, 3, .18f, true), new OpeningSoundCue(56.6f, 3, .25f, true)
        };

        void BeginOpeningCinematic()
        {
            if (OpeningPlaying || IsStageLoading || View != GameView.Title) return;
            EnsureOpeningCinematic();
            CloseOverlay();
            if (joystick) joystick.ResetInput();
            heldDirection = Vector2.zero;
            hasTarget = false;
            openingTime = 0;
            openingLastTick = Time.realtimeSinceStartupAsDouble;
            openingMapPrepared = false;
            openingSound = -1;
            openingCue = openingScene = -1;
            openingStartedFrame = Time.frameCount;
            openingReducedMotion = ReducedArenaMotion;
            openingGroup.alpha = 1;
            openingStoryContent.alpha = 1;
            openingGroup.blocksRaycasts = openingGroup.interactable = true;
            openingSkip.interactable = true;
            titleRoot.gameObject.SetActive(false);
            worldRoot.gameObject.SetActive(false);
            battleCanvas.SetActive(false);
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            openingMusicVolume = musicSource ? musicSource.volume : 0;
            PlayMusic(openingMusic ? openingMusic : mapMusic);
            RenderOpeningCinematic();
        }

        void TickOpeningCinematic()
        {
            UpdateOpeningSafeArea();
            double now = Time.realtimeSinceStartupAsDouble;
            float delta = Mathf.Max(0, (float)(now - openingLastTick));
            openingLastTick = now;
            if (!openingFocused || openingPaused) return;
            openingTime += delta;
            if (openingTime >= 60)
            {
                CompleteOpeningCinematic();
                return;
            }
            RenderOpeningCinematic();
            var keyboard = Keyboard.current;
            if (keyboard == null || Time.frameCount == openingStartedFrame || !EventSystem.current) return;
            if (keyboard.tabKey.wasPressedThisFrame)
                EventSystem.current.SetSelectedGameObject(openingSkip.gameObject);
            else if ((keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame) &&
                     EventSystem.current.currentSelectedGameObject == openingSkip.gameObject)
                openingSkip.onClick.Invoke();
        }

        void RenderOpeningCinematic()
        {
            UpdateOpeningSafeArea();
            if (openingTime >= 59.3f)
            {
                PrepareOpeningMap();
                openingStoryContent.alpha = 0;
                openingCurtain.color = new Color(0, .015f, .035f, 1 - Mathf.SmoothStep(0, 1, (openingTime - 59.3f) / .7f));
                return;
            }
            int scene = openingTime < 10 ? 0 : openingTime < 22 ? 1 : openingTime < 35 ? 2 : openingTime < 47 ? 3 : 4;
            if (openingScene != scene)
            {
                openingScene = scene;
                openingChapter.text = new[] { "01 / SINYAL DARURAT", "02 / ANCAMAN TERIDENTIFIKASI", "03 / AKTIVASI NANOBOT",
                    "04 / PROTOKOL MISI", "05 / MISI DIMULAI" }[scene];
                for (int i = 0; i < openingSets.Length; i++)
                    openingSets[i].SetActive(i == scene);
            }
            int cue = 0;
            while (cue + 1 < openingScript.Length && openingTime >= openingScript[cue + 1].time) cue++;
            if (openingTime < openingScript[0].time) cue = -1;
            if (cue != openingCue)
            {
                openingCue = cue;
                openingSpeaker.text = cue < 0 ? "" : openingScript[cue].speaker;
                openingSubtitle.text = cue < 0 ? "" : openingScript[cue].text;
            }
            openingSpeaker.color = cue >= 0 && openingScript[cue].speaker == "NANOBOT" ? OpeningCyan : OpeningAmber;
            openingSubtitle.maxVisibleCharacters = cue < 0 ? 0 :
                Mathf.Min(openingScript[cue].text.Length, Mathf.CeilToInt(Mathf.Max(0, openingTime - openingScript[cue].time) * 150));
            RenderOpeningEnvironment(scene);
            RenderOpeningActors(scene);
            RenderOpeningAudio();
            float fade = 1 - Mathf.SmoothStep(0, 1, (openingTime - .9f) / 1);
            fade = Mathf.Max(fade, Mathf.SmoothStep(0, 1, (openingTime - 58.5f) / .8f));
            float boundary = Mathf.Min(Mathf.Min(Mathf.Abs(openingTime - 10), Mathf.Abs(openingTime - 22)),
                Mathf.Min(Mathf.Abs(openingTime - 35), Mathf.Abs(openingTime - 47)));
            if (openingTime > 2 && openingTime < 58.5f) fade = Mathf.Max(fade, 1 - Mathf.SmoothStep(0, 1, boundary / .24f));
            openingCurtain.color = new Color(0, .015f, .035f, fade);
        }

        void RenderOpeningAudio()
        {
            if (!OpeningPlaying || !Application.isPlaying || !openingFocused || openingPaused) return;
            if (musicSource)
                musicSource.volume = openingMusicVolume * Mathf.Lerp(.38f, 1, Mathf.SmoothStep(0, 1, openingTime / 57));
            int index = -1;
            while (index + 1 < openingSounds.Length && openingTime >= openingSounds[index + 1].time) index++;
            if (index <= openingSound || index < 0) return;
            openingSound = index;
            var cue = openingSounds[index];
            AudioClip clip = cue.clip == 0 ? clickSound : cue.clip == 1 ? enemyAttackSound :
                cue.clip == 2 ? chargeSound : cue.clip == 3 ? defendSound : cue.clip == 4 ? attackSound : enemyDefendSound;
            if (!effectSource || !clip) return;
            if (cue.stopPrevious) effectSource.Stop();
            effectSource.PlayOneShot(clip, cue.gain);
        }

        void PrepareOpeningMap()
        {
            if (openingMapPrepared) return;
            openingMapPrepared = true;
            if (effectSource) effectSource.Stop();
            if (musicSource) musicSource.volume = openingMusicVolume;
            StartExploration();
            BeginPortalSpawn();
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }

        void CompleteOpeningCinematic()
        {
            if (!OpeningPlaying) return;
            bool prepared = openingMapPrepared;
            CancelOpeningCinematic();
            if (!prepared) { StartGame(); BeginPortalSpawn(); }
            openingWaitForMovementRelease = OpeningMovementKeysHeld();
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
        }

        static bool OpeningMovementKeysHeld()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && (keyboard.wKey.isPressed || keyboard.aKey.isPressed || keyboard.sKey.isPressed ||
                keyboard.dKey.isPressed || keyboard.upArrowKey.isPressed || keyboard.leftArrowKey.isPressed ||
                keyboard.downArrowKey.isPressed || keyboard.rightArrowKey.isPressed);
        }

        void CancelOpeningCinematic()
        {
            if (!openingGroup) return;
            spawnLastTick = Time.realtimeSinceStartupAsDouble;
            bool playing = OpeningPlaying;
            openingGroup.alpha = 0;
            openingGroup.blocksRaycasts = openingGroup.interactable = false;
            openingSkip.interactable = false;
            if (playing)
            {
                if (effectSource) effectSource.Stop();
                if (musicSource) musicSource.volume = openingMusicVolume;
            }
        }

        void OnApplicationPause(bool paused)
        {
            openingPaused = paused;
            SetOpeningAudioPaused(!openingFocused || openingPaused);
        }

        void SetOpeningAudioPaused(bool paused)
        {
            openingLastTick = Time.realtimeSinceStartupAsDouble;
            spawnLastTick = openingLastTick;
            if (!OpeningPlaying && !PortalSpawnPlaying) return;
            if (paused)
            {
                if (musicSource) musicSource.Pause();
                if (effectSource) effectSource.Pause();
            }
            else
            {
                if (musicSource) musicSource.UnPause();
                if (effectSource) effectSource.UnPause();
            }
        }
    }
}
