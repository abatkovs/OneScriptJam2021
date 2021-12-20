
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class TheScript : MonoBehaviour
{
    [SerializeField] private bool _gameStarted;
    private static string _enemyStr = "Enemy";
    private static string _playerStr = "Player";

    
    [SerializeField] private Color _playerColor = Color.green;
    [SerializeField] private Color _enemyColor = Color.red;
    [SerializeField] private GameObject _playerGo;
    [SerializeField] private float _playerMaxYpos = 0;
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
    [SerializeField] private GameObject _hunter;
    [SerializeField] private GameObject _enemyPf;
    [SerializeField] private List<GameObject> _enemies;
    [SerializeField] private int _enemyChecksForObstacles = 5;
    [FormerlySerializedAs("_textPf")] [SerializeField] private TMP_Text _trailTextPf;
    
    [SerializeField] private float _varRayDistance = 2.5f;
    [SerializeField] private List<int> _playerTrailScore = new List<int>();
    [SerializeField] private List<int> _enemyTrailScore = new List<int>();
    [SerializeField] private int _playerScore;
    [SerializeField] private int _enemyScore;


    private void Start()
    {
        //_gameStarted = true;
        _playerStartPosition = _playerGo.transform.position;
        _playerStartRotation = _playerGo.transform.rotation;
        SpawnEnemies(4);
        SpawnHunter();
    }

    private void Update()
    {
        
        Movement();
        UpdateCameraPosition();
        if (_playerGo == null) return;
        UpdateMaxY();
        //Debug();
    }

    //First time collision not detected correctly and rigid bodies go wild.
    private void UpdateMaxY()
    {
        float colisionDiff = 1.8f;
        float playerY = _playerGo.transform.position.y;
        if (playerY > _playerMaxYpos)
        {
            _playerMaxYpos = playerY;
        }

        if(_playerMaxYpos > colisionDiff) NotDetectedCollision();


    }

    #region User Interface    
    [Space] [Header("User Interface")] 
    [SerializeField] private TMP_Text _playerScoreTxt;
    [SerializeField] private TMP_Text _enemyScoreTxt;
    
    [SerializeField] private Button _startBtn;
    [SerializeField] private Button _restartBtn;
    [SerializeField] private TMP_Text _winTxt;
    [SerializeField] private TMP_Text _loseTxt;
    [SerializeField] private GameObject _BG;
    [SerializeField] private GameObject _creditsBtn;
    [SerializeField] private GameObject _credits;
    public void StartGame()
    {
        _creditsBtn.SetActive(false);
        _BG.SetActive(false);
        _playerScoreTxt.transform.gameObject.SetActive(true);
        _enemyScoreTxt.transform.gameObject.SetActive(true);
        _gameStarted = true;
        // Hide ui elements
        _startBtn.transform.gameObject.SetActive(false);
    }

    public void ToggleCredits()
    {
        _credits.SetActive(!_credits.activeSelf);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(0);
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

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            RollDirection(Vector3.left, _playerGo);
            MoveEnemies();
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            RollDirection(Vector3.right, _playerGo);
            MoveEnemies();
        }
        else if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            RollDirection(Vector3.forward, _playerGo);
            MoveEnemies();
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            RollDirection(Vector3.back, _playerGo);
            MoveEnemies();
        }

    }



    private void Debug()
    {
        //Vector3 offset = new Vector3();
        //Change
        // if (Input.GetKeyDown(KeyCode.R))
        // {
        //     _playerGo.transform.position = _playerStartPosition;
        //     _playerGo.transform.rotation = _playerStartRotation;
        // }

        // if (Input.GetKeyDown(KeyCode.T))
        // {
        //     StartCoroutine(KnockBack(Vector3.forward, _playerGo,6));
        // }

        // if (Input.GetKeyDown(KeyCode.Y))
        // {
        //     for (int i = 0; i < 50; i++)
        //     {
        //         for (int j = 0; j < 50; j++)
        //         {
        //             Instantiate(_trailPf, new Vector3(i-25,0.92f,j-25), Quaternion.identity);
        //         }
        //     }
        // }
        //
        // if (Input.GetKeyDown(KeyCode.Q))
        // {
        //     RaycastHit[] Results = new RaycastHit[4];
        //     int hits = Physics.RaycastNonAlloc(_playerGo.transform.position, Vector3.down, Results, Mathf.Infinity);
        //     for (int i = 0; i < hits; i++)
        //     {
        //         UnityEngine.Debug.Log($"Rayhit: {Results[i].transform.name}");
        //     }
        // }
        
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

        CheckMove(Vector3.down, go/*, _varRayDistance*/);

        _isMoving = false;

    }
    


    private void MoveEnemies()
    {
        HunterLogic();
        Vector3 randomDirection = new Vector3();
        foreach (var enemy in _enemies)
        {
            
            if(enemy == null) continue;
            randomDirection = RandomVector3Direction();
            //Check for obstacles then move>>>
            CheckForObstacles(enemy, randomDirection);
            

        }
        
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
            trails = CheckForTrails(owner.transform, movementVector);
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
                //if(owner.name == "Hunter") UnityEngine.Debug.Log($"Hunter collision logic: {Results[i].transform.name}");
                if (trails)
                {
                    DestroyOnCollision(Results[i].transform.gameObject,  owner);
                }
                
                if (movementVector == Vector3.down)
                {
//                    UnityEngine.Debug.Log($"Down: {colliderName}");
                    SpawnTrail(owner, $"{colliderName}");
                }
                //UnityEngine.Debug.Break();
                if (colliderName == "Enemy")
                {
                    UnityEngine.Debug.Log($"Enemy vs: {owner} {Results[i].transform.gameObject}");
                    DestroyOnCollision(Results[i].transform.gameObject,  owner);
                }
                if(colliderName == "Wall") DestroyOnCollision(Results[i].transform.gameObject,  owner);
                if(colliderName == "Hunter") DestroyOnCollision(Results[i].transform.gameObject,  owner);
                if(goName == "Player") continue;
                trails = false;
//                UnityEngine.Debug.Log($"goName: {goName} colliderName: {colliderName}");
            }
        }
    }

    private void CheckMove(Vector3 movementVector, GameObject mover)
    {
        string colliderName = "";
        bool trails = false;
        if (movementVector != Vector3.down)
        {
            trails = CheckForTrails(mover.transform, movementVector);
        }
        RaycastHit[] hits;
        hits = Physics.RaycastAll(mover.transform.position, movementVector, 1f);
        if (hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                
                RaycastHit hit = hits[i];
                colliderName = hit.collider.gameObject.name;
                UnityEngine.Debug.Log(colliderName);
                if (trails)
                {
                    DestroyOnCollision(hit.transform.gameObject,  mover);
                }
                if (movementVector == Vector3.down)
                {
                    SpawnTrail(mover, colliderName);
                }
                if(colliderName == "Enemy") DestroyOnCollision(hit.collider.gameObject, mover);
                if(colliderName == "Hunter") DestroyOnCollision(null, mover);
                if(colliderName == "Wall") DestroyOnCollision(hit.collider.gameObject, mover);
            }
        }
    }

    private void CollideWithWall(GameObject owner)
    {
        Destroy(owner);
    }

    private void DestroyOnCollision(GameObject target, GameObject owner)
    {
        

        //UnityEngine.Debug.Log($"Ōwner {owner} result {result.transform.gameObject} player {_playerGo}");
        if (owner == _playerGo || target.gameObject == _playerGo)
        {
            //UnityEngine.Debug.Log("Player destroyed. > GameOver");
            GameOver();
            Destroy(_playerGo);
        }
        //UnityEngine.Debug.Log($"Hunter collision: {owner} {target.gameObject}");
        if (owner.name == "Hunter")
        {
            return;
        }

        Instantiate(_particlePf, target.transform.position, Quaternion.identity);
        Instantiate(_particlePf, owner.transform.position, Quaternion.identity);
        if(target.gameObject.name != "Wall") Destroy(target.gameObject);
        if(owner.name != "Wall") Destroy(owner);

    }

    private void NotDetectedCollision()
    {
        GameOver();
        Destroy(_playerGo);
    }
    private void GameOver()
    {
        //show loose text
        //some random text/score ?
        //show restart button
        _restartBtn.transform.gameObject.SetActive(true);
        if (_playerScore > _enemyScore)
        {
            _winTxt.transform.gameObject.SetActive(true);
        }
        else
        {
            _loseTxt.transform.gameObject.SetActive(true);
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
        if (trailName == "Ground") return;
        Vector3 textOffset = new Vector3(0.5f,0.105f,-0.5f);
        
        var offset = new Vector3(trailOwner.transform.position.x-0.5f, 0.92f, trailOwner.transform.position.z+0.5f);
        var trail = Instantiate(_trailPf, offset, Quaternion.identity);
        trail.name = trailName;
        var trailInt = Int32.Parse(trailName);
        var text = Instantiate(_trailTextPf, trail.transform.position + textOffset, _trailTextPf.transform.rotation, trail.transform);
        //Change trail text color
        if (trailOwner.name == "Hunter")
        {
            trailName = "X";
            text.GetComponent<TMP_Text>().color = Color.black;
            text.GetComponent<TMP_Text>().text = trailName;
            return;
        }
        if (trailOwner.name == "Enemy")
        {
            text.GetComponent<TMP_Text>().color = _enemyColor;
            _enemyScoreTxt.text = $"Enemy Score: {_enemyScore.ToString()}";
            _enemyScore += 1;
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
            //UnityEngine.Debug.Log("You Win.");
            //TODO
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

    private void SpawnHunter()
    {
        GameObject hunter = Instantiate(_enemyPf, RandomV3(), _enemyPf.transform.rotation);
        hunter.name = "Hunter";
        _hunter = hunter;
        hunter.GetComponentInChildren<Renderer>().material.color = Color.red;
    }

    [SerializeField] private float _hunterCounter = 0;
    [SerializeField] private float _hunterMovePower = 2;
    private void HunterLogic()
    {
        float hunterMove = Random.Range(1, _hunterMovePower);
        _hunterMovePower += 0.1f;
        Vector3 playerLocation = _playerGo.transform.position;
        Vector3 hunterLocation = _hunter.transform.position;
        var diffX = Mathf.Abs(playerLocation.x - hunterLocation.x);
        var diffZ = Mathf.Abs(playerLocation.z - hunterLocation.z);
        var hunterX = hunterLocation.x;
        var hunterZ = hunterLocation.z;
        var playerX = playerLocation.x;
        var playerZ = playerLocation.z;
        //UnityEngine.Debug.Log($"Z: {diffZ} X: {diffX}");

        _hunterCounter += hunterMove;
        if (_hunterCounter > 4)
        {
            if (diffZ > diffX)
            {
                //UnityEngine.Debug.Log("diffZ");
                //Move on X axis
                if(playerZ > hunterZ) RollDirection(Vector3.forward, _hunter);
                if(playerZ < hunterZ) RollDirection(Vector3.back, _hunter);
            }
            else
            {
                if(playerX < hunterX) RollDirection(Vector3.left, _hunter);
                if(playerX > hunterX) RollDirection(Vector3.right, _hunter);
            }

            _hunterCounter = 0;
        }
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


    private void CheckForObstacles(GameObject enemy, Vector3 randomDirection)
    {
        //randomDirection = RandomVector3();
        var enemyPos = enemy.transform;
        bool wall = CheckForWall(enemyPos.position, randomDirection);
        bool path = CheckForTrails(enemyPos, randomDirection);

        for (int i = 0; i < _enemyChecksForObstacles; i++)
        {
            
            //check for wall
            if (wall)
            {

                //UnityEngine.Debug.Log($"Enemy {enemy.name} detected wall in {randomDirection} direction.");
                randomDirection = RandomVector3Direction();
                wall = CheckForWall(enemyPos.position, randomDirection);
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

    private bool CheckForTrails(Transform target, Vector3 dir)
    {
        bool detectPath = false;
        RaycastHit[] Results = new RaycastHit[4];
        int hits = Physics.RaycastNonAlloc(target.position + dir, Vector3.down, Results, 3f);
        //UnityEngine.Debug.DrawRay(start + dir, Vector3.down * 1.2f, Color.magenta, 100f);
        if (hits > 0)
        {
            for (int i = 0; i < hits; i++)
            {
                //UnityEngine.Debug.Log($"Wall check: {Results[i].transform.name}");
                if (Results[i].transform.name is "1" or "2" or "3" or "4" or "5" or "6")
                {
                    detectPath = true;
                    if(target.name == "Hunter") Destroy(Results[i].transform.gameObject);
                }
            }
        }
        return detectPath;
    }
    #endregion
}
