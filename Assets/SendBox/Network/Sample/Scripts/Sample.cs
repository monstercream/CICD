using Network;
using UnityEngine;

public class Sample : MonoBehaviour
{
    private Network.Network network;

    private async void Awake()
    {
        network = new Network.Network();

        NetworkResult login = await network.LoginAsync( "7789B", "42779113-0012-58F2-939B-0870AFAE582E" );
        NetworkResult request = await network.RequestAsync( "ServerTest" );
        NetworkResult titleData = await network.TitleDataRequestAsync();
        NetworkResult userData = await network.UserDataRequestAsync();

        if( login.IsSuccuces == false )
        {
            Debug.LogWarning( $"Failed" );
        }
        if( request.IsSuccuces == false )
        {
            Debug.LogWarning( $"Failed" );
        }
        if( titleData.IsSuccuces == false )
        {
            Debug.LogWarning( $"Failed" );
        }
        if( userData.IsSuccuces == false )
        {
            Debug.LogWarning( $"Failed" );
        }
    }
}
