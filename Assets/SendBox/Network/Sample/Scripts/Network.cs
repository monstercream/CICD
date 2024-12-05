using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

namespace Network
{
    public class PlayFabException : Exception
    {
        public PlayFabError PlayFabError { get; }

        public PlayFabException(PlayFabError error) : base(error.ErrorMessage)
        {
            foreach (var errorErrorDetail in error.ErrorDetails)
            {
                Debug.LogWarning(errorErrorDetail.Key + ": " + string.Join(", ", errorErrorDetail.Value));
            }

            PlayFabError = error;
        }
    }

    public class NetworkResult
    {
        public bool IsSuccess { get; private set; }  // Fixed typo in property name
        public Dictionary<string, string> TitleResult { get; private set; }
        public Dictionary<string, UserDataRecord> ReadResult { get; private set; }

        public NetworkResult(bool isSuccess)
        {
            
        }
        
        public NetworkResult(bool isSuccess, Dictionary<string, string> result)
        {
            IsSuccess = isSuccess;
            TitleResult = result;
        }
        
        public NetworkResult(bool isSuccess, Dictionary<string, UserDataRecord> result)
        {
            IsSuccess = isSuccess;
            ReadResult = result;
        }
    }

    public class Network : INetwork
    {
        public static UserData UserData { get; private set; }
        private static int _orderNumber_Function;

        public async Task<NetworkResult> LoginAsync(string titleID, string loginID)
        {
            var infoRequestParams = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true,
                GetUserVirtualCurrency = true,
                GetUserAccountInfo = true
            };

            Debug.LogWarning(loginID);

            LoginResult loginResult;

#if UNITY_IOS
            var request = new LoginWithIOSDeviceIDRequest
            {
                TitleId = titleID,
                DeviceId = loginID,
                CreateAccount = false,
                InfoRequestParameters = infoRequestParams
            };

            loginResult = await LoginWithIOSDeviceIDAsync(request);
#elif UNITY_ANDROID
            var request = new LoginWithAndroidDeviceIDRequest
            {
                TitleId = titleID,
                AndroidDeviceId = loginID,
                CreateAccount = false,
                InfoRequestParameters = infoRequestParams
            };

            loginResult = await LoginWithAndroidDeviceIDAsync(request);
#else
            var request = new LoginWithIOSDeviceIDRequest
            {
                TitleId = titleID,
                DeviceId = loginID,
                CreateAccount = false,
                InfoRequestParameters = infoRequestParams
            };

            loginResult = await LoginWithIOSDeviceIDAsync(request);
#endif

            UserData = new UserData(
                loginResult.InfoResultPayload.PlayerProfile.DisplayName,
                loginResult.InfoResultPayload.PlayerProfile.PlayerId,
                loginResult.InfoResultPayload.AccountInfo.TitleInfo.AvatarUrl,
                loginResult.NewlyCreated
            );

            return new NetworkResult(true, new Dictionary<string, string>());
        }

        private Task<LoginResult> LoginWithIOSDeviceIDAsync(LoginWithIOSDeviceIDRequest request)
        {
            var taskCompletionSource = new TaskCompletionSource<LoginResult>();

            PlayFabClientAPI.LoginWithIOSDeviceID(request,
                result => taskCompletionSource.SetResult(result),
                error => taskCompletionSource.SetException(new PlayFabException(error)));

            return taskCompletionSource.Task;
        }

