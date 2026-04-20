using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;
using VRT.Transport.WebRTC;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
using VRT.Orchestrator;
using VRT.OrchestratorComm;

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

        [Tooltip("Prefab used to create players")]
        public GameObject PlayerPrefab;
        [Tooltip("Prefab used to create self-player")]
        public GameObject SelfPlayerPrefab;
        [Tooltip("Locations where players will be instantiated")]
		public List<PlayerLocation> PlayerLocations;
		[Tooltip("Location where no-representation players will be instantiated")]
		public Transform NonPlayersLocation;
		[Tooltip("Delay between Start() and InstantiatePlayers()")]
		public float InstantiatePlayersDelay = 2.0f;

		[Tooltip("If true, the players will be put on the available locations in order of appearance in the Player Locations list")]
		public bool AutoSpawnOnLocation = false;
		[Header("Introspection/debugging")]
		[Tooltip("Debugging: the local player")]
		[DisableEditing] public GameObject localPlayer;
		[Tooltip("All players")]
		[DisableEditing] public List<PlayerNetworkControllerBase> AllUsers;

		[Tooltip("Verbose logging")]
		[SerializeField] private bool debug = true;

		private Dictionary<string, PlayerNetworkControllerBase> _playerDict;
		private Dictionary<string, PlayerLocation> _PlayerIdToLocation;
		private Dictionary<PlayerLocation, string> _LocationToPlayerId;

		public Dictionary<string, PlayerNetworkControllerBase> Spectators;
		
		private static SessionPlayersManager _Instance;

		public static SessionPlayersManager Instance
		{
			get
			{
				if (_Instance == null)
				{
					_Instance = FindAnyObjectByType<SessionPlayersManager>();
				}
				return _Instance;
			}
		}

		public void Awake()
		{
			VRTOrchestratorSingleton.Comm.RegisterEventType(MessageTypeID.TID_PlayerLocationData, typeof(PlayerLocationData));
			VRTOrchestratorSingleton.Comm.RegisterEventType(MessageTypeID.TID_PlayerLocationDataRequest, typeof(PlayerLocationDataRequest));
			AllUsers = new List<PlayerNetworkControllerBase>();
			_playerDict = new Dictionary<string, PlayerNetworkControllerBase>();
			_PlayerIdToLocation = new Dictionary<string, PlayerLocation>();
			_LocationToPlayerId = new Dictionary<PlayerLocation, string>();

			Spectators = new Dictionary<string, PlayerNetworkControllerBase>();
			if (VRTOrchestratorSingleton.Comm == null)
			{
				return;
			}
			VRTOrchestratorSingleton.Comm.OnUserLeaveSessionEvent += OnUserLeft;

			if (debug) Debug.Log($"SessionPlayersManager: Awake");
		}

		public void OnEnable()
		{
			VRTOrchestratorSingleton.Comm.Subscribe<PlayerLocationData>(OnPlayerLocationData);
			VRTOrchestratorSingleton.Comm.Subscribe<PlayerLocationDataRequest>(OnPlayerLocationDataRequest);
		}

		public void OnDisable()
		{
			VRTOrchestratorSingleton.Comm?.Unsubscribe<PlayerLocationData>(OnPlayerLocationData);
			VRTOrchestratorSingleton.Comm?.Unsubscribe<PlayerLocationDataRequest>(OnPlayerLocationDataRequest);
		}

		public void OnDestroy()
		{
            if (debug) Debug.Log($"SessionPlayersManager: OnDestroy");
            if (VRTOrchestratorSingleton.Comm != null) VRTOrchestratorSingleton.Comm.OnUserLeaveSessionEvent -= OnUserLeft;
		}

		public void Start()
		{
			StartCoroutine(InstantiatePlayersAfterDelay());
		}

		protected IEnumerator InstantiatePlayersAfterDelay()
		{
			yield return new WaitForSeconds(InstantiatePlayersDelay);
			InstantiatePlayers();
		}

		public PlayerNetworkControllerBase GetPlayerById(string playerId)
		{
			if (_playerDict.ContainsKey(playerId))
			{
				return _playerDict[playerId];
			}

			return null;
		}
		public void SetupConfigDistributors()
		{
            if (debug) Debug.Log($"SessionPlayersManager: SetupConfigDistributors");
            // xxxjack or do we need them sorted?
            var configDistributors = FindObjectsByType<BaseConfigDistributor>(FindObjectsSortMode.None);
            if (configDistributors == null || configDistributors.Length == 0)
            {
                Debug.LogWarning("No BaseConfigDistributor found");
            }
            foreach (var cd in configDistributors)
            {
                cd?.SetSelfUserId(VRTOrchestratorSingleton.Comm.SelfUser.userId);
            }
        }

        public void InstantiatePlayers()
		{
			var me = VRTOrchestratorSingleton.Comm.SelfUser;
#if !VRT_WITHOUT_WEBRTC
            // Initialize WebRTC clientId small integers.
            if (SessionConfig.Instance.protocolType == "webrtc")
            {
                int indexWithinCurrentSession = 0;
                foreach (User user in VRTOrchestratorSingleton.Comm.CurrentSession.GetUsers())
                {
                    bool isLocalPlayer = me.userId == user.userId;
                    // This is a bit of a hack. We need a unique 1-based client ID for webRTC that is the same on the sender side and the
                    // receiver side. The only reassonable way we have to create this is to use the index of this user within the array of
                    // users in this session. We also need our own client ID before we can initialize the webRTC peer process.
                    // This is a bit of a hack. We need a unique 1-based client ID for webRTC that is the same on the sender side and the
                    // receiver side. The only reassonable way we have to create this is to use the index of this user within the array of
                    // users in this session.
                    user.webRTCClientId = indexWithinCurrentSession + 1;
                    if (isLocalPlayer)
                    {
                        me.webRTCClientId = user.webRTCClientId;
						string webrtc_url = user.sfuData.url_gen;
						Debug.Log($"SessionPlayersManager: pre-creating WebRTC connection to {webrtc_url}");
						var dummy = TransportProtocolWebRTC.Connect(webrtc_url);
						if (dummy ==  null)
						{
							Debug.LogError($"SessionPlayersManager: pre-creating WebRTC connection to {webrtc_url} failed");
						}
                    }
                    indexWithinCurrentSession++;
                }
            }
#endif
            

			SetupConfigDistributors();
            if (debug) Debug.Log($"SessionPlayersManager: Instantiating players");

            foreach (User user in VRTOrchestratorSingleton.Comm.CurrentSession.GetUsers())
			{
				bool isLocalPlayer = me.userId == user.userId;
				GameObject player = null;
				if (isLocalPlayer)
				{
					player = Instantiate(SelfPlayerPrefab);
                    localPlayer = player;
                }
                else
				{
                    player = Instantiate(PlayerPrefab);
                }
                player.SetActive(true);
				player.name = $"Player_{user.userId}";


#if VRT_WITH_STATS
                Statistics.Output("SessionPlayerManager", $"self={isLocalPlayer}, userId={user.userId}, userName={user.userName}");
#endif

				PlayerControllerBase playerController = player.GetComponent<PlayerControllerBase>();
                PlayerNetworkControllerBase networkPlayer = player.GetComponent<PlayerNetworkControllerBase>();

                if (isLocalPlayer)
                {
	                playerController.SetUpSelfPlayerController();
                }
                else
                {
	                playerController.SetUpOtherPlayerController(user);
                }
				
                AllUsers.Add(networkPlayer);

				var representationType = user.userData.userRepresentation;
				switch(representationType)
				{
					case UserRepresentationType.NoRepresentation:
					case UserRepresentationType.AudioOnly:
                        AddSpectator(networkPlayer);
                        break;
                    case UserRepresentationType.NoRepresentationCamera:
						AddSpectator(networkPlayer);
#if UNITY_EDITOR
                        if (isLocalPlayer)
                        {
                            Debug.Log($"SessionPlayerManager: Cameraman: {player.name} representationType {representationType}");
                            ((PlayerControllerSelf)playerController).getCameraTransform().GetComponent<VRT.Core.UnityRecorderController>().enabled = true;
                        }
#endif
						break;
                    default:
						AddPlayer(networkPlayer);
						break;
				}
			}


            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
			{
				if (debug) Debug.Log($"SessionPlayersManager: sending playerLocationData to all");
				SendPlayerLocationData();
			}
			else
			{
                if (debug) Debug.Log($"SessionPlayersManager: ask master for playerLocationData");
				RequestPlayerLocationData();
            }
            if (debug) Debug.Log($"SessionPlayersManager: All players instantiated");
			//
			// For WebRTC transport protocol there is some special handling required.
			// We need to communicate our WebRTC client ID (index within the session) and we need
			// to state that all incoming and outgoing connections have been specified (so the peer can be started)
				//
#if !VRT_WITHOUT_WEBRTC
            // Initialize WebRTC and pass our clientId.
            if (SessionConfig.Instance.protocolType == "webrtc")
            {
				TransportProtocolWebRTC webRTC = TransportProtocolWebRTC.GetInstance();
                if (webRTC == null)
                {
                    throw new Exception($"SessionPlayersManager: No TransportProtocolWebRTC.Instance but webRTC protocol requested");
                }

                webRTC.SetClientID(me.webRTCClientId);
                
                webRTC.AllConnectionsDone();
            }
#endif
        }

        private void OnUserLeft(string userId)
		{
            if (debug) Debug.Log($"SessionPlayersManager: OnUserLeft({userId})");

            if (_playerDict.TryGetValue(userId, out PlayerNetworkControllerBase playerToRemove))
			{
				RemovePlayer(playerToRemove);
			} else
			if (Spectators.TryGetValue(userId, out PlayerNetworkControllerBase spectatorToRemove))
			{
				RemoveSpectator(spectatorToRemove);
			} else
			{
				Debug.LogWarning($"SessionPlayersManager: Unknown player left: {userId}");
			}
		}

		private void AddPlayer(PlayerNetworkControllerBase player)
		{
			_playerDict.Add(player.UserId, player);

			if (VRTOrchestratorSingleton.Comm.UserIsMaster && AutoSpawnOnLocation)
			{
				foreach (var location in PlayerLocations)
				{
					if (location.IsEmpty)
					{
                        Debug.Log($"SessionPlayersManager: Initialize player {player.UserId} to location {location.NetworkId}.");

                        location.SetPlayer(player);
						_PlayerIdToLocation[player.UserId] = location;
						_LocationToPlayerId[location] = player.UserId;
						break;
					}
				}
			}
		}

        private void AddSpectator(PlayerNetworkControllerBase player)
        {
            player.transform.SetParent(NonPlayersLocation);
            player.transform.position = NonPlayersLocation.position;
            player.transform.rotation = NonPlayersLocation.rotation;

			Spectators.Add(player.UserId, player);
        }

        private void RemovePlayer(PlayerNetworkControllerBase player)
		{
			_playerDict.Remove(player.UserId);

			if (_PlayerIdToLocation.TryGetValue(player.UserId, out PlayerLocation location))
			{
				location.ClearPlayer();
				_PlayerIdToLocation.Remove(player.UserId);
				_LocationToPlayerId.Remove(location);
			}
			else
			{
				Debug.LogWarning($"SessionPlayerManager: RemovePlayer({player.UserId}) which has no location");
			}

			Destroy(player.gameObject);
		}

		private void RemoveSpectator(PlayerNetworkControllerBase player)
		{
			Spectators.Remove(player.UserId);
			Destroy(player.gameObject);
		}

