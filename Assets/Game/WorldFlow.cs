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
        public bool[] defeated = new bool[2];
        public bool SettingsOpen => overlay.gameObject.activeSelf;
        public float MusicVolume => musicSource.volume;
        public float SfxVolume => effectSource.volume;
        [SerializeField] GameObject battleCanvas;
        [SerializeField] RectTransform flowSafe, titleRoot, worldRoot, worldContent, worldPlayer, overlay, overlayContent;
        [SerializeField] UnityEngine.UI.Image worldPlayerImage;
        [SerializeField] RectTransform[] worldEnemies = new RectTransform[2];
        [SerializeField] UnityEngine.UI.Image[] worldEnemyImages = new UnityEngine.UI.Image[2];
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
            string[] sheets={"WalkDown","WalkRight","WalkLeft","WalkUp","WalkDownRight","WalkDownLeft","WalkUpRight","WalkUpLeft","IdleFront"};
            walkFrames=new Sprite[136];
            for(int group=0;group<sheets.Length;group++)
            {
                var sheet=Resources.Load<Texture2D>("VeinVanguard/"+sheets[group]);
                if(!sheet){Debug.LogError("Missing exploration sprites: "+sheets[group]);walkFrames=null;return;}
                int rows=group<8?4:2;
                float w=sheet.width/4f,h=sheet.height/(float)rows;
                // Sixteen walk poses; front idle retains its eight-frame sheet.
                for(int row=0;row<rows;row++)for(int col=0;col<4;col++)
                    walkFrames[group*16+row*4+col]=Sprite.Create(sheet,new Rect(col*w,(rows-1-row)*h,w,h),new Vector2(.5f,0),100,0,SpriteMeshType.FullRect);
            }
        }
        [SerializeField] AudioSource musicSource, effectSource;
        Vector2 worldPosition = new Vector2(.31f,.25f), targetPosition;
        bool hasTarget;
        Vector2 heldDirection;
        readonly Vector2 mapSize = new Vector2(2400,1697);
        readonly Vector2[] enemyPositions = {new Vector2(.50f,.36f),new Vector2(.85f,.25f)};
        const float EncounterRadius = 105;
        void PreparePresentation()
        {
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
            if(!musicSource)musicSource=gameObject.AddComponent<AudioSource>();musicSource.loop=true;musicSource.playOnAwake=false;musicSource.spatialBlend=0;
            if(!effectSource)effectSource=gameObject.AddComponent<AudioSource>();effectSource.playOnAwake=false;effectSource.spatialBlend=0;
            musicSource.volume=PlayerPrefs.GetFloat("VV.Music",.45f);effectSource.volume=PlayerPrefs.GetFloat("VV.SFX",.75f);
        }
        void OnDestroy() { if(hud!=null&&hudSprites==null)foreach(var s in hud)if(s)Destroy(s);if(walkFrames!=null)foreach(var s in walkFrames)if(s)Destroy(s);Time.timeScale=1; }
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
            var image=panel.GetComponent<UnityEngine.UI.Image>()??panel.gameObject.AddComponent<UnityEngine.UI.Image>();
            Skin(image,1);
            image.raycastTarget=false;
            var oldBorder=panel.Find("Cyan Border");
            if(oldBorder)oldBorder.gameObject.SetActive(false);
        }
        void MedicalButton(UnityEngine.UI.Button b,int icon=-1)
        {
            Skin(b.GetComponent<UnityEngine.UI.Image>(),1);
            if(hud.Length>=5&&hud[2]&&hud[3]&&hud[4])
            { b.transition=UnityEngine.UI.Selectable.Transition.SpriteSwap;b.spriteState=new UnityEngine.UI.SpriteState {highlightedSprite=hud[2],selectedSprite=hud[2],pressedSprite=hud[3],disabledSprite=hud[4]}; }
            var label=b.GetComponentInChildren<TextMeshProUGUI>();label.fontStyle=FontStyles.Bold;label.color=new Color(.035f,.20f,.40f);
            label.alignment=TextAlignmentOptions.Center;label.margin=new Vector4(5,2,5,2);
            label.rectTransform.anchorMin=new Vector2(.10f,.16f);label.rectTransform.anchorMax=new Vector2(.90f,.84f);
            label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;
            if(icon>=0)
            {
                var image=Box("Action Icon",b.transform,new Vector2(.035f,.15f),new Vector2(.22f,.85f),Color.white).GetComponent<UnityEngine.UI.Image>();Skin(image,icon,false);image.preserveAspect=true;
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
            diagnose.GetComponentInChildren<TextMeshProUGUI>().text="DIAGNOSE\n<size=65%>Pindai komposisi</size>";
            synthesize.GetComponentInChildren<TextMeshProUGUI>().text="SYNTHESIZE\n<size=65%>Pilih nutrisi · 20 MP</size>";
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
                    Vector2 target=local/mapSize;if(IsWalkable(target)){targetPosition=target;hasTarget=true;}
                }
            };
            worldPlayer=MapActor("Nanobot",worldContent,new Vector2(130,150));worldPlayerImage=worldPlayer.GetComponent<UnityEngine.UI.Image>();
            for(int i=0;i<2;i++)
            {
                worldEnemies[i]=MapActor("Enemy "+(i+1),worldContent,new Vector2(145,155));worldEnemyImages[i]=worldEnemies[i].GetComponent<UnityEngine.UI.Image>();
                worldEnemies[i].anchoredPosition=MapPoint(enemyPositions[i]);
                Label(i==0?"LIPID GOLEM":"GLUCO SLIME",worldEnemies[i],new Vector2(-.25f,1),new Vector2(1.25f,1.22f),20,navy,TextAlignmentOptions.Center).fontStyle=FontStyles.Bold;
            }
            var header=Box("Exploration HUD",worldRoot,new Vector2(.02f,.87f),new Vector2(.98f,.98f),new Color(navy.r,navy.g,navy.b,.94f));header.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            worldStatus=Label("",header,new Vector2(.02f,.1f),new Vector2(.71f,.9f),24,Color.white);
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
            joystick.changed=value=>{heldDirection=View==GameView.World&&!SettingsOpen?value:Vector2.zero;if(value!=Vector2.zero)hasTarget=false;};
            var hint=worldRoot.Find("Movement Hint");
            if(hint)hint.GetComponentInChildren<TextMeshProUGUI>().text="Analog / WASD / panah • Ketuk daratan untuk bergerak • Dekati musuh untuk bertarung";
        }
        void HandleMapClick(Vector2 screen)
        {
            if(View!=GameView.World||SettingsOpen)return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(worldContent,screen,null,out Vector2 local))
            {Vector2 target=local/mapSize;if(IsWalkable(target)){targetPosition=target;hasTarget=true;}}
        }
        void WireBakedHierarchy()
        {
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
            foreach(var path in new[]{"Header/Nanobot Status","Header/Pathogen Status","Commands/Message Panel"})
            {
                var panel=safe.Find(path);
                if(panel)ButtonFrame(panel as RectTransform);
            }
        }
        public void BakeHierarchyForEditor()
        {
            if(Application.isPlaying)return;
            if(hierarchyBaked){PreparePresentation();EnsureBattlePanels();EnsureAnalogControl();return;}
            PreparePresentation();BuildUI();BuildFlow();hierarchyBaked=true;
            battleCanvas.name="Battle UI";flowSafe.transform.parent.name="Game Screens";
            battleCanvas.SetActive(false);titleRoot.gameObject.SetActive(true);worldRoot.gameObject.SetActive(false);overlay.gameObject.SetActive(false);
            if(walkFrames!=null)worldPlayerImage.sprite=walkFrames[128];
            for(int i=0;i<2;i++){var frames=i==0?enemyOneFrames:enemyTwoFrames;if(frames!=null&&frames.Length>0)worldEnemyImages[i].sprite=frames[0];}
        }
        void ShowTitle()
        {
            if(joystick)joystick.ResetInput();
            Time.timeScale=1;CloseOverlay();View=GameView.Title;model.phase=BattlePhase.Menu;
            battleCanvas.SetActive(false);titleRoot.gameObject.SetActive(true);worldRoot.gameObject.SetActive(false);PlayMusic(mainMusic);
        }
        void StartExploration()
        {
            StopAllCoroutines();ClearVisualEffects();actionAnimating=false;walkRow=0;walkTime=0;model=new BattleModel();defeated=new bool[2];questionIndex=-1;
            worldPosition=new Vector2(.31f,.25f);ReturnToWorld();
        }
        void ReturnToWorld()
        {
            StopAllCoroutines();ClearVisualEffects();Time.timeScale=1;CloseOverlay();View=GameView.World;model.phase=BattlePhase.Menu;
            if(joystick)joystick.ResetInput();
            hasTarget=false;heldDirection=Vector2.zero;walking=false;walkTime=idleTime=0;actionAnimating=false;
            battleCanvas.SetActive(false);titleRoot.gameObject.SetActive(false);worldRoot.gameObject.SetActive(true);
            for(int i=0;i<2;i++)worldEnemies[i].gameObject.SetActive(!defeated[i]);
            PlayMusic(mapMusic);PositionWorld();
        }
        public void BeginEncounter(int index)
        {
            if(View!=GameView.World||SettingsOpen||index<0||index>1||defeated[index])return;
            View=GameView.Battle;hasTarget=false;heldDirection=Vector2.zero;model.Begin(index);
            worldRoot.gameObject.SetActive(false);titleRoot.gameObject.SetActive(false);battleCanvas.SetActive(true);
            HideDialog();Pose("idle");log.text="Patogen mendekat! Gunakan Diagnose untuk memindai komposisinya.";Refresh();PlayMusic(battleMusic);
        }
        void UpdateFlow()
        {
            Rect r=Screen.safeArea;flowSafe.anchorMin=new Vector2(r.xMin/Screen.width,r.yMin/Screen.height);flowSafe.anchorMax=new Vector2(r.xMax/Screen.width,r.yMax/Screen.height);
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){if(SettingsOpen)CloseOverlay();else if(View!=GameView.Battle)OpenSettings();}
            if(View!=GameView.World||SettingsOpen)return;
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
            PositionWorld();
            for(int i=0;i<2;i++)if(!defeated[i]&&Vector2.Scale(worldPosition-enemyPositions[i],mapSize).magnitude<EncounterRadius){BeginEncounter(i);break;}
        }
        public static bool IsWalkable(Vector2 p)
        {
            if(p.x<.12f||p.x>.96f||p.y<.045f||p.y>.64f)return false;
            if(p.x<.28f&&p.y>.22f)return false;
            Vector2[] pond={new Vector2(.36f,.48f),new Vector2(.43f,.42f),new Vector2(.66f,.40f),new Vector2(.82f,.37f),new Vector2(.88f,.45f),new Vector2(1,.41f),new Vector2(1,.59f),new Vector2(.78f,.61f),new Vector2(.60f,.58f),new Vector2(.55f,.51f)};
        bool inside=false;
            for(int i=0,j=pond.Length-1;i<pond.Length;j=i++)if((pond[i].y>p.y)!=(pond[j].y>p.y)&&p.x<(pond[j].x-pond[i].x)*(p.y-pond[i].y)/(pond[j].y-pond[i].y)+pond[i].x)inside=!inside;
            return !inside;
        }
        public void MoveWorld(Vector2 deltaPixels)
        {
            if(View!=GameView.World||SettingsOpen)return;
            var delta=deltaPixels/mapSize;var original=worldPosition;
            var next=worldPosition+new Vector2(delta.x,0);if(IsWalkable(next))worldPosition=next;
            next=worldPosition+new Vector2(0,delta.y);if(IsWalkable(next))worldPosition=next;
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
            worldPlayer.anchoredPosition=MapPoint(worldPosition);
            int frame=(int)(Time.unscaledTime*7)%4;
            if(walkFrames!=null)worldPlayerImage.sprite=walkFrames[walking?walkRow*16+(int)(walkTime*24)%16:128+(int)(idleTime*4)%8];
            else if(playerFrames!=null&&playerFrames.Length>0)worldPlayerImage.sprite=playerFrames[0];
            for(int i=0;i<2;i++)worldEnemyImages[i].sprite=(i==0?enemyOneFrames:enemyTwoFrames)[frame];
            Vector2 viewport=worldRoot.rect.size;
            Vector2 center=MapPoint(worldPosition);
            worldContent.anchoredPosition=new Vector2(Mathf.Clamp(viewport.x*.5f-center.x,Mathf.Min(0,viewport.x-mapSize.x),0),Mathf.Clamp(viewport.y*.55f-center.y,Mathf.Min(0,viewport.y-mapSize.y),0));
            worldStatus.text=$"MAP 01  •  HP {model.hp}/100  •  MP {model.energy}/60\n"+((defeated[0]&&defeated[1])?"Area aman — semua patogen telah dikalahkan.":$"Patogen dikalahkan: {(defeated[0]?1:0)+(defeated[1]?1:0)}/2. Jelajahi jalan menuju timur.");
        }
        void ClearOverlay()
        {
            if(joystick)joystick.ResetInput();
            heldDirection=Vector2.zero;hasTarget=false;overlay.gameObject.SetActive(true);
            var panelImage=overlayContent.GetComponent<UnityEngine.UI.Image>();
            if(panelImage)ButtonFrame(overlayContent);
            for(int i=overlayContent.childCount-1;i>=0;i--){var child=overlayContent.GetChild(i).gameObject;child.SetActive(false);Destroy(child);}
        }
        public void OpenCredits()
        {
            ClearOverlay();Label("CREDITS",overlayContent,new Vector2(.08f,.78f),new Vector2(.92f,.94f),40,mint,TextAlignmentOptions.Center);
            Label("Dibuat oleh\n\nHilmi Aminuddien\nMohammad Ferry Irwansyah\nChristian Gideon\nSyachrul Ramadhan",overlayContent,new Vector2(.08f,.24f),new Vector2(.92f,.77f),30,navy,TextAlignmentOptions.Center);
            MedicalButton(Button("KEMBALI",overlayContent,new Vector2(.25f,.06f),new Vector2(.75f,.19f),CloseOverlay));
        }
        public void OpenSettings()
        {
            ClearOverlay();Label("SETTINGS",overlayContent,new Vector2(.08f,.78f),new Vector2(.92f,.94f),40,mint,TextAlignmentOptions.Center);
            VolumeSlider("Musik",.61f,musicSource.volume,SetMusicVolume);
            VolumeSlider("Efek suara",.39f,effectSource.volume,SetSfxVolume);
            Label("Pengaturan volume disimpan di perangkat ini.",overlayContent,new Vector2(.08f,.22f),new Vector2(.92f,.31f),20,navy,TextAlignmentOptions.Center);
            MedicalButton(Button("KEMBALI",overlayContent,new Vector2(.25f,.06f),new Vector2(.75f,.19f),CloseOverlay));
        }
        void VolumeSlider(string title,float y,float value,UnityEngine.Events.UnityAction<float> action)
        {
            var label=Label(title+"  "+Mathf.RoundToInt(value*100)+"%",overlayContent,new Vector2(.09f,y),new Vector2(.91f,y+.1f),26,navy);
            var root=Box(title+" Volume",overlayContent,new Vector2(.1f,y-.06f),new Vector2(.90f,y),Color.white);
            Skin(root.GetComponent<UnityEngine.UI.Image>(),5);
            var slider=root.gameObject.AddComponent<UnityEngine.UI.Slider>();root.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            var fill=Box("Fill",root,Vector2.zero,Vector2.one,mint);
            var handleArea=Box("Handle Area",root,Vector2.zero,Vector2.one);
            var handle=Box("Handle",handleArea,Vector2.zero,Vector2.one,Color.white);handle.sizeDelta=new Vector2(24,14);handle.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<UnityEngine.UI.Image>();slider.minValue=0;slider.maxValue=1;slider.value=value;
            slider.onValueChanged.AddListener(v=>{action(v);label.text=title+"  "+Mathf.RoundToInt(v*100)+"%";});
        }
        public void CloseOverlay(){if(overlay)overlay.gameObject.SetActive(false);PlayerPrefs.Save();}
    }

    public class WorldMapClick : MonoBehaviour,IPointerClickHandler
    {
        public Action<Vector2> clicked;
        public void OnPointerClick(PointerEventData e)=>clicked?.Invoke(e.position);
    }
}
