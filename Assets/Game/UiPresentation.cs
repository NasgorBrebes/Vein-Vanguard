using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace VeinVanguard
{
    public partial class BattleManager
    {
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
            Place(commands,new Vector2(.025f,.025f),new Vector2(.975f,.24f));commands.GetComponent<Image>().color=Color.clear;
            var message=commands.Find("Message Panel") as RectTransform;
            Place(message,new Vector2(.045f,.64f),new Vector2(.955f,.98f));InformationPanel(message);
            Place(log.rectTransform,new Vector2(.045f,.05f),new Vector2(.98f,.95f));log.color=Paper;log.fontSizeMax=22;log.fontSizeMin=18;
            var mark=NamedLabel("Narration Indicator",message,"i",new Vector2(.01f,.1f),new Vector2(.045f,.90f),22);
            mark.color=Cyan;mark.alignment=TextAlignmentOptions.Center;
            var actions=new[]{diagnose,synthesize,restore,homeostasis};var accents=new[]{Cyan,Orange,Green,Amber};
            for(int i=0;i<actions.Length;i++)
            {
                Place((RectTransform)actions[i].transform,new Vector2(.025f+i*.245f,.03f),new Vector2(.255f+i*.245f,.54f));
                StyleButton(actions[i],accents[i]);
                var icon=actions[i].transform.Find("Action Icon");if(icon)icon.GetComponent<Image>().color=accents[i];
                var text=actions[i].GetComponentInChildren<TextMeshProUGUI>();text.fontSizeMax=25;text.fontSizeMin=21;
            }
            InformationPanel(modal);modal.GetComponent<Image>().raycastTarget=true;
            modalTitle.color=Paper;modalBody.color=Paper;
            var stripe=modal.Find("Accent");if(stripe)stripe.gameObject.SetActive(false);
            StyleExploration();
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
            System.Array.Sort(controls,(a,b)=>{
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
