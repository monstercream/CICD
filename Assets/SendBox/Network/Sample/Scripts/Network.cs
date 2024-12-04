using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using Object = System.Object;

namespace Network
{
    public class PlayFabException : Exception
    {
        public PlayFabError PlayFabError { get; }

        public PlayFabException(PlayFabError error) : base( error.ErrorMessage )
        {
            foreach (var errorErrorDetail in error.ErrorDetails)
            {
                Debug.LogWarning( errorErrorDetail.Value );
            }

            PlayFabError = error;
        }
    }

    public class NetworkResult
    {
        public bool IsSuccuces;
        public Object Result;

        public NetworkResult(bool isSuccuces, Object result)
        {
            this.IsSuccuces = isSuccuces;
            this.Result = result;
        }
    }

    public class Network : INetwork
    {
        public static UserData UserData;
        private volatile static Int32 _orderNumber_Function = 0;


        public async Task<NetworkResult> LoginAsync(string titleID, string loginID)
        {
            GetPlayerCombinedInfoRequestParams InfoRequestParams = new GetPlayerCombinedInfoRequestParams();
            InfoRequestParams.GetPlayerProfile = true;
            InfoRequestParams.GetUserVirtualCurrency = true;
            InfoRequestParams.GetUserAccountInfo = true;

            Debug.LogWarning( loginID );

            LoginResult loginResult = null;

#if IOS
            LoginWithIOSDeviceIDRequest request = new LoginWithIOSDeviceIDRequest()
            {
                TitleId = titleID,
                DeviceId = loginID,
                CreateAccount = false,
                InfoRequestParameters = InfoRequestParams
            };

            loginResult = await LoginWithIOSDeviceIDAsync(request);
#elif ANDROID
            LoginWithAndroidDeviceIDRequest request = new LoginWithAndroidDeviceIDRequest()
            {
                TitleId = titleID,
                AndroidDeviceId = loginID,
                CreateAccount = false,
                InfoRequestParameters = InfoRequestParams
            };

            loginResult = await LoginWithAndroidDeviceIDAsync(request);
#else
            LoginWithIOSDeviceIDRequest request = new LoginWithIOSDeviceIDRequest()
            {
                TitleId = titleID,
                DeviceId = loginID,
                CreateAccount = false,
                InfoRequestParameters = InfoRequestParams
            };

            loginResult = await LoginWithIOSDeviceIDAsync( request );
#endif
            // 성공적인 로그인 처리

            UserData = new UserData(
                loginResult.InfoResultPayload.PlayerProfile.DisplayName,
                loginResult.InfoResultPayload.PlayerProfile.PlayerId,
                loginResult.InfoResultPayload.AccountInfo.TitleInfo.AvatarUrl,
                loginResult.NewlyCreated
            );

            return new NetworkResult( true, request );
        }

        private Task<LoginResult> LoginWithIOSDeviceIDAsync(LoginWithIOSDeviceIDRequest request)
        {
            var taskCompletionSource = new TaskCompletionSource<LoginResult>();

            PlayFabClientAPI.LoginWithIOSDeviceID( request,
                result => taskCompletionSource.SetResult( result ),
                error => taskCompletionSource.SetException( new PlayFabException( error ) ) );

            return taskCompletionSource.Task;
        }

        private Task<LoginResult> LoginWithAndroidDeviceIDAsync(LoginWithAndroidDeviceIDRequest request)
        {
            var taskCompletionSource = new TaskCompletionSource<LoginResult>();

            PlayFabClientAPI.LoginWithAndroidDeviceID( request,
                result => taskCompletionSource.SetResult( result ),
                error => taskCompletionSource.SetException( new PlayFabException( error ) ) );

            return taskCompletionSource.Task;
        }

