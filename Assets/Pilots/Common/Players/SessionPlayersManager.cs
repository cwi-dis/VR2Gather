using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using VRT.Core;
using VRT.Orchestrator.Wrapping;
using VRT.UserRepresentation.Voice;

namespace VRT.Pilots.Common
{

    /// <summary>
    /// Spawns the player prefab for players in a session, and if we're the 
    /// Master, it assigns them to an available location when AutoSpawnOnLocation is true
    /// </summary>
    public class SessionPlayersManager : MonoBehaviour
	{
		/// <summary>
		/// Empty request message to ask the Master to inform us about the player locations
		/// </summary>
		public class PlayerLocationDataRequest : BaseMessage { }

		/// <summary>
		/// Message sent by the master to inform users of player locations (or their updates)
		/// </summary>
		public class PlayerLocationData : BaseMessage
		{
			public string[] PlayerIds;
			public int[] LocationIds;

			public PlayerLocationData(int numPlayers)
			{
				PlayerIds = new string[numPlayers];
				LocationIds = new int[numPlayers];
			}
		}

		public class PlayerLocationChangeRequest : BaseMessage
		{
			public string LocationNetworkId;
		}

		public GameObject PlayerPrefab;

		public List<PlayerLocation> PlayerLocations;
		public Transform NonPlayersLocation;

		[Tooltip("If true, the players will be put on the available locations in order of appearance in the Player Locations list")]
		public bool AutoSpawnOnLocation = false;

		public List<NetworkPlayer> AllUsers;

		public Dictionary<string, NetworkPlayer> Players;
		private Dictionary<string, PlayerLocation> _PlayerIdToLocation;
		private Dictionary<PlayerLocation, string> _LocationToPlayerId;

		public Dictionary<string, NetworkPlayer> Spectators;
		public Dictionary<string, NetworkPlayer> Voyeurs;

		private static SessionPlayersManager _Instance;

		public static SessionPlayersManager Instance
		{
			get
			{
				if (_Instance == null)
				{
					_Instance = FindObjectOfType<SessionPlayersManager>();
				}
				return _Instance;
			}
		}

