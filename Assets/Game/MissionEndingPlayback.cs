using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace VeinVanguard
{
    public sealed class MissionEndingPlayback : MonoBehaviour
    {
        Canvas root;
        RectTransform stage;
        VideoPlayer video;
        RawImage image;
        AudioSource audioSource;
        TMP_Text loading, restoration, report;
        GameObject buttons;
        Button firstButton;
        EndingMissionResult result;
        Action replay, menu, fallback;
        double lastTime;
        float stalled;
        bool active, paused, ended, started, applicationPaused;
        bool applicationFocused = true;
        public bool Playing => active;

        RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = min; r.anchorMax = max;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }
        TMP_Text Text(string name, Transform parent, Vector2 min, Vector2 max, float size)
        {
            var r = Rect(name, parent, min, max);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.fontSize = size; t.color = new Color(.92f,.98f,1);
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }
        void Build()
        {
            root = new GameObject("Mission Ending", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            root.transform.SetParent(transform, false);
            root.renderMode = RenderMode.ScreenSpaceOverlay; root.sortingOrder = 30000;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080);
            var curtain = root.gameObject.AddComponent<Image>(); curtain.color = Color.black;
            stage = Rect("Ending 16x9", root.transform, Vector2.zero, Vector2.one);
            var fit = stage.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 16f/9;
            var screen = Rect("Video", stage, Vector2.zero, Vector2.one);
            image = screen.gameObject.AddComponent<RawImage>(); image.raycastTarget = false;
            video = screen.gameObject.AddComponent<VideoPlayer>();
            video.playOnAwake = false; video.isLooping = false; video.waitForFirstFrame = true;
            video.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime; video.renderMode = VideoRenderMode.APIOnly;
            audioSource = screen.gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false;
            video.audioOutputMode = VideoAudioOutputMode.AudioSource; video.controlledAudioTrackCount = 1;
            video.SetTargetAudioSource(0, audioSource);
            video.errorReceived += OnError; video.loopPointReached += OnEnd;
            loading = Text("Preparing ending",stage,new Vector2(.15f,.43f),new Vector2(.85f,.57f),36);
            loading.alignment = TextAlignmentOptions.Center; loading.text = "MENYIAPKAN AKHIR MISI";
            var integrityPanel=Rect("Integrity panel",stage,new Vector2(.055f,.75f),new Vector2(.39f,.92f));
            integrityPanel.gameObject.AddComponent<Image>().color=new Color(.04f,.08f,.15f,.9f);
            restoration = Text("Actual integrity",integrityPanel,new Vector2(.06f,.08f),new Vector2(.94f,.94f),38);
            report = Text("Actual mission report",stage,new Vector2(.44f,.15f),new Vector2(.89f,.65f),34);
            var controls = Rect("Navigation",stage,new Vector2(.08f,.32f),new Vector2(.57f,.45f));
            buttons = controls.gameObject;
            MakeButton("MAIN LAGI",controls,0,.47f,() => replay?.Invoke());
            MakeButton("MENU UTAMA",controls,.53f,1,() => menu?.Invoke());
        }
        void MakeButton(string label,Transform parent,float left,float right,Action action)
        {
            var r=Rect(label,parent,new Vector2(left,0),new Vector2(right,1));
            var bg=r.gameObject.AddComponent<Image>(); bg.color=new Color(.07f,.19f,.27f);
            var b=r.gameObject.AddComponent<Button>(); b.targetGraphic=bg; b.onClick.AddListener(()=>action());
            var t=Text(label+" Label",r,Vector2.zero,Vector2.one,31); t.text=label; t.alignment=TextAlignmentOptions.Center;
            if (!firstButton) firstButton=b;
        }
        public void Begin(VideoClip clip,EndingMissionResult snapshot,float volume,Action onReplay,Action onMenu,Action onFailure)
        {
            StopPlayback();
            result=snapshot; replay=onReplay; menu=onMenu; fallback=onFailure;
            if (!root) Build();
            root.gameObject.SetActive(true); active=true; ended=false; paused=false; started=false;
            loading.gameObject.SetActive(true); report.gameObject.SetActive(false); restoration.transform.parent.gameObject.SetActive(false);
            buttons.SetActive(false); image.enabled=false;
            lastTime=-1; stalled=0;
            audioSource.volume=volume; video.clip=clip; video.Prepare();
        }
        void Update()
        {
            if (!active || paused) return;
            if (!ended)
            {
                if (video.isPrepared && !started) { started=true; video.Play(); }
                stalled = Math.Abs(video.time-lastTime) < .0001 ? stalled+Time.unscaledDeltaTime : 0;
                lastTime=video.time;
                if (stalled>8) { Fail(); return; }
                if (video.texture && video.frame>=0)
                {
                    image.texture=video.texture; image.enabled=true; loading.gameObject.SetActive(false);
                }
            }
            double time=ended?30:video.time;
            bool restoring=time>=4 && time<10;
            restoration.transform.parent.gameObject.SetActive(restoring);
            if(restoring)
            {
                float p=Mathf.SmoothStep(0,1,Mathf.InverseLerp(4.4f,9.5f,(float)time));
                restoration.text=$"INTEGRITAS SISTEM\n{Mathf.RoundToInt(result.hp*p)}%";
            }
            bool showingReport=time>=14 && time<20;
            report.gameObject.SetActive(showingReport);
            if(showingReport)
            {
                float p=Mathf.SmoothStep(0,1,Mathf.InverseLerp(14.8f,18.2f,(float)time));
                int Count(int value)=>Mathf.RoundToInt(value*p);
                report.text=$"STATUS MISI · SELESAI\n\nPATOGEN DINETRALISIR   {Count(result.defeated.Length)}\n\nSISA INTEGRITAS   {Count(result.hp)}/100\n\nKUIS BENAR   {Count(result.correct)}/{result.answered}\n\nSTAGE SELESAI   {Count(2)}\n\nHASIL AKHIR · BERHASIL";
            }
            bool showButtons=time>=26;
            if(buttons.activeSelf!=showButtons)
            {
                buttons.SetActive(showButtons);
                if(showButtons && EventSystem.current)EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
            }
        }
        void OnEnd(VideoPlayer _) { ended=true; }
        void OnError(VideoPlayer _,string message) { Debug.LogWarning("Ending video: "+message); Fail(); }
        void Fail() { var action=fallback; StopPlayback(); action?.Invoke(); }
        public void StopPlayback()
        {
            active=false;
            if(video) {video.Stop(); video.clip=null;}
            if(image) {image.texture=null;image.enabled=false;}
            if(root)root.gameObject.SetActive(false);
            replay=menu=fallback=null;
        }
        void OnApplicationPause(bool value) { applicationPaused=value; SetPaused(applicationPaused || !applicationFocused); }
        void OnApplicationFocus(bool focused) { applicationFocused=focused; SetPaused(applicationPaused || !applicationFocused); }
        void SetPaused(bool value)
        {
            paused=value; stalled=0;
            if(!active || !video || !video.isPrepared || ended)return;
            if(value)video.Pause(); else video.Play();
        }
        void OnDisable() { StopPlayback(); }
        void OnDestroy()
        {
            if(video){video.errorReceived-=OnError;video.loopPointReached-=OnEnd;}
            if(root)Destroy(root.gameObject);
        }
    }
}
