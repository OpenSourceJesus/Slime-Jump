using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//-------------------------------------------------------------------------
/// <summary>
/// Frontend class to deal with UnityEditor specific tasks which the
/// Runtime-dll is not allowed to access, but the script can.
/// The backend part is directly exposed via the .Backend member. So
/// methods and members other than BinaryAlphaThresholdImageFromTexture()
/// can be accessed by calling PolygonOutlineFromImageFrontend.Backend.method().
/// </summary>
public class PolygonOutlineFromImageFrontend {

    private Texture2D mLastSubImageTexture = null;
    private bool mLastUseRegion = false;
    private int mLastSubImageX = 0;
    private int mLastSubImageY = 0;
    private int mLastSubImageWidth = 0;
    private int mLastSubImageHeight = 0;
    private Color32[] mLastSubImage = null;
    private float mLastNormalizedAlphaThreshold = 0;
    private bool[] mLastUnerodedBinaryImage = null;
    private bool[] mLastErodedBinaryImage = null;

    private float mLowerTop = 0.0f;     // Erodes top pixels in a single direction to transperent before processing them. Value is [0..1] normalized to image region, not in pixels.
    private float mRaiseBottom = 0.0f;  // Erodes bottom pixels in a single direction to transperent before processing them. Value is [0..1] normalized to image region, not in pixels.
    private float mCutLeft = 0.0f;      // Erodes left pixels in a single direction to transperent before processing them. Value is [0..1] normalized to image region, not in pixels.
    private float mCutRight = 0.0f;     // Erodes right pixels in a single direction to transperent before processing them. Value is [0..1] normalized to image region, not in pixels.
    private float mExpandTop = 0.0f;    // Dilates top pixels in a single direction to solid before processing them. Value is [0..1] normalized to image region, not in pixels.
    private float mExpandBottom = 0.0f; // Dilates bottom pixels in a single direction to solid before processing them. Value is [0..1] normalized to image region, not in pixels.
    private float mExpandLeft = 0.0f;   // Dilates left pixels in a single direction to solid before processing them. Value is [0..1] normalized to image region, not in pixels.
    private float mExpandRight = 0.0f;  // Dilates right pixels in a single direction to solid before processing them. Value is [0..1] normalized to image region, not in pixels.

    private PolygonOutlineFromImage mBackend = new PolygonOutlineFromImage();
    private PolygonOutlineFromImage Backend {
        get {
            return mBackend;
        }
    }

    public string LastError
    {
        get
        {
            return mBackend.GetLastError();
        }
    }
	
	//-------------------------------------------------------------------------
	// ALGORITHM PARAMETERS
	//-------------------------------------------------------------------------
	public float VertexReductionDistanceTolerance {
		set {
			mBackend.mVertexReductionDistanceTolerance = value;
		}
		get {
			return mBackend.mVertexReductionDistanceTolerance;
		}
	}
	public int MaxPointCount {
		set {
			mBackend.mMaxPointCount = value;
		}
		get {
			return mBackend.mMaxPointCount;
		}
	}
	public bool Convex {
		set {
			mBackend.mConvex = value;
		}
		get {
			return mBackend.mConvex;
		}
	}
	public float XOffsetNormalized {
		set {
			mBackend.mXOffsetNormalized = value;
		}
		get {
			return mBackend.mXOffsetNormalized;
		}
	}
	public float YOffsetNormalized {
		set {
			mBackend.mYOffsetNormalized = value;
		}
		get {
			return mBackend.mYOffsetNormalized;
		}
	}
	public float XScale {
		set {
			mBackend.mXScale = value;
		}
		get {
			return mBackend.mXScale;
		}
	}
	public float YScale {
		set {
			mBackend.mYScale = value;
		}
		get {
			return mBackend.mYScale;
		}
	}
	public float Thickness {
		set {
			mBackend.mThickness = value;
		}
		get {
			return mBackend.mYScale;
		}
	}
	