        private Task<LoginResult> LoginWithAndroidDeviceIDAsync(LoginWithAndroidDeviceIDRequest request)
        {
            var taskCompletionSource = new TaskCompletionSource<LoginResult>();

            PlayFabClientAPI.LoginWithAndroidDeviceID(request,
                result => taskCompletionSource.SetResult(result),
                error => taskCompletionSource.SetException(new PlayFabException(error)));

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

            uint orderNumber = (uint)Interlocked.Increment(ref _orderNumber_Function);
            string argsStr = GetParameterString(functionParameter);
            Debug.Log($"<size=15><color=#00ff00ff>Req>> [{orderNumber}] </color></size><b>{functionName}</b>\n{argsStr}");

            try
            {
                var result = await ExecuteCloudScriptAsync(request);

                foreach (var log in result.Logs)
                {
                    Debug.Log($"<size=15><color=#1200ffff>Log<< [{orderNumber}] </color></size><b>{functionName}</b>\n{log.Message}");
                }

                var functionResult = result.FunctionResult as Dictionary<string, string>;
                return new NetworkResult(true, functionResult);
            }
            catch (Exception ex)
            {
                Debug.LogError($"<size=15><color=#ff0000ff>Err<< [{orderNumber}] </color></size><b>{functionName}</b>\n{ex.Message}\n{ex.StackTrace}");
                return new NetworkResult(false);
            }
        }

        public async Task<NetworkResult> UserDataRequestAsync(string[] keys = null)
        {
            uint orderNumber = unchecked((uint)Interlocked.Increment(ref _orderNumber_Function));
            Debug.Log($"<size=15><color=#00ff00ff>Req>> [{orderNumber}] </color></size><b>{"GetUserData"}</b>");

            try
            {
                var result = await GetUserDataAsync(keys);
                Debug.Log($"<size=15><color=#ffa500ff>Res<< [{orderNumber}] </color></size><b>{"GetUserReadOnlyData"}</b>\n{result}");
                return new NetworkResult(true, result.Data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"<size=15><color=#ff0000ff>Err<< [{orderNumber}] </color></size>\n{ex.Message}");
                return new NetworkResult(false);
            }
        }

        private Task<GetUserDataResult> GetUserDataAsync(string[] keys)
        {
            var taskCompletionSource = new TaskCompletionSource<GetUserDataResult>();
            var request = new GetUserDataRequest
            {
                Keys = keys?.ToList()
            };

            PlayFabClientAPI.GetUserReadOnlyData(request,
                result => taskCompletionSource.SetResult(result),
                error => taskCompletionSource.SetException(new PlayFabException(error)));

            return taskCompletionSource.Task;
        }

        public async Task<NetworkResult> TitleDataRequestAsync(string[] keys = null)
        {
            uint orderNumber = unchecked((uint)Interlocked.Increment(ref _orderNumber_Function));
            Debug.Log($"<size=15><color=#00ff00ff>Req>> [{orderNumber}] </color></size><b>{"GetTitleData"}</b>");

            try
            {
                var result = await GetTitleDataAsync(keys);
                Debug.Log($"<size=15><color=#ffa500ff>Res<< [{orderNumber}] </color></size><b>{"GetTitleData"}</b>\n{result}");
                return new NetworkResult(true, result.Data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"<size=15><color=#ff0000ff>Err<< [{orderNumber}] </color></size>\n{ex.Message}");
                return new NetworkResult(false);
            }
        }

        private Task<GetTitleDataResult> GetTitleDataAsync(string[] keys)
        {
            var taskCompletionSource = new TaskCompletionSource<GetTitleDataResult>();
            var request = new GetTitleDataRequest
            {
                Keys = keys?.ToList()
            };

            PlayFabClientAPI.GetTitleData(request,
                result => taskCompletionSource.SetResult(result),
                error => taskCompletionSource.SetException(new PlayFabException(error)));

            return taskCompletionSource.Task;
        }

        private Task<ExecuteCloudScriptResult> ExecuteCloudScriptAsync(ExecuteCloudScriptRequest request)
        {
            var taskCompletionSource = new TaskCompletionSource<ExecuteCloudScriptResult>();

            PlayFabClientAPI.ExecuteCloudScript(request,
                result => taskCompletionSource.SetResult(result),
                error => taskCompletionSource.SetException(new PlayFabException(error)));

            return taskCompletionSource.Task;
        }

        private string GetParameterString(Dictionary<string, string> functionParameter)
        {
            if (functionParameter == null) return null;

            var sb = new StringBuilder(functionParameter.Count * 100);
            foreach (var kv in functionParameter)
            {
                sb.AppendLine($"     {kv.Key} : {kv.Value}");
            }

            return sb.ToString();
        }
    }
}