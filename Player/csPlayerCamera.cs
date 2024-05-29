using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Cysharp.Threading.Tasks;

enum PlayerCameraOrderType
{
    Ready,                  //준비.
    Starting                //시작
}

enum PlayerCameraDataType
{
    PlayerCamera,
}

public class csPlayerCamera : SubStream
{
    public float rotX;
    public float posY;

    Camera _cam;

    Transform _trPlayer;

    protected override void SetInit()
    {
        _cam = GetComponentInChildren<Camera>();

        _cam.transform.localEulerAngles = new Vector3(rotX, 0.0f, 0.0f);

        float _posZ = posY * (Mathf.Tan((rotX -90.0f) * Mathf.Deg2Rad));
        _cam.transform.localPosition = new Vector3(0.0f, posY, _posZ);
    }

    protected override void SetData()
    {
        SetData(PlayerCameraDataType.PlayerCamera, _cam);
    }

    protected override void SetOrder()
    {
         AddOrder(PlayerCameraOrderType.Ready, Ready);
         AddOrder(PlayerCameraOrderType.Starting, Starting);
    }

    void Ready()
    {
        transform.localPosition = Vector3.zero;

        _trPlayer = GetData<Transform>(PlayerMovementDataType.PlayerTransform);
    }

    void Starting()
    {
        SetObserver();
    }

    void SetObserver()
    {
        //PlayerMovementData _playerMovementData = GetData<PlayerMovementData>(PlayerMovementDataType.PlayerMovementData);

        this.LateUpdateAsObservable()
            .Subscribe(x => MovePosBase())
            .AddTo(this);

        /*
        this.UpdateAsObservable()
            .Where(x => (!Mathf.Approximately(Input.GetAxisRaw("Horizontal"), 0.0f) || !Mathf.Approximately(Input.GetAxisRaw("Vertical"), 0.0f)))
            .Subscribe(_ => MovePosBase(csManagerGame.instance.player.playerMovement.pos)
            .AddTo(this);

        this.UpdateAsObservable()
            .Where(x => (Mathf.Approximately(Input.GetAxisRaw("Horizontal"), 0.0f) && Mathf.Approximately(Input.GetAxisRaw("Vertical"), 0.0f)))
            .Subscribe(_ => MoveStop())
            .AddTo(this);
        
        this.UpdateAsObservable()
           .Where(_ => Vector3.Magnitude(Input.mousePosition) > 0.0f)
           .Subscribe(x => MoveRotate(Input.mousePosition))
           .AddTo(this);
        */
    }

    void MovePosBase()
    {
        transform.localPosition = _trPlayer.localPosition;
    }

    void MoveRotate(Vector3 _pos)
    {
        /*
        Ray _ray = _cam.ScreenPointToRay(_pos);

        int _layer = 1 << LayerMask.NameToLayer("Ground");
        if (Physics.Raycast(_ray, out RaycastHit _hit, 100.0f, _layer))
        {
            Vector3 _hitPoint = _hit.point;
            _hitPoint.y = csManagerGame.instance.player.transform.position.y;
            Vector3 _dir = _hitPoint - csManagerGame.instance.player.transform.position;
            //Quaternion _q = Quaternion.LookRotation(_dir);
            //transform.rotation = _q;

            //_dirCursor = _dir.normalized;
            //_disCorsor = Vector3.Distance(_hitPoint, _tr.position);
        }
        */
    }

    void MoveLimit()
    {
        float _size = 0.5f;
        float _limit = 40.0f;// - _size;       //최대 이동 범위.

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

        if (_pos.z > _limit)
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

    public void SetCameraMove()
    {

    }
}