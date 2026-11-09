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
        FormulaIcon[] activePalette = UniversalFormula.Palette(0);
        readonly FormulaIcon[] formulaSlots = { FormulaIcon.None, FormulaIcon.None };
        FormulaIcon selectedFormulaIcon = FormulaIcon.None, draggingFormulaIcon = FormulaIcon.None;
        Coroutine formulaChargeRoutine;
        bool formulaCharged;
        float formulaCharge;
        readonly Vector2[] arenaLayout = new Vector2[8];
        bool arenaDocked;
        RectTransform assemblyContent,assemblyViewport,paletteContent,assemblyWeaponArea,infoPanel;
        UnityEngine.UI.ScrollRect assemblyScroll,paletteScroll,infoScroll;
        TextMeshProUGUI formulaInfoHeading,formulaInfoBody;
        RectTransform moduleRail;
        TextMeshProUGUI[] moduleChoiceLabels;
        Sprite moduleWeaponSprite;
        readonly Coroutine[] moduleInstallRoutines = new Coroutine[2];
        FormulaIcon inspectedFormulaIcon=FormulaIcon.None;
        public bool AssemblyScrolling { get; private set; }

        UnityEngine.UI.ScrollRect AssemblyScrollRect(string name,Transform parent,out RectTransform content)
        {
            var viewport=Box(name,parent,Vector2.zero,Vector2.one,new Color(0,0,0,.005f));
            viewport.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            content=Box(name+" Content",viewport,new Vector2(0,1),new Vector2(0,1));content.pivot=new Vector2(0,1);
            var scroll=viewport.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();scroll.viewport=viewport;scroll.content=content;
            scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;scroll.inertia=false;scroll.horizontal=false;scroll.vertical=false;
            return scroll;
        }
        void EnsureAssemblyLayout()
        {
            if(assemblyScroll)return;
            assemblyWeaponArea=learningRoot.Find("Plasma Weapon Area") as RectTransform;
            assemblyScroll=AssemblyScrollRect("Assembly Viewport",learningRoot,out assemblyContent);
            assemblyViewport=(RectTransform)assemblyScroll.transform;
            paletteScroll=AssemblyScrollRect("Palette Viewport",assemblyContent,out paletteContent);
            foreach(var button in formulaPalette)button.transform.SetParent(paletteContent,false);
            assemblyWeaponArea.SetParent(assemblyContent,false);learningFire.transform.SetParent(assemblyContent,false);
            infoPanel=Box("Formula Information",assemblyContent,Vector2.zero,Vector2.one);
            formulaInfoHeading=Label("INFO FORMULA",infoPanel,new Vector2(0,.86f),Vector2.one,20,Paper);
            var hint=NamedLabel("Info Scroll Hint",infoPanel,"Geser teks / PgUp-PgDn",new Vector2(0,0),new Vector2(1,.08f),14);
            hint.margin=Vector4.zero;
            RectTransform content;infoScroll=AssemblyScrollRect("Info Viewport",infoPanel,out content);infoScroll.vertical=true;
            formulaInfoBody=Label("Ketuk kartu formula untuk membaca faktanya.",content,Vector2.zero,Vector2.one,18,Paper);
            formulaInfoBody.richText=false;formulaInfoBody.enableAutoSizing=false;formulaInfoBody.textWrappingMode=TextWrappingModes.Normal;
            formulaInfoBody.rectTransform.pivot=new Vector2(.5f,1);formulaInfoBody.rectTransform.anchorMin=new Vector2(0,1);formulaInfoBody.rectTransform.anchorMax=Vector2.one;
            content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(0,1);
            formulaInfoBody.transform.SetParent(infoScroll.viewport,false);
            Destroy(content.gameObject);infoScroll.content=formulaInfoBody.rectTransform;
            var bodyFit=formulaInfoBody.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();bodyFit.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            learningFeedback.gameObject.SetActive(false);
            learningClose.transform.SetAsLastSibling();formulaGhost.SetAsLastSibling();
        }
        void EnsureModulePorts()
        {
            EnsureAssemblyLayout();
            if (moduleRail) return;
            moduleRail = Box("Module Rail", assemblyWeaponArea, Vector2.zero, Vector2.one);
            moduleChoiceLabels = new TextMeshProUGUI[2];
            for (int i = 0; i < 2; i++)
            {
                var port = formulaSockets[i];
                port.transform.SetParent(moduleRail, false);
                formulaSourceReminders[i].transform.SetParent(port.transform, false);
                moduleChoiceLabels[i] = NamedLabel("Module Choice " + i, port.transform, "Pilih modul", Vector2.zero, Vector2.one, 20);
                moduleChoiceLabels[i].fontSizeMin = 16;
                moduleChoiceLabels[i].alignment = TextAlignmentOptions.MidlineLeft;
                var connector = Box("Module Connector " + i, assemblyWeaponArea, Vector2.zero, Vector2.one, Edge);
                connector.SetAsFirstSibling();
            }
            var weapon = assemblyWeaponArea.Find("Plasma Weapon Fit");
            var weaponImage = weapon.Find("plasma_weapon").GetComponent<UnityEngine.UI.Image>();
            var source = LearningSprite("plasma_weapon");
            // The source has transparent rows above and below its measured artwork bounds.
            var crop = new Rect(source.rect.x, source.rect.y + source.rect.height * .05f, source.rect.width, source.rect.height * (440f/600f));
            if (!moduleWeaponSprite)
            {
                moduleWeaponSprite = Sprite.Create(source.texture, crop, Vector2.one * .5f, source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                moduleWeaponSprite.name = "Plasma Weapon Display";
            }
            weaponImage.sprite = moduleWeaponSprite;
            Place(reactorGlow.rectTransform,new Vector2(.25f,148f/440f),new Vector2(.45f,348f/440f));
            Place(energyConduit.rectTransform,new Vector2(.4f,201f/440f),new Vector2(.87f,204f/440f));
            for (int i = 0; i < 2; i++)
            {
                var old = weapon.Find("Socket Identity " + i);
                if (old) old.gameObject.SetActive(false);
            }
        }

        void LayoutSynthesisDrawer(float logicalWidth,float logicalHeight)
        {
            EnsureModulePorts();
            float w = learningRoot.rect.width - 24, h = learningRoot.rect.height - 50;
            float scale = learningRoot.GetComponentInParent<Canvas>(true).scaleFactor;
            float touch = Mathf.Max(80, MinimumTouchCanvasUnits(touchDensity, scale));
            bool compact = logicalWidth < 600;
            float actionWidth = Mathf.Max(touch + 16, w * .18f);
            float viewportWidth = Mathf.Max(1, w - actionWidth - 16);
            float contentWidth = Mathf.Max(viewportWidth, compact ? touch * 10 : viewportWidth);
            float contentHeight = Mathf.Max(h, touch + 110);
            Fixed(assemblyViewport, new Vector2(0,1), new Vector2(0,1), new Vector2(12,-38), new Vector2(viewportWidth,h));
            Fixed(assemblyContent, new Vector2(0,1), new Vector2(0,1), Vector2.zero, new Vector2(contentWidth,contentHeight));
            assemblyScroll.horizontal = contentWidth > viewportWidth + 1;
            assemblyScroll.vertical = contentHeight > h + 1;
            assemblyScroll.horizontalNormalizedPosition = 0; assemblyScroll.verticalNormalizedPosition = 1;

            float paletteWidth = contentWidth * .44f, weaponWidth = contentWidth - paletteWidth - 20;
            float infoHeight = Mathf.Clamp(h * .25f, 58, 78);
            float paletteHeight = h - infoHeight - 10;
            float cellHeight = Mathf.Max(touch, (paletteHeight - 6) * .5f);
            float cellWidth = (paletteWidth - 12) / 3;
            Fixed((RectTransform)paletteScroll.transform, new Vector2(0,1), new Vector2(0,1), Vector2.zero, new Vector2(paletteWidth,paletteHeight));
            Fixed(paletteContent, new Vector2(0,1), new Vector2(0,1), Vector2.zero, new Vector2(paletteWidth,cellHeight*2+6));
            paletteScroll.vertical = cellHeight * 2 + 6 > paletteHeight + 1;
            paletteScroll.verticalNormalizedPosition = 1;
            for (int i = 0; i < 6; i++)
                Fixed((RectTransform)formulaPalette[i].transform, new Vector2(0,1), new Vector2(0,1), new Vector2(i%3*(cellWidth+6),-i/3*(cellHeight+6)), new Vector2(cellWidth,cellHeight));
            Fixed(infoPanel, new Vector2(0,1), new Vector2(0,1), new Vector2(0,-paletteHeight-10), new Vector2(paletteWidth,infoHeight));
            Fixed(formulaInfoHeading.rectTransform, new Vector2(0,1), new Vector2(0,1), Vector2.zero, new Vector2(paletteWidth-110,24));
            formulaInfoHeading.margin = Vector4.zero;
            formulaInfoHeading.fontSizeMax = 18; formulaInfoHeading.fontSizeMin = 16;
            var hint = infoPanel.Find("Info Scroll Hint") as RectTransform;
            Fixed(hint, new Vector2(1,1), new Vector2(1,1), Vector2.zero, new Vector2(100,20));
            hint.GetComponent<TextMeshProUGUI>().text = "↕ / PgUp·Dn";
            Fixed((RectTransform)infoScroll.transform, new Vector2(0,1), new Vector2(0,1), new Vector2(0,-24), new Vector2(paletteWidth,infoHeight-24));

            Fixed(assemblyWeaponArea, new Vector2(0,1), new Vector2(0,1), new Vector2(paletteWidth+20,0), new Vector2(weaponWidth,contentHeight));
            Shape(assemblyWeaponArea.GetComponent<UnityEngine.UI.Image>(),new Color(22f/255,41f/255,66f/255),12,0,null,false);
            var weapon = assemblyWeaponArea.Find("Plasma Weapon Fit") as RectTransform;
            weapon.GetComponent<UnityEngine.UI.AspectRatioFitter>().enabled = false;
            float gunHeight = Mathf.Max(70,contentHeight-touch-24);
            float gunWidth = Mathf.Min(weaponWidth-16,gunHeight*1000f/440f);
            Fixed(weapon, new Vector2(.5f,1), new Vector2(.5f,1), Vector2.zero, new Vector2(gunWidth,gunWidth*.44f));
            Fixed(moduleRail, new Vector2(0,0), new Vector2(0,0), new Vector2(8,8), new Vector2(weaponWidth-16,touch));
            float portWidth = (weaponWidth-28)*.5f;
            for (int i = 0; i < 2; i++)
            {
                var port = (RectTransform)formulaSockets[i].transform;
                Fixed(port, new Vector2(0,0), new Vector2(0,0), new Vector2(i*(portWidth+12),0), new Vector2(portWidth,touch));
                float iconRight = Mathf.Min(.45f,touch/portWidth);
                Place(formulaSocketIcons[i].rectTransform,new Vector2(.025f,.12f),new Vector2(iconRight-.025f,.88f));
                Place(formulaSocketLabels[i].rectTransform,new Vector2(.025f,.05f),new Vector2(iconRight-.025f,.95f));
                formulaSocketLabels[i].fontSizeMax = 32; formulaSocketLabels[i].fontSizeMin = 28;
                Place(formulaSourceReminders[i].rectTransform,new Vector2(iconRight,.57f),new Vector2(.98f,.96f));
                formulaSourceReminders[i].fontSizeMax = 18; formulaSourceReminders[i].fontSizeMin = 15;
                Place(moduleChoiceLabels[i].rectTransform,new Vector2(iconRight,.06f),new Vector2(.98f,.56f));
                var connector = assemblyWeaponArea.Find("Module Connector " + i) as RectTransform;
                Fixed(connector,new Vector2(0,0),new Vector2(.5f,0),new Vector2(8+(i+.5f)*portWidth+i*12,touch+8),new Vector2(2,14));
            }

            float actionX = w-actionWidth+12;
            learningFire.transform.SetParent(learningRoot,false);
            Fixed((RectTransform)learningFire.transform,new Vector2(0,0),new Vector2(0,0),new Vector2(actionX,12),new Vector2(actionWidth,touch));
            Place(learningFire.GetComponentInChildren<TextMeshProUGUI>().rectTransform,new Vector2(.05f,.05f),new Vector2(.95f,.95f));
            learningFire.GetComponentInChildren<TextMeshProUGUI>().fontSizeMax = 25;
            var track=(RectTransform)formulaChargeFill.parent; track.SetParent(learningRoot,false);
            Fixed(track,new Vector2(0,0),new Vector2(0,0),new Vector2(actionX,touch+24),new Vector2(actionWidth,8));
            Fixed(formulaChargeLabel.rectTransform,new Vector2(0,0),new Vector2(0,0),new Vector2(actionX,touch+38),new Vector2(actionWidth,44));
            formulaChargeLabel.fontSizeMax=20; formulaChargeLabel.fontSizeMin=17;
            formulaChargeLabel.alignment=TextAlignmentOptions.BottomLeft;
            Fixed((RectTransform)learningClose.transform,Vector2.one,Vector2.one,new Vector2(-12,-8),new Vector2(Mathf.Max(touch,actionWidth*.65f),touch));
            StyleButton(learningClose,Edge,new Color(22f/255,41f/255,66f/255));
            learningClose.GetComponentInChildren<TextMeshProUGUI>().fontSizeMax=18;
            Fixed(learningHeading.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(12,-6),new Vector2(viewportWidth,30));
            learningHeading.margin = Vector4.zero;
            learningHeading.fontSizeMax=20; learningHeading.fontSizeMin=18;
            learningClose.transform.SetAsLastSibling(); formulaGhost.SetAsLastSibling();
        }
        void RefreshFormulaInformation()
        {
            if(!formulaInfoBody)return;
            formulaInfoHeading.text=inspectedFormulaIcon==FormulaIcon.None?"INFO FORMULA":UniversalFormula.Title(inspectedFormulaIcon);
            formulaInfoBody.text=inspectedFormulaIcon==FormulaIcon.None?
                "Pasangkan data dengan rekaman A dan B untuk mengkalibrasi plasma. Plasma adalah mekanik fiksi. Kartu tidak menunjukkan penawar atau pengobatan." : FormulaInfoText(inspectedFormulaIcon);
            infoScroll.verticalNormalizedPosition=1;
        }
        public UnityEngine.UI.ScrollRect BeginAssemblyScroll(PointerEventData pointer,Transform source)
        {
            if(pointer==null||!assemblyScroll||AssemblyScrolling||draggingFormulaIcon!=FormulaIcon.None||!CanConfigureFormula())return null;
            var delta=pointer.position-pointer.pressPosition;
            UnityEngine.UI.ScrollRect result=null;
            if(paletteScroll.vertical&&Mathf.Abs(delta.y)>Mathf.Abs(delta.x))result=paletteScroll;
            else if(assemblyScroll.horizontal&&Mathf.Abs(delta.x)>=Mathf.Abs(delta.y))result=assemblyScroll;
            AssemblyScrolling=result;return result;
        }
        public void EndAssemblyScroll(){AssemblyScrolling=false;}
        public void RevealAssemblyControl(RectTransform target)
        {
            if(!learningRoot||!learningRoot.gameObject.activeInHierarchy)return;
            foreach(var scroll in new[]{paletteScroll,assemblyScroll})
            {
                if(!scroll||!target.IsChildOf(scroll.content))continue;
                var corners=new Vector3[4];target.GetWorldCorners(corners);
                var low=(Vector2)scroll.viewport.InverseTransformPoint(corners[0]);
                var high=(Vector2)scroll.viewport.InverseTransformPoint(corners[2]);
                var viewport=scroll.viewport.rect;var position=scroll.content.anchoredPosition;
                if(scroll.horizontal)
                {
                    position.x+=low.x<viewport.xMin?viewport.xMin-low.x:high.x>viewport.xMax?viewport.xMax-high.x:0;
                    position.x=Mathf.Clamp(position.x,Mathf.Min(0,viewport.width-scroll.content.rect.width),0);
                }
                if(scroll.vertical)
                {
                    position.y+=low.y<viewport.yMin?viewport.yMin-low.y:high.y>viewport.yMax?viewport.yMax-high.y:0;
                    position.y=Mathf.Clamp(position.y,0,Mathf.Max(0,scroll.content.rect.height-viewport.height));
                }
                scroll.content.anchoredPosition=position;
            }
        }

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
        void RenderFormulaVisual(UnityEngine.UI.Image image, FormulaIcon icon)
        {
            bool data = icon >= FormulaIcon.Nicotine && UniversalFormula.IsValid(icon);
            image.gameObject.SetActive(icon != FormulaIcon.None);
            image.sprite = LearningSprite(UniversalFormula.IconName(icon));
            image.enabled = !data;
            var badge = image.transform.Find("Data Badge")?.GetComponent<TextMeshProUGUI>();
            if (data && !badge)
            {
                badge = Label("", image.transform, Vector2.zero, Vector2.one, 30, Cyan, TextAlignmentOptions.Center);
                badge.name = "Data Badge"; badge.raycastTarget = false;
                badge.fontSizeMax = 30; badge.fontSizeMin = 18;
            }
            if (badge)
            {
                badge.text = UniversalFormula.Badge(icon);
                badge.gameObject.SetActive(data);
            }
        }

        void BindFormulaPalette()
        {
            activePalette = UniversalFormula.Palette(model.encounter);
            if (formulaPalette == null) return;
            for (int i = 0; i < formulaPalette.Length; i++)
            {
                var button = formulaPalette[i];
                var icon = activePalette[i];
                button.name = "Formula Icon " + icon;
                var title = button.GetComponentInChildren<TextMeshProUGUI>(true);
                title.text = UniversalFormula.Title(icon);
                title.fontSizeMin = icon >= FormulaIcon.Nicotine ? 15 : 18;
                var drag = button.GetComponent<FormulaIconDrag>();
                drag.icon = icon;
                RenderFormulaVisual(button.transform.GetChild(1).GetComponent<UnityEngine.UI.Image>(), icon);
            }
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
                int paletteIndex = i;
                var icon = (FormulaIcon)i; int column=i%3, row=i/3;
                var button = Button(UniversalFormula.Title(icon), learningRoot, new Vector2(.015f+column*.108f,.49f-row*.33f), new Vector2(.116f+column*.108f,.81f-row*.33f), ()=>SelectFormulaIcon(activePalette[paletteIndex]));
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
            BindFormulaPalette();
        }
        void ShowLearningWorkbench(bool synthesizeMode)
        {
            if (actionAnimating || View!=GameView.Battle || (model.phase!=BattlePhase.Diagnose && model.phase!=BattlePhase.Player)) return;
            if (synthesizeMode && !model.CanAttack) return;
            SetModalLayout(DialogLayout.Bottom); EnsureLearningWorkbench(); BindFormulaPalette();
            modal.SetParent(safe,false); modal.anchorMin=new Vector2(.025f,.025f); modal.anchorMax=new Vector2(.975f,.375f); modal.offsetMin=modal.offsetMax=Vector2.zero;
            modalTitle.gameObject.SetActive(false); modalBody.gameObject.SetActive(false); options.gameObject.SetActive(false); diagnosticFrame.gameObject.SetActive(false);
            diagnosisRoot.gameObject.SetActive(!synthesizeMode); learningRoot.gameObject.SetActive(synthesizeMode);
            modal.gameObject.SetActive(true); modalCanvasGroup.alpha=1; modal.SetAsLastSibling(); SetLearningArena(true);
            if(synthesizeMode)
            {
                Canvas.ForceUpdateCanvases();
                float scale=learningRoot.GetComponentInParent<Canvas>().scaleFactor;
                float density=touchDensity>0?touchDensity:1;
                LayoutSynthesisDrawer(safe.rect.width*scale/density,safe.rect.height*scale/density);
                RefreshFormulaInformation();
            }
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
            inspectedFormulaIcon=FormulaIcon.None;AssemblyScrolling=false;
            BindFormulaPalette();
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
            if (EncounterTransitionPlaying || actionAnimating || View!=GameView.Battle || model.phase!=BattlePhase.Diagnose || model.diagnosed || !diagnosisRoot || !diagnosisRoot.gameObject.activeInHierarchy) return;
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
        bool CanConfigureFormula() => !EncounterTransitionPlaying && !actionAnimating && View==GameView.Battle && model.diagnosed && model.phase==BattlePhase.Player && learningRoot && learningRoot.gameObject.activeInHierarchy;
        public void SelectFormulaIcon(FormulaIcon icon)
        {
            if (!CanConfigureFormula() || !UniversalFormula.Contains(model.encounter, icon) || draggingFormulaIcon!=FormulaIcon.None) return;
            selectedFormulaIcon=selectedFormulaIcon==icon?FormulaIcon.None:icon; RefreshLearningWorkbench();
            inspectedFormulaIcon=icon;RefreshFormulaInformation();
        }
        void TapFormulaSocket(int socket)
        {
            if (!CanConfigureFormula() || socket<0 || socket>1 || draggingFormulaIcon!=FormulaIcon.None) return;
            if (selectedFormulaIcon!=FormulaIcon.None) InstallFormulaIcon(selectedFormulaIcon,socket);
            else if (formulaSlots[socket]!=FormulaIcon.None) { formulaSlots[socket]=FormulaIcon.None; RestartFormulaCharge(); }
        }
        public void InstallFormulaIcon(FormulaIcon icon,int socket)
        {
            if (!CanConfigureFormula() || socket<0 || socket>1 || !UniversalFormula.Contains(model.encounter, icon) || (draggingFormulaIcon!=FormulaIcon.None && draggingFormulaIcon!=icon)) return;
            formulaSlots[socket]=icon; selectedFormulaIcon=FormulaIcon.None; EndFormulaDrag(null); RestartFormulaCharge();
            if (moduleInstallRoutines[socket] != null) StopCoroutine(moduleInstallRoutines[socket]);
            moduleInstallRoutines[socket] = StartCoroutine(AnimateModuleInstall(socket));
        }
        IEnumerator AnimateModuleInstall(int socket)
        {
            var icon = formulaSocketIcons[socket].rectTransform;
            for (float elapsed = 0; elapsed < .12f; elapsed += Time.unscaledDeltaTime)
            {
                if (!learningRoot.gameObject.activeInHierarchy) break;
                float progress = Mathf.Clamp01(elapsed / .12f);
                icon.localScale = Vector3.one * Mathf.Lerp(.75f, 1, 1 - (1-progress)*(1-progress));
                yield return null;
            }
            icon.localScale = Vector3.one;
            moduleInstallRoutines[socket] = null;
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
            if (pointer==null || AssemblyScrolling || !CanConfigureFormula() || !UniversalFormula.Contains(model.encounter, icon) || draggingFormulaIcon!=FormulaIcon.None) return false;
            draggingFormulaIcon=icon; selectedFormulaIcon=FormulaIcon.None;
            inspectedFormulaIcon=icon;RefreshFormulaInformation();
            RenderFormulaVisual(formulaGhost.GetChild(0).GetComponent<UnityEngine.UI.Image>(), icon);
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
            AssemblyScrolling=false;
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
            formulaChargeLabel.text = model.energy < 20 ? "Perlu 20 MP" : formulaCharged ? "Siap tembak" :
                formulaCharge > 0 ? "Mengisi daya…" : "Pasang dua modul";
            formulaChargeFill.GetComponent<UnityEngine.UI.Image>().color=formulaCharged?Green:Cyan;
        }
        void RefreshLearningWorkbench()
        {
            if (!learningRoot || (!learningRoot.gameObject.activeSelf && !diagnosisRoot.gameObject.activeSelf)) return;
            if (diagnosisRoot.gameObject.activeSelf)
            {
                diagnosisHeading.text="DIAGNOSE  //  "+EncounterCatalog.Title(model.encounter);
                diagnosisClose.interactable=!actionAnimating;
                diagnosisFeedback.text=actionAnimating?"Memindai musuh: membuka kedua rekaman sumber A dan B...":"Kedua rekaman tersimpan. Tutup untuk kembali ke pilihan aksi.";
                for (int i=0;i<2;i++)
                {
                    var source=CombatSamples.Get(model.encounter,i);
                    var image = formulaSourceIcons[i];
                    image.gameObject.SetActive(model.diagnosed);
                    image.sprite = LearningSprite(source.icon);
                    image.enabled = source.kind != SampleClass.Data;
                    var badge = image.transform.Find("Record Badge")?.GetComponent<TextMeshProUGUI>();
                    if (source.kind == SampleClass.Data && !badge)
                    {
                        badge = Label(i == 0 ? "A" : "B", image.transform, Vector2.zero, Vector2.one, 42, Cyan, TextAlignmentOptions.Center);
                        badge.name = "Record Badge"; badge.raycastTarget = false;
                    }
                    if (badge) badge.gameObject.SetActive(source.kind == SampleClass.Data);
                    sourceTitles[i].text=model.diagnosed?source.title:(i==0?"A":"B")+" / memindai...";
                    sourceCategories[i].transform.parent.gameObject.SetActive(model.diagnosed && model.encounter < 2);
                    Place(formulaSourceLabels[i].rectTransform, new Vector2(.28f,.035f),
                        new Vector2(.98f, model.encounter >= 2 ? .70f : .49f));
                    sourceCategories[i].text=source.kind==SampleClass.Data ? (model.encounter==2?"Data tembakau":model.encounter==4?"Data alkohol":"Data gabungan") : source.kind==SampleClass.FreeSugar?"Gula bebas":"Lemak jenuh";
                    formulaSourceLabels[i].text=model.diagnosed?source.record:"";
                }
                return;
            }
            bool configure=CanConfigureFormula();
            learningHeading.text="SYNTHESIZE";
            learningClose.interactable=!actionAnimating;
            for (int i=0;i<2;i++)
            {
                formulaSourceReminders[i].text=CombatSamples.Get(model.encounter,i).title;
                if (moduleChoiceLabels != null)
                    moduleChoiceLabels[i].text=formulaSlots[i]==FormulaIcon.None?"Pilih modul":UniversalFormula.Title(formulaSlots[i]);
                formulaSockets[i].interactable=configure;
                RenderFormulaVisual(formulaSocketIcons[i], formulaSlots[i]);
                formulaSocketLabels[i].text="+";
                formulaSocketLabels[i].gameObject.SetActive(formulaSlots[i]==FormulaIcon.None);
                var shape=formulaSockets[i].GetComponent<OutlinedUiGraphic>();
                shape.dashed=formulaSlots[i]==FormulaIcon.None;shape.pulse=shape.dashed&&configure;
                formulaSockets[i].image.SetVerticesDirty();
            }
            for (int i=0;i<6;i++) { formulaPalette[i].interactable=configure; formulaPalette[i].GetComponent<UiControlFeedback>().selected=selectedFormulaIcon==activePalette[i]; }
            learningFire.interactable=configure && draggingFormulaIcon==FormulaIcon.None && formulaCharged && model.CanSynthesizeFormula(formulaSlots[0],formulaSlots[1]);
            learningFire.GetComponentInChildren<TextMeshProUGUI>().text="TEMBAK · 20 MP";
            learningFeedback.text=draggingFormulaIcon!=FormulaIcon.None?"Lepaskan ikon ke soket A atau B.":selectedFormulaIcon!=FormulaIcon.None?"Pilih soket A/B: "+UniversalFormula.Title(selectedFormulaIcon)+".":formulaCharge>0&&!formulaCharged?"Dua ikon terpasang. Mengisi daya otomatis...":model.energy<20?"Perlu 20 MP. Tutup lalu gunakan Restore.":formulaCharged?"Charge bukan nilai jawaban. Siap ditembakkan.":"Seret ikon ke soket A/B, atau ketuk ikon lalu soket.";
            RefreshFormulaChargeVisual();
        }
    }
}
