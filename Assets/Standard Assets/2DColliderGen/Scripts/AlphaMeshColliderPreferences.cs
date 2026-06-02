#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2)
#define UNITY_4_3_AND_LATER
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//-------------------------------------------------------------------------
/// <summary>
/// Class to read and write AlphaMeshCollider relevant editor preference values.
/// </summary>
public class AlphaMeshColliderPreferences
{

#if UNITY_EDITOR
	//-------------------------------------------------------------------------
	/// <summary> The singleton instance. </summary>
	static AlphaMeshColliderPreferences mInstance = null;	
	public static AlphaMeshColliderPreferences Instance {
		get {
			if (mInstance == null) {
				mInstance = new AlphaMeshColliderPreferences();
				mInstance.ReadAllParams();
			}
			return mInstance;
		}
	}
	
	const string INITIAL_COLLIDER_PATH = "Assets/Colliders/Generated";
	//-------------------------------------------------------------------------	
	string mDefaultColliderDirectory = INITIAL_COLLIDER_PATH;
	bool mDefaultLiveUpdate;
    float mDefaultAlphaOpaqueThreshold;
	int mDefaultColliderPointCount;
	int mColliderPointCountSliderMaxValue;
	bool mDefaultConvex;
    bool mDefaultTrigger;
	float mDefaultAbsoluteColliderThickness;
#if UNITY_4_3_AND_LATER
	AlphaMeshCollider.TargetColliderType mDefaultTargetColliderType = AlphaMeshCollider.TargetColliderType.PolygonCollider2D;
#endif
    bool mDefaultFlipNormals;
    float mDefaultLowerTop;
    float mDefaultRaiseBottom;
    float mDefaultCutLeft;
    float mDefaultCutRight;
    float mDefaultExpandTop;
    float mDefaultExpandBottom;
    float mDefaultExpandLeft;
    float mDefaultExpandRight;
    float mCutSidesSliderMaxValue;

    //-------------------------------------------------------------------------
    public string DefaultColliderDirectory {
		get {
			return mDefaultColliderDirectory;
		}
		set {
			if (mDefaultColliderDirectory != value) {
				string correctedPath = value;
				if (correctedPath.Length==0) {
					correctedPath = INITIAL_COLLIDER_PATH;
				}
				mDefaultColliderDirectory = correctedPath;
				EditorPrefs.SetString("AlphaMeshCollider_DefaultColliderDirectory", mDefaultColliderDirectory);
			}
		}
	}
	public bool DefaultLiveUpdate {
		get {
			return mDefaultLiveUpdate;
		}
		set {
			if (mDefaultLiveUpdate != value) {
				mDefaultLiveUpdate = value;
				EditorPrefs.SetBool("AlphaMeshCollider_DefaultLiveUpdate", mDefaultLiveUpdate);
			}
		}
	}
    public float DefaultAlphaOpaqueThreshold {
        get {
            return mDefaultAlphaOpaqueThreshold;
        }
        set {
            if (mDefaultAlphaOpaqueThreshold != value) {
                mDefaultAlphaOpaqueThreshold = value;
                EditorPrefs.SetFloat("AlphaMeshCollider_DefaultAlphaOpaqueThreshold", mDefaultAlphaOpaqueThreshold);
            }
        }
    }
	public int DefaultColliderPointCount {
		get {
			return mDefaultColliderPointCount;
		}
		set {
			if (mDefaultColliderPointCount != value) {
				mDefaultColliderPointCount = value;
				if (mDefaultColliderPointCount < 2)
					mDefaultColliderPointCount = 2;
				EditorPrefs.SetInt("AlphaMeshCollider_DefaultColliderPointCount", mDefaultColliderPointCount);
			}
		}
	}
	public int ColliderPointCountSliderMaxValue {
		get {
			return mColliderPointCountSliderMaxValue;
		}
		set {
			if (mColliderPointCountSliderMaxValue != value)  {
				mColliderPointCountSliderMaxValue = value;
				if (mColliderPointCountSliderMaxValue < 4)
					mColliderPointCountSliderMaxValue = 4;
				EditorPrefs.SetInt("AlphaMeshCollider_ColliderPointCountSliderMaxValue", mColliderPointCountSliderMaxValue);
			}
		}
	}
	public bool DefaultConvex {
		get {
			return mDefaultConvex;
		}
		set {
			if (mDefaultConvex != value) {
				mDefaultConvex = value;
				EditorPrefs.SetBool("AlphaMeshCollider_DefaultConvex", mDefaultConvex);
			}
		}
	}

    public bool DefaultTrigger
    {
        get {
            return mDefaultTrigger;
        }
        set {
            if (mDefaultTrigger != value) {
                mDefaultTrigger = value;
                EditorPrefs.SetBool("AlphaMeshCollider_DefaultTrigger", mDefaultTrigger);
            }
        }
    }