	// Used to extract a region of an input binary image that is not exactly following pixel borders but cuts out a region of e.g. [0.3px to width-0.5px]. Outside vertices are clamped component-wise to the border.
	public float RegionPixelCutawayLeft {
		set {
			mBackend.mRegionPixelCutawayLeft = value;
		}
		get {
			return mBackend.mRegionPixelCutawayLeft;
		}
	}
	// See above.
    public float RegionPixelCutawayRight {
		set {
			mBackend.mRegionPixelCutawayRight = value;
		}
		get {
			return mBackend.mRegionPixelCutawayRight;
		}
	}
	// See above. Top is at the positive side of the image's y axis, thus mapping to the vertex position <width>.
    public float RegionPixelCutawayTop {
		set {
			mBackend.mRegionPixelCutawayTop = value;
		}
		get {
			return mBackend.mRegionPixelCutawayTop;
		}
	}
	// See above. Bottom is at the origin of the image's y axis, thus mapping to the vertex position <0>.
    public float RegionPixelCutawayBottom {
		set {
			mBackend.mRegionPixelCutawayBottom = value;
		}
		get {
			return mBackend.mRegionPixelCutawayBottom;
		}
	}
	
    public bool NormalizeResultToCutRegion {
		set {
			mBackend.mNormalizeResultToCutRegion = value;
		}
		get {
			return mBackend.mNormalizeResultToCutRegion;
		}
	}
    
    public float LowerTop {
        set {
            mLowerTop = value;
        }
        get {
            return mLowerTop;
        }
    }
    public float RaiseBottom {
        set {
            mRaiseBottom = value;
        }
        get {
            return mRaiseBottom;
        }
    }
    public float CutLeft {
        set {
            mCutLeft = value;
        }
        get {
            return mCutLeft;
        }
    }
    public float CutRight {
        set {
            mCutRight = value;
        }
        get {
            return mCutRight;
        }
    }

    public float ExpandTop {
        set {
            mExpandTop = value;
        }
        get {
            return mExpandTop;
        }
    }
    public float ExpandBottom {
        set {
            mExpandBottom = value;
        }
        get {
            return mExpandBottom;
        }
    }
    public float ExpandLeft {
        set {
            mExpandLeft = value;
        }
        get {
            return mExpandLeft;
        }
    }
    public float ExpandRight {
        set {
            mExpandRight = value;
        }
        get {
            return mExpandRight;
        }
    }

    //-------------------------------------------------------------------------
    /// <param name='binaryImage'>
    /// A bool array representing the resuling threshold image. In row-major order, thus accessed binaryImage[y,x].
    /// </param>
    public bool BinaryAlphaThresholdImageFromTexture(ref bool[] binaryImage, out int binaryImageWidth, out int binaryImageHeight,
                                                     Texture2D texture, float normalizedAlphaThreshold,
													 bool useRegion, int regionX, int regionYFromTop, int regionWidth, int regionHeight) {
		
		bool isCachedSubImageUpToDate = IsCachedImageUpToDate(texture, useRegion, regionX, regionYFromTop, regionWidth, regionHeight);
        bool isCachedUnerodedBinaryImageUpToDate = isCachedSubImageUpToDate && (normalizedAlphaThreshold == mLastNormalizedAlphaThreshold);
        
        if (!isCachedSubImageUpToDate) {
			if (!ReadAndCacheSubImage(texture, useRegion, regionX, regionYFromTop, regionWidth, regionHeight)) { // this method calls SetLastError with a precise message.
				binaryImage = null;
                binaryImageWidth = 0;
                binaryImageHeight = 0;
                return false;
			}
		}
        
        int width = mLastSubImageWidth;
        int height = mLastSubImageHeight;
        binaryImageWidth = width;
        binaryImageHeight = height;

        if (!isCachedUnerodedBinaryImageUpToDate) {
            byte alphaThreshold8Bit = (byte)(normalizedAlphaThreshold * 255.0f);
            if (alphaThreshold8Bit == 0) {
                alphaThreshold8Bit = 1; // Note: we won't test for >= 0, we test at least >= 1.
            }
            int imageSize = width * height;
            if (mLastUnerodedBinaryImage == null || mLastUnerodedBinaryImage.Length < imageSize)
                mLastUnerodedBinaryImage = new bool[imageSize];

            // NOTE: mainTexPixels is read from bottom left origin upwards.
            for (int y = 0; y < height; ++y) {
                
                for (int x = 0; x < width; ++x) {
                    
                    byte alpha = mLastSubImage[x + y * width].a;
                    if (alpha >= alphaThreshold8Bit)
                        mLastUnerodedBinaryImage[x + y * width] = true;
                    else
                        mLastUnerodedBinaryImage[x + y * width] = false;
                }
            }
        }
        mLastNormalizedAlphaThreshold = normalizedAlphaThreshold;
        binaryImage = mLastUnerodedBinaryImage;
        
        if (mLowerTop != 0 || mRaiseBottom != 0 || mCutLeft != 0 || mCutRight != 0 ||
            mExpandTop != 0 || mExpandBottom != 0 || mExpandLeft != 0 || mExpandRight != 0) {

            if (mLastErodedBinaryImage == null || mLastErodedBinaryImage.Length != mLastUnerodedBinaryImage.Length) {
                mLastErodedBinaryImage = new bool[mLastUnerodedBinaryImage.Length];
            }
            System.Array.Copy(mLastUnerodedBinaryImage, mLastErodedBinaryImage, mLastUnerodedBinaryImage.Length);
            
            mBackend.DirectionalErodeBinaryImagePixels(ref mLastErodedBinaryImage, width, height, mLowerTop, mRaiseBottom, mCutLeft, mCutRight);
            mBackend.DirectionalDilateBinaryImagePixels(ref mLastErodedBinaryImage, width, height, mExpandTop, mExpandBottom, mExpandLeft, mExpandRight);
            binaryImage = mLastErodedBinaryImage;
        }
        return true;
	}
    
