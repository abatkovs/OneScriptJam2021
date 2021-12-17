
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class TheScript : MonoBehaviour
{
    [SerializeField] private bool _gameStarted = false; 
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

    [SerializeField] private TMP_Text _textPf;

    [SerializeField] private float _varRayDistance = 2.5f;
    private void Start()
    {
        _gameStarted = true;
        _playerStartPosition = _playerGo.transform.position;
        _playerStartRotation = _playerGo.transform.rotation;
        SpawnEnemies(4);
    }

    private void Update()
    {
        PlayerMovement();
        UpdateCameraPosition();
        Debug();
    }

    private void SpawnEnemies(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject enemy = Instantiate(_enemyPf, RandomV3(), _enemyPf.transform.rotation);
            enemy.name = "Enemy";
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

    private void PlayerMovement()
    {
        if (!_gameStarted) return;
        if (_playerGo == null) return;
        if (_isMoving || _isKnockedBack) return;

        if (Input.GetKey(KeyCode.A)) RollDirection(Vector3.left, _playerGo);
        else if (Input.GetKey(KeyCode.D)) RollDirection(Vector3.right, _playerGo);
        else if (Input.GetKey(KeyCode.W)) RollDirection(Vector3.forward, _playerGo);
        else if (Input.GetKey(KeyCode.S)) RollDirection(Vector3.back, _playerGo);

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

        if (Input.GetKeyDown(KeyCode.T))
        {
            StartCoroutine(KnockBack(Vector3.forward, _playerGo,6));
        }

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
        CheckMove(dir);
         
        _isMoving = true;
        for (var i = 0; i < 90 / _rollSpeed; i++) {
            go.transform.RotateAround(anchor, axis, _rollSpeed);
            yield return new WaitForSeconds(0.01f);
        }

        CheckMove(Vector3.down, _varRayDistance);
        _isMoving = false;

    }
    
    //[SerializeField] private float _reduceVectorBy = 2;
    private void CheckMove(Vector3 movementVector, float _reduceVectorBy = 2)
    {
        RaycastHit[] Results = new RaycastHit[4];
        string colliderName;
        string goName;
        int hits = Physics.RaycastNonAlloc(_playerGo.transform.position + (movementVector/_reduceVectorBy), movementVector, Results, 1f);
        UnityEngine.Debug.DrawRay(_playerGo.transform.position + (movementVector/_reduceVectorBy), movementVector/2, Color.red, 100f);
        //UnityEngine.Debug.Break();
        if (hits > 0)
        {
            for (int i = 0; i < hits; i++)
            {
                goName = Results[i].transform.name;
                colliderName = Results[i].collider.name;
                if (movementVector == Vector3.down)
                {
                    UnityEngine.Debug.Log($"Down: {colliderName}");
                    SpawnTrail(_playerGo, $"{colliderName}");
                }
                if(colliderName == "Enemy") GameOver(Results[i].transform);
                
                if(goName == "Player") continue;
                
                
                UnityEngine.Debug.Log($"Hit: {goName}");
                
                
            }
        }
    }

    private void GameOver(Transform result)
    {
        Instantiate(_particlePf, result.position, Quaternion.identity);
        Instantiate(_particlePf, _playerGo.transform.position, Quaternion.identity);
        Destroy(result.gameObject);
        Destroy(_playerGo);
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

    private void SpawnTrail(GameObject trailOwner, string name)
    {
        Vector3 textOffset = new Vector3(0.5f,0.105f,-0.5f);
        var offset = new Vector3(trailOwner.transform.position.x-0.5f, 0.92f, trailOwner.transform.position.z+0.5f);
        var trail = Instantiate(_trailPf, offset, Quaternion.identity);
        trail.name = name;
        var text = Instantiate(_textPf, trail.transform.position + textOffset, _textPf.transform.rotation, trail.transform);
        text.GetComponent<TMP_Text>().text = name;
    }
}
