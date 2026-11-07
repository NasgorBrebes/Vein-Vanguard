using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace VeinVanguard
{
    public enum GameView { Title, World, Battle }

    public partial class BattleManager
    {
        public Sprite titleScreen, worldMap;
        public Texture2D[] hudTextures;
        public Rect[] hudCrops;
        public Sprite[] hudSprites;
        public AudioClip mainMusic, mapMusic, battleMusic, clickSound, attackSound, enemyAttackSound,
            chargeSound, defendSound, enemyDefendSound, victorySound, loseSound;
        public GameView View { get; private set; }
        public Vector2 WorldPosition => worldPosition;
        public bool[] defeated = new bool[6];
        public bool SettingsOpen => overlay.gameObject.activeSelf;
        public float MusicVolume => musicSource.volume;
        public float SfxVolume => effectSource.volume;
        public int StageIndex { get; private set; } = 0;
        public bool CanBeginEncounter(int id)
        {
            if (View != GameView.World || SettingsOpen || actionAnimating || (modal != null && modal.gameObject.activeInHierarchy)) return false;
            if (id < 0 || id >= EncounterCatalog.Count) return false;
            if (id < defeated.Length && defeated[id]) return false;

            if (StageIndex == 0)
            {
                if (id > 3) return false;
            }
            else if (StageIndex == 1)
            {
                if (id < 4 || id > 5) return false;
                if (id == 5 && (defeated.Length <= 4 || !defeated[4])) return false;
            }
            else return false;

            if (id == 0 && (enemyOneFrames == null || enemyOneFrames.Length == 0)) return false;
            if (id == 1 && (enemyTwoFrames == null || enemyTwoFrames.Length == 0)) return false;
            if (id >= 2 && (additionalEnemyTextures == null || id - 2 >= additionalEnemyTextures.Length || additionalEnemyTextures[id - 2] == null)) return false;

            return true;
        }

        public bool TryAdvanceStage()
        {
            if (View != GameView.World || SettingsOpen || actionAnimating || (modal != null && modal.gameObject.activeSelf)) return false;
            if (StageIndex != 0) return false;
            if (defeated == null || defeated.Length < 4) return false;
            if (!defeated[0] || !defeated[1] || !defeated[2] || !defeated[3]) return false;

            StageIndex = 1;

            if (secondWorldMap != null && worldContent != null)
            {
                var img = worldContent.GetComponent<UnityEngine.UI.Image>();
                if (img) img.sprite = secondWorldMap;
            }

            worldPosition = new Vector2(0.08f, 0.48f);
            targetPosition = worldPosition;
            hasTarget = false;
            heldDirection = Vector2.zero;

            if (worldEnemies != null)
            {
                if (worldEnemies.Length > 0 && worldEnemies[0])
                {
                    enemyPositions[0] = enemyHomes[0] = enemyWanderTargets[0] = EncounterPosition(4);
                    worldEnemies[0].anchoredPosition = MapPoint(enemyPositions[0]);
                    var label = worldEnemies[0].GetComponentInChildren<TextMeshProUGUI>();
                    if (label) label.text = EncounterCatalog.Title(4);
                    if (worldEnemyImages != null && worldEnemyImages.Length > 0 && worldEnemyImages[0])
                        worldEnemyImages[0].sprite = EncounterSprite(4);
                    worldEnemies[0].gameObject.SetActive(!defeated[4]);
                }
                if (worldEnemies.Length > 1 && worldEnemies[1])
                {
                    enemyPositions[1] = enemyHomes[1] = enemyWanderTargets[1] = EncounterPosition(5);
                    worldEnemies[1].anchoredPosition = MapPoint(enemyPositions[1]);
                    var label = worldEnemies[1].GetComponentInChildren<TextMeshProUGUI>();
                    if (label) label.text = EncounterCatalog.Title(5);
                    if (worldEnemyImages != null && worldEnemyImages.Length > 1 && worldEnemyImages[1])
                        worldEnemyImages[1].sprite = EncounterSprite(5);
                    worldEnemies[1].gameObject.SetActive(defeated[4] && !defeated[5]);
                }
                if (worldEnemies.Length > 2 && worldEnemies[2]) worldEnemies[2].gameObject.SetActive(false);
                if (worldEnemies.Length > 3 && worldEnemies[3]) worldEnemies[3].gameObject.SetActive(false);
            }

            if (stageExitButton) stageExitButton.interactable = false;

            PositionWorld();
            return true;
        }

        private int[] StageEncounterIds(int stage)
        {
            if (stage == 0) return new[] { 0, 1, 2, 3 };
            if (stage == 1) return new[] { 4, 5 };
            return Array.Empty<int>();
        }

        public Vector2 EncounterPosition(int id)
        {
            return id switch
            {
                0 => new Vector2(0.75f, 0.15f),
                1 => new Vector2(0.31f, 0.15f),
                2 => new Vector2(0.75f, 0.40f),
                3 => new Vector2(0.34f, 0.50f),
                4 => new Vector2(0.77f, 0.33f),
                5 => new Vector2(0.85f, 0.64f),
                _ => Vector2.zero
            };
        }

        struct RouteSegment
        {
            public Vector2 a, b;
            public float halfWidth;
            public RouteSegment(Vector2 a, Vector2 b, float hw) { this.a = a; this.b = b; this.halfWidth = hw; }
        }

        static readonly Vector2 MapCanvasSize = new Vector2(2400f, 1697f);

        static readonly RouteSegment[] Stage1Route = {
            new RouteSegment(new Vector2(0.08f, 0.48f), new Vector2(0.12f, 0.53f), 30f),
            new RouteSegment(new Vector2(0.12f, 0.53f), new Vector2(0.16f, 0.58f), 35f),
            new RouteSegment(new Vector2(0.16f, 0.58f), new Vector2(0.22f, 0.62f), 40f),
            new RouteSegment(new Vector2(0.22f, 0.62f), new Vector2(0.30f, 0.63f), 50f),

            new RouteSegment(new Vector2(0.30f, 0.63f), new Vector2(0.24f, 0.54f), 35f),
            new RouteSegment(new Vector2(0.24f, 0.54f), new Vector2(0.19f, 0.46f), 30f),
            new RouteSegment(new Vector2(0.19f, 0.46f), new Vector2(0.18f, 0.44f), 25f),
            new RouteSegment(new Vector2(0.18f, 0.44f), new Vector2(0.28f, 0.43f), 25f),
            new RouteSegment(new Vector2(0.28f, 0.43f), new Vector2(0.38f, 0.41f), 25f),
            new RouteSegment(new Vector2(0.38f, 0.41f), new Vector2(0.51f, 0.38f), 25f),
            new RouteSegment(new Vector2(0.51f, 0.38f), new Vector2(0.64f, 0.35f), 45f),
            new RouteSegment(new Vector2(0.64f, 0.35f), new Vector2(0.77f, 0.33f), 45f),

            new RouteSegment(new Vector2(0.30f, 0.63f), new Vector2(0.36f, 0.65f), 40f),
            new RouteSegment(new Vector2(0.36f, 0.65f), new Vector2(0.42f, 0.66f), 30f),
            new RouteSegment(new Vector2(0.42f, 0.66f), new Vector2(0.52f, 0.69f), 25f),
            new RouteSegment(new Vector2(0.52f, 0.69f), new Vector2(0.64f, 0.70f), 25f),
            new RouteSegment(new Vector2(0.64f, 0.70f), new Vector2(0.74f, 0.68f), 25f),
            new RouteSegment(new Vector2(0.74f, 0.68f), new Vector2(0.79f, 0.66f), 40f),
            new RouteSegment(new Vector2(0.79f, 0.66f), new Vector2(0.85f, 0.64f), 50f)
        };
        [SerializeField] GameObject battleCanvas;
        [SerializeField] RectTransform flowSafe, titleRoot, worldRoot, worldContent, worldPlayer, overlay, overlayContent;
        [SerializeField] UnityEngine.UI.Image worldPlayerImage;
        [SerializeField] RectTransform[] worldEnemies = new RectTransform[4];
        [SerializeField] UnityEngine.UI.Image[] worldEnemyImages = new UnityEngine.UI.Image[4];
        [SerializeField] RectTransform stageExit;
        [SerializeField] UnityEngine.UI.Button stageExitButton;
        [SerializeField] TextMeshProUGUI worldStatus;
        public Sprite joystickDisc;
        [SerializeField] ExplorationJoystick joystick;
        Sprite[] hud;
        Sprite[] walkFrames;
        int walkRow;
        float walkTime, idleTime;
        bool walking;
        void PrepareWalkFrames()
        {
            if(walkFrames!=null)return;
            var master=Resources.Load<Texture2D>("VeinVanguard/MC_Walk_Master");
            var idle=Resources.Load<Texture2D>("VeinVanguard/IdleFront");
            if(!master||!idle){Debug.LogError("Missing exploration master or idle sheet");return;}
            // Measured bounds from the approved preview; source coordinates are top-down.
            var crops=new[]{new[]{new[]{92f,15f,118f,199f,58.5f},new[]{272f,15f,123f,199f,64f},new[]{449f,15f,125f,199f,65.5f},new[]{645f,15f,118f,199f,59.5f},new[]{831f,15f,119f,199f,59.5f},new[]{1005f,15f,126f,200f,67f},new[]{1190f,15f,125f,200f,64f},new[]{1377f,15f,122f,200f,63f}},new[]{new[]{76f,220f,140f,192f,73.5f},new[]{264f,221f,138f,192f,71f},new[]{443f,222f,145f,192f,74.5f},new[]{633f,221f,135f,191f,74f},new[]{814f,220f,138f,192f,71.5f},new[]{1004f,220f,133f,194f,66.5f},new[]{1185f,222f,143f,192f,72.5f},new[]{1370f,221f,132f,191f,70.5f}},new[]{new[]{78f,420f,139f,190f,77f},new[]{266f,420f,138f,191f,72f},new[]{446f,420f,142f,191f,75f},new[]{639f,420f,128f,191f,66.5f},new[]{819f,420f,139f,190f,74.5f},new[]{1008f,419f,127f,193f,65f},new[]{1190f,420f,138f,191f,71.5f},new[]{1375f,419f,133f,194f,70f}},new[]{new[]{74f,613f,140f,191f,75.5f},new[]{261f,613f,140f,191f,75f},new[]{441f,616f,144f,189f,78.5f},new[]{642f,615f,131f,190f,66.5f},new[]{813f,615f,141f,190f,76.5f},new[]{998f,613f,142f,192f,76f},new[]{1187f,616f,141f,189f,75f},new[]{1372f,615f,141f,191f,75f}},new[]{new[]{76f,809f,134f,179f,74f},new[]{264f,807f,136f,183f,71.5f},new[]{444f,809f,145f,180f,75.5f},new[]{639f,808f,129f,182f,69.5f},new[]{817f,809f,142f,180f,75.5f},new[]{1006f,808f,130f,182f,70f},new[]{1188f,808f,140f,181f,72f},new[]{1375f,808f,133f,182f,71f}}};
            int[] rows={1,0,0,2,3,3,4,4};
            walkFrames=new Sprite[72];
            for(int group=0;group<8;group++)for(int frame=0;frame<8;frame++)
            {
                var c=crops[rows[group]][frame];
                walkFrames[group*8+frame]=Sprite.Create(master,new Rect(c[0],master.height-c[1]-c[3],c[2],c[3]),new Vector2(c[4]/c[2],0),100,0,SpriteMeshType.FullRect);
            }
            float w=idle.width/4f,h=idle.height/2f;
            for(int row=0;row<2;row++)for(int col=0;col<4;col++)
                walkFrames[64+row*4+col]=Sprite.Create(idle,new Rect(col*w,(1-row)*h,w,h),new Vector2(.5f,0),100,0,SpriteMeshType.FullRect);
        }
        [SerializeField] AudioSource musicSource, effectSource;
        Vector2 worldPosition = new Vector2(.31f,.25f), targetPosition;
        bool hasTarget;
        Vector2 heldDirection;
        readonly Vector2 mapSize = new Vector2(2400,1697);
        // Bottom road: Golem right (0.75, 0.15), Slime left (0.31, 0.15); Rokok right curve (0.75, 0.40), Burger left curve (0.34, 0.50).
        readonly Vector2[] enemyPositions = {new Vector2(.75f,.15f),new Vector2(.31f,.15f),new Vector2(.75f,.40f),new Vector2(.34f,.50f)};
        readonly Vector2[] enemyHomes = {new Vector2(.75f,.15f),new Vector2(.31f,.15f),new Vector2(.75f,.40f),new Vector2(.34f,.50f)};
        readonly Vector2[] enemyWanderTargets = {new Vector2(.75f,.15f),new Vector2(.31f,.15f),new Vector2(.75f,.40f),new Vector2(.34f,.50f)};
        readonly float[] enemyWanderClock = {0, 0, 0, 0};
        const float EncounterRadius = 55;
        void PreparePresentation()
        {
            if(!worldMap)worldMap=Resources.Load<Sprite>("map_2");
            PrepareWalkFrames();
            hud=new Sprite[hudSprites!=null&&hudSprites.Length>0?hudSprites.Length:hudTextures?.Length??0];
            for(int i=0;i<hud.Length;i++)
            {
                if(hudSprites!=null&&i<hudSprites.Length&&hudSprites[i]){hud[i]=hudSprites[i];continue;}
                if(hudTextures==null||i>=hudTextures.Length||!hudTextures[i])continue;
                Rect rect=hudCrops[i];
                Vector4 border = i<=5 ? new Vector4(rect.width*.18f,rect.height*.3f,rect.width*.18f,rect.height*.3f) : Vector4.zero;
                hud[i]=Sprite.Create(hudTextures[i],rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,border);
            }
            if(!musicSource)musicSource=gameObject.AddComponent<AudioSource>();musicSource.loop=true;musicSource.ignoreListenerPause=true;musicSource.playOnAwake=false;musicSource.spatialBlend=0;
            if(!effectSource)effectSource=gameObject.AddComponent<AudioSource>();effectSource.playOnAwake=false;effectSource.spatialBlend=0;
            musicSource.volume=PlayerPrefs.GetFloat("VV.Music",.45f);if(musicSource.volume<=0f)musicSource.volume=.45f;if(!mainMusic)mainMusic=Resources.Load<AudioClip>("music");if(!mainMusic)Debug.LogError("Menu music clip is not assigned.");effectSource.volume=PlayerPrefs.GetFloat("VV.SFX",.75f);
        }
        void OnDestroy() { CleanupRuntimeSprites(); if(hud!=null&&hudSprites==null)foreach(var s in hud)if(s)Destroy(s);if(walkFrames!=null)foreach(var s in walkFrames)if(s)Destroy(s);Time.timeScale=1; }
        void OnApplicationFocus(bool focus) { if(!focus){if(joystick)joystick.ResetInput();heldDirection=Vector2.zero;hasTarget=false;} }
        void PlayMusic(AudioClip clip)
        {
            if(musicSource.clip==clip&&musicSource.isPlaying)return;
            musicSource.Stop();musicSource.clip=clip;if(clip)musicSource.Play();
        }
        void PlayEffect(AudioClip clip) { if(clip&&effectSource)effectSource.PlayOneShot(clip); }
        public void SetMusicVolume(float value){musicSource.volume=Mathf.Clamp01(value);PlayerPrefs.SetFloat("VV.Music",musicSource.volume);}
        public void SetSfxVolume(float value){effectSource.volume=Mathf.Clamp01(value);PlayerPrefs.SetFloat("VV.SFX",effectSource.volume);}
        void Skin(UnityEngine.UI.Image im,int index,bool sliced=true)
        {
            if(hud==null||index>=hud.Length||!hud[index])return;
            im.sprite=hud[index];im.color=Color.white;im.type=sliced?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;
            im.pixelsPerUnitMultiplier=3;
        }
        void PlainPanel(UnityEngine.UI.Image image,Color color)
        {
            image.sprite=null;
            image.type=UnityEngine.UI.Image.Type.Simple;
            image.color=color;
            image.raycastTarget=false;
        }
        void ButtonFrame(RectTransform panel)
        {
            InformationPanel(panel);
        }
        void MedicalButton(UnityEngine.UI.Button b,int icon=-1)
        {
            StyleButton(b);
            var label=b.GetComponentInChildren<TextMeshProUGUI>();label.fontStyle=FontStyles.Bold;label.color=Paper;
            label.alignment=TextAlignmentOptions.Center;label.margin=new Vector4(5,2,5,2);
            label.rectTransform.anchorMin=new Vector2(.10f,.16f);label.rectTransform.anchorMax=new Vector2(.90f,.84f);
            label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
            if(icon>=0)
            {
                var iconRect=b.transform.Find("Action Icon") as RectTransform;
                if(!iconRect)iconRect=Box("Action Icon",b.transform,new Vector2(.035f,.15f),new Vector2(.22f,.85f),Color.white);
                var image=iconRect.GetComponent<UnityEngine.UI.Image>();Skin(image,icon,false);image.preserveAspect=true;image.raycastTarget=false;
                label.rectTransform.anchorMin=new Vector2(.24f,.15f);label.rectTransform.anchorMax=new Vector2(.91f,.85f);
            }
        }
        void ApplyMedicalHUD(RectTransform head,RectTransform left,RectTransform right,RectTransform bottom)
        {
            head.GetComponent<UnityEngine.UI.Image>().color=Color.clear;
            ButtonFrame(left);ButtonFrame(right);
            playerStats.color=enemyStats.color=navy;playerStats.fontStyle=enemyStats.fontStyle=FontStyles.Bold;
            playerStats.rectTransform.anchorMin=new Vector2(.10f,.44f);playerStats.rectTransform.anchorMax=new Vector2(.90f,.87f);
            enemyStats.rectTransform.anchorMin=new Vector2(.10f,.44f);enemyStats.rectTransform.anchorMax=new Vector2(.90f,.87f);
            playerStats.alignment=enemyStats.alignment=TextAlignmentOptions.MidlineLeft;
            playerStats.fontSizeMax=enemyStats.fontSizeMax=21;playerStats.fontSizeMin=enemyStats.fontSizeMin=18;
            playerStats.margin=enemyStats.margin=new Vector4(2,0,2,0);
            status.color=Color.white;status.fontStyle=FontStyles.Bold;
            Skin(hpBar.transform.parent.GetComponent<UnityEngine.UI.Image>(),5);
            Skin(mpBar.transform.parent.GetComponent<UnityEngine.UI.Image>(),5);
            Skin(enemyBar.transform.parent.GetComponent<UnityEngine.UI.Image>(),5);
            Skin(hpBar,6,false);Skin(mpBar,7,false);Skin(enemyBar,6,false);
            foreach(var fill in new[]{hpBar,mpBar,enemyBar}){fill.rectTransform.offsetMin=new Vector2(4,2);fill.rectTransform.offsetMax=new Vector2(-4,-2);}
            bottom.GetComponent<UnityEngine.UI.Image>().color=Color.clear;
            var logPanel=Box("Message Panel",bottom,new Vector2(.12f,.60f),new Vector2(.98f,.98f),Color.white);ButtonFrame(logPanel);
            log.transform.SetParent(logPanel,false);log.rectTransform.anchorMin=new Vector2(.03f,.08f);log.rectTransform.anchorMax=new Vector2(.97f,.92f);log.color=navy;log.fontStyle=FontStyles.Bold;
            log.alignment=TextAlignmentOptions.MidlineLeft;log.fontSizeMax=22;log.fontSizeMin=18;
            log.rectTransform.offsetMin=log.rectTransform.offsetMax=Vector2.zero;
            diagnose.GetComponent<RectTransform>().anchorMin=new Vector2(.13f,.03f);diagnose.GetComponent<RectTransform>().anchorMax=new Vector2(.40f,.57f);
            synthesize.GetComponent<RectTransform>().anchorMin=new Vector2(.42f,.03f);synthesize.GetComponent<RectTransform>().anchorMax=new Vector2(.69f,.57f);
            restore.GetComponent<RectTransform>().anchorMin=new Vector2(.71f,.03f);restore.GetComponent<RectTransform>().anchorMax=new Vector2(.98f,.57f);
            MedicalButton(diagnose,8);MedicalButton(synthesize,9);MedicalButton(restore,10);
            diagnose.GetComponentInChildren<TextMeshProUGUI>().text="DIAGNOSE\n<size=65%>Satu scan · ikon A/B</size>";
            synthesize.GetComponentInChildren<TextMeshProUGUI>().text="SYNTHESIZE\n<size=65%>Fusi plasma · 20 MP</size>";
            restore.GetComponentInChildren<TextMeshProUGUI>().text="RESTORE\n<size=65%>Isi energi lewat kuis</size>";
            var shield=Box("Homeostasis Icon",bottom,new Vector2(.005f,.10f),new Vector2(.11f,.73f),Color.white).GetComponent<UnityEngine.UI.Image>();Skin(shield,11,false);shield.preserveAspect=true;
            Label("HOMEOSTASIS",bottom,new Vector2(0,0),new Vector2(.12f,.14f),16,Color.white,TextAlignmentOptions.Center);
            EnsureHomeostasisButton();
        }
        void BuildFlow()
        {
            var go=new GameObject("World and Menus",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));go.transform.SetParent(transform,false);
            var c=go.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=20;
            var scale=go.GetComponent<UnityEngine.UI.CanvasScaler>();scale.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1600,900);scale.matchWidthOrHeight=.5f;
            flowSafe=Box("Safe Area",go.transform,Vector2.zero,Vector2.one);
            titleRoot=Box("Title Screen",flowSafe,Vector2.zero,Vector2.one,new Color(.98f,.80f,.48f));
            var art=Box("Title Artwork",titleRoot,Vector2.zero,Vector2.one,Color.white).GetComponent<UnityEngine.UI.Image>();art.sprite=titleScreen;art.preserveAspect=true;
            var menu=Box("Main Menu Overlay",titleRoot,new Vector2(.38f,.035f),new Vector2(.62f,.285f));
            MedicalButton(Button("START",menu,new Vector2(0,.70f),Vector2.one,StartGame));
            MedicalButton(Button("SETTINGS",menu,new Vector2(0,.35f),new Vector2(1,.65f),OpenSettings));
            MedicalButton(Button("CREDITS",menu,Vector2.zero,new Vector2(1,.30f),OpenCredits));
            worldRoot=Box("Exploration",flowSafe,Vector2.zero,Vector2.one,navy);worldRoot.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            worldContent=Box("Map 1",worldRoot,Vector2.zero,Vector2.zero,Color.white);worldContent.pivot=Vector2.zero;worldContent.sizeDelta=mapSize;
            worldContent.GetComponent<UnityEngine.UI.Image>().sprite=worldMap;worldContent.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            worldContent.gameObject.AddComponent<WorldMapClick>().clicked=screen=>{
                if(View!=GameView.World||SettingsOpen)return;
                if(RectTransformUtility.ScreenPointToLocalPointInRectangle(worldContent,screen,null,out Vector2 local)){
                    Vector2 target=local/mapSize;if(IsWalkable(StageIndex,target)&&CanTraverse(StageIndex,worldPosition,target)){targetPosition=target;hasTarget=true;}
                }
            };
            worldPlayer=MapActor("Nanobot",worldContent,new Vector2(130,150));worldPlayerImage=worldPlayer.GetComponent<UnityEngine.UI.Image>();
            for(int i=0;i<4;i++)
            {
                worldEnemies[i]=MapActor("Enemy "+(i+1),worldContent,new Vector2(145,155));worldEnemyImages[i]=worldEnemies[i].GetComponent<UnityEngine.UI.Image>();
                worldEnemies[i].anchoredPosition=MapPoint(enemyPositions[i]);
                Label(EncounterCatalog.Title(i),worldEnemies[i],new Vector2(-.25f,1),new Vector2(1.25f,1.22f),20,navy,TextAlignmentOptions.Center).fontStyle=FontStyles.Bold;
            }
            var header=Box("Exploration HUD",worldRoot,new Vector2(.02f,.87f),new Vector2(.98f,.98f),new Color(navy.r,navy.g,navy.b,.94f));header.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            worldStatus=Label("",header,new Vector2(.02f,.1f),new Vector2(.56f,.9f),24,Color.white);
            var exitBtn=Button("STAGE 2 >",header,new Vector2(.58f,.16f),new Vector2(.71f,.84f),OnStageExitClicked);
            exitBtn.name="Stage Exit";stageExit=exitBtn.GetComponent<RectTransform>();stageExitButton=exitBtn;MedicalButton(exitBtn);
            MedicalButton(Button("SETTINGS",header,new Vector2(.72f,.16f),new Vector2(.85f,.84f),OpenSettings));
            MedicalButton(Button("MENU",header,new Vector2(.86f,.16f),new Vector2(.99f,.84f),ShowMenu));
            var hint=Box("Movement Hint",worldRoot,new Vector2(.28f,.025f),new Vector2(.98f,.10f),new Color(navy.r,navy.g,navy.b,.92f));
            Label("Analog / WASD / panah • Ketuk daratan untuk bergerak • Dekati musuh untuk bertarung",hint,Vector2.zero,Vector2.one,21,Color.white,TextAlignmentOptions.Center);
            overlay=Box("Settings and Credits",flowSafe,Vector2.zero,Vector2.one,new Color(0,.025f,.05f,.85f));overlay.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            overlayContent=Box("Panel",overlay,new Vector2(.25f,.15f),new Vector2(.75f,.85f),Color.white);
            ButtonFrame(overlayContent);
            overlay.gameObject.SetActive(false);
            EnsureAnalogControl();
        }
        Vector2 MapPoint(Vector2 p)=>new Vector2(p.x*mapSize.x,p.y*mapSize.y);
        RectTransform MapActor(string name,Transform parent,Vector2 size)
        {
            var rt=Box(name,parent,Vector2.zero,Vector2.zero,Color.white);rt.pivot=new Vector2(.5f,0);rt.sizeDelta=size;rt.GetComponent<UnityEngine.UI.Image>().preserveAspect=true;return rt;
        }
        void EnsureAnalogControl()
        {
            var old=worldRoot.Find("Touch Controls");if(old)old.gameObject.SetActive(false);
            if(!joystick)
            {
                var rect=Box("Analog Joystick",worldRoot,Vector2.zero,Vector2.zero,new Color(.02f,.18f,.30f,.82f));
                rect.pivot=Vector2.zero;rect.anchoredPosition=new Vector2(40,40);rect.sizeDelta=new Vector2(180,180);
                var image=rect.GetComponent<UnityEngine.UI.Image>();image.sprite=joystickDisc;image.raycastTarget=true;
                var ring=Box("Medical Ring",rect,new Vector2(.06f,.06f),new Vector2(.94f,.94f),new Color(.15f,.78f,1f,.28f));
                ring.GetComponent<UnityEngine.UI.Image>().sprite=joystickDisc;
                var knob=Box("Knob",rect,Vector2.one*.5f,Vector2.one*.5f,mint);knob.sizeDelta=new Vector2(66,66);
                knob.GetComponent<UnityEngine.UI.Image>().sprite=joystickDisc;
                joystick=rect.gameObject.AddComponent<ExplorationJoystick>();joystick.knob=knob;
            }
            var analog=(RectTransform)joystick.transform;
            analog.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var medicalRing=analog.Find("Medical Ring").GetComponent<UnityEngine.UI.Image>();
            medicalRing.color=new Color(.04f,.70f,.90f,.22f);
            medicalRing.raycastTarget=false;
            joystick.knob.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            joystick.changed=value=>{heldDirection=View==GameView.World&&!SettingsOpen?value:Vector2.zero;if(value!=Vector2.zero)hasTarget=false;};
            var hint=worldRoot.Find("Movement Hint");
            if(hint)hint.GetComponentInChildren<TextMeshProUGUI>().text="Analog / WASD / panah • Ketuk daratan untuk bergerak • Dekati musuh untuk bertarung";
        }
        void HandleMapClick(Vector2 screen)
        {
            if(View!=GameView.World||SettingsOpen)return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(worldContent,screen,null,out Vector2 local))
            {
                Vector2 target=local/mapSize;
                if(IsWalkable(StageIndex,target)&&CanTraverse(StageIndex,worldPosition,target))
                {
                    targetPosition=target;
                    hasTarget=true;
                }
            }
        }
        void WireBakedHierarchy()
        {
            if(worldContent)worldContent.GetComponent<UnityEngine.UI.Image>().sprite=worldMap;
            var mapClick=worldContent.GetComponent<WorldMapClick>();mapClick.clicked=HandleMapClick;
            foreach(var b in flowSafe.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                b.onClick.RemoveAllListeners();UnityEngine.Events.UnityAction action=null;
                if(b.name=="START")action=StartGame;
                else if(b.name=="CREDITS")action=OpenCredits;
                else if(b.name=="MENU")action=ShowMenu;
                else if(b.name=="SETTINGS")action=b.transform.IsChildOf(titleRoot)?OpenSettings:OpenSettings;
                if(action!=null)b.onClick.AddListener(()=>{PlayEffect(clickSound);action();});
            }
            EnsureAnalogControl();
            EnsureStageMapNodes();
            diagnose.onClick.RemoveAllListeners();diagnose.onClick.AddListener(()=>{PlayEffect(clickSound);Diagnose();});
            synthesize.onClick.RemoveAllListeners();synthesize.onClick.AddListener(()=>{PlayEffect(clickSound);Synthesize();});
            restore.onClick.RemoveAllListeners();restore.onClick.AddListener(()=>{PlayEffect(clickSound);Restore();});
            // The editor-baked scene needs the same dynamic diagnosis panel wiring as a fresh build.
            EnsureBattlePanels();
            homeostasis.onClick.RemoveAllListeners();homeostasis.onClick.AddListener(()=>{PlayEffect(clickSound);Homeostasis();});
            CleanBakedPanels();
        }
        void CleanBakedPanels()
        {
            var iceTint=new Color(.93f,.985f,1f,.97f);
            foreach(var path in new[]{"Header/Nanobot Status","Header/Pathogen Status","Message Panel"})
            {
                var panel=safe.Find(path);
                if(panel)ButtonFrame(panel as RectTransform);
            }
        }
        void EnsureStageMapNodes()
        {
            if (worldEnemies == null || worldEnemies.Length < 4)
            {
                var newEnemies = new RectTransform[4];
                if (worldEnemies != null)
                    for (int i = 0; i < Mathf.Min(worldEnemies.Length, 4); i++) newEnemies[i] = worldEnemies[i];
                worldEnemies = newEnemies;
            }
            if (worldEnemyImages == null || worldEnemyImages.Length < 4)
            {
                var newImages = new UnityEngine.UI.Image[4];
                if (worldEnemyImages != null)
                    for (int i = 0; i < Mathf.Min(worldEnemyImages.Length, 4); i++) newImages[i] = worldEnemyImages[i];
                worldEnemyImages = newImages;
            }
            if (!worldContent && transform)
            {
                var found = transform.Find("Game Screens/World Screen/World Viewport/World Map");
                if (found) worldContent = found as RectTransform;
            }
            if (!worldRoot && transform)
            {
                var found = transform.Find("Game Screens/World Screen");
                if (found) worldRoot = found as RectTransform;
            }
            if (worldContent)
            {
                for (int i = 0; i < 4; i++)
                {
                    string nodeName = "Enemy " + (i + 1);
                    var child = worldContent.Find(nodeName) as RectTransform;
                    if (!child)
                    {
                        child = MapActor(nodeName, worldContent, new Vector2(145, 155));
                        child.anchoredPosition = MapPoint(i < enemyPositions.Length ? enemyPositions[i] : new Vector2(.5f, .5f));
                        Label(EncounterCatalog.Title(i), child, new Vector2(-.25f, 1), new Vector2(1.25f, 1.22f), 20, navy, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
                    }
                    worldEnemies[i] = child;
                    worldEnemyImages[i] = child.GetComponent<UnityEngine.UI.Image>();
                    var label = child.GetComponentInChildren<TextMeshProUGUI>();
                    if (label) label.text = EncounterCatalog.Title(i);
                }
            }
            var header = worldRoot ? worldRoot.Find("Exploration HUD") as RectTransform : null;
            var exitChild = (header ? header.Find("Stage Exit") : null) ?? (worldRoot ? worldRoot.Find("Stage Exit") : null);
            if (!exitChild && (header || worldRoot))
            {
                var parent = worldRoot ? worldRoot : header;
                var btn = Button("STAGE 2 >", parent, new Vector2(.58f, .16f), new Vector2(.71f, .84f), OnStageExitClicked);
                btn.name = "Stage Exit";
                stageExit = btn.GetComponent<RectTransform>();
                stageExitButton = btn;
                MedicalButton(btn);
            }
            else if (exitChild)
            {
                stageExit = exitChild as RectTransform;
                stageExitButton = exitChild.GetComponent<UnityEngine.UI.Button>();
                if (stageExitButton)
                {
                    stageExitButton.onClick.RemoveAllListeners();
                    stageExitButton.onClick.AddListener(OnStageExitClicked);
                }
            }
        }
        void OnStageExitClicked()
        {
            TryAdvanceStage();
        }
        public void BakeHierarchyForEditor()
        {
            if(Application.isPlaying)return;
            if(hierarchyBaked){PreparePresentation();EnsureBattlePanels();EnsureAnalogControl();EnsureStageMapNodes();ApplyPresentation();return;}
            PreparePresentation();BuildUI();BuildFlow();hierarchyBaked=true;EnsureStageMapNodes();ApplyPresentation();
            battleCanvas.name="Battle UI";flowSafe.transform.parent.name="Game Screens";
            battleCanvas.SetActive(false);titleRoot.gameObject.SetActive(true);worldRoot.gameObject.SetActive(false);overlay.gameObject.SetActive(false);
            if(walkFrames!=null)worldPlayerImage.sprite=walkFrames[64];
            for(int i=0;i<4;i++)
            {
                if(i<2){var frames=i==0?enemyOneFrames:enemyTwoFrames;if(frames!=null&&frames.Length>0&&worldEnemyImages[i])worldEnemyImages[i].sprite=frames[0];}
                else {var s=EncounterSprite(i);if(s&&worldEnemyImages[i])worldEnemyImages[i].sprite=s;}
            }
        }
        void ShowTitle()
        {
            if(joystick)joystick.ResetInput();
            Time.timeScale=1;CloseOverlay();View=GameView.Title;model.phase=BattlePhase.Menu;
            battleCanvas.SetActive(false);titleRoot.gameObject.SetActive(true);worldRoot.gameObject.SetActive(false);PlayMusic(mainMusic);
        }
        void StartExploration()
        {
            ResetBattlePresentation();
            StopAllCoroutines();ClearVisualEffects();actionAnimating=false;walkRow=0;walkTime=0;model=new BattleModel();defeated=new bool[6];triviaDeck=new TriviaDeck();currentQuestion=null;
            ResetLearningState();
            StageIndex = 0;
            movementHintDismissed=false;if(movementHint)movementHint.alpha=1;
            worldPosition=new Vector2(.50f,.04f);
            targetPosition=worldPosition;hasTarget=false;heldDirection=Vector2.zero;
            if(worldContent)worldContent.GetComponent<UnityEngine.UI.Image>().sprite=worldMap;
            for(int i=0;i<enemyPositions.Length;i++)
            {
                enemyPositions[i]=EncounterPosition(i);
                enemyHomes[i]=enemyPositions[i];
                enemyWanderTargets[i]=enemyHomes[i];
                enemyWanderClock[i]=UnityEngine.Random.Range(.2f,1.2f);
                if(worldEnemies!=null&&i<worldEnemies.Length&&worldEnemies[i])
                {
                    worldEnemies[i].anchoredPosition=MapPoint(enemyPositions[i]);
                    var label=worldEnemies[i].GetComponentInChildren<TextMeshProUGUI>();
                    if(label)label.text=EncounterCatalog.Title(i);
                    if(worldEnemyImages!=null&&i<worldEnemyImages.Length&&worldEnemyImages[i])
                    {
                        if(i<2)
                        {
                            var frames=(i==0?enemyOneFrames:enemyTwoFrames);
                            if(frames!=null&&frames.Length>0)worldEnemyImages[i].sprite=frames[0];
                        }
                        else
                        {
                            var s=EncounterSprite(i);
                            if(s)worldEnemyImages[i].sprite=s;
                        }
                    }
                    worldEnemies[i].gameObject.SetActive(true);
                }
            }
            if(stageExitButton)stageExitButton.interactable=false;
            ReturnToWorld();
        }
        void ReturnToWorld()
        {
            StopAllCoroutines();ClearVisualEffects();Time.timeScale=1;CloseOverlay();View=GameView.World;model.phase=BattlePhase.Menu;
            HideLearningPanels();
            HideDialog();
            if(joystick)joystick.ResetInput();
            hasTarget=false;heldDirection=Vector2.zero;walking=false;walkTime=idleTime=0;actionAnimating=false;
            battleCanvas.SetActive(false);titleRoot.gameObject.SetActive(false);worldRoot.gameObject.SetActive(true);
            if(StageIndex==0)
            {
                for(int i=0;i<worldEnemies.Length;i++)if(worldEnemies[i])worldEnemies[i].gameObject.SetActive(i<4&&!defeated[i]);
                if(stageExitButton)stageExitButton.interactable=defeated[0]&&defeated[1]&&defeated[2]&&defeated[3];
            }
            else
            {
                if(worldEnemies.Length>0&&worldEnemies[0])worldEnemies[0].gameObject.SetActive(!defeated[4]);
                if(worldEnemies.Length>1&&worldEnemies[1])worldEnemies[1].gameObject.SetActive(defeated[4]&&!defeated[5]);
                for(int i=2;i<worldEnemies.Length;i++)if(worldEnemies[i])worldEnemies[i].gameObject.SetActive(false);
                if(stageExitButton)stageExitButton.interactable=false;
            }
            PlayMusic(mapMusic);PositionWorld();
        }
        public void BeginEncounter(int index)
        {
            if(!CanBeginEncounter(index))return;
            View=GameView.Battle;hasTarget=false;heldDirection=Vector2.zero;model.Begin(index);
            ResetLearningState();
            worldRoot.gameObject.SetActive(false);titleRoot.gameObject.SetActive(false);battleCanvas.SetActive(true);
            ConfigureEnemyBounds(index);
            RefreshBattleBackground();
            HideDialog();Pose("idle");log.text="Patogen mendekat! Gunakan Diagnose untuk memindai komposisinya.";Refresh();PlayMusic(battleMusic);
            RequestBattleFraming(BattleFraming.Idle);
        }
        void UpdateFlow()
        {
            RefreshRadialActions();
            Rect r=Screen.safeArea;flowSafe.anchorMin=new Vector2(r.xMin/Screen.width,r.yMin/Screen.height);flowSafe.anchorMax=new Vector2(r.xMax/Screen.width,r.yMax/Screen.height);
            UpdateUiNavigation();
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){if(SettingsOpen)CloseOverlay();else if(View!=GameView.Battle)OpenSettings();}
            if(View!=GameView.World||SettingsOpen)return;
            UpdateEnemyWander();
            walking=false;
            Vector2 direction=heldDirection;
            if(Keyboard.current!=null)
            {
                var k=Keyboard.current;
                direction+=new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0));
            }
            if(direction.sqrMagnitude>0){hasTarget=false;MoveWorld(Vector2.ClampMagnitude(direction,1)*250*Time.deltaTime);}
            else if(hasTarget){var delta=Vector2.Scale(targetPosition-worldPosition,mapSize);if(delta.magnitude<5)hasTarget=false;else MoveWorld(Vector2.ClampMagnitude(delta,250*Time.deltaTime));}
            if(walking)idleTime=0;else {walkTime=0;idleTime+=Time.deltaTime;}
            UpdateMovementHint();
            PositionWorld();
            int[] activeIds=StageEncounterIds(StageIndex);
            for(int i=0;i<activeIds.Length;i++)
            {
                int encId=activeIds[i];
                if(CanBeginEncounter(encId)&&Vector2.Scale(worldPosition-enemyPositions[i],mapSize).magnitude<EncounterRadius)
                {
                    BeginEncounter(encId);
                    break;
                }
            }
        }
        // Cream road centerline on map_2, in normalized map coordinates.
        static readonly Vector2[] Road = {
            new Vector2(.50f,.02f), new Vector2(.64f,.05f), new Vector2(.75f,.08f),
            new Vector2(.75f,.15f), new Vector2(.70f,.20f), new Vector2(.60f,.22f),
            new Vector2(.50f,.20f), new Vector2(.40f,.14f), new Vector2(.31f,.08f),
            new Vector2(.31f,.15f), new Vector2(.36f,.23f), new Vector2(.46f,.29f),
            new Vector2(.62f,.32f), new Vector2(.75f,.36f),
            new Vector2(.79f,.34f), new Vector2(.72f,.46f), new Vector2(.54f,.51f),
            new Vector2(.38f,.48f), new Vector2(.28f,.56f), new Vector2(.30f,.64f),
            new Vector2(.42f,.70f), new Vector2(.59f,.73f), new Vector2(.76f,.76f),
            new Vector2(.88f,.82f)
        };
        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab=b-a; float t=Vector2.Dot(p-a,ab)/Mathf.Max(.0001f,Vector2.Dot(ab,ab));
            return Vector2.Distance(p,Vector2.Lerp(a,b,Mathf.Clamp01(t)));
        }
        public static bool CanTraverse(Vector2 from, Vector2 to) => CanTraverse(0, from, to);
        public static bool CanTraverse(int stage, Vector2 from, Vector2 to)
        {
            if(stage!=0&&stage!=1)return false;
            if(float.IsNaN(from.x)||float.IsNaN(from.y)||float.IsNaN(to.x)||float.IsNaN(to.y))return false;
            if(float.IsInfinity(from.x)||float.IsInfinity(from.y)||float.IsInfinity(to.x)||float.IsInfinity(to.y))return false;

            if(!IsWalkable(stage,from)||!IsWalkable(stage,to))return false;

            Vector2 deltaPx=Vector2.Scale(to-from,MapCanvasSize);
            float distPx=deltaPx.magnitude;
            int steps=Mathf.Max(1,Mathf.CeilToInt(distPx/2f));
            for(int i=1;i<steps;i++)
            {
                Vector2 sample=Vector2.Lerp(from,to,(float)i/steps);
                if(!IsWalkable(stage,sample))return false;
            }
            return true;
        }
        void UpdateEnemyWander()
        {
            if(StageIndex!=0)return;
            for(int i=0;i<Mathf.Min(4,enemyPositions.Length);i++)
            {
                if(i<defeated.Length&&defeated[i])continue;
                enemyWanderClock[i]-=Time.deltaTime;
                if(enemyWanderClock[i]<=0)
                {
                    enemyWanderClock[i]=UnityEngine.Random.Range(1.4f,2.8f);
                    Vector2 candidate=enemyHomes[i]+UnityEngine.Random.insideUnitCircle*.025f;
                    enemyWanderTargets[i]=IsWalkable(0,candidate)?candidate:enemyHomes[i];
                }
                enemyPositions[i]=Vector2.MoveTowards(enemyPositions[i],enemyWanderTargets[i],Time.deltaTime*.035f);
            }
        }
        public static bool IsWalkable(Vector2 p) => IsWalkable(0, p);
        public static bool IsWalkable(int stage, Vector2 p)
        {
            if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y))return false;
            if(p.x<.02f||p.x>.98f||p.y<.01f||p.y>.98f)return false;
            if(stage==0)
            {
                for(int i=1;i<Road.Length;i++)if(SegmentDistance(p,Road[i-1],Road[i])<.065f)return true;
                return false;
            }
            if(stage==1)
            {
                Vector2 pCanvas=Vector2.Scale(p,MapCanvasSize);
                for(int i=0;i<Stage1Route.Length;i++)
                {
                    Vector2 aCanvas=Vector2.Scale(Stage1Route[i].a,MapCanvasSize);
                    Vector2 bCanvas=Vector2.Scale(Stage1Route[i].b,MapCanvasSize);
                    if(SegmentDistance(pCanvas,aCanvas,bCanvas)<=Stage1Route[i].halfWidth)return true;
                }
                return false;
            }
            return false;
        }
        public void MoveWorld(Vector2 deltaPixels)
        {
            if(View!=GameView.World||SettingsOpen)return;
            var delta=deltaPixels/mapSize;var original=worldPosition;
            var next=worldPosition+delta;
            if(IsWalkable(StageIndex,next)&&CanTraverse(StageIndex,original,next))worldPosition=next;
            var moved=Vector2.Scale(worldPosition-original,mapSize);
            walking=moved.sqrMagnitude>.0001f;
            if(walking)
            {
                int sector=(Mathf.RoundToInt(Mathf.Atan2(moved.y,moved.x)*Mathf.Rad2Deg/45)+8)%8;
                walkRow=sector switch {0=>1,1=>6,2=>3,3=>7,4=>2,5=>5,6=>0,_=>4};
                walkTime+=moved.magnitude/250f;
            }
            if((worldPosition-original).sqrMagnitude<.00000001f)hasTarget=false;
        }
        void PositionWorld()
        {
            worldPlayer.localScale=new Vector3(walking&&(walkRow==2||walkRow==5||walkRow==7)?-1:1,1,1);
            int frame=(int)(Time.unscaledTime*7)%4;
            if(walkFrames!=null)worldPlayerImage.sprite=walkFrames[walking?walkRow*8+(int)(walkTime*12)%8:64+(int)(idleTime*4)%8];
            else if(playerFrames!=null&&playerFrames.Length>0)worldPlayerImage.sprite=playerFrames[0];
            var pose=worldPlayerImage.sprite;
            worldPlayer.pivot=walking&&pose?new Vector2(pose.pivot.x/pose.rect.width,0):new Vector2(.5f,0);
            worldPlayer.sizeDelta=walking&&pose?new Vector2(110*pose.rect.width/pose.rect.height,110):new Vector2(130,150);
            worldPlayer.anchoredPosition=MapPoint(worldPosition);
            int[] activeIds=StageEncounterIds(StageIndex);
            for(int i=0;i<activeIds.Length;i++)
            {
                if(i>=enemyPositions.Length||!worldEnemies[i])continue;
                worldEnemies[i].anchoredPosition=MapPoint(enemyPositions[i]);
                int id=activeIds[i];
                if(id<2)worldEnemyImages[i].sprite=(id==0?enemyOneFrames:enemyTwoFrames)[frame];
                else {
                    var s=EncounterSprite(id);
                    if(s)worldEnemyImages[i].sprite=s;
                }
            }
            Vector2 viewport=worldRoot.rect.size;
            Vector2 center=MapPoint(worldPosition);
            worldContent.anchoredPosition=new Vector2(Mathf.Clamp(viewport.x*.5f-center.x,Mathf.Min(0,viewport.x-mapSize.x),0),Mathf.Clamp(viewport.y*.55f-center.y,Mathf.Min(0,viewport.y-mapSize.y),0));
            if(StageIndex==0)
            {
                int defeatedCount=0;for(int i=0;i<4;i++)if(i<defeated.Length&&defeated[i])defeatedCount++;
                worldStatus.text=(defeatedCount>=4)?"Area aman — 4 patogen telah dinetralisir. Lanjut ke Stage 2 via tombol exit.":$"Patogen dikalahkan: {defeatedCount}/4. Jelajahi jalan pembuluh darah.";
                if(stageExitButton)
                {
                    stageExitButton.gameObject.SetActive(true);
                    stageExitButton.interactable=(defeatedCount>=4);
                }
            }
            else
            {
                int defeatedCount=0;for(int i=4;i<6;i++)if(i<defeated.Length&&defeated[i])defeatedCount++;
                worldStatus.text=(defeatedCount>=2)?"Area aman — semua patogen dan Boss telah dikalahkan.":(!defeated[4])?"Patogen dikalahkan: 0/2. Netralisir Botol Alkohol sebelum menghadapi Boss.":"Patogen dikalahkan: 1/2. Hadapi Boss di pulau timur.";
                if(stageExitButton)
                {
                    stageExitButton.gameObject.SetActive(false);
                    stageExitButton.interactable=false;
                }
            }
            RefreshPresentationValues();
        }
        void ClearOverlay()
        {
            if(EventSystem.current)overlayReturnFocus=EventSystem.current.currentSelectedGameObject;
            if(joystick)joystick.ResetInput();
            heldDirection=Vector2.zero;hasTarget=false;overlay.gameObject.SetActive(true);
            var panelImage=overlayContent.GetComponent<UnityEngine.UI.Image>();
            if(panelImage)ButtonFrame(overlayContent);
            for(int i=overlayContent.childCount-1;i>=0;i--){var child=overlayContent.GetChild(i).gameObject;child.SetActive(false);Destroy(child);}
        }
        public void OpenCredits()
        {
            Fixed(overlayContent,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(460,510));
            ClearOverlay();Label("CREDITS",overlayContent,new Vector2(.08f,.78f),new Vector2(.92f,.94f),40,mint,TextAlignmentOptions.Center);
            Label("Dibuat oleh\n\nHilmi Aminuddien\nMohammad Ferry Irwansyah\nChristian Gideon\nSyachrul Ramadhan",overlayContent,new Vector2(.08f,.24f),new Vector2(.92f,.77f),30,Paper,TextAlignmentOptions.Center);
            MedicalButton(Button("KEMBALI",overlayContent,new Vector2(.25f,.06f),new Vector2(.75f,.19f),CloseOverlay));
            FocusOverlay();
        }
        public void OpenSettings()
        {
            Fixed(overlayContent,Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(460,410));
            ClearOverlay();Label("SETTINGS",overlayContent,new Vector2(.08f,.78f),new Vector2(.92f,.94f),40,mint,TextAlignmentOptions.Center);
            VolumeSlider("Musik",.61f,musicSource.volume,SetMusicVolume);
            VolumeSlider("Efek suara",.39f,effectSource.volume,SetSfxVolume);
            Label("Pengaturan volume disimpan di perangkat ini.",overlayContent,new Vector2(.06f,.21f),new Vector2(.94f,.31f),20,Paper,TextAlignmentOptions.Center);
            MedicalButton(Button("KEMBALI",overlayContent,new Vector2(.25f,.06f),new Vector2(.75f,.19f),CloseOverlay));
            FocusOverlay();
        }
        void VolumeSlider(string title,float y,float value,UnityEngine.Events.UnityAction<float> action)
        {
            var label=Label(title+"  "+Mathf.RoundToInt(value*100)+"%",overlayContent,new Vector2(.09f,y),new Vector2(.91f,y+.1f),26,Paper);
            var root=Box(title+" Volume",overlayContent,new Vector2(.1f,y-.10f),new Vector2(.90f,y+.01f),Color.clear);
            var slider=root.gameObject.AddComponent<UnityEngine.UI.Slider>();root.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var track=Box("Track",root,new Vector2(0,.32f),new Vector2(1,.68f),Ink);
            Shape(track.GetComponent<UnityEngine.UI.Image>(),Ink,8,1,Edge,false);
            var fill=Box("Fill",track,Vector2.zero,Vector2.one,mint);Shape(fill.GetComponent<UnityEngine.UI.Image>(),Cyan,8,0,null,false);
            // Slider stretches the handle across its travel area; zero height keeps the thumb circular.
            var handleArea=Box("Handle Area",root,new Vector2(.04f,.5f),new Vector2(.96f,.5f));
            var handle=Box("Handle",handleArea,new Vector2(0,.5f),new Vector2(1,.5f),Color.white);handle.sizeDelta=new Vector2(30,30);handle.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            Shape(handle.GetComponent<UnityEngine.UI.Image>(),Paper,15,3,Ink,false);
            slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<UnityEngine.UI.Image>();slider.minValue=0;slider.maxValue=1;slider.value=value;
            slider.transition=UnityEngine.UI.Selectable.Transition.None;
            var feedback=root.gameObject.AddComponent<UiControlFeedback>();feedback.surface=Paper;feedback.accent=Ink;
            slider.onValueChanged.AddListener(v=>{action(v);label.text=title+"  "+Mathf.RoundToInt(v*100)+"%";});
        }
        public void CloseOverlay()
        {
            if(overlay)overlay.gameObject.SetActive(false);PlayerPrefs.Save();
            if(EventSystem.current&&overlayReturnFocus&&overlayReturnFocus.activeInHierarchy)EventSystem.current.SetSelectedGameObject(overlayReturnFocus);
            overlayReturnFocus=null;
        }
    }

    public class WorldMapClick : MonoBehaviour,IPointerClickHandler
    {
        public Action<Vector2> clicked;
        public void OnPointerClick(PointerEventData e)=>clicked?.Invoke(e.position);
    }
}