    //-------------------------------------------------------------------------
    private bool IsCachedImageUpToDate(Texture2D texture, bool useRegion,
										 int regionX, int regionYFromTop, int regionWidth, int regionHeight) {
		
		return (mLastSubImageTexture == texture &&
			    mLastUseRegion == useRegion &&
			    mLastSubImageX == regionX &&
			    mLastSubImageY == regionYFromTop &&
			    mLastSubImageWidth == regionWidth &&
			    mLastSubImageHeight == regionHeight &&
			    mLastSubImage != null);
	}
	
	//-------------------------------------------------------------------------
	private bool ReadAndCacheSubImage(Texture2D texture, bool useRegion,
										int regionX, int regionYFromTop, int regionWidth, int regionHeight) {
		
		Color32[] texturePixels = null;
		try {
			texturePixels = texture.GetPixels32();
		}
		catch (System.Exception ) {
			// expected behaviour, if the texture is read-only.
		}
			
		bool wasTextureReadOnly = (texturePixels == null);
		if (wasTextureReadOnly) {
			if (!SetTextureReadable(texture, true)) {
				SetLastError("Unable to set the texture '" + texture.name + "' to readable state. Aborting collider mesh generation. Please set the texture's readable flag manually via the editor.");
				return false;
			}
			
			texturePixels = texture.GetPixels32();
		}
		
		int destWidth = texture.width;
		int destHeight = texture.height;
		int srcRegionOffsetX = 0;
		int srcRegionOffsetY = 0;  // = bottom left origin
		
		if (!useRegion) {
			mLastSubImage = texturePixels;
		}
		else {
			destWidth = regionWidth;
			destHeight = regionHeight;
			srcRegionOffsetX = regionX;
			srcRegionOffsetY = texture.height - destHeight - regionYFromTop; // regionYFromTop is measured from top(-left) origin to the top of the region.

            int destSize = destHeight * destWidth;
            if (mLastSubImage == null || mLastSubImage.Length < destSize) {
                mLastSubImage = new Color32[destSize];
            }
            else {
                // nothing todo.
            }
		
			// NOTE: mainTexPixels is read from bottom left origin upwards.
			for (int destY = 0; destY < destHeight; ++destY) {
				for (int destX = 0; destX < destWidth; ++destX) {
					int srcX = destX + srcRegionOffsetX;
					int srcY = destY + srcRegionOffsetY;
					int destIndex = destY * destWidth + destX;
					int srcIndex = srcY * texture.width + srcX;
					mLastSubImage[destIndex] = texturePixels[srcIndex];
				}
			}
		}
		
		mLastSubImageTexture = texture;
		mLastUseRegion = useRegion;
		mLastSubImageX = regionX;
		mLastSubImageY = regionYFromTop;
		mLastSubImageWidth = destWidth;
		mLastSubImageHeight = destHeight;
		
		if (wasTextureReadOnly) {
			if (!SetTextureReadable(texture, false)) {
				SetLastError("Unable to set the texture '" + texture.name + "' back to read-only state. Continuing anyway, please set the texture's readable flag manually via the editor.");
				return false;
			}
		}
		return true;
	}
	
