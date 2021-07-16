using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FiveKnights.Misc
{
    public class DebugColliders : MonoBehaviour
    {
        public List<BoxCollider2D> boxColliders;
        public List<CircleCollider2D> circleColliders;
        public List<PolygonCollider2D> polyColliders;
        public List<EdgeCollider2D> edgeColliders;

        public GameObject lineRoot;

        public float lineWidth = .05f;
        public float zDepth = -2.1f;
        public Color colliderColor = Color.green;
        public Color triggerColor = Color.blue;
        public Color disabledColor = Color.red;
        public Color gameObjectDisabledColor = Color.gray;
        public bool renderDisabledColliders = true;
        public bool runDebugInput = true;

        Dictionary<Collider2D,LineRenderer> lines;
        
        Material colliderLineMat;
        Material triggerLineMat;
        Material disabledLineMat;
        Material gameObjectDisabledLineMat;


        Dictionary<Collider2D,TextMesh> labels;

        List<Vector2> GetPointsFromCollider( BoxCollider2D col )
        {
            List<Vector2> points = new List<Vector2>();

            Vector2 cSize = col.size * .5f;
            Vector2 bottomLeft = -cSize;
            Vector2 topLeft = new Vector2(-cSize.x, cSize.y);
            Vector2 bottomRight = new Vector2(cSize.x, -cSize.y);
            Vector2 topRight = cSize;

            points.Add( bottomLeft + col.offset );
            points.Add( topLeft + col.offset );
            points.Add( topRight + col.offset );
            points.Add( bottomRight + col.offset );
            points.Add( bottomLeft + col.offset );

            for(int i = 0; i < points.Count; ++i )
            {
                Vector3 result = col.transform.TransformPoint( (Vector3)points[ i ] );                
                points[ i ] = (Vector2)result;
            }

            return points;
        }

        List<Vector2> GetPointsFromCollider( CircleCollider2D col )
        {
            List<Vector2> points = new List<Vector2>();

            Vector2 center = col.offset + (Vector2)col.transform.position;

            float radius = col.radius;
            int slices = 16;
            
            float sliceSize = (2f*Mathf.PI) / (float)slices;

            for( int i = 0; i < slices; ++i )
            {
                float theta = sliceSize * (float)i;
                                
                float sin = Mathf.Sin(theta);
                float cos = Mathf.Cos(theta);

                float x = cos - sin;
                float y = sin + cos;

                Vector2 point = new Vector2( x, y );
                
                point.x = point.x * radius * col.transform.localScale.x;
                point.y = point.y * radius * col.transform.localScale.y;

                points.Add( point + center );
            }

            //add the start point to complete the shape
            {
                float theta = 0f;

                float sin = Mathf.Sin(theta);
                float cos = Mathf.Cos(theta);

                float x = cos - sin;
                float y = sin + cos;

                Vector2 point = new Vector2( x, y );

                point.x = point.x * radius * col.transform.localScale.x;
                point.y = point.y * radius * col.transform.localScale.y;

                points.Add( point + center );
            }

            return points;
        }

        List<Vector2> GetPointsFromCollider( PolygonCollider2D col )
        {
            List<Vector2> points = col.points.ToList();
            points.Add( col.points[ 0 ] );

            for( int i = 0; i < points.Count; ++i )
            {
                Vector3 result = col.transform.TransformPoint( (Vector3)points[ i ] + (Vector3)col.offset );
                points[ i ] = (Vector2)result;
            }

            return points;
        }

        List<Vector2> GetPointsFromCollider( EdgeCollider2D col )
        {
            List<Vector2> points = col.points.ToList();

            for( int i = 0; i < points.Count; ++i )
            {
                Vector3 result = col.transform.TransformPoint( (Vector3)points[ i ] + (Vector3)col.offset );
                points[ i ] = (Vector2)result;
            }

            return points;
            //return col.points.Select( x => { return x + (Vector2)col.transform.position; } ).ToList();
        }

        List<Vector2> GetPointsHelper( Collider2D collider )
        {
            if( collider as PolygonCollider2D )
            {
                return GetPointsFromCollider( collider as PolygonCollider2D );
            }
            if( collider as EdgeCollider2D )
            {
                return GetPointsFromCollider( collider as EdgeCollider2D );
            }
            if( collider as CircleCollider2D )
            {
                return GetPointsFromCollider( collider as CircleCollider2D );
            }
            if( collider as BoxCollider2D )
            {
                return GetPointsFromCollider( collider as BoxCollider2D );
            }

            return new List<Vector2>();
        }

        //use this static bool to keep the input only running on one component 
        static bool forceSingleEntry = false;
        static IEnumerator debugInput = null;
        IEnumerator DebugInput()
        {
            bool suspended = false;
            for(; ; )
            {
                if( suspended )
                    Time.timeScale = 0f;

                //toggle renderers on/off
                if( UnityEngine.Input.GetKeyDown( KeyCode.Slash ) )
                {
                    foreach(var v in lines)
                    {
                        v.Value.GetComponent<Renderer>().enabled = !v.Value.GetComponent<Renderer>().enabled;
                    }
                    foreach( var v in labels )
                    {
                        v.Value.gameObject.SetActive( !v.Value.gameObject.activeInHierarchy );
                    }
                }

                //toggle the rendering of 
                if( UnityEngine.Input.GetKeyDown( KeyCode.Backslash ) )
                {
                    renderDisabledColliders = !renderDisabledColliders;
                }

                //suspend
                if( !forceSingleEntry && UnityEngine.Input.GetKeyDown( KeyCode.Q ) )
                {
                    forceSingleEntry = true;
                    Time.timeScale = 0f;
                    suspended = true;
                }
                //advance by about one frame
                if( !forceSingleEntry && UnityEngine.Input.GetKeyDown( KeyCode.W ) )
                {
                    forceSingleEntry = true;
                    Time.timeScale = 1f;
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    Time.timeScale = 0f;
                }
                //advance by many frames (hold R)
                if( !forceSingleEntry && UnityEngine.Input.GetKey( KeyCode.R ) )
                {
                    forceSingleEntry = true;
                    Time.timeScale = 1f;
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    Time.timeScale = 0f;
                }
                //resume from suspend
                if( !forceSingleEntry && UnityEngine.Input.GetKeyDown( KeyCode.E ) )
                {
                    forceSingleEntry = true;
                    Time.timeScale = 1f;
                    suspended = false;
                }
                yield return new WaitForEndOfFrame();
                forceSingleEntry = false;
            }
            //yield break;
        }

        private void OnEnable()
        {
            if( runDebugInput )
            {
                if( debugInput == null )
                {
                    debugInput = DebugInput();
                    StartCoroutine( debugInput );
                }
            }
        }

        private void OnDisable()
        {
            if( runDebugInput )
            {
                if( debugInput != null )
                {
                    StopCoroutine( debugInput );
                    debugInput = null;
                }
            }
        }

        private IEnumerator Start()
        {
            boxColliders = GetComponentsInChildren<BoxCollider2D>(true).ToList();
            circleColliders = GetComponentsInChildren<CircleCollider2D>( true ).ToList();
            polyColliders = GetComponentsInChildren<PolygonCollider2D>( true ).ToList();
            edgeColliders = GetComponentsInChildren<EdgeCollider2D>( true ).ToList();

            lineRoot = new GameObject( "DebugCollider Lines" );
            lineRoot.transform.SetParent( transform );
            lineRoot.transform.position = Vector3.zero;

            lines = new Dictionary<Collider2D, LineRenderer>();
            labels = new Dictionary<Collider2D, TextMesh>();

            colliderLineMat = new Material( Shader.Find( "Diffuse" ) );
            triggerLineMat = new Material( Shader.Find( "Diffuse" ) );
            disabledLineMat = new Material( Shader.Find( "Diffuse" ) );
            gameObjectDisabledLineMat = new Material( Shader.Find( "Diffuse" ) );

            CreateColliders();

            for(;;)
            {
                UpdateColliders();
                yield return new WaitForEndOfFrame();
            }
        }

        void CreateColliders()
        {
            CreateLinesFromColliders( boxColliders );
            CreateLinesFromColliders( circleColliders );
            CreateLinesFromColliders( polyColliders );
            CreateLinesFromColliders( edgeColliders );
        }

        void CreateLinesFromColliders<T>(List<T> colliders) where T : Collider2D
        {
            foreach( var v in colliders )
            {
                string name = "["+v.gameObject.name+",  "+v.GetType().Name+"]";
                GameObject newLine = new GameObject( name );
                newLine.transform.SetParent( lineRoot.transform );
                newLine.transform.position = Vector3.zero;

                Color lineColor = GetLineColor(v);

                //TODO: wrap these line calls in #if checks for unity versions...
                LineRenderer line = newLine.AddComponent<LineRenderer>();
                Vector3[] points = GetPointsHelper(v).Select(x=>{ return new Vector3(x.x,x.y,zDepth); } ).ToArray();
                line.positionCount = points.Length;
                //line.SetVertexCount( points.Length );
                line.SetPositions( points );
                //line.SetWidth( lineWidth, lineWidth );
                line.startWidth = lineWidth;
                line.endWidth = lineWidth;
                line.sharedMaterial = GetLineMaterial( v );
                //line.SetColors( lineColor, lineColor );
                line.startColor = lineColor;
                line.endColor = lineColor;
                lines.Add( v, line );

                GameObject lineLabel = new GameObject( name + " LABEL" );
                lineLabel.transform.SetParent( newLine.transform );
                lineLabel.transform.localScale = Vector3.one * .25f;
                TextMesh tm = newLine.gameObject.AddComponent<TextMesh>();
                tm.text = name;
                tm.fontSize = 14;
                //tm.font = Font.CreateDynamicFontFromOSFont( "Arial", 12 );
                tm.color = Color.white;
                tm.alignment = TextAlignment.Center;
                labels.Add( v, tm );
            }
        }

        Color GetLineColor(Collider2D col)
        {
            if( !col.gameObject.activeInHierarchy )
            {
                return gameObjectDisabledColor;
            }

            if(!col.enabled)
            {
                return disabledColor;
            }

            if( col.isTrigger )
            {
                return triggerColor;
            }

            return colliderColor;
        }

        Material GetLineMaterial( Collider2D col )
        {
            if( !col.gameObject.activeInHierarchy )
            {
                return gameObjectDisabledLineMat;
            }

            if( !col.enabled )
            {
                return disabledLineMat;
            }

            if( col.isTrigger )
            {
                return triggerLineMat;
            }

            return colliderLineMat;
        }

        void UpdateColliders()
        {
            float additionalLabelOffset = 0f;
            foreach(var pair in lines)
            {
                if( pair.Key == null )
                    continue;

                if( !renderDisabledColliders )
                {
                    if( !pair.Key.enabled || !pair.Key.gameObject.activeInHierarchy )
                    {
                        pair.Value.enabled = false;
                        continue;
                    }
                }

                pair.Value.sharedMaterial = GetLineMaterial( pair.Key );

                Color lineColor = GetLineColor(pair.Key);
                //pair.Value.SetColors( lineColor, lineColor );
                pair.Value.startColor = lineColor;
                pair.Value.endColor = lineColor;

                if( pair.Value.GetComponent<Renderer>() )
                    pair.Value.GetComponent<Renderer>().sharedMaterial.color = lineColor;

                labels[ pair.Key ].color = lineColor;
                labels[ pair.Key ].transform.position = pair.Key.bounds.max;
                if( pair.Key as CircleCollider2D )
                {
                    labels[ pair.Key ].transform.position = Vector3.one * (pair.Key as CircleCollider2D).radius;
                }

                foreach( var v in labels )
                {
                    if( Mathf.Abs( v.Value.transform.position.y - labels[ pair.Key ].transform.position.y ) < 0.2f )
                    {
                        labels[ pair.Key ].transform.Translate( new Vector3( 0f, additionalLabelOffset, 0f ) );
                        additionalLabelOffset -= .2f;
                        break;
                    }
                }

                Vector3 labelPos = new Vector3(labels[ pair.Key ].transform.position.x,labels[ pair.Key ].transform.position.y,-.1f);
                labels[ pair.Key ].transform.position = labelPos;
                labels[ pair.Key ].transform.localScale = Vector3.one * .2f;

                pair.Value.enabled = true;

                LineRenderer line = pair.Value;
                Vector3[] points = GetPointsHelper(pair.Key).Select(x=>{ return new Vector3(x.x,x.y,zDepth); } ).ToArray();
                line.SetPositions( points );
            }
        }
    }
}