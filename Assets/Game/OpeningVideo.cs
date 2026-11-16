using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace VeinVanguard
{
    public partial class BattleManager
    {
        VideoPlayer openingVideo;
        AudioSource openingVideoAudio;
        RawImage openingVideoImage;
        AspectRatioFitter openingVideoAspect;
        bool openingVideoActive;
        long openingVideoFrame;
        float openingVideoIdle;

        bool TryBeginOpeningVideo()
        {
            if (openingReducedMotion) return false;
            var clip = Resources.Load<VideoClip>("VeinVanguardOpeningV2");
            if (!clip) return false;

            if (!openingVideo)
            {
                var rect = Box("Opening V2 Video", openingGroup.transform, Vector2.zero, Vector2.one);
                rect.SetSiblingIndex(openingCurtain.transform.GetSiblingIndex());
                openingVideoImage = rect.gameObject.AddComponent<RawImage>();
                openingVideoImage.raycastTarget = false;
                openingVideoImage.enabled = false;
                openingVideoAspect = rect.gameObject.AddComponent<AspectRatioFitter>();
                openingVideoAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                openingVideo = rect.gameObject.AddComponent<VideoPlayer>();
                openingVideo.playOnAwake = false;
                openingVideo.renderMode = VideoRenderMode.APIOnly;
                openingVideo.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
                openingVideoAudio = rect.gameObject.AddComponent<AudioSource>();
                openingVideoAudio.playOnAwake = false;
                openingVideo.audioOutputMode = VideoAudioOutputMode.AudioSource;
                openingVideo.controlledAudioTrackCount = 1;
                openingVideo.SetTargetAudioSource(0, openingVideoAudio);
                openingVideo.waitForFirstFrame = true;
                openingVideo.errorReceived += (_, message) => FallBackFromOpeningVideo(message);
                openingVideo.loopPointReached += _ => { if (openingVideoActive) CompleteOpeningCinematic(); };
            }

            openingVideoActive = true;
            openingVideoFrame = -1;
            openingVideoIdle = 0;
            openingStoryContent.alpha = 0;
            openingCurtain.color = Color.black;
            openingGroup.GetComponent<Image>().color = Color.black;
            if (musicSource) musicSource.Stop();
            if (effectSource) effectSource.Stop();
            // The Remotion export has one mixed track. Use the music setting for that mix.
            openingVideoAudio.volume = openingMusicVolume;
            openingVideo.clip = clip;
            openingVideoAspect.aspectRatio = (float)clip.width / clip.height;
            openingVideo.Prepare();
            return true;
        }

        void TickOpeningVideo(float delta)
        {
            if (openingVideo.isPrepared && !openingVideo.isPlaying) openingVideo.Play();

            long frame = openingVideo.frame;
            openingVideoIdle = frame == openingVideoFrame ? openingVideoIdle + delta : 0;
            openingVideoFrame = frame;
            if (openingVideoIdle >= 5)
            {
                FallBackFromOpeningVideo("Decoder did not advance.");
                return;
            }
            if (frame < 0 || !openingVideo.texture) return;

            openingTime = (float)openingVideo.time;
            openingVideoImage.texture = openingVideo.texture;
            openingVideoImage.enabled = true;
            float remaining = (float)(openingVideo.clip.length - openingVideo.time);
            if (remaining <= .25f) PrepareOpeningMap();
            openingCurtain.color = Color.black;
            openingCurtain.enabled = false;
            float alpha = Mathf.SmoothStep(0, 1, remaining / .22f);
            openingVideoImage.color = new Color(1, 1, 1, alpha);
            openingVideoAudio.volume = openingMusicVolume * alpha;
            openingGroup.GetComponent<Image>().color = new Color(0, 0, 0, alpha);
        }

        void FallBackFromOpeningVideo(string reason)
        {
            if (!openingVideoActive) return;
            Debug.LogWarning("Opening V2: " + reason);
            if (openingMapPrepared) { CompleteOpeningCinematic(); return; }
            StopOpeningVideo();
            openingTime = 0;
            BeginNativeOpening();
        }

        void StopOpeningVideo()
        {
            if (!openingVideoActive) return;
            openingVideoActive = false;
            openingVideoImage.enabled = false;
            openingVideoImage.texture = null;
            openingVideo.Stop();
            openingVideo.clip = null;
            openingGroup.GetComponent<Image>().color = Color.clear;
            openingCurtain.enabled = true;
        }
    }
}
