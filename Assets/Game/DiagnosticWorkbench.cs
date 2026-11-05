using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VeinVanguard
{
    public partial class BattleManager
    {
        public Sprite[] learningSprites;
        RectTransform diagnosisRoot, learningRoot, scanLine, formulaGhost, formulaChargeFill;
        UnityEngine.UI.Image reactorGlow, energyConduit;
        UnityEngine.UI.Image[] formulaSourceIcons, formulaSocketIcons;
        UnityEngine.UI.Button diagnosisClose, learningClose, learningFire;
        UnityEngine.UI.Button[] formulaPalette, formulaSockets;
        TextMeshProUGUI diagnosisHeading, diagnosisFeedback, learningHeading, learningFeedback, formulaChargeLabel;
        TextMeshProUGUI[] formulaSourceLabels, formulaSocketLabels, formulaSourceReminders;
        TextMeshProUGUI[] sourceTitles,sourceCategories;
        readonly FormulaIcon[] formulaSlots = { FormulaIcon.None, FormulaIcon.None };
        FormulaIcon selectedFormulaIcon = FormulaIcon.None, draggingFormulaIcon = FormulaIcon.None;
        Coroutine formulaChargeRoutine;
        bool formulaCharged;
        float formulaCharge;
        readonly Vector2[] arenaLayout = new Vector2[8];
        bool arenaDocked;

        Sprite LearningSprite(string name)
        {
            if (learningSprites != null)
                foreach (var sprite in learningSprites) if (sprite && sprite.name == name) return sprite;
            return null;
        }
        UnityEngine.UI.Image LearningIcon(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var image = Box(name, parent, min, max, Color.white).GetComponent<UnityEngine.UI.Image>();
            image.sprite = LearningSprite(name); image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }
        void EnsureLearningWorkbench()
        {
            if (learningRoot) return;
            diagnosisRoot = Box("Diagnosis Results", modal, Vector2.zero, Vector2.one);
            diagnosisHeading = Label("DIAGNOSE", diagnosisRoot, new Vector2(.02f,.82f), new Vector2(.82f,.985f), 27, navy);
            diagnosisClose = Button("TUTUP", diagnosisRoot, new Vector2(.87f,.83f), new Vector2(.985f,.985f), CloseLearningWorkbench);
            MedicalButton(diagnosisClose);
            diagnosisFeedback = Label("", diagnosisRoot, new Vector2(.02f,.015f), new Vector2(.98f,.17f), 22, navy);
            formulaSourceIcons = new UnityEngine.UI.Image[2]; formulaSourceLabels = new TextMeshProUGUI[2];
            for (int i=0;i<2;i++)
            {
                var record = Box("Source " + (i==0?"A":"B"), diagnosisRoot, new Vector2(.02f+i*.49f,.19f), new Vector2(.49f+i*.49f,.80f), new Color(.88f,.97f,1));
                formulaSourceIcons[i] = LearningIcon("sample_syrup", record, new Vector2(.025f,.22f), new Vector2(.175f,.78f));
                formulaSourceLabels[i] = Label("", record, new Vector2(.19f,.04f), new Vector2(.98f,.96f), 24, navy);
                formulaSourceLabels[i].textWrappingMode = TextWrappingModes.Normal;
                formulaSourceLabels[i].fontSizeMin = 21;
            }
            scanLine = LearningIcon("scan_line", enemyArt.transform, new Vector2(.05f,.1f), new Vector2(.95f,.1f)).rectTransform;
            scanLine.sizeDelta = new Vector2(0,14); scanLine.gameObject.SetActive(false);
            learningRoot = Box("Plasma Icon Workbench", modal, Vector2.zero, Vector2.one);
            learningHeading = Label("SYNTHESIZE / FUSI PLASMA", learningRoot, new Vector2(.015f,.83f), new Vector2(.79f,.995f), 27, navy);
            learningClose = Button("TUTUP", learningRoot, new Vector2(.87f,.83f), new Vector2(.985f,.985f), CloseLearningWorkbench);
            MedicalButton(learningClose);
            learningFeedback = Label("", learningRoot, new Vector2(.80f,.06f), new Vector2(.985f,.29f), 19, navy);
            learningFeedback.textWrappingMode = TextWrappingModes.Normal; learningFeedback.fontSizeMin = 17;
            formulaSourceReminders = new TextMeshProUGUI[2];
            for (int i=0;i<2;i++)
                formulaSourceReminders[i] = Label("", learningRoot, new Vector2(.015f+i*.39f,.008f), new Vector2(.395f+i*.39f,.125f), 20, navy);
            var weaponArea = Box("Plasma Weapon Area", learningRoot, new Vector2(.35f,.08f), new Vector2(.79f,.80f), new Color(.90f,.97f,1));
            var weapon = Box("Plasma Weapon Fit", weaponArea, Vector2.zero, Vector2.one);
            var aspect = weapon.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            aspect.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent; aspect.aspectRatio = 1000f/600f;
            LearningIcon("plasma_weapon", weapon, Vector2.zero, Vector2.one);
            reactorGlow = LearningIcon("scanner_reticle_active", weapon, new Vector2(.25f,.296667f), new Vector2(.45f,.63f));
            reactorGlow.color = new Color(0,.85f,1,0);
            energyConduit = Box("Plasma Energy Conduit", weapon, new Vector2(.4f,.385f), new Vector2(.87f,.39f), mint).GetComponent<UnityEngine.UI.Image>();
            formulaSockets = new UnityEngine.UI.Button[2]; formulaSocketIcons = new UnityEngine.UI.Image[2]; formulaSocketLabels = new TextMeshProUGUI[2];
            for (int i=0;i<2;i++)
            {
                int socket=i; float left=i==0?.48f:.697f;
                var button = Button("Kosong", weapon, new Vector2(left,.40f), new Vector2(left+.203f,.65f), ()=>TapFormulaSocket(socket));
                button.name = "Plasma Socket " + (i==0?"A":"B"); MedicalButton(button);
                formulaSocketLabels[i] = button.GetComponentInChildren<TextMeshProUGUI>();
                formulaSocketLabels[i].rectTransform.anchorMin=new Vector2(.02f,.52f); formulaSocketLabels[i].rectTransform.anchorMax=new Vector2(.98f,.99f);
                formulaSocketLabels[i].fontSizeMax=16; formulaSocketLabels[i].fontSizeMin=14;
                formulaSocketIcons[i] = LearningIcon("formula_added", button.transform, new Vector2(.23f,.04f), new Vector2(.77f,.55f));
                var drop = button.gameObject.AddComponent<FormulaSocketDrop>(); drop.owner = this; drop.socket = i;
                formulaSockets[i] = button;
            }
            formulaChargeLabel = Label("", learningRoot, new Vector2(.80f,.62f), new Vector2(.985f,.80f), 21, navy, TextAlignmentOptions.Center);
            formulaChargeLabel.textWrappingMode = TextWrappingModes.Normal; formulaChargeLabel.fontSizeMin=18;
            var track = Box("Formula Charge Track", learningRoot, new Vector2(.81f,.56f), new Vector2(.975f,.59f), new Color(.77f,.89f,.94f));
            formulaChargeFill = Box("Formula Charge Fill", track, Vector2.zero, Vector2.one, mint);
            formulaPalette = new UnityEngine.UI.Button[6];
            for (int i=0;i<6;i++)
            {
                var icon = (FormulaIcon)i; int column=i%3, row=i/3;
                var button = Button(UniversalFormula.Title(icon), learningRoot, new Vector2(.015f+column*.108f,.49f-row*.33f), new Vector2(.116f+column*.108f,.81f-row*.33f), ()=>SelectFormulaIcon(icon));
                button.name = "Formula Icon " + icon; MedicalButton(button);
                var label = button.GetComponentInChildren<TextMeshProUGUI>(); label.rectTransform.anchorMin = new Vector2(.02f,.015f); label.rectTransform.anchorMax = new Vector2(.98f,.475f); label.fontSizeMax = 18; label.fontSizeMin = 16;
                label.textWrappingMode = TextWrappingModes.Normal;
                LearningIcon(UniversalFormula.IconName(icon), button.transform, new Vector2(.10f,.40f), new Vector2(.90f,.99f));
                var drag = button.gameObject.AddComponent<FormulaIconDrag>(); drag.owner = this; drag.icon = icon;
                formulaPalette[i] = button;
            }
            learningFire = Button("TEMBAKKAN\nPLASMA / 20 MP", learningRoot, new Vector2(.80f,.31f), new Vector2(.985f,.53f), FireLearningSynthesis);
            learningFire.name = "Fire Plasma"; MedicalButton(learningFire);
            var fireLabel=learningFire.GetComponentInChildren<TextMeshProUGUI>(); fireLabel.fontSizeMax=21;
            fireLabel.rectTransform.anchorMin=new Vector2(.10f,.05f); fireLabel.rectTransform.anchorMax=new Vector2(.90f,.95f);
            foreach (var reminder in formulaSourceReminders) reminder.transform.SetAsLastSibling();
            formulaGhost = Box("Formula Drag Ghost", learningRoot, new Vector2(.5f,.5f), new Vector2(.5f,.5f), new Color(.85f,1,1,.94f));
            formulaGhost.sizeDelta = new Vector2(96,96); LearningIcon("formula_added", formulaGhost, new Vector2(.14f,.14f), new Vector2(.86f,.86f));
            var ghostGroup = formulaGhost.gameObject.AddComponent<CanvasGroup>(); ghostGroup.blocksRaycasts = false; ghostGroup.interactable = false;
            formulaGhost.gameObject.SetActive(false);
            diagnosisRoot.gameObject.SetActive(false); learningRoot.gameObject.SetActive(false);
            StyleLearningWorkbench();
        }
        void ShowLearningWorkbench(bool synthesizeMode)
        {
            if (actionAnimating || View!=GameView.Battle || (model.phase!=BattlePhase.Diagnose && model.phase!=BattlePhase.Player)) return;
            if (synthesizeMode && !model.CanAttack) return;
            SetModalLayout(DialogLayout.Bottom); EnsureLearningWorkbench();
            modal.SetParent(safe,false); modal.anchorMin=new Vector2(.025f,.025f); modal.anchorMax=new Vector2(.975f,.375f); modal.offsetMin=modal.offsetMax=Vector2.zero;
            modalTitle.gameObject.SetActive(false); modalBody.gameObject.SetActive(false); options.gameObject.SetActive(false); diagnosticFrame.gameObject.SetActive(false);
            diagnosisRoot.gameObject.SetActive(!synthesizeMode); learningRoot.gameObject.SetActive(synthesizeMode);
            modal.gameObject.SetActive(true); modalCanvasGroup.alpha=1; modal.SetAsLastSibling(); SetLearningArena(true);
            RefreshLearningWorkbench(); Refresh();
            if (synthesizeMode && UniversalFormula.Complete(formulaSlots[0],formulaSlots[1])) RestartFormulaCharge();
        }
        void SetLearningArena(bool docked)
        {
            if(arenaDocked==docked)return;
            arenaDocked=docked;
            RequestBattleFraming(docked?BattleFraming.Drawer:BattleFraming.Idle);
        }
        void CancelFormulaCharge()
        {
            if (formulaChargeRoutine!=null) StopCoroutine(formulaChargeRoutine);
            formulaChargeRoutine=null; formulaCharged=false; formulaCharge=0;
        }
        void HideLearningPanels()
        {
            CancelFormulaCharge(); EndFormulaDrag(null);
            if (learningRoot) learningRoot.gameObject.SetActive(false);
            if (diagnosisRoot) diagnosisRoot.gameObject.SetActive(false);
            if (scanLine) scanLine.gameObject.SetActive(false);
            SetLearningArena(false);
            if (modalTitle) modalTitle.gameObject.SetActive(true);
            if (modalBody) modalBody.gameObject.SetActive(true);
            if (options) options.gameObject.SetActive(true);
        }
        void ResetLearningState()
        {
            formulaSlots[0]=formulaSlots[1]=FormulaIcon.None; selectedFormulaIcon=FormulaIcon.None;
            if (scanLine) scanLine.gameObject.SetActive(false);
            HideLearningPanels();
        }
        void CloseLearningWorkbench()
        {
            if (actionAnimating) return;
            var trigger=learningRoot&&learningRoot.gameObject.activeSelf?synthesize:diagnose;
            HideDialog(); Pose("idle"); log.text=model.diagnosed?"Hasil Diagnose tersimpan. Synthesize membuka senjata plasma.":"Gunakan Diagnose untuk membuka kedua sampel."; Refresh();
            if(EventSystem.current&&trigger.IsInteractable())EventSystem.current.SetSelectedGameObject(trigger.gameObject);
        }
        public void ScanLearningSamples()
        {
            if (actionAnimating || View!=GameView.Battle || model.phase!=BattlePhase.Diagnose || model.diagnosed || !diagnosisRoot || !diagnosisRoot.gameObject.activeInHierarchy) return;
            StartCoroutine(ScanAllLearningSamples());
        }
        IEnumerator ScanAllLearningSamples()
        {
            actionAnimating=true; Pose("diagnose"); PlayEffect(chargeSound); scanLine.gameObject.SetActive(true);
            RefreshLearningWorkbench(); StartCoroutine(VisualEffect(0,enemyArt));
            for (float t=0;t<.35f;t+=Time.unscaledDeltaTime)
            {
                float y=Mathf.Lerp(.12f,.88f,t/.35f); scanLine.anchorMin=new Vector2(.05f,y); scanLine.anchorMax=new Vector2(.95f,y); yield return null;
            }
            model.DiagnoseAll(); scanLine.gameObject.SetActive(false); actionAnimating=false; Pose("idle"); RefreshLearningWorkbench(); Refresh();
        }
        bool CanConfigureFormula() => !actionAnimating && View==GameView.Battle && model.diagnosed && model.phase==BattlePhase.Player && learningRoot && learningRoot.gameObject.activeInHierarchy;
        public void SelectFormulaIcon(FormulaIcon icon)
        {
            if (!CanConfigureFormula() || !UniversalFormula.IsValid(icon) || draggingFormulaIcon!=FormulaIcon.None) return;
            selectedFormulaIcon=selectedFormulaIcon==icon?FormulaIcon.None:icon; RefreshLearningWorkbench();
        }
        void TapFormulaSocket(int socket)
        {
            if (!CanConfigureFormula() || socket<0 || socket>1 || draggingFormulaIcon!=FormulaIcon.None) return;
            if (selectedFormulaIcon!=FormulaIcon.None) InstallFormulaIcon(selectedFormulaIcon,socket);
            else if (formulaSlots[socket]!=FormulaIcon.None) { formulaSlots[socket]=FormulaIcon.None; RestartFormulaCharge(); }
        }
        public void InstallFormulaIcon(FormulaIcon icon,int socket)
        {
            if (!CanConfigureFormula() || socket<0 || socket>1 || !UniversalFormula.IsValid(icon) || (draggingFormulaIcon!=FormulaIcon.None && draggingFormulaIcon!=icon)) return;
            formulaSlots[socket]=icon; selectedFormulaIcon=FormulaIcon.None; EndFormulaDrag(null); RestartFormulaCharge();
        }
        void RestartFormulaCharge()
        {
            CancelFormulaCharge(); Pose("idle"); RefreshLearningWorkbench();
            if (CanConfigureFormula() && UniversalFormula.Complete(formulaSlots[0],formulaSlots[1])) formulaChargeRoutine=StartCoroutine(ChargeFormula());
        }
        IEnumerator ChargeFormula()
        {
            formulaCharged=false; formulaCharge=0; PlayEffect(chargeSound); Pose("synthesize");
            while (formulaCharge<1)
            {
                if (!CanConfigureFormula() || !UniversalFormula.Complete(formulaSlots[0],formulaSlots[1])) { formulaChargeRoutine=null; yield break; }
                bool firstFrame=formulaCharge==0;
                formulaCharge=Mathf.Min(1,formulaCharge+Time.unscaledDeltaTime/.65f);
                if(firstFrame)RefreshLearningWorkbench();else RefreshFormulaChargeVisual();
                yield return null;
            }
            formulaCharged=true; formulaChargeRoutine=null; Pose("idle"); RefreshLearningWorkbench();
        }
        public bool BeginFormulaDrag(FormulaIcon icon,PointerEventData pointer)
        {
            if (pointer==null || !CanConfigureFormula() || !UniversalFormula.IsValid(icon) || draggingFormulaIcon!=FormulaIcon.None) return false;
            draggingFormulaIcon=icon; selectedFormulaIcon=FormulaIcon.None;
            var ghostImages=formulaGhost.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            ghostImages[ghostImages.Length-1].sprite=LearningSprite(UniversalFormula.IconName(icon));
            formulaGhost.gameObject.SetActive(true); formulaGhost.SetAsLastSibling(); MoveFormulaDrag(pointer); RefreshLearningWorkbench(); return true;
        }
        public void MoveFormulaDrag(PointerEventData pointer)
        {
            if (pointer==null || draggingFormulaIcon==FormulaIcon.None || !formulaGhost) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(learningRoot,pointer.position,pointer.pressEventCamera,out Vector2 local))
            {
                var rect=learningRoot.rect; local.x=Mathf.Clamp(local.x,rect.xMin+48,rect.xMax-48); local.y=Mathf.Clamp(local.y,rect.yMin+48,rect.yMax-48); formulaGhost.anchoredPosition=local;
            }
        }
        public void EndFormulaDrag(PointerEventData pointer)
        {
            draggingFormulaIcon=FormulaIcon.None;
            if (formulaGhost) formulaGhost.gameObject.SetActive(false);
            RefreshLearningWorkbench();
        }
        public void FireLearningSynthesis()
        {
            if (!CanConfigureFormula() || !formulaCharged || draggingFormulaIcon!=FormulaIcon.None || !model.CanSynthesizeFormula(formulaSlots[0],formulaSlots[1])) return;
            AttackFormula(formulaSlots[0],formulaSlots[1]);
        }
        void RefreshFormulaChargeVisual()
        {
            if (!learningRoot) return;
            formulaChargeFill.anchorMax=new Vector2(formulaCharge,1);
            reactorGlow.color=new Color(0,.85f,1,formulaCharge*.6f); reactorGlow.rectTransform.localEulerAngles=new Vector3(0,0,-360*formulaCharge);
            energyConduit.color=new Color(0,.85f,1,formulaCharge);
            formulaChargeLabel.text=$"REAKTOR / {Mathf.RoundToInt(formulaCharge*100)}%\n{(formulaCharged?"CHARGED":formulaCharge>0?"FUSION...":"STANDBY")}";
            formulaChargeFill.GetComponent<UnityEngine.UI.Image>().color=formulaCharged?Green:Cyan;
        }
        void RefreshLearningWorkbench()
        {
            if (!learningRoot || (!learningRoot.gameObject.activeSelf && !diagnosisRoot.gameObject.activeSelf)) return;
            if (diagnosisRoot.gameObject.activeSelf)
            {
                diagnosisHeading.text="DIAGNOSE  //  "+(model.encounter==0?"LIPID GOLEM":"GLUCO SLIME");
                diagnosisClose.interactable=!actionAnimating;
                diagnosisFeedback.text=actionAnimating?"Memindai musuh: membuka kedua rekaman sumber A dan B...":"Kedua rekaman tersimpan. Tutup untuk kembali ke pilihan aksi.";
                for (int i=0;i<2;i++)
                {
                    var source=CombatSamples.Get(model.encounter,i);
                    formulaSourceIcons[i].gameObject.SetActive(model.diagnosed); formulaSourceIcons[i].sprite=LearningSprite(source.icon);
                    sourceTitles[i].text=model.diagnosed?source.title:(i==0?"A":"B")+" / memindai...";
                    sourceCategories[i].transform.parent.gameObject.SetActive(model.diagnosed);
                    sourceCategories[i].text=source.kind==SampleClass.FreeSugar?"Gula bebas":"Lemak jenuh";
                    formulaSourceLabels[i].text=model.diagnosed?source.record:"";
                }
                return;
            }
            bool configure=CanConfigureFormula();
            learningHeading.text="SYNTHESIZE / FUSI PLASMA  //  "+(model.encounter==0?"LIPID GOLEM":"GLUCO SLIME");
            learningClose.interactable=!actionAnimating;
            for (int i=0;i<2;i++)
            {
                formulaSourceReminders[i].text=CombatSamples.Get(model.encounter,i).title+" → soket "+(i==0?"A":"B");
                formulaSockets[i].interactable=configure;
                formulaSocketIcons[i].sprite=LearningSprite(UniversalFormula.IconName(formulaSlots[i])); formulaSocketIcons[i].gameObject.SetActive(formulaSlots[i]!=FormulaIcon.None);
                formulaSocketLabels[i].text=i==0?"A":"B";
                var shape=formulaSockets[i].GetComponent<OutlinedUiGraphic>();
                shape.dashed=formulaSlots[i]==FormulaIcon.None;shape.pulse=shape.dashed&&configure;
                formulaSockets[i].image.SetVerticesDirty();
            }
            for (int i=0;i<6;i++) { formulaPalette[i].interactable=configure; formulaPalette[i].GetComponent<UiControlFeedback>().selected=selectedFormulaIcon==(FormulaIcon)i; }
            learningFire.interactable=configure && draggingFormulaIcon==FormulaIcon.None && formulaCharged && model.CanSynthesizeFormula(formulaSlots[0],formulaSlots[1]);
            learningFire.GetComponentInChildren<TextMeshProUGUI>().text=formulaCharge>0&&!formulaCharged?"FUSION\nMENGISI DAYA...":"TEMBAKKAN\nPLASMA / 20 MP";
            learningFeedback.text=draggingFormulaIcon!=FormulaIcon.None?"Lepaskan ikon ke soket A atau B.":selectedFormulaIcon!=FormulaIcon.None?"Pilih soket A/B: "+UniversalFormula.Title(selectedFormulaIcon)+".":formulaCharge>0&&!formulaCharged?"Dua ikon terpasang. Mengisi daya otomatis...":model.energy<20?"Perlu 20 MP. Tutup lalu gunakan Restore.":formulaCharged?"Charge bukan nilai jawaban. Siap ditembakkan.":"Seret ikon ke soket A/B, atau ketuk ikon lalu soket.";
            RefreshFormulaChargeVisual();
        }
    }
}
