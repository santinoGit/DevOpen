using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Cysharp.Threading.Tasks;

enum PlayerMovementOrderType
{
    Ready,              //움직임 데이터 리셋.
    Starting            //시작.
}

enum PlayerMovementDataType
{
    PlayerTransform,
    PlayerMovementData
}

enum PlayerDodgeType
{
    Rolling, Jump, Dash
}

class PlayerMovementData
{
    float _speedMove;
    public float SpeedMoveForward { get { return _speedMove; } }

    float _speedMoveBackwardAdd;
    public float SpeedMoveBackward { get { return _speedMove * _speedMoveBackwardAdd; } }

    bool _isDodge;
    public bool IsDodge { get { return _isDodge; } }

    PlayerDodgeType _dodgeType;
    public PlayerDodgeType DodgeType { get { return _dodgeType; } }

    public void SetSpeedMove(float speedMove)
    {
        _speedMove = speedMove;
    }

    public void SetSpeedMoveBackwardAdd(float speedMoveBackwardAdd)
    {
        _speedMoveBackwardAdd = speedMoveBackwardAdd;
    }

    public void SetDodge(bool isDodge)
    {
        _isDodge = isDodge;
    }
    public void SetDodgeType(PlayerDodgeType dodgeType)
    {
        _dodgeType = dodgeType;
    }

    public void Reset()
    {
        SetSpeedMove(3.0f);
        SetSpeedMoveBackwardAdd(0.7f);
        SetDodge(false);
        SetDodgeType(PlayerDodgeType.Rolling);
    }

    public PlayerMovementData()
    {
        Reset();
    }
}

public class csPlayerMovement : SubStream
{
    PlayerMovementData _playerMovementData;

    Rigidbody _rb;

    Animator _animCharacter;

    Transform _trTarget;

    Vector3 _dirMove;

    bool _isForward;

    public AnimationCurve _animJumpCurve;

    protected override void SetInit()
    {
        _rb = GetComponent<Rigidbody>();
    }

    protected override void SetData()
    {
        _playerMovementData = new PlayerMovementData();

        SetData(PlayerMovementDataType.PlayerTransform, this.transform); 
        SetData(PlayerMovementDataType.PlayerMovementData, _playerMovementData);
    }

    protected override void SetOrder()
    {
        AddOrder(PlayerMovementOrderType.Ready, Ready);
        AddOrder(PlayerMovementOrderType.Starting, Starting);
    }

    void Ready()
    {
        _playerMovementData.Reset();

        _animCharacter = GetData<Animator>(PlayerModelDataType.PlayerAnimator);

        _trTarget = GetData<Transform>(PlayerTargetDataType.PlayerTargetTransform);

        _dirMove = Vector3.forward;
        _isForward = true;
    }

    void Starting()
    {
        SetObserver();
    }

    void SetObserver()
    {
        this.UpdateAsObservable()
            .Where(_ => (!Mathf.Approximately(Input.GetAxisRaw("Horizontal"), 0.0f) || !Mathf.Approximately(Input.GetAxisRaw("Vertical"), 0.0f)))
            .Subscribe(x => SetMoveDirection(new Vector3(Input.GetAxisRaw("Horizontal"), 0.0f, Input.GetAxisRaw("Vertical"))))
            .AddTo(this);

        this.UpdateAsObservable()
            .Where(_ => (Mathf.Approximately(Input.GetAxisRaw("Horizontal"), 0.0f) && Mathf.Approximately(Input.GetAxisRaw("Vertical"), 0.0f)))
            .Subscribe(x => SetMoveStop())
            .AddTo(this);

        this.UpdateAsObservable()
           .Where(_ => Input.GetButtonDown("Jump"))
           .Subscribe(x => StartDodge())
           .AddTo(this);

        this.UpdateAsObservable()
            .Subscribe(x => SetMoveLimit())
            .AddTo(this);

        this.LateUpdateAsObservable()
            .Subscribe(x => SetFront())
            .AddTo(this);

        this.LateUpdateAsObservable()
            .Subscribe(x => SetRotate())
            .AddTo(this);

        this.LateUpdateAsObservable()
            .Subscribe(x => SetAnimatioin())
            .AddTo(this);
    }

