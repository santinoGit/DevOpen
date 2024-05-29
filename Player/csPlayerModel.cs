using RootMotion.FinalIK;
using UnityEngine;

enum PlayerModelOrderType
{
    Ready,
    SetIK,

    DodgeStart,
    DodgeComplete
}

enum PlayerModelDataType
{
    PlayerAnimator,
}

public class csPlayerModel : SubStream
{
    //추후에 변경해야함.
    [SerializeField]
    GameObject playerModelPrefab;

    ArmIK _armIKRight;
    ArmIK _armIKLeft;
    AimIK _aimIK;

    protected override void SetInit()
    {
        ArmIK[] _armIKs = GetNameToComps<ArmIK>("IKBase");
        _armIKRight = _armIKs[0];
        _armIKLeft = _armIKs[1];

        _aimIK = GetNameToComp<AimIK>("IKBase");
    }

    protected override void SetOrder()
    {
        AddOrder(PlayerModelOrderType.Ready, Ready);
        AddOrder(PlayerModelOrderType.SetIK, SetIK);

        AddOrder(PlayerModelOrderType.DodgeStart, DodgeStart);
        AddOrder(PlayerModelOrderType.DodgeComplete, DodgeComplete);
    }

    void Ready()
    {
        //캐릭터 바디 생성.
        Transform _trIKBase = GetNameToTr("IKBase");
        GameObject _obj = InstantiateSub(playerModelPrefab, new Vector3(0.0f, -1.0f, 0.0f), Quaternion.identity, _trIKBase);
        _obj.name = playerModelPrefab.name;

        Transform _chestR = GetNameToTr("Spine_03");
        Transform _shoulderR = GetNameToTr("Clavicle_R");
        Transform _upperArmR = GetNameToTr("Shoulder_R");
        Transform _foreArmR = GetNameToTr("Elbow_R");
        Transform _handR = GetNameToTr("Hand_R");
        _armIKRight.solver.SetChain(_chestR, _shoulderR, _upperArmR, _foreArmR, _handR, transform);

        _armIKRight.solver.isLeft = false;

        Transform _chestL = GetNameToTr("Spine_03");
        Transform _shoulderL = GetNameToTr("Clavicle_L");
        Transform _upperArmL = GetNameToTr("Shoulder_L");
        Transform _foreArmL = GetNameToTr("Elbow_L");
        Transform _handL = GetNameToTr("Hand_L");
        _armIKLeft.solver.SetChain(_chestL, _shoulderL, _upperArmL, _foreArmL, _handL, transform);

        _armIKLeft.solver.isLeft = true;

        IKSolver.Bone [] _bones = new IKSolver.Bone[4];
        _bones[0] = new IKSolver.Bone(GetNameToTr("Spine_01"), 1.0f);
        _bones[1] = new IKSolver.Bone(GetNameToTr("Spine_02"), 1.0f);
        _bones[2] = new IKSolver.Bone(GetNameToTr("Spine_03"), 1.0f);
        _bones[3] = new IKSolver.Bone(GetNameToTr("Neck"), 1.0f);

        _aimIK.solver.bones = _bones;

        SetData(PlayerModelDataType.PlayerAnimator, _obj.GetComponent<Animator>());
    }

    void SetIK()
    {
        WeaponTransformData _weaponTransformData = GetData<WeaponTransformData>(WeaponModelDataType.WeaponTransform);

        _armIKRight.solver.arm.target = _weaponTransformData.TrHandRight;
        _armIKLeft.solver.arm.target = _weaponTransformData.TrHandLeft;

        _aimIK.solver.transform = _weaponTransformData.TrMuzzle;

        _aimIK.solver.target = GetData<Transform>(PlayerTargetDataType.PlayerTargetTransform);
    }

    void DodgeStart()
    {
        _armIKLeft.enabled = false;
        _aimIK.enabled = false;
    }

    void DodgeComplete()
    {
        _armIKLeft.enabled = true;
        _aimIK.enabled = true;

        _armIKLeft.solver.IKPositionWeight = 0.0f;
        DG.Tweening.DOVirtual.Float(0.0f, 1.0f, 0.2f, (value) => { _armIKLeft.solver.IKPositionWeight = value; });
    }
}