	//-------------------------------------------------------------------------
	public bool SetTextureReadable(Texture2D texture, bool readable) {
		
#if UNITY_EDITOR
		string texturePath = UnityEditor.AssetDatabase.GetAssetPath(texture); 
		if (!System.IO.File.Exists(texturePath)) {
			SetLastError("Aborting Generation: Texture at path " + texturePath + " not found while changing import settings - did you delete it?");
			return false;
		}
		
		UnityEditor.TextureImporter textureImporter = (UnityEditor.TextureImporter) UnityEditor.AssetImporter.GetAtPath(texturePath);
        textureImporter.isReadable = readable;
        UnityEditor.AssetDatabase.ImportAsset(texturePath);
#endif
		return true;
	}

    //-------------------------------------------------------------------------
	private void SetLastError(string description) {
        mBackend.SetLastError(description);
	}
	
	//-------------------------------------------------------------------------
	// METHODS SIMPLY FORWARDED TO THE BACKEND CLASS
	//-------------------------------------------------------------------------
	//-------------------------------------------------------------------------
	/// <summary>
	/// Performs all steps of the collider computation and fills the output
	/// parameters vertices and triangleIndices with data of an (uncapped)
	/// triangle-fence.
	/// </summary>
	/// <param name='vertices'>
	/// The resulting vertex array, can e.g. be assigned at MeshCollider.sharedMesh.vertices.
	/// </param>
	/// <param name='triangleIndices'>
	/// The resulting triangle indices for the vertex array, can e.g. be assigned at MeshCollider.sharedMesh.triangles.
	/// </param>
	/// <param name='binaryImage'>
	/// The input binary image as a bool array. True represents island-pixels, false sea-pixels.
	/// </param>
	/// <param name='regionStartingPoint'>
	/// The starting point within the desired island- or sea-region for the collider computation.
	/// </param>
	/// <param name='isIsland'>
	/// If true, then the outline of island-pixels connected to regionStartingPoint is computed. If false, the outline of sea-pixels is computed.
	/// </param>
	/// <param name='outputVerticesInNormalizedSpace'>
	/// If true, the resulting vertices will be in the range [0..1], if false it will be [0..imageWidth].
	/// </param>
	/// <param name='traverseInCCWOrder'>
	/// If true, the resulting outline will be in counter-clockwise (ccw) order, thus normals pointing outwards (usually needed at islands).
	/// If false, the outline will be in clockwise (cw) order, thus normals pointing inwards (usually needed at sea-regions).
	/// </param>
    public void TriangleFenceOutlineFromBinaryImage(out Vector3[] vertices, out int[] triangleIndices, bool[] binaryImage, int binaryImageWidth, int binaryImageHeight, IntVector2 regionStartingPoint, bool isIsland, bool outputVerticesInNormalizedSpace, bool traverseInCCWOrder)
    {
        mBackend.TriangleFenceOutlineFromBinaryImage(out vertices, out triangleIndices, binaryImage, binaryImageWidth, binaryImageHeight, regionStartingPoint, isIsland, outputVerticesInNormalizedSpace, traverseInCCWOrder);
	}
	
