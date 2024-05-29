using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Cysharp.Threading.Tasks;

enum PlayerWeaponOrderType
{
    Ready,      //무기 데이터 초기화.
    Starting,    //시작.

    ReadyWeapon,
    StopWeapon,
}

enum PlayerWeaponDataType
{
    PlayerWeaponData,
    PlayerBulletData
}

class PlayerWeaponData
{
    int _bulletCount;
    public int BulletCount { get { return _bulletCount; } }

    int _bulletUse;
    public int BulletUse { get { return _bulletUse; } }

    int _magazineCount;
    public int MagazineCount { get { return _magazineCount; } }

    float _rateOfReloade;
    public float RateOfReloade { get { return _rateOfReloade; } }

    float _rateOfFire;
    public float RateOfFire { get { return _rateOfFire; } }

    public bool IsBulletCheck
    {
        get
        {
            if (_bulletCount == 0)
                return false;
            if (_bulletCount < _bulletUse)
                return false;

            return true;
        }
    }

    public void UseBulletCount()
    {
        _bulletCount -= _bulletUse;
    }
    public void SetUseBulletCount(int bulletUse)
    {
        _bulletUse = bulletUse;
    }
    public void ResetBulletCount()
    {
        _bulletCount = _magazineCount;
    }

    public void SetMagazineCount(int magazineCount)
    {
        _magazineCount = magazineCount;
        ResetBulletCount();
    }

    public void SetRateOfReloade(float rateOfReloade)
    {
        _rateOfReloade = rateOfReloade;
    }

    public void SetRateOfFire(float rateOfFire)
    {
        _rateOfFire = rateOfFire;
    }

    public void Reset()
    {
        SetMagazineCount(10);
        SetUseBulletCount(1);
        ResetBulletCount();
        SetRateOfReloade(2.0f);
        SetRateOfFire(0.1f);
    }

    public PlayerWeaponData()
    {
        Reset();
    }
}

public class csPlayerWeapon : SubStream
{
    //추후에 뱅크에서 꺼내오는 걸로 변경해야함.
    [SerializeField]
    GameObject _bulletPrefab;

    [SerializeField]
    GameObject weaponModelPrefab;

    [SerializeField]
    Vector3 _rotWeapon;

    csWeaponModel _weaponModel;

    PlayerWeaponData _playerWeaponData;

    BulletData _playerBulletData;

    float _rateOfFireReal = 0.0f;

    bool _isFire = false;
    bool _isFireStop = false;
    bool _isReload = false;

    Transform _trTarget;

    WeaponTransformData _weaponTransformData;

    protected override void SetInit()
    {
   
    }

    protected override void SetData()
    {
        _playerWeaponData = new PlayerWeaponData();
        _playerBulletData = new BulletData();

        SetData(PlayerWeaponDataType.PlayerWeaponData, _playerWeaponData);
        SetData(PlayerWeaponDataType.PlayerBulletData, _playerBulletData);
    }

    protected override void SetOrder()
    {
        AddOrder(PlayerWeaponOrderType.Ready, Ready);
        AddOrder(PlayerWeaponOrderType.Starting, Starting);

        AddOrder(PlayerWeaponOrderType.ReadyWeapon, ReadyWeapon);
        AddOrder(PlayerWeaponOrderType.StopWeapon, StopWeapon);
    }

    void Ready()
    {
        _playerWeaponData.Reset();
        _playerBulletData.Reset();

        Transform _trSpine = GetNameToTr("Spine_03");
        GameObject _objWeapon = new GameObject("Weapon");
        _objWeapon.transform.SetParent(_trSpine);
        _objWeapon.transform.localPosition = Vector3.zero;
        _objWeapon.transform.localEulerAngles = _rotWeapon;

        //무기생성.
        _weaponModel = InstantiateSub(weaponModelPrefab, Vector3.zero, Quaternion.identity, _objWeapon.transform).GetComponent<csWeaponModel>();
        _weaponModel.name = weaponModelPrefab.name;
    }

    void Starting()
    {
        SetObserver();

        _trTarget = GetData<Transform>(PlayerTargetDataType.PlayerTargetTransform);

        _weaponTransformData = GetData<WeaponTransformData>(WeaponModelDataType.WeaponTransform);
    }

    void SetObserver()
    {
        this.UpdateAsObservable()
            .Where(_ => Input.GetButtonDown("Fire1"))
            .Subscribe(x => SetWeaponFire(true))
            .AddTo(this);

        this.UpdateAsObservable()
             .Where(_ => Input.GetButtonUp("Fire1"))
             .Subscribe(x => SetWeaponFire(false))
             .AddTo(this);

        this.UpdateAsObservable()
            .Subscribe(x => UpdateWeaponFire())
            .AddTo(this);
    }

    void SetWeaponFire(bool fire)
    {
        _isFire = fire;
    }

    void UpdateWeaponFire()
    {
        if (_isFire == true && _isFireStop == false && _isReload == false)
        {
            if (_playerWeaponData.IsBulletCheck)
            {
                _rateOfFireReal -= Time.deltaTime;
                if (_rateOfFireReal <= 0.0f)
                {
                    _rateOfFireReal += _playerWeaponData.RateOfFire;

                    if (_bulletPrefab != null)
                    {
                        //뭔가 기능으로 총알이 1개 이상 감소하거나 감소 안 할 수도 있다.
                        _playerWeaponData.UseBulletCount();

                        //이펙트 생성.

                        //총알생성.
                        csBullet _bullet = InstantiateSub(_bulletPrefab).GetComponent<csBullet>();

                        //방향(조준한 위치로 발사됨).
                        //Transform _trTarget = GetData<Transform>(PlayerTargetDataType.PlayerTargetTransform);
                        Vector3 _dir = (_trTarget.position - _weaponTransformData.TrMuzzle.position).normalized;

                        //최초 발사 거리. (적이 가까울 경우, 플레이어 몸통에서 머즐까지의 거리)
                        float _disFirst = Vector3.Distance(_weaponTransformData.TrMuzzleReal.position, _weaponTransformData.TrMuzzle.position);

                        _bullet.SetActive(_weaponTransformData.TrMuzzleReal.position, _dir, new BulletData(_playerBulletData), _disFirst);
                    }
                }
            }
            else
            {
                //재장전.
                ResetBulletCount();
            }
        }
        else
        {
            if (_rateOfFireReal > 0.0f)
                _rateOfFireReal -= Time.deltaTime;
        }
    }

    async void ResetBulletCount()
    {
        _isReload = true;

        await UniTask.WaitForSeconds(_playerWeaponData.RateOfReloade);

        _playerWeaponData.ResetBulletCount();

        _isReload = false;
    }

    void ReadyWeapon()
    {
        _isFireStop = false;
    }

    void StopWeapon()
    {
        _isFireStop = true;
    }
}