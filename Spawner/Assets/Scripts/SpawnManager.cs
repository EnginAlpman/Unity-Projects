using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public enum PlayerTeam
{
    None,
    BlueTeam,

    RedTeam
}

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private List<SpawnPoint> _sharedSpawnPoints = new List<SpawnPoint>();
    System.Random _random = new System.Random();
	float _closestDistance;
    [Tooltip("This will be used to calculate the second filter where algorithm looks for closest friends, if the friends are away from this value, they will be ignored")]
    [SerializeField] private float _maxDistanceToClosestFriend = 30;
    [Tooltip("This will be used to calculate the first filter where algorithm looks for enemies that are far away from this value. Only enemies which are away from this value will be calculated.")]
    [SerializeField] private float _minDistanceToClosestEnemy = 10;
    [Tooltip("This value is to prevent friendly player spawning on top of eachothers. If a player is within the range of this value to a spawn point, that spawn point will be ignored")]
    [SerializeField] private float _minMemberDistance = 2;


    public DummyPlayer PlayerToBeSpawned;
    public DummyPlayer[] DummyPlayers;

    private void Awake()
    {
		_sharedSpawnPoints.AddRange(FindObjectsOfType<SpawnPoint>());

		DummyPlayers = FindObjectsOfType<DummyPlayer>();
    }

    #region SPAWN ALGORITHM
    public SpawnPoint GetSharedSpawnPoint(PlayerTeam team)
    {

		List<SpawnPoint> spawnPoints = new List<SpawnPoint>(_sharedSpawnPoints.Count);

		CalculateDistancesForSpawnPoints(team);

		GetSpawnPointsByDistanceSpawning(team, ref spawnPoints);

		if (spawnPoints.Count <= 0)//if at the result of the GetSpawnPointsByDistanceSpawning method no proper point can be found, find spawn point using GetSpawnPointsBySquadSpawning method
		{
			GetSpawnPointsBySquadSpawning(team, ref spawnPoints);
		}

		SpawnPoint spawnPoint = spawnPoints[0];//I changed the code here because _random.Next(0, (int)((float)spawnPoints.Count * .5f)) part in the previous one cause game to behave randomly at choosing spawn point when GetSpawnPointsBySquadSpawning gets involved

		spawnPoint.StartTimer();//start the timer on the chosen spawn point

		return spawnPoint;
    }



	/// <summary>
	/// Select active spawn point (the point that its timer has not been started or 2 seconds after its timer started) that has valid values for DistanceToClosestEnemy and DistanceToClosestFriend 
	/// Compare valid points and return one that is the closest to the enemy
	/// </summary>
	private void GetSpawnPointsByDistanceSpawning(PlayerTeam team, ref List<SpawnPoint> spawnPoints)
    {
		//create list of SpawnPoint to fill it with  most proper spawn point. The list will have most 1 value in it.
		//the list type is chosen because of the compatibility of this method with GetSharedSpawnPoint method
		//where logical comparison and later assignment are done using list of SpawnPoint s
		//List<SpawnPoint> optimalSpawnPoints = new List<SpawnPoint>(); 
		spawnPoints = new List<SpawnPoint>();
		



		foreach (var point in _sharedSpawnPoints)//iterate over all of the spawnPoints 
		{
			//check if the point is active(by the means of spawnTimer) and has the proper distance to the enemies and friends
			//proper distance: closest enemy must be far away at least by the  _minDistanceToClosestEnemy's value
			//and closest enemy must be far away at least by the _minMemberDistance's value
			if ((point.SpawnTimer <= Mathf.Epsilon)
				&& (point.DistanceToClosestEnemy > _minDistanceToClosestEnemy)
				&& (point.DistanceToClosestFriend > _minMemberDistance)
				)

			{


				if (spawnPoints.Count == 0)//if the list is emty, there are no other points to compare for which one is more suitable, so add first appropriate one to the list
				{
					
					spawnPoints.Add(point);
				}

				else
				{
					if (point.DistanceToClosestEnemy <  spawnPoints[0].DistanceToClosestEnemy)//Now list is not empty so we can compare their distance to the closest enemy and choose closer one over other
					{
						spawnPoints[0] = point;//update the fist one to the new point that is closer to the enemy
					}
				}
			}
		}

		//ref keyword is used so this will update the spawnPoints list so GetSharedSpawnPoint can work with it.
		//If no appropriate was found here, spawnPoints will be a empty list and it will trigger the  GetSpawnPointsBySquadSpawning method
		if(spawnPoints.Count == 1)
		{
			Debug.Log(spawnPoints[0] + " is chosen because this is the point which meets GetSpawnPointsByDistanceSpawning conditions and the closest to the enemies among active ones.");
		}
	}











	private void GetSpawnPointsBySquadSpawning(PlayerTeam team, ref List<SpawnPoint> suitableSpawnPoints)
    {
        if (suitableSpawnPoints == null)
        {
            suitableSpawnPoints = new List<SpawnPoint>();
        }
        suitableSpawnPoints.Clear();
        _sharedSpawnPoints.Sort(delegate (SpawnPoint a, SpawnPoint b)//sort the _sharedSpawnPoints list so that the points with lower DistanceToClosestFriend comes before others
		{
            if (a.DistanceToClosestFriend == b.DistanceToClosestFriend)
            {
                return 0;
            }
            if (a.DistanceToClosestFriend > b.DistanceToClosestFriend)
            {
                return 1;
            }
            return -1;
        });

		//iterate over _sharedSpawnPoints and add the points that have DistanceToClosestFriend variable less or equal than _maxDistanceToClosestFriend to the suitableSpawnPoints list.
		//Because of the _sharedSpawnPoints was sorted , the points that are closer to the friend objects will come before others
		for (int i = 0; i < _sharedSpawnPoints.Count && _sharedSpawnPoints[i].DistanceToClosestFriend <= _maxDistanceToClosestFriend; i++)
        {
            if (!(_sharedSpawnPoints[i].DistanceToClosestFriend <= _minMemberDistance) && !(_sharedSpawnPoints[i].DistanceToClosestEnemy <= _minMemberDistance) && _sharedSpawnPoints[i].SpawnTimer <= 0)
            {
                suitableSpawnPoints.Add(_sharedSpawnPoints[i]);
            }
        }

		//if no points could be found, return the closest to the friend 
        if (suitableSpawnPoints.Count <= 0)
        {
            suitableSpawnPoints.Add(_sharedSpawnPoints[0]);
			Debug.Log(_sharedSpawnPoints[0] +" is chosen because there is no other active point satisfying our conditions");
        }

		else
		{
			Debug.Log(suitableSpawnPoints[0] + " is chosen because there is no active point satisfying GetSpawnPointsByDistanceSpawning's conditions so spawning was made according to distance to the friends, and this is the closest to the friends among active ones ");
		}

    }


	/// <summary>
	///  Sets DistanceToClosestFriend and  DistanceToClosestEnemy attributes  of the SpawnPoint s
	/// </summary>
	private void CalculateDistancesForSpawnPoints(PlayerTeam playerTeam)
    {
        for (int i = 0; i < _sharedSpawnPoints.Count; i++)
        {
            _sharedSpawnPoints[i].DistanceToClosestFriend = GetDistanceToClosestMember(_sharedSpawnPoints[i].PointTransform.position, playerTeam);//Call GetDistanceToClosestMember method with the player team attribute that is same as player to be spawned
			_sharedSpawnPoints[i].DistanceToClosestEnemy = GetDistanceToClosestMember(_sharedSpawnPoints[i].PointTransform.position, playerTeam == PlayerTeam.BlueTeam ? PlayerTeam.RedTeam : playerTeam == PlayerTeam.RedTeam ? PlayerTeam.BlueTeam : PlayerTeam.None);//Call GetDistanceToClosestMember method with the player team attribute that is the type of the enemy of a player to be spawned. If player to be spawned has a type None, calls method with playerTeam as None

			
		}
    }


	//calculates the closest distance of the spawnpoints relative to enemies and friends.
    private float GetDistanceToClosestMember(Vector3 position, PlayerTeam playerTeam)// This method is where the bug was.
	{
		bool isFirstTime = true; // creating boolean variable to control if code insede the foreach is executed firs time 

		foreach (var player in DummyPlayers)
        {
            if (!player.Disabled && player.PlayerTeamValue != PlayerTeam.None && player.PlayerTeamValue == playerTeam && !player.IsDead())//if the player is not disabled and dead, does not have None teamvalue and has same type as player to be spawned proceed with code below 
            {
                float playerDistanceToSpawnPoint = Vector3.Distance(position, player.Transform.position);//calculates the distance between dummy player and the spawn point

				if (isFirstTime) // There is no other distance to compare, so in the first execution, set _closestDistance to the first "player" s distance
				{
					_closestDistance = playerDistanceToSpawnPoint;
					isFirstTime = false;
				}

				else // the _closestDistance assigned a value in the first execution so we can compare it to new values to find smallest value
				{
					if (playerDistanceToSpawnPoint < _closestDistance)
					{
						_closestDistance = playerDistanceToSpawnPoint;
					}
				}

				
            }
        }
        return _closestDistance;
    }

    #endregion
	/// <summary>
	/// Test için paylaşımlı spawn noktalarından en uygun olanını seçer.
	/// Test oyuncusunun pozisyonunu seçilen spawn noktasına atar.
	/// </summary>
    public void TestGetSpawnPoint()
    {
    	SpawnPoint spawnPoint = GetSharedSpawnPoint(PlayerToBeSpawned.PlayerTeamValue);
    	PlayerToBeSpawned.Transform.position = spawnPoint.PointTransform.position;
    }

}