	//-------------------------------------------------------------------------
	/// <summary>
	/// Performs all steps of the collider computation and fills the output
	/// parameters vertices and quadIndices with data of an (uncapped)
	/// quad-fence.
	/// </summary>
	/// <param name='vertices'>
	/// The resulting vertex array.
	/// </param>
	/// <param name='quadIndices'>
	/// The resulting quad indices for the vertex array.
	/// </param>
	/// <param name='binaryImage'>
	/// The input binary image as a bool array. True represents island-pixels, false sea-pixels.
	/// </param>
	/// <param name='regionStartingPoint'>
	/// The starting point within the desired island- or sea-region for the collider computation.
	/// </param>
	/// <param name='isIsland'>
	/// If true, then the outline of island-pixels connected to regionStartingPoint is computed. If false, the outline of sea-pixels is computed.
	/// </param>
	/// <param name='outputVerticesInNormalizedSpace'>
	/// If true, the resulting vertices will be in the range [0..1], if false it will be [0..imageWidth].
	/// </param>
	/// <param name='traverseInCCWOrder'>
	/// If true, the resulting outline will be in counter-clockwise (ccw) order, thus normals pointing outwards (usually needed at islands).
	/// If false, the outline will be in clockwise (cw) order, thus normals pointing inwards (usually needed at sea-regions).
	/// </param>
    public void QuadFenceOutlineFromBinaryImage(out Vector3[] vertices, out int[] quadIndices, bool[] binaryImage, int binaryImageWidth, int binaryImageHeight, IntVector2 regionStartingPoint, bool isIsland, bool outputVerticesInNormalizedSpace, bool traverseInCCWOrder)
    {
		mBackend.QuadFenceOutlineFromBinaryImage(out vertices, out quadIndices, binaryImage, binaryImageWidth, binaryImageHeight, regionStartingPoint, isIsland, outputVerticesInNormalizedSpace, traverseInCCWOrder);
	}
	
	//-------------------------------------------------------------------------
	/// <summary>
	/// Performs multiple steps of the collider computation at once and fills the output
	/// parameter outlinePolygonVertexList with data of the computed outline polygon.
	/// </summary>
	/// <param name='outlinePolygonVertexList'>
	/// The resulting outline polygon vertices.
	/// </param>
	/// <param name='binaryImage'>
	/// The input binary image as a bool array. True represents island-pixels, false sea-pixels.
	/// </param>
	/// <param name='regionStartingPoint'>
	/// The starting point within the desired island- or sea-region for the collider computation.
	/// </param>
	/// <param name='isIsland'>
	/// If true, then the outline of island-pixels connected to regionStartingPoint is computed. If false, the outline of sea-pixels is computed.
	/// </param>
	/// <param name='outputVerticesInNormalizedSpace'>
	/// If true, the resulting vertices will be in the range [0..1], if false it will be [0..imageWidth].
	/// </param>
	/// <param name='traverseInCCWOrder'>
	/// If true, the resulting outline will be in counter-clockwise (ccw) order, thus normals pointing outwards (usually needed at islands).
	/// If false, the outline will be in clockwise (cw) order, thus normals pointing inwards (usually needed at sea-regions).
	/// </param>
    public void PolygonOutlineFromBinaryImage(ref List<Vector2> outlinePolygonVertexList, bool[] binaryImage, int binaryImageWidth, int binaryImageHeight, IntVector2 regionStartingPoint, bool isIsland, bool outputVerticesInNormalizedSpace, bool traverseInCCWOrder)
    {
		mBackend.PolygonOutlineFromBinaryImage(ref outlinePolygonVertexList, binaryImage, binaryImageWidth, binaryImageHeight, regionStartingPoint, isIsland, outputVerticesInNormalizedSpace, traverseInCCWOrder);
	}
	
	//-------------------------------------------------------------------------
	/// <summary>
	/// First step in the TriangleFenceOutlineFromBinaryImage and
	/// QuadFenceOutlineFromBinaryImage algorithms above.
	/// First call this method, then pass the <c>outlineVertices</c> to the
	/// TriangleFenceFromOutlineVertices method.
	/// </summary>
	/// <param name='outlineVertices'>
	/// The resulting vertices describing the outline, not optimized yet.
	/// Pass this output parameter to the TriangleFenceFromOutlineVertices
	/// method to get an optimized outline triangle fence.
	/// </param>
	/// /// <param name='regionStartingPoint'>
	/// The starting point within the desired island- or sea-region for the collider computation.
	/// </param>
	/// <param name='isIsland'>
	/// If true, then the outline of island-pixels connected to regionStartingPoint is computed. If false, the outline of sea-pixels is computed.
	/// </param>
	/// <param name='outputVerticesInNormalizedSpace'>
	/// If true, the resulting vertices will be in the range [0..1], if false it will be [0..imageWidth].
	/// </param>
	/// <param name='traverseInCCWOrder'>
	/// If true, the resulting outline will be in counter-clockwise (ccw) order, thus normals pointing outwards (usually needed at islands).
	/// If false, the outline will be in clockwise (cw) order, thus normals pointing inwards (usually needed at sea-regions).
	/// </param>
    public void UnreducedOutlineFromBinaryImage(ref List<Vector2> outlineVertices, bool[] binaryImage, int binaryImageWidth, int binaryImageHeight, IntVector2 regionStartingPoint, bool isIsland, bool outputVerticesInNormalizedSpace, bool traverseInCCWOrder)
    {
        mBackend.UnreducedOutlineFromBinaryImage(ref outlineVertices, binaryImage, binaryImageWidth, binaryImageHeight, regionStartingPoint, isIsland, outputVerticesInNormalizedSpace, traverseInCCWOrder);
	}
	
