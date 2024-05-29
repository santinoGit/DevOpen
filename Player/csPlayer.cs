using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Cysharp.Threading.Tasks;

enum PlayerOrderType
{
    Ready,               //움직임 데이터 리셋.
}

enum PlayerDataType
{
    PlayerData,
}

class PlayerData
{
    int _hp;
    int HP { get { return _hp; } }

    public void SetHP(int hp)
    {
        _hp = hp;
    }

    public void AddHP(int hp)
    {
        _hp = hp;
    }

    public void Reset()
    {
        SetHP(100);
    }

    public PlayerData()
    {
        Reset();
    }
}

public class csPlayer : SubStream
{
    PlayerData _PlayerData;

    protected override void SetInit()
    {

    }

    protected override void SetData()
    {
        _PlayerData = new PlayerData();

        SetData(PlayerDataType.PlayerData, _PlayerData);
    }

    protected override void SetOrder()
    {
        _PlayerData.Reset();

        AddOrder(PlayerOrderType.Ready, Ready);
    }

    void Ready()
    {
        //캐릭터 생성.
        ActiveOrder(PlayerModelOrderType.Ready);
        //무기 생성.
        ActiveOrder(PlayerWeaponOrderType.Ready);

        //준비.
        ActiveOrder(WeaponModelOrderType.Ready);
        ActiveOrder(PlayerTargetOrderType.Ready);
        ActiveOrder(PlayerCameraOrderType.Ready);
        ActiveOrder(PlayerMovementOrderType.Ready);

        //준비가 되면 IK 세팅.
        ActiveOrder(PlayerModelOrderType.SetIK);
        ActiveOrder(PlayerTargetOrderType.SetAimPosition);

        //인풋세팅.
        ActiveOrder(PlayerCameraOrderType.Starting);
        ActiveOrder(PlayerWeaponOrderType.Starting);
        ActiveOrder(PlayerTargetOrderType.Starting);
        ActiveOrder(PlayerMovementOrderType.Starting);

        ActiveOrder(ManagerUIOrderType.AllCloseUI);

        ActiveOrder(UIHUDOrderType.Show);

        ActiveOrder(EnemyGenOrderType.Starting);

#if UNITY_EDITOR
        SetObserver();
#endif
    }

#if UNITY_EDITOR
    void SetObserver()
    {
        this.UpdateAsObservable()
           .Where(_ => Input.GetKeyDown(KeyCode.Keypad1))
           .Subscribe(x => ChangeDodge())
           .AddTo(this);

        this.UpdateAsObservable()
           .Where(_ => Input.GetKeyDown(KeyCode.Keypad4))
           .Subscribe(x => ChangeBulletSpeed())
           .AddTo(this);
    }

    void ChangeDodge()
    {
        PlayerMovementData _playerMovementData = GetData< PlayerMovementData>(PlayerMovementDataType.PlayerMovementData);

        _playerMovementData.SetDodgeType(PlayerDodgeType.Jump);
    }

    void ChangeBulletSpeed()
    {
        BulletData _playerBulletData = GetData<BulletData>(PlayerWeaponDataType.PlayerBulletData);

        _playerBulletData.SetSpeed(100.0f);
    }
#endif
}
