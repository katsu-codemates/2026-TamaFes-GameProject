using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class CameraMover : MonoBehaviour
{
    // WASD：前後左右の移動
    // QE：上昇・降下
    // 右ドラッグ：カメラの回転
    // 左ドラッグ：前後左右の移動
    // スペース：カメラ操作の有効・無効の切り替え
    // P：位置・回転をデフォルト状態にリセットする

	//カメラの移動量
	[SerializeField, Range(0.1f, 10.0f)]
	private float _positionStep = 2.0f;

    //マウス感度
    [SerializeField, Range(30.0f, 150.0f)]
    private float _mouseSensitive = 90.0f;

	[Header("左右カメラ操作反転")]
	[SerializeField]
	private bool _invertHorizontalRotationControl = false;

	[Header("上下カメラ操作反転")]
	[SerializeField]
	private bool _invertVerticalRotationControl = false;

    [Header("オプション画面から変更する感度倍率(PlayerPrefsに保存される)")]
    //マウス操作（回転・ドラッグ移動・ホイールズーム）の感度倍率
    [SerializeField]
    private float _mouseSensitivityMultiplier = 1.0f;
    //キー操作（WASD・QE）の移動速度倍率
    [SerializeField]
    private float _keySensitivityMultiplier = 1.0f;
    //感度倍率の下限・上限
    [SerializeField]
    private float _minSensitivityMultiplier = 0.2f;
    [SerializeField]
    private float _maxSensitivityMultiplier = 3.0f;

    [Header("可動範囲の制限")]
    //可動範囲の基準にする床
    [SerializeField]
    private Transform _floorReference;
    //床の範囲に対してどれだけ余裕を持たせるか
    [SerializeField, Range(1.0f, 3.0f)]
    private float _boundsMarginMultiplier = 1.3f;
    //床面からの最低高度
    [SerializeField, Range(0f, 10f)]
    private float _minHeightAboveFloor = 1.0f;
    //床面からの最高高度
    [SerializeField, Range(5f, 100f)]
    private float _maxHeightAboveFloor = 40.0f;
    //見下ろし角度の下限
    [SerializeField, Range(-89f, 0f)]
    private float _minPitchAngle = -80f;
    //見上げ角度の上限
    [SerializeField, Range(0f, 89f)]
    private float _maxPitchAngle = 80f;

    //カメラ操作の有効無効
	private bool _cameraMoveActive = true;
    //カメラのtransform
    private Transform _camTransform;
    //マウスの始点
    private Vector3 _startMousePos;
    //カメラ回転の始点情報
    private Vector3 _presentCamRotation;
    private Vector3 _presentCamPos;
    //ドラッグが有効な状態で開始されたか（無効中に押されたボタンを、有効化後にドラッグとして扱わないため）
    private bool _isRotateDragging;
    private bool _isSlideDragging;
    //最後に有効化されたフレーム（そのフレームのクリックはフォーカス解除などに使われたものなので無視する）
    private int _enabledFrame = -1;
    //初期状態 Rotation
    private Quaternion _initialCamRotation;
    //初期状態 Position
    private Vector3 _initialCamPosition;
    //UIメッセージの表示
    private bool _uiMessageActiv;
    //可動範囲（床の大きさから算出）
    private float _minX, _maxX, _minY, _maxY, _minZ, _maxZ;

    //カメラ設定を保存するPlayerPrefsのキー
    private const string MouseSensitivityKey = "CameraMouseSensitivity";
    private const string KeySensitivityKey = "CameraKeySensitivity";
    private const string InvertHorizontalKey = "CameraInvertHorizontal";
    private const string InvertVerticalKey = "CameraInvertVertical";

    //Inspectorで設定した初期値（キャッシュクリア時にここへ戻す）
    private float _defaultMouseSensitivityMultiplier;
    private float _defaultKeySensitivityMultiplier;
    private bool _defaultInvertHorizontal;
    private bool _defaultInvertVertical;

    public float MouseSensitivityMultiplier => _mouseSensitivityMultiplier;
    public float KeySensitivityMultiplier => _keySensitivityMultiplier;
    public float MinSensitivityMultiplier => _minSensitivityMultiplier;
    public float MaxSensitivityMultiplier => _maxSensitivityMultiplier;
    public bool InvertHorizontal => _invertHorizontalRotationControl;
    public bool InvertVertical => _invertVerticalRotationControl;

    void Awake()
    {
        _defaultMouseSensitivityMultiplier = _mouseSensitivityMultiplier;
        _defaultKeySensitivityMultiplier = _keySensitivityMultiplier;
        _defaultInvertHorizontal = _invertHorizontalRotationControl;
        _defaultInvertVertical = _invertVerticalRotationControl;

        LoadSettings();
    }

    void OnEnable()
    {
        _enabledFrame = Time.frameCount;
        CancelDrag();
    }

    void OnDisable()
    {
        CancelDrag();
    }

    //ドラッグ状態を破棄する
    private void CancelDrag()
    {
        _isRotateDragging = false;
        _isSlideDragging = false;
    }

    //このフレームでドラッグを開始してよいか
    private bool CanStartDrag => Time.frameCount != _enabledFrame;

    void Start ()
	{
		_camTransform = this.gameObject.transform;

		//初期回転・位置の保存
		_initialCamRotation = this.gameObject.transform.rotation;
        _initialCamPosition = this.gameObject.transform.position;

        ComputeCameraBounds();
	}

	void Update () {

		CamControlIsActive(); //カメラ操作の有効無効

        if (_cameraMoveActive)
		{
			HandleResetKey(); //位置・回転をデフォルトにリセット
            CameraRotationMouseControl(); //カメラの回転 マウス
            CameraSlideMouseControl(); //カメラの縦横移動 マウス
			CameraZoomMouseControl(); //カメラのズーム　マウス
            CameraPositionKeyControl(); //カメラのローカル移動 キー
            ClampCameraPosition(); //可動範囲の制限
        }
        else
        {
            CancelDrag();
        }
	}

    //床の大きさから可動範囲を算出する
    private void ComputeCameraBounds()
    {
        Bounds floorBounds = _floorReference.GetComponent<Collider>().bounds;
        Vector3 extents = floorBounds.extents * _boundsMarginMultiplier;

        _minX = floorBounds.center.x - extents.x;
        _maxX = floorBounds.center.x + extents.x;
        _minZ = floorBounds.center.z - extents.z;
        _maxZ = floorBounds.center.z + extents.z;
        _minY = floorBounds.max.y + _minHeightAboveFloor;
        _maxY = floorBounds.max.y + _maxHeightAboveFloor;
    }

    //カメラ位置を可動範囲内に収める
    private void ClampCameraPosition()
    {
        Vector3 pos = _camTransform.position;
        pos.x = Mathf.Clamp(pos.x, _minX, _maxX);
        pos.y = Mathf.Clamp(pos.y, _minY, _maxY);
        pos.z = Mathf.Clamp(pos.z, _minZ, _maxZ);
        _camTransform.position = pos;
    }
	
	//カメラ操作の有効無効
	public void CamControlIsActive()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			_cameraMoveActive = !_cameraMoveActive;

            if (_uiMessageActiv == false)
            {
                StartCoroutine(DisplayUiMessage());
            }            
			Debug.Log("CamControl : " + _cameraMoveActive);
		}
	}
	
	//Pキーでリセットを実行する
	private void HandleResetKey()
	{
		if (Input.GetKeyDown(KeyCode.P))
		{
			ResetCameraToDefault();
		}
	}

	//位置・回転を初期状態に戻す（UIボタンからも呼び出せる）
	public void ResetCameraToDefault()
	{
		// カメラ操作が無効化されている間（動物へのフォーカス中など）は何もしない
		if (!enabled) return;
        
        ForcedResetCameraToDefault();
    }

    //位置・回転を初期状態に戻す（強制）
    public void ForcedResetCameraToDefault()
    {
		_camTransform.position = _initialCamPosition;
		_camTransform.rotation = _initialCamRotation;
		Debug.Log("Cam Reset : pos=" + _initialCamPosition + " rot=" + _initialCamRotation);
	}
	
	//カメラの回転 マウス
	private void CameraRotationMouseControl()
	{
		float invertVertical = _invertVerticalRotationControl ? -1.0f : 1.0f;
		float invertHorizontal = _invertHorizontalRotationControl ? -1.0f : 1.0f;
		if (Input.GetMouseButtonDown(0) && CanStartDrag)
		{
			_isRotateDragging = true;
			_startMousePos = Input.mousePosition;
			//eulerAngles.xは[0,360)で返るため、-80〜80等の範囲でクランプできるよう(-180,180]に正規化して保存する
			float rawX = _camTransform.transform.eulerAngles.x;
			_presentCamRotation.x = rawX > 180f ? rawX - 360f : rawX;
			_presentCamRotation.y = _camTransform.transform.eulerAngles.y;
		}

		if (!Input.GetMouseButton(0))
		{
			_isRotateDragging = false;
		}

		if (_isRotateDragging)
		{
			//(移動開始座標 - マウスの現在座標) / 解像度 で正規化
			float x = (_startMousePos.x - Input.mousePosition.x) / Screen.width;
			float y = (_startMousePos.y - Input.mousePosition.y) / Screen.height;

			//回転開始角度 ＋ マウスの変化量 * マウス感度 * 反転
			float eulerX = _presentCamRotation.x + y * _mouseSensitive * _mouseSensitivityMultiplier * invertVertical;
			eulerX = Mathf.Clamp(eulerX, _minPitchAngle, _maxPitchAngle); // 真上・真下・反転を防ぐ
			float eulerY = _presentCamRotation.y + x * _mouseSensitive * _mouseSensitivityMultiplier * invertHorizontal;

			_camTransform.rotation = Quaternion.Euler(eulerX, eulerY, 0);
		}
	}
	
	//カメラの移動 マウス
	private void CameraSlideMouseControl()
	{
		if (Input.GetMouseButtonDown(1) && CanStartDrag)
		{
			_isSlideDragging = true;
			_startMousePos = Input.mousePosition;
			_presentCamPos = _camTransform.position;
		}

		if (!Input.GetMouseButton(1))
		{
			_isSlideDragging = false;
		}

		if (_isSlideDragging)
		{
			//(移動開始座標 - マウスの現在座標) / 解像度 で正規化
			float x = (_startMousePos.x - Input.mousePosition.x) / (Screen.width * 0.1f);
			float y = (_startMousePos.y - Input.mousePosition.y) / (Screen.height * 0.1f);

            x = x * _positionStep * _mouseSensitivityMultiplier;
            y = y * _positionStep * _mouseSensitivityMultiplier;

            Vector3 velocity = _camTransform.rotation * new Vector3(x, y, 0);
            velocity = velocity + _presentCamPos;
            _camTransform.position = velocity;
		}
	}

	private void CameraZoomMouseControl()
	{
		float scroll = Input.mouseScrollDelta.y;
		Vector3 campos = _camTransform.position;

		campos += _camTransform.forward * scroll * _positionStep * _mouseSensitivityMultiplier * 0.5f;

		_camTransform.position = campos;
	}
		
	//カメラのローカル移動 キー
	private void CameraPositionKeyControl()
	{
        Vector3 campos = _camTransform.position;
        float step = Time.deltaTime * _positionStep * _keySensitivityMultiplier;

		if (Input.GetKey(KeyCode.D)) { campos += _camTransform.right * step; }
		if (Input.GetKey(KeyCode.A)) { campos -= _camTransform.right * step; }
		if (Input.GetKey(KeyCode.E)) { campos += _camTransform.up * step; }
		if (Input.GetKey(KeyCode.Q)) { campos -= _camTransform.up * step; }
		if (Input.GetKey(KeyCode.W)) { campos += _camTransform.forward * step; }
		if (Input.GetKey(KeyCode.S)) { campos -= _camTransform.forward * step; }

		_camTransform.position = campos;
	}

    #region オプション画面からの設定

    //マウス感度の倍率を設定する
    public void SetMouseSensitivityMultiplier(float value)
    {
        _mouseSensitivityMultiplier = ClampSensitivityMultiplier(value);
        PlayerPrefs.SetFloat(MouseSensitivityKey, _mouseSensitivityMultiplier);
        PlayerPrefs.Save();
    }

    //キー操作（WASD・QE）の移動速度の倍率を設定する
    public void SetKeySensitivityMultiplier(float value)
    {
        _keySensitivityMultiplier = ClampSensitivityMultiplier(value);
        PlayerPrefs.SetFloat(KeySensitivityKey, _keySensitivityMultiplier);
        PlayerPrefs.Save();
    }

    //左右のカメラ回転を反転するか設定する
    public void SetInvertHorizontal(bool invert)
    {
        _invertHorizontalRotationControl = invert;
        PlayerPrefs.SetInt(InvertHorizontalKey, invert ? 1 : 0);
        PlayerPrefs.Save();
    }

    //上下のカメラ回転を反転するか設定する
    public void SetInvertVertical(bool invert)
    {
        _invertVerticalRotationControl = invert;
        PlayerPrefs.SetInt(InvertVerticalKey, invert ? 1 : 0);
        PlayerPrefs.Save();
    }

    //保存したカメラ設定を消してInspectorの初期値に戻す
    public void ResetSettingsToDefault()
    {
        PlayerPrefs.DeleteKey(MouseSensitivityKey);
        PlayerPrefs.DeleteKey(KeySensitivityKey);
        PlayerPrefs.DeleteKey(InvertHorizontalKey);
        PlayerPrefs.DeleteKey(InvertVerticalKey);
        PlayerPrefs.Save();

        _mouseSensitivityMultiplier = ClampSensitivityMultiplier(_defaultMouseSensitivityMultiplier);
        _keySensitivityMultiplier = ClampSensitivityMultiplier(_defaultKeySensitivityMultiplier);
        _invertHorizontalRotationControl = _defaultInvertHorizontal;
        _invertVerticalRotationControl = _defaultInvertVertical;
    }

    private void LoadSettings()
    {
        _mouseSensitivityMultiplier = ClampSensitivityMultiplier(PlayerPrefs.GetFloat(MouseSensitivityKey, _mouseSensitivityMultiplier));
        _keySensitivityMultiplier = ClampSensitivityMultiplier(PlayerPrefs.GetFloat(KeySensitivityKey, _keySensitivityMultiplier));
        _invertHorizontalRotationControl = PlayerPrefs.GetInt(InvertHorizontalKey, _invertHorizontalRotationControl ? 1 : 0) != 0;
        _invertVerticalRotationControl = PlayerPrefs.GetInt(InvertVerticalKey, _invertVerticalRotationControl ? 1 : 0) != 0;
    }

    private float ClampSensitivityMultiplier(float value)
    {
        return Mathf.Clamp(value, _minSensitivityMultiplier, _maxSensitivityMultiplier);
    }

    #endregion

    //UIメッセージの表示
    private IEnumerator DisplayUiMessage()
    {
        _uiMessageActiv = true;
        float time = 0;
        while (time < 2)
        {
            time = time + Time.deltaTime;
            yield return null;
        }
        _uiMessageActiv = false;
    }

    void OnGUI()
    {
        if (_uiMessageActiv == false) { return; }
        GUI.color = Color.black;
        if (_cameraMoveActive == true)
        {
            GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height - 30, 100, 20), "カメラ操作 有効");
        }

        if (_cameraMoveActive == false)
        {
            GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height - 30, 100, 20), "カメラ操作 無効");
        }
    }

}