	//-------------------------------------------------------------------------
	/// <summary>
	/// Reduces the vertex count of a given Vector2 vertex list.
	/// The reduction algorithm produces no more vertices than both criteria
	/// <c>mVertexReductionDistanceTolerance</c> and <c>mMaxPointCount</c>
	/// combined allow for.
	/// </summary>
	/// <returns>
	/// The outline with reduced vertex count.
	/// </returns>
	/// <param name='unreducedOutlineVertices'>
	/// The input un-optimized outline polygon vertices.
	/// </param>
	/// <param name='outlineWasCreatedInCCWOrder'>
	/// Whether the outline was created in counter-clockwise (CCW) order. This parameter is
	/// important only if convex is set to true at the parameters.
	/// </param>
	public void ReduceOutline(List<Vector2> unreducedOutlineVertices, ref List<Vector2> resultReducedPoints, ref List<int> resultUsedIndices, bool outlineWasCreatedInCCWOrder) {

        if (unreducedOutlineVertices == null || unreducedOutlineVertices.Count == 0) {
            return;
        }
        mBackend.ReduceOutline(unreducedOutlineVertices, ref resultReducedPoints, ref resultUsedIndices, outlineWasCreatedInCCWOrder);
	}
	
	//-------------------------------------------------------------------------
	/// <summary>
	/// Last step in the TriangleFenceOutlineFromBinaryImage algorithm above.
	/// First call UnreducedOutlineFromBinaryImage, then optionally ReduceOutline
	/// and finally pass the returned <c>outlineVertices</c> to this method.
	/// </summary>
	/// <param name='resultFenceVertices'>
	/// The resulting vertex array, can e.g. be assigned at MeshCollider.sharedMesh.vertices.
	/// </param>
	/// <param name='triangleIndices'>
	/// The resulting triangle indices for the vertex array, can e.g. be assigned at MeshCollider.sharedMesh.triangles.
	/// </param>
	/// <param name='outlineVertices'>
	/// The input outline polygon vertices.
	/// </param>
	/// <param name='reverseVertexOrder'>
	/// If true, the resulting triangle-fence will be in opposite (clockwise vs. counter-clockwise) order
	/// than the input outlineVertices.
	/// </param>
	public void TriangleFenceFromOutline(out Vector3[] resultFenceVertices, out int[] triangleIndices, List<Vector2> outlineVertices, bool reverseVertexOrder) {
		
		mBackend.TriangleFenceFromOutline(out resultFenceVertices, out triangleIndices, outlineVertices, reverseVertexOrder);
	}
	
	//-------------------------------------------------------------------------
	/// <summary>
	/// Last step in the QuadFenceOutlineFromBinaryImage algorithm above.
	/// First call UnreducedOutlineFromBinaryImage, then optionally ReduceOutline
	/// and finally pass the returned <c>outlineVertices</c> to this method.
	/// </summary>
	/// <param name='resultFenceVertices'>
	/// The resulting vertex array.
	/// </param>
	/// <param name='quadIndices'>
	/// The resulting quad indices for the vertex array.
	/// </param>
	/// <param name='outlineVertices'>
	/// The input outline polygon vertices.
	/// </param>
	/// <param name='reverseVertexOrder'>
	/// If true, the resulting triangle-fence will be in opposite (clockwise vs. counter-clockwise) order
	/// than the input outlineVertices.
	/// </param>
	public void QuadFenceFromOutline(out Vector3[] resultFenceVertices, out int[] quadIndices, List<Vector2> outlineVertices, bool reverseVertexOrder) {
		
		mBackend.QuadFenceFromOutline(out resultFenceVertices, out quadIndices, outlineVertices, reverseVertexOrder);
	}
}
