using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace VeinVanguard
{
    [System.Serializable]
    public sealed class BattlePresentationSettings
    {
        public float targetMs = 800f;
        public float normalPerfectMs = 70f;
        public float normalGoodMs = 160f;
        public float widePerfectMs = 120f;
        public float wideGoodMs = 240f;
        public float initialRadius = 120f;
        public float targetRadius = 42f;
        public float minimumRadius = 12f;
        public float releaseWaitMs = 500f;
        public float timingLabelMs = 180f;
        public float anticipationMs = 150f;
        public float flightMs = 500f;
        public float impactHoldMs = 80f;
        public float revealMs = 350f;
        public float idleTransitionMs = 280f;
        public float drawerTransitionMs = 220f;
        public float attackTransitionMs = 180f;
        public float projectileTransitionMs = 240f;
        public float parryTransitionMs = 220f;
        public float resultTransitionMs = 260f;
        public float radialX = 170f;
        public float radialY = 110f;
        public float radialWidth = 190f;
        public float radialHeight = 120f;
        public float popMs = 180f;
        public float staggerMs = 35f;
        public float fadeMs = 100f;
        public float focusScale = 1.05f;
        public float idleZoom = 1.06f;
        public float drawerZoom = 1f;
        public float hitZoom = 1.16f;
        public float parryZoom = 1.06f;
        public float panX = 0.08f;
        public float panY = 0.025f;
        public float farFactor = 0.2f;
        public float midFactor = 0.4f;
        public float nearFactor = 0.6f;
        public float overscan = 0.18f;
        public float midAlpha = 0.1f;
        public float nearAlpha = 0.06f;
        public float idleAmplitude = 0.015f;
        public float idlePeriod = 1.6f;
        public float shakePixels = 3f;
        public float shakeMs = 120f;
        public int popupCount = 8;
        public float popupMs = 700f;
        public float popupRise = 42f;
        public void Validate()
        {
            if(float.IsNaN(targetMs)||float.IsInfinity(targetMs)||targetMs<=0)targetMs=800f;
            if(float.IsNaN(normalPerfectMs)||float.IsInfinity(normalPerfectMs)||normalPerfectMs<=0)normalPerfectMs=70f;
            if(float.IsNaN(normalGoodMs)||float.IsInfinity(normalGoodMs)||normalGoodMs<=0)normalGoodMs=160f;
            if(float.IsNaN(widePerfectMs)||float.IsInfinity(widePerfectMs)||widePerfectMs<=0)widePerfectMs=120f;
            if(float.IsNaN(wideGoodMs)||float.IsInfinity(wideGoodMs)||wideGoodMs<=0)wideGoodMs=240f;
            if(float.IsNaN(initialRadius)||float.IsInfinity(initialRadius)||initialRadius<=0)initialRadius=120f;
            if(float.IsNaN(targetRadius)||float.IsInfinity(targetRadius)||targetRadius<=0)targetRadius=42f;
            if(float.IsNaN(minimumRadius)||float.IsInfinity(minimumRadius)||minimumRadius<=0)minimumRadius=12f;
            if(float.IsNaN(releaseWaitMs)||float.IsInfinity(releaseWaitMs)||releaseWaitMs<=0)releaseWaitMs=500f;
            if(float.IsNaN(timingLabelMs)||float.IsInfinity(timingLabelMs)||timingLabelMs<=0)timingLabelMs=180f;
            if(float.IsNaN(anticipationMs)||float.IsInfinity(anticipationMs)||anticipationMs<=0)anticipationMs=150f;
            if(float.IsNaN(flightMs)||float.IsInfinity(flightMs)||flightMs<=0)flightMs=500f;
            if(float.IsNaN(impactHoldMs)||float.IsInfinity(impactHoldMs)||impactHoldMs<=0)impactHoldMs=80f;
            if(float.IsNaN(revealMs)||float.IsInfinity(revealMs)||revealMs<=0)revealMs=350f;
            if(float.IsNaN(idleTransitionMs)||float.IsInfinity(idleTransitionMs)||idleTransitionMs<=0)idleTransitionMs=280f;
            if(float.IsNaN(drawerTransitionMs)||float.IsInfinity(drawerTransitionMs)||drawerTransitionMs<=0)drawerTransitionMs=220f;
            if(float.IsNaN(attackTransitionMs)||float.IsInfinity(attackTransitionMs)||attackTransitionMs<=0)attackTransitionMs=180f;
            if(float.IsNaN(projectileTransitionMs)||float.IsInfinity(projectileTransitionMs)||projectileTransitionMs<=0)projectileTransitionMs=240f;
            if(float.IsNaN(parryTransitionMs)||float.IsInfinity(parryTransitionMs)||parryTransitionMs<=0)parryTransitionMs=220f;
            if(float.IsNaN(resultTransitionMs)||float.IsInfinity(resultTransitionMs)||resultTransitionMs<=0)resultTransitionMs=260f;
            if(float.IsNaN(radialX)||float.IsInfinity(radialX)||radialX<=0)radialX=170f;
            if(float.IsNaN(radialY)||float.IsInfinity(radialY)||radialY<=0)radialY=110f;
            if(float.IsNaN(radialWidth)||float.IsInfinity(radialWidth)||radialWidth<=0)radialWidth=190f;
            if(float.IsNaN(radialHeight)||float.IsInfinity(radialHeight)||radialHeight<=0)radialHeight=120f;
            if(float.IsNaN(popMs)||float.IsInfinity(popMs)||popMs<=0)popMs=180f;
            if(float.IsNaN(staggerMs)||float.IsInfinity(staggerMs)||staggerMs<=0)staggerMs=35f;
            if(float.IsNaN(fadeMs)||float.IsInfinity(fadeMs)||fadeMs<=0)fadeMs=100f;
            if(float.IsNaN(focusScale)||float.IsInfinity(focusScale)||focusScale<=0)focusScale=1.05f;
            if(float.IsNaN(idleZoom)||float.IsInfinity(idleZoom)||idleZoom<=0)idleZoom=1.06f;
            if(float.IsNaN(drawerZoom)||float.IsInfinity(drawerZoom)||drawerZoom<=0)drawerZoom=1f;
            if(float.IsNaN(hitZoom)||float.IsInfinity(hitZoom)||hitZoom<=0)hitZoom=1.16f;
            if(float.IsNaN(parryZoom)||float.IsInfinity(parryZoom)||parryZoom<=0)parryZoom=1.06f;
            if(float.IsNaN(panX)||float.IsInfinity(panX)||panX<=0)panX=0.08f;
            if(float.IsNaN(panY)||float.IsInfinity(panY)||panY<=0)panY=0.025f;
            if(float.IsNaN(farFactor)||float.IsInfinity(farFactor)||farFactor<=0)farFactor=0.2f;
            if(float.IsNaN(midFactor)||float.IsInfinity(midFactor)||midFactor<=0)midFactor=0.4f;
            if(float.IsNaN(nearFactor)||float.IsInfinity(nearFactor)||nearFactor<=0)nearFactor=0.6f;
            if(float.IsNaN(overscan)||float.IsInfinity(overscan)||overscan<=0)overscan=0.18f;
            if(float.IsNaN(midAlpha)||float.IsInfinity(midAlpha)||midAlpha<=0)midAlpha=0.1f;
            if(float.IsNaN(nearAlpha)||float.IsInfinity(nearAlpha)||nearAlpha<=0)nearAlpha=0.06f;
            if(float.IsNaN(idleAmplitude)||float.IsInfinity(idleAmplitude)||idleAmplitude<=0)idleAmplitude=0.015f;
            if(float.IsNaN(idlePeriod)||float.IsInfinity(idlePeriod)||idlePeriod<=0)idlePeriod=1.6f;
            if(float.IsNaN(shakePixels)||float.IsInfinity(shakePixels)||shakePixels<=0)shakePixels=3f;
            if(float.IsNaN(shakeMs)||float.IsInfinity(shakeMs)||shakeMs<=0)shakeMs=120f;
            if(float.IsNaN(popupMs)||float.IsInfinity(popupMs)||popupMs<=0)popupMs=700f;
            if(float.IsNaN(popupRise)||float.IsInfinity(popupRise)||popupRise<=0)popupRise=42f;
            normalGoodMs=Mathf.Max(normalPerfectMs,normalGoodMs);
            wideGoodMs=Mathf.Max(widePerfectMs,wideGoodMs);
            hitZoom=Mathf.Clamp(hitZoom,1,1.16f);
            idleZoom=Mathf.Clamp(idleZoom,1,hitZoom);
            drawerZoom=1;parryZoom=Mathf.Clamp(parryZoom,1,hitZoom);
            panX=Mathf.Min(panX,.08f);panY=Mathf.Min(panY,.025f);
            popupCount=Mathf.Max(8,popupCount);
        }
    }
    public partial class BattleManager
    {
        public BattlePresentationSettings presentation = new BattlePresentationSettings();
        [TextArea(3,8)] public string[] formulaInfoTexts = {
            "Gula, lemak, dan bahan lain yang ditambahkan saat mengolah atau menyajikan makanan ikut menambah energi pada sajian. Daftar komposisi dan label gizi menunjukkan apa saja yang ditambahkan. WHO menganjurkan gula bebas kurang dari 10% dari asupan energi harian.",
            "Air putih tidak mengandung energi, gula, maupun lemak. Sebagai minuman, air putih tidak menambah gula bebas ke dalam asupan harian.",
            "Buah utuh masih membawa seratnya. Gula yang menyatu dalam struktur buah utuh tidak dihitung sebagai gula bebas menurut definisi WHO.",
            "Lemak tak jenuh banyak terdapat pada ikan, kacang-kacangan, alpukat, serta minyak seperti minyak zaitun, kanola, dan bunga matahari. WHO menganjurkan lemak jenuh kurang dari 10% dari asupan energi harian, dengan pergeseran konsumsi lemak ke arah lemak tak jenuh.",
            "Mengukus dan merebus memasak makanan dengan uap atau air panas, tanpa minyak masak. Cara memasak menentukan apakah minyak ikut ditambahkan ke dalam sajian.",
            "Variasi olesan berarti mengganti jenis bahan yang dioleskan pada roti. Alpukat, misalnya, mengandung serat, dan lemaknya sebagian besar tak jenuh."
        };
        void ValidatePresentationSettings()
        {
            if(presentation==null)presentation=new BattlePresentationSettings();
            presentation.Validate();
        }
        public string FormulaInfoText(FormulaIcon icon)
        {
            int index=(int)icon;
            return formulaInfoTexts!=null && index>=0 && index<formulaInfoTexts.Length &&
                !string.IsNullOrWhiteSpace(formulaInfoTexts[index]) ? formulaInfoTexts[index] : "Fakta formula belum diisi.";
        }
        static readonly Color PanelSurface=new Color(15f/255,28f/255,46f/255,.90f);
        static readonly Color Ink=new Color(15f/255,28f/255,46f/255);
        static readonly Color Paper=new Color(248f/255,250f/255,252f/255);
        static readonly Color Cyan=new Color(6f/255,182f/255,212f/255);
        static readonly Color Green=new Color(34f/255,197f/255,94f/255);
        static readonly Color Orange=new Color(1,.55f,.22f);
        static readonly Color Amber=new Color(1,.75f,.20f);
        static readonly Color Edge=new Color(45f/255,106f/255,142f/255);
        TextMeshProUGUI playerHpValue,playerMpValue,enemyHpValue,shieldValue,worldHpValue,worldMpValue;
        Image worldHpFill,worldMpFill;
        CanvasGroup movementHint;
        bool movementHintDismissed;
        GameObject overlayReturnFocus;

        static void Place(RectTransform rect,Vector2 min,Vector2 max)
        {rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        static void Fixed(RectTransform rect,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
        {rect.anchorMin=rect.anchorMax=anchor;rect.pivot=pivot;rect.anchoredPosition=position;rect.sizeDelta=size;}
        OutlinedUiGraphic Shape(Image image,Color color,float radius=14,float border=2,Color? edge=null,bool shadow=true)
        {
            image.sprite=null;image.type=Image.Type.Simple;image.color=color;
            var shape=image.GetComponent<OutlinedUiGraphic>();if(!shape)shape=image.gameObject.AddComponent<OutlinedUiGraphic>();
            shape.radius=radius;shape.borderWidth=border;shape.borderColor=edge??Edge;
            shape.shadowOffset=shadow?new Vector2(0,-4):Vector2.zero;
            image.SetVerticesDirty();return shape;
        }
        void InformationPanel(RectTransform rect,Color? color=null,float radius=14)
        {
            var image=rect.GetComponent<Image>();if(!image)image=rect.gameObject.AddComponent<Image>();
            Shape(image,color??PanelSurface,radius);image.raycastTarget=false;
            var old=rect.Find("Cyan Border");if(old)old.gameObject.SetActive(false);
        }
        void StyleButton(Button button,Color? accent=null,Color? surface=null,bool darkText=false)
        {
            Color fill=surface??new Color(.075f,.15f,.23f,.99f),outline=accent??Edge;
            Shape(button.image,fill,14,3,outline);
            button.image.raycastTarget=true;button.transition=Selectable.Transition.None;
            var feedback=button.GetComponent<UiControlFeedback>();if(!feedback)feedback=button.gameObject.AddComponent<UiControlFeedback>();
            feedback.surface=fill;feedback.accent=outline;
            var label=button.GetComponentInChildren<TextMeshProUGUI>();
            label.color=darkText?Ink:Paper;label.fontStyle=FontStyles.Bold;
            label.alignment=TextAlignmentOptions.Center;label.margin=new Vector4(7,3,7,3);
            label.textWrappingMode=TextWrappingModes.Normal;
        }
        TextMeshProUGUI NamedLabel(string name,Transform parent,string text,Vector2 min,Vector2 max,float size=22)
        {
            var existing=parent.Find(name);
            var label=existing?existing.GetComponent<TextMeshProUGUI>():Label(text,parent,min,max,size,Paper);
            label.name=name;Place(label.rectTransform,min,max);label.color=Paper;
            label.fontStyle=FontStyles.Bold;label.fontSizeMax=size;label.fontSizeMin=size*.82f;
            return label;
        }
        RectTransform NamedBox(string name,Transform parent,Vector2 min,Vector2 max)
        {
            var found=parent.Find(name) as RectTransform;
            return found?found:Box(name,parent,min,max,PanelSurface);
        }
        void StyleBar(Image fill,Vector2 min,Vector2 max,Color color)
        {
            var track=(RectTransform)fill.transform.parent;Place(track,min,max);
            Shape(track.GetComponent<Image>(),new Color(.025f,.055f,.09f),8,1,Edge,false);
            Shape(fill,color,6,0,null,false);
            fill.rectTransform.offsetMin=new Vector2(2,2);fill.rectTransform.offsetMax=new Vector2(-2,-2);
        }
        void ApplyPresentation()
        {
            ValidatePresentationSettings();
            EnsureBattleStage();
            var menu=titleRoot.Find("Main Menu Overlay") as RectTransform;
            Fixed(menu,new Vector2(.5f,.04f),new Vector2(.5f,0),Vector2.zero,new Vector2(280,220));
            foreach(var b in menu.GetComponentsInChildren<Button>(true))
            {
                bool start=b.name=="START";float y=start?158:b.name=="SETTINGS"?82:14;
                Fixed((RectTransform)b.transform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,y),new Vector2(start?280:248,start?62:54));
                StyleButton(b,Ink,start?Green:new Color(.89f,.95f,.98f),true);
            }
            var head=safe.Find("Header") as RectTransform;
            Place(head,new Vector2(.025f,.80f),new Vector2(.975f,.975f));
            head.GetComponent<Image>().color=Color.clear;
            var left=head.Find("Nanobot Status") as RectTransform;
            var right=head.Find("Pathogen Status") as RectTransform;
            Place(left,Vector2.zero,new Vector2(.29f,1));Place(right,new Vector2(.71f,0),Vector2.one);
            InformationPanel(left);InformationPanel(right);
            Place(playerStats.rectTransform,new Vector2(.04f,.73f),new Vector2(.47f,.98f));
            Place(enemyStats.rectTransform,new Vector2(.04f,.62f),new Vector2(.96f,.96f));
            playerStats.color=enemyStats.color=Paper;playerStats.fontSizeMax=enemyStats.fontSizeMax=24;
            playerStats.fontSizeMin=enemyStats.fontSizeMin=20;
            var shield=NamedBox("Shield Badge",left,new Vector2(.49f,.74f),new Vector2(.96f,.96f));
            Place(shield,new Vector2(.49f,.74f),new Vector2(.96f,.96f));
            Shape(shield.GetComponent<Image>(),Ink,10,1,Edge,false);
            shieldValue=NamedLabel("Shield Value",shield,"",Vector2.zero,Vector2.one,16);
            Place(shieldValue.rectTransform,new Vector2(.16f,0),Vector2.one);
            shieldValue.alignment=TextAlignmentOptions.Center;
            var shieldIcon=NamedBox("Shield Icon",shield,new Vector2(.02f,.12f),new Vector2(.17f,.88f)).GetComponent<Image>();
            Skin(shieldIcon,11,false);shieldIcon.preserveAspect=true;shieldIcon.raycastTarget=false;
            StyleBar(hpBar,new Vector2(.065f,.46f),new Vector2(.73f,.59f),Green);
            StyleBar(mpBar,new Vector2(.065f,.19f),new Vector2(.73f,.32f),Cyan);
            StyleBar(enemyBar,new Vector2(.065f,.28f),new Vector2(.73f,.41f),Green);
            playerHpValue=NamedLabel("HP Value",left,"",new Vector2(.74f,.40f),new Vector2(.99f,.65f),20);
            playerMpValue=NamedLabel("MP Value",left,"",new Vector2(.74f,.13f),new Vector2(.99f,.38f),20);
            enemyHpValue=NamedLabel("HP Value",right,"",new Vector2(.74f,.22f),new Vector2(.99f,.47f),20);
            var badge=NamedBox("Location Badge",head,new Vector2(.38f,.33f),new Vector2(.62f,.90f));
            InformationPanel(badge);status.transform.SetParent(badge,false);Place(status.rectTransform,Vector2.zero,Vector2.one);
            status.color=Paper;status.fontSizeMax=23;status.fontSizeMin=19;
            commands.GetComponent<Image>().color=Color.clear;
            var message=(safe.Find("Message Panel")??commands.Find("Message Panel")) as RectTransform;
            message.SetParent(safe,false);
            Place(message,new Vector2(.045f,.045f),new Vector2(.955f,.16f));InformationPanel(message);
            Place(log.rectTransform,new Vector2(.045f,.05f),new Vector2(.98f,.95f));log.color=Paper;log.fontSizeMax=22;log.fontSizeMin=18;
            var mark=NamedLabel("Narration Indicator",message,"i",new Vector2(.01f,.1f),new Vector2(.045f,.90f),22);
            mark.color=Cyan;mark.alignment=TextAlignmentOptions.Center;
            var actions=new[]{diagnose,synthesize,restore,homeostasis};var accents=new[]{Cyan,Orange,Green,Amber};
            for(int i=0;i<actions.Length;i++)
            {
                StyleButton(actions[i],accents[i]);
                var icon=actions[i].transform.Find("Action Icon");if(icon)icon.GetComponent<Image>().color=accents[i];
                var text=actions[i].GetComponentInChildren<TextMeshProUGUI>();text.fontSizeMax=25;text.fontSizeMin=21;
            }
            InformationPanel(modal);modal.GetComponent<Image>().raycastTarget=true;
            modalTitle.color=Paper;modalBody.color=Paper;
            var stripe=modal.Find("Accent");if(stripe)stripe.gameObject.SetActive(false);
            StyleExploration();
            EnsureRadialActions();
        }

        enum BattleFraming { Idle, Drawer, AttackQte, Projectile, ParryQte, Result }
        [SerializeField] RectTransform stageRoot,stageViewport,stageVfx,playerMotion,enemyMotion;
        [SerializeField] Image farBackground;
        [SerializeField] RectTransform[] stageSilhouettes;
        Coroutine framingRoutine;
        bool presentationSuspended;

        RectTransform StageBox(string name,Transform parent)
        {
            var found=parent.Find(name) as RectTransform;
            return found?found:Box(name,parent,Vector2.zero,Vector2.one);
        }
        void EnsureBattleStage()
        {
            var canvas=battleCanvas.transform;
            stageViewport=StageBox("Stage Viewport",canvas);stageViewport.SetAsFirstSibling();
            if(!stageViewport.GetComponent<RectMask2D>())stageViewport.gameObject.AddComponent<RectMask2D>();
            stageRoot=StageBox("Stage Root",stageViewport);
            playerMotion=StageBox("Nanobot Motion",stageRoot);enemyMotion=StageBox("Pathogen Motion",stageRoot);
            playerArt.transform.SetParent(playerMotion,false);enemyArt.transform.SetParent(enemyMotion,false);
            stageVfx=StageBox("Stage VFX",stageRoot);stageVfx.SetAsLastSibling();
            var backgroundTransform=canvas.Find("Blood Vessel")??stageViewport.Find("Blood Vessel");
            farBackground=backgroundTransform.GetComponent<Image>();farBackground.transform.SetParent(stageViewport,false);farBackground.transform.SetAsFirstSibling();
            var shade=canvas.Find("Arena Shade")??stageViewport.Find("Arena Shade");
            shade.SetParent(stageViewport,false);shade.SetSiblingIndex(stageRoot.GetSiblingIndex());
            stageSilhouettes=new RectTransform[4];
            for(int i=0;i<4;i++)
            {
                string name="Stage Silhouette "+i;
                var found=stageViewport.Find(name) as RectTransform;
                if(!found)found=Box(name,stageViewport,Vector2.zero,Vector2.one,Color.white);
                var image=found.GetComponent<Image>();image.sprite=enemyOneFrames!=null&&enemyOneFrames.Length>0?enemyOneFrames[0]:null;
                image.preserveAspect=true;image.raycastTarget=false;
                image.color=new Color(.02f,.05f,.07f,i<2?presentation.midAlpha:presentation.nearAlpha);
                stageSilhouettes[i]=found;found.SetSiblingIndex(1+i);
            }
            stageRoot.SetAsLastSibling();UpdateStageLayers();
        }
        void SetPresentationSuspended(bool suspended){presentationSuspended=suspended;}
        void RequestBattleFraming(BattleFraming framing)
        {
            if(!stageRoot)return;
            if(framingRoutine!=null)StopCoroutine(framingRoutine);
            if(Application.isPlaying)framingRoutine=StartCoroutine(TransitionBattleFraming(framing));
            else {stageRoot.localScale=Vector3.one;stageRoot.anchoredPosition=Vector2.zero;UpdateStageLayers();}
        }
        System.Collections.IEnumerator TransitionBattleFraming(BattleFraming framing)
        {
            Canvas.ForceUpdateCanvases();
            bool docked=framing==BattleFraming.Drawer||framing==BattleFraming.Result;
            var actors=new[]{playerArt.rectTransform,enemyArt.rectTransform};
            for(int i=0;i<2;i++)
                Place(actors[i],new Vector2(i==0?.06f:.61f,docked?.395f:.24f),new Vector2(i==0?.40f:.95f,docked?.755f:.76f));
            bool reduced=PlayerPrefs.GetInt("VV.Motion.Reduced",0)==1;
            float zoom=reduced?1:framing==BattleFraming.Projectile?presentation.hitZoom:
                framing==BattleFraming.ParryQte?presentation.parryZoom:docked?presentation.drawerZoom:presentation.idleZoom;
            float ms=framing==BattleFraming.Drawer?presentation.drawerTransitionMs:framing==BattleFraming.Result?presentation.resultTransitionMs:
                framing==BattleFraming.Projectile?presentation.projectileTransitionMs:framing==BattleFraming.ParryQte?presentation.parryTransitionMs:
                framing==BattleFraming.AttackQte?presentation.attackTransitionMs:presentation.idleTransitionMs;
            Vector2 size=stageViewport.rect.size;
            Vector2 target=Vector2.zero;
            if(!reduced&&!docked)
            {
                float sourceX=framing==BattleFraming.Projectile?.78f:framing==BattleFraming.ParryQte?.505f:.23f;
                float destinationX=framing==BattleFraming.Projectile?.64f:framing==BattleFraming.ParryQte?.5f:.30f;
                target=new Vector2(((destinationX-.5f)-(sourceX-.5f)*zoom)*size.x,.03f*size.y);
                target.x=Mathf.Clamp(target.x,-presentation.panX*size.x,presentation.panX*size.x);
                target.y=Mathf.Clamp(target.y,-presentation.panY*size.y,presentation.panY*size.y);
            }
            var start=stageRoot.anchoredPosition;float startZoom=stageRoot.localScale.x;
            for(float elapsed=0;elapsed<ms;)
            {
                if(!presentationSuspended)
                {
                    elapsed+=Time.unscaledDeltaTime*1000;
                    float t=Mathf.Clamp01(elapsed/ms);t=t*t*(3-2*t);
                    stageRoot.anchoredPosition=Vector2.Lerp(start,target,t);stageRoot.localScale=Vector3.one*Mathf.Lerp(startZoom,zoom,t);
                    UpdateStageLayers();
                }
                yield return null;
            }
            framingRoutine=null;
        }
        void UpdateStageLayers()
        {
            if(!farBackground||!stageViewport)return;
            var viewport=stageViewport.rect.size;
            float aspect=farBackground.sprite?farBackground.sprite.rect.width/farBackground.sprite.rect.height:1;
            float width=Mathf.Max(viewport.x,viewport.y*aspect)*(1+presentation.overscan);
            var rect=farBackground.rectTransform;
            bool reduced=PlayerPrefs.GetInt("VV.Motion.Reduced",0)==1;
            var offset=reduced?Vector2.zero:stageRoot.anchoredPosition;
            var limit=new Vector2(Mathf.Max(0,(width-viewport.x)*.5f-4),Mathf.Max(0,(width/aspect-viewport.y)*.5f-4));
            var farOffset=offset*presentation.farFactor;
            farOffset.x=Mathf.Clamp(farOffset.x,-limit.x,limit.x);farOffset.y=Mathf.Clamp(farOffset.y,-limit.y,limit.y);
            Fixed(rect,Vector2.one*.5f,Vector2.one*.5f,farOffset,new Vector2(width,width/aspect));
            if(stageSilhouettes==null)return;
            for(int i=0;i<stageSilhouettes.Length;i++)
            {
                var silhouette=stageSilhouettes[i];if(!silhouette)continue;
                var home=new Vector2((i%2==0?-.42f:.42f)*viewport.x,(i<2?.05f:-.35f)*viewport.y);
                Fixed(silhouette,Vector2.one*.5f,Vector2.one*.5f,home+offset*(i<2?presentation.midFactor:presentation.nearFactor),Vector2.one*viewport.y*(i<2?.32f:.5f));
            }
        }

        CanvasGroup radialGroup;
        Button[] radialButtons;
        bool radialVisible;
        float radialOpenedAt;
        float touchDensity=1;
        int densityScreenWidth;

        public static float MinimumTouchCanvasUnits(float density,float canvasScale)
        {
            if(float.IsNaN(density)||float.IsInfinity(density)||density<=0 ||
                float.IsNaN(canvasScale)||float.IsInfinity(canvasScale)||canvasScale<=0)return 120;
            return 48*density/canvasScale;
        }
        void EnsureRadialActions()
        {
            commands.name="Radial Actions";
            Place(commands,Vector2.zero,Vector2.one);
            commands.GetComponent<Image>().raycastTarget=false;
            radialGroup=commands.GetComponent<CanvasGroup>();
            if(!radialGroup)radialGroup=commands.gameObject.AddComponent<CanvasGroup>();
            radialButtons=new[]{diagnose,synthesize,restore,homeostasis};
            for(int i=0;i<4;i++)
            {
                var b=radialButtons[i];b.transform.SetParent(commands,false);
                var group=b.GetComponent<CanvasGroup>();if(!group)b.gameObject.AddComponent<CanvasGroup>();
                b.GetComponent<UiControlFeedback>().animateVisual=true;
                var label=b.GetComponentInChildren<TextMeshProUGUI>();
                Place(label.rectTransform,new Vector2(.04f,.04f),new Vector2(.96f,.80f));
                label.fontSizeMax=23;label.fontSizeMin=18;
                var icon=b.transform.Find("Action Icon") as RectTransform;
                if(icon)Place(icon,new Vector2(.43f,.79f),new Vector2(.57f,.98f));
                var navigation=b.navigation;navigation.mode=Navigation.Mode.Explicit;
                navigation.selectOnRight=radialButtons[(i+1)%4];navigation.selectOnDown=radialButtons[(i+1)%4];
                navigation.selectOnLeft=radialButtons[(i+3)%4];navigation.selectOnUp=radialButtons[(i+3)%4];b.navigation=navigation;
            }
            var badge=NamedBox("Scan Complete",synthesize.transform,new Vector2(.84f,.77f),new Vector2(.97f,.97f));
            Shape(badge.GetComponent<Image>(),Green,8,0,null,false);
            if(!badge.Find("Short Stroke"))
            {
                var shortStroke=Box("Short Stroke",badge,new Vector2(.20f,.28f),new Vector2(.52f,.43f),Ink);
                shortStroke.localEulerAngles=new Vector3(0,0,-40);
                var longStroke=Box("Long Stroke",badge,new Vector2(.40f,.35f),new Vector2(.85f,.50f),Ink);
                longStroke.localEulerAngles=new Vector3(0,0,45);
            }
            RefreshRadialActions();
        }
        void ResetBattlePresentation()
        {
            presentationSuspended=false;framingRoutine=null;
            if(stageRoot){stageRoot.anchoredPosition=Vector2.zero;stageRoot.localScale=Vector3.one;UpdateStageLayers();}
            radialVisible=false;
            if(radialGroup){radialGroup.alpha=0;radialGroup.blocksRaycasts=false;radialGroup.interactable=false;}
        }
        void RefreshRadialActions()
        {
            if(!radialGroup||radialButtons==null)return;
            if(Screen.width!=densityScreenWidth)
            {
                densityScreenWidth=Screen.width;
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))
                    using(var resources=activity.Call<AndroidJavaObject>("getResources"))
                    using(var metrics=resources.Call<AndroidJavaObject>("getDisplayMetrics"))
                        touchDensity=metrics.Get<float>("density");
                }
                catch(System.Exception){touchDensity=0;}
#endif
            }
            float minimum=MinimumTouchCanvasUnits(touchDensity,safe.GetComponentInParent<Canvas>(true).scaleFactor);
            Vector2 center=(Vector2)safe.InverseTransformPoint(playerArt.rectTransform.TransformPoint(playerArt.rectTransform.rect.center));
            float width=Mathf.Max(230,Mathf.Max(presentation.radialWidth,minimum)),height=Mathf.Max(presentation.radialHeight,minimum);
            float orbitX=Mathf.Max(presentation.radialX,width*.5f+100);
            float orbitY=Mathf.Max(presentation.radialY,height*.5f+32);
            center.x=Mathf.Clamp(center.x,safe.rect.xMin+orbitX+width*.5f+16,safe.rect.xMax-orbitX-width*.5f-16);
            center.y=Mathf.Clamp(center.y,safe.rect.yMin+safe.rect.height*.17f+orbitY+height*.5f,safe.rect.yMax-safe.rect.height*.20f-orbitY-height*.5f);
            for(int i=0;i<4;i++)
            {
                Vector2 offset=new Vector2(i==0||i==3?-orbitX:orbitX,i<2?orbitY:-orbitY);
                Fixed((RectTransform)radialButtons[i].transform,Vector2.one*.5f,Vector2.one*.5f,center+offset,new Vector2(width,height));
                var shape=radialButtons[i].GetComponent<OutlinedUiGraphic>();
                if(shape.radius!=height*.5f){shape.radius=height*.5f;radialButtons[i].image.SetVerticesDirty();}
            }
            bool visible=View==GameView.Battle&&!actionAnimating&&!modal.gameObject.activeSelf&&
                (model.phase==BattlePhase.Player||model.phase==BattlePhase.Diagnose);
            if(visible&&!radialVisible)radialOpenedAt=Time.unscaledTime;
            radialVisible=visible;
            float progress=Application.isPlaying?Mathf.Clamp01((Time.unscaledTime-radialOpenedAt)*1000/(presentation.popMs+3*presentation.staggerMs)):1;
            radialGroup.alpha=Mathf.MoveTowards(radialGroup.alpha,visible?1:0,Application.isPlaying?Time.unscaledDeltaTime*1000/presentation.fadeMs:1);
            radialGroup.interactable=radialGroup.blocksRaycasts=visible&&progress>=1;
            bool reduced=PlayerPrefs.GetInt("VV.Motion.Reduced",0)==1;
            for(int i=0;i<4;i++)
            {
                float t=Application.isPlaying?Mathf.Clamp01(((Time.unscaledTime-radialOpenedAt)*1000-i*presentation.staggerMs)/presentation.popMs):1;
                float ease=t*t*(3-2*t);
                var feedback=radialButtons[i].GetComponent<UiControlFeedback>();
                feedback.entranceOffset=reduced?Vector2.zero:(center-((RectTransform)radialButtons[i].transform).anchoredPosition)*(1-ease);
                feedback.entranceScale=reduced?1:Mathf.Lerp(.65f,1,ease);
                feedback.focusScale=reduced?1:presentation.focusScale;
                radialButtons[i].GetComponent<CanvasGroup>().alpha=reduced?1:ease;
            }
            synthesize.GetComponentInChildren<TextMeshProUGUI>().text=model.diagnosed?"SYNTHESIZE\n<size=65%>Rekaman tersedia · 20 MP</size>":"SYNTHESIZE\n<size=65%>Scan dahulu</size>";
            var badge=synthesize.transform.Find("Scan Complete");if(badge)badge.gameObject.SetActive(model.diagnosed);
        }

        void StyleExploration()
        {
            var header=worldRoot.Find("Exploration HUD") as RectTransform;
            Fixed(header,new Vector2(0,1),new Vector2(0,1),new Vector2(28,-24),new Vector2(410,116));InformationPanel(header);
            worldStatus.name="World Objective";
            var objective=NamedBox("Objective Panel",header,new Vector2(0,-.68f),new Vector2(1,-.10f));
            Place(objective,new Vector2(0,-.68f),new Vector2(1,-.10f));InformationPanel(objective);
            worldStatus.transform.SetParent(objective,false);Place(worldStatus.rectTransform,new Vector2(.02f,.06f),new Vector2(.98f,.94f));
            worldStatus.color=Paper;worldStatus.fontSizeMax=21;worldStatus.fontSizeMin=18;
            var map=NamedLabel("Map Badge",header,"MAP 02",new Vector2(.025f,.64f),new Vector2(.97f,.98f),23);
            map.alignment=TextAlignmentOptions.Left;
            var hp=NamedBox("World HP",header,new Vector2(.06f,.37f),new Vector2(.71f,.52f));
            worldHpFill=NamedBox("Fill",hp,Vector2.zero,Vector2.one).GetComponent<Image>();
            StyleBar(worldHpFill,new Vector2(.06f,.37f),new Vector2(.71f,.52f),Green);
            var mp=NamedBox("World MP",header,new Vector2(.06f,.13f),new Vector2(.71f,.28f));
            worldMpFill=NamedBox("Fill",mp,Vector2.zero,Vector2.one).GetComponent<Image>();
            StyleBar(worldMpFill,new Vector2(.06f,.13f),new Vector2(.71f,.28f),Cyan);
            worldHpValue=NamedLabel("HP Value",header,"",new Vector2(.73f,.30f),new Vector2(.99f,.60f),19);
            worldMpValue=NamedLabel("MP Value",header,"",new Vector2(.73f,.06f),new Vector2(.99f,.36f),19);
            foreach(var item in new[]{("HP Icon",.37f,10),("MP Icon",.13f,9)})
            {
                var icon=NamedBox(item.Item1,header,new Vector2(.015f,item.Item2-.03f),new Vector2(.06f,item.Item2+.18f)).GetComponent<Image>();
                Skin(icon,item.Item3,false);icon.preserveAspect=true;icon.raycastTarget=false;
            }
            foreach(var name in new[]{"SETTINGS","MENU"})
            {
                var found=header.Find(name)??worldRoot.Find(name);if(!found)continue;
                found.SetParent(worldRoot,false);var rt=(RectTransform)found;
                Fixed(rt,Vector2.one,Vector2.one,new Vector2(name=="MENU"?-28:-150,-24),new Vector2(name=="MENU"?106:130,52));
                StyleButton(found.GetComponent<Button>());
                found.GetComponentInChildren<TextMeshProUGUI>().fontSizeMax=22;
            }
            for(int i=0;i<worldEnemies.Length;i++)
            {
                var label=worldEnemies[i].GetComponentInChildren<TextMeshProUGUI>(true);
                var plate=worldEnemies[i].Find("Nameplate") as RectTransform;
                if(!plate)plate=Box("Nameplate",worldEnemies[i],new Vector2(-.38f,1.01f),new Vector2(1.38f,1.28f),PanelSurface);
                Shape(plate.GetComponent<Image>(),new Color(15f/255,28f/255,46f/255,.75f),18,0,null,false);
                label.transform.SetParent(plate,false);Place(label.rectTransform,Vector2.zero,Vector2.one);
                label.color=Paper;label.fontSizeMax=22;label.fontSizeMin=20;label.fontStyle=FontStyles.Bold;
            }
            var analog=(RectTransform)joystick.transform;
            Shape(analog.GetComponent<Image>(),new Color(.035f,.09f,.14f,.35f),90,2,new Color(.55f,.78f,.86f,.7f),false);
            var ring=analog.Find("Medical Ring").GetComponent<Image>();
            Shape(ring,new Color(0,0,0,0),90,2,new Color(.55f,.78f,.86f,.25f),false);
            Shape(joystick.knob.GetComponent<Image>(),new Color(.17f,.45f,.56f,.85f),33,3,new Color(.55f,.85f,.91f),false);
            foreach(var cross in new[]{"Cross Horizontal","Cross Vertical"})
            {var child=joystick.knob.Find(cross);if(child)child.gameObject.SetActive(false);}
            var hint=worldRoot.Find("Movement Hint") as RectTransform;
            Fixed(hint,new Vector2(.60f,0),new Vector2(.5f,0),new Vector2(0,26),new Vector2(860,64));InformationPanel(hint,new Color(15f/255,28f/255,46f/255,.88f),24);
            movementHint=hint.GetComponent<CanvasGroup>();if(!movementHint)movementHint=hint.gameObject.AddComponent<CanvasGroup>();movementHint.blocksRaycasts=false;
            var hintText=hint.GetComponentInChildren<TextMeshProUGUI>();hintText.fontSizeMax=20;hintText.fontSizeMin=18;
            InformationPanel(overlayContent);overlayContent.GetComponent<Image>().raycastTarget=true;
        }
        void RefreshPresentationValues()
        {
            if(playerHpValue)
            {
                playerStats.text="NANOBOT";enemyStats.text=model.encounter==0?"LIPID GOLEM":"GLUCO SLIME";
                playerHpValue.text=$"HP {model.hp}/100";playerMpValue.text=$"MP {model.energy}/60";
                enemyHpValue.text=$"HP {model.enemyHp}/{model.MaxEnemyHp}";shieldValue.text=$"SHIELD {model.shield}% ({model.shieldHits})";
            }
            if(worldHpFill)
            {
                Fill(worldHpFill,model.hp/100f);Fill(worldMpFill,model.energy/60f);
                worldHpValue.text=$"HP {model.hp}/100";worldMpValue.text=$"MP {model.energy}/60";
            }
        }
        void UpdateMovementHint()
        {
            if(!movementHint)return;
            if(walking)movementHintDismissed=true;
            movementHint.alpha=Mathf.MoveTowards(movementHint.alpha,movementHintDismissed?0:1,Time.unscaledDeltaTime*1.8f);
        }
        void StyleLearningWorkbench()
        {
            diagnosisHeading.color=diagnosisFeedback.color=learningHeading.color=learningFeedback.color=Paper;
            sourceTitles=new TextMeshProUGUI[2];sourceCategories=new TextMeshProUGUI[2];
            for(int i=0;i<2;i++)
            {
                var card=(RectTransform)formulaSourceLabels[i].transform.parent;
                InformationPanel(card,new Color(.075f,.15f,.23f,.99f),12);
                var iconPad=NamedBox("Sample Icon Backing",card,new Vector2(.025f,.15f),new Vector2(.255f,.90f));
                Shape(iconPad.GetComponent<Image>(),Ink,12,0,null,false);iconPad.SetAsFirstSibling();
                Place(formulaSourceIcons[i].rectTransform,new Vector2(.035f,.19f),new Vector2(.245f,.87f));
                sourceTitles[i]=NamedLabel("Source Title",card,"",new Vector2(.28f,.72f),new Vector2(.98f,.97f),27);
                var tag=NamedBox("Category Badge",card,new Vector2(.29f,.51f),new Vector2(.73f,.69f));
                Shape(tag.GetComponent<Image>(),Ink,14,1,Edge,false);
                sourceCategories[i]=NamedLabel("Category",tag,"",Vector2.zero,Vector2.one,20);
                sourceCategories[i].color=Cyan;sourceCategories[i].alignment=TextAlignmentOptions.Center;
                Place(formulaSourceLabels[i].rectTransform,new Vector2(.28f,.035f),new Vector2(.98f,.49f));
                formulaSourceLabels[i].color=Paper;formulaSourceLabels[i].fontSizeMax=24;formulaSourceLabels[i].fontSizeMin=21;
                formulaSourceReminders[i].color=Paper;
                var socket=formulaSockets[i];StyleButton(socket,Cyan,Ink);
                socket.GetComponent<OutlinedUiGraphic>().shadowOffset=Vector2.zero;
                Place(formulaSocketLabels[i].rectTransform,new Vector2(.02f,.58f),new Vector2(.98f,1));
                formulaSocketLabels[i].margin=Vector4.zero;
                formulaSocketLabels[i].fontSizeMax=18;formulaSocketLabels[i].fontSizeMin=16;
                Place(formulaSocketIcons[i].rectTransform,new Vector2(.12f,.04f),new Vector2(.88f,.70f));
            }
            var area=learningRoot.Find("Plasma Weapon Area") as RectTransform;
            Shape(area.GetComponent<Image>(),new Color(.055f,.11f,.18f,.98f),12,1,Edge,false);
            for(int i=0;i<formulaPalette.Length;i++)
            {
                var button=formulaPalette[i];StyleButton(button,Edge);
                var label=button.GetComponentInChildren<TextMeshProUGUI>();
                Place(label.rectTransform,new Vector2(.015f,.015f),new Vector2(.985f,.39f));
                label.margin=new Vector4(2,0,2,0);label.fontSizeMax=20;label.fontSizeMin=18;
                var icon=button.transform.Find(UniversalFormula.IconName((FormulaIcon)i)) as RectTransform;
                Place(icon,new Vector2(.13f,.36f),new Vector2(.87f,.99f));
            }
            var track=(RectTransform)formulaChargeFill.parent;
            Place(track,new Vector2(.81f,.54f),new Vector2(.975f,.60f));
            Shape(track.GetComponent<Image>(),Ink,8,1,Edge,false);
            Shape(formulaChargeFill.GetComponent<Image>(),Cyan,7,0,null,false);
            formulaChargeLabel.color=Paper;
            formulaChargeLabel.margin=new Vector4(2,0,2,0);
            StyleButton(learningFire,Cyan,new Color(.035f,.25f,.32f));
            StyleButton(diagnosisClose);StyleButton(learningClose);
            Shape(formulaGhost.GetComponent<Image>(),new Color(.075f,.15f,.23f,.98f),14,3,Cyan);
        }
        void UpdateUiNavigation()
        {
            var keyboard=Keyboard.current;if(keyboard==null||!EventSystem.current)return;
            if(keyboard.escapeKey.wasPressedThisFrame && View==GameView.Battle && !actionAnimating &&
                ((learningRoot&&learningRoot.gameObject.activeInHierarchy)||(diagnosisRoot&&diagnosisRoot.gameObject.activeInHierarchy)))
            {CloseLearningWorkbench();return;}
            if(!keyboard.tabKey.wasPressedThisFrame&&!keyboard.spaceKey.wasPressedThisFrame)return;
            Transform root=SettingsOpen?overlayContent:View==GameView.Title?titleRoot:
                View==GameView.World?worldRoot:modal.gameObject.activeSelf?modal:commands;
            var controls=System.Array.FindAll(root.GetComponentsInChildren<Selectable>(),control=>control.IsInteractable());
            if(controls.Length==0)return;
            if(root!=commands)System.Array.Sort(controls,(a,b)=>{
                var first=a.transform.position;var second=b.transform.position;
                int row=Mathf.RoundToInt(second.y/40)-Mathf.RoundToInt(first.y/40);
                return row!=0?row:first.x.CompareTo(second.x);
            });
            int current=System.Array.FindIndex(controls,control=>control.gameObject==EventSystem.current.currentSelectedGameObject);
            if(keyboard.tabKey.wasPressedThisFrame)
            {
                int next=current<0?(keyboard.shiftKey.isPressed?controls.Length-1:0):
                    (current+(keyboard.shiftKey.isPressed?-1:1)+controls.Length)%controls.Length;
                EventSystem.current.SetSelectedGameObject(controls[next].gameObject);
            }
            else if(current>=0 && controls[current] is Button button)
                ExecuteEvents.Execute(button.gameObject,new BaseEventData(EventSystem.current),ExecuteEvents.submitHandler);
        }
        void FocusOverlay()
        {
            if(!EventSystem.current)return;
            var controls=overlayContent.GetComponentsInChildren<Selectable>();
            for(int i=0;i<controls.Length;i++)
            {
                var navigation=controls[i].navigation;navigation.mode=Navigation.Mode.Explicit;
                navigation.selectOnUp=controls[(i+controls.Length-1)%controls.Length];
                navigation.selectOnDown=controls[(i+1)%controls.Length];
                controls[i].navigation=navigation;
            }
            if(controls.Length>0)EventSystem.current.SetSelectedGameObject(controls[0].gameObject);
        }
    }
}
