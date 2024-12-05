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

        if( login.IsSuccess == false )
        {
            Debug.LogWarning( $"login Failed" );
            return;
        }
        if( request.IsSuccess == false )
        {
            Debug.LogWarning( $"request Failed" );
            return;
        }
        if( titleData.IsSuccess == false )
        {
            Debug.LogWarning( $"titleData Failed" );
            return;
        }
        if( userData.IsSuccess == false )
        {
            Debug.LogWarning( $"userData Failed" );
            return;
        }
    }
}