    public bool DefaultFlipNormals {
        get {
            return mDefaultFlipNormals;
        }
        set {
            if (mDefaultFlipNormals != value) {
                mDefaultFlipNormals = value;
                EditorPrefs.SetBool("AlphaMeshCollider_DefaultFlipNormals", mDefaultFlipNormals);
            }
        }
    }

    public float DefaultLowerTop {
        get {
            return mDefaultLowerTop;
        }
        set {
            if (mDefaultLowerTop != value) {
                mDefaultLowerTop = value;
                EditorPrefs.SetFloat("AlphaMeshCollider_DefaultLowerTop", mDefaultLowerTop);
            }
        }
    }
    public float DefaultRaiseBottom {
        get {
            return mDefaultRaiseBottom;
        }
        set {
            if (mDefaultRaiseBottom != value) {
                mDefaultRaiseBottom = value;
                EditorPrefs.SetFloat("AlphaMeshCollider_DefaultRaiseBottom", mDefaultRaiseBottom);
            }
        }
    }
    public float DefaultCutLeft {
        get {
            return mDefaultCutLeft;
        }
        set {
            if (mDefaultCutLeft != value) {
                mDefaultCutLeft = value;
                EditorPrefs.SetFloat("AlphaMeshCollider_DefaultCutLeft", mDefaultCutLeft);
            }
        }
    }
    public float DefaultCutRight {
        get {
            return mDefaultCutRight;
        }
        set {
            if (mDefaultCutRight != value) {
                mDefaultCutRight = value;
                EditorPrefs.SetFloat("AlphaMeshCollider_DefaultCutRight", mDefaultCutRight);
            }
        }
    }


    public float DefaultExpandTop {
        get {
            return mDefaultExpandTop;
        }
        set {
            if (mDefaultExpandTop != value) {
                mDefaultExpandTop = value;
                EditorPrefs.SetFloat("AlphaMeshCollider_DefaultExpandTop", mDefaultExpandTop);
            }
        }
    }
    public float DefaultExpandBottom {
        get {
            return mDefaultExpandBottom;
        }
        set {
            if (mDefaultExpandBottom != value) {
                mDefaultExpandBottom = value;
                EditorPrefs.SetFloat("AlphaMeshCollider_DefaultExpandBottom", mDefaultExpandBottom);
            }
        }
    }
    public float DefaultExpandLeft {
        get {
            return mDefaultExpandLeft;
        }
        set {
            if (mDefaultExpandLeft != value) {
                mDefaultExpandLeft = value;
                EditorPrefs.SetFloat("AlphaMeshCollider_DefaultExpandLeft", mDefaultExpandLeft);
            }
        }
    }
    public float DefaultExpandRight {
        get {
            return mDefaultExpandRight;
        }
        set {
            if (mDefaultExpandRight != value) {
                mDefaultExpandRight = value;
                EditorPrefs.SetFloat("AlphaMeshCollider_DefaultExpandRight", mDefaultExpandRight);
            }
        }
    }


    public float CutSidesSliderMaxValue {
        get {
            return mCutSidesSliderMaxValue;
        }
        set {
            if (mCutSidesSliderMaxValue != value) {
                mCutSidesSliderMaxValue = value;
                EditorPrefs.SetFloat("AlphaMeshCollider_CutSidesSliderMaxValue", mCutSidesSliderMaxValue);
            }
        }
    }

    public float DefaultAbsoluteColliderThickness {
		get {
			return mDefaultAbsoluteColliderThickness;
		}
		set {
			if (mDefaultAbsoluteColliderThickness != value) {
				mDefaultAbsoluteColliderThickness = value;
				EditorPrefs.SetFloat("AlphaMeshCollider_DefaultAbsoluteColliderThickness", mDefaultAbsoluteColliderThickness);
			}
		}
	}

#if UNITY_4_3_AND_LATER
	public AlphaMeshCollider.TargetColliderType DefaultTargetColliderType {
		get {
			return mDefaultTargetColliderType;
		}
		set {
			if (mDefaultTargetColliderType != value) {
				mDefaultTargetColliderType = value;
				EditorPrefs.SetInt("AlphaMeshCollider_DefaultTargetColliderType", (int)mDefaultTargetColliderType);
			}
		}
	}
#endif
	//-------------------------------------------------------------------------
	
