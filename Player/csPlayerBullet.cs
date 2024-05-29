using UnityEngine;
using Cysharp.Threading.Tasks;

public class csPlayerBullet : csBullet
{
    TrailRenderer _trail;

    protected override void SetInit()
    {
        _trail = GetComponentInChildren<TrailRenderer>();
    }

    protected override void SetData() { }

    protected override void SetOrder() { }

    protected override void SetDestroy() 
    {
        if (_taskDestroy != null) { _taskDestroy.Cancel(); }
    }

    //생성할 때 입력 받는다.
    public override void SetActive(Vector3 pos, Vector3 dir, BulletData bulletData, float disFirst = 0.0f)
    {
        //속도에 따른 알파값.
        float _a = (bulletData.Speed * 0.005f) - 0.125f;

        _trail.time = Mathf.Lerp(0.4f, 0.2f, _a);

        AnimationCurve _curveWidth = new AnimationCurve();
        _curveWidth.AddKey(0.0f, 0.05f);
        _curveWidth.AddKey(1.0f, 0.0f);
        //_curveWidth.AddKey(Mathf.Lerp(0.7f, 0.9f, _a), 0.0f);

        _trail.widthCurve = _curveWidth;
        _trail.widthMultiplier = 1.0f;

        transform.position = pos;
        Quaternion _q = Quaternion.LookRotation(dir);
        transform.rotation = _q;

        _bulletData = bulletData;

        CheckMoveFirst(disFirst);

        _isMove = true;
    }

    //초탄 동작.
    //총알은 플레이어 가슴쯤에서 발사된다.
    //총구에서 발사하면 근접한 적은 공격이 안되기 때문에 근접체크를 해줘야 한다.
    void CheckMoveFirst(float disFirst)
    {
        Vector3 _dir = transform.TransformDirection(Vector3.forward);
        float _dis = disFirst;

        CheckHit(_dir, _dis);

        CheckDestroy(_dir, _dis);

        transform.position += (_dir * _dis);
    }

    private void Update()
    {
        if(_isMove == true)
        {
            Vector3 _dir = transform.TransformDirection(Vector3.forward);
            float _dis = _bulletData.Speed * Time.deltaTime;

            CheckHit(_dir, _dis);

            CheckDestroy(_dir, _dis);

            transform.position += (_dir * _dis);
        }
    }

    void CheckHit(Vector3 dir, float dis)
    {
        if (_bulletData.IsActive == true)
        {
            int _layer = (LayerMask.GetMask("Enemy") | LayerMask.GetMask("EnemyShield"));
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, dis, _layer) == true)
            {
                switch (hit.collider.tag)
                {
                    case "Enemy":
                        _bulletData.ActivePiercing();

                        ActiveHit(hit);

                        //관통효과가 끝났다.
                        if (_bulletData.Piercing < 0)
                        {
                            _taskDestroy = new RunTask(TaskDestroy(hit));
                        }
                        break;
                    case "EnemyShield":
                        if (Mathf.Approximately(_bulletData.PiercingShield, 0.0f) == true)
                        {
                            ActiveHit(hit);
                        }
                        else
                        {

                        }
                        break;
                }
            }
        }
    }

    void CheckDestroy(Vector3 dir, float dis)
    {
        int _layer = LayerMask.GetMask("WallIn");
        if (Physics.Raycast(transform.position, dir, out RaycastHit hit_0, dis, _layer) == true)
        {
            _bulletData.SetDeActive();
        }

        _layer = LayerMask.GetMask("WallOut");
        if (Physics.Raycast(transform.position, dir, out RaycastHit hit_1, dis, _layer) == true)
        {
            InstanceDestroy();
        }
    }

    void ActiveHit(RaycastHit hit)
    {
        csEnemy _hitCol = hit.collider.GetComponent<csEnemy>();
        _hitCol.SetHit(new BulletData(_bulletData), new BulletHitData(hit.point, hit.normal));
    }

    protected override async UniTask TaskDestroy(RaycastHit hit)
    {
        _isMove = false;

        transform.position = hit.point;

        _trail.time = 0.05f;

        await UniTask.Delay(1500);

        InstanceDestroy();
    }
}