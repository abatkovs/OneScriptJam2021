
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using UnityEditor;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class TheScript : MonoBehaviour
{
    [SerializeField] private bool _gameStarted;
    
    [SerializeField] private Color _playerColor = Color.green;
    [SerializeField] private Color _enemyColor = Color.red;
    [SerializeField] private GameObject _playerGo;
    [SerializeField] private GameObject _playerTrail;
    private Vector3 _playerStartPosition;
    private Quaternion _playerStartRotation;
    [Space]
    [SerializeField] private GameObject _particlePf;
    [Space]
    [SerializeField] private GameObject _cameraGo;
    [SerializeField] private Vector3 _cameraOffset;
    [Space]
    [SerializeField] private GameObject _trailPf;
    [Space]
    [SerializeField] private float _rollSpeed = 5;
    private bool _isMoving;
    private bool _isKnockedBack;
    [SerializeField] private GameObject _enemyPf;
    [SerializeField] private List<GameObject> _enemies;
    [SerializeField] private int _enemyChecksForObstacles = 5;
    [FormerlySerializedAs("_textPf")] [SerializeField] private TMP_Text _trailTextPf;
    
    [SerializeField] private float _varRayDistance = 2.5f;
    [SerializeField] private List<int> _playerTrailScore = new List<int>();
    [SerializeField] private List<int> _enemyTrailScore = new List<int>();
    [SerializeField] private int _playerScore;
    [SerializeField] private int _enemyScore;

    [Space] [Header("User Interface")] 
    [SerializeField] private TMP_Text _playerScoreTxt;
    [SerializeField] private TMP_Text _enemyScoreTxt;

    private void Start()
    {
        //_gameStarted = true;
        _playerStartPosition = _playerGo.transform.position;
        _playerStartRotation = _playerGo.transform.rotation;
        SpawnEnemies(4);
    }

    private void Update()
    {
        Movement();
        UpdateCameraPosition();
        Debug();
    }

    #region User Interface

    [SerializeField] private Button _startBtn;
    [SerializeField] private Button _restartBtn;
    [SerializeField] private TMP_Text _winTxt;
    [SerializeField] private TMP_Text _loseTxt;
    public void StartGame()
    {
        _gameStarted = true;
        // Hide ui elements
    }

    #endregion

    private void CalculatePlayerScore()
    {
        foreach (var scoreToAdd in _playerTrailScore)
        {
            _playerScore += scoreToAdd;
        }
    }



    private Vector3 RandomV3()
    {
        int minPlayArea = -20;
        int maxPlayArea = 20;
        float gridOffset = 0.5f;
        var x = Random.Range(minPlayArea, maxPlayArea);
        var y = 1.2f;
        var z = Random.Range(minPlayArea, maxPlayArea);
        return new Vector3(x - gridOffset, y, z - gridOffset);
    }

    private void Movement()
    {
        if (!_gameStarted) return;
        if (_playerGo == null) return;
        if (_isMoving || _isKnockedBack) return;

        if (Input.GetKey(KeyCode.A))
        {
            RollDirection(Vector3.left, _playerGo);
            MoveEnemies();
        }
        else if (Input.GetKey(KeyCode.D))
        {
            RollDirection(Vector3.right, _playerGo);
            MoveEnemies();
        }
        else if (Input.GetKey(KeyCode.W))
        {
            RollDirection(Vector3.forward, _playerGo);
            MoveEnemies();
        }
        else if (Input.GetKey(KeyCode.S))
        {
            RollDirection(Vector3.back, _playerGo);
            MoveEnemies();
        }

    }



    private void Debug()
    {
        Vector3 offset = new Vector3();
        //Change
        if (Input.GetKeyDown(KeyCode.R))
        {
            _playerGo.transform.position = _playerStartPosition;
            _playerGo.transform.rotation = _playerStartRotation;
        }

        // if (Input.GetKeyDown(KeyCode.T))
        // {
        //     StartCoroutine(KnockBack(Vector3.forward, _playerGo,6));
        // }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            for (int i = 0; i < 50; i++)
            {
                for (int j = 0; j < 50; j++)
                {
                    Instantiate(_trailPf, new Vector3(i-25,0.92f,j-25), Quaternion.identity);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            RaycastHit[] Results = new RaycastHit[4];
            int hits = Physics.RaycastNonAlloc(_playerGo.transform.position, Vector3.down, Results, Mathf.Infinity);
            for (int i = 0; i < hits; i++)
            {
                UnityEngine.Debug.Log($"Rayhit: {Results[i].transform.name}");
            }
        }
        
        if(Input.GetKeyDown(KeyCode.P)) SpawnEnemies(4);
    }

    private void UpdateCameraPosition()
    {
        if (_playerGo == null) return;
        var transformPosition = _playerGo.transform.position;
        Vector3 cameraVector = new Vector3(transformPosition.x, 0, transformPosition.z);
        _cameraGo.transform.position = cameraVector + _cameraOffset;
    }

    private void RollDirection(Vector3 dir, GameObject go) {
        var anchor = go.transform.position + (Vector3.down + dir) * 0.5f;
        var axis = Vector3.Cross(Vector3.up, dir);
        StartCoroutine(Roll(anchor, axis, go, dir));
    }

    
    private IEnumerator Roll(Vector3 anchor, Vector3 axis, GameObject go, Vector3 dir)
    {
        if (go == null) yield break;
        //CalculatePlayerScore();
        CheckMove(dir,go);
         
        _isMoving = true;
        for (var i = 0; i < 90 / _rollSpeed; i++) {
            go.transform.RotateAround(anchor, axis, _rollSpeed);
            yield return new WaitForSeconds(0.01f);
        }

        CheckMove(Vector3.down, go, _varRayDistance);
        _isMoving = false;

    }
    
    //[SerializeField] private float _reduceVectorBy = 2;
    private void CheckMove(Vector3 movementVector, GameObject owner, float _reduceVectorBy = 2)
    {
        RaycastHit[] Results = new RaycastHit[4];
        string colliderName;
        string goName;
        bool trails = false;
        if (movementVector != Vector3.down)
        {
            trails = CheckForTrails(owner.transform.position, movementVector);
        }
        int hits = Physics.RaycastNonAlloc(owner.transform.position + (movementVector/_reduceVectorBy), movementVector, Results, 1f);
        UnityEngine.Debug.DrawRay(owner.transform.position + (movementVector/_reduceVectorBy), movementVector/2, Color.red, 100f);
        //UnityEngine.Debug.Break();
        if (hits > 0)
        {
            for (int i = 0; i < hits; i++)
            {
                goName = Results[i].transform.name;
                colliderName = Results[i].collider.name;
                UnityEngine.Debug.Log(colliderName);
                if (trails) Destroy(owner);
                if (movementVector == Vector3.down)
                {
//                    UnityEngine.Debug.Log($"Down: {colliderName}");
                    SpawnTrail(owner, $"{colliderName}");
                }
                //UnityEngine.Debug.Break();
                if(colliderName == "Enemy") DestroyOnCollision(Results[i].transform,  owner);
                if(colliderName == "Wall") CollideWithWall(owner);
                if(goName == "Player") continue;

//                UnityEngine.Debug.Log($"goName: {goName} colliderName: {colliderName}");
            }
        }
    }

    private void CollideWithWall(GameObject owner)
    {
        Destroy(owner);
    }

    private void DestroyOnCollision(Transform result, GameObject owner)
    {
        Instantiate(_particlePf, result.position, Quaternion.identity);
        Instantiate(_particlePf, owner.transform.position, Quaternion.identity);
        Destroy(result.gameObject);
        Destroy(owner);
        if (owner == _playerGo)
        {
            UnityEngine.Debug.Log("Player destroyed. > GameOver");
        }
    }


    private IEnumerator KnockBack(Vector3 dir, GameObject go, int times)
    {
        Vector3 anchor;
        Vector3 axis;
        _isKnockedBack = true;
        for (int power = 0; power < times; power++)
        {
            anchor = GetAnchor(dir, go);
            axis = GetAxis(dir);
            for (var i = 0; i < 90 / _rollSpeed; i++) {
                go.transform.RotateAround(anchor, axis, _rollSpeed);
                yield return new WaitForSeconds(0.01f);
            }
        }

        _isKnockedBack = false;
    }

    private static Vector3 GetAxis(Vector3 dir)
    {
        var axis = Vector3.Cross(Vector3.up, dir);
        return axis;
    }

    private static Vector3 GetAnchor(Vector3 dir, GameObject go)
    {
        var anchor = go.transform.position + (Vector3.down + dir) * 0.5f;
        return anchor;
    }

    private void SpawnTrail(GameObject trailOwner, string trailName)
    {
        Vector3 textOffset = new Vector3(0.5f,0.105f,-0.5f);
        
        var offset = new Vector3(trailOwner.transform.position.x-0.5f, 0.92f, trailOwner.transform.position.z+0.5f);
        var trail = Instantiate(_trailPf, offset, Quaternion.identity);
        trail.name = trailName;
        int trailInt = Int32.Parse(trailName);
        var text = Instantiate(_trailTextPf, trail.transform.position + textOffset, _trailTextPf.transform.rotation, trail.transform);
        //Change trail text color
        if (trailOwner.name == "Enemy")
        {
            text.GetComponent<TMP_Text>().color = _enemyColor;
            _enemyScoreTxt.text = $"Enemy Score: {_enemyScore.ToString()}";
            _enemyScore += trailInt;
            _enemyTrailScore.Add(trailInt);
        }
        else
        {
            _playerScoreTxt.text = $"Player Score: {_playerScore.ToString()}";
            _playerScore += trailInt;
            _playerTrailScore.Add(trailInt);
            ValidateScore();
        }
        text.GetComponent<TMP_Text>().text = trailName;
        
    }

    private void ValidateScore()
    {
        if (_playerScore > _enemyScore)
        {
            UnityEngine.Debug.Log("You Win.");
        }
    }


    #region EnemyStuff

    private void AddEnemyToList(GameObject enemy)
    {
        _enemies.Add(enemy);
    }

    private void RemoveEnemyFromList(GameObject enemy)
    {
        _enemies.Remove(enemy);
    }
    private void SpawnEnemies(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject enemy = Instantiate(_enemyPf, RandomV3(), _enemyPf.transform.rotation);
            enemy.name = "Enemy";
            AddEnemyToList(enemy);
        }
    }
    private void MoveEnemies()
    {
        Vector3 randomDirection = new Vector3();
        foreach (var enemy in _enemies)
        {
            
            if(enemy == null) continue;
            randomDirection = RandomVector3Direction();
            //Check for obstacles then move>>>
            CheckForObstacles(enemy, randomDirection);
            

        }
    }

    private void CheckForObstacles(GameObject enemy, Vector3 randomDirection)
    {
        //randomDirection = RandomVector3();
        var enemyPos = enemy.transform.position;
        bool wall = CheckForWall(enemyPos, randomDirection);
        bool path = CheckForTrails(enemyPos, randomDirection);

        for (int i = 0; i < _enemyChecksForObstacles; i++)
        {
            //check for wall
            if (wall)
            {
                //UnityEngine.Debug.Log($"Enemy {enemy.name} detected wall in {randomDirection} direction.");
                randomDirection = RandomVector3Direction();
                wall = CheckForWall(enemyPos, randomDirection);
            }

            if (path)
            {
                randomDirection = RandomVector3Direction();
                path = CheckForTrails(enemyPos, randomDirection);
            }
        }

        RollDirection(randomDirection, enemy);

    }



    private Vector3 RandomVector3Direction()
    {
        Vector3 v = new Vector3();
        int r = Random.Range(0, 3);
        v = r switch
        {
            0 => Vector3.forward,
            1 => Vector3.left,
            2 => Vector3.right,
            3 => Vector3.back,
            _ => v
        };

        return v;
    }

    private bool CheckForWall(Vector3 start, Vector3 dir)
    {
        bool wallDetected = false;
        RaycastHit[] Results = new RaycastHit[4];
        int hits = Physics.RaycastNonAlloc(start + dir, dir, Results, 3f);
        if (hits > 0)
        {
            for (int i = 0; i < hits; i++)
            {
                //UnityEngine.Debug.Log($"Wall check: {Results[i].transform.name}");
                if (Results[i].transform.name == "Wall")
                {
                    wallDetected = true;
                }
            }
        }
        //UnityEngine.Debug.DrawRay(start, (dir * 3) + new Vector3(0,0.1f,0), Color.green, 100f);
        return wallDetected;
    }

    private bool CheckForTrails(Vector3 start, Vector3 dir)
    {
        bool detectPath = false;
        RaycastHit[] Results = new RaycastHit[4];
        int hits = Physics.RaycastNonAlloc(start + dir, Vector3.down, Results, 3f);
        UnityEngine.Debug.DrawRay(start + dir, Vector3.down * 1.2f, Color.magenta, 100f);
        if (hits > 0)
        {
            for (int i = 0; i < hits; i++)
            {
                //UnityEngine.Debug.Log($"Wall check: {Results[i].transform.name}");
                if (Results[i].transform.name is "1" or "2" or "3" or "4" or "5" or "6")
                {
                    detectPath = true;
                }
            }
        }
        return detectPath;
    }
    #endregion
}
