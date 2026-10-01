using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace VeinVanguard
{
    public partial class BattleManager : MonoBehaviour
    {
        public Sprite background;
        public Sprite[] playerFrames, enemyOneFrames, enemyTwoFrames;
        public TMP_FontAsset font;
        public bool hierarchyBaked;
        public BattleModel model = new BattleModel();
        public BattlePhase Phase => model.phase;
        [SerializeField] RectTransform safe, modal, options, commands;
        [SerializeField] TextMeshProUGUI playerStats, enemyStats, status, log, modalTitle, modalBody;
        [SerializeField] UnityEngine.UI.Image hpBar, mpBar, enemyBar, playerArt, enemyArt;
        [SerializeField] UnityEngine.UI.Image diagnosticArt;
        [SerializeField] RectTransform diagnosticFrame;
        [SerializeField] CanvasGroup modalCanvasGroup;
        [SerializeField] UnityEngine.UI.Button diagnose, synthesize, restore, homeostasis;
        public Sprite[] vfxFrames;
        [Min(0)] public int homeostasisCost = 10;
        [Range(1,100)] public int homeostasisReduction = 90;
        [Min(1)] public int homeostasisHits = 2;
        string playerPose = "idle", enemyPose = "idle";
        TriviaDeck triviaDeck = new TriviaDeck();
        TriviaQuestion currentQuestion;
        float poseTime;
        bool actionAnimating;
        const float ActionDuration = .65f;
        enum DialogLayout { Center, Bottom, Diagnosis }
        readonly Color navy = new Color(.025f, .12f, .26f), mint = new Color(.15f, .78f, 1f);
        void Awake()
        {
            PreparePresentation();
            if(!hierarchyBaked){BuildUI();BuildFlow();}
            else WireBakedHierarchy();
            ShowMenu();
        }
        void Update()
        {
            Rect r = Screen.safeArea;
            safe.anchorMin = new Vector2(r.xMin / Screen.width, r.yMin / Screen.height);
            safe.anchorMax = new Vector2(r.xMax / Screen.width, r.yMax / Screen.height);
            poseTime += Time.deltaTime;
            SetFrame(playerArt, playerFrames, PlayerPoseIndex(playerPose));
            SetFrame(enemyArt, model.encounter == 0 ? enemyOneFrames : enemyTwoFrames, EnemyPoseIndex(enemyPose));
            UpdateFlow();
        }
        int PlayerPoseIndex(string s)
        {
            string[] names = { "idle", "diagnose", "synthesize", "nutrient_attack", "restore", "trivia_wait", "homeostasis_activate", "homeostasis_hit", "hurt", "low_energy", "victory", "shutdown" };
            return System.Array.IndexOf(names, s);
        }
        int EnemyPoseIndex(string s) => s == "attack" ? 1 : s == "hurt" ? 2 : s == "defeat" ? 3 : 0;
        void SetFrame(UnityEngine.UI.Image image, Sprite[] frames, int pose)
        {
            if (frames == null || frames.Length == 0) return;
            bool loop = pose == 0 || (image == playerArt && (pose == 5 || pose == 9));
            int frame = loop ? (int)(poseTime * 7) % 4 : Mathf.Min(3, (int)(poseTime * 8));
            int index = Mathf.Clamp(pose * 4 + frame, 0, frames.Length - 1);
            if (frames[index]) image.sprite = frames[index];
        }
        void Pose(string p, string e = "idle") { playerPose = p; enemyPose = e; poseTime = 0; }
        void EnsureHomeostasisButton()
        {
            if(!homeostasis)homeostasis=Button("HOMEOSTASIS",commands,Vector2.zero,Vector2.one,Homeostasis);
            var buttons=new[]{diagnose,synthesize,restore,homeostasis};
            for(int i=0;i<buttons.Length;i++)
            {
                var rect=buttons[i].GetComponent<RectTransform>();
                rect.anchorMin=new Vector2(.025f+i*.245f,.03f);rect.anchorMax=new Vector2(.255f+i*.245f,.57f);
                rect.offsetMin=rect.offsetMax=Vector2.zero;
            }
            if(hud!=null)MedicalButton(homeostasis,11);
            homeostasis.GetComponentInChildren<TextMeshProUGUI>().text=$"HOMEOSTASIS\n<size=65%>{homeostasisCost} MP · -{homeostasisReduction}% · {homeostasisHits} hit</size>";
            var oldIcon=commands.Find("Homeostasis Icon");if(oldIcon)oldIcon.gameObject.SetActive(false);
            foreach(var label in commands.GetComponentsInChildren<TextMeshProUGUI>(true))
                if(label.transform.parent==commands&&label.text=="HOMEOSTASIS")label.gameObject.SetActive(false);
        }
        public void Homeostasis()
        {
            if(actionAnimating||View!=GameView.Battle||modal.gameObject.activeSelf||!model.Homeostasis(homeostasisCost,homeostasisReduction,homeostasisHits))return;
            actionAnimating=true;Refresh();StartCoroutine(HomeostasisSequence());
        }
        IEnumerator HomeostasisSequence()
        {
            Pose("homeostasis_activate");PlayEffect(defendSound);
            log.text=$"Homeostasis aktif: -{homeostasisReduction}% damage untuk {homeostasisHits} serangan • -{homeostasisCost} MP.";
            yield return VisualEffect(4,playerArt);
            yield return EnemyTurn();
        }
        IEnumerator VisualEffect(int effect,UnityEngine.UI.Image origin,UnityEngine.UI.Image destination=null)
        {
            if(vfxFrames==null||vfxFrames.Length<(effect+1)*4)yield break;
            var rect=Box("Action VFX",safe,Vector2.zero,Vector2.zero,Color.white);
            var image=rect.GetComponent<UnityEngine.UI.Image>();image.preserveAspect=true;
            // Match the full square sprite cell, not opaque bounds, so registration stays stable.
            float size=Mathf.Min(origin.rectTransform.rect.width,origin.rectTransform.rect.height);
            rect.sizeDelta=Vector2.one*size;
            Vector3 start=origin.rectTransform.TransformPoint(origin.rectTransform.rect.center);
            Vector3 end=destination?destination.rectTransform.TransformPoint(destination.rectTransform.rect.center):start;
            const float duration=.5f;
            for(float time=0;time<duration;time+=Time.deltaTime)
            {
                image.sprite=vfxFrames[effect*4+Mathf.Min(3,(int)(time/duration*4))];
                rect.position=Vector3.Lerp(start,end,time/duration);yield return null;
            }
            if(destination){rect.position=end;yield return null;}
            rect.gameObject.SetActive(false);Destroy(rect.gameObject);
        }
        void ClearVisualEffects()
        {
            if(!safe)return;
            for(int i=safe.childCount-1;i>=0;i--)if(safe.GetChild(i).name=="Action VFX")
            {safe.GetChild(i).gameObject.SetActive(false);Destroy(safe.GetChild(i).gameObject);}
        }
        RectTransform Box(string name, Transform parent, Vector2 min, Vector2 max, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>(); rt.SetParent(parent, false);
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
            if (color.HasValue) { var im = go.AddComponent<UnityEngine.UI.Image>(); im.color = color.Value; im.raycastTarget = false; }
            return rt;
        }
        TextMeshProUGUI Label(string text, Transform parent, Vector2 min, Vector2 max, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var t = Box("Label", parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.enableAutoSizing = true; t.fontSizeMin = size * .7f; t.fontSizeMax = size;
            t.raycastTarget = false; t.margin = new Vector4(8, 4, 8, 4); return t;
        }
        UnityEngine.UI.Button Button(string title, Transform parent, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action, Color? tint = null)
        {
            var rt = Box(title, parent, min, max, tint ?? mint);
            var im = rt.GetComponent<UnityEngine.UI.Image>(); im.raycastTarget = true;
            var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = im;
            var colors = b.colors; colors.highlightedColor = new Color(.8f, 1, 1); colors.pressedColor = new Color(.5f, .75f, .75f); colors.disabledColor = new Color(.25f, .32f, .35f); b.colors = colors;
            b.onClick.AddListener(()=>{PlayEffect(clickSound);action();});
            Label(title, rt, Vector2.zero, Vector2.one, 25, navy, TextAlignmentOptions.Center);
            return b;
        }
        UnityEngine.UI.Image Bar(Transform parent, float y, Color color)
        {
            var track = Box("Track", parent, new Vector2(.055f, y), new Vector2(.945f, y + .085f), new Color(.12f, .22f, .27f));
            var fill = Box("Fill", track, Vector2.zero, Vector2.one, color).GetComponent<UnityEngine.UI.Image>();
            return fill;
        }
        void Fill(UnityEngine.UI.Image image, float fraction) { image.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1); }
        void BuildUI()
        {
            var canvasGO = new GameObject("Medical HUD", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            battleCanvas = canvasGO;
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            var backdrop = Box("Blood Vessel", canvasGO.transform, Vector2.zero, Vector2.one, Color.white).GetComponent<UnityEngine.UI.Image>(); backdrop.sprite = background;
            Box("Arena Shade", canvasGO.transform, Vector2.zero, Vector2.one, new Color(.015f, .04f, .065f, .42f));
            safe = Box("Safe Area", canvasGO.transform, Vector2.zero, Vector2.one);
            var head = Box("Header", safe, new Vector2(.025f,.76f), new Vector2(.975f,.97f), new Color(navy.r,navy.g,navy.b,.95f));
            var left = Box("Nanobot Status", head, Vector2.zero, new Vector2(.34f,1));
            playerStats = Label("", left, new Vector2(.035f,.40f), new Vector2(.97f,.97f), 24, Color.white);
            hpBar = Bar(left,.29f,mint); mpBar = Bar(left,.14f,new Color(.28f,.72f,1));
            status = Label("", head, new Vector2(.35f,.12f), new Vector2(.65f,.92f), 25, mint, TextAlignmentOptions.Center);
            var right = Box("Pathogen Status", head, new Vector2(.67f,0), Vector2.one);
            enemyStats = Label("", right, new Vector2(.035f,.34f), new Vector2(.97f,.97f), 24, Color.white);
            enemyBar = Bar(right,.16f,new Color(1,.51f,.38f));
            playerArt = Box("Nanobot", safe, new Vector2(.06f,.24f), new Vector2(.40f,.76f), Color.white).GetComponent<UnityEngine.UI.Image>(); playerArt.preserveAspect = true;
            enemyArt = Box("Pathogen", safe, new Vector2(.61f,.24f), new Vector2(.95f,.76f), Color.white).GetComponent<UnityEngine.UI.Image>(); enemyArt.preserveAspect = true;
            commands = Box("Commands", safe, new Vector2(.025f,.025f), new Vector2(.975f,.25f), new Color(navy.r,navy.g,navy.b,.98f));
            var bottom=commands;
            log = Label("", bottom, new Vector2(.025f,.57f), new Vector2(.975f,.97f), 24, Color.white);
            diagnose = Button("01  DIAGNOSE", bottom, new Vector2(.025f,.08f), new Vector2(.325f,.52f), Diagnose);
            synthesize = Button("02  SYNTHESIZE  ·  20 MP", bottom, new Vector2(.35f,.08f), new Vector2(.65f,.52f), Synthesize);
            restore = Button("03  RESTORE  ·  KUIS", bottom, new Vector2(.675f,.08f), new Vector2(.975f,.52f), Restore);
            modal = Box("Dialog", safe, new Vector2(.17f,.27f), new Vector2(.83f,.745f), Color.white);
            modal.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            Box("Accent", modal, new Vector2(0,.986f), Vector2.one,mint);
            modalTitle = Label("", modal, new Vector2(.04f,.79f), new Vector2(.96f,.98f), 32, navy);
            modalBody = Label("", modal, new Vector2(.04f,.38f), new Vector2(.96f,.79f), 24, navy);
            options = Box("Choices", modal, new Vector2(.045f,.055f), new Vector2(.955f,.36f));
            EnsureBattlePanels();
            if (!FindFirstObjectByType<EventSystem>()) { var es = new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)); es.transform.SetParent(transform,false); }
            ApplyMedicalHUD(head, left, right, bottom);
        }
        public void EnsureBattlePanels()
        {
            if(!commands)commands=safe.Find("Commands") as RectTransform;
            EnsureHomeostasisButton();
            modalCanvasGroup=modal.GetComponent<CanvasGroup>();
            if(!modalCanvasGroup)modalCanvasGroup=modal.gameObject.AddComponent<CanvasGroup>();
            ApplyDialogMedicalSkin();
            // Remove the old reference image from the live HUD. It is a design reference only.
            var oldReference=modal.Find("Enemy Stats Reference");
            if(oldReference)oldReference.gameObject.SetActive(false);
            if(!diagnosticFrame)
            {
                diagnosticFrame=Box("Specimen Display Frame",modal,new Vector2(.045f,.17f),new Vector2(.455f,.88f),Color.white);
                ButtonFrame(diagnosticFrame);
                Box("Specimen Display",diagnosticFrame,new Vector2(.012f,.018f),new Vector2(.988f,.982f),new Color(.88f,.97f,1f));
                Box("Scanner Accent",diagnosticFrame,new Vector2(.025f,.965f),new Vector2(.975f,.985f),mint);
                Label("TARGET // PATHOGEN",diagnosticFrame,new Vector2(.06f,.87f),new Vector2(.94f,.95f),18,new Color(.02f,.25f,.42f),TextAlignmentOptions.Center);
                Label("BIO-SCAN  •  LIVE",diagnosticFrame,new Vector2(.06f,.035f),new Vector2(.94f,.105f),15,new Color(.02f,.25f,.42f),TextAlignmentOptions.Center);
            }
            diagnosticFrame.SetAsFirstSibling();
            if(!diagnosticArt)
            {
                diagnosticArt=Box("Diagnostic Monster Display",diagnosticFrame,new Vector2(.10f,.16f),new Vector2(.90f,.85f),Color.clear).GetComponent<UnityEngine.UI.Image>();
                diagnosticArt.preserveAspect=true;diagnosticArt.raycastTarget=false;
            }
            else if(diagnosticArt.transform.parent!=diagnosticFrame)
            {
                var artTransform=diagnosticArt.rectTransform;
                artTransform.SetParent(diagnosticFrame,false);
                artTransform.anchorMin=new Vector2(.10f,.16f);artTransform.anchorMax=new Vector2(.90f,.85f);
                artTransform.offsetMin=artTransform.offsetMax=Vector2.zero;
                diagnosticArt.color=Color.white;diagnosticArt.preserveAspect=true;diagnosticArt.raycastTarget=false;
            }
            diagnosticArt.gameObject.SetActive(false);
        }
        void SetModalLayout(DialogLayout layout)
        {
            modal.SetParent(layout==DialogLayout.Bottom?commands:safe,false);
            if(layout==DialogLayout.Diagnosis){modal.anchorMin=Vector2.zero;modal.anchorMax=Vector2.one;modal.offsetMin=modal.offsetMax=Vector2.zero;}
            else if(layout==DialogLayout.Bottom){modal.anchorMin=Vector2.zero;modal.anchorMax=Vector2.one;modal.offsetMin=modal.offsetMax=new Vector2(10,6);}
            else {modal.anchorMin=new Vector2(.17f,.27f);modal.anchorMax=new Vector2(.83f,.745f);modal.offsetMin=modal.offsetMax=Vector2.zero;}
            diagnosticFrame.gameObject.SetActive(layout==DialogLayout.Diagnosis);
            diagnosticArt.gameObject.SetActive(layout==DialogLayout.Diagnosis);
            diagnose.gameObject.SetActive(layout!=DialogLayout.Bottom);synthesize.gameObject.SetActive(layout!=DialogLayout.Bottom);restore.gameObject.SetActive(layout!=DialogLayout.Bottom);
            homeostasis.gameObject.SetActive(layout!=DialogLayout.Bottom);
            if(layout==DialogLayout.Diagnosis)
            {
                modalTitle.rectTransform.anchorMin=new Vector2(.51f,.79f);modalTitle.rectTransform.anchorMax=new Vector2(.95f,.94f);
                modalBody.rectTransform.anchorMin=new Vector2(.51f,.34f);modalBody.rectTransform.anchorMax=new Vector2(.95f,.76f);
                options.anchorMin=new Vector2(.51f,.12f);options.anchorMax=new Vector2(.95f,.27f);
                var frames=model.encounter==0?enemyOneFrames:enemyTwoFrames;
                if(frames!=null&&frames.Length>0)diagnosticArt.sprite=frames[0];
                modalTitle.color=mint;modalBody.color=navy;
            }
            else if(layout==DialogLayout.Bottom)
            {
                modalTitle.rectTransform.anchorMin=new Vector2(.03f,.63f);modalTitle.rectTransform.anchorMax=new Vector2(.30f,.94f);
                modalBody.rectTransform.anchorMin=new Vector2(.31f,.57f);modalBody.rectTransform.anchorMax=new Vector2(.97f,.95f);
                options.anchorMin=new Vector2(.03f,.08f);options.anchorMax=new Vector2(.97f,.50f);
            }
            else {diagnosticFrame.gameObject.SetActive(false);modalTitle.color=mint;modalBody.color=navy;modalTitle.rectTransform.anchorMin=new Vector2(.04f,.79f);modalTitle.rectTransform.anchorMax=new Vector2(.96f,.98f);modalBody.rectTransform.anchorMin=new Vector2(.04f,.38f);modalBody.rectTransform.anchorMax=new Vector2(.96f,.79f);options.anchorMin=new Vector2(.045f,.055f);options.anchorMax=new Vector2(.955f,.36f);}
        }
        void HideDialog(){StopCoroutine("FadeDialog");modal.gameObject.SetActive(false);diagnose.gameObject.SetActive(true);synthesize.gameObject.SetActive(true);restore.gameObject.SetActive(true);homeostasis.gameObject.SetActive(true);}
        IEnumerator FadeDialog(){modalCanvasGroup.alpha=0;while(modalCanvasGroup.alpha<1){modalCanvasGroup.alpha+=Time.deltaTime*8;yield return null;}modalCanvasGroup.alpha=1;}
        void Dialog(string title, string body, string[] labels, UnityEngine.Events.UnityAction[] actions)
        {
            var layout=title=="HASIL DIAGNOSIS"?DialogLayout.Diagnosis:
                (title=="SYNTHESIZE"||title=="TRIVIA RECHARGE"||title=="PATOGEN DINETRALISIR"||title=="MISI SELESAI"||
                 title.StartsWith("RECHARGE BERHASIL")||title.StartsWith("DATA TERSIMPAN")||
                 title.StartsWith("BENAR")||title.StartsWith("BELAJAR")
                    ?DialogLayout.Bottom:DialogLayout.Center);
            SetModalLayout(layout);modal.gameObject.SetActive(true); modalTitle.text = title; modalBody.text = body;
            for(int i = options.childCount-1; i >= 0; i--) { options.GetChild(i).gameObject.SetActive(false); Destroy(options.GetChild(i).gameObject); }
            for(int i = 0; i < labels.Length; i++) MedicalButton(Button(labels[i], options, new Vector2(i/(float)labels.Length+.008f,0),new Vector2((i+1)/(float)labels.Length-.008f,1),actions[i]));
            StartCoroutine(FadeDialog());
            Refresh();
        }
        void ApplyDialogMedicalSkin()
        {
            var panelImage=modal.GetComponent<UnityEngine.UI.Image>();
            if(panelImage){ButtonFrame(modal);panelImage.raycastTarget=true;}
            var choicesImage=options.GetComponent<UnityEngine.UI.Image>();
            if(!choicesImage)choicesImage=options.gameObject.AddComponent<UnityEngine.UI.Image>();
            PlainPanel(choicesImage,Color.clear);
            modalTitle.color=mint;modalTitle.fontStyle=FontStyles.Bold;
            modalBody.color=navy;modalBody.fontStyle=FontStyles.Bold;
        }
        public void ShowMenu()
        {
            StopAllCoroutines(); ClearVisualEffects(); actionAnimating=false; ShowTitle();
        }
        public void StartGame() { StartExploration(); }
        public void Diagnose()
        {
            if (actionAnimating || View!=GameView.Battle || modal.gameObject.activeSelf || !model.Diagnose()) return;
            StartCoroutine(DiagnoseSequence());
        }
        IEnumerator DiagnoseSequence()
        {
            actionAnimating=true; Refresh();
            Pose("diagnose");
            StartCoroutine(VisualEffect(0, enemyArt));
            PlayEffect(chargeSound);
            yield return new WaitForSeconds(ActionDuration);
            actionAnimating=false;
            Dialog("HASIL DIAGNOSIS", model.encounter == 0 ? "<b>PROFIL ANCAMAN 01</b>\nLipid Golem terdeteksi di pembuluh darah.\n\n<b>KOMPOSISI</b>\n70% lemak jenuh • 30% senyawa lain\n\n<b>RESPONS NUTRISI</b>\nOMEGA-3  ×2.0  //  AIR  ×1.0  //  SERAT  ×0.1" : "<b>PROFIL ANCAMAN 02</b>\nGluco Slime membawa residu gula berlebih.\n\n<b>KOMPOSISI</b>\n70% gula sederhana • 30% senyawa lain\n\n<b>RESPONS NUTRISI</b>\nSERAT  ×2.0  //  AIR  ×1.0  //  OMEGA-3  ×0.1", new[]{"LANJUT KE SYNTHESIZE"},new UnityEngine.Events.UnityAction[]{()=>{HideDialog(); Pose("idle"); log.text="Diagnosis selesai. Pilih Synthesize untuk menyerang.";Refresh();}});
        }
        public void Synthesize()
        {
            if(actionAnimating || View!=GameView.Battle || modal.gameObject.activeSelf || model.phase != BattlePhase.Player || !model.diagnosed || model.energy < 20) return;
            Dialog("SYNTHESIZE", "Pilih nutrisi sesuai hasil diagnosis. Setiap serangan memakai 20 MP.\nDamage dasar 20: cocok ×2, netral ×1, tidak cocok ×0,1.", new[]{"OMEGA-3","SERAT","AIR"},new UnityEngine.Events.UnityAction[]{()=>Attack(Nutrient.Omega3),()=>Attack(Nutrient.Fiber),()=>Attack(Nutrient.Water)});
        }
        public void Attack(Nutrient nutrient)
        {
            if(actionAnimating || View!=GameView.Battle || !model.CanAttack)return;
            HideDialog(); actionAnimating=true; Refresh();
            StartCoroutine(AttackSequence(nutrient));
        }
        IEnumerator AttackSequence(Nutrient nutrient)
        {
            Pose("synthesize");PlayEffect(chargeSound);
            StartCoroutine(VisualEffect(3, playerArt));
            yield return new WaitForSeconds(ActionDuration);
            Pose("nutrient_attack");
            PlayEffect(attackSound);
            yield return VisualEffect(1, playerArt, enemyArt);
            int damage=model.Attack(nutrient);
            if(damage<0){actionAnimating=false;Refresh();yield break;}
            Pose("nutrient_attack","hurt");
            StartCoroutine(VisualEffect(2, enemyArt));
            if(damage < 20) PlayEffect(enemyDefendSound);
            log.text = $"{(damage == 40 ? "AKURAT!" : damage == 20 ? "NETRAL" : "TIDAK COCOK")}  Damage {damage} • Energi -20 MP"; Refresh();
            yield return AfterAttack();
        }
        IEnumerator AfterAttack()
        {
            yield return new WaitForSeconds(.85f);
            if(model.phase == BattlePhase.Victory) { Victory(); yield break; }
            yield return EnemyTurn();
        }
        IEnumerator EnemyTurn()
        {
            actionAnimating=true;
            int protection = model.shield;
            Pose(model.shield > 0 ? "homeostasis_hit" : "hurt", "attack");
            StartCoroutine(VisualEffect(protection > 0 ? 5 : 2, playerArt));
            PlayEffect(enemyAttackSound);
            yield return new WaitForSeconds(.55f);
            int damage = model.EnemyAttack();
            if(protection>0) PlayEffect(defendSound);
            log.text = $"Musuh menyerang: -{damage} HP." + (protection>0 ? $" Homeostasis -{protection}% damage • sisa {model.shieldHits} serangan." : "");
            Refresh(); yield return new WaitForSeconds(.5f); actionAnimating=false;
            if(model.phase == BattlePhase.Defeat) { Pose("shutdown");PlayMusic(null);PlayEffect(loseSound); Dialog("MISI TERHENTI", "Integritas pembuluh darah habis. Coba lagi: gunakan diagnosis dan pulihkan energi sebelum terlambat.",new[]{"COBA LAGI","MENU"},new UnityEngine.Events.UnityAction[]{StartGame,ShowMenu}); }
            else { Pose(model.energy < 20 ? "low_energy" : "idle"); Refresh(); }
        }
        public void Restore()
        {
            if(actionAnimating || View!=GameView.Battle || modal.gameObject.activeSelf || !model.Restore()) return;
            currentQuestion = triviaDeck.Next(model.encounter);
            var q = currentQuestion; Pose("trivia_wait");
            Dialog("TRIVIA RECHARGE", q.prompt, q.options, new UnityEngine.Events.UnityAction[]{()=>Answer(0),()=>Answer(1),()=>Answer(2)});
        }
        public void Answer(int index)
        {
            if(actionAnimating || View!=GameView.Battle || model.phase!=BattlePhase.Trivia || currentQuestion==null || index<0 || index>=3)return;
            StartCoroutine(RestoreSequence(index));
        }
        IEnumerator RestoreSequence(int index)
        {
            var q = currentQuestion; bool correct = index == q.answer;
            if(!model.Answer(correct)) yield break;
            actionAnimating=true;HideDialog();Refresh();
            Pose("restore");
            StartCoroutine(VisualEffect(3, playerArt));
            PlayEffect(chargeSound);
            yield return new WaitForSeconds(ActionDuration);
            actionAnimating=false;
            Dialog(correct ? "RECHARGE BERHASIL  +40 MP" : "DATA TERSIMPAN  +10 MP", q.explanation + (correct ? "" : "\nJawaban benar: " + q.options[q.answer]) + "\nLanjutkan untuk menghadapi serangan musuh.", new[]{"LANJUT"},new UnityEngine.Events.UnityAction[]{()=>{if(model.phase != BattlePhase.Feedback)return;HideDialog();model.phase=BattlePhase.Enemy;Refresh();StartCoroutine(EnemyTurn());}});
        }
        void Victory()
        {
            actionAnimating=false;
            Pose("victory","defeat");
            defeated[model.encounter]=true;
            PlayMusic(null);PlayEffect(victorySound);
            bool complete=defeated[0]&&defeated[1];
            if(complete) model.phase=BattlePhase.Complete;
            Dialog(complete?"MISI SELESAI":"PATOGEN DINETRALISIR",complete?$"Dua patogen berhasil dinetralisir.\nSisa integritas: {model.hp}/100 • Kuis benar: {model.correct}/{model.answered}\nKamu dapat kembali menjelajahi dunia.":"Patogen ini telah dikalahkan. Kembali ke peta dan cari musuh lainnya.\nBonus: +25 HP dan +20 MP.",new[]{"KEMBALI KE PETA"},new UnityEngine.Events.UnityAction[]{()=>{model.hp=Mathf.Min(100,model.hp+25);model.energy=Mathf.Min(60,model.energy+20);ReturnToWorld();}});
        }
        void Refresh()
        {
            playerStats.text=$"NANOBOT <size=75%>• SHIELD {model.shield}% ({model.shieldHits})</size>\n<size=85%>HP {model.hp}/100    MP {model.energy}/60</size>";
            enemyStats.text=$"{(model.encounter==0?"LIPID GOLEM":"GLUCO SLIME")}\nHP {model.enemyHp}/{model.MaxEnemyHp}";
            Fill(hpBar,model.hp/100f);Fill(mpBar,model.energy/60f);Fill(enemyBar,model.enemyHp/(float)model.MaxEnemyHp);
            status.text=$"LOKASI {model.encounter+1} / 2\n" + (model.phase==BattlePhase.Enemy?"GILIRAN MUSUH":model.phase==BattlePhase.Trivia?"TRIVIA":$"GILIRAN {model.turn}");
            bool available=!actionAnimating&&!modal.gameObject.activeSelf;
            diagnose.interactable=available&&(model.phase==BattlePhase.Diagnose||model.phase==BattlePhase.Player);
            synthesize.interactable=available&&model.phase==BattlePhase.Player&&model.diagnosed&&model.energy>=20;
            restore.interactable=available&&model.phase==BattlePhase.Player&&model.energy<60;
            homeostasis.interactable=available&&model.phase==BattlePhase.Player&&model.diagnosed&&model.energy>=homeostasisCost&&model.shieldHits==0;
        }
    }
}