        public async Task<NetworkResult> RequestAsync(string functionName, Dictionary<string, string> functionParameter = null)
        {
            var request = new ExecuteCloudScriptRequest
            {
                FunctionName = functionName,
                FunctionParameter = functionParameter,
#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEBUG_BUILD || PRE_RELEASE_BUILD
                GeneratePlayStreamEvent = true,
                RevisionSelection = CloudScriptRevisionOption.Latest
#else
            RevisionSelection = CloudScriptRevisionOption.Live
#endif
            };

            uint orderNumber = (uint)Interlocked.Increment( ref _orderNumber_Function );
            string argsStr = GetParameterString( functionParameter );
            Debug.Log(
                $"<size=15><color=#00ff00ff>Req>> [{orderNumber}] </color></size><b>{functionName}</b>\n{argsStr}" );

            try
            {
                var result = await ExecuteCloudScriptAsync( request );

                foreach (var log in result.Logs)
                {
                    Debug.Log(
                        $"<size=15><color=#1200ffff>Log<< [{orderNumber}] </color></size><b>{functionName}</b>\n{log.Message}" );
                }

                return new NetworkResult( true, result.FunctionResult.ToString() );
            }
            catch( Exception ex )
            {
                Debug.LogError(
                    $"<size=15><color=#ff0000ff>Err<< [{orderNumber}] </color></size><b>{functionName}</b>\n{ex.Message}\n{ex.StackTrace}" );
                return new NetworkResult( false, null );
            }
        }

        public async Task<NetworkResult> UserDataRequestAsync(string[] keys = null)
        {
            UInt32 orderNumber = unchecked((UInt32)Interlocked.Increment( ref _orderNumber_Function ));
            Debug.Log( $"<size=15><color=#00ff00ff>Req>> [{orderNumber}] </color></size><b>{"GetTitleData"}</b>\n{""}" );
            var tcs = new TaskCompletionSource<GetTitleDataResult>();
            GetUserDataResult result;

            var data = new GetUserDataRequest()
            {
                Keys = keys == null ? null : keys.ToList()
            };

            PlayFabClientAPI.GetUserReadOnlyData( data, res =>
                {
                    result = res;
                    Debug.Log( $"<size=15><color=#ffa500ff>Res<< [{orderNumber}] </color></size><b>{"GetUserReadOnlyData"}</b>\n{res}" );
                },
                (error) => { tcs.SetException( new Exception( "An error occurred" + error ) ); } );

            return new NetworkResult( true, result );
        }

        public async Task<NetworkResult> TitleDataRequestAsync(string[] keys = null)
        {
            UInt32 orderNumber = unchecked((UInt32)Interlocked.Increment( ref _orderNumber_Function ));
            Debug.Log( $"<size=15><color=#00ff00ff>Req>> [{orderNumber}] </color></size><b>{"GetTitleData"}</b>\n{""}" );
            var tcs = new TaskCompletionSource<GetTitleDataResult>();
            GetTitleDataResult result;

            var data = new GetTitleDataRequest()
            {
                Keys = keys == null ? null : keys.ToList()
            };

            PlayFabClientAPI.GetTitleData( data, res =>
                {
                    result = res;
                    Debug.Log(
                        $"<size=15><color=#ffa500ff>Res<< [{orderNumber}] </color></size><b>{"GetUserReadOnlyData"}</b>\n{res}" );
                    tcs.SetResult( res );
                },
                (error) => { tcs.SetException( new Exception( "An error occurred" + error ) ); } );

            return new NetworkResult( true, result );
        }

        private Task<ExecuteCloudScriptResult> ExecuteCloudScriptAsync(ExecuteCloudScriptRequest request)
        {
            var taskCompletionSource = new TaskCompletionSource<ExecuteCloudScriptResult>();

            PlayFabClientAPI.ExecuteCloudScript( request,
                result => taskCompletionSource.SetResult( result ),
                error => taskCompletionSource.SetException( new PlayFabException( error ) ) );

            return taskCompletionSource.Task;
        }

        private string GetParameterString(Dictionary<string, string> functionParameter)
        {
            if( functionParameter is Dictionary<string, string> dic )
            {
                var sb = new StringBuilder( dic.Count * 100 );
                foreach (var kv in dic)
                {
                    sb.AppendLine( $"     {kv.Key} : {kv.Value}" );
                }

                return sb.ToString();
            }

            return null;
        }
    }
}