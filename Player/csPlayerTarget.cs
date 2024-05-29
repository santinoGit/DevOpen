using UnityEngine;
using UniRx;
using UniRx.Triggers;

enum PlayerTargetOrderType
{
    Ready,              //움직임 데이터 리셋.
    Starting,            //시작.
    SetAimPosition,
}

enum PlayerTargetDataType
{
    PlayerTargetTransform,
}

public class csPlayerTarget : SubStream
{
    Camera _cam;

    Transform _trPlayer;

    Transform _trAimCheck;
    Transform _trTarget;

    protected override void SetInit()
    {
        _trAimCheck = GetNameToTr("AimCheck");
        _trTarget = GetNameToTr("Target");
    }

    protected override void SetData()
    {
        SetData(PlayerTargetDataType.PlayerTargetTransform, _trTarget);
    }

    protected override void SetOrder()
    {
        AddOrder(PlayerTargetOrderType.Ready, Ready);
        AddOrder(PlayerTargetOrderType.Starting, Starting);
        AddOrder(PlayerTargetOrderType.SetAimPosition, SetAimCheckPosition);
    }

    void Ready()
    {
        _cam = GetData<Camera>(PlayerCameraDataType.PlayerCamera);

        _trPlayer = GetData<Transform>(PlayerMovementDataType.PlayerTransform);

        _trTarget.position = _trTarget.position +_trPlayer.TransformDirection(Vector3.forward) * 2.0f;
    }

    void Starting()
    {
        SetObserver();
    }

    void SetObserver()
    {
        this.FixedUpdateAsObservable()
           .Subscribe(x => MoveTarget(Input.mousePosition))
           .AddTo(this);
    }

    void SetAimCheckPosition()
    {
        WeaponTransformData _weaponTransformData = GetData<WeaponTransformData>(WeaponModelDataType.WeaponTransform);

        Vector3 _posMuzzle = _weaponTransformData.TrMuzzle.position;
        _posMuzzle.x = 0.0f;
        _posMuzzle.z = 0.0f;
        _trAimCheck.position = _posMuzzle;
    }

    void MoveTarget(Vector3 pos)
    {
        if (_cam == null)
            return;

        Ray _ray = _cam.ScreenPointToRay(pos);
        int _layer = 1 << LayerMask.NameToLayer("Aiming");
        if (Physics.Raycast(_ray, out RaycastHit _hit, 100.0f, _layer))
        {
            //타겟 트랜스폼을 플레이어 2m 이내로 못 들어오게 한다.
            Vector3 _posHit = _hit.point;
            Vector3 _posPlayer = _trPlayer.position;

            _posHit.y = 0;
            _posPlayer.y = 0;

            float _dis = Vector3.Distance(new Vector3(_hit.point.x, 0.0f, _hit.point.z), new Vector3(_trPlayer.position.x, 0.0f, _trPlayer.position.z));

            if(_dis < 2.0f)
            {
                Vector3 _dir = (_posHit - _posPlayer).normalized;

                Vector3 _posTarget = _posPlayer + _dir * 2.0f;
                _posTarget.y = _hit.point.y;

                _trTarget.position = _posTarget;
            }
            else
            {
                _trTarget.position = _hit.point;
            }
        }
    }
}