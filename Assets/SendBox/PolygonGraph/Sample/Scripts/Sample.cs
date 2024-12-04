using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace PolygonGraph
{
    public class Sample : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text Count;
        private int vertices = 5;

        [SerializeField]
        private PolygonGraph polygonGraph;

        [Button( "Random Test" )]
        public void TestRandomValue()
        {
            float[] newValues = new float[vertices];

            for( int i = 0; i < vertices; i++ )
            {
                newValues[ i ] = Random.Range( 0f, 1f );
            }

            Count.SetStringAnim( Random.Range( 1, 100 ), "Point" );
            polygonGraph.SetValues( newValues );
        }
    }
}