#region Player Locations
		private void SendPlayerLocationData(string userId = null)
		{
			PlayerLocationData data = new PlayerLocationData(_playerDict.Count);
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
				VRTOrchestratorSingleton.Comm.SendTypeEventToUser(userId, data);
			}
			else
			{
				VRTOrchestratorSingleton.Comm.SendTypeEventToAll(data);
			}
		}

		private void RequestPlayerLocationData()
		{
			if (VRTOrchestratorSingleton.Comm.UserIsMaster)
			{
				Debug.LogError($"SessionPlayersManager: master should not call RequestPlayerLocationData");
			}
			PlayerLocationDataRequest data = new PlayerLocationDataRequest();
			VRTOrchestratorSingleton.Comm.SendTypeEventToMaster(data);
		}

		private void OnPlayerLocationData(PlayerLocationData playerLocationData)
		{
            if (playerLocationData == null)
            {
				Debug.LogWarning($"SessionPlayersManager: OnPlayerLocationData: playerLocationData is null");
                return;
            }
            if (VRTOrchestratorSingleton.Comm.UserIsMaster)
            {
                Debug.LogWarning($"SessionPlayersManager: OnPlayerLocationData: we are not master");
                return;
            }
		
            for (int i = 0; i < playerLocationData.PlayerIds.Length; ++i)
			{
				string playerId = playerLocationData.PlayerIds[i];
				if (_playerDict.ContainsKey(playerId))
				{
					if (debug) Debug.Log($"SessionsPlayerManager: OnPlayerLocationData: set player {playerId} to location {i}");
                    PlayerNetworkControllerBase player = _playerDict[playerId];
					PlayerLocation location = PlayerLocations[playerLocationData.LocationIds[i]];

					SetPlayerToLocation(player, location);
				} else
				{
					Debug.LogWarning($"SessionsPlayersManager: OnPlayerLocationData: unknown player {playerId}");
				}
			}

		}

		private void OnPlayerLocationDataRequest(PlayerLocationDataRequest request)
		{
			if (VRTOrchestratorSingleton.Comm.UserIsMaster)
			{
				Debug.Log($"SessionPlayersManager: OnPlayerLocationDataRequest: reply to {request.SenderId}");
				SendPlayerLocationData(request.SenderId);
			}
			else
			{
                Debug.LogWarning($"SessionPlayersManager: OnPlayerLocationDataRequest: we are not master");
            }
        }
		
		private void SetPlayerToLocation(PlayerNetworkControllerBase player, PlayerLocation location)
		{
			Debug.Log($"SessionPlayersManager: Set player {player.UserId} to location {location.NetworkId}.");

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
#endregion
	}
}