using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

public enum VehicleDirection // Basic Vehicle direction is Vertical
{
    Up = 0, Left = 1, Down = 2, Right = 3
}

public class Vehicle : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] private List<Seat> seatList = new();
    [SerializeField] private Transform dustPos;
    [Space(5f)]

    [SerializeField] private GameObject dustPrefab;
    [SerializeField] private GameObject arrowCanvas;
    [SerializeField] private GameObject seatsRenderer;
    [SerializeField] private GameObject vehicleRenderer;

    [Space(10f)]
    [SerializeField] private int vehicleSize; // Vehicle's length
    [SerializeField] private int vehicleIndex;
    [SerializeField] private int seatCount;
    [SerializeField] private float rotateVariation = 3f;
    [SerializeField] private float positionVariation = 0.1f;
    [SerializeField] private float vehicleSpeed = 3f;

    [Header("Runtime Settings")]
    public int poolIndex;
    public int targetPriority = -1;
    public Transform target;
    public VehicleDirection vehicleDirection;
    [SerializeField] private int colorIndex;
    public List<Customer> ridingCustomers;
    public bool isPicked = false;
    public bool isParked = false;
    public int customerCount = 0;

    public Vector2Int gridPos;

    private VehicleSpace vehicleSpace;
    private bool isMoving = false;
    private Quaternion initialRot;
    private float initialSpeed;
    private Vector3 initialPosition;

    public void Awake()
    {
        initialSpeed = vehicleSpeed;
    }

    public void OnDisable()
    {
        spawnDustToken.Cancel();
    }

    public void Initialize(VehicleSpace vehicleSpace, VehicleDirection direction, Vector2Int gridPos, int colorIndex, int poolIndex)
    {
        this.poolIndex = poolIndex;
        this.vehicleSpace = vehicleSpace;
        vehicleDirection = direction;
        this.colorIndex = colorIndex;
        this.gridPos = gridPos;
        customerCount = 0;
        targetPriority = -1;
        isPicked = false;
        isParked = false;
        isMoving = false;
        vehicleSpeed = initialSpeed;
        arrowCanvas.SetActive(true);
        vehicleRenderer.SetActive(true);
        seatsRenderer.SetActive(false);
        target = null;

        initialPosition = transform.position;
        vehicleRenderer.transform.localRotation = Quaternion.Euler(0, UnityEngine.Random.Range(-rotateVariation, rotateVariation), 0);
        transform.Translate(new Vector3(UnityEngine.Random.Range(-positionVariation, positionVariation), UnityEngine.Random.Range(-positionVariation, positionVariation), UnityEngine.Random.Range(-positionVariation, positionVariation)));
        initialRot = vehicleRenderer.transform.localRotation;
        ridingCustomers = new List<Customer>();

        ElasticBounce(UnityEngine.Random.Range(1.5f, 2.5f));
    }
    
    public int GetVehicleIndex()
    {
        return vehicleIndex;
    }

    public int GetVehicleSize()
    {
        return vehicleSize;
    }

    public int GetVehicleColorIndex()
    {
        return colorIndex;
    }

    public int GetSeatCount()
    {
        return seatCount;
    }


    public static Vector2Int GetVehicleDirection(VehicleDirection direction)
    {
        Vector2Int dir = new Vector2Int(0, 1);

        switch (direction)
        {
            case VehicleDirection.Up:
                dir.x = 0;
                dir.y = -1;
                break;
            case VehicleDirection.Down:
                dir.x = 0;
                dir.y = 1;
                break;
            case VehicleDirection.Left:
                dir.x = 1;
                dir.y = 0;
                break;
            case VehicleDirection.Right:
                dir.x = -1;
                dir.y = 0;
                break;
        }
        return dir;
    }

    public static int GetVehicleSizeByIndex(int vehicleIndex)
    {
        switch (vehicleIndex)
        {
            case 0:
                return 2;
            case 1:
                return 2;
            case 2:
                return 3;
        }
        return -1;
    }
    public static int GetCustomerCountByIndex(int vehicleIndex)
    {
        switch (vehicleIndex)
        {
            case 0:
                return 4;
            case 1:
                return 6;
            case 2:
                return 10;
        }
        return -1;
    }

    public void AbsolutePick()
    {
        if (!isPicked && GameManager.Instance.isGameRunning)
        {
            vehicleSpace.Park(this);
            isPicked = true;
            arrowCanvas.SetActive(false);
            seatsRenderer.SetActive(true);
            vehicleRenderer.transform.localRotation = Quaternion.identity;
            vehicleSpace.movingCarCount++;
            GoForward(1f);
        }
    }

    public void GoForward(float speedMultiplier)
    {
        TraceManager.Instance.canUndo = false;
        vehicleSpeed = initialSpeed * speedMultiplier;
        DOVirtual.Vector3(transform.position, transform.position - vehicleRenderer.transform.right * 50f, 50f / vehicleSpeed, value =>
        {
            if (target == null)
            {
                transform.position = value;
            }
        }).SetEase(Ease.Linear);
        SetParticleState(true);
    }

    Vector3 vehicleExistPos;
    public void Pick()
    {
        if (!isPicked && GameManager.Instance.isGameRunning)
        {
            TraceManager.Instance.canUndo = false;
            FrameRateManager.Instance.SetRenderingFullFps(true);
            if (!RoundManager.Instance.CanPopFromField(this, out vehicleExistPos))
            {
                if(vehicleExistPos == Vector3.zero)
                {
                    CannotMoveAnimation().Forget();
                } else
                {
                    CannotPopAnimation(vehicleExistPos - RoundManager.Instance.GetVehicleOffsetFromDirection(vehicleDirection, vehicleIndex) * 2f * (vehicleIndex == 2 ? 0.8f : 1.0f));
                }
                return;
            }
            vehicleSpace.Park(this);
            RoundManager.Instance.PopVehicle(this);
            SetWaitingState();
            vehicleSpace.movingCarCount++;
            RoundManager.Instance.IncreasePickCount();

            GoForward(1f);

            RoundManager.Instance.ShowAnswerAfterPick();
        }
    }

    public void SetWaitingState()
    {
        isPicked = true;
        arrowCanvas.SetActive(false);
        seatsRenderer.SetActive(true);
        vehicleRenderer.transform.localRotation = Quaternion.identity;
    }


    private void CannotPopAnimation(Vector3 hitPosition)
    {
        DOVirtual.Vector3(initialPosition, hitPosition, 0.1f, value =>
        {
            transform.position = value;
        }).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            DOVirtual.Vector3(hitPosition, initialPosition, 0.2f, value =>
            {
                transform.position = value;
            }).SetEase(Ease.Linear);
        });
    }

    private void CannotPopFeedbackAnimation(VehicleDirection hitVehicleDirection)
    {
        Vector2Int feedbackDirection = GetVehicleDirection(hitVehicleDirection);

    }

    private async UniTask CannotMoveAnimation()
    {
        for(int i = 0; i < 3; i++)
        {
            vehicleRenderer.transform.DOLocalRotateQuaternion(Quaternion.Euler(0, ((i % 2 == 0) ? -1 : 1) * UnityEngine.Random.Range(5f, 10f), 0), 0.15f).SetEase(Ease.OutCubic);
            await UniTask.Delay(TimeSpan.FromSeconds(0.15f));
        }
        vehicleRenderer.transform.DOLocalRotateQuaternion(initialRot, 0.5f).SetEase(Ease.OutCubic);
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
    }

    private DG.Tweening.Core.TweenerCore<Vector3, Vector3, DG.Tweening.Plugins.Options.VectorOptions> moveTween, scaleTween;
    private DG.Tweening.Core.TweenerCore<Quaternion, Vector3, DG.Tweening.Plugins.Options.QuaternionOptions> rotateTween;
    public void SetTarget(Transform target, int priority, TweenCallback callback = null)
    {
        if(targetPriority < priority)
        {
            TraceManager.Instance.canUndo = false;
            moveTween.Kill(false);
            rotateTween.Kill(false);

            this.target = target;
            targetPriority = priority;

            moveTween = transform.DOMove(target.position, Vector3.Distance(transform.position, target.position) / vehicleSpeed).SetEase(Ease.Linear).OnComplete(callback).OnUpdate(() =>
            {
                TraceManager.Instance.canUndo = false;
            });
            rotateTween = transform.DORotate((Quaternion.Euler(new Vector3(0, 90f, 0)) * Quaternion.LookRotation(target.position - transform.position)).eulerAngles, 0.1f).SetEase(Ease.OutCubic);
        }
    }

    public Seat[] GetSeatTransforms()
    {
        return seatList.ToArray();
    }

    public void Ride(Customer customer)
    {

        InputManager.Instance.InvokeVibrate(0);
        ridingCustomers.Add(customer);
        customerCount++;
        ElasticBounce(0.5f);

        if(customerCount == seatCount)
        {
            GameManager.Instance.GetRidingGold(seatCount, 40f, transform.position);
            vehicleSpace.PopVehicle(this);
        }
    }

    public void ElasticBounce(float duration)
    {
        vehicleRenderer.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        if (scaleTween != null) scaleTween.Kill(false);
        scaleTween = vehicleRenderer.transform.DOScale(Vector3.one, duration).SetEase(Ease.OutElastic).OnComplete(() =>
        {
            scaleTween = null;
        });
    }

    private UniTask spawnDustTask;
    public void SetParticleState(bool state)
    {
        if (state)
        {
            isMoving = true;
            spawnDustTask = SpawnDust(spawnDustToken.Token);
        } else
        {
            isMoving = false;
        }
    }

    private CancellationTokenSource spawnDustToken = new();
    GameObject particle;
    public async UniTask SpawnDust(CancellationToken token)
    {
        while (isMoving)
        {
            if (token.IsCancellationRequested) return;
            particle = PoolManager.Instance.Get(dustPrefab);
            particle.transform.position = dustPos.position;
            particle.GetComponent<ParticleBurster>().poolIdx = PoolManager.Instance.GetPoolIndex(dustPrefab);
            particle.GetComponent<ParticleBurster>().Play();
            await UniTask.Delay(TimeSpan.FromSeconds(0.05f));
        }
    }
}
