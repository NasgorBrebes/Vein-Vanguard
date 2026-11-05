using UnityEngine;
using UnityEngine.UI;

namespace VeinVanguard
{
    [RequireComponent(typeof(Image))]
    public class OutlinedUiGraphic : BaseMeshEffect
    {
        public float radius = 14, borderWidth = 2;
        public Color borderColor = new Color(45f/255,106f/255,142f/255);
        public Vector2 shadowOffset = new Vector2(0,-4), visualOffset;
        public float visualScale=1;
        public bool dashed, pulse;
        const int Count = 96;
        const int SmoothCount = 36;

        void Update() { if (pulse && Application.isPlaying) graphic.SetVerticesDirty(); }

        static Vector2 Point(Rect rect, float rounding, int index)
        {
            float arc=Mathf.PI*rounding*.5f;
            float horizontal=Mathf.Max(0,rect.width-2*rounding),vertical=Mathf.Max(0,rect.height-2*rounding);
            float distance=index/(float)Count*(arc*4+horizontal*2+vertical*2);
            for(int corner=0;corner<4;corner++)
            {
                var center=new Vector2(corner==0||corner==3?rect.xMax-rounding:rect.xMin+rounding,
                    corner<2?rect.yMax-rounding:rect.yMin+rounding);
                if(distance<=arc)
                {
                    float angle=(corner*90+(arc>0?distance/arc*90:0))*Mathf.Deg2Rad;
                    return center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*rounding;
                }
                distance-=arc;
                float side=corner%2==0?horizontal:vertical;
                if(distance<=side)
                {
                    if(corner==0)return new Vector2(rect.xMax-rounding-distance,rect.yMax);
                    if(corner==1)return new Vector2(rect.xMin,rect.yMax-rounding-distance);
                    if(corner==2)return new Vector2(rect.xMin+rounding+distance,rect.yMin);
                    return new Vector2(rect.xMax,rect.yMin+rounding+distance);
                }
                distance-=side;
            }
            return new Vector2(rect.xMax,rect.yMax-rounding);
        }

        static void Solid(VertexHelper mesh, Rect rect, float rounding, Color color)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(rect.center,color,Vector2.zero);
            for(int i=0;i<SmoothCount;i++)mesh.AddVert(SmoothPoint(rect,rounding,i),color,Vector2.zero);
            for(int i=0;i<SmoothCount;i++)mesh.AddTriangle(start,start+1+i,start+1+(i+1)%SmoothCount);
        }
        static Vector2 SmoothPoint(Rect rect,float rounding,int index)
        {
            int corner=index/9,step=index%9;
            float angle=(corner*90+step*90f/8)*Mathf.Deg2Rad;
            var center=new Vector2(corner==0||corner==3?rect.xMax-rounding:rect.xMin+rounding,
                corner<2?rect.yMax-rounding:rect.yMin+rounding);
            return center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*rounding;
        }

        public override void ModifyMesh(VertexHelper mesh)
        {
            if(!IsActive())return;
            mesh.Clear();
            Rect rect = graphic.GetPixelAdjustedRect();
            if(rect.width<=0||rect.height<=0)return;
            var center=rect.center;rect.size*=visualScale;rect.center=center;
            rect.position += visualOffset;
            float rounding = Mathf.Clamp(radius,0,Mathf.Min(rect.width,rect.height)*.5f);
            if(shadowOffset != Vector2.zero)
            {
                var shadow = rect; shadow.position += shadowOffset;
                Solid(mesh,shadow,rounding,new Color(8f/255,16f/255,26f/255,graphic.color.a));
            }
            Solid(mesh,rect,rounding,graphic.color);
            if(borderWidth<=0)return;
            Rect inner = Rect.MinMaxRect(rect.xMin+borderWidth,rect.yMin+borderWidth,rect.xMax-borderWidth,rect.yMax-borderWidth);
            float innerRadius = Mathf.Max(0,rounding-borderWidth);
            Color edge = borderColor;
            if(pulse && Application.isPlaying)edge.a*=.7f+.3f*Mathf.Sin(Time.unscaledTime*5);
            int count=dashed?Count:SmoothCount;
            for(int i=0;i<count;i++)
            {
                if(dashed && i%3==2)continue;
                int next=(i+1)%count, start=mesh.currentVertCount;
                mesh.AddVert(dashed?Point(rect,rounding,i):SmoothPoint(rect,rounding,i),edge,Vector2.zero);
                mesh.AddVert(dashed?Point(rect,rounding,next):SmoothPoint(rect,rounding,next),edge,Vector2.zero);
                mesh.AddVert(dashed?Point(inner,innerRadius,next):SmoothPoint(inner,innerRadius,next),edge,Vector2.zero);
                mesh.AddVert(dashed?Point(inner,innerRadius,i):SmoothPoint(inner,innerRadius,i),edge,Vector2.zero);
                mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