		public void Awake()
		{
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_PlayerLocationData, typeof(PlayerLocationData));
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_PlayerLocationDataRequest, typeof(PlayerLocationDataRequest));
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_PlayerLocationChangeRequest, typeof(PlayerLocationChangeRequest));
			AllUsers = new List<NetworkPlayer>();
			Players = new Dictionary<string, NetworkPlayer>();
			_PlayerIdToLocation = new Dictionary<string, PlayerLocation>();
			_LocationToPlayerId = new Dictionary<PlayerLocation, string>();

			Spectators = new Dictionary<string, NetworkPlayer>();
			Voyeurs = new Dictionary<string, NetworkPlayer>();

			OrchestratorController.Instance.OnUserLeaveSessionEvent += OnUserLeft;

			OrchestratorController.Instance.Subscribe<PlayerLocationData>(OnPlayerLocationData);
			OrchestratorController.Instance.Subscribe<PlayerLocationDataRequest>(OnPlayerLocationDataRequest);
			OrchestratorController.Instance.Subscribe<PlayerLocationChangeRequest>(OnPlayerLocationChangeRequest);
		}

		public void OnDestroy()
		{
			OrchestratorController.Instance.OnUserLeaveSessionEvent -= OnUserLeft;

			OrchestratorController.Instance.Unsubscribe<PlayerLocationData>(OnPlayerLocationData);
			OrchestratorController.Instance.Unsubscribe<PlayerLocationDataRequest>(OnPlayerLocationDataRequest);
			OrchestratorController.Instance.Unsubscribe<PlayerLocationChangeRequest>(OnPlayerLocationChangeRequest);
		}

		public void Start()
		{
			InstantiatePlayers();
		}

		public void InstantiatePlayers()
		{
			var me = OrchestratorController.Instance.SelfUser;

			// First tell the tilingConfigDistributor what our user ID is.
			var configDistributors = FindObjectsOfType<BaseConfigDistributor>();
			if (configDistributors == null || configDistributors.Length == 0)
			{
				Debug.LogWarning("No BaseConfigDistributor found");
			}
			foreach(var cd in configDistributors) {
				cd?.Init(OrchestratorController.Instance.SelfUser.userId);
			}
			

			foreach (User user in OrchestratorController.Instance.ConnectedUsers)
			{
				var player = Instantiate(PlayerPrefab);
				player.SetActive(true);
				player.name = $"Player_{user.userId}";
				player.tag = "Player";	// we tag it as player so its easier to find it in the scene

				PlayerManager playerManager = player.GetComponent<PlayerManager>();
				var representationType = user.userData.userRepresentationType;
				
				SetUpPlayerManager(playerManager, user, configDistributors);
				
				NetworkPlayer networkPlayer = player.GetComponent<NetworkPlayer>();
				networkPlayer.UserId = user.userId;
				networkPlayer.SetIsLocalPlayer(me.userId == user.userId);

				AllUsers.Add(networkPlayer);
				if (representationType != UserRepresentationType.__NONE__ && representationType != UserRepresentationType.__SPECTATOR__ && representationType != UserRepresentationType.__CAMERAMAN__)
				{
					AddPlayer(networkPlayer);
				}
				else
				{
					player.transform.SetParent(NonPlayersLocation);
					player.transform.position = NonPlayersLocation.position;
					player.transform.rotation = NonPlayersLocation.rotation;

					if (representationType == UserRepresentationType.__SPECTATOR__)
					{
						Spectators.Add(networkPlayer.UserId, networkPlayer);
						playerManager.cam.fieldOfView = 47.0f;
					}
					else
					{
						// __NONE__ && __CAMERAMAN__
#if UNITY_EDITOR
						if (representationType == UserRepresentationType.__CAMERAMAN__ && me.userId == user.userId) {
							Debug.Log($"-----------------------> {player.name} representationType {representationType}");
							playerManager.getCameraTransform().GetComponent<VRTCore.UnityRecorderController>().enabled = true;
						}
#endif
						Voyeurs.Add(networkPlayer.UserId, networkPlayer);
					}
				}
			}

			if (!OrchestratorController.Instance.UserIsMaster)
			{
				OrchestratorController.Instance.SendTypeEventToMaster(new PlayerLocationDataRequest());
			}
			else
			{
				SendPlayerLocationData();
			}
		}

		//Looks like this could very well be internal to the PlayerManager? 
		private void SetUpPlayerManager(PlayerManager playerManager, User user, BaseConfigDistributor[] configDistributors)
		{

			playerManager.orchestratorId = user.userId;
			playerManager.userName.text = user.userName;

			bool isLocalPlayer = user.userId == OrchestratorController.Instance.SelfUser.userId;
			playerManager.setupInputOutput(isLocalPlayer);
			Transform cameraTransform = null;
			if (isLocalPlayer)
            {
				cameraTransform = playerManager.getCameraTransform();
			}

			VRT.Core.BaseStats.Output("SessionPlayerManager", $"self={isLocalPlayer}, userId={user.userId}, userName={user.userName}");

			if (user.userData.userRepresentationType != UserRepresentationType.__NONE__)
			{
				switch (user.userData.userRepresentationType)
				{
					case UserRepresentationType.__2D__:
						// FER: Implementacion representacion de webcam.
						playerManager.webcam.SetActive(true);
						Config._User userCfg = isLocalPlayer ? Config.Instance.LocalUser : Config.Instance.RemoteUser;
						BasePipeline wcPipeline = BasePipeline.AddPipelineComponent(playerManager.webcam, user.userData.userRepresentationType);
						wcPipeline?.Init(user, userCfg);
						break;
					case UserRepresentationType.__AVATAR__:
						playerManager.avatar.SetActive(true);
						break;
					case UserRepresentationType.__PCC_SYNTH__:
					case UserRepresentationType.__PCC_PRERECORDED__:
					case UserRepresentationType.__PCC_CWIK4A_:
					case UserRepresentationType.__PCC_PROXY__:
					case UserRepresentationType.__PCC_CWI_: // PC
						playerManager.pc.SetActive(true);
						if (cameraTransform)
						{
							Vector3 pos = new Vector3(PlayerPrefs.GetFloat("pcs_pos_x", 0), PlayerPrefs.GetFloat("pcs_pos_y", 0), PlayerPrefs.GetFloat("pcs_pos_z", 0));
							Vector3 rot = new Vector3(PlayerPrefs.GetFloat("pcs_rot_x", 0), PlayerPrefs.GetFloat("pcs_rot_y", 0), PlayerPrefs.GetFloat("pcs_rot_z", 0));
							Debug.Log($"SessionPlayersManager: self-camera pos={pos}, rot={rot}");
							playerManager.cam.gameObject.transform.parent.localPosition = pos;
							playerManager.cam.gameObject.transform.parent.localRotation = Quaternion.Euler(rot);
						}
						userCfg = isLocalPlayer ? Config.Instance.LocalUser : Config.Instance.RemoteUser;
						BasePipeline pcPipeline = BasePipeline.AddPipelineComponent(playerManager.pc, user.userData.userRepresentationType);
						pcPipeline?.Init(user, userCfg);
						if (configDistributors == null || configDistributors.Length == 0)
                        {
							Debug.LogError("Programmer Error: No tilingConfigDistributor, you may not be able to see other participants");
                        }
						// Register for distribution of tiling and sync configurations
						foreach(var cd in configDistributors)
                        {
							cd?.RegisterPipeline(user.userId, pcPipeline);
						}
						
						break;
					default:
						break;


				}

				// Audio
				playerManager.voice.SetActive(true);
				try
				{
					LoadAudio(playerManager, user);
				}
				catch (Exception e)
				{
					Debug.Log($"[SessionPlayersManager] Exception occured when trying to load audio for user {user.userName} - {user.userId}: " + e);
					Debug.LogError($"Cannot receive audio from participant {user.userName}");
					throw;
				}
			}
		}

		public void LoadAudio(PlayerManager player, User user)
		{
			if (user.userData.microphoneName == "None")
            {
				Debug.LogWarning($"SessionPlayersManager: user {user.userId} has no microphone, skipping audio.");
				return;
            }
			if (user.userId == OrchestratorController.Instance.SelfUser.userId)
			{ // Sender
				var AudioBin2Dash = Config.Instance.LocalUser.PCSelfConfig.AudioBin2Dash;
				if (AudioBin2Dash == null)
					throw new Exception("PointCloudPipeline: missing self-user PCSelfConfig.AudioBin2Dash config");
				try
				{
					player.voice.AddComponent<VoiceSender>().Init(user, "audio", AudioBin2Dash.segmentSize, AudioBin2Dash.segmentLife, Config.Instance.protocolType); //Audio Pipeline
				}
				catch (EntryPointNotFoundException e)
				{
					Debug.Log("PointCloudPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
					throw new Exception("PointCloudPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
				}
			}
			else
			{ // Receiver
				var AudioSUBConfig = Config.Instance.RemoteUser.AudioSUBConfig;
				if (AudioSUBConfig == null)
					throw new Exception("PointCloudPipeline: missing other-user AudioSUBConfig config");
				player.voice.AddComponent<VoiceReceiver>().Init(user, "audio", AudioSUBConfig.streamNumber, Config.Instance.protocolType); //Audio Pipeline
			}
		}

		private void OnUserLeft(string userId)
		{
			if (Players.TryGetValue(userId, out NetworkPlayer playerToRemove))
			{
				RemovePlayer(playerToRemove);
			}
			if (Spectators.TryGetValue(userId, out NetworkPlayer spectatorToRemove))
			{
				RemoveSpectator(spectatorToRemove);
			}
			if (Voyeurs.TryGetValue(userId, out NetworkPlayer voyeurToRemove))
			{
				RemoveVoyeur(voyeurToRemove);
			}
		}

		private void AddPlayer(NetworkPlayer player)
		{
			Players.Add(player.UserId, player);

			if (OrchestratorController.Instance.UserIsMaster && AutoSpawnOnLocation)
			{
				foreach (var location in PlayerLocations)
				{
					if (location.IsEmpty)
					{
						location.SetPlayer(player);
						_PlayerIdToLocation[player.UserId] = location;
						_LocationToPlayerId[location] = player.UserId;
						break;
					}
				}

				if (Players.Count > 1)
				{
					//No need to send anything if it's just us
					SendPlayerLocationData();
				}
			}
		}

		private void RemovePlayer(NetworkPlayer player)
		{
			Players.Remove(player.UserId);

			if (_PlayerIdToLocation.TryGetValue(player.UserId, out PlayerLocation location))
			{
				location.ClearPlayer();
				_PlayerIdToLocation.Remove(player.UserId);
				_LocationToPlayerId.Remove(location);
			}

			Destroy(player.gameObject);
		}

		private void RemoveSpectator(NetworkPlayer player)
		{
			Spectators.Remove(player.UserId);
			Destroy(player.gameObject);
		}

		private void RemoveVoyeur(NetworkPlayer player)
		{
			Voyeurs.Remove(player.UserId);
			Destroy(player.gameObject);
		}

		#region Player Locations
		private void SendPlayerLocationData(string userId = null)
		{
			PlayerLocationData data = new PlayerLocationData(Players.Count);
			int dataIndex = 0;
			for (int i = 0; i < PlayerLocations.Count; ++i)
			{
				if (!PlayerLocations[i].IsEmpty)
				{
					data.LocationIds[dataIndex] = i;
					data.PlayerIds[dataIndex] = PlayerLocations[i].CurrentPlayer.UserId;
					++dataIndex;
				}
			}

			//If a specific user requested the data, just send to that user.
			if (!string.IsNullOrEmpty(userId))
			{
				OrchestratorController.Instance.SendTypeEventToUser(userId, data);
			}
			else
			{
				OrchestratorController.Instance.SendTypeEventToAll(data);
			}
		}

		private void OnPlayerLocationData(PlayerLocationData playerLocationData)
		{
			if (playerLocationData == null || OrchestratorController.Instance.UserIsMaster)
			{
				return;
			}

			for (int i = 0; i < playerLocationData.PlayerIds.Length; ++i)
			{
				string playerId = playerLocationData.PlayerIds[i];
				if (Players.ContainsKey(playerId))
				{
					NetworkPlayer player = Players[playerId];
					PlayerLocation location = PlayerLocations[playerLocationData.LocationIds[i]];

					SetPlayerToLocation(player, location);
				}
			}

		}

		private void OnPlayerLocationDataRequest(PlayerLocationDataRequest request)
		{
			if (OrchestratorController.Instance.UserIsMaster)
			{
				SendPlayerLocationData(request.SenderId);
			}
		}

		public void RequestLocationChangeForPlayer(string locationNetworkId, NetworkPlayer player)
		{
			Debug.Log($"[SessionPlayersManager] Requesting location {locationNetworkId} for player {player.UserId}.");

			if (!OrchestratorController.Instance.UserIsMaster)
			{
				Debug.LogError("Programmer error: [SessionsPlayersManager] Requesting location change for a specific player while not master.");
				return;
			}

			if (TryGetPlayerLocationFromNetworkId(locationNetworkId, out PlayerLocation location))
			{
				if (location.IsEmpty && location.isActiveAndEnabled)
				{
					if (OrchestratorController.Instance.UserIsMaster)
					{
						SetPlayerToLocation(Players[player.UserId], location);
						SendPlayerLocationData();
					}
				}
				else
				{
					Debug.LogWarning("[SessionsPlayersManager] Location was already occupied or not active.");
				}
			}
		}

		public void RequestLocationChange(string locationNetworkId)
		{
			if (TryGetPlayerLocationFromNetworkId(locationNetworkId, out PlayerLocation location))
			{
				if (location.IsEmpty)
				{
					var me = OrchestratorController.Instance.SelfUser;
					if (OrchestratorController.Instance.UserIsMaster)
					{
						SetPlayerToLocation(Players[me.userId], location);
					}
					else
					{
						OrchestratorController.Instance.SendTypeEventToMaster
							(
							new PlayerLocationChangeRequest { LocationNetworkId = locationNetworkId }
							);
					}
				} else
				{
					Debug.Log($"[SessionsPlayersManager] RequestLocationChange destination {locationNetworkId} is occupied");
				}
			} else
			{
				Debug.Log($"[SessionPlayersManager] could not TryGetPlayerLocationFromNetworkId for {locationNetworkId}");
			}
		}

		private void OnPlayerLocationChangeRequest(PlayerLocationChangeRequest locationChangeRequest)
		{
			if (OrchestratorController.Instance.UserIsMaster)
			{
				if (TryGetPlayerLocationFromNetworkId(locationChangeRequest.LocationNetworkId, out PlayerLocation location))
				{
					SetPlayerToLocation(Players[locationChangeRequest.SenderId], location);
					SendPlayerLocationData();
				}
			}
		}

		private void SetPlayerToLocation(NetworkPlayer player, PlayerLocation location)
		{
			Debug.Log($"[SessionPlayersManager] Set player {player.UserId} to location {location.NetworkId}.");

			string playerId = player.UserId;

			if (!location.IsEmpty && location.CurrentPlayer.UserId == playerId)
			{
				//All good, nothing to do;
				return;
			}

			if (_PlayerIdToLocation.ContainsKey(playerId))
			{
				var currentLocation = _PlayerIdToLocation[playerId];
				currentLocation.ClearPlayer();
				_LocationToPlayerId.Remove(currentLocation);
			}

			location.SetPlayer(player);
			_PlayerIdToLocation[playerId] = location;
			_LocationToPlayerId[location] = playerId;
		}

		private bool TryGetPlayerLocationFromNetworkId(string networkId, out PlayerLocation playerLocation)
		{
			foreach (var location in PlayerLocations)
			{
				if (location.NetworkId == networkId && location.IsEmpty)
				{
					playerLocation = location;
					return true;
				}
			}

			playerLocation = null;
			return false;
		}
		#endregion
	}
}