	//-------------------------------------------------------------------------
	void ReadAllParams()
	{
		mDefaultColliderDirectory = EditorPrefs.GetString("AlphaMeshCollider_DefaultColliderDirectory", INITIAL_COLLIDER_PATH);
		mDefaultLiveUpdate = EditorPrefs.GetBool("AlphaMeshCollider_DefaultLiveUpdate", true);
        mDefaultAlphaOpaqueThreshold = EditorPrefs.GetFloat("AlphaMeshCollider_DefaultAlphaOpaqueThreshold", 0.1f);
		mDefaultColliderPointCount = EditorPrefs.GetInt("AlphaMeshCollider_DefaultColliderPointCount", 20);
		mColliderPointCountSliderMaxValue = EditorPrefs.GetInt("AlphaMeshCollider_ColliderPointCountSliderMaxValue", 100);
		mDefaultAbsoluteColliderThickness = EditorPrefs.GetFloat("AlphaMeshCollider_DefaultAbsoluteColliderThickness", 2.0f);
#if UNITY_4_3_AND_LATER
		mDefaultTargetColliderType = (AlphaMeshCollider.TargetColliderType) EditorPrefs.GetInt("AlphaMeshCollider_DefaultTargetColliderType", 1); // defaults to 1 == PolygonCollider2D
#endif
        mDefaultConvex = EditorPrefs.GetBool("AlphaMeshCollider_DefaultConvex", false);
        mDefaultTrigger = EditorPrefs.GetBool("AlphaMeshCollider_DefaultTrigger", false);
        mDefaultFlipNormals = EditorPrefs.GetBool("AlphaMeshCollider_DefaultFlipNormals", false);

        mDefaultLowerTop = EditorPrefs.GetFloat("AlphaMeshCollider_DefaultLowerTop", 0.0f);
        mDefaultRaiseBottom = EditorPrefs.GetFloat("AlphaMeshCollider_DefaultRaiseBottom", 0.0f);
        mDefaultCutLeft = EditorPrefs.GetFloat("AlphaMeshCollider_DefaultCutLeft", 0.0f);
        mDefaultCutRight = EditorPrefs.GetFloat("AlphaMeshCollider_DefaultCutRight", 0.0f);
        mDefaultExpandTop = EditorPrefs.GetFloat("AlphaMeshCollider_DefaultExpandTop", 0.0f);
        mDefaultExpandBottom = EditorPrefs.GetFloat("AlphaMeshCollider_DefaultExpandBottom", 0.0f);
        mDefaultExpandLeft = EditorPrefs.GetFloat("AlphaMeshCollider_DefaultExpandLeft", 0.0f);
        mDefaultExpandRight = EditorPrefs.GetFloat("AlphaMeshCollider_DefaultExpandRight", 0.0f);

        mCutSidesSliderMaxValue = EditorPrefs.GetFloat("AlphaMeshCollider_CutSidesSliderMaxValue", 0.5f);
    }
	
	//-------------------------------------------------------------------------
	public void WriteAllParams()
	{
		EditorPrefs.SetString("AlphaMeshCollider_DefaultColliderDirectory", mDefaultColliderDirectory);
		EditorPrefs.SetBool("AlphaMeshCollider_DefaultLiveUpdate", mDefaultLiveUpdate);
        EditorPrefs.SetFloat("AlphaMeshCollider_DefaultAlphaOpaqueThreshold", mDefaultAlphaOpaqueThreshold);
		EditorPrefs.SetInt("AlphaMeshCollider_DefaultColliderPointCount", mDefaultColliderPointCount);
		EditorPrefs.SetInt("AlphaMeshCollider_ColliderPointCountSliderMaxValue", mColliderPointCountSliderMaxValue);
		
		EditorPrefs.SetFloat("AlphaMeshCollider_DefaultAbsoluteColliderThickness", mDefaultAbsoluteColliderThickness);
        EditorPrefs.SetBool("AlphaMeshCollider_DefaultConvex", mDefaultConvex);
        EditorPrefs.SetBool("AlphaMeshCollider_DefaultTrigger", mDefaultTrigger);
        EditorPrefs.SetBool("AlphaMeshCollider_DefaultFlipNormals", mDefaultFlipNormals);

        EditorPrefs.SetFloat("AlphaMeshCollider_DefaultLowerTop", mDefaultLowerTop);
        EditorPrefs.SetFloat("AlphaMeshCollider_DefaultRaiseBottom", mDefaultRaiseBottom);
        EditorPrefs.SetFloat("AlphaMeshCollider_DefaultCutLeft", mDefaultCutLeft);
        EditorPrefs.SetFloat("AlphaMeshCollider_DefaultCutRight", mDefaultCutRight);
        EditorPrefs.SetFloat("AlphaMeshCollider_DefaultExpandTop", mDefaultExpandTop);
        EditorPrefs.SetFloat("AlphaMeshCollider_DefaultExpandBottom", mDefaultExpandBottom);
        EditorPrefs.SetFloat("AlphaMeshCollider_DefaultExpandLeft", mDefaultExpandLeft);
        EditorPrefs.SetFloat("AlphaMeshCollider_DefaultExpandRight", mDefaultExpandRight);
        EditorPrefs.SetFloat("AlphaMeshCollider_CutSidesSliderMaxValue", mCutSidesSliderMaxValue);
    }
#endif // UNITY_EDITOR
}
