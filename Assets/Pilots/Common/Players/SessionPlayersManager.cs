using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using VRT.Core;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
using VRT.Orchestrator.Wrapping;

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
        [Tooltip("Prefab used to create players")]
        public GameObject PlayerPrefab;
        [Tooltip("Prefab used to create self-player")]
        public GameObject SelfPlayerPrefab;
        [Tooltip("Locations where players will be instantiated")]
		public List<PlayerLocation> PlayerLocations;
		[Tooltip("Location where no-representation players will be instantiated")]
		public Transform NonPlayersLocation;

		[Tooltip("If true, the players will be put on the available locations in order of appearance in the Player Locations list")]
		public bool AutoSpawnOnLocation = false;
		[Header("Introspection/debugging")]
		[Tooltip("Debugging: the local player")]
		public GameObject localPlayer;
		[Tooltip("All players")]
		public List<PlayerNetworkController> AllUsers;

		public Dictionary<string, PlayerNetworkController> Players;
		private Dictionary<string, PlayerLocation> _PlayerIdToLocation;
		private Dictionary<PlayerLocation, string> _LocationToPlayerId;

		public Dictionary<string, PlayerNetworkController> Spectators;
		public Dictionary<string, PlayerNetworkController> Voyeurs;

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
			AllUsers = new List<PlayerNetworkController>();
			Players = new Dictionary<string, PlayerNetworkController>();
			_PlayerIdToLocation = new Dictionary<string, PlayerLocation>();
			_LocationToPlayerId = new Dictionary<PlayerLocation, string>();

			Spectators = new Dictionary<string, PlayerNetworkController>();
			Voyeurs = new Dictionary<string, PlayerNetworkController>();

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
                playerController.SetUpPlayerController(isLocalPlayer, user, configDistributors);

				PlayerNetworkController networkPlayer = player.GetComponent<PlayerNetworkController>();
                networkPlayer.SetupPlayerNetworkControllerPlayer(isLocalPlayer, user.userId);


                AllUsers.Add(networkPlayer);

				var representationType = user.userData.userRepresentationType;
				if (representationType == UserRepresentationType.__SPECTATOR__)
				{
					AddSpectator(networkPlayer);
				}
				else
				if (representationType == UserRepresentationType.__NONE__)
				{
					AddVoyeur(networkPlayer);
				}
				else
				if (representationType == UserRepresentationType.__CAMERAMAN__)
				{
					AddVoyeur(networkPlayer);
#if UNITY_EDITOR
                    if (me.userId == user.userId)
                    {
                        Debug.Log($"SessionPlayerManager: Cameraman: {player.name} representationType {representationType}");
                        playerController.getCameraTransform().GetComponent<VRT.Core.UnityRecorderController>().enabled = true;
                    }
#endif
                }
                else
				if (representationType != UserRepresentationType.__CAMERAMAN__)
				{
					AddPlayer(networkPlayer);
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

		private void OnUserLeft(string userId)
		{
			if (Players.TryGetValue(userId, out PlayerNetworkController playerToRemove))
			{
				RemovePlayer(playerToRemove);
			}
			if (Spectators.TryGetValue(userId, out PlayerNetworkController spectatorToRemove))
			{
				RemoveSpectator(spectatorToRemove);
			}
			if (Voyeurs.TryGetValue(userId, out PlayerNetworkController voyeurToRemove))
			{
				RemoveVoyeur(voyeurToRemove);
			}
		}

		private void AddPlayer(PlayerNetworkController player)
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

        private void AddSpectator(PlayerNetworkController player)
        {
            player.transform.SetParent(NonPlayersLocation);
            player.transform.position = NonPlayersLocation.position;
            player.transform.rotation = NonPlayersLocation.rotation;

			Spectators.Add(player.UserId, player);
           

        }

        private void AddVoyeur(PlayerNetworkController player)
        {
            player.transform.SetParent(NonPlayersLocation);
            player.transform.position = NonPlayersLocation.position;
            player.transform.rotation = NonPlayersLocation.rotation;

            Voyeurs.Add(player.UserId, player);
        }

        private void RemovePlayer(PlayerNetworkController player)
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

		private void RemoveSpectator(PlayerNetworkController player)
		{
			Spectators.Remove(player.UserId);
			Destroy(player.gameObject);
		}

		private void RemoveVoyeur(PlayerNetworkController player)
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
                    PlayerNetworkController player = Players[playerId];
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

		public void RequestLocationChangeForPlayer(string locationNetworkId, PlayerNetworkController player)
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

		private void SetPlayerToLocation(PlayerNetworkController player, PlayerLocation location)
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