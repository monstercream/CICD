using System.Collections.Generic;
using System.Threading.Tasks;

namespace Network
{
	public interface INetwork
	{
		public Task<NetworkResult> LoginAsync(string titleID, string loginID);
		public Task<NetworkResult> RequestAsync(string functionName, Dictionary<string, string> functionParameter);
		public Task<NetworkResult> UserDataRequestAsync(string[] keys);
		public Task<NetworkResult> TitleDataRequestAsync(string[] keys);
	}

	public class UserData
	{
		public string DisplayName { get; set; }
		public string PlayerID { get; set; }
		public string PlayerIconPath { get; set; }
		public bool IsNewUser { get; set; }

		public UserData(string displayName, string playerID, string playerIconPath, bool isNewUser)
		{
			this.DisplayName = displayName;
			this.PlayerID = playerID;
			this.PlayerIconPath = playerIconPath;
			this.IsNewUser = isNewUser;
		}
	}
}