    void SetMoveDirection(Vector3 dir)
    {
        _animCharacter.SetBool("IsMoving", true);

        if (_playerMovementData.IsDodge == true)
            return;

        _dirMove = dir.normalized;

        if (_isForward == true)
            _rb.velocity = dir.normalized * _playerMovementData.SpeedMoveForward;
        else
            _rb.velocity = dir.normalized * _playerMovementData.SpeedMoveBackward;
    }

    void SetMoveStop()
    {
        _animCharacter.SetBool("IsMoving", false);

        if (_playerMovementData.IsDodge == true)
            return;

        _rb.velocity = Vector3.zero;
    }

    //사방 공간 제한.
    void SetMoveLimit()
    {
        // float _size = 0.5f;
        float _limit = 30.0f;// - _size;       //최대 이동 범위.

        Vector3 _posOri = transform.position;
        Vector3 _pos = transform.position;
        if (_pos.x >= _limit)
        {
            _pos.x = _limit;
        }
        else if (_pos.x < -_limit)
        {
            _pos.x = -_limit;
        }

        if (_pos.z >= _limit)
        {
            _pos.z = _limit;
        }
        else if (_pos.z < -_limit)
        {
            _pos.z = -_limit;
        }

        if (Vector3.Distance(_pos, _posOri) > 0.01f)
            transform.position = _pos;
    }

    void SetFront()
    {
        if (_playerMovementData.IsDodge == true)
        {
            _isForward = true;
        }
        else
        {
            Vector3 _dirTarget = new Vector3((_trTarget.position.x - transform.position.x), 0.0f, (_trTarget.position.z - transform.position.z)).normalized;

            float _dot = Vector3.Dot(_dirMove, _dirTarget);
            float _angle = Mathf.Acos(_dot) * Mathf.Rad2Deg;

            if (_angle <= 90.0f)
                _isForward = true;
            else
                _isForward = false;
        }
    }

    void SetRotate()
    {
        Vector3 _dirPlayerView = _dirMove;

        if (_isForward == false)
            _dirPlayerView *= -1;

        transform.rotation = Quaternion.LookRotation(_dirPlayerView, Vector3.up);

    }

    async void StartDodge()
    {
        if (_playerMovementData.IsDodge == true)
            return;

        _playerMovementData.SetDodge(true);

        ActiveOrder(PlayerModelOrderType.DodgeStart);
        ActiveOrder(PlayerWeaponOrderType.StopWeapon);

        float _timeStop = 0.0f;
        Vector3 _speedAdd = _dirMove * _playerMovementData.SpeedMoveForward;
        switch (_playerMovementData.DodgeType)
        {
            case PlayerDodgeType.Rolling:
                _animCharacter.SetTrigger("Rolling");
                _speedAdd *=  2.0f;
                _timeStop = 0.8f;
                break;
            case PlayerDodgeType.Jump:
                _animCharacter.SetTrigger("Jumping");
                _speedAdd *= 2.5f;
                _timeStop = 0.6f;
                gameObject.layer = LayerMask.NameToLayer("Invisible");
                //점프는 콜라이더를 끈다.
                break;
            case PlayerDodgeType.Dash:

                //대쉬는 더 멀고 힘있게 이동됨.
                break;
        }

        float _time = 0.0f;
        while (true)
        {
            _rb.velocity = _speedAdd;

            //동시에 높이조절을 해야함.
            switch (_playerMovementData.DodgeType)
            {
                case PlayerDodgeType.Jump:
                    float _timeToPos = _animJumpCurve.Evaluate(_time * (1.0f / _timeStop)) * 2.0f;
                    transform.position = new Vector3(transform.position.x, _timeToPos, transform.position.z);
                    break;
            }

            await UniTask.NextFrame();

            _time += Time.deltaTime;
            if (_time > _timeStop)
                break;
        }
        _rb.velocity = Vector3.zero;

        transform.position = new Vector3(transform.position.x, 0.0f, transform.position.z);

        gameObject.layer = LayerMask.NameToLayer("Player");

        await UniTask.WaitForSeconds(1.0f - _timeStop);

        _playerMovementData.SetDodge(false);

        ActiveOrder(PlayerModelOrderType.DodgeComplete);
        ActiveOrder(PlayerWeaponOrderType.ReadyWeapon);
    }

    void SetAnimatioin()
    {
        float _speedMove = _rb.velocity.magnitude;
        if (_isForward == false)
            _speedMove *= -0.7f;

        if (_playerMovementData.IsDodge == true)
            _speedMove = 0.0f;

        _animCharacter.SetFloat("Speed", _speedMove);
    }
}
