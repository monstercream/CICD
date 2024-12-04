using UnityEngine;
using UnityEngine.UI;

namespace PolygonGraph
{
	[RequireComponent( typeof(CanvasRenderer) )]
	public class PolygonGraph : Graphic
	{
		[SerializeField, Range( 3, 10 )]
		private int vertices = 5;

		[SerializeField, Range( 0, 1 )]
		private float[] values;

		[SerializeField]
		private float radius = 100f;

		[SerializeField]
		private Color fillColor = new Color( 0.5f, 0.5f, 1f, 0.5f );

		[SerializeField]
		private Color lineColor = Color.blue;

		[SerializeField]
		private float lineWidth = 2f;

		[SerializeField, Range( 0.1f, 1f )]
		private float duration = 1f;

		public void SetValues(float[] nextValues)
		{
			if( values.Length != values.Length )
			{
				Debug.LogError( "New values array length must match current values array length!" );
				return;
			}

			for( int i = 0; i < values.Length; i++ )
			{
				int index = i;
				SetValue( index, nextValues[ index ] );
			}
		}

		public void SetValue(int index, float value)
		{
			float curValue = values[ index ];

			CustomTween.DOFloat( curValue, value, duration, (float val) =>
			{
				values[ index ] = val;
				SetVerticesDirty();
			} );
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();

			if( values == null || values.Length != vertices )
			{
				values = new float[vertices];
				for( int i = 0; i < vertices; i++ )
				{
					values[ i ] = 0.5f;
				}
			}

			vh.AddVert( Vector3.zero, fillColor, Vector2.zero );

			for( int i = 0; i < vertices; i++ )
			{
				float angle = ( i * 360f / vertices ) * Mathf.Deg2Rad;
				float x = Mathf.Sin( angle ) * radius * values[ i ];
				float y = Mathf.Cos( angle ) * radius * values[ i ];

				vh.AddVert( new Vector3( x, y, 0 ), fillColor, new Vector2( 0.5f + x / radius / 2f, 0.5f + y / radius / 2f ) );
			}

			for( int i = 0; i < vertices; i++ )
			{
				vh.AddTriangle( 0, i + 1, ( ( i + 1 ) % vertices ) + 1 );
			}

			for( int i = 0; i < vertices; i++ )
			{
				int nextIndex = ( i + 1 ) % vertices;
				AddLine( vh, i + 1, nextIndex + 1 );
			}
		}

		private void AddLine(VertexHelper vh, int startIndex, int endIndex)
		{
			UIVertex vertex1 = new UIVertex();
			UIVertex vertex2 = new UIVertex();
			vh.PopulateUIVertex( ref vertex1, startIndex );
			vh.PopulateUIVertex( ref vertex2, endIndex );

			Vector3 normal = Vector3.Cross( ( vertex2.position - vertex1.position ).normalized, Vector3.forward );
			Vector3 offsetUp = normal * lineWidth / 2f;
			Vector3 offsetDown = -offsetUp;

			int vertexCount = vh.currentVertCount;

			vh.AddVert( vertex1.position + offsetUp, lineColor, vertex1.uv0 );
			vh.AddVert( vertex1.position + offsetDown, lineColor, vertex1.uv0 );
			vh.AddVert( vertex2.position + offsetUp, lineColor, vertex2.uv0 );
			vh.AddVert( vertex2.position + offsetDown, lineColor, vertex2.uv0 );

			vh.AddTriangle( vertexCount + 0, vertexCount + 1, vertexCount + 2 );
			vh.AddTriangle( vertexCount + 1, vertexCount + 3, vertexCount + 2 );
		}
#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			SetVerticesDirty();
		}
#endif
	}